using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AESEncrypter.Model
{
    class NotifyString : INotifyPropertyChanged
    {
        private PropertyChangedEventArgs args = new PropertyChangedEventArgs("Content");
        private string content = "";

        public string Content
        {
            get { return content; }
            set
            {
                content = value;
                OnContentChanged();
            }
        }

        public NotifyString()
        {
        }

        public NotifyString(string value)
        {
            content = value;
        }

        void OnContentChanged()
        {
            if (PropertyChanged != null)
                PropertyChanged(this, args);
        }
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
