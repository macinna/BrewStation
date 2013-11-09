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
using System.Windows.Threading;
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
        private bool timerRunning = false;
        private DispatcherTimer timer = null;


        private CancellationTokenSource HLTRegulatorCancellationTokenSource;
        private CancellationTokenSource MTRegulatorCancellationTokenSource;

        private BackgroundWorker temperatureUpdateWorker = new BackgroundWorker();



        private Task HLTRegulatorTask;
        private Task MTRegulatorTask;

        private Temperatures kettleTemps = new Temperatures();
        private string countDownTime;
        private RelayController relayController;
        private TemperatureReader temperatureReader;
        private bool isInitialized = true;
        private string hotLiquorTankSetPoint;
        private string mashTunSetPoint;

        public MainWindow()
        {

            InitializeComponent();

            this.IsEnabled = false;


            this.temperatureUpdateWorker.DoWork += temperatureUpdateWorker_DoWork;
            this.temperatureUpdateWorker.ProgressChanged += temperatureUpdateWorker_ProgressChanged;
            this.temperatureUpdateWorker.WorkerReportsProgress = true;

            this.temperatureReader = new OneWireTemperatureReader();            
            this.relayController = new TinyOSRelayController();


            if (!this.relayController.Initialize())
            {
                //relay initialization failure
                //nothing should happen now.  

                this.statusBarText.Text = "Could not initialize relay controller.";
                this.isInitialized = false;
 
            }


            if (!this.temperatureReader.Initialize())
            {
                //couldn't initialize temp controllers
                this.statusBarText.Text = "Could not initialize temperature probes.";
                this.isInitialized = false;
            }

            if(this.isInitialized)
            {
                this.statusBarText.Text = "Ready";
                //MonitorAndUpdateTemperatureDisplays();
                this.temperatureUpdateWorker.RunWorkerAsync(this.temperatureReader);

                this.IsEnabled = true;

            }
                
        }

  

        void temperatureUpdateWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            int[] temps = (int[]) e.UserState;
            this.kettleTemps.MashTunTemperature = temps[0];
            this.kettleTemps.HotLiquorTankTemperature = temps[1];
            this.kettleTemps.BoilKettleTemperature = temps[2];
        
        }

        void temperatureUpdateWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            TemperatureReader tempReader = (TemperatureReader)e.Argument;

            int[] temps = new int[3];

            while (true)
            {
                
                temps[0] = tempReader.GetCurrentTemperature(TemperatureProbes.MashTun);
                temps[1] = tempReader.GetCurrentTemperature(TemperatureProbes.HotLiquorTank);
                temps[2] = tempReader.GetCurrentTemperature(TemperatureProbes.BoilKettle);

                Thread.Sleep(1000);

                this.temperatureUpdateWorker.ReportProgress(0, temps);

            }
        }

        public Temperatures KettleTemperatures
        {
            get { return kettleTemps; }
        }



        private void RegulateMashTunSwitch_Changed(object sender, RoutedEventArgs e)
        {

            int setPoint = 0;

            if (!int.TryParse(MashTunSetPoint.Text, out setPoint))
            {
                //no number provided.  don't do anything.
                RegulateMashTunSwitch.IsChecked = false;
                return;
            }
                


            //start process to regulate temperature of mt

            if (!mtRegulationRunning)
            {
                MTRegulatorCancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = MTRegulatorCancellationTokenSource.Token;

                MTRegulatorTask = Task.Factory.StartNew(() => RegulateTemperature(Kettles.MashTun, setPoint, token), token);

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


            int setPoint = 0;

            if (!int.TryParse(HotLiquorTankSetPoint.Text, out setPoint))
            {
                //no number provided.  don't do anything.
                RegulateHotLiquorTankSwitch.IsChecked = false;
                return;
            }

            //start process to regulate temperature of hlt

            if (!hltRegulationRunning)
            {
                this.statusBarText.Text = "Regulating HLT Temperature...";
                HLTRegulatorCancellationTokenSource = new CancellationTokenSource();
                CancellationToken token = HLTRegulatorCancellationTokenSource.Token;
                
                HLTRegulatorTask = Task.Factory.StartNew(() => RegulateTemperature(Kettles.HotLiquorTank, setPoint, token), token);
             
                HotLiquorTankBurnerSwitch.IsEnabled = false;
                HotLiquorTankSetPoint.IsEnabled = false;
                hltRegulationRunning = true;

            }
            else
            {
                this.statusBarText.Text = "Ready";
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
                        this.relayController.CloseRelay(relay);


                        //update the UI
                        this.Dispatcher.BeginInvoke(new Action(() => burnerSwitch.IsChecked = true), null);
                        
                    }
                    else
                    {
                        //Turn off the buner
                        this.relayController.OpenRelay(relay);
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
                this.relayController.OpenRelay(relay);
                this.Dispatcher.BeginInvoke(new Action(() => burnerSwitch.IsChecked = false), null);
            }

        }


        private void MashTunBurnerSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (MashTunBurnerSwitch.IsChecked == true)
                this.relayController.CloseRelay(Relays.MashTunBurner);
            else
                this.relayController.OpenRelay(Relays.MashTunBurner);
        }

        private void HotLiquorTankBurnerSwitch_Changed(object sender, RoutedEventArgs e)
        {
            
            if (HotLiquorTankBurnerSwitch.IsChecked == true)
                this.relayController.CloseRelay(Relays.HotLiquorTankBurner);
            else
                this.relayController.OpenRelay(Relays.HotLiquorTankBurner);

        }

        private void LeftPumpSwitch_Changed(object sender, RoutedEventArgs e)
        {


            if (LeftPumpSwitch.IsChecked == true)
                this.relayController.CloseRelay(Relays.Pump1);
            else
                this.relayController.OpenRelay(Relays.Pump1);

        }

        private void RightPumpSwitch_Changed(object sender, RoutedEventArgs e)
        {
            if (RightPumpSwitch.IsChecked == true)
                this.relayController.CloseRelay(Relays.Pump2);
            else
                this.relayController.OpenRelay(Relays.Pump2);

        }

        private void CountdownTimerStartButton_Click(object sender, RoutedEventArgs e)
        {

            int hours = int.Parse(String.IsNullOrEmpty(this.CountdownTimerTextBoxHours.Text) ? "0" : this.CountdownTimerTextBoxHours.Text);
            int minutes = int.Parse(String.IsNullOrEmpty(this.CountdownTimerTextBoxMinutes.Text) ? "0" : this.CountdownTimerTextBoxMinutes.Text);
            int seconds = int.Parse(String.IsNullOrEmpty(this.CountdownTimerTextBoxSeconds.Text) ? "0" : this.CountdownTimerTextBoxSeconds.Text);
 

            if(this.timerRunning)
            {
                this.timer.Stop();
                CountdownTimerTextBoxSeconds.IsEnabled = true;
                CountdownTimerTextBoxMinutes.IsEnabled = true;
                CountdownTimerTextBoxHours.IsEnabled = true;
                CountdownTimerStartButton.Content = "Start";
                this.timerRunning = false;
                return;
            }


            CountdownTimerTextBoxSeconds.IsEnabled = false;
            CountdownTimerTextBoxMinutes.IsEnabled = false;
            CountdownTimerTextBoxHours.IsEnabled = false;

            CountdownTimerStartButton.Content = "Stop";

            TimeSpan time = new TimeSpan(hours, minutes, seconds);

            timer = new DispatcherTimer(new TimeSpan(0, 0, 1), DispatcherPriority.Normal, delegate
            {
                CountdownTimerTextBoxHours.Text = time.Hours.ToString();
                CountdownTimerTextBoxMinutes.Text = time.Minutes.ToString();
                CountdownTimerTextBoxSeconds.Text = time.Seconds.ToString();

                if (time == TimeSpan.Zero)
                {
                    timer.Stop();
                    CountdownTimerTextBoxSeconds.IsEnabled = true;
                    CountdownTimerTextBoxMinutes.IsEnabled = true;
                    CountdownTimerTextBoxHours.IsEnabled = true;
                    CountdownTimerStartButton.Content = "Start";
                    this.timerRunning = false;
                }
                time = time.Add(TimeSpan.FromSeconds(-1)); 

            }, Application.Current.Dispatcher);

            timer.Start();
            this.timerRunning = true;

        }

        private void CountdownTimerTextBox_PreviewKeyDown(object sender, KeyEventArgs e)
        {

            if ((e.Key <= Key.D9 && e.Key >= Key.D0) || (e.Key <= Key.NumPad9 && e.Key >= Key.NumPad0) || e.Key == Key.Delete || e.Key == Key.Back)
            {
                e.Handled = false;
            }
            else
            {
                e.Handled = true;
            }
            base.OnPreviewKeyDown(e);
        }


        private void _this_Closing(object sender, CancelEventArgs e)
        {
            //open all relays upon exit
            this.relayController.OpenAllRelays();
        }

    }
}
