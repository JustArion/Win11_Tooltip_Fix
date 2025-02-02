namespace Start_TooltipFix;

public static class Errors
{
    public static void Panic(Exception e)
    {
        MessageBox.Show($"An Unexpected error occurred: {e.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        Environment.Exit(1);
    }
}