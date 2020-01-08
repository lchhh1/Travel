using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Xml;
using Windows.Devices.Geolocation;
using Windows.Storage;
using Windows.Storage.Streams;
using Windows.System;
using Windows.UI;
using Windows.UI.Core;
using Windows.UI.Input.Inking;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Maps;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;

namespace Travel
{
    /// <summary>
    /// 相关GUI的按钮、选择以及页面调用、绘制操作
    /// </summary>
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        private TimeOption _timeOption;
        private Strategy _strategy;

        private readonly ObservableCollection<WayPoint> _wayPoints =
            new ObservableCollection<WayPoint>(Enumerable.Repeat(new WayPoint(), 2));

        private TravelSummary _travelSummary;

        private readonly ObservableCollection<TravelDetail> _travelDetails =
            new ObservableCollection<TravelDetail>();

        private readonly MapPolyline _previewPolyline = new MapPolyline
        {
            StrokeDashed = true,
            StrokeThickness = _mapPolylineStrokeThickness,
            StrokeColor = _mapPolylineStrokeColor,
            Path = new Geopath(new BasicGeoposition[] { default })
        };

        private readonly MapPolyline _actualPolyline = new MapPolyline
        {
            StrokeDashed = true,
            StrokeThickness = _mapPolylineStrokeThickness,
            StrokeColor = _mapPolylineStrokeColor
        };

        private TimeSpan _elapsedTime;

        private TextBox _focusedTextBox;

        #region Simulation
        private static readonly RandomAccessStreamReference _stopImage =
            RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/eb4b.png"));
        private static readonly RandomAccessStreamReference _busImage =
            RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/eb47.png"));
        private static readonly RandomAccessStreamReference _trainImage =
            RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/eb4d.png"));
        private static readonly RandomAccessStreamReference _flightImage =
            RandomAccessStreamReference.CreateFromUri(new Uri("ms-appx:///Assets/eb4c.png"));

        private static readonly double _mapPolylineStrokeThickness = 2;
        private static readonly Color _mapPolylineStrokeColor = (Color)Application.Current.Resources["SystemAccentColor"];

        private PlayStatus _playStatus;
        private readonly DispatcherTimer _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
        private readonly TimeSpan _simulationInterval = TimeSpan.FromMinutes(5);
        private readonly MapIcon _travelerIcon = new MapIcon { Image = _stopImage };
        private DateTime _currentTime;
        private int _currentCost;
        private IList<TravelDetail> _currentDetailList;
        private IEnumerator<TravelDetail> _detailEnumerator;
        private TravelDetail _currentDetail;
        private TravelStop _prevStop;
        private TravelStop _nextStop;
        private DateTime _startTime;
        private DateTime _endTime;
        private BasicGeoposition _startPos;
        private BasicGeoposition _endPos;
        #endregion

        public MainPage()
        {
            Map.Load();

            InitializeComponent();
            MapControl.MapServiceToken = "BqMNc8r47lp75CNfBMlE~527MCyS4WLxF2QOdaTiqVw~AnvFHkm5ER4fSZMTM8mdDvkFyDefonIXKl1gh8Lqyd6KIVh1Hvx1RRlimmOXTGEO";
            MapControl.Center = new Geopoint(new BasicGeoposition { Longitude = 100, Latitude = 40 });
            MapControl.MapElements.Add(_previewPolyline);
            ActualThemeChanged += (sender, args) => NotifyPropertyChanged(nameof(ActualTheme));
            _wayPoints.CollectionChanged += (sender, e) =>
            {
                NotifyPropertyChanged(nameof(CanQuery));
                if (CanQuery)
                {
                    UpdatePreviewPolyline();
                }
            };
            _timer.Tick += Timer_Tick;

            MapControl.GettingFocus += MapControl_GettingFocus;

            MapInkCanvas.InkPresenter.StrokesCollected += InkPresenter_StrokesCollected;
            MapInkCanvas.InkPresenter.IsInputEnabled = false;
            MapInkCanvas.InkPresenter.InputDeviceTypes = CoreInputDeviceTypes.Touch | CoreInputDeviceTypes.Pen | CoreInputDeviceTypes.Mouse;
        }

