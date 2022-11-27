namespace PopupHost_ClickThroughPatch;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;
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
        InitializeWinEventHook();
        
        Application.Run(); // The process needs a message loop.
    }

    private static void InitializeConsole()
    {
        var attached = Kernel32.AttachConsole(-1);

        AppDomain.CurrentDomain.UnhandledException += (_, eo) => Trace.TraceError((eo.ExceptionObject as Exception)?.ToString());
        
        Trace.Listeners.Add(new TextWriterTraceListener("PopupHost.log"));
        Trace.AutoFlush = true;

        if (!attached) return;
        
        var stdOut = new StreamWriter(Console.OpenStandardOutput());
        stdOut.AutoFlush = true;
        Console.SetOut(stdOut);
        Trace.Listeners.Add(new ConsoleTraceListener());

        Trace.WriteLine("Attached Console Output to Session");
    }

    #if DEBUG
    private const int _DebugProcessID = 0;
    #endif
    private static void InitializeWinEventHook()
    {
        #if DEBUG
        var hHandle = InitializeOnPID(_DebugProcessID);
        #else
        var hHandle = InitializeOnPID(0);
        #endif

        if (!hHandle.IsInvalid) return;
        MessageBox.Show($"Program Failed with error code '{Marshal.GetLastWin32Error()}'", Application.ProductName);
        Application.Exit();
    }

    private static User32.SafeEventHookHandle InitializeOnPID(int pid) =>
        User32.SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_SHOW,
            User32.WindowsEventHookType.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            WinHookCallback, pid, 0,
            User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);
    
    private static void WinHookCallback(IntPtr hookHandle, User32.WindowsEventHookType @event, IntPtr hwnd, int idobject, int idchild, int dweventthread, uint dwmseventtime)
    {
        try
        {
            if (User32.GetClassName(hwnd) != "Xaml_WindowedPopupClass") return;

            var owner = User32.GetWindow(hwnd, User32.GetWindowCommands.GW_OWNER);
            var ownerClassName = User32.GetClassName(owner);
            
            /// This is the 'Windows Explorer' right click context menu.
            /// It is treated as a Xaml Popup.
            if (ownerClassName is "CabinetWClass" && !IsValidExplorerWindow(hwnd))
                return;

            SetTransparent(hwnd);
        }
        // If the Handle is disposed while we work with it, we don't care.
        catch (Exception e) when (e is Win32Exception) { }
    }
    
    private static readonly CUIAutomationClass _Automation = new();
    private static bool IsValidExplorerWindow(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero) return false;
            var element = _Automation.ElementFromHandle(hwnd);
            if (element == null) return false;

            var popup = element.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);

            var child = popup.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);

            #if DEBUG
            Console.WriteLine($"Type: '{child.CurrentClassName}' - '{child.CurrentName}'");
            #endif
            return child.CurrentClassName == "ToolTip";
        }
        // The user moved over a tooltip so fast that it was disposed before we could get to it.
        catch (NullReferenceException) { return false; }
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
}