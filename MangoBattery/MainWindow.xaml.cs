using System;
using System.Collections.Generic;
using System.Management;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Threading;

namespace MangoBattery
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private DispatcherTimer updateTimer;

        public MainWindow()
        {
            InitializeComponent();

            updateTimer = new DispatcherTimer();
            updateTimer.Interval = TimeSpan.FromSeconds(5);
            updateTimer.Tick += UpdateTimer_Tick;
            updateTimer.Start();

            Loaded += MainWindow_Loaded;
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateBatteryInfo();
        }

        private void UpdateTimer_Tick(object sender, EventArgs e)
        {
            UpdateBatteryInfo();
        }

        private void UpdateBatteryInfo()
        {
            ManagementScope scope = new ManagementScope(@"\\.\root\wmi");

            // Queries
            ObjectQuery fullChargeQuery = new ObjectQuery("Select * from BatteryFullChargedCapacity");
            ObjectQuery designedCapacityQuery = new ObjectQuery("Select * from BatteryStaticData");
            ObjectQuery statusQuery = new ObjectQuery("Select * from BatteryStatus where Voltage > 0");
            ObjectQuery cycleCountQuery = new ObjectQuery("Select * from BatteryCycleCount");

            // Searchers
            ManagementObjectSearcher fullChargeSearcher = new ManagementObjectSearcher(scope, fullChargeQuery);
            ManagementObjectSearcher designedCapacitySearcher = new ManagementObjectSearcher(scope, designedCapacityQuery);
            ManagementObjectSearcher statusSearcher = new ManagementObjectSearcher(scope, statusQuery);
            ManagementObjectSearcher cycleCountSearcher = new ManagementObjectSearcher(scope, cycleCountQuery);

            var fullChargedCapacities = new Dictionary<int, uint>();
            var designedCapacities = new Dictionary<int, uint>();
            var cycleCounts = new Dictionary<int, uint>();

            txtBatteryInfo.Inlines.Clear();

            txtMachineName.Text = $"Machine Name: {Environment.MachineName}";
            txtCurrentTime.Text = $"Snapshot Time: {DateTime.Now}";

            int i = 0;
            foreach (ManagementObject battery in fullChargeSearcher.Get())
            {
                uint fullChargedCapacity = (uint)battery["FullChargedCapacity"];
                fullChargedCapacities[i] = fullChargedCapacity;
                i++;
            }

            i = 0;
            foreach (ManagementObject battery in designedCapacitySearcher.Get())
            {
                uint designedCapacity = (uint)battery["DesignedCapacity"];
                designedCapacities[i] = designedCapacity;
                i++;
            }

            i = 0;
            foreach (ManagementObject battery in cycleCountSearcher.Get())
            {
                uint cycleCount = (uint)battery["CycleCount"];
                cycleCounts[i] = cycleCount;
                i++;
            }

            i = 0;
            foreach (ManagementObject battery in statusSearcher.Get())
            {
                AppendText($"Battery {i}\n-----------");
                AppendKeyValue("Tag", battery["Tag"]);
                AppendKeyValue("Name", battery["InstanceName"]);
                AppendKeyValue("Power Online", battery["PowerOnline"]);
                AppendKeyValue("Discharging", battery["Discharging"]);
                AppendKeyValue("Charging", battery["Charging"]);
                AppendKeyValue("Voltage", battery["Voltage"], "mV");
                AppendKeyValue("Discharge Rate", battery["DischargeRate"], "mW");
                AppendKeyValue("Charge Rate", battery["ChargeRate"], "mW");
                AppendKeyValue("Remaining Capacity", battery["RemainingCapacity"], "mWh");
                AppendKeyValue("Active", battery["Active"]);
                AppendKeyValue("Critical", battery["Critical"]);

                if (fullChargedCapacities.ContainsKey(i) && designedCapacities.ContainsKey(i))
                {
                    uint fullChargedCapacity = fullChargedCapacities[i];
                    uint designedCapacity = designedCapacities[i];
                    int health = (int)(100 * fullChargedCapacity / designedCapacity);
                    AppendKeyValue("Battery Health", $"{health}%", "");

                    // Update progress bar for battery health
                    progressBarHealth.Value = health;
                    txtHealthPercentage.Text = $"{health}%";

                    // Calculate and update progress bar for current charge
                    uint remainingCapacity = (uint)battery["RemainingCapacity"];
                    int currentChargePercent = (int)(100 * remainingCapacity / fullChargedCapacity);
                    progressBarCurrentCharge.Value = currentChargePercent;
                    txtCurrentChargePercentage.Text = $"{currentChargePercent}%";
                }

                if (cycleCounts.ContainsKey(i))
                {
                    AppendKeyValue("Cycle Count", cycleCounts[i]);
                }

                AppendText("\n");
                i++;
            }

            if (i == 0)
            {
                AppendText("No battery information available.");
            }
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
