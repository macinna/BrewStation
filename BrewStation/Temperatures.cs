using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;


namespace BrewStation
{
    public class Temperatures //: INotifyPropertyChanged 
    {

        private double mashTunTemp;
        private double hotLiquorTankTemp;
        private double boilKettleTemp;

        public double MashTunTemperature
        {
            get
            {
                return mashTunTemp;
            }
            set
            {
                mashTunTemp = value;
                //OnPropertyChanged("MashTunTemperature");
            }

        }

        public double HotLiquorTankTemperature
        {
            get
            {
                return hotLiquorTankTemp;
            }
            set
            {
                hotLiquorTankTemp = value;
                //OnPropertyChanged("HotLiquorTankTemperature");
            }

        }

        public double BoilKettleTemperature
        {
            get
            {
                return boilKettleTemp;
            }
            set
            {
                boilKettleTemp = value;
                //OnPropertyChanged("BoilKettleTemperature");
            }

        }

        //public event PropertyChangedEventHandler PropertyChanged;

        //private void OnPropertyChanged(string propertyName)
        //{
        //    if (PropertyChanged != null)
        //        PropertyChanged(this, new PropertyChangedEventArgs(propertyName));

        //}
    }
}
