using System.Collections.Generic;

namespace MangoBattery
{
    public class BatteryInfo
    {
        public bool HasBattery { get; set; }
        public string ErrorMessage { get; set; }
        public List<BatteryDetail> Batteries { get; set; } = new List<BatteryDetail>();
    }

    public class BatteryDetail
    {
        public int Index { get; set; }
        public string InstanceName { get; set; }
        public uint FullChargedCapacity { get; set; }
        public uint DesignedCapacity { get; set; }
        public uint CycleCount { get; set; }
        
        // Status properties
        public bool PowerOnline { get; set; }
        public bool Discharging { get; set; }
        public bool Charging { get; set; }
        public uint Voltage { get; set; }
        public int DischargeRate { get; set; }
        public int ChargeRate { get; set; }
        public uint RemainingCapacity { get; set; }
        public bool Active { get; set; }
        public bool Critical { get; set; }

        public int HealthPercentage => DesignedCapacity > 0 ? (int)(100 * FullChargedCapacity / DesignedCapacity) : 0;
        public int ChargePercentage => FullChargedCapacity > 0 ? (int)(100 * RemainingCapacity / FullChargedCapacity) : 0;
    }
}
