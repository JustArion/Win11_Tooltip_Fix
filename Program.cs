namespace PopupHost_ClickThroughPatch;

using System.Diagnostics;
using System.Runtime.InteropServices;
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
        
        if (!attached) return;
        
        var stdOut = new StreamWriter(Console.OpenStandardOutput());
        stdOut.AutoFlush = true;
        Console.SetOut(stdOut);
        
        Console.WriteLine("Attached Console Session");
    }

    private static void InitializeWinEventHook()
    {
        var hHandle = User32.SetWinEventHook(User32.WindowsEventHookType.EVENT_OBJECT_SHOW,
            User32.WindowsEventHookType.EVENT_OBJECT_SHOW, IntPtr.Zero, WinHookCallback, 0, 0,
            User32.WindowsEventHookFlags.WINEVENT_OUTOFCONTEXT);

        if (hHandle.DangerousGetHandle() != IntPtr.Zero) return;
        MessageBox.Show($"Program Failed with error code '{Marshal.GetLastWin32Error()}");
        Application.Exit();
    }

    private static void WinHookCallback(IntPtr hookHandle, User32.WindowsEventHookType @event, IntPtr hwnd, int idobject, int idchild, int dweventthread, uint dwmseventtime)
    {
        try
        {
            if (User32.GetClassName(hwnd) != "Xaml_WindowedPopupClass") return;

            SetTransparent(hwnd);
        }
        catch {} // Some GC racing conditions can cause this to fail
    }

    private static unsafe bool SetTransparent(IntPtr windowHandle)
    {
        var style = (User32.SetWindowLongFlags) User32.GetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE);
        if (style.HasFlag(User32.SetWindowLongFlags.WS_EX_TRANSPARENT) &&
            style.HasFlag(User32.SetWindowLongFlags.WS_EX_LAYERED)) return true;

        var size = default(RECT);
        try
        {
            size = GetWindowSize(windowHandle);
            var height = size.bottom - size.top;
            if (height != 41) return false; // 41 is the absolute height of the popup
        }
        catch 
        { 
            return false; // The window was closed. The element is no longer valid. (Very likely the user moved past the element.
        }
        
        style |= User32.SetWindowLongFlags.WS_EX_TRANSPARENT | User32.SetWindowLongFlags.WS_EX_LAYERED;
        var retVal = User32.SetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE, style);
        
        if (retVal != 0)
        {
            // Process process = null;
            // try
            // {
            //     // Get the PID
            //     User32.GetWindowThreadProcessId(windowHandle, out var pid);
            //     // Get the process
            //     process = Process.GetProcessById(pid);
            // }
            // catch {}
            
            // 1024x768: Width: 252, Height: 41
            // 1920x1080: Width: 252, Height: 41
            // The Popup has an absolute width. So we can look for that.
            // var width = size.right - size.left;
            // var height = size.bottom - size.top;
            // Console.WriteLine($"Window at '{process?.ProcessName}' - {width}x{height}");
            return true;
        }
        
        var error = Marshal.GetLastWin32Error();
        Console.WriteLine("Error setting window style: {0}", new Win32Exception(error));
        return false;
    }
    
    private static bool _IsEmpty(RECT rect) => rect.left == 0 && rect.top == 0 && rect.right == 0 && rect.bottom == 0;

    private static RECT GetWindowSize(IntPtr windowHandle)
    {
        Thread.Sleep(2); // Wait to Initialize
        User32.GetWindowRect(windowHandle, out var rect);
        if (_IsEmpty(rect)) 
            User32.GetClientRect(windowHandle, out rect);
        return rect;
    }

}