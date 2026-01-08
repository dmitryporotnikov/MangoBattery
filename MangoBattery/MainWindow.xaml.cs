using System;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Threading;

namespace MangoBattery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer updateTimer;
        private BatteryService _batteryService;

        public MainWindow()
        {
            InitializeComponent();

            _batteryService = new BatteryService();

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(5);
            updateTimer.Tick += UpdateTimer_Tick;
            
            Loaded += MainWindow_Loaded;
        }

        private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await UpdateBatteryInfoAsync();
            updateTimer.Start();
        }

        private async void UpdateTimer_Tick(object sender, EventArgs e)
        {
            await UpdateBatteryInfoAsync();
        }

        private async System.Threading.Tasks.Task UpdateBatteryInfoAsync()
        {
            txtMachineName.Text = $"Machine Name: {Environment.MachineName}";
            txtCurrentTime.Text = $"Snapshot Time: {DateTime.Now}";
            
            // Show loading state if needed, or just wait for update
            
            var info = await _batteryService.GetBatteryInfoAsync();

            txtBatteryInfo.Inlines.Clear();

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                AppendText($"Error retrieving battery info: {info.ErrorMessage}");
                ResetProgressBars();
                return;
            }

            if (!info.HasBattery || info.Batteries.Count == 0)
            {
                AppendText("No battery information available.");
                ResetProgressBars();
                return;
            }

            foreach (var battery in info.Batteries)
            {
                AppendText($"Battery {battery.Index}\n-----------");
                AppendKeyValue("Tag", $"Battery {battery.Index}"); // Tag wasn't in my model, using simpler label
                AppendKeyValue("Name", battery.InstanceName);
                AppendKeyValue("Power Online", battery.PowerOnline);
                AppendKeyValue("Discharging", battery.Discharging);
                AppendKeyValue("Charging", battery.Charging);
                AppendKeyValue("Voltage", battery.Voltage, "mV");
                AppendKeyValue("Discharge Rate", battery.DischargeRate, "mW");
                AppendKeyValue("Charge Rate", battery.ChargeRate, "mW");
                AppendKeyValue("Remaining Capacity", battery.RemainingCapacity, "mWh");
                AppendKeyValue("Active", battery.Active);
                AppendKeyValue("Critical", battery.Critical);

                if (battery.DesignedCapacity > 0)
                {
                    int health = battery.HealthPercentage;
                    AppendKeyValue("Battery Health", $"{health}%", "");

                    // Update progress bar for battery health
                    progressBarHealth.Value = health;
                    txtHealthPercentage.Text = $"{health}%";
                    
                    // Color code health
                    progressBarHealth.Foreground = GetColorForHealth(health);
                }
                else
                {
                    AppendKeyValue("Battery Health", "Unknown (Designed Capacity is 0)");
                }

                if (battery.FullChargedCapacity > 0)
                {
                    int currentChargePercent = battery.ChargePercentage;
                    
                    // Update progress bar for current charge
                    progressBarCurrentCharge.Value = currentChargePercent;
                    txtCurrentChargePercentage.Text = $"{currentChargePercent}%";
                }

                AppendKeyValue("Cycle Count", battery.CycleCount);

                AppendText("\n");
            }
        }

        private void ResetProgressBars()
        {
            progressBarHealth.Value = 0;
            txtHealthPercentage.Text = "N/A";
            progressBarCurrentCharge.Value = 0;
            txtCurrentChargePercentage.Text = "N/A";
        }

        private SolidColorBrush GetColorForHealth(int health)
        {
            if (health >= 80) return Brushes.Green;
            if (health >= 50) return Brushes.Orange;
            return Brushes.Red;
        }

        private void AppendText(string text)
        {
            txtBatteryInfo.Inlines.Add(new Run(text + "\n"));
        }

        private void AppendKeyValue(string key, object value, string unit = "")
        {
            txtBatteryInfo.Inlines.Add(new Bold(new Run($"{key}: ")));
            txtBatteryInfo.Inlines.Add(new Run($"{value}{unit}\n"));
        }
    }
}
