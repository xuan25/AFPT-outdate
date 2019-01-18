using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Threading;

namespace AudioFastProcessingTool
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
        public App()
        {
            // 在异常由应用程序引发但未进行处理时发生。主要指的是UI线程。
            this.DispatcherUnhandledException += new DispatcherUnhandledExceptionEventHandler(Application_DispatcherUnhandledException);
            //  当某个异常未被捕获时出现。主要指的是非UI线程
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(CurrentDomain_UnhandledException);
        }

        void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            Exception ex = e.ExceptionObject as Exception;
            MessageBox.Show("An unexpected and unrecoverable problem has occourred. Launcher will now exit.", "Unexpected operation", MessageBoxButton.OK, MessageBoxImage.Error);
            CrashLog("Non-UI thread exceptions : \n\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：{2}", ex.GetType(), ex.Message, ex.StackTrace));
            //MessageBox.Show("Non-UI thread exceptions : \n\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：{2}", ex.GetType(), ex.Message, ex.StackTrace));
            System.Environment.Exit(0);
        }

        private void Application_DispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
        {
            Exception ex = e.Exception;
            MessageBox.Show("An unexpected problem has occourred. Some operation has been terminated.", "Unexpected operation", MessageBoxButton.OK, MessageBoxImage.Information);
            CrashLog("UI thread exception : \n\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：{2}", ex.GetType(), ex.Message, ex.StackTrace));
            //MessageBox.Show("UI thread exception : \n\n" + string.Format("Captured an unhandled exception：{0}\r\nException Message：{1}\r\nException StackTrace：{2}", ex.GetType(), ex.Message, ex.StackTrace));
            e.Handled = true;
        }

        private void CrashLog(string message)
        {
            string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\\CrashLog\\";
            Directory.CreateDirectory(Folder);
            string time = DateTime.Now.ToString().Replace(':', '-').Replace('/', '-');

            int i = 0;
            while (i < 100)
            {
                string filename = time;
                if (i != 0)
                {
                    filename = filename + " (" + i + ")";
                }
                if (!File.Exists(Folder + filename + ".log"))
                {
                    try
                    {
                        StreamWriter SW = new StreamWriter(Folder + filename + ".log", false);
                        SW.WriteLine(message);
                        SW.Close();
                        break;
                    }
                    catch { }
                }
                i++;
            }
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            LoadLang();
        }

        private void LoadLang()
        {
            List<ResourceDictionary> dictionaryList = new List<ResourceDictionary>();
            foreach (ResourceDictionary dictionary in Application.Current.Resources.MergedDictionaries)
            {
                dictionaryList.Add(dictionary);
            }
            string requestedCulture = string.Format(@"Lang\{0}.xaml", System.Globalization.CultureInfo.CurrentCulture);
            ResourceDictionary resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            if (resourceDictionary == null)
            {
                requestedCulture = @"Lang\en-US.xaml";
                resourceDictionary = dictionaryList.FirstOrDefault(d => d.Source.OriginalString.Equals(requestedCulture));
            }
            if (resourceDictionary != null)
            {
                Application.Current.Resources.MergedDictionaries.Remove(resourceDictionary);
                Application.Current.Resources.MergedDictionaries.Add(resourceDictionary);
            }
        }
    }
}
