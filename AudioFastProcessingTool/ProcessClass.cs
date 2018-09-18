using System;
using System.Diagnostics;
using System.IO;
using System.Threading;

namespace AudioFastProcessingTool
{
    class ProcessClass
    {

        public delegate void DelOutputReceived(string message);
        public event DelOutputReceived OutputReceived;
        public delegate void DelProcessExited();
        public event DelProcessExited ProcessExited;
        public delegate void DelProgressUpdate(double value);
        public event DelProgressUpdate ProgressUpdate;
        public delegate void DelFileFinished(string path);
        public event DelFileFinished FileFinished;

        private ProcessMode Mode;
        public enum ProcessMode
        {
            Vocal = 0,
            Music = 1,
            Mixed = 2
        }

        public double ProgressValue
        {
            get
            {
                return PValue;
            }
        }
        private double PValue;

        private object FileList;

        private Thread T_Process;
        private int SR, CR;
        string FM;
        public ProcessClass(object filelist, ProcessMode mode, int SampleRate, int CodeRate, string Formate)
        {
            Environment.CurrentDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            PValue = -1;
            Mode = mode;
            FileList = filelist;
            SR = SampleRate;
            CR = CodeRate;
            FM = Formate;
            
        }

        public void Start()
        {
            T_Process = new Thread(TraversalList);
            T_Process.Start(FileList);
        }

        public void Abort()
        {
            if(T_Process != null)
            {
                if (T_Process.ThreadState == System.Threading.ThreadState.Running)
                {
                    T_Process.Abort();
                }
            }
        }

        private void TraversalList(object e)
        {
            Array pathArr = (Array)e;
            int index = 0;
            foreach (string i in pathArr)
            {
                index++;
                PValue = (double)index / pathArr.Length * 100;
                ProgressUpdate(PValue);
                if (Mode == ProcessMode.Vocal)
                {
                    CodingStartVocal(i);
                }
                else if (Mode == ProcessMode.Music)
                {
                    CodingStartMusic(i);
                }
                else if (Mode == ProcessMode.Mixed)
                {
                    CodingStartMixed(i);
                }
            }
            PValue = -1;
            ProgressUpdate(PValue);
            ProcessExited();
        }

        Process CmdProcess;
        /// <summary>
        /// 文件处理进程
        /// </summary>
        /// <param name="StartFileName"></param>
        /// <param name="StartFileArg"></param>
        private void CreateProcess(string StartFileName, string StartFileArg)
        {
            CmdProcess = new Process();
            CmdProcess.StartInfo.FileName = StartFileName;
            CmdProcess.StartInfo.Arguments = StartFileArg;

            CmdProcess.StartInfo.CreateNoWindow = true;
            CmdProcess.StartInfo.UseShellExecute = false;
            CmdProcess.StartInfo.RedirectStandardInput = true;
            CmdProcess.StartInfo.RedirectStandardOutput = true;
            CmdProcess.StartInfo.RedirectStandardError = true;
            CmdProcess.ErrorDataReceived -= new DataReceivedEventHandler(p_DataReceived);
            CmdProcess.OutputDataReceived -= new DataReceivedEventHandler(p_DataReceived);
            CmdProcess.ErrorDataReceived += new DataReceivedEventHandler(p_DataReceived);
            CmdProcess.OutputDataReceived += new DataReceivedEventHandler(p_DataReceived);
            CmdProcess.EnableRaisingEvents = true;
            CmdProcess.Exited += new EventHandler(CmdProcess_Exited);

            CmdProcess.Start();
            //CmdProcess.PriorityClass = ProcessPriorityClass.BelowNormal;
            CmdProcess.BeginErrorReadLine();
            CmdProcess.WaitForExit();

        }

        /// <summary>
        /// 报文接收
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void p_DataReceived(object sender, DataReceivedEventArgs e)
        {
            if (e.Data != null)
            {
                string result = e.Data;
                OutputReceived(result);
                if (result.IndexOf("max_volume") != -1)
                {
                    string s = result.Substring(result.IndexOf(':') + 1, result.IndexOf("dB") - result.IndexOf(':') - 1).Trim();
                    double.TryParse(s, out double d);
                    max_volume = d;
                }
                if (result.IndexOf("mapchan: invalid audio channel") != -1)
                {
                    invalid = true;
                }
                if (result.IndexOf("mapchan: stream #0.0 is not an audio stream.") != -1)
                {
                    video = true;
                }
            }
        }

        /// <summary>
        /// 文件处理线程结束
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void CmdProcess_Exited(object sender, EventArgs e)
        {

        }

        double? max_volume;
        bool invalid, video;
        /// <summary>
        /// 处理人声
        /// </summary>
        /// <param name="Path">文件路径列表</param>
        private void CodingStartVocal(object Path)
        {
            invalid = false;
            video = false;
            max_volume = null;
            string CMD;

            string Folder = Path.ToString().Substring(0, Path.ToString().LastIndexOf("\\") + 1);
            string FileName = Path.ToString().Substring(Path.ToString().LastIndexOf("\\") + 1);
            if(FileName.LastIndexOf(".") != -1)
            {
                FileName = FileName.Substring(0, FileName.LastIndexOf("."));
            }

            CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -i \"" + Path + "\" -vn -sn -af \"volumedetect\" -f null null";
            OutputReceived("Command - " + CMD);
            CreateProcess("cmd.exe", "/c " + CMD);  //info

            double? volume = 0 - max_volume - 6;

            CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -af \"volume=" + volume + "dB\" -c:a pcm_s16le -f wav -map_channel 0.0.0 \"" + Folder + "AFPT_" + FileName + "_L.wav\" -map_channel 0.0.1 \"" + Folder + "AFPT_" + FileName + "_R.wav\"";
            OutputReceived("Command - " + CMD);
            CreateProcess("cmd.exe", "/c " + CMD);  //coding

            if (video)
            {
                File.Delete(Folder + "AFPT_" + FileName + "_L.wav");
                CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -af \"volume=" + volume + "dB\" -c:a pcm_s16le -f wav -map_channel 0.1.0 \"" + Folder + "AFPT_" + FileName + "_L.wav\" -map_channel 0.1.1 \"" + Folder + "AFPT_" + FileName + "_R.wav\""; OutputReceived("Command - " + CMD);
                OutputReceived("Command - " + CMD);
                CreateProcess("cmd.exe", "/c " + CMD);  //coding

                if (invalid)
                {
                    File.Delete(Folder + "AFPT_" + FileName + "_L.wav");
                    CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -af \"volume=" + volume + "dB\" -c:a pcm_s16le -f wav -map_channel 0.1.0 \"" + Folder + "AFPT_" + FileName + "_M.wav\"";
                    OutputReceived("Command - " + CMD);
                    CreateProcess("cmd.exe", "/c " + CMD);  //coding

                    FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_M.wav");
                }
                else
                {

                    FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_L.wav");
                    FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_R.wav");
                }
            }
            else if (invalid)
            {
                File.Delete(Folder + "AFPT_" + FileName + "_L.wav");
                CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -af \"volume=" + volume + "dB\" -c:a pcm_s16le -f wav -map_channel 0.0.0 \"" + Folder + "AFPT_" + FileName + "_M.wav\"";
                OutputReceived("Command - " + CMD);
                CreateProcess("cmd.exe", "/c " + CMD);  //coding

                FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_M.wav");
            }
            else
            {
                FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_L.wav");
                FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_R.wav");
            }
        }

        /// <summary>
        /// 处理伴奏
        /// </summary>
        /// <param name="Path">文件列表</param>
        private void CodingStartMusic(object Path)
        {
            invalid = false;
            max_volume = null;
            string CMD;

            string Folder = Path.ToString().Substring(0, Path.ToString().LastIndexOf("\\") + 1);
            string FileName = Path.ToString().Substring(Path.ToString().LastIndexOf("\\") + 1);
            if (FileName.LastIndexOf(".") != -1)
            {
                FileName = FileName.Substring(0, FileName.LastIndexOf("."));
            }

            CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -i \"" + Path + "\" -vn -sn -af \"volumedetect\" -f null null";
            OutputReceived("Command - " + CMD);
            CreateProcess("cmd.exe", "/c " + CMD);  //info

            double? volume = 0 - max_volume - 6;

            CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -af \"volume=" + volume + "dB\" -c:a pcm_s16le -f wav \"" + Folder + "AFPT_" + FileName + "_S.wav\"";
            OutputReceived("Command - " + CMD);
            CreateProcess("cmd.exe", "/c " + CMD);  //coding

            FileFinished?.Invoke(Folder + "AFPT_" + FileName + "_S.wav");

        }

        /// <summary>
        /// 处理成品
        /// </summary>
        /// <param name="Path">文件路径列表</param>
        private void CodingStartMixed(object Path)
        {
            invalid = false;
            max_volume = null;
            string CMD;

            string Folder = Path.ToString().Substring(0, Path.ToString().LastIndexOf("\\") + 1);
            string FileName = Path.ToString().Substring(Path.ToString().LastIndexOf("\\") + 1);
            if (FileName.LastIndexOf(".") != -1)
            {
                FileName = FileName.Substring(0, FileName.LastIndexOf("."));
            }

            if(FM.ToUpper() == "MP3")
            {
                CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -c:a pcm_s16le -f wav pipe: | .\\bin\\lame -b " + CR + " -p - \"" + Folder + "AFPT_" + FileName + "." + FM + "\"";
            }
            else
            {
                CMD = "cd \"" + Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\AFPT\" && .\\Bin\\ffmpeg -y -i \"" + Path + "\" -vn -sn -ar " + SR + " -c:a pcm_s16le -f wav pipe: | .\\bin\\neroAacEnc -ignorelength -cbr " + CR * 1000 + " -if - -of \"" + Folder + "AFPT_" + FileName + "." + FM + "\"";
            }
            OutputReceived("Command - " + CMD);
            CreateProcess("cmd.exe", "/c " + CMD);  //coding

            FileFinished?.Invoke(Folder + "AFPT_" + FileName + "." + FM);

        }
    }
}
