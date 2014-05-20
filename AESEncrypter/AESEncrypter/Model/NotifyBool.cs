using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AESEncrypter.Model
{
    class NotifyBool : INotifyPropertyChanged
    {
        private PropertyChangedEventArgs args = new PropertyChangedEventArgs("Content");
        private bool content = true;

        public bool Content
        {
            get { return content; }
            set
            {
                content = value;
                OnContentChanged();
            }
        }

        public NotifyBool()
        {
        }

        public NotifyBool(bool value)
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
