namespace Start_TooltipFix;

using System.Runtime.InteropServices;

public static partial class Interop
{
    [LibraryImport("Kernel32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static partial bool FreeConsole();
}