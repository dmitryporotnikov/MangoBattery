using System;
using System.Collections.Generic;
using System.Management;
using System.Threading.Tasks;

namespace MangoBattery
{
    public class BatteryService
    {
        public async Task<BatteryInfo> GetBatteryInfoAsync()
        {
            return await Task.Run(() =>
            {
                var info = new BatteryInfo();
                try
                {
                    var scope = new ManagementScope(@"\\.\root\wmi");
                    // Check if WMI path exists by trying to connect
                    scope.Connect();

                    // Pre-fetch data into dictionaries to map by instance if possible, 
                    // though usually WMI returns collections we can iterate.
                    // However, we need to correlate data from different classes. 

                    var statusList = GetManagementObjects(scope, "Select * from BatteryStatus where Voltage > 0");
                    if (statusList.Count == 0)
                    {
                        info.HasBattery = false;
                        return info;
                    }

                    var fullChargeList = GetManagementObjects(scope, "Select * from BatteryFullChargedCapacity");
                    var designedStartList = GetManagementObjects(scope, "Select * from BatteryStaticData");
                    var cycleCountList = GetManagementObjects(scope, "Select * from BatteryCycleCount");

                    info.HasBattery = true;

                    for (int i = 0; i < statusList.Count; i++)
                    {
                        var status = statusList[i];
                        var detail = new BatteryDetail
                        {
                            Index = i,
                            InstanceName = TryGetProperty<string>(status, "InstanceName"),
                            PowerOnline = TryGetProperty<bool>(status, "PowerOnline"),
                            Discharging = TryGetProperty<bool>(status, "Discharging"),
                            Charging = TryGetProperty<bool>(status, "Charging"),
                            Voltage = TryGetProperty<uint>(status, "Voltage"),
                            DischargeRate = TryGetProperty<int>(status, "DischargeRate"), // Can be negative? usually uint or int
                            ChargeRate = TryGetProperty<int>(status, "ChargeRate"),
                            RemainingCapacity = TryGetProperty<uint>(status, "RemainingCapacity"),
                            Active = TryGetProperty<bool>(status, "Active"),
                            Critical = TryGetProperty<bool>(status, "Critical")
                        };

                        // Correlate with others by Index
                        if (i < fullChargeList.Count)
                            detail.FullChargedCapacity = TryGetProperty<uint>(fullChargeList[i], "FullChargedCapacity");
                        
                        if (i < designedStartList.Count)
                            detail.DesignedCapacity = TryGetProperty<uint>(designedStartList[i], "DesignedCapacity");

                        if (i < cycleCountList.Count)
                            detail.CycleCount = TryGetProperty<uint>(cycleCountList[i], "CycleCount");

                        info.Batteries.Add(detail);
                    }
                }
                catch (Exception ex)
                {
                    // If we can't connect to WMI or query fails completely
                    info.HasBattery = false;
                    info.ErrorMessage = ex.Message;
                }

                return info;
            });
        }

        private List<ManagementObject> GetManagementObjects(ManagementScope scope, string query)
        {
            var list = new List<ManagementObject>();
            try
            {
                using (var searcher = new ManagementObjectSearcher(scope, new ObjectQuery(query)))
                using (var collection = searcher.Get())
                {
                    foreach (ManagementObject obj in collection)
                    {
                        list.Add(obj);
                    }
                }
            }
            catch
            {
                // Ignore individual query failures (e.g. if a class doesn't exist on this machine)
            }
            return list;
        }

        private T TryGetProperty<T>(ManagementObject obj, string name)
        {
            try
            {
                return (T)obj[name];
            }
            catch
            {
                return default;
            }
        }
    }
}
