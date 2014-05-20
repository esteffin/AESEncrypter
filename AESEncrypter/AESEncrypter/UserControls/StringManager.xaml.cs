using AESCore;
using AESEncrypter.Model;
using System;
using System.Collections.Generic;
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
    /// Interaction logic for StringManager.xaml
    /// </summary>
    public partial class StringManager : UserControl
    {
        NotifyString plain_text = new NotifyString();
        NotifyString cipher_text = new NotifyString();
        volatile bool locked = false;
        
        public StringManager()
        {
            InitializeComponent();
            PlainText.DataContext = plain_text;
            CipherText.DataContext = cipher_text;      
        }

        private bool CheckPasswd()
        {
            if (PasswordBox.Password == null || PasswordBox.Password == "")
            {
                MessageBox.Show("Errore. Manca la chiave!");
                return false;
            }
            return true;
        }

        private void Lock()
        {
            locked = true;
        }

        private void Unlock()
        {
            locked = false;
        }

        private bool IsValid(string s)
        {
            return s != null && s != "";
        }

        private string Encrypt(string key, string plain_text)
        {
            if (!IsValid(plain_text))
                return "";
            try
            {
                using (var myAes = new AESCrypter())
                {
                    return myAes.EncryptString(key, plain_text);
                }
            }
            catch (Exception e)
            {
                var msg = string.Format("Errore\n{0}", e.Message);
                MessageBox.Show(msg);
                return msg;
            }
        }

        private string Decrypt(string key, string cipher_text)
        {
            if (!IsValid(cipher_text))
                return "";
            try
            {
                using (var myAes = new AESCrypter())
                {
                    return myAes.DecryptString(key, cipher_text);                    
                }
            }
            catch (Exception e)
            {
                var msg = string.Format("Errore\n{0}", e.Message);
                MessageBox.Show(msg);
                return msg;
            }
        }

        private void PlainText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (locked || !CheckPasswd())
                return;
            Lock();
            cipher_text.Content = Encrypt(PasswordBox.Password, plain_text.Content);
            Unlock();
        }
        
        private void CipherText_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (locked || !CheckPasswd())
                return;
            Lock();
            plain_text.Content = Decrypt(PasswordBox.Password, cipher_text.Content);
            Unlock();
        }

        private void PasswordBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (!IsValid(PasswordBox.Password))
                return;
            if (IsValid(plain_text.Content))
            {
                Lock();
                cipher_text.Content = Encrypt(PasswordBox.Password, plain_text.Content);
                Unlock();
                return;
            }
            if (IsValid(cipher_text.Content))
            {
                Lock();
                plain_text.Content = Decrypt(PasswordBox.Password, cipher_text.Content);
                Unlock();
                return;
            }
            return;
        }
    }
}