        #region Simulation
        private BasicGeoposition ClampGeoposition
        {
            get
            {
                var value = (_currentTime - _startTime) / (_endTime - _startTime);
                return new BasicGeoposition
                {
                    Longitude = _startPos.Longitude + (_endPos.Longitude - _startPos.Longitude) * value,
                    Latitude = _startPos.Latitude + (_endPos.Latitude - _startPos.Latitude) * value,
                };
            }
        }

        private City NextStartCity =>
            (_currentDetail switch
            {
                TravelStop stop => stop,
                TravelStep _ => _nextStop,
                _ => throw new ArgumentException()
            }).City;

        private DateTime NextDepartureTime =>
            _currentDetail switch
            {
                TravelStop _ => _currentTime,
                TravelStep _ => _nextStop.ArrivalTime,
                _ => throw new ArgumentException()
            };

        private bool CanStart => _playStatus == PlayStatus.Stopped || _playStatus == PlayStatus.Editing;

        private bool CanContinue => _playStatus == PlayStatus.Paused;

        private bool CanPause => _playStatus == PlayStatus.Playing;

        private bool CanStop => _playStatus != PlayStatus.Stopped;

        private bool CanRestart => CanStop && _playStatus != PlayStatus.Editing;
        #endregion

        private bool CanQuery =>
            _wayPoints.Count >= 2 && _wayPoints.All(point => point.City != null) &&
            (_playStatus != PlayStatus.Editing || _wayPoints[0].City == NextStartCity);

        private bool CanEdit => _playStatus != PlayStatus.Playing;

        private DateTime MinDateTime => _playStatus == PlayStatus.Stopped ? DateTime.Now : NextDepartureTime;

        private DateTime PickedDateTime => DatePicker.Date.Date + TimePicker.Time;

        private bool IsDateTimePickersVisible => _timeOption != TimeOption.LeaveNow;

        private Strategy ActualStrategy => _timeOption == TimeOption.ArriveBy ? Strategy.MinimizeCostLimitedTime : _strategy;

        private TravelSummary TravelSummary
        {
            get => _travelSummary;

            set
            {
                _travelSummary = value;
                NotifyPropertyChanged();
                NotifyPropertyChanged(nameof(IsTravelSummaryVisible));
            }
        }

        private bool IsTravelSummaryVisible => _travelSummary != null;

        private string ElapsedTimeString => $"{_elapsedTime:s'.'ff} seconds";

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged([CallerMemberName] string propertyName = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));

        private void UpdatePreviewPolyline() =>
            _previewPolyline.Path = new Geopath(_wayPoints.Select(point => point.City.Geoposition));

        private void SetTimeOption(TimeOption value)
        {
            _timeOption = value;
            NotifyPropertyChanged(nameof(IsDateTimePickersVisible));
        }

        private void SetPlayStatus(PlayStatus value)
        {
            _playStatus = value;
            NotifyPropertyChanged(nameof(CanStart));
            NotifyPropertyChanged(nameof(CanContinue));
            NotifyPropertyChanged(nameof(CanPause));
            NotifyPropertyChanged(nameof(CanStop));
            NotifyPropertyChanged(nameof(CanRestart));
            NotifyPropertyChanged(nameof(CanEdit));
        }

        private T FindAncestor<T>(DependencyObject reference) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(reference);
            return parent is T ancestor ? ancestor : FindAncestor<T>(parent);
        }

        private int GetContainerIndex(ItemsControl control, DependencyObject containier) =>
            control.IndexFromContainer(FindAncestor<ContentControl>(containier));

