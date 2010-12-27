using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using System.ComponentModel;

namespace TheNapkin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : INotifyPropertyChanged
    {
        private readonly IDisposable _mouseSub;
        private Color _chosenColor;
        private Brush _chosenBrush;
        public Brush SelectedBrush { get { return _chosenBrush; } }
        public Color PickedColor
        {
            get { return _chosenColor; }
            set
            {
                _chosenColor = value;
                _chosenBrush = new SolidColorBrush(value);
                OnPropertyChanged("PickedColor");
                OnPropertyChanged("SelectedBrush");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindow()
        {
            PickedColor = Colors.Black;
            InitializeComponent();
            Unloaded += This_Unloaded;

            var stylusDown = Observable.FromEvent<StylusDownEventArgs>(theCanvas, "StylusDown");
            var stylusUp = Observable.FromEvent<StylusEventArgs>(theCanvas, "StylusUp");
            var stylusMove = Observable.FromEvent<StylusEventArgs>(theCanvas, "StylusMove")
                .Where(sm => sm.EventArgs.Inverted == false)
                .Select(sm => sm.EventArgs.StylusDevice.GetStylusPoints(theCanvas).First())
                .Select(p => new { p.X, p.Y, p.PressureFactor });

            var stylusDiffs = stylusMove
                .Skip(1)
                .Zip(stylusMove, (l, r) => new { X1 = l.X, Y1 = l.Y, X2 = r.X, Y2 = r.Y, Pressure = l.PressureFactor });

            var stylusDrag = from __ in stylusDown
                             from md in stylusDiffs.TakeUntil(stylusUp)
                             select md;
            _mouseSub = stylusDrag.Subscribe(item =>
            {
                var line = new Line
                {
                    Stroke = SelectedBrush,
                    X1 = item.X1,
                    X2 = item.X2,
                    Y1 = item.Y1,
                    Y2 = item.Y2,
                    StrokeThickness = 25 * item.Pressure
                };
                var ellipse = new Ellipse
                {
                    Fill = SelectedBrush,
                    Width = 25 * item.Pressure,
                    Height = 25 * item.Pressure
                };
                Canvas.SetLeft(ellipse, item.X1 - 12.5 * item.Pressure);
                Canvas.SetTop(ellipse, item.Y1 - 12.5 * item.Pressure);
                theCanvas.Children.Add(ellipse);
                theCanvas.Children.Add(line);
            });
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= This_Unloaded;
            _mouseSub.Dispose();
        }

        public void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
