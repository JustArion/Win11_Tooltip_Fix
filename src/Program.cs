namespace Dawn.Patches.PopupHost_ClickThrough;

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Interop.UIAutomationClient;
using Vanara.PInvoke;

internal static class Program
{
    private const string ProductName = "PopupHost Clickthrough Patch";
    
    [STAThread]
    internal static void Main()
    {
        
        var isWin11 = Environment.OSVersion.Version is { Major: >= 10, Minor: >= 0, Build: >= 22000 };
        if (!isWin11)
        {
            User32.MessageBox(IntPtr.Zero, "The program is designed to run on Windows 11 or later.", ProductName, User32.MB_FLAGS.MB_OK);
            return;
        }
        InitializeConsole();
        InitializeWinEventHook();
        
        // The process needs a message loop.
        MessageLoop();
    }
    
    private static void MessageLoop()
    {
        while (User32.GetMessage(out var msg) > 0)
        {
            User32.TranslateMessage(msg);
            User32.DispatchMessage(msg);
        }
    }

    private static void InitializeConsole()
    {
        var attached = Kernel32.AttachConsole(Kernel32.ATTACH_PARENT_PROCESS);
        
        AppDomain.CurrentDomain.UnhandledException += (_, eo) => Trace.TraceError((eo.ExceptionObject as Exception)?.ToString());
        
        Trace.Listeners.Add(new TextWriterTraceListener("PopupHost.log"));
        Trace.AutoFlush = true;


        Trace.Listeners.Add(new ConsoleTraceListener());

        if (!attached)
        {
            // On Some PCs a shadow console is created. We need to free it.
            Kernel32.FreeConsole();
            return;
        }

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

        if (!hHandle.IsNull)
            return;
        User32.MessageBox(IntPtr.Zero, $"Program Failed with error code '{Marshal.GetLastWin32Error()}'", ProductName, User32.MB_FLAGS.MB_OK);
        Environment.Exit(1);
    }
    
    private static User32.HWINEVENTHOOK InitializeOnPID(uint pid) =>
        User32.SetWinEventHook(User32.EventConstants.EVENT_OBJECT_SHOW,
            User32.EventConstants.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            Callback, pid, 0,
            User32.WINEVENT.WINEVENT_OUTOFCONTEXT);


    private static User32.WinEventProc Callback;
    private const int _Xaml_WindowedPopupClass_StringLength = 23;
    private static readonly StringBuilder _StringBuilder = new(_Xaml_WindowedPopupClass_StringLength + 1); // Xaml_WindowedPopupClass + \0
    private static void WinHookCallback(User32.HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            try
            {
                var classNameLength = User32.GetClassName(hwnd, _StringBuilder, _StringBuilder.Capacity);
                if (classNameLength != _Xaml_WindowedPopupClass_StringLength)
                    return; // We save some time by comparing the length first. The majority of windows are not tooltips.
                
                if (_StringBuilder.ToString() is not "Xaml_WindowedPopupClass") 
                    return;
            }
            finally
            {
                _StringBuilder.Clear();
            }

            if (!IsTooltip(hwnd))
                return;

            var windowInfo = GetWindowFlags(hwnd);
            if (IsTransparent(windowInfo))
                return;
            
            SetTransparent(hwnd, windowInfo);
        }
        // If the Handle is disposed while we work with it, we don't care.
        catch (Win32Exception) { }
        catch (Exception e) { Trace.TraceError(e.ToString()); }
    }

    private static readonly CUIAutomationClass _Automation = new();
    
    /// <code>
    /// This is the general structure we look for.
    ///     - Xaml_WindowedPopupClass   (hwnd)
    ///         - Popup
    ///             - Tooltip
    ///                 - TextBlock
    /// </code>
    private static bool IsTooltip(HWND hwnd)
    {
        try
        {
            if (hwnd.IsNull) return false;
            var element = _Automation.ElementFromHandle(hwnd.DangerousGetHandle());
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
    private static bool IsTransparent(User32.WindowStylesEx style) => style.HasFlag(User32.WindowStylesEx.WS_EX_TRANSPARENT) && style.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparent(HWND hwnd, User32.WindowStylesEx style)
    {

        style |= User32.WindowStylesEx.WS_EX_TRANSPARENT | User32.WindowStylesEx.WS_EX_LAYERED;
        
        var retVal = User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (int)style);
        
        if (retVal != 0) 
            return;

        Trace.TraceError($"Error setting window style: {new Win32Exception(Marshal.GetLastWin32Error())}");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static User32.WindowStylesEx GetWindowFlags(HWND hwnd) => (User32.WindowStylesEx)User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);
}