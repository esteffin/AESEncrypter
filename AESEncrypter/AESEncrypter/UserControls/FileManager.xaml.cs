using AESCore;
using AESEncrypter.Components;
using AESEncrypter.Model;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace AESEncrypter.UserControls
{
    /// <summary>
    /// Interaction logic for FileManager.xaml
    /// </summary>
    public partial class FileManager : UserControl
    {
        AesFileWorker worker = new AesFileWorker();
        NotifyInt progress = new NotifyInt();
        string DocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public FileManager()
        {
            InitializeComponent();
            SourcePath.DataContext = new NotifyString();
            Progress.DataContext = progress;
            worker.Finished += worker_Finished;
            Status.Visibility = System.Windows.Visibility.Hidden;
        }

        void worker_Finished(object sender, string e)
        {
            MessageBox.Show(e);
            Unlock();
        }

        private void Lock()
        {
            ChangeSource.IsEnabled = false;
            EncryptButton.IsEnabled = false;
            DecryptButton.IsEnabled = false;
            Status.Visibility = System.Windows.Visibility.Visible;
        }

        private void Unlock()
        {
            ChangeSource.IsEnabled = true;
            EncryptButton.IsEnabled = true;
            DecryptButton.IsEnabled = true;
            Status.Visibility = System.Windows.Visibility.Hidden;
            (Progress.DataContext as NotifyInt).Content = 0;
        }

        private string ChooseFolder(string old_path)
        {
            using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
            {
                if (old_path == null)
                    dlg.InitialDirectory = DocumentsFolder;
                else if (File.Exists(old_path) || Directory.Exists(old_path))
                    dlg.InitialDirectory = new FileInfo(old_path).DirectoryName;
                else
                    dlg.InitialDirectory = DocumentsFolder;
                dlg.Multiselect = false;
                dlg.Filter = "All Files (*.*)|*.*|Encrypted Files (.enc)|*.enc";
                dlg.FilterIndex = 1;
                System.Windows.Forms.DialogResult result = dlg.ShowDialog();
                if (result == System.Windows.Forms.DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }
            return old_path;
        }

        private void ChangeSource_Click(object sender, RoutedEventArgs e)
        {
            var context = (SourcePath.DataContext as NotifyString);
            var old_path = context.Content;
            var new_path = ChooseFolder(old_path);
            context.Content = new_path;
        }

        private void EncryptButton_Click(object sender, RoutedEventArgs e)
        {
            var in_path = (SourcePath.DataContext as NotifyString).Content;
            var out_path = DestinationName.Text != "" ? DestinationName.Text : null;
            try
            {
                Lock();
                worker.StartAsyncEncrypt(progress, PasswordField.Password, in_path, out_path);
                //using (var myAes = new AESCrypter())
                //{
                    
                //    //result = myAes.EncryptPath(PasswordField.Password, in_path, out_path);
                //}
                //MessageBox.Show(result ? "Cifratura avvenuta con successo" : "errore nella cifratura");
            }
            catch (Exception exc)
            {
                MessageBox.Show("errore nella cifratura\n" + exc.Message);
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            var in_path = (SourcePath.DataContext as NotifyString).Content;
            try
            {
                Lock();
                worker.StartAsyncDecrypt(progress, PasswordField.Password, in_path);
                //using (var myAes = new AESCrypter())
                //{
                //    //result = myAes.DecryptPath(PasswordField.Password, in_path);
                //}
               // MessageBox.Show(result ? "Decifratura avvenuta con successo" : "errore nella cifratura");
            }
            catch (Exception exc)
            {
                MessageBox.Show("errore nella cifratura\n" + exc.Message);
            }
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            worker.Abort();
        }
    }
}
