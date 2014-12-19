// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Text;
using System.Text.RegularExpressions;
using Xunit;

public partial class RegexReplaceStringTests
{
    [Fact]
    public static void RegexReplaceStringTestCase0()
    {
        //////////// Global Variables used for all tests
        String strLoc = "Loc_000oo";
        String strValue = String.Empty;
        int iCountErrors = 0;
        int iCountTestcases = 0;
        Regex r;
        String s;
        String sResult;
        Int32 i;
        String pattern;
        String sExp;
        Match match;
        try
        {
            /////////////////////////  START TESTS ////////////////////////////
            ///////////////////////////////////////////////////////////////////
            // [] Group name checks
            //-----------------------------------------------------------------
            strLoc = "Loc_498yg";
            iCountTestcases++;
            s = "08/10/99 16:00";
            r = new Regex(@"(?<1>\d{1,2})/(?<2>\d{1,2})/(?<3>\d{2,4})\s(?<time>\S+)");
            match = r.Match(s);
            sResult = r.Match(s).Result("${time}");
            if (!sResult.Equals("16:00"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match, " + sResult);
            }

            sResult = r.Match(s).Result("$1");
            if (!sResult.Equals("08"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match, " + sResult);
            }

            sResult = r.Match(s).Result("$2");
            if (!sResult.Equals("10"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match, " + sResult);
            }

            sResult = r.Match(s).Result("$3");
            if (!sResult.Equals("99"))
            {
                iCountErrors++;
                Console.WriteLine("Err_234fsadg! doesnot match, " + sResult);
            }

            // [] Regex.Replace()
            //-----------------------------------------------------------------
            strLoc = "Loc_298vy";
            iCountTestcases++;
            s = "08/10/99 16:00";
            sResult = Regex.Replace(s, @"[^ ]+\s(?<time>)", "${time}");
            if (!sResult.Equals("16:00") || !s.Equals("08/10/99 16:00"))
            {
                iCountErrors++;
                Console.WriteLine("Err_0723gsg! doesnot match, " + sResult + " " + s);
            }

            s = "MiCrOsOfT";
            sResult = Regex.Replace(s, "icrosoft", "icrosoft", RegexOptions.IgnoreCase);
            if (!sResult.Equals("Microsoft"))
            {
                iCountErrors++;
                Console.WriteLine("Err_174ds! doesnot match, " + sResult + " " + s);
            }

            s = "my dog has fleas";
            sResult = Regex.Replace(s, "dog", "CAT", RegexOptions.IgnoreCase);
            if (!sResult.Equals("my CAT has fleas"))
            {
                iCountErrors++;
                Console.WriteLine("Err_174ds! doesnot match, " + sResult + " " + s);
            }

            s = "D.Bau";
            r = new Regex(@"D\.(.+)");
            sResult = r.Replace(s, "David $1");
            if (!sResult.Equals("David Bau"))
            {
                iCountErrors++;
                Console.WriteLine("Err_5734s! doesnot match, " + sResult + " " + s);
            }

            s = "aaaaa";
            r = new Regex("a");
            sResult = r.Replace(s, "b", 2);
            if (!sResult.Equals("bbaaa"))
            {
                iCountErrors++;
                Console.WriteLine("Err_78453fgs! doesnot match, " + sResult + " " + s);
            }

            sResult = r.Replace(s, "b", 2, 3);
            if (!sResult.Equals("aaabb"))
            {
                iCountErrors++;
                Console.WriteLine("Err_712ff! doesnot match, " + sResult + " " + s);
            }

            // [] Replace with MatchEvaluators - relevant fn's for this are defined below 
            //-----------------------------------------------------------------
            strLoc = "Loc_746tegd";
            sResult = Regex.Replace("Big mountain", "(Big|Small)", new MatchEvaluator(Rep1));
            iCountTestcases++;
            if (!sResult.Equals("Huge mountain"))
            {
                iCountErrors++;
                Console.WriteLine("Err_452wfdf! doesnot match, " + sResult);
            }

            sResult = Regex.Replace("Small village", "(Big|Small)", new MatchEvaluator(Rep1));
            if (!sResult.Equals("Tiny village"))
            {
                iCountErrors++;
                Console.WriteLine("Err_8420sgf! doesnot match, " + sResult);
            }

            if ("i".ToUpper() == "I")
            {
                sResult = Regex.Replace("bIG horse", "(Big|Small)", new MatchEvaluator(Rep1), RegexOptions.IgnoreCase);
                if (!sResult.Equals("Huge horse"))
                {
                    iCountErrors++;
                    Console.WriteLine("Err_232fg! doesnot match, " + sResult);
                }
            }

            sResult = Regex.Replace("sMaLl dog", "(Big|Small)", new MatchEvaluator(Rep1), RegexOptions.IgnoreCase);
            if (!sResult.Equals("Tiny dog"))
            {
                iCountErrors++;
                Console.WriteLine("Err_073235rdfa! doesnot match, " + sResult);
            }

            r = new Regex(".+");
            sResult = r.Replace("XSP_TEST_FAILURE", new MatchEvaluator(Rep2));
            if (!sResult.Equals("SUCCESS"))
            {
                iCountErrors++;
                Console.WriteLine("Err_246egsd! doesnot match, " + sResult);
            }

            r = new Regex("[abcabc]");
            sResult = r.Replace("abcabc", new MatchEvaluator(Rep3));
            if (!sResult.Equals("ABCABC"))
            {
                iCountErrors++;
                Console.WriteLine("Err_75432sg! doesnot match, " + sResult);
            }

            r = new Regex("[abcabc]");
            sResult = r.Replace("abcabc", new MatchEvaluator(Rep3), 3);
            if (!sResult.Equals("ABCabc"))
            {
                iCountErrors++;
                Console.WriteLine("Err_75432sg! doesnot match, " + sResult);
            }

            r = new Regex("[abcabc]");
            sResult = r.Replace("abcabc", new MatchEvaluator(Rep3), 3, 2);
            if (!sResult.Equals("abCABc"))
            {
                iCountErrors++;
                Console.WriteLine("Err_75432sg! doesnot match, " + sResult);
            }

            // [] Replace with group numbers
            //-----------------------------------------------------------------
            strLoc = "Loc_563rfg";
            iCountTestcases++;
            r = new Regex("([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z]([a-z])))))))))))))))");
            sResult = r.Replace("abcdefghiklmnop", "$15");
            if (!sResult.Equals("p"))
            {
                iCountErrors++;
                Console.WriteLine("Err_865rfsg! doesnot match, " + sResult);
            }

            sResult = r.Replace("abcdefghiklmnop", "$3");
            if (!sResult.Equals("cdefghiklmnop"))
            {
                iCountErrors++;
                Console.WriteLine("Err_9752fsf! doesnot match, " + sResult);
            }

            //Stress
            pattern = String.Empty;
            for (i = 0; i < 1000; i++)
                pattern += "([a-z]";
            for (i = 0; i < 1000; i++)
                pattern += ")";
            s = String.Empty;
            for (i = 0; i < 200; i++)
                s += "abcde";
            r = new Regex(pattern);
            sResult = r.Replace(s, "$1000");
            if (!sResult.Equals("e"))
            {
                iCountErrors++;
                Console.WriteLine("Err_98725rdfsg! doesnot match, " + sResult);
            }

            sResult = r.Replace(s, "$1");
            sExp = String.Empty;
            for (i = 0; i < 200; i++)
                sExp += "abcde";
            if (!sResult.Equals(sExp))
            {
                iCountErrors++;
                Console.WriteLine("Err_1074wf! doesnot match, " + sResult);
            }

            //undefined group
            sResult = Regex.Replace("abc", "([a_z])(.+)", "$3");
            if (!sResult.Equals("$3"))
            {
                iCountErrors++;
                Console.WriteLine("Err_1074wf! doesnot match, " + sResult);
            }

            // Regression test:
            // Regex treating Devanagari matra characters as matching "\b"
            // Unicode characters in the "Mark, NonSpacing" Category, U+0902=Devanagari sign anusvara, U+0947=Devanagri vowel sign E
            try
            {
                iCountTestcases++;
                Regex regexRendering = new Regex(@"\u0915\u0930.*?\b", RegexOptions.CultureInvariant | RegexOptions.Singleline);
                String strBoldedText = regexRendering.Replace("\u092f\u0939 \u0915\u0930 \u0935\u0939 \u0915\u0930\u0947\u0902 \u0939\u0948\u0964", RepBold);
                String expectedBoldedText = "\u092f\u0939 <b>\u0915\u0930</b> \u0935\u0939 <b>\u0915\u0930\u0947\u0902</b> \u0939\u0948\u0964";
                if (strBoldedText != expectedBoldedText)
                {
                    iCountErrors++;
                    Console.WriteLine("Err_788921gh! Devanagari matra characters are threated as \"\b\"");
                    Console.WriteLine("Expected string: {0}\nActual string: {1}", GetUnicodeString(expectedBoldedText), GetUnicodeString(strBoldedText));
                }
            }
            catch (Exception e)
            {
                iCountErrors++;
                Console.WriteLine("Err_788921! Unexpected Exception: " + e);
            }
            ///////////////////////////////////////////////////////////////////
            /////////////////////////// END TESTS /////////////////////////////
        }
        catch (Exception exc_general)
        {
            ++iCountErrors;
            Console.WriteLine("Error Err_8888yyy!  strLoc==" + strLoc + ", exc_general==" + exc_general.ToString());
        }

        ////  Finish Diagnostics
        Assert.Equal(0, iCountErrors);
    }

    public static String Rep1(Match input)
    {
        String s = String.Empty;
        if (String.Compare(input.ToString(), "Big", StringComparison.CurrentCultureIgnoreCase) == 0)
            s = "Huge";
        else
            s = "Tiny";
        return s;
    }

    public static String Rep2(Match input)
    {
        return "SUCCESS";
    }

    public static String Rep3(Match input)
    {
        String s = String.Empty;
        if (input.ToString().Equals("a"))
            s = "A";
        if (input.ToString().Equals("b"))
            s = "B";
        if (input.ToString().Equals("c"))
            s = "C";
        return s;
    }

    public static String RepBold(Match m)
    {
        string str = m.ToString();
        return String.Format("<b>{0}</b>", str);
    }

    static String GetUnicodeString(String str)
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

        return (buffer.ToString());
    }
}