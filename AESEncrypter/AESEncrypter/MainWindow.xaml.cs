using System;
using System.Collections.Generic;
using System.ComponentModel;
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
using AESCore;

namespace AESEncrypter
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        public MainWindow()
        {
            InitializeComponent();
            //SourcePath.DataContext = new NotifyString();            
        }

        //private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        //{
        //    Console.WriteLine(string.Format("width:{0}; height:{1}", e.NewSize.Width, e.NewSize.Height));
        //}

        //private string ChooseFolder(string old_path)
        //{
        //    using (System.Windows.Forms.OpenFileDialog dlg = new System.Windows.Forms.OpenFileDialog())
        //    {
        //        dlg.InitialDirectory = old_path == null || old_path == "" || !File.Exists(old_path) ? null : new FileInfo(old_path).DirectoryName;
        //        dlg.Multiselect = false;
        //        dlg.Filter = "All Files (*.*)|*.*|Encrypted Files (.enc)|*.enc";
        //        dlg.FilterIndex = 1;
        //        System.Windows.Forms.DialogResult result = dlg.ShowDialog();
        //        if (result == System.Windows.Forms.DialogResult.OK)
        //        {
        //            return dlg.FileName;
        //        }
        //    }
        //    return old_path;
        //}

        //private void ChangeSource_Click(object sender, RoutedEventArgs e)
        //{
        //    var context = (SourcePath.DataContext as NotifyString);
        //    var old_path = context.Content;
        //    var new_path = ChooseFolder(old_path);
        //    context.Content = new_path;
        //}

        //private void EncryptButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var in_path = (SourcePath.DataContext as NotifyString).Content;
        //    var out_path = DestinationName.Text != "" ? DestinationName.Text : null;
        //    try
        //    {
        //        bool result = false;
        //        using (var myAes = new AESCrypter())
        //        {
        //            result = myAes.EncryptPath(PasswordField.Password, in_path, out_path);
        //        }
        //        MessageBox.Show(result ? "Cifratura avvenuta con successo" : "errore nella cifratura");
        //    }
        //    catch (Exception exc)
        //    {
        //        MessageBox.Show("errore nella cifratura\n" + exc.Message);
        //    }
        //}

        //private void DecryptButton_Click(object sender, RoutedEventArgs e)
        //{
        //    var in_path = (SourcePath.DataContext as NotifyString).Content;
        //    try
        //    {
        //        bool result = false;
        //        using (var myAes = new AESCrypter())
        //        {
        //            result = myAes.DecryptPath(PasswordField.Password, in_path);
        //        }
        //        MessageBox.Show(result ? "Decifratura avvenuta con successo" : "errore nella cifratura");
        //    }
        //    catch (Exception exc)
        //    {
        //        MessageBox.Show("errore nella cifratura\n" + exc.Message);
        //    }
        //}
    }

}
