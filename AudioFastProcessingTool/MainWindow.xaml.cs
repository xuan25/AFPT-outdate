using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Animation;


namespace AudioFastProcessingTool
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        //定义API函数
        [DllImport("kernel32.dll")]
        static extern uint SetThreadExecutionState(uint esFlags);
        const uint ES_SYSTEM_REQUIRED = 0x00000001;
        const uint ES_DISPLAY_REQUIRED = 0x00000002;
        const uint ES_CONTINUOUS = 0x80000000;

        //Sets window attributes
        [DllImport("USER32.DLL")]
        public static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        //Gets window attributes
        [DllImport("USER32.DLL")]
        public static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        //assorted constants needed
        public static int GWL_STYLE = -16;
        public static int WS_CHILD = 0x40000000; //child window
        public static int WS_BORDER = 0x00800000; //window with border
        public static int WS_DLGFRAME = 0x00400000; //window with double border but no title
        public static int WS_CAPTION = WS_BORDER | WS_DLGFRAME; //window with a title bar

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            IntPtr windowHandle = new WindowInteropHelper(this).Handle;
            int style = GetWindowLong(windowHandle, GWL_STYLE);
            SetWindowLong(windowHandle, GWL_STYLE, (style | WS_CAPTION));
        }

        public byte[] StreamToBytes(Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始
            stream.Seek(0, SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        /// <param name="path">释放路径</param>
        /// <param name="source">释放源</param>
        private void ExportResource(string path, string source)
        {
            if (!File.Exists(path))
            {
                //释放资源到磁盘
                String projectName = Assembly.GetExecutingAssembly().GetName().Name.ToString();
                Stream gzStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(projectName + "." + source);
                GZipStream stream = new GZipStream(gzStream, CompressionMode.Decompress);
                FileStream decompressedFile = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write);
                stream.CopyTo(decompressedFile);
                decompressedFile.Close();
                stream.Close();
                gzStream.Close();
            }
        }

        private string VerifyResource(string source)
        {
            String projectName = Assembly.GetExecutingAssembly().GetName().Name.ToString();
            Stream gzStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(projectName + "." + source);
            string md5 = GetMD5(StreamToBytes(gzStream));
            gzStream.Close();
            return md5;
        }

        /// <summary>
        /// MD5验证
        /// </summary>
        /// <param name="fromData">输入</param>
        /// <returns></returns>
        public static string GetMD5(byte[] fromData)
        {
            MD5 md5 = new MD5CryptoServiceProvider();
            byte[] targetData = md5.ComputeHash(fromData);
            string byte2String = null;

            for (int i = 0; i < targetData.Length; i++)
            {
                byte2String += targetData[i].ToString("x");
            }

            return byte2String;
        }

        static string logTitle = "[Version]  Function : HannDanngo  UI design : Ping Zi  Software : Xuan  |  Version 2.1\r\n";
        public MainWindow()
        {
            InitializeComponent();

            Process currentProcess = Process.GetCurrentProcess();
            currentProcess.PriorityClass = ProcessPriorityClass.RealTime;
            Thread primaryThread = Thread.CurrentThread;
            primaryThread.Priority = ThreadPriority.Highest;
            //UI初始化
            AboutBox.Text = "Version 2.1";
            Description.Visibility = Visibility.Hidden;
            Initialization.Visibility = Visibility.Hidden;
            ES = ES_REQUIRE.OFF;

            //功能实例化
            P_Vocal = new ProcessClass(null, ProcessClass.ProcessMode.Vocal, 0, 0, "");
            P_Music = new ProcessClass(null, ProcessClass.ProcessMode.Music, 0, 0, "");
            P_Mixed = new ProcessClass(null, ProcessClass.ProcessMode.Mixed, 0, 0, "");

            //释放资源
            if(!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\ffmpeg.exe") || !File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\lame.exe"))
            {
                Initialization.Visibility = Visibility.Visible;
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin");
                Thread ExportResource_T = new Thread(delegate ()
                {
                    if(!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\ffmpeg.exe"))
                    {
                        //Console.WriteLine(VerifyResource("gz.ffmpeg.exe"));
                        if(VerifyResource("gz.ffmpeg.exe") == "93c97daed6a266c388c4335746941d40")
                        {
                            ExportResource(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\ffmpeg.exe", "gz.ffmpeg.exe");
                        }
                        else
                        {
                            ProcessLog("VERIFICATION FAILED at  " + DateTime.Now.ToString());
                            Environment.Exit(0);
                        }
                    }
                    if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\lame.exe"))
                    {
                        //Console.WriteLine(VerifyResource("gz.lame.exe"));
                        if (VerifyResource("gz.lame.exe") == "eb3cd03670397c7d39973532ac3ef5d8")
                        {
                            ExportResource(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\Bin\\lame.exe", "gz.lame.exe");
                        }
                        else
                        {
                            ProcessLog("VERIFICATION FAILED at  " + DateTime.Now.ToString());
                            Environment.Exit(0);
                        }
                    }

                    double O = 1;
                    while (O > 0)
                    {
                        O = O - 0.1;
                        Dispatcher.Invoke(new Action(() =>
                        {
                            Initialization.Opacity = O;
                        }));
                        Thread.Sleep(20);
                    }
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Initialization.Visibility = Visibility.Hidden;
                    }));
                });
                ExportResource_T.Start();
            }

            //UI初始化
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.Topmost = true;

            VocalProgressBar.BarOpacity = 0;
            MusicProgressBar.BarOpacity = 0;
            MixedProgressBar.BarOpacity = 0;

            if (!File.Exists(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\DescriptionHided"))
            {
                Description.Visibility = Visibility.Visible;
            }

            log += "[Software] Initial Complete at  " + DateTime.Now.ToString();

            //显示UI
            MainGrid.Opacity = 0;
            Thread T = new Thread(delegate ()
            {
                double O = 0;
                while (O < 1)
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MainGrid.Opacity = O;
                    }));
                    O = O + 0.1;
                    Thread.Sleep(10);
                }
            });
            T.Start();

            VocalProgressBar.Reseted += delegate { Dispatcher.Invoke(new Action(() => { VocalGrid.AllowDrop = true; })); };
            MusicProgressBar.Reseted += delegate { Dispatcher.Invoke(new Action(() => { MusicGrid.AllowDrop = true; })); };
            MixedProgressBar.Reseted += delegate { Dispatcher.Invoke(new Action(() => { MixedGrid.AllowDrop = true; })); };
        }

        string log = "";

        ProcessClass P_Vocal;
        private void VocalGrid_PreviewDrop(object sender, DragEventArgs e)
        {
            Sys_ProcessOn();
            VocalGrid.AllowDrop = false;
            Array pathArr = (Array)e.Data.GetData(DataFormats.FileDrop);
            P_Vocal = new ProcessClass(pathArr, ProcessClass.ProcessMode.Vocal, 44100, 0, "");
            P_Vocal.OutputReceived += P_Vocal_OutputReceivedEvent;
            P_Vocal.ProcessExited += Sys_ProcessOff;
            P_Vocal.ProgressUpdate += P_Vocal_ProgressUpdate;
            P_Vocal.Start();
        }

        private void P_Vocal_ProgressUpdate(double value)
        {
            VocalProgressBar.ChangeValue(value);
        }

        private void P_Vocal_OutputReceivedEvent(string message)
        {
            log += "\r\n[Vocal] " + message;
            if (logBoxIsEnable)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (LogBox.Visibility == Visibility.Visible)
                    {
                        LogBox.AppendText("\r\n[Vocal] " + message);
                        LogBox.ScrollToEnd();
                    }
                }));
            }
             
        }

        ProcessClass P_Music;
        private void MusicGrid_PreviewDrop(object sender, DragEventArgs e)
        {
            Sys_ProcessOn();
            MusicGrid.AllowDrop = false;
            Array pathArr = (Array)e.Data.GetData(DataFormats.FileDrop);
            P_Music = new ProcessClass(pathArr, ProcessClass.ProcessMode.Music, 44100, 0, "");
            P_Music.OutputReceived += P_Music_OutputReceivedEvent;
            P_Music.ProcessExited += Sys_ProcessOff;
            P_Music.ProgressUpdate += P_Music_ProgressUpdate;
            P_Music.FileFinished += P_Music_FileFinished;
            P_Music.Start();
        }

        private void P_Music_FileFinished(string path)
        {
            if (!File.Exists(path))
            {
                return;
            }
            Thread AT = new Thread(delegate ()
            {
                BPMAnalyzerCore AC = new BPMAnalyzerCore(path, 1);

                string newPath = path.Substring(0, path.LastIndexOf('\\') + 1) + AC.results[0].bpm + " bpm" + path.Substring(path.LastIndexOf('\\') + 1 + "AFPT".Length);
                File.Delete(newPath);
                File.Move(path, newPath);

                Dispatcher.Invoke(new Action(() =>
                {
                    BPMList.Items.Insert(0, new { Name = newPath.Substring(AC.results[0].path.LastIndexOf('\\') + 1), BPM = AC.results[0].bpm });
                }));
                
            });
            AT.Start();
        }

        private void P_Music_ProgressUpdate(double value)
        {
            MusicProgressBar.ChangeValue(value);
        }

        private void P_Music_OutputReceivedEvent(string message)
        {
            log += "\r\n[Music] " + message;
            if (logBoxIsEnable)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (LogBox.Visibility == Visibility.Visible)
                    {
                        LogBox.AppendText("\r\n[Music] " + message);
                        LogBox.ScrollToEnd();
                    }
                }));
            }
             
        }

        ProcessClass P_Mixed;
        private void MixedGrid_PreviewDrop(object sender, DragEventArgs e)
        {
            Sys_ProcessOn();
            MixedGrid.AllowDrop = false;
            Array pathArr = (Array)e.Data.GetData(DataFormats.FileDrop);
            P_Mixed = new ProcessClass(pathArr, ProcessClass.ProcessMode.Mixed, 44100, 320, "mp3");
            P_Mixed.OutputReceived += P_Mixed_OutputReceived;
            P_Mixed.ProcessExited += Sys_ProcessOff;
            P_Mixed.ProgressUpdate += P_Mixed_ProgressUpdate;
            P_Mixed.Start();
        }

        private void P_Mixed_ProgressUpdate(double value)
        {
            MixedProgressBar.ChangeValue(value);
        }

        private void P_Mixed_OutputReceived(string message)
        {
            log += "\r\n[Mixed] " + message;
            if (logBoxIsEnable)
            {
                Dispatcher.Invoke(new Action(() =>
                {
                    if (LogBox.Visibility == Visibility.Visible)
                    {
                        LogBox.AppendText("\r\n[Mixed] " + message);
                        LogBox.ScrollToEnd();
                    }
                }));
            }
            
        }

        private enum ES_REQUIRE
        {
            OFF = 0,
            ON = 1
        }

        ES_REQUIRE ES;
        private void Sys_ProcessOn()
        {
            if (ES == ES_REQUIRE.OFF)
            {
                SetThreadExecutionState(ES_CONTINUOUS | ES_DISPLAY_REQUIRED | ES_SYSTEM_REQUIRED);  //阻止休眠
                ES = ES_REQUIRE.ON;
                //Console.WriteLine("休眠已阻止");
            }
        }

        private void Sys_ProcessOff()
        {
            if(P_Vocal.ProgressValue == -1 && P_Music.ProgressValue == -1 && P_Mixed.ProgressValue == -1)
            {
                SetThreadExecutionState(ES_CONTINUOUS);  //恢复休眠
                ES = ES_REQUIRE.OFF;
                //Console.WriteLine("休眠已恢复");
            }
        }

        bool logBoxIsEnable = false;
        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyboardDevice.Modifiers == ModifierKeys.Control && e.Key == Key.R)
            {
                if (LogBox.Visibility == Visibility.Hidden)
                {
                    LogBox.Text = logTitle + log;
                    logBoxIsEnable = true;
                    LogBox.Visibility = Visibility.Visible;
                    LogBox.ScrollToEnd();
                    Storyboard S = Resources["ShowLogBox"] as Storyboard;
                    S.Begin();
                }
                else
                {
                    logBoxIsEnable = false;
                    Storyboard S = Resources["HideLogBox"] as Storyboard;
                    S.Completed += delegate { LogBox.Visibility = Visibility.Hidden; LogBox.Text = ""; };
                    S.Begin();
                }
            }
            if(e.Key == Key.Escape)
            {
                this.Close();
            }
        }

        private enum TitleFlag
        {
            DragMove = 0,
            Minimize = 1,
            Close = 2
        }
        private TitleFlag titleflag;

        private void Header_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (CloseBtn.IsMouseOver == false && MinimizeBtn.IsMouseOver == false)
            {
                //this.DragMove();
                titleflag = TitleFlag.DragMove;
            }
            else if (MinimizeBtn.IsMouseOver == true)
            {
                titleflag = TitleFlag.Minimize;
            }
            else if (CloseBtn.IsMouseOver == true)
            {
                titleflag = TitleFlag.Close;
            }
        }

        private void Header_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (MinimizeBtn.IsMouseOver == true && titleflag == TitleFlag.Minimize)
            {
                this.WindowState = WindowState.Minimized;
            }
            else if (CloseBtn.IsMouseOver == true && titleflag == TitleFlag.Close)
            {
                this.Close();
            }
        }

        public struct POINT
        {
            public int X;
            public int Y;
            public POINT(int x, int y)
            {
                this.X = x;
                this.Y = y;
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        private void Header_PreviewMouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed && titleflag == TitleFlag.DragMove)
            {
                if (WindowState == WindowState.Maximized)
                {
                    WindowState = WindowState.Normal;

                    Point pWin = Mouse.GetPosition(this);

                    POINT pScr = new POINT();
                    GetCursorPos(out pScr);

                    this.Top = pScr.Y - pWin.Y - 16;
                    //this.Left = (pScr.X - this.ActualWidth / 2);
                    this.Left = pScr.X - (pScr.X / SystemParameters.WorkArea.Width * this.ActualWidth);
                }
                this.DragMove();
            }
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = true;

            if (P_Vocal.ProgressValue != -1 || P_Music.ProgressValue != -1 || P_Mixed.ProgressValue != -1)
            {
                if (MessageBox.Show("There are still tasks in progress, whether to terminate", "Software is about to close", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                {
                    return;
                }
            }

            P_Vocal.Abort();
            P_Music.Abort();
            P_Mixed.Abort();

            ProcessLog(log);

            Thread T = new Thread(delegate ()
            {
                double O = 1;
                while (O > 0)
                {
                    O = O - 0.1;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        MainGrid.Opacity = O;
                    }));
                    Thread.Sleep(10);
                }
                Dispatcher.Invoke(new Action(() =>
                {
                    this.Hide();
                    Environment.Exit(0);
                }));
            });
            T.Start();
        }

        private void ProcessLog(string message)
        {
            string Folder = System.Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\";
            Directory.CreateDirectory(Folder);
            int i = 0;
            while (i < 100)
            {
                string filename = "ProcessLog";
                if (i != 0)
                {
                    filename = filename + " (" + i + ")";
                }
                File.Delete(Folder + filename + ".log");
                if (!File.Exists(Folder + filename + ".log"))
                {
                    try
                    {
                        StreamWriter SW = new StreamWriter(Folder + filename + ".log", false);
                        SW.WriteLine("Application Version : " + Application.ResourceAssembly.GetName().Version.ToString() + "\r\n" + message);
                        SW.Close();
                        break;
                    }
                    catch { }
                }
                i++;
            }
        }

        private void CloseDescriptionBtn_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if(DescriptionCkb.IsChecked == true)
            {
                Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT");
                StreamWriter SW = new StreamWriter(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\DescriptionHided", false);
                SW.Close();
            }
            Thread T = new Thread(delegate ()
            {
                double O = 1;
                while (O > 0)
                {
                    O = O - 0.1;
                    Dispatcher.Invoke(new Action(() =>
                    {
                        Description.Opacity = O;
                    }));
                    Thread.Sleep(20);
                }
                Dispatcher.Invoke(new Action(() =>
                {
                    Description.Visibility = Visibility.Hidden;
                }));
            });
            T.Start();
        }

        private void ProgressBar_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            CircularProgressBar CP = (CircularProgressBar)sender;
            if (e.NewSize.Height< e.NewSize.Width)
            {
                CP.InnerRadius = e.NewSize.Height * 0.75 / 2;
            }
            else
            {
                CP.InnerRadius = e.NewSize.Width * 0.75 / 2;
            }
        }

        private void DataGridCell_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            e.Handled = true;
        }

        private void BPMList_BeginningEdit(object sender, DataGridBeginningEditEventArgs e)
        {
            e.Cancel = true;
        }

        private void Window_StateChanged(object sender, EventArgs e)
        {
            Window W = (Window)sender;
            if(W.WindowState == WindowState.Maximized)
            {
                W.BorderThickness = new Thickness(8);
                DropShadow.Opacity = 0;
            }
            else
            {
                W.BorderThickness = new Thickness(16);
                DropShadow.Opacity = 0.5;
            }
        }

    }

    public class OpacityMaskRect : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            double w = (double)values[0];
            double h = (double)values[1];

            Rect rect = new Rect(0, 0, w, h);

            return rect;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            return null;
        }
    }
}
