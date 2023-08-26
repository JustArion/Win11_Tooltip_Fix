namespace Start_Tooltip_Fix.Implementations;

using System.Text;
using CliWrap;
using Contracts;

[RequireAdmin(RequireAdminType.Admin)]
[Obsolete("This installer type only works with the ASP version of Tooltip_Fix")]
public class CLI_FixInstaller : IFixInstaller
{
    
    public bool IsInstalled(FixConfigureOptions options)
    {
        var outPipe = new StringBuilder();
        var result = Cli.Wrap("sc")
            .WithArguments(new[]
            {
                "query",
                Strings.SurroundQuotes(options.Name),
            })
            .WithValidation(CommandResultValidation.None)
            .WithStandardOutputPipe(PipeTarget.ToStringBuilder(outPipe))
            .ExecuteAsync().GetAwaiter().GetResult();
        
        return !outPipe.ToString().Contains(Errors.DOES_NOT_EXIST.ToString()) && result.ExitCode != Errors.DOES_NOT_EXIST;
    }
    public void InstallService(FixConfigureOptions options)
    {
        try
        {
            var createResult = Cli.Wrap("sc")
                .WithArguments(new[]
                {
                    "create",
                    Strings.SurroundQuotes(options.Name),
                    "start= auto",
                    "type= own|interact"
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync().GetAwaiter().GetResult();
            
            if (createResult.ExitCode == Errors.UNAUTHORIZED)
                Errors.ThrowUnauthorizedError(options.Name);

            var startResult = Cli.Wrap("sc")
                .WithArguments(new[]
                {
                    "start",
                    Strings.SurroundQuotes(options.Name),
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync().GetAwaiter().GetResult();
            
            if (startResult.ExitCode == Errors.UNAUTHORIZED)
                Errors.ThrowUnauthorizedError(options.Name);
            
            if (startResult.ExitCode != Errors.SUCCESS)
                throw new Exception($"Failed to start service {options.Name}. Exit code: {startResult.ExitCode}");
            
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, options.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }

    public void UninstallService(FixConfigureOptions options)
    {
        try
        {
            var stopResult = Cli.Wrap("sc")
                .WithArguments(new[]
                {
                    "stop",
                    Strings.SurroundQuotes(options.Name),
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync().GetAwaiter().GetResult();
            
            if (stopResult.ExitCode == Errors.UNAUTHORIZED)
                Errors.ThrowUnauthorizedError(options.Name);

            var deleteResult = Cli.Wrap("sc")
                .WithArguments(new[]
                {
                    "delete",
                    Strings.SurroundQuotes(options.Name),
                })
                .WithValidation(CommandResultValidation.None)
                .ExecuteAsync().GetAwaiter().GetResult();
            
            if (deleteResult.ExitCode == Errors.UNAUTHORIZED)
                Errors.ThrowUnauthorizedError(options.Name);
            
            if (deleteResult.ExitCode != Errors.SUCCESS)
                throw new Exception($"Failed to delete service {options.Name}. Exit code: {deleteResult.ExitCode}");
        }
        catch (Exception e)
        {
            MessageBox.Show(e.Message, options.Name, MessageBoxButtons.OK, MessageBoxIcon.Error);
            Environment.Exit(1);
        }
    }
}