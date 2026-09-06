using System;
using System.Threading.Tasks;
using Windows.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;

namespace MangoBattery
{
    public sealed partial class MainWindow : Window
    {
        private const int WindowClientWidth = 360;
        private const int WindowClientHeight = 600;

        private BatteryService _batteryService;

        public MainWindow()
        {
            InitializeComponent();

            _batteryService = new BatteryService();

            ExtendsContentIntoTitleBar = true;
            SetTitleBar(AppTitleBar);
            SystemBackdrop = new MicaBackdrop();

            Activated += MainWindow_Activated;
        }

        private async void MainWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (args.WindowActivationState != WindowActivationState.CodeActivated &&
                args.WindowActivationState != WindowActivationState.PointerActivated)
            {
                return;
            }

            Activated -= MainWindow_Activated;

            // AppWindow sizing works in physical pixels; XamlRoot (and therefore
            // RasterizationScale) is only available once the content has loaded.
            ((FrameworkElement)Content).Loaded += (s, e) =>
            {
                var scale = Content.XamlRoot.RasterizationScale;
                AppWindow.ResizeClient(new Windows.Graphics.SizeInt32(
                    (int)(WindowClientWidth * scale),
                    (int)(WindowClientHeight * scale)));

                var iconPath = System.IO.Path.Combine(AppContext.BaseDirectory, "icon_256x256.ico");
                AppWindow.SetIcon(iconPath);
            };

            await UpdateBatteryInfoAsync();

            var timer = Microsoft.UI.Dispatching.DispatcherQueue.GetForCurrentThread().CreateTimer();
            timer.Interval = TimeSpan.FromSeconds(5);
            timer.IsRepeating = true;
            timer.Tick += async (s, e) => await UpdateBatteryInfoAsync();
            timer.Start();
        }

        private async Task UpdateBatteryInfoAsync()
        {
            MachineNameText.Text = Environment.MachineName;
            SnapshotTimeText.Text = $"Snapshot: {DateTime.Now}";

            var info = await _batteryService.GetBatteryInfoAsync();

            if (!string.IsNullOrEmpty(info.ErrorMessage))
            {
                ShowInfoBar(InfoBarSeverity.Error, $"Error retrieving battery info: {info.ErrorMessage}");
                ResetCards();
                DetailsPanel.Children.Clear();
                return;
            }

            if (!info.HasBattery || info.Batteries.Count == 0)
            {
                ShowInfoBar(InfoBarSeverity.Warning, "No battery information available.");
                ResetCards();
                DetailsPanel.Children.Clear();
                return;
            }

            StatusInfoBar.IsOpen = false;

            var primary = info.Batteries[0];
            UpdateChargeCard(primary);
            UpdateHealthCard(primary);
            BuildBatteryDetails(info.Batteries);
        }

        private void UpdateChargeCard(BatteryDetail battery)
        {
            if (battery.FullChargedCapacity > 0)
            {
                int chargePercent = battery.ChargePercentage;
                ChargePercentageText.Text = $"{chargePercent}%";
                ChargeProgressBar.Value = chargePercent;
            }
            else
            {
                ChargePercentageText.Text = "N/A";
                ChargeProgressBar.Value = 0;
            }

            if (battery.Charging)
            {
                SetStatus(Microsoft.UI.Colors.MediumSeaGreen, "Charging");
                RateText.Text = FormatMilliwatts(battery.ChargeRate);
            }
            else if (battery.Discharging)
            {
                SetStatus(Microsoft.UI.Colors.Orange, "Discharging");
                RateText.Text = FormatMilliwatts(battery.DischargeRate);
            }
            else if (battery.PowerOnline)
            {
                SetStatus(Microsoft.UI.Colors.MediumSeaGreen, "Plugged in");
                RateText.Text = "";
            }
            else
            {
                SetStatus(Microsoft.UI.Colors.Gray, "Not charging");
                RateText.Text = "";
            }
        }

        private void UpdateHealthCard(BatteryDetail battery)
        {
            if (battery.DesignedCapacity > 0)
            {
                int health = battery.HealthPercentage;
                HealthPercentageText.Text = $"{health}%";
                HealthProgressBar.Value = health;
                HealthProgressBar.Foreground = GetBrushForHealth(health);
            }
            else
            {
                HealthPercentageText.Text = "N/A";
                HealthProgressBar.Value = 0;
                HealthProgressBar.Foreground = GetBrushForHealth(0);
            }

            if (battery.FullChargedCapacity > 0)
            {
                CapacityText.Text = $"{battery.RemainingCapacity:N0} / {battery.FullChargedCapacity:N0} mWh";
            }
            else
            {
                CapacityText.Text = "Full charge capacity unknown";
            }

            CyclesText.Text = $"Cycle count: {battery.CycleCount:N0}";
        }

        private void BuildBatteryDetails(System.Collections.Generic.List<BatteryDetail> batteries)
        {
            DetailsPanel.Children.Clear();

            foreach (var battery in batteries)
            {
                var panel = new StackPanel { Spacing = 4 };
                AddKeyValue(panel, "Battery", $"Battery {battery.Index}");
                AddKeyValue(panel, "Name", battery.InstanceName);
                AddKeyValue(panel, "Power Online", battery.PowerOnline);
                AddKeyValue(panel, "Discharging", battery.Discharging);
                AddKeyValue(panel, "Charging", battery.Charging);
                AddKeyValue(panel, "Voltage", battery.Voltage, "mV");
                AddKeyValue(panel, "Discharge Rate", battery.DischargeRate, "mW");
                AddKeyValue(panel, "Charge Rate", battery.ChargeRate, "mW");
                AddKeyValue(panel, "Remaining Capacity", battery.RemainingCapacity, "mWh");
                AddKeyValue(panel, "Active", battery.Active);
                AddKeyValue(panel, "Critical", battery.Critical);
                AddKeyValue(panel, "Cycle Count", battery.CycleCount);

                if (battery.DesignedCapacity > 0)
                {
                    AddKeyValue(panel, "Battery Health", $"{battery.HealthPercentage}%");
                }
                else
                {
                    AddKeyValue(panel, "Battery Health", "Unknown (Designed Capacity is 0)");
                }

                if (battery.Index < batteries.Count - 1)
                {
                    panel.Children.Add(new Microsoft.UI.Xaml.Controls.Border
                    {
                        Height = 1,
                        Margin = new Thickness(0, 8, 0, 0),
                        Background = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                        Opacity = 0.3
                    });
                }

                DetailsPanel.Children.Add(panel);
            }
        }

        private static void AddKeyValue(StackPanel panel, string key, object value, string unit = "")
        {
            var text = new Microsoft.UI.Xaml.Controls.TextBlock();
            var run = new Microsoft.UI.Xaml.Documents.Run { Text = key + ": " };
            run.FontWeight = Microsoft.UI.Text.FontWeights.SemiBold;
            text.Inlines.Add(run);
            text.Inlines.Add(new Microsoft.UI.Xaml.Documents.Run { Text = $"{value}{unit}" });
            text.TextWrapping = Microsoft.UI.Xaml.TextWrapping.Wrap;
            panel.Children.Add(text);
        }

        private void SetStatus(Color color, string text)
        {
            StatusDot.Fill = new SolidColorBrush(color);
            StatusText.Text = text;
        }

        private static SolidColorBrush GetBrushForHealth(int health)
        {
            var color = health >= 80 ? Microsoft.UI.Colors.MediumSeaGreen
                      : health >= 50 ? Microsoft.UI.Colors.Orange
                      : Microsoft.UI.Colors.OrangeRed;
            return new SolidColorBrush(color);
        }

        private static string FormatMilliwatts(int milliwatts)
        {
            return milliwatts != 0 ? $"{milliwatts:N0} mW" : "";
        }

        private void ResetCards()
        {
            ChargePercentageText.Text = "N/A";
            ChargeProgressBar.Value = 0;
            HealthPercentageText.Text = "N/A";
            HealthProgressBar.Value = 0;
            CapacityText.Text = "";
            CyclesText.Text = "";
            SetStatus(Microsoft.UI.Colors.Gray, "");
            RateText.Text = "";
        }

        private void ShowInfoBar(InfoBarSeverity severity, string message)
        {
            StatusInfoBar.Severity = severity;
            StatusInfoBar.Message = message;
            StatusInfoBar.IsOpen = true;
        }
    }
}