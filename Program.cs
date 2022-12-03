namespace PopupHost_ClickThroughPatch;

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Interop.UIAutomationClient;
using PInvoke;

internal static class Program
{
    /// <summary>
    ///  The main entry point for the application.
    /// </summary>
    [STAThread]
    static void Main()
    {
        
        var isWin11 = Environment.OSVersion.Version is { Major: >= 10, Minor: >= 0, Build: >= 22000 };
        if (!isWin11)
        {
            MessageBox.Show($"The program is designed to run on Windows 11 or later.", Application.ProductName);
            return;
        }
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
        
        Callback = WinHookCallback;
        
        /// If we pass 'WinHookCallback' as an implicit cast to a delegate, the GC may in some cases may collect it.
        /// So we need to keep a reference to it.
        GC.KeepAlive(Callback);

        #if DEBUG
        var hHandle = InitializeOnPID(_DebugProcessID);
        #else
        var hHandle = InitializeOnPID(0);
        #endif

        if (!hHandle.IsInvalid)
            return;
        MessageBox.Show($"Program Failed with error code '{Marshal.GetLastWin32Error()}'", Application.ProductName);
        Application.Exit();
    }

    private static User32.SafeEventHookHandle InitializeOnPID(int pid) =>
        User32.SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_SHOW,
            User32.WindowsEventHookType.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            Callback, pid, 0,
            User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);


    private static User32.WinEventProc Callback;
    private static void WinHookCallback(IntPtr hookHandle, User32.WindowsEventHookType @event, IntPtr hwnd, int idobject, int idchild, int dweventthread, uint dwmseventtime)
    {
        try
        {
            if (User32.GetClassName(hwnd) is not "Xaml_WindowedPopupClass") 
                return;
            
            if (!IsTooltip(hwnd))
                return;
            

            SetTransparent(hwnd);
        }
        // If the Handle is disposed while we work with it, we don't care.
        catch (Win32Exception) { }
        catch (Exception e) { Trace.TraceError(e.ToString()); }
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

    private static readonly CUIAutomationClass _Automation = new();
    
    /// <code>
    /// This is the general structure we look for.
    ///     - Xaml_WindowedPopupClass   (hwnd)
    ///         - Popup
    ///             - Tooltip
    ///                 - TextBlock
    /// </code>
    private static bool IsTooltip(IntPtr hwnd)
    {
        try
        {
            if (hwnd == IntPtr.Zero) return false;
            var element = _Automation.ElementFromHandle(hwnd);
            if (element is not { CurrentFrameworkId: "XAML" }) return false;

            var popup = element.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);

            var child = popup?.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);
            if (child is null)
                return false;

            #if DEBUG
            Console.WriteLine($"Type: '{child.CurrentClassName}' - '{child.CurrentName}'");
            #endif
            return child.CurrentClassName == "ToolTip";
        }
        // The user moved over a tooltip so fast that it was disposed before we could access all its information.
        catch (NullReferenceException) {}
        catch (COMException e)
        {
            // An event was unable to invoke any of the subscribers (0x80040201)
            // UIA_E_ELEMENTNOTAVAILABLE
            // The element is ( not / no longer ) available on the UI Automation tree.
            // The error wouldn't count as a NullRef as the error occurs when we call '_Automation.ElementFromhandle(hwnd);' The method will throw a COM Exception
            // since from the time we got the handle to the time we called the method, the handle was disposed.
            if (e.ErrorCode is not -0x7FFBFDFF)
                HandleError(e);
        }
        catch (Exception e) { HandleError(e); }
        return false;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void HandleError(Exception e) => Trace.TraceError(e?.ToString());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool HasTransparentFlags(User32.SetWindowLongFlags style) => style.HasFlag(User32.SetWindowLongFlags.WS_EX_TRANSPARENT) && style.HasFlag(User32.SetWindowLongFlags.WS_EX_LAYERED);
    
    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparentFlags(ref User32.SetWindowLongFlags style) => style |= User32.SetWindowLongFlags.WS_EX_TRANSPARENT | User32.SetWindowLongFlags.WS_EX_LAYERED;
}