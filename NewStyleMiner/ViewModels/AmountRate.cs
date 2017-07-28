using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewStyleMiner.ViewModels
{
    public class AmountRate : ViewModelBase
    {
        private string _amountType;
        public string AmountType
        {
            get
            {
                return _amountType;
            }
            set
            {
                _amountType = value;
                OnPropertyChanged("AmountType");
            }
        }

        private double _amountCount;
        public double AmountCount
        {
            get
            {
                return _amountCount;
            }
            set
            {
                _amountCount = value;
                OnPropertyChanged("AmountCount");
            }
        }
    }
}
