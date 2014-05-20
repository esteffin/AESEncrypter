using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AESEncrypter.Model
{
    class NotifyInt : INotifyPropertyChanged
    {
        private PropertyChangedEventArgs args = new PropertyChangedEventArgs("Content");
        private int content = 0;

        public int Content
        {
            get { return content; }
            set
            {
                content = value;
                OnContentChanged();
            }
        }

        public NotifyInt()
        {
        }

        public NotifyInt(int value)
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
