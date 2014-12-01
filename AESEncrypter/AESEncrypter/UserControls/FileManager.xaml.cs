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
        NotifyBool new_version = new NotifyBool(true);
        string DocumentsFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        public FileManager()
        {
            InitializeComponent();
            SourcePath.DataContext = new NotifyString();
            EncriptionVersion.DataContext = new_version;
            Progress.DataContext = progress;
            worker.Finished += worker_Finished;
            Status.Visibility = System.Windows.Visibility.Hidden;
        }

        void worker_Finished(object sender, string e)
        {
            var window = Window.GetWindow(this);
            MessageBox.Show(window, e);
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
            Microsoft.Win32.OpenFileDialog dlg = new Microsoft.Win32.OpenFileDialog();
            if (old_path == null)
                dlg.InitialDirectory = DocumentsFolder;
            else if (File.Exists(old_path) || Directory.Exists(old_path))
                dlg.InitialDirectory = new FileInfo(old_path).DirectoryName;
            else
                dlg.InitialDirectory = DocumentsFolder;
            dlg.Multiselect = false;
            dlg.Filter = "All Files (*.*)|*.*|Encrypted Files (.enc)|*.enc";
            dlg.FilterIndex = 1;
            var result = dlg.ShowDialog();
            if (result.HasValue && result.Value)
            {
                return dlg.FileName;
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
            var window = Window.GetWindow(this);
            if (PasswordField.Password.Length < 1)
            {
                MessageBox.Show(window, "Inserire una password");
                return;
            }
            if (PasswordField.Password.Length < 8)
            {
                var pwd_check = MessageBox.Show(window, "La password ha lunghezza inferiore di 8 caratteri...\nE' caldamente consigliato di usare una password di almeno 8 caratteri.\nContinuare?", "Password corta", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);
                if (pwd_check != MessageBoxResult.OK)
                    return;
            }
            // Instantiate the dialog box
            DialogBox.ConfirmPasswordDialog dlg = new DialogBox.ConfirmPasswordDialog();
            // Configure the dialog box
            dlg.Owner = Window.GetWindow(this);
            // Open the dialog box modally 
            var res = dlg.ShowDialog();

            if (!dlg.Result.HasValue || !dlg.Result.Value)
            {
                return;
            }

            if (PasswordField.Password != dlg.PasswordField.Password)
            {
                MessageBox.Show(window, "Le password non coincidono");
                return;
            }

            var in_path = (SourcePath.DataContext as NotifyString).Content;
            var out_path = DestinationName.Text != "" ? DestinationName.Text : null;
            try
            {
                Lock();
                worker.StartAsyncEncrypt(progress, new_version.Content, PasswordField.Password, in_path, out_path);
                
                //using (var myAes = new AESCrypter())
                //{
                    
                //    //result = myAes.EncryptPath(PasswordField.Password, in_path, out_path);
                //}
                //MessageBox.Show(result ? "Cifratura avvenuta con successo" : "errore nella cifratura");
            }
            catch (Exception exc)
            {
                MessageBox.Show(window, "errore nella cifratura\n" + exc.Message);
            }
        }

        private void DecryptButton_Click(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);
            if (PasswordField.Password.Length < 1)
            {
                MessageBox.Show(window, "Inserire una password");
                return;
            }
            //if (PasswordField.Password.Length < 8)
            //{
            //    var pwd_check = MessageBox.Show("La password ha lunghezza inferiore di 8 caratteri...\nE' caldamente consigliato di usare una password di almeno 8 caratteri.\nContinuare?", "Password corta", MessageBoxButton.OKCancel, MessageBoxImage.Question, MessageBoxResult.OK);
            //    if (pwd_check != MessageBoxResult.OK)
            //        return;
            //}
            //// Instantiate the dialog box
            //DialogBox.ConfirmPasswordDialog dlg = new DialogBox.ConfirmPasswordDialog();
            //// Configure the dialog box
            //dlg.Owner = Window.GetWindow(this);
            //// Open the dialog box modally 
            //var res = dlg.ShowDialog();

            //if (!dlg.Result.HasValue || !dlg.Result.Value)
            //{
            //    return;
            //}

            //if (PasswordField.Password != dlg.PasswordField.Password)
            //{
            //    MessageBox.Show("Le password non coincidono");
            //    return;
            //}

            var in_path = (SourcePath.DataContext as NotifyString).Content;
            try
            {
                Lock();
                worker.StartAsyncDecrypt(progress, new_version.Content, PasswordField.Password, in_path);
                //using (var myAes = new AESCrypter())
                //{
                //    //result = myAes.DecryptPath(PasswordField.Password, in_path);
                //}
               // MessageBox.Show(result ? "Decifratura avvenuta con successo" : "errore nella cifratura");
            }
            catch (Exception exc)
            {
                MessageBox.Show(window, "errore nella cifratura\n" + exc.Message);
            }
        }

        private void Abort_Click(object sender, RoutedEventArgs e)
        {
            worker.Abort();
        }
    }
}
