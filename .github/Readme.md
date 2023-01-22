### Steps
* Build ([DotNet 6 SDK Required](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) (Select the ".NET SDK 6.0.~" for x86)
* (Optional) Run As Administrator (For editing other programs that run in higher elevations, eg. Task Manager)
* Enjoy.

### Requirements (To Run)
* ([.NET 6.0.X Runtime](https://dotnet.microsoft.com/en-us/download/dotnet/6.0)) (Select the ".NET Desktop Runtime 6.0.~" for x86)

Alternatively you can add a Task Scheduler script to have it auto start or throw it in your `shell:Startup` folder and get a UAC prompt each startup ;)

This was a quick fix to an annoying problem, I doubt many other people have this issue, due to this reason, there is a lack of documentation to the project.

The popup blocks the mouse from clicking things behind the popup which causes issue during window dragging since Windows File Explorer has the same issue, Hovering Tabs shows their names, "Sort" and "View" and "..." displays their respective popups which block the ability to click and drag the explorer window. This fix addresses the issue.

The fix does not address popups that are larger than 1 line as that may cause unintended issues with other parts of windows.

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
