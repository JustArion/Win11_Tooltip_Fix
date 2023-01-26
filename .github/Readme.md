### Workflow Output:
The following contains the latest build outputs from a ([Github Build Action](https://github.com/JustArion/Win11_PopupHost_Fix/actions/workflows/Build.yml))

### Requirements (To Run & Build)
* ([.NET 6.0.X Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) (Select the ".NET Desktop Runtime 6.0.~" for x86)
* ([.NET 6.0.X SDK](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) (Select the ".NET SDK 6.0.~" for x86)
* (Optional* For Downloading the Repo) ([Git](https://git-scm.com/downloads))

### Build Steps (Simple)
* git clone https://github.com/JustArion/Win11_PopupHost_Fix
* cd .\Win11_PopupHost_Fix\
* ./Build.ps1
* (Optional) Run As Administrator (For editing other programs that run in higher elevations, eg. Task Manager)
* Enjoy.

### Build Steps (Detailed)
* git clone https://github.com/JustArion/Win11_PopupHost_Fix
* cd .\Win11_PopupHost_Fix\
* ([dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) restore ./src/
* ([dotnet](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) publish ./src/ --no-restore --runtime win-x86 -p:PublishSingleFile=true --output ./src/Publish --no-self-contained --configuration Release;
* (Optional) Run As Administrator (For editing other programs that run in higher elevations, eg. Task Manager)
* Enjoy.

### Running on Startup
You can add a Task Scheduler script to have it auto start or throw it in your `shell:Startup` folder and get a UAC prompt each startup ;)

The popup blocks the mouse from clicking things behind the popup which causes issue during window dragging since Windows File Explorer has the same issue, Hovering Tabs shows their names, "Sort" and "View" and "..." displays their respective popups which block the ability to click and drag the explorer window. This fix addresses the issue.

### Packages
<p align="center">
The following packages are used in this project:
</p>

```xml
<PackageReference Include="Interop.UIAutomationClient" Version="10.19041.0" />
<PackageReference Include="Vanara.PInvoke.Kernel32" Version="0.7.124" />
<PackageReference Include="Vanara.PInvoke.User32" Version="0.7.124" />
```

Pictures of an info popup:

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287020038901920/5096f326-4e01-47b6-bdac-039aec7da779_30-10-2022.png"/>

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287334846578770/d62c5052-a6c1-461b-bff9-33418f2d2d20_30-10-2022.png"/>

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036287528354988163/ca74a981-2d73-49da-852d-d42f831d588c_30-10-2022.png"/>
