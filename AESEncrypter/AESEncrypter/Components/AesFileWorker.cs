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
        enum Version
        {
            Version1,
            Version2
        }

        private class Arg : IDisposable
        {
            public string key;
            public bool encrypt;
            public Version version;

            public Arg(string key, bool encrypt, Version version)
            {
                this.key = key;
                this.encrypt = encrypt;
                this.version = version;
            }

            public void Dispose()
            {
                this.key = null;
            }
        }

        public BackgroundWorker bw = new BackgroundWorker();
        private NotifyInt DataContext;
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

        public void StartAsyncEncrypt(NotifyInt binding, bool new_version, string key, string in_path, string out_path = null)
        {
            Initialize(binding, in_path);
            this.out_path = out_path;
            bw.RunWorkerAsync(new Arg(key, true, new_version ? Version.Version2 : Version.Version1));
        }

        public void StartAsyncDecrypt(NotifyInt binding, bool new_version, string key, string in_path)
        {
            Initialize(binding, in_path);
            bw.RunWorkerAsync(new Arg(key, false, new_version ? Version.Version2 : Version.Version1));
        }

        public void Abort()
        {
            bw.CancelAsync();
        }

        private void Initialize(NotifyInt binding, string in_path)
        {
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
            using (var arg = (Arg)e.Argument)
            {
                if (arg.version == Version.Version1 && arg.encrypt)
                {
                    using (var aes = new AESCrypter())
                    {
                        e.Result = aes.EncryptPath(arg.key, in_path, bw, out_path);
                    }
                }
                else if (arg.version == Version.Version2 && arg.encrypt)
                {
                    using (var aes_v2 = new AESCrypterV2())
                    {
                        e.Result = aes_v2.EncryptPath(arg.key, in_path, bw, out_path);
                    }
                }
                else
                {
                    
                    using (var aes = new AESCrypter())
                    {
                        e.Result = aes.DecryptPath(arg.key, in_path, bw);
                    }
                    if (((string)e.Result).StartsWith("Errore"))
                    {
                        //Console.WriteLine("Exception with version 1...\nTrying with version 2...");
                        try
                        {
                            using (var aes_v2 = new AESCrypterV2())
                            {
                                e.Result = aes_v2.DecryptPath(arg.key, in_path, bw);
                            }
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                }
            }
        }

        public void Dispose()
        {
            bw.Dispose();
            bw = new BackgroundWorker();
            DataContext = null;
            in_path = null;
            out_path = null;
        }
    }
}
