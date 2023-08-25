namespace Start_Tooltip_Fix;

using System.Runtime.InteropServices;

public static partial class Interop
{
    [LibraryImport("Shell32.dll", StringMarshalling = StringMarshalling.Utf16)]
    public static partial int ShellExecuteW(IntPtr hwnd, string lpOperation, string lpFile, string lpParameters, string? lpDirectory, int nShowCmd);

    public const int SW_HIDE = 0;
}