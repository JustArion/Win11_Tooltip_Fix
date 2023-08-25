namespace Start_Tooltip_Fix;

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

[SuppressMessage("ReSharper", "SwitchStatementHandlesSomeKnownEnumValuesWithDefault")]
public static class ProcessStartInfoEx
{
    public static ProcessStartInfo WithArgs(this ProcessStartInfo info, LaunchArgs args)
    {
        if (args.SkipPrompts)
            info.ArgumentList.Add(LaunchArgs.SKIP_PROMPTS_ARG);

        switch (args.Hotpath)
        {
            case Hotpath.Install:
                info.ArgumentList.Add(LaunchArgs.INSTALL_ARG);
                break;
            case Hotpath.Uninstall:
                info.ArgumentList.Add(LaunchArgs.UNINSTALL_ARG);
                break;
        }

        return info;
    }
}