namespace Start_Tooltip_Fix;

using Implementations;

public static class Errors
{
    public const int UNAUTHORIZED = 5;
    public const int DOES_NOT_EXIST = 1060;
    public const int SUCCESS = 0;

    public static void ThrowUnauthorizedError(string serviceName)
    {
        MessageBox.Show("You need to run this program as Administrator", serviceName, MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(UNAUTHORIZED);
    }

    public static void Panic(Exception e)
    {
        MessageBox.Show($"An Unexpected error occurred: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(1);
    }
}