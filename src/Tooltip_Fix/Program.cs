using System.ComponentModel;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;
using Serilog;
using Dawn.Apps.Tooltip_Fix.Serilog.CustomEnrichers;
using Interop.UIAutomationClient;
using MessageBox = System.Windows.Forms.MessageBox;

internal static class Program
{
    [STAThread]
    internal static void Main()
    {
        var isWin11 = Environment.OSVersion.Version is { Major: >= 10, Minor: >= 0, Build: >= 22000 };
        if (!isWin11)
        {
            MessageBox.Show("This program is only compatible with Windows 11.", "Tooltip Fix", MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }

        InitializeConsole();
        try
        {

            InitializeWinEventHook();

            RunMessageLoop();
        }
        catch (Exception e)
        {
            Log.Fatal(e, "Fatal Error, terminating application");
        }
        finally
        {
            Log.CloseAndFlush();
        }

    }

    private static void RunMessageLoop()
    {
        while (GetMessage(out var msg) > 0)
        {
            TranslateMessage(msg);
            DispatchMessage(msg);
        }
    }

    private static void InitializeConsole()
    {
        ExtractAppSettingsIfNecessary();
        
        // var loggingConfig = new ConfigurationBuilder()
        //     .SetBasePath(AppContext.BaseDirectory)
        //     .AddJsonFile("appsettings.json", false, true)
        //     .Build();

        /* Prevention of the following exception under Single-File Publish:
         * System.InvalidOperationException: No Serilog:Using configuration section is defined and no Serilog assemblies were found.
            This is most likely because the application is published as single-file.
         */
        // var options = new ConfigurationReaderOptions(
        //     typeof(ConsoleLoggerConfigurationExtensions).Assembly, 
        //     typeof(Serilog.Enrichers.ProcessNameEnricher).Assembly, 
        //     typeof(Serilog.AspNetCore.RequestLoggingOptions).Assembly);

        Log.Logger = new LoggerConfiguration()
            .Enrich.WithClassName()
            .WriteTo.Console(outputTemplate: "{Level:u1} {Timestamp:yyyy-MM-dd HH:mm:ss.ffffff} [{Source}] {Message:lj}{NewLine}{Exception}")
            .Enrich.WithProcessName()
            .Enrich.FromLogContext()
            .WriteTo.Seq("http://localhost:9999")
            .CreateLogger();

        var attached = AttachConsole(ATTACH_PARENT_PROCESS);
        
        AppDomain.CurrentDomain.UnhandledException += (_, eo) => Log.Error(eo.ExceptionObject as Exception, "Unhandled Exception");
        
        if (!attached)
        {
            // On Some PCs a shadow console is created. We need to free it.
            FreeConsole();
            return;
        }

        Log.Information("Tooltip Fix Initialized");
    }

    private static void ExtractAppSettingsIfNecessary()
    {
        var appSettingsPath = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
        if (File.Exists(appSettingsPath) && JsonSerializer.Deserialize<object>(File.ReadAllText(appSettingsPath)) is not null)
            return;
        
        // Extract is from embedded resources
        
        var assembly = Assembly.GetExecutingAssembly();
        
        var resourceNames = assembly.GetManifestResourceNames();
        
        var resourcePath = resourceNames.FirstOrDefault(x => x.EndsWith("appsettings.json"));
        
        if (resourcePath is null)
            throw new InvalidOperationException("appsettings.json was not found in the embedded resources");
        
        using var stream = assembly.GetManifestResourceStream(resourcePath);
        
        if (stream is null)
            throw new InvalidOperationException("appsettings.json was not found in the embedded resources");
        
        using var reader = new StreamReader(stream);
        
        File.WriteAllText(appSettingsPath, reader.ReadToEnd());
    }

#if DEBUG
    private const int _DebugProcessID = 0;
    #endif
    private static HWINEVENTHOOK _hHook;
    private static void InitializeWinEventHook()
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
        Log.Fatal(GetLastError().GetException(), "Failed to initialize WinEventHook");
        Environment.Exit(1);
    }
    
    private static HWINEVENTHOOK InitializeOnPID(uint pid) =>
        SetWinEventHook(EventConstants.EVENT_OBJECT_SHOW,
            EventConstants.EVENT_OBJECT_SHOW, 
            IntPtr.Zero, 
            Callback, pid, 0,
            WINEVENT.WINEVENT_OUTOFCONTEXT);


    private static WinEventProc Callback;
    private const int _Xaml_WindowedPopupClass_StringLength = 23;
    private static readonly StringBuilder _StringBuilder = new(_Xaml_WindowedPopupClass_StringLength + 1); // Xaml_WindowedPopupClass + Null Terminator
    private static void WinHookCallback(HWINEVENTHOOK hWinEventHook, uint winEvent, HWND hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
    {
        try
        {
            try
            {
                var classNameLength = GetClassName(hwnd, _StringBuilder, _StringBuilder.Capacity);
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

            //                                Xaml_WindowedPopupClass         PopupHost                   Popup                       Popup
            Log.Logger.Debug("'{CurrentElementClassName}' - '{CurrentElementName}' - '{CurrentPopupClassName}' - '{CurrentPopupName}'", 
                element.CurrentClassName, element.CurrentName, popup.CurrentClassName, popup.CurrentName);
            
            var child = popup.FindFirst(TreeScope.TreeScope_Children, _Automation.ControlViewCondition);
            if (child is null)
                return false;

            //                                  ToolTip            {ToolTipName}
            Log.Logger.Debug("Type: '{CurrentClassName}' - '{CurrentName}'", 
                child.CurrentClassName, child.CurrentName);

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
    private static bool IsTransparent(WindowStylesEx style) => style.HasFlag(WindowStylesEx.WS_EX_TRANSPARENT) && style.HasFlag(WindowStylesEx.WS_EX_LAYERED);

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static void SetTransparent(HWND hwnd, WindowStylesEx style)
    {

        style |= WindowStylesEx.WS_EX_TRANSPARENT | WindowStylesEx.WS_EX_LAYERED;
        
        var retVal = SetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE, (int)style);
        
        if (retVal != 0) 
            return;

        Log.Logger.Error(new Win32Exception(Marshal.GetLastWin32Error()), "Error setting window style");
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining | MethodImplOptions.AggressiveOptimization)]
    private static WindowStylesEx GetWindowFlags(HWND hwnd) => (WindowStylesEx)GetWindowLong(hwnd, WindowLongFlags.GWL_EXSTYLE);
}