#nullable enable
namespace Dawn.Apps.Tooltip_Fix;

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using global::Serilog;
using Interop.UIAutomationClient;
using Vanara.PInvoke;

internal class MessageRunner : IDisposable
{
    private const string ProductName = "Tooltip Fix";

    private ApplicationContext? ctx;

    internal MessageRunner() => new Thread(ExecuteMessageLoop).Start();

    /// <summary>
    ///  A bit different than what we did before, but we needed to throw this onto a separate thread to avoid blocking the main thread, the thread starts and exits gracefully.
    ///  Blocking the main thread would lead to Service Stop and Start events not being fired / delayed.
    /// </summary>
    private void ExecuteMessageLoop()
    {
        
        Log.Logger.Verbose("Starting Message Loop");
        
        ctx = new();
        ctx.ThreadExit += (_, _) => Log.Logger.Verbose("Stopping Message Loop");
        InitializeWinEventHook();
        
        Application.Run(ctx);
    }
    
    public void EnsureDisposed()
    {
        ctx?.ExitThread();
        if (_hHook.IsNull)
            return;
        
        Log.Logger.Verbose("Disposing of the WinEventHook");
        Dispose();
    }
    public void Dispose() => User32.UnhookWinEvent(_hHook);

    #if DEBUG
    private const int _DebugProcessID = 0;
    #endif
    private User32.HWINEVENTHOOK _hHook;
    private void InitializeWinEventHook()
    {
        
        Callback ??= WinHookCallback;
        
        /// If we pass 'WinHookCallback' as an implicit cast to a delegate, the GC may in some cases may collect it.
        /// So we need to keep a reference to it.
        GC.KeepAlive(Callback);

        #if DEBUG
        _hHook = InitializeOnPID(_DebugProcessID);
        #else
        _hHook = InitializeOnPID(0);
        #endif

        if (!_hHook.IsNull)
            return;
        User32.MessageBox(IntPtr.Zero, $"Program Failed with error code '{Marshal.GetLastWin32Error()}'", ProductName);
        Environment.Exit(1);
    }
    
    private static User32.HWINEVENTHOOK InitializeOnPID(uint pid) =>
        User32.SetWinEventHook(User32.EventConstants.EVENT_OBJECT_SHOW,
            User32.EventConstants.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            Callback, pid, 0,
            User32.WINEVENT.WINEVENT_OUTOFCONTEXT);


    private static User32.WinEventProc? Callback;
    private const int _Xaml_WindowedPopupClass_StringLength = 23;
    private static readonly StringBuilder _StringBuilder = new(_Xaml_WindowedPopupClass_StringLength + 1); // Xaml_WindowedPopupClass + Null Terminator
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
        catch (Exception e) { Log.Logger.Error(e, "Unknown Error"); }
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
            if (popup is null)
                return false;

            #if DEBUG
            //                                Xaml_WindowedPopupClass         PopupHost                   Popup                       Popup
            Log.Logger.Debug("'{CurrentElementClassName}' - '{CurrentElementName}' - '{CurrentPopupClassName}' - '{CurrentPopupName}'", 
                element.CurrentClassName, element.CurrentName, popup.CurrentClassName, popup.CurrentName);
            #endif
            
            var child = popup.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);
            if (child is null)
                return false;

            #if DEBUG
            //                                  ToolTip            {ToolTipName}
            Log.Logger.Debug("Type: '{CurrentClassName}' - '{CurrentName}'", 
                child.CurrentClassName, child.CurrentName);

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
    private static void HandleError(Exception e) => Log.Logger.Error(e, "Tooltip Error Handler");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsTransparent(User32.WindowStylesEx style) => style.HasFlag(User32.WindowStylesEx.WS_EX_TRANSPARENT) && style.HasFlag(User32.WindowStylesEx.WS_EX_LAYERED);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparent(HWND hwnd, User32.WindowStylesEx style)
    {

        style |= User32.WindowStylesEx.WS_EX_TRANSPARENT | User32.WindowStylesEx.WS_EX_LAYERED;
        
        var retVal = User32.SetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE, (int)style);
        
        if (retVal != 0) 
            return;

        Log.Logger.Error(new Win32Exception(Marshal.GetLastWin32Error()), "Error setting window style");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static User32.WindowStylesEx GetWindowFlags(HWND hwnd) => (User32.WindowStylesEx)User32.GetWindowLong(hwnd, User32.WindowLongFlags.GWL_EXSTYLE);
}