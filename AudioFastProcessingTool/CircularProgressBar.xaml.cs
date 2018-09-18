using System;
using System.Globalization;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace AudioFastProcessingTool
{
    /// <summary>
    /// CircularProgressBar.xaml 的交互逻辑
    /// </summary>
    public partial class CircularProgressBar : UserControl
    {
        public Brush TrackBrush
        {
            get
            {
                return Track.Fill;
            }
            set
            {
                Track.Fill = value;
            }
        }

        public Brush OutterBrush
        {
            get
            {
                return Header.Fill;
            }
            set
            {
                Header.Fill = value;
                PathR.Fill = value;
                PathL.Fill = value;
            }
        }

        public double InnerRadius
        {
            get
            {
                return MaskInner.RadiusX;
            }
            set
            {
                MaskInner.RadiusX = value;
                MaskInner.RadiusY = value;
            }
        }

        private double oldValue, currentValue, newValue;
        private Thread ANM_T;
        public double Value
        {
            get
            {
                return currentValue;
            }
            set
            {
                currentValue = value;
                ShowProgress(value);
                
            }
        }

        public delegate void DelReseted();
        public event DelReseted Reseted;

        public void ChangeValue(double value)
        {
            oldValue = currentValue;
            newValue = value;

            double BO = 0;
            Dispatcher.Invoke(new Action(() =>
            {
                BO = BarOpacity;
            }));

            if (newValue == -1 && BO > 0)
            {
                ThreadState TS = ThreadState.Running;
                do
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        TS = ANM_T.ThreadState;
                    }));
                    Thread.Sleep(20);
                } while (TS != ThreadState.Stopped);

                Thread T = new Thread(delegate ()
                {
                    double i = 1;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        i = BarOpacity;
                    }));
                    while (i > 0)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            BarOpacity = i;
                        }));
                        i = i - 0.1;
                        Thread.Sleep(20);
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Value = 0;
                    }));
                    Reseted();
                });
                T.Start();
            }
            else
            {
                ChangeValueANM();
                if (BO < 1)
                {
                    Thread T = new Thread(delegate ()
                    {
                        double i = 0;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            i = BarOpacity;
                        }));
                        while (i < 1)
                        {
                            Dispatcher.Invoke(new Action(() =>
                            {
                                BarOpacity = i;
                            }));
                            i = i + 0.1;
                            Thread.Sleep(20);
                        }
                        Dispatcher.Invoke(new Action(() =>
                        {
                            BarOpacity = 1;
                        }));
                    });
                    T.Start();
                }
            }

        }

        private void ChangeValueANM()
        {
            if (ANM_T != null)
            {
                ANM_T.Abort();
            }

            ANM_T = new Thread(delegate ()
            {
                double x = 0;
                double iv = 0;
                double tv = 0;
                Dispatcher.Invoke(new Action(() =>
                {
                    iv = oldValue;
                    tv = newValue;
                }));
                while (x < Math.PI)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        x = x + 0.015;
                        double y = (Math.Cos(x) + 1) / 2;
                        Value = iv + (1 - y) * (tv - iv);
                    }));
                    Thread.Sleep(5);
                }
            });
            ANM_T.Start();
        }

        public double BarOpacity
        {
            get
            {
                return Bar.Opacity;
            }
            set
            {
                Bar.Opacity = value;
            }
        }

        public CircularProgressBar()
        {
            InitializeComponent();
        }

        private void ShowProgress(double per)
        {
            double radius, radiusHeader;
            if(this.ActualWidth < this.ActualHeight)
            {
                radius = this.ActualWidth / 2;
            }
            else
            {
                radius = this.ActualHeight / 2;
            }
            radiusHeader = (radius - MaskInner.RadiusX) / 2;

            //定义变量
            Point endPoint = new Point();
            double x, y, angle;
            if (per > 100)
            {
                per = 100;
            }
            if (per < 0)
            {
                per = 0;
            }
            //换算角度
            angle = (per / 100) * (2 * Math.PI);
            //换算节点坐标
            x = Math.Cos(angle - Math.PI / 2);
            y = Math.Sin(angle - Math.PI / 2);
            //判断角度范围
            if (angle <= Math.PI)
            {
                //半圆1
                endPoint.X = x * radius + radius;
                endPoint.Y = y * radius + radius;
                EndR.Point = endPoint;
                //半圆2
                endPoint.X = radius;
                endPoint.Y = 2 * radius;
                EndL.Point = endPoint;
            }
            else
            {
                //半圆1
                endPoint.X = radius;
                endPoint.Y = 2 * radius;
                EndR.Point = endPoint;  //颜色图层
                //半圆2
                endPoint.X = x * radius + radius;
                endPoint.Y = y * radius + radius;
                EndL.Point = endPoint;  //颜色图层
            }

            double xHeader = x * (radius - radiusHeader) + radius - radiusHeader;
            double yHeader = y * (radius - radiusHeader) + radius - radiusHeader;
            Header.Margin = new Thickness(xHeader, yHeader, 0, 0);
        }

        private void UserControl_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            ShowProgress(currentValue);
        }
    }

    public class BackgroundDiameter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if ((double)values[0] < (double)values[1])
            {
                return (double)values[0];
            }
            else
            {
                return (double)values[1];
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class CenterPoint : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Point P = new Point();

            if ((double)values[0] < (double)values[1])
            {
                P.X = (double)values[0] / 2;
                P.Y = (double)values[0] / 2;
            }
            else
            {
                P.X = (double)values[1] / 2;
                P.Y = (double)values[1] / 2;
            }

            return P;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StartPointR : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Point P = new Point();
            P.X = PCenter.X;
            P.Y = 0;
            return P;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class StartPointL : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Point P = new Point();
            P.X = PCenter.X;
            P.Y = PCenter.Y * 2;
            return P;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EndPointR : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Point P = new Point();
            P.X = PCenter.X;
            P.Y = PCenter.Y * 2;
            return P;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EndPointL : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Point P = new Point();
            P.X = PCenter.X;
            P.Y = 0;
            return P;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EndSizeR : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Size S = new Size();
            S.Width = PCenter.X;
            S.Height = PCenter.X;
            return S;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class EndSizeL : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            Point PCenter = (Point)value;

            Size S = new Size();
            S.Width = PCenter.X;
            S.Height = PCenter.X;
            return S;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class HeaderDiameter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double outerRadius, innerRadius;

            if ((double)values[0] < (double)values[1])
            {
                outerRadius = (double)values[0] / 2;
            }
            else
            {
                outerRadius = (double)values[1] / 2;
            }

            innerRadius = (double)values[2];
            double diameter = outerRadius - innerRadius;
            if (diameter > 0)
            {
                return diameter;
            }
            return 0.0;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class HeaderMargin : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness T = new Thickness();

            double outerRadius, innerRadius;

            if ((double)values[0] < (double)values[1])
            {
                outerRadius = (double)values[0] / 2;
            }
            else
            {
                outerRadius = (double)values[1] / 2;
            }

            innerRadius = (double)values[2];

            T.Left = outerRadius - (outerRadius - innerRadius) / 2;

            return T;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class MaskMargin : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            Thickness T = new Thickness();

            double outerRadius, innerRadius;

            if ((double)values[0] < (double)values[1])
            {
                outerRadius = (double)values[0] / 2;
            }
            else
            {
                outerRadius = (double)values[1] / 2;
            }

            innerRadius = (double)values[2] / 2;

            T.Top = outerRadius - innerRadius;
            T.Left = outerRadius - innerRadius;

            return T;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class MaskInner : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double innerRadius;
            innerRadius = (double)values[2] / 2;
            return innerRadius;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
    
    public class MaskOutter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double outerRadius;
            if ((double)values[0] < (double)values[1])
            {
                outerRadius = (double)values[0] / 2;
            }
            else
            {
                outerRadius = (double)values[1] / 2;
            }
            return outerRadius;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

    public class Smaller : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double r;
            if ((double)values[0] < (double)values[1])
            {
                r = (double)values[0];
            }
            else
            {
                r = (double)values[1];
            }
            return r;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }

}
