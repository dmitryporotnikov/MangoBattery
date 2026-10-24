![banner](Resources/banner.jpg)

# MangoBattery

[![Windows](https://img.shields.io/badge/Windows-10%20%7C%2011-0078D6?logo=windows)](https://www.microsoft.com/windows)
[![.NET 8](https://img.shields.io/badge/.NET-8.0-512BD4?logo=dotnet)](https://dotnet.microsoft.com/)
[![Latest build](https://img.shields.io/github/v/release/dmitryporotnikov/MangoBattery?label=build)](https://github.com/dmitryporotnikov/MangoBattery/releases/latest)
[![License: GPL-3.0](https://img.shields.io/github/license/dmitryporotnikov/MangoBattery)](LICENSE)

MangoBattery shows the battery numbers that actually matter, without a command line.

On macOS, CoconutBattery gives you cycle count, health and charge rate at a glance. Windows has nothing like it out of the box. `powercfg /batteryreport` will generate a full report, which is more than you need when the only question is whether the battery is holding up.

The app reads the same WMI data and puts it in a small window: current charge, health as full charge capacity against design capacity, cycle count, and whether the machine is charging, discharging or plugged in. There is nothing to configure. Run it.

## Download

The portable, self-contained build is on the [releases page](https://github.com/dmitryporotnikov/MangoBattery/releases/latest). Unzip `MangoBattery-win-x64.zip`, run `MangoBattery.exe`. No installer, no runtime to install.

## Screenshot

![screenshot](Resources/screenshot.png)

## Build

With the .NET 8 SDK:

```bash
dotnet build MangoBattery -c Release -p:Platform=x64
```

The release workflow publishes a self-contained win-x64 ZIP on every push to `main`.

## Microsoft Store submission

Open `MangoBattery.sln` in Visual Studio with the Windows App SDK / WinUI tooling installed. The project defaults to single-project MSIX packaging and includes `Package.appxmanifest` and logo assets derived from the existing app icon.

1. Right-click **MangoBattery > Package and Publish > Associate App with the Store** and select your existing listing. The checked-in identity (`MangoBattery`, `CN=MangoBattery`) is for development only. Association must replace the identity name, publisher, and publisher display name with the values from Partner Center.
2. Set the manifest version higher than the previous submission. For Store submissions, keep the fourth version component at `0`.
3. Choose **Package and Publish > Create App Packages > Microsoft Store**, select Release and the architectures you intend to support, and run the Windows App Certification Kit checks. Start with x64; test x86 and ARM64 on appropriate devices before submitting those builds.
4. Upload the resulting `.msixupload` from `MangoBattery/AppPackages` to Partner Center. Use the Store option rather than the Sideloading wizard or the GitHub portable ZIP. A successful local packaging build does not establish Store certification or capability approval.

### Why runFullTrust is declared

This is a WinUI desktop application running at medium integrity, which requires `runFullTrust`. BatteryService uses `System.Management` to query the local `root\\wmi` namespace for `BatteryStatus`, `BatteryFullChargedCapacity`, `BatteryStaticData`, and `BatteryCycleCount`. Removing the capability alone is not a supported fix: moving to AppContainer would also require validating or replacing that data-access implementation.

In Partner Center's restricted-capability section, request approval with an explanation such as:

> MangoBattery is a WinUI 3 desktop battery-monitoring application. It uses System.Management to perform read-only queries of local Windows WMI battery classes (BatteryStatus, BatteryFullChargedCapacity, BatteryStaticData, and BatteryCycleCount) to display charge, health, capacity, voltage, charge/discharge rate, and cycle count. runFullTrust is required for its desktop execution model and WMI-based implementation. The app does not request administrator elevation or modify battery or system settings.

If approval has already been denied, address the certification feedback with this explanation; rebuilding the package cannot grant approval.

References: [Microsoft Store packaging](https://learn.microsoft.com/en-us/windows/msix/package/packaging-uwp-apps), [capability requirements and approval](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/app-capability-declarations), [AppContainer configuration](https://learn.microsoft.com/en-us/windows/msix/msix-container).

### Local packaging verification

From a Visual Studio Developer PowerShell prompt:

```powershell
msbuild MangoBattery/MangoBattery.csproj /restore /p:Configuration=Release /p:Platform=x64 /p:RuntimeIdentifier=win-x64 /p:SelfContained=true /p:GenerateAppxPackageOnBuild=true /p:UapAppxPackageBuildMode=StoreUpload /p:AppxPackageSigningEnabled=false /p:AppxBundle=Never
```

This creates an unsigned Store upload for verification; associate the project with the Store before creating the actual submission. It is not a signed sideload installer.

The GitHub release workflow explicitly uses `WindowsPackageType=None` to retain portable builds:

```powershell
dotnet publish MangoBattery -c Release -p:Platform=x64 -r win-x64 -p:SelfContained=true -p:WindowsPackageType=None -p:AppxPackage=false
```

## License

GPL-3.0. See [LICENSE](LICENSE).
