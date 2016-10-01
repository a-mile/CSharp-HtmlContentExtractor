using System.Text.RegularExpressions;

namespace CSharp_HtmlContentExtractor_Library.ExtensionMethods
{
    static class StringExtension
    {
        public static int WordCount(this string str)
        {
            string input = str;
            input = Regex.Replace(input, "\n|\r|\t", " ");
            input = Regex.Replace(input, "[ ]{2,}", " ");
            return input.Split(' ').Length;
        }
    }
}
