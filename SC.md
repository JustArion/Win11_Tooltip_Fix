### SC - Service Control Manager

#### What is being ran?
`Info` 
For Installation and Uninstallation the program asks Windows to register it as a "Service", A "Service" is a program that almost always runs in the background; perfect for what we need it to do.

`What commands are being ran?`
For installs: 
`sc create "Tooltip Fix Service" binPath=.../Service/Tooltip_Fix.exe start=auto`
(Registers our service and tells Windows what to auto-start when you log in)

and
`sc start "Tooltip Fix Service"`
(Starts the program as a service)

For uninstalls:
`sc stop "Tooltip Fix Service"`
(Stops the program from running)

and
`sc delete "Tooltip Fix Service"`
(Unregisters our service)

#### Why two admin prompts each time?

When someone runs something as admin you can click a little `Show more details` button which allows you to see exactly whats being ran as admin.

#### What if I want to run this myself?

Sure thing! Open up `Command Prompt` or `Powershell` as Admin and run the following commands (Note you'll need to get the absolute path of the Tooltip_Fix.exe for example: C:/Users/MYUSERNAME/Downloads/Tooltip_Fix/Service/Tooltip_Fix.exe)

**Install:** *(Needs Admin & Absolute Path)*
```cmd
sc create "Tooltip Fix Service" binPath=ABSOLUTE_PATH start=auto
sc start "Tooltip Fix Service"
```
**Uninstall:** *(Needs Admin)*
```cmd
sc stop "Tooltip Fix Service"
sc delete "Tooltip Fix Service"
```