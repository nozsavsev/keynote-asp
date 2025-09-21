using System.Text.RegularExpressions;

namespace keynote_asp.Helpers
{
    public static class StringExtensions
    {
        public static string SplitPascalCase(this string input)
        {
            return Regex.Replace(input, "([A-Z])", " $1").Trim();
        }

        public static string RemovePrefixAndSplitCamelCase(this string input, string prefix)
        {
            if (input.StartsWith(prefix))
            {
                input = input.Substring(prefix.Length);
            }
            return Regex.Replace(input, "(?<!^)([A-Z])", " $1");
        }
    }
}