#pragma warning disable IDE0060 // Remove unused parameter
        private void ThemeRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RequestedTheme = Enum.Parse<ElementTheme>((sender as RadioButton).Tag as string);
            foreach (var popup in VisualTreeHelper.GetOpenPopups(Window.Current))
            {
                popup.RequestedTheme = RequestedTheme;
            }
        }

        private void TimeOptionRadioButton_Checked(object sender, RoutedEventArgs e)
        {
            SetTimeOption(Enum.Parse<TimeOption>((sender as RadioButton).Tag as string));
            if (IsDateTimePickersVisible)
            {
                DateTime defaultTime = _playStatus == PlayStatus.Stopped ? DateTime.Now : NextDepartureTime;
                DatePicker.Date = defaultTime;
                TimePicker.Time = defaultTime.TimeOfDay;
            }
        }

        private void StrategyRadioButton_Checked(object sender, RoutedEventArgs e) =>
            _strategy = Enum.Parse<Strategy>((sender as RadioButton).Tag as string);

        private void AddButton_Click(object sender, RoutedEventArgs e) =>
            _wayPoints.Add(new WayPoint());

        private void AddAllButton_Click(object sender, RoutedEventArgs e)
        {
            _wayPoints.Clear();
            foreach (var city in Map.Cities)
            {
                _wayPoints.Add(new WayPoint(city));
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e) =>
            _wayPoints.RemoveAt(GetContainerIndex(WayPointList, sender as DependencyObject));

        private void QueryButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsPanel.Visibility = Visibility.Collapsed;
            ResultsPanel.Visibility = Visibility.Visible;

            _travelDetails.Clear();
            MapControl.MapElements.Remove(_previewPolyline);

            var pickedDateTime = IsDateTimePickersVisible ? PickedDateTime >= MinDateTime ? PickedDateTime : MinDateTime : default;

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var result = new Routing().Query(
                cities: _wayPoints.Select(point => point.City.Index).ToArray(),
                strategy: ActualStrategy,
                departureTime: _timeOption == TimeOption.DepartAt ? pickedDateTime : MinDateTime,
                arrivalTime: _timeOption == TimeOption.ArriveBy ? pickedDateTime : default);
            _elapsedTime = stopwatch.Elapsed;
            NotifyPropertyChanged(nameof(ElapsedTimeString));

            if (result == null)
            {
                _travelDetails.Add(new TravelNull());
                TravelSummary = null;
                return;
            }

            var positions = new List<BasicGeoposition>();

            var enumerator = result.Path.GetEnumerator();
            _ = enumerator.MoveNext();

            for (Node current = enumerator.Current, next = null; current != null; current = next)
            {
                if (current.PrevTech != Technology.None)
                {
                    _travelDetails.Add(new TravelStep(current.PrevTech, current.PrevRoute));
                }

                City currentCity;
                if (enumerator.MoveNext())
                {
                    next = enumerator.Current;
                    currentCity = next.PrevCity;
                    _travelDetails.Add(new TravelStop(currentCity, current.ArrivalTime, current.DepartureTime));
                }
                else
                {
                    next = null;
                    currentCity = _wayPoints.Last().City;
                    _travelDetails.Add(new TravelStop(_wayPoints.Last().City, current.ArrivalTime, null));

                    TravelSummary = new TravelSummary(result.Path.First().DepartureTime, current.ArrivalTime, result.Cost);
                }

                positions.Add(currentCity.Geoposition);
            }

            _actualPolyline.Path = new Geopath(positions);
            MapControl.MapElements.Add(_actualPolyline);
        }

        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            OptionsPanel.Visibility = Visibility.Visible;
            ResultsPanel.Visibility = Visibility.Collapsed;

            MapControl.MapElements.Remove(_actualPolyline);
            MapControl.MapElements.Add(_previewPolyline);

            if (_playStatus == PlayStatus.Paused)
            {
                SetPlayStatus(PlayStatus.Editing);
                var startCity = NextStartCity;
                while (_wayPoints[0].City != startCity)
                {
                    _wayPoints.RemoveAt(0);
                }
            }
        }

        #region Simulation
        private void StartSimulation()
        {
            switch (_playStatus)
            {
                case PlayStatus.Stopped:
                    _currentTime = DateTime.Now;
                    _currentCost = 0;

                    _currentDetailList = _travelDetails.ToList();
                    _detailEnumerator = _currentDetailList.GetEnumerator();
                    _ = _detailEnumerator.MoveNext();
                    _currentDetail = _detailEnumerator.Current;
                    _endTime = (_currentDetail as TravelStop).DepartureTime.Value;

                    _travelerIcon.Location = new Geopoint((_currentDetail as TravelStop).City.Geoposition);
                    MapControl.MapElements.Add(_travelerIcon);
                    break;

                case PlayStatus.Editing:
                    _currentDetailList = _travelDetails.ToList();
                    _detailEnumerator = _currentDetailList.GetEnumerator();
                    _ = _detailEnumerator.MoveNext();

                    switch (_currentDetail)
                    {
                        case TravelStop _:
                            _currentDetail = _detailEnumerator.Current;
                            _endTime = (_currentDetail as TravelStop).DepartureTime.Value;
                            break;
                        case TravelStep _:
                            _nextStop = _detailEnumerator.Current as TravelStop;

                            MapControl.MapElements.Add(new MapPolyline
                            {
                                StrokeThickness = _mapPolylineStrokeThickness,
                                StrokeColor = _mapPolylineStrokeColor,
                                Path = new Geopath(new[] { _startPos, _endPos })
                            });
                            break;
                    }

                    break;
            }

            _travelerIcon.Title = $"{_currentDetail.Name}\n{_currentTime:MM-dd HH:mm}\n{_currentCost} 元";

            ContinueSimulation();
        }

        private void ContinueSimulation()
        {
            SetPlayStatus(PlayStatus.Playing);
            _timer.Start();
        }

        private void StopSimulation()
        {
            SetPlayStatus(PlayStatus.Stopped);
            _timer.Stop();

            MapControl.MapElements.Remove(_travelerIcon);
            for (int i = MapControl.MapElements.Count - 1; i >= 0; i--)
            {
                if (MapControl.MapElements[i] is MapPolyline polyline &&
                    !polyline.StrokeDashed)
                {
                    MapControl.MapElements.RemoveAt(i);
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e) =>
            StartSimulation();

        private void Timer_Tick(object sender, object e)
        {
            _currentTime += _simulationInterval;

            if (_currentTime >= _endTime)
            {
                while (_currentTime >= _endTime)
                {
                    switch (_currentDetail)
                    {
                        case TravelStop stop:
                            _prevStop = stop;
                            _detailEnumerator.MoveNext();
                            _currentDetail = _detailEnumerator.Current;
                            _currentCost += (_currentDetail as TravelStep).Price;
                            _detailEnumerator.MoveNext();
                            _nextStop = _detailEnumerator.Current as TravelStop;

                            _startTime = _prevStop.DepartureTime.Value;
                            _endTime = _nextStop.ArrivalTime;
                            _startPos = _prevStop.City.Geoposition;
                            _endPos = _nextStop.City.Geoposition;
                            _travelerIcon.Location = new Geopoint(ClampGeoposition);
                            _travelerIcon.Image = (_currentDetail as TravelStep).Technology switch
                            {
                                Technology.Bus => _busImage,
                                Technology.Train => _trainImage,
                                Technology.Flight => _flightImage,
                                _ => throw new ArgumentOutOfRangeException()
                            };

                            MapControl.MapElements.Add(new MapPolyline
                            {
                                StrokeThickness = _mapPolylineStrokeThickness,
                                StrokeColor = _mapPolylineStrokeColor,
                                Path = new Geopath(new[] { _startPos, _endPos })
                            });
                            break;

                        case TravelStep _:
                            _currentDetail = _nextStop;
                            if (_nextStop.DepartureTime == null)
                            {
                                StopSimulation();
                                return;
                            }

                            _endTime = _nextStop.DepartureTime.Value;
                            _travelerIcon.Location = new Geopoint(_nextStop.City.Geoposition);
                            _travelerIcon.Image = _stopImage;
                            break;
                    }
                }
            }
            else if (_currentDetail is TravelStep)
            {
                _travelerIcon.Location = new Geopoint(ClampGeoposition);
            }

            _travelerIcon.Title =
                $"{_currentDetail.Name}{(_currentDetail is TravelStep ? " -> " + _nextStop.Name : null)}\n" +
                $"{_currentTime:MM-dd HH:mm}\n" +
                $"{_currentCost} 元";
        }

        private void ContinueButton_Click(object sender, RoutedEventArgs e) =>
            ContinueSimulation();

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            SetPlayStatus(PlayStatus.Paused);
            _timer.Stop();
        }

        private void StopButton_Click(object sender, RoutedEventArgs e) =>
            StopSimulation();

        private void RestartButton_Click(object sender, RoutedEventArgs e)
        {
            StopSimulation();
            StartSimulation();
        }
        #endregion

        #region Input
        private void AutoSuggestBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            if (args.Reason == AutoSuggestionBoxTextChangeReason.UserInput)
            {
                sender.ItemsSource =
                    from city in Map.Cities
                    where city.FullName.Contains(sender.Text, StringComparison.OrdinalIgnoreCase) ||
                          city.FullLocalName.Contains(sender.Text, StringComparison.OrdinalIgnoreCase)
                    select city;
            }
        }

        private void AutoSuggestBox_QuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
        {
            if (args.ChosenSuggestion is City city)
            {
                _wayPoints[GetContainerIndex(WayPointList, sender as DependencyObject)] = new WayPoint(city);
            }
        }
        #endregion

        #region Choose
        private void MapControl_GettingFocus(UIElement sender, GettingFocusEventArgs args) =>
            _focusedTextBox = args.OldFocusedElement is TextBox textBox ? textBox : null;

        private void MapControl_MapTapped(MapControl sender, MapInputEventArgs args)
        {
            if (_focusedTextBox != null)
            {
                _wayPoints[GetContainerIndex(WayPointList, _focusedTextBox)] =
                    new WayPoint(Map.GetNearestCity(args.Location.Position));
            }
        }
        #endregion

        #region Draw
        private void MapControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            MapInkCanvas.Width = e.NewSize.Width;
            MapInkCanvas.Height = e.NewSize.Height;
        }

        private void DrawButton_Click(object sender, RoutedEventArgs e)
        {
            DrawButton.IsEnabled = false;
            MapInkCanvas.InkPresenter.IsInputEnabled = true;
        }

        private void InkPresenter_StrokesCollected(InkPresenter sender, InkStrokesCollectedEventArgs args)
        {
            DrawButton.IsEnabled = true;
            MapInkCanvas.InkPresenter.IsInputEnabled = false;
            MapInkCanvas.InkPresenter.StrokeContainer.Clear();
            _wayPoints.Clear();

            City prevCity = null;
            foreach (var segment in args.Strokes.First().GetRenderingSegments())
            {
                MapControl.TryGetLocationFromOffset(segment.Position, out var location);
                var city = Map.GetNearestCity(location.Position);
                if (city != null && city != prevCity)
                {
                    prevCity = city;
                    _wayPoints.Add(new WayPoint(city));
                }
            }
        }
        #endregion

        private void TravelDetailList_ItemClick(object sender, ItemClickEventArgs e)
        {
            switch (e.ClickedItem)
            {
                case TravelStop stop:
                    MapControl.Center = new Geopoint(stop.City.Geoposition);
                    break;

                case TravelStep step:
                    var index = _travelDetails.IndexOf(step);
                    var prevPos = (_travelDetails[index - 1] as TravelStop).City.Geoposition;
                    var nextPos = (_travelDetails[index + 1] as TravelStop).City.Geoposition;
                    MapControl.Center = new Geopoint(new BasicGeoposition
                    {
                        Longitude = (prevPos.Longitude + nextPos.Longitude) / 2,
                        Latitude = (prevPos.Latitude + nextPos.Latitude) / 2
                    });

                    break;
            }
        }

        private async void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            var file = await ApplicationData.Current.TemporaryFolder.CreateFileAsync("Save.xml", CreationCollisionOption.ReplaceExisting);
            var stream = await file.OpenStreamForWriteAsync();
            using (var writer = XmlWriter.Create(stream, new XmlWriterSettings { Indent = true }))
            {
                var dateTimeToStringConverter = new DateTimeToStringConverter();
                var timeSpanToStringConverter = new TimeSpanToStringConverter();
                var priceToStringConverter = new PriceToStringConverter();

                writer.WriteStartElement(nameof(Travel));
                {
                    writer.WriteElementString(nameof(TimeOption), _timeOption.ToString());
                    writer.WriteElementString(nameof(PickedDateTime), dateTimeToStringConverter.Convert(IsDateTimePickersVisible ? PickedDateTime >= MinDateTime ? PickedDateTime : MinDateTime : default(DateTime?)));
                    writer.WriteElementString(nameof(Strategy), ActualStrategy.ToString());

                    writer.WriteStartElement("WayPoints");
                    foreach (var point in _wayPoints)
                    {
                        writer.WriteStartElement(nameof(WayPoint));
                        writer.WriteElementString(nameof(WayPoint.Name), point.Name);
                        writer.WriteEndElement();
                    }

                    writer.WriteEndElement();

                    writer.WriteStartElement(nameof(TravelSummary));
                    {
                        writer.WriteElementString(nameof(TravelSummary.DepartureTime), dateTimeToStringConverter.Convert(_travelSummary.DepartureTime));
                        writer.WriteElementString(nameof(TravelSummary.ArrivalTime), dateTimeToStringConverter.Convert(_travelSummary.ArrivalTime));
                        writer.WriteElementString(nameof(TravelSummary.Duration), timeSpanToStringConverter.Convert(_travelSummary.Duration));
                        writer.WriteElementString(nameof(TravelSummary.Cost), priceToStringConverter.Convert(_travelSummary.Cost));
                    }

                    writer.WriteEndElement();

                    writer.WriteStartElement("TravelDetails");
                    foreach (var detail in _travelDetails)
                    {
                        switch (detail)
                        {
                            case TravelStop stop:
                                writer.WriteStartElement(nameof(TravelStop));
                                writer.WriteElementString(nameof(TravelStop.ArrivalTime), dateTimeToStringConverter.Convert(stop.ArrivalTime));
                                writer.WriteElementString(nameof(TravelStop.DepartureTime), dateTimeToStringConverter.Convert(stop.DepartureTime));
                                writer.WriteElementString(nameof(TravelStop.Name), stop.Name);
                                writer.WriteElementString(nameof(TravelStop.Duration), timeSpanToStringConverter.Convert(stop.Duration));
                                writer.WriteEndElement();
                                break;

                            case TravelStep step:
                                writer.WriteStartElement(nameof(TravelStep));
                                writer.WriteElementString(nameof(TravelStep.Technology), step.Technology.ToString());
                                writer.WriteElementString(nameof(TravelStep.Name), step.Name);
                                writer.WriteElementString(nameof(TravelStep.Duration), timeSpanToStringConverter.Convert(step.Duration));
                                writer.WriteElementString(nameof(TravelStep.Price), priceToStringConverter.Convert(step.Price));
                                writer.WriteEndElement();
                                break;
                        }
                    }

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }

            await Launcher.LaunchFileAsync(file);
        }
#pragma warning restore IDE0060 // Remove unused parameter
    }
}
