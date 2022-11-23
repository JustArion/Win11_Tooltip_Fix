namespace PopupHost_ClickThroughPatch;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.VisualBasic.Logging;
using Newtonsoft.Json;
using PInvoke;

static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        InitializeConsole();

        // Task.Run(InitializePollingLoop);
        InitializeWinEventHook();
        
        Application.Run(); // The process needs a message loop.
    }

    private static void InitializeConsole()
    {
        var attached = Kernel32.AttachConsole(-1);

        AppDomain.CurrentDomain.UnhandledException += (_, eo) => Trace.TraceError((eo.ExceptionObject as Exception)?.ToString());
        

        Trace.Listeners.Add(new FileLogTraceListener 
        { 
            Name = "PopupHost.log", 
            Location = LogFileLocation.ExecutableDirectory // For some reason this is now not the default?
        });
        Trace.AutoFlush = true;

        if (!attached) return;
        
        var stdOut = new StreamWriter(Console.OpenStandardOutput());
        stdOut.AutoFlush = true;
        Console.SetOut(stdOut);
        Trace.Listeners.Add(new ConsoleTraceListener());

        Trace.WriteLine("Attached Console Session");
    }

    private static void InitializeWinEventHook()
    {
        var hHandle = User32.SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_SHOW,
            User32.WindowsEventHookType.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            WinHookCallback, 0, 0,
            User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);

        if (hHandle.DangerousGetHandle() != IntPtr.Zero) return;
        MessageBox.Show($"Program Failed with error code '{Marshal.GetLastWin32Error()}'", Application.ProductName);
        Application.Exit();
    }

    #if DEBUG
    private const int _DebugProcessID = 9900;
    #endif
    private static void WinHookCallback(IntPtr hookHandle, User32.WindowsEventHookType @event, IntPtr hwnd, int idobject, int idchild, int dweventthread, uint dwmseventtime)
    {
        try
        {
            if (User32.GetClassName(hwnd) != "Xaml_WindowedPopupClass") return;

            // if (ProcessIDFromWindowHandle(hwnd) != 9900) return;

            var owner = User32.GetWindow(hwnd, User32.GetWindowCommands.GW_OWNER);
            var ownerClassName = User32.GetClassName(owner);
            
            /// This is the 'Windows Explorer' right click context menu.
            /// It is treated as a Xaml Popup.
            if (ownerClassName is "CabinetWClass")
                return;

            #if DEBUG
            var info = new Dictionary<string, object>()
            {
                {"Process", ProcessNameFromWindowHandle(hwnd)},
                {"ClassName", User32.GetClassName(hwnd)},
                {"Text", User32.GetWindowText(hwnd) },
                {"Owner", new Dictionary<string, object>
                {
                    {"ClassName", ownerClassName},
                    {"Text", User32.GetWindowText(owner)}
                }}
                
            };
            Trace.WriteLine(JsonConvert.SerializeObject(info, Formatting.Indented));
            #endif

            SetTransparent(hwnd);
        }
        catch {} // If the Handle is disposed while we work with it, we don't care.
    }

    private static void SetTransparent(IntPtr windowHandle)
    {
        var style = (User32.SetWindowLongFlags) User32.GetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE);
        if (HasTransparentFlags(style)) return;

        SetTransparentFlags(ref style);
        var retVal = User32.SetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE, style);
        
        if (retVal != 0) return;

        Trace.TraceError($"Error setting window style: {new Win32Exception(Marshal.GetLastWin32Error())}");
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasTransparentFlags(User32.SetWindowLongFlags style) => style.HasFlag(User32.SetWindowLongFlags.WS_EX_TRANSPARENT) && style.HasFlag(User32.SetWindowLongFlags.WS_EX_LAYERED);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparentFlags(ref User32.SetWindowLongFlags style) => style |= User32.SetWindowLongFlags.WS_EX_TRANSPARENT | User32.SetWindowLongFlags.WS_EX_LAYERED;
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static string ProcessNameFromWindowHandle(IntPtr hwnd, bool pidOnFail = true)
    {
        _ = User32.GetWindowThreadProcessId(hwnd, out var pid);
        try
        {
            return Process.GetProcessById(pid).ProcessName;
        }
        catch
        {
            if (pidOnFail) 
                return pid.ToString();
            throw;
        }
    }

}