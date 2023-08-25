namespace Start_Tooltip_Fix;

public static class Strings
{
    private const string QUOTE = "\"{0}\"";
    public static string SurroundQuotes(string s) => string.Format(QUOTE, s);
}