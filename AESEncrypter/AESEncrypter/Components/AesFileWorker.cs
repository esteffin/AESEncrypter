using AESCore;
using AESEncrypter.Model;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AESEncrypter.Components
{
    class AesFileWorker : IDisposable
    {

        public BackgroundWorker bw = new BackgroundWorker();
        private NotifyInt DataContext;
        private string key;
        private string in_path;
        private string out_path;

        public string Result = "";
        public event EventHandler<string> Finished;

        private void OnFinished(RunWorkerCompletedEventArgs e)
        {
            if (Finished == null)
                return;
            if ((e.Cancelled == true))
            {
                Finished.Invoke(null, "Canceled!");
            }

            else if (e.Result != null && e.Result as string != "")
            {
                Finished.Invoke(null, e.Result as string);
            }

            else
            {
                Finished.Invoke(null, "Fatto!");
            }
        }

        public void StartAsyncEncrypt(NotifyInt binding, string key, string in_path, string out_path = null)
        {
            Initialize(binding, key, in_path);
            this.out_path = out_path;
            bw.RunWorkerAsync(true);
        }

        public void StartAsyncDecrypt(NotifyInt binding, string key, string in_path)
        {
            Initialize(binding, key, in_path);
            bw.RunWorkerAsync(false);
        }

        public void Abort()
        {
            bw.CancelAsync();
        }

        private void Initialize(NotifyInt binding, string key, string in_path)
        {
            this.key = key;
            this.in_path = in_path;
            DataContext = binding;
            bw.WorkerReportsProgress = true;
            bw.WorkerSupportsCancellation = true;
            bw.DoWork += bw_DoWork;
            bw.ProgressChanged += bw_ProgressChanged;
            bw.RunWorkerCompleted += bw_RunWorkerCompleted;
        }

        private void bw_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            OnFinished(e);
            this.Dispose();
        }

        private void bw_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            DataContext.Content = e.ProgressPercentage;
        }

        private void bw_DoWork(object sender, DoWorkEventArgs e)
        {
            using (var aes = new AESCrypter())
            {

                if ((bool)e.Argument)
                {
                    e.Result = aes.EncryptPath(key, in_path, bw, out_path);
                }
                else
                {
                    e.Result = aes.DecryptPath(key, in_path, bw);
                }
            }
        }
        
        public void Dispose()
        {
            bw.Dispose();
            bw = new BackgroundWorker();
            DataContext = null;
            key = null;
            in_path = null;
            out_path = null;
        }
    }
}
