// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexReplaceStringTests
{
    [Fact]
    public static void RegexReplaceStringTestCase0()
    {
        string input = "08/10/99 16:00";
        Regex regex = new Regex(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)");
        Match match = regex.Match(input);
        string result = regex.Match(input).Result("${time}");

        Assert.Equal("16:00", result);

        result = regex.Match(input).Result("$1");
        Assert.Equal("08", result);

        result = regex.Match(input).Result("$2");
        Assert.Equal("10", result);

        result = regex.Match(input).Result("$3");
        Assert.Equal("99", result);

        // Regex.Replace()
        input = "08/10/99 16:00";
        result = Regex.Replace(input, @"[^ ]+\s(?<time>)", "${time}");
        Assert.Equal("16:00", result);
        Assert.Equal("08/10/99 16:00", input);

        input = "MiCrOsOfT";
        result = Regex.Replace(input, "icrosoft", "icrosoft", RegexOptions.IgnoreCase);
        Assert.Equal("Microsoft", result);

        input = "my dog has fleas";
        result = Regex.Replace(input, "dog", "CAT", RegexOptions.IgnoreCase);
        Assert.Equal("my CAT has fleas", result);

        input = "D.Bau";
        regex = new Regex(@"D\.(.+)");
        result = regex.Replace(input, "David $1");
        Assert.Equal("David Bau", result);

        input = "aaaaa";
        regex = new Regex("a");
        result = regex.Replace(input, "b", 2);
        Assert.Equal("bbaaa", result);

        result = regex.Replace(input, "b", 2, 3);
        Assert.Equal("aaabb", result);
        
        // Replace with MatchEvaluators - relevant fn's for this are defined below
        result = Regex.Replace("Big mountain", "(Big|Small)", new MatchEvaluator(Rep1));
        Assert.Equal("Huge mountain", result);

        result = Regex.Replace("Small village", "(Big|Small)", new MatchEvaluator(Rep1));
        Assert.Equal("Tiny village", result);

        if ("i".ToUpper() == "I")
        {
            result = Regex.Replace("bIG horse", "(Big|Small)", new MatchEvaluator(Rep1), RegexOptions.IgnoreCase);
            Assert.Equal("Huge horse", result);
        }

        result = Regex.Replace("sMaLl dog", "(Big|Small)", new MatchEvaluator(Rep1), RegexOptions.IgnoreCase);
        Assert.Equal("Tiny dog", result);

        regex = new Regex(".+");
        result = regex.Replace("XSP_TEST_FAILURE", new MatchEvaluator(Rep2));
        Assert.Equal("SUCCESS", result);

        regex = new Regex("[abcabc]");
        result = regex.Replace("abcabc", new MatchEvaluator(Rep3));
        Assert.Equal("ABCABC", result);

        regex = new Regex("[abcabc]");
        result = regex.Replace("abcabc", new MatchEvaluator(Rep3), 3);
        Assert.Equal("ABCabc", result);

        regex = new Regex("[abcabc]");
        result = regex.Replace("abcabc", new MatchEvaluator(Rep3), 3, 2);
        Assert.Equal("abCABc", result);

        // Replace with group numbers
        regex = new Regex("([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))");
        result = regex.Replace("abcdefghiklmnop", "$15");
        Assert.Equal("p", result);

        result = regex.Replace("abcdefghiklmnop", "$3");
        Assert.Equal("cdefghiklmnop", result);

        // Stress
        string pattern = string.Empty;
        for (int i = 0; i < 1000; i++)
            pattern += "([a-z]";
        for (int i = 0; i < 1000; i++)
            pattern += ")";
        input = string.Empty;
        for (int i = 0; i < 200; i++)
            input += "abcde";
        regex = new Regex(pattern);
        result = regex.Replace(input, "$1000");
        Assert.Equal("e", result);

        result = regex.Replace(input, "$1");
        string sExp = string.Empty;
        for (int i = 0; i < 200; i++)
            sExp += "abcde";
        Assert.Equal(sExp, result);

        // Undefined group
        result = Regex.Replace("abc", "([a_z])(.+)", "$3");
        Assert.Equal("$3", result);

        // Regression test:
        // Regex treating Devanagari matra characters as matching "\b"
        // Unicode characters in the "Mark, NonSpacing" Category, U+0902=Devanagari sign anusvara, U+0947=Devanagri vowel sign E
        Regex regexRendering = new Regex(@"\u0915\u0930.*?\b", RegexOptions.CultureInvariant | RegexOptions.Singleline);
        string strBoldedText = regexRendering.Replace("\u092f\u0939 \u0915\u0930 \u0935\u0939 \u0915\u0930\u0947\u0902 \u0939\u0948\u0964", RepBold);
        string expectedBoldedText = "\u092f\u0939 <b>\u0915\u0930</b> \u0935\u0939 <b>\u0915\u0930\u0947\u0902</b> \u0939\u0948\u0964";
        Assert.Equal(expectedBoldedText, strBoldedText);
    }

    public static string Rep1(Match input)
    {
        if (string.Compare(input.ToString(), "Big", StringComparison.CurrentCultureIgnoreCase) == 0)
            return "Huge";
        else
            return "Tiny";
    }

    public static string Rep2(Match input)
    {
        return "SUCCESS";
    }

    public static string Rep3(Match input)
    {
        if (input.Value.Equals("a"))
            return "A";
        if (input.Value.Equals("b"))
            return "B";
        if (input.Value.Equals("c"))
            return "C";
        return string.Empty;
    }

    public static string RepBold(Match m)
    {
        return string.Format("<b>{0}</b>", m.Value);
    }

    private static string GetUnicodeString(string str)
    {
        StringBuilder buffer = new StringBuilder();
        for (int i = 0; i < str.Length; i++)
        {
            if (str[i] < 0x20)
            {
                buffer.Append("\\u" + ((int)str[i]).ToString("x4"));
            }
            else if (str[i] < 0x7f)
            {
                buffer.Append(str[i]);
            }
            else
            {
                buffer.Append("\\u" + ((int)str[i]).ToString("x4"));
            }
        }

        return buffer.ToString();
    }
}
