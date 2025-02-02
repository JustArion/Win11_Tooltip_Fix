namespace Start_TooltipFix;

public sealed class LaunchArgs
{
    public LaunchArgs(string[] args)
    {
        RawArgs = args;

        SkipPrompts = RawArgs.Contains(SKIP_PROMPTS_ARG);
        
        Hotpath = RawArgs.Contains(INSTALL_ARG) ? Start_TooltipFix.Hotpath.Install : RawArgs.Contains(UNINSTALL_ARG) ? Start_TooltipFix.Hotpath.Uninstall : null;
    }

    public LaunchArgs()
    {
        RawArgs = Environment.GetCommandLineArgs();
    }
    public string[] RawArgs { get; }
    public bool SkipPrompts { get; init; }
    
    public Hotpath? Hotpath { get; init; }
    
    public const string SKIP_PROMPTS_ARG = "--skip-prompts";
    public const string INSTALL_ARG = "--install";
    public const string UNINSTALL_ARG = "--uninstall";
}

public enum Hotpath
{
    Install,
    Uninstall,
}