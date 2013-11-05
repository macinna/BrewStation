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
using System.ComponentModel;
using System.Threading;
using System.Collections.Concurrent;
using ToggleSwitch;

namespace BrewStation
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private bool hltRegulationRunning = false;
        private bool mtRegulationRunning = false;


        private CancellationTokenSource HLTRegulatorCancellationTokenSource;
        private CancellationTokenSource MTRegulatorCancellationTokenSource;

        private Task HLTRegulatorTask;
        private Task MTRegulatorTask;

        private Temperatures kettleTemps = new Temperatures();

        public MainWindow()
        {

            InitializeComponent();

            if (!RelayController.InitializeRelays())
            {
                //relay initialization failure
                //nothing should happen now.  
                
            }

            MonitorAndUpdateTemperatureDisplays();
            
        }

        public Temperatures KettleTemperatures
        {
            get { return kettleTemps; }
        }



        private void MonitorAndUpdateTemperatureDisplays()
        {
            Task task = Task.Factory.StartNew(() =>
            {
                while (true)
                {
                    
                    this.Dispatcher.BeginInvoke(new Action(() =>
                    {
                        kettleTemps.MashTunTemperature = TemperatrureUtil.GetCurrentTemperature(TemperatureProbes.MashTun);
                        kettleTemps.HotLiquorTankTemperature = TemperatrureUtil.GetCurrentTemperature(TemperatureProbes.HotLiquorTank);
                        kettleTemps.BoilKettleTemperature = TemperatrureUtil.GetCurrentTemperature(TemperatureProbes.BoilKettle);
                    }), null);

                    Thread.Sleep(1000);
                }
            });
        }


        private void RegulateMashTunSwitch_Changed(object sender, RoutedEventArgs e)
        {
            //start process to regulate temperature of mt

            if (!mtRegulationRunning)
            {
                MTRegulatorCancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = MTRegulatorCancellationTokenSource.Token;

                int desiredTemp = Convert.ToInt32(MashTunSetPoint.Text);
                MTRegulatorTask = Task.Factory.StartNew(() => RegulateTemperature(Kettles.MashTun, desiredTemp, token), token);

                MashTunBurnerSwitch.IsEnabled = false;
                MashTunSetPoint.IsEnabled = false;
                mtRegulationRunning = true;

            }
            else
            {
                MTRegulatorCancellationTokenSource.Cancel();
                ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
                tasks.Add(MTRegulatorTask);
                Task.WaitAll(tasks.ToArray());
                MashTunBurnerSwitch.IsEnabled = true;
                MashTunSetPoint.IsEnabled = true;
                mtRegulationRunning = false;
            }


        }


        private void RegulateHotLiquorTankSwitch_Changed(object sender, RoutedEventArgs e)
        {
            //start process to regulate temperature of hlt

            if (!hltRegulationRunning)
            {
                HLTRegulatorCancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = HLTRegulatorCancellationTokenSource.Token;
                
                int desiredTemp = Convert.ToInt32(HotLiquorTankSetPoint.Text);
                HLTRegulatorTask = Task.Factory.StartNew(() => RegulateTemperature(Kettles.HotLiquorTank, desiredTemp, token), token);
             
                HotLiquorTankBurnerSwitch.IsEnabled = false;
                HotLiquorTankSetPoint.IsEnabled = false;
                hltRegulationRunning = true;

            }
            else
            {
                HLTRegulatorCancellationTokenSource.Cancel();
                ConcurrentBag<Task> tasks = new ConcurrentBag<Task>();
                tasks.Add(HLTRegulatorTask);
                Task.WaitAll(tasks.ToArray());
                HotLiquorTankBurnerSwitch.IsEnabled = true;
                HotLiquorTankSetPoint.IsEnabled = true;
                hltRegulationRunning = false;
            }

        }


        private void RegulateTemperature(Kettles kettle, int targetTemp, CancellationToken cancelToken)
        {

            Relays relay = Relays.MashTunBurner;
            TemperatureProbes probe = TemperatureProbes.MashTun;
            HorizontalToggleSwitch burnerSwitch = null;

            double target = Convert.ToDouble(targetTemp);

            switch (kettle)
            {
                case Kettles.HotLiquorTank:
                    relay = Relays.HotLiquorTankBurner;
                    probe = TemperatureProbes.HotLiquorTank;
                    burnerSwitch = HotLiquorTankBurnerSwitch;
                    break;
                case Kettles.MashTun:
                    relay = Relays.MashTunBurner;
                    probe = TemperatureProbes.MashTun;
                    burnerSwitch = MashTunBurnerSwitch;
                    break;
            }

            try
            {
                while (true)
                {
                    cancelToken.ThrowIfCancellationRequested();

                    //get temp from UI thread since it's already being monitored over there, 
                    //and we'd like the timing of the regulator to match the temp in the UI

                    double currentTemp = 0.0;

                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        if (probe == TemperatureProbes.HotLiquorTank)
                            currentTemp = kettleTemps.HotLiquorTankTemperature;
                        else
                            currentTemp = kettleTemps.MashTunTemperature;

                    }), null);


                    if (currentTemp < target)
                    {
                        //Turn on the buner
                        RelayController.CloseRelay(relay);


                        //update the UI
                        this.Dispatcher.BeginInvoke(new Action(() => burnerSwitch.IsChecked = true), null);
                        
                    }
                    else
                    {
                        //Turn off the buner
                        RelayController.OpenRelay(relay);
                        this.Dispatcher.BeginInvoke(new Action(() => burnerSwitch.IsChecked = false), null);
                    }

                    Thread.Sleep(1000);


                }
            }
            catch
            {

            
            }
            finally
            {
                //clean up by opening relays to turn off buner
                RelayController.OpenRelay(relay);
                this.Dispatcher.BeginInvoke(new Action(() => burnerSwitch.IsChecked = false), null);
            }

        }


        private void MashTunBurnerSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (MashTunBurnerSwitch.IsChecked == true)
                RelayController.CloseRelay(Relays.MashTunBurner);
            else
                RelayController.OpenRelay(Relays.MashTunBurner);
        }

        private void HotLiquorTankBurnerSwitch_Changed(object sender, RoutedEventArgs e)
        {
            
            if (HotLiquorTankBurnerSwitch.IsChecked == true)
                RelayController.CloseRelay(Relays.HotLiquorTankBurner);
            else
                RelayController.OpenRelay(Relays.HotLiquorTankBurner);

        }

        private void LeftPumpSwitch_Changed(object sender, RoutedEventArgs e)
        {


            if (LeftPumpSwitch.IsChecked == true)
                RelayController.CloseRelay(Relays.Pump1);
            else
                RelayController.OpenRelay(Relays.Pump1);

        }

        private void RightPumpSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (RightPumpSwitch.IsChecked == true)
                RelayController.CloseRelay(Relays.Pump2);
            else
                RelayController.OpenRelay(Relays.Pump2);

        }


    }
}
