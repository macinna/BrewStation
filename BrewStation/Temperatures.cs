using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace BrewStation
{
    public class Temperatures : INotifyPropertyChanged 
    {

        private int mashTunTemp;
        private int hotLiquorTankTemp;
        private int boilKettleTemp;

        public int MashTunTemperature
        {
            get
            {
                return mashTunTemp;
            }
            set
            {
                mashTunTemp = value;
                OnPropertyChanged("MashTunTemperature");
            }

        }

        public int HotLiquorTankTemperature
        {
            get
            {
                return hotLiquorTankTemp;
            }
            set
            {
                hotLiquorTankTemp = value;
                OnPropertyChanged("HotLiquorTankTemperature");
            }

        }

        public int BoilKettleTemperature
        {
            get
            {
                return boilKettleTemp;
            }
            set
            {
                boilKettleTemp = value;
                OnPropertyChanged("BoilKettleTemperature");
            }

        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        }
    }
}
