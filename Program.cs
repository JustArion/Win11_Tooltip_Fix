namespace PopupHost_ClickThroughPatch;

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

    private static bool SetTransparent(IntPtr windowHandle)
    {
        var style = (User32.SetWindowLongFlags) User32.GetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE);
        if (style.HasFlag(User32.SetWindowLongFlags.WS_EX_TRANSPARENT) &&
            style.HasFlag(User32.SetWindowLongFlags.WS_EX_LAYERED)) return true;
        
        style |= User32.SetWindowLongFlags.WS_EX_TRANSPARENT | User32.SetWindowLongFlags.WS_EX_LAYERED;
        var retVal = User32.SetWindowLong(windowHandle, User32.WindowLongIndexFlags.GWL_EXSTYLE, style);

        if (retVal != 0)
        {
            Console.WriteLine($"Window at '{windowHandle}' Set Transparent!");
            return true;
        }
        
        var error = Marshal.GetLastWin32Error();
        Console.WriteLine("Error setting window style: {0}", new Win32Exception(error));
        return false;
    }

    private static async Task InitializePollingLoop()
    {
        while (true)
        {
            await Task.Delay(100);

            var hWnd = User32.FindWindow("Xaml_WindowedPopupClass", null);
            
            if (hWnd == IntPtr.Zero) continue;

            Console.WriteLine("Found a window at {0}", hWnd);
            
            var style = User32.GetWindowLong(hWnd, User32.WindowLongIndexFlags.GWL_EXSTYLE);
            style |= (int)User32.SetWindowLongFlags.WS_EX_TRANSPARENT | (int)User32.SetWindowLongFlags.WS_EX_LAYERED;
            var retVal = User32.SetWindowLong(hWnd, User32.WindowLongIndexFlags.GWL_EXSTYLE, (User32.SetWindowLongFlags)style);
            if (retVal == 0)
            {
                var error = Marshal.GetLastWin32Error();
                Console.WriteLine("Error setting window style: {0}", new Win32Exception(error));
            }

        }
    }
}