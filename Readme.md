### Steps
* Build (Dotnet 6 Required)
* Run As Administrator
* Enjoy.

Alternatively you can add a Task Scheduler script to have it auto start or throw it in your `shell:Startup` folder and get a UAC prompt each startup ;)

This was a quick fix to an annoying problem, I doubt many other people have this issue, due to this reason, there is a lack of documentation to the project.

The popup blocks the mouse from clicking things behind the popup which causes issue during window dragging since Windows File Explorer has the same issue, Hovering Tabs shows their names, "Sort" and "View" and "..." displays their respective popups which block the ability to click and drag the explorer window. This fix addresses the issue.

Picture of a info popup:

<img src="https://cdn.discordapp.com/attachments/883435300880261120/1036285401746386974/1e323618-0573-4f48-99eb-7b311f9c899c_30-10-2022.png"/>
