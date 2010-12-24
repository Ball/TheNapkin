using System;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

namespace TheNapkin
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly IDisposable _mouseSub;

        public MainWindow()
        {
            InitializeComponent();
            Unloaded += This_Unloaded;

            var stylusDown = Observable.FromEvent<StylusDownEventArgs>(theCanvas, "StylusDown");
            var stylusUp   = Observable.FromEvent<StylusEventArgs>(theCanvas, "StylusUp");
            var stylusMove = Observable.FromEvent<StylusEventArgs>(theCanvas, "StylusMove")
                .Select(rm => rm.EventArgs.StylusDevice.GetStylusPoints(theCanvas).First())
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
                    Stroke = Brushes.Black,
                    X1 = item.X1,
                    X2 = item.X2,
                    Y1 = item.Y1,
                    Y2 = item.Y2,
                    StrokeThickness = 25 * item.Pressure
                };
                theCanvas.Children.Add(line);
            });
        }

        private void This_Unloaded(object sender, RoutedEventArgs e)
        {
            Unloaded -= This_Unloaded;
            _mouseSub.Dispose();
        }
    }
}
