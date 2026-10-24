![screenshot](Resources/banner.jpg)

# MangoBattery

MangoBattery shows you the battery stats that actually matter - no command line required.

On Mac, CoconutBattery gives you cycle count, health percentage, and charge rates at a glance. Windows doesn't come with anything similar out of the box. The built-in option is running `powercfg /batteryreport` to generate a report, which is fine if you need a detailed analysis but overkill when you just want to check if your battery is holding up.

This app pulls the same WMI data and displays it in a small, simple window. You get current charge, battery health (full charged capacity versus design capacity), cycle count, and whether you're charging, discharging, or plugged in.

There's nothing to configure. Just run it.

## Screenshot
![screenshot](Resources/screenshot.png)

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
