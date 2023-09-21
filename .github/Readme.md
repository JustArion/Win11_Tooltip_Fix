### Prerequisites
* [.NET 7.0.X Desktop Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/7.0) (Select the ".NET Desktop Runtime 7.0" for `x86`)

### Installing
Given you've installed the `Prerequisites`
To `Install` or `Uninstall` simply run `Start_Tooltip_Fix.exe` from the extracted .zip from the releases section.

#### Optional
The program can be installed as admin to affect programs that run as admin too but is `not required`

### Requirements (To Build from Source)
* [.NET 7.0.X SDK - x86](https://dotnet.microsoft.com/en-us/download/dotnet/7.0)
* Optional* [Git](https://git-scm.com/downloads)

### Building from Source
`Make sure the proper .NET 7.0.X SDK is installed.`
```ps1
git clone https://github.com/JustArion/Win11_Tooltip_Fix
cd .\Win11_Tooltip_Fix\
Set-ExecutionPolicy Bypass -Scope Process
./Build.ps1
```

### Packages
<p align="center">
The following packages are used in this project:
</p>

```xml
        <PackageReference Include="TaskScheduler" Version="2.10.1" />
        <PackageReference Include="Interop.UIAutomationClient.Signed" Version="10.19041.0" />
        <PackageReference Include="Vanara.PInvoke.Kernel32" Version="3.4.16" />
        <PackageReference Include="Vanara.PInvoke.User32" Version="3.4.15" />
        <PackageReference Include="Serilog.Sinks.Console" Version="4.1.0" />
        <PackageReference Include="Serilog.Enrichers.Process" Version="2.0.2" />
        <PackageReference Include="Serilog.Sinks.Seq" Version="5.2.2" />
```

Addresses: 
https://linustechtips.com/topic/1466612-windows-11-annoyance-tooltips-blocking-every-other-action/
Pictures of an info popup:

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287020038901920/5096f326-4e01-47b6-bdac-039aec7da779_30-10-2022.png"/>

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287334846578770/d62c5052-a6c1-461b-bff9-33418f2d2d20_30-10-2022.png"/>

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287528354988163/ca74a981-2d73-49da-852d-d42f831d588c_30-10-2022.png"/>
