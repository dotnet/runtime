// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexKnownPatternTests
    {
        //
        // These patterns come from the Regex documentation at docs.microsoft.com.
        //

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-example-scanning-for-hrefs
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Examples_ScanningHrefs(RegexOptions options)
        {
            const string HrefPattern =
                @"href\s*=\s*(?:[""'](?<1>[^""']*)[""']|(?<1>\S+))";

            const string InputString =
                "My favorite web sites include:</P>" +
                "<A HREF=\"http://msdn2.microsoft.com\">" +
                "MSDN Home Page</A></P>" +
                "<A HREF=\"http://www.microsoft.com\">" +
                "Microsoft Corporation Home Page</A></P>" +
                "<A HREF=\"http://blogs.msdn.com/bclteam\">" +
                ".NET Base Class Library blog</A></P>";

            Match m = Regex.Match(InputString, HrefPattern, options | RegexOptions.IgnoreCase);
            Assert.True(m.Success);
            Assert.Equal("http://msdn2.microsoft.com", m.Groups[1].ToString());
            Assert.Equal(43, m.Groups[1].Index);

            m = m.NextMatch();
            Assert.True(m.Success);
            Assert.Equal("http://www.microsoft.com", m.Groups[1].ToString());
            Assert.Equal(102, m.Groups[1].Index);

            m = m.NextMatch();
            Assert.True(m.Success);
            Assert.Equal("http://blogs.msdn.com/bclteam", m.Groups[1].ToString());
            Assert.Equal(176, m.Groups[1].Index);

            m = m.NextMatch();
            Assert.False(m.Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-example-changing-date-formats
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Examples_MDYtoDMY(RegexOptions options)
        {
            string dt = new DateTime(2020, 1, 8, 0, 0, 0, DateTimeKind.Utc).ToString("d", DateTimeFormatInfo.InvariantInfo);
            string result = Regex.Replace(dt, @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b", "${day}-${month}-${year}", options);
            Assert.Equal("08-01-2020", result);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-extract-a-protocol-and-port-number-from-a-url
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Examples_ExtractProtocolPort(RegexOptions options)
        {
            string url = "http://www.contoso.com:8080/letters/readme.html";
            Regex r = new Regex(@"^(?<proto>\w+)://[^/]+?(?<port>:\d+)?/", options);
            Match m = r.Match(url);
            Assert.True(m.Success);
            Assert.Equal("http:8080", m.Result("${proto}${port}"));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        [Theory]
        [InlineData("david.jones@proseware.com", true)]
        [InlineData("d.j@server1.proseware.com", true)]
        [InlineData("jones@ms1.proseware.com", true)]
        [InlineData("j.@server1.proseware.com", false)]
        [InlineData("j@proseware.com9", true)]
        [InlineData("js#internal@proseware.com", true)]
        [InlineData("j_9@[129.126.118.1]", true)]
        [InlineData("j..s@proseware.com", false)]
        [InlineData("js*@proseware.com", false)]
        [InlineData("js@proseware..com", false)]
        [InlineData("js@proseware.com9", true)]
        [InlineData("j.s@server1.proseware.com", true)]
        [InlineData("\"j\\\"s\\\"\"@proseware.com", true)]
        [InlineData("js@contoso.\u4E2D\u56FD", true)]
        public void Docs_Examples_ValidateEmail(string email, bool expectedIsValid)
        {
            Assert.Equal(expectedIsValid, IsValidEmail(email, RegexOptions.None));
            Assert.Equal(expectedIsValid, IsValidEmail(email, RegexOptions.Compiled));

            bool IsValidEmail(string email, RegexOptions options)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                try
                {
                    // Normalize the domain
                    email = Regex.Replace(email, @"(@)(.+)$", DomainMapper, options, TimeSpan.FromMilliseconds(200));

                    // Examines the domain part of the email and normalizes it.
                    string DomainMapper(Match match)
                    {
                        // Use IdnMapping class to convert Unicode domain names.
                        var idn = new IdnMapping();

                        // Pull out and process domain name (throws ArgumentException on invalid)
                        string domainName = idn.GetAscii(match.Groups[2].Value);

                        return match.Groups[1].Value + domainName;
                    }
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
                catch (ArgumentException)
                {
                    return false;
                }

                try
                {
                    return Regex.IsMatch(email,
                        @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                        @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                        options | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));
                }
                catch (RegexMatchTimeoutException)
                {
                    return false;
                }
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#matched_subexpression
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_MatchedSubexpression(RegexOptions options)
        {
            const string Pattern = @"(\w+)\s(\1)";
            const string Input = "He said that that was the the correct answer.";

            Match match = Regex.Match(Input, Pattern, RegexOptions.IgnoreCase | options);

            Assert.True(match.Success);
            Assert.Equal("that", match.Groups[1].Value);
            Assert.Equal(8, match.Groups[1].Index);
            Assert.Equal(13, match.Groups[2].Index);

            match = match.NextMatch();
            Assert.True(match.Success);
            Assert.Equal("the", match.Groups[1].Value);
            Assert.Equal(22, match.Groups[1].Index);
            Assert.Equal(26, match.Groups[2].Index);

            Assert.False(match.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#named-matched-subexpressions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_NamedMatchedSubexpression1(RegexOptions options)
        {
            const string Pattern = @"(?<duplicateWord>\w+)\s\k<duplicateWord>\W(?<nextWord>\w+)";
            const string Input = "He said that that was the the correct answer.";

            Match match = Regex.Match(Input, Pattern, RegexOptions.IgnoreCase | options);

            Assert.True(match.Success);
            Assert.Equal("that", match.Groups["duplicateWord"].Value);
            Assert.Equal(8, match.Groups["duplicateWord"].Index);
            Assert.Equal("was", match.Groups["nextWord"].Value);

            match = match.NextMatch();
            Assert.True(match.Success);
            Assert.Equal("the", match.Groups["duplicateWord"].Value);
            Assert.Equal(22, match.Groups["duplicateWord"].Index);
            Assert.Equal("correct", match.Groups["nextWord"].Value);

            Assert.False(match.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#named-matched-subexpressions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_NamedMatchedSubexpression2(RegexOptions options)
        {
            const string Pattern = @"\D+(?<digit>\d+)\D+(?<digit>\d+)?";
            string[] inputs = { "abc123def456", "abc123def" };

            var actual = new StringBuilder();
            foreach (string input in inputs)
            {
                Match m = Regex.Match(input, Pattern, options);
                if (m.Success)
                {
                    actual.AppendLine($"Match: {m.Value}");
                    for (int grpCtr = 1; grpCtr < m.Groups.Count; grpCtr++)
                    {
                        Group grp = m.Groups[grpCtr];
                        actual.AppendLine($"Group {grpCtr}: {grp.Value}");
                        for (int capCtr = 0; capCtr < grp.Captures.Count; capCtr++)
                        {
                            actual.AppendLine($"   Capture {capCtr}: {grp.Captures[capCtr].Value}");
                        }
                    }
                }
            }

            string expected =
                "Match: abc123def456" + Environment.NewLine +
                "Group 1: 456" + Environment.NewLine +
                "   Capture 0: 123" + Environment.NewLine +
                "   Capture 1: 456" + Environment.NewLine +
                "Match: abc123def" + Environment.NewLine +
                "Group 1: 123" + Environment.NewLine +
                "   Capture 0: 123" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#balancing-group-definitions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_BalancingGroups(RegexOptions options)
        {
            const string Pattern =
                "^[^<>]*" +
                 "(" +
                 "((?'Open'<)[^<>]*)+" +
                 "((?'Close-Open'>)[^<>]*)+" +
                 ")*" +
                 "(?(Open)(?!))$";
            const string Input = "<abc><mno<xyz>>";

            var actual = new StringBuilder();
            Match m = Regex.Match(Input, Pattern, options);
            if (m.Success)
            {
                actual.AppendLine($"Input: \"{Input}\"");
                actual.AppendLine($"Match: \"{m}\"");
                int grpCtr = 0;
                foreach (Group grp in m.Groups)
                {
                    actual.AppendLine($"   Group {grpCtr}: {grp.Value}");
                    grpCtr++;
                    int capCtr = 0;
                    foreach (Capture cap in grp.Captures)
                    {
                        actual.AppendLine($"      Capture {capCtr}: {cap.Value}");
                        capCtr++;
                    }
                }
            }

            string expected =
                "Input: \"<abc><mno<xyz>>\"" + Environment.NewLine +
                "Match: \"<abc><mno<xyz>>\"" + Environment.NewLine +
                "   Group 0: <abc><mno<xyz>>" + Environment.NewLine +
                "      Capture 0: <abc><mno<xyz>>" + Environment.NewLine +
                "   Group 1: <mno<xyz>>" + Environment.NewLine +
                "      Capture 0: <abc>" + Environment.NewLine +
                "      Capture 1: <mno<xyz>>" + Environment.NewLine +
                "   Group 2: <xyz" + Environment.NewLine +
                "      Capture 0: <abc" + Environment.NewLine +
                "      Capture 1: <mno" + Environment.NewLine +
                "      Capture 2: <xyz" + Environment.NewLine +
                "   Group 3: >" + Environment.NewLine +
                "      Capture 0: >" + Environment.NewLine +
                "      Capture 1: >" + Environment.NewLine +
                "      Capture 2: >" + Environment.NewLine +
                "   Group 4: " + Environment.NewLine +
                "   Group 5: mno<xyz>" + Environment.NewLine +
                "      Capture 0: abc" + Environment.NewLine +
                "      Capture 1: xyz" + Environment.NewLine +
                "      Capture 2: mno<xyz>" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#noncapturing-groups
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_NoncapturingGroups(RegexOptions options)
        {
            const string Pattern = @"(?:\b(?:\w+)\W*)+\.";
            const string Input = "This is a short sentence.";

            Match match = Regex.Match(Input, Pattern, options);
            Assert.True(match.Success);
            Assert.Equal("This is a short sentence.", match.Value);
            Assert.Equal(1, match.Groups.Count);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#group-options
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_GroupOptions(RegexOptions options)
        {
            const string Pattern = @"\b(?ix: d \w+)\s";
            const string Input = "Dogs are decidedly good pets.";

            Match match = Regex.Match(Input, Pattern, options);
            Assert.True(match.Success);
            Assert.Equal("Dogs ", match.Value);
            Assert.Equal(0, match.Index);

            match = match.NextMatch();
            Assert.True(match.Success);
            Assert.Equal("decidedly ", match.Value);
            Assert.Equal(9, match.Index);

            Assert.False(match.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-positive-lookahead-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_ZeroWidthPositiveLookaheadAssertions(RegexOptions options)
        {
            const string Pattern = @"\b\w+(?=\sis\b)";
            Match match;

            match = Regex.Match("The dog is a Malamute.", Pattern, options);
            Assert.True(match.Success);
            Assert.Equal("dog", match.Value);

            match = Regex.Match("The island has beautiful birds.", Pattern, options);
            Assert.False(match.Success);

            match = Regex.Match("The pitch missed home plate.", Pattern, options);
            Assert.False(match.Success);

            match = Regex.Match("Sunday is a weekend day.", Pattern, options);
            Assert.True(match.Success);
            Assert.Equal("Sunday", match.Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-negative-lookahead-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_ZeroWidthNegativeLookaheadAssertions(RegexOptions options)
        {
            const string Pattern = @"\b(?!un)\w+\b";
            const string Input = "unite one unethical ethics use untie ultimate";

            MatchCollection matches = Regex.Matches(Input, Pattern, RegexOptions.IgnoreCase | options);
            Assert.Equal("one", matches[0].Value);
            Assert.Equal("ethics", matches[1].Value);
            Assert.Equal("use", matches[2].Value);
            Assert.Equal("ultimate", matches[3].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-positive-lookbehind-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_ZeroWidthPositiveLookbehindAssertions(RegexOptions options)
        {
            const string Pattern = @"(?<=\b20)\d{2}\b";
            const string Input = "2010 1999 1861 2140 2009";

            MatchCollection matches = Regex.Matches(Input, Pattern, RegexOptions.IgnoreCase | options);
            Assert.Equal("10", matches[0].Value);
            Assert.Equal("09", matches[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-negative-lookbehind-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_ZeroWidthNegativeLookbehindAssertions(RegexOptions options)
        {
            const string Pattern = @"(?<!(Saturday|Sunday) )\b\w+ \d{1,2}, \d{4}\b";

            Assert.Equal("February 1, 2010", Regex.Match("Monday February 1, 2010", Pattern, options).Value);
            Assert.Equal("February 3, 2010", Regex.Match("Wednesday February 3, 2010", Pattern, options).Value);
            Assert.False(Regex.IsMatch("Saturday February 6, 2010", Pattern, options));
            Assert.False(Regex.IsMatch("Sunday February 7, 2010", Pattern, options));
            Assert.Equal("February 8, 2010", Regex.Match("Monday, February 8, 2010", Pattern, options).Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#nonbacktracking-subexpressions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_NonbacktrackingSubexpressions(RegexOptions options)
        {
            const string Back = @"(\w)\1+.\b";
            const string NoBack = @"(?>(\w)\1+).\b";
            string[] inputs = { "aaad", "aaaa" };

            Match back, noback;

            back = Regex.Match("cccd.", Back, options);
            noback = Regex.Match("cccd.", NoBack, options);
            Assert.True(back.Success);
            Assert.True(noback.Success);
            Assert.Equal("cccd", back.Value);
            Assert.Equal("cccd", noback.Value);

            back = Regex.Match("aaad", Back, options);
            noback = Regex.Match("aaad", NoBack, options);
            Assert.True(back.Success);
            Assert.True(noback.Success);
            Assert.Equal("aaad", back.Value);
            Assert.Equal("aaad", noback.Value);

            back = Regex.Match("aaaa", Back, options);
            noback = Regex.Match("aaaa", NoBack, options);
            Assert.True(back.Success);
            Assert.False(noback.Success);
            Assert.Equal("aaaa", back.Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#grouping-constructs-and-regular-expression-objects
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_GroupingConstructs_GroupCaptureRelationship(RegexOptions options)
        {
            const string Pattern = @"(\b(\w+)\W+)+";
            const string Input = "This is a short sentence.";
            Match match = Regex.Match(Input, Pattern, options);

            var actual = new StringBuilder();
            actual.AppendLine($"Match: '{match.Value}'");
            for (int ctr = 1; ctr < match.Groups.Count; ctr++)
            {
                actual.AppendLine($"   Group {ctr}: '{match.Groups[ctr].Value}'");
                int capCtr = 0;
                foreach (Capture capture in match.Groups[ctr].Captures)
                {
                    actual.AppendLine($"      Capture {capCtr}: '{capture.Value}'");
                    capCtr++;
                }
            }

            string expected =
                "Match: 'This is a short sentence.'" + Environment.NewLine +
                "   Group 1: 'sentence.'" + Environment.NewLine +
                "      Capture 0: 'This '" + Environment.NewLine +
                "      Capture 1: 'is '" + Environment.NewLine +
                "      Capture 2: 'a '" + Environment.NewLine +
                "      Capture 3: 'short '" + Environment.NewLine +
                "      Capture 4: 'sentence.'" + Environment.NewLine +
                "   Group 2: 'sentence'" + Environment.NewLine +
                "      Capture 0: 'This'" + Environment.NewLine +
                "      Capture 1: 'is'" + Environment.NewLine +
                "      Capture 2: 'a'" + Environment.NewLine +
                "      Capture 3: 'short'" + Environment.NewLine +
                "      Capture 4: 'sentence'" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.capture?view=netcore-3.1#examples
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Capture_Sentences(RegexOptions options)
        {
            const string Pattern = @"((\w+)[\s.])+";
            const string Input = "Yes. This dog is very friendly.";

            var actual = new StringBuilder();
            foreach (Match match in Regex.Matches(Input, Pattern, options))
            {
                actual.AppendLine($"Match: {match.Value}");
                for (int groupCtr = 0; groupCtr < match.Groups.Count; groupCtr++)
                {
                    Group group = match.Groups[groupCtr];
                    actual.AppendLine($"   Group {groupCtr}: {group.Value}");
                    for (int captureCtr = 0; captureCtr < group.Captures.Count; captureCtr++)
                    {
                        actual.AppendLine($"      Capture {captureCtr}: {group.Captures[captureCtr].Value}");
                    }
                }
            }

            string expected =
                "Match: Yes." + Environment.NewLine +
                "   Group 0: Yes." + Environment.NewLine +
                "      Capture 0: Yes." + Environment.NewLine +
                "   Group 1: Yes." + Environment.NewLine +
                "      Capture 0: Yes." + Environment.NewLine +
                "   Group 2: Yes" + Environment.NewLine +
                "      Capture 0: Yes" + Environment.NewLine +
                "Match: This dog is very friendly." + Environment.NewLine +
                "   Group 0: This dog is very friendly." + Environment.NewLine +
                "      Capture 0: This dog is very friendly." + Environment.NewLine +
                "   Group 1: friendly." + Environment.NewLine +
                "      Capture 0: This " + Environment.NewLine +
                "      Capture 1: dog " + Environment.NewLine +
                "      Capture 2: is " + Environment.NewLine +
                "      Capture 3: very " + Environment.NewLine +
                "      Capture 4: friendly." + Environment.NewLine +
                "   Group 2: friendly" + Environment.NewLine +
                "      Capture 0: This" + Environment.NewLine +
                "      Capture 1: dog" + Environment.NewLine +
                "      Capture 2: is" + Environment.NewLine +
                "      Capture 3: very" + Environment.NewLine +
                "      Capture 4: friendly" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/api/system.text.regularexpressions.capture.value?view=netcore-3.1
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Capture_ProductNumber(RegexOptions options)
        {
            const string Pattern = @"^([a-z]+)(\d+)?\.([a-z]+(\d)*)$";
            string[] values = { "AC10", "Za203.CYM", "XYZ.CoA", "ABC.x170" };

            var actual = new StringBuilder();
            foreach (var value in values)
            {
                Match m = Regex.Match(value, Pattern, RegexOptions.IgnoreCase | options);
                if (m.Success)
                {
                    actual.AppendLine($"Match: '{m.Value}'");
                    actual.AppendLine($"   Number of Capturing Groups: {m.Groups.Count}");
                    for (int gCtr = 0; gCtr < m.Groups.Count; gCtr++)
                    {
                        Group group = m.Groups[gCtr];
                        actual.AppendLine($"      Group {gCtr}: {(group.Value == "" ? "<empty>" : "'" + group.Value + "'")}");
                        actual.AppendLine($"         Number of Captures: {group.Captures.Count}");
                        for (int cCtr = 0; cCtr < group.Captures.Count; cCtr++)
                        {
                            actual.AppendLine($"            Capture {cCtr}: {group.Captures[cCtr].Value}");
                        }
                    }
                }
                else
                {
                    actual.AppendLine($"No match for {value}: Match.Value is {(m.Value == String.Empty ? "<empty>" : m.Value)}");
                }
            }

            string expected =
                "No match for AC10: Match.Value is <empty>" + Environment.NewLine +
                "Match: 'Za203.CYM'" + Environment.NewLine +
                "   Number of Capturing Groups: 5" + Environment.NewLine +
                "      Group 0: 'Za203.CYM'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: Za203.CYM" + Environment.NewLine +
                "      Group 1: 'Za'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: Za" + Environment.NewLine +
                "      Group 2: '203'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: 203" + Environment.NewLine +
                "      Group 3: 'CYM'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: CYM" + Environment.NewLine +
                "      Group 4: <empty>" + Environment.NewLine +
                "         Number of Captures: 0" + Environment.NewLine +
                "Match: 'XYZ.CoA'" + Environment.NewLine +
                "   Number of Capturing Groups: 5" + Environment.NewLine +
                "      Group 0: 'XYZ.CoA'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: XYZ.CoA" + Environment.NewLine +
                "      Group 1: 'XYZ'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: XYZ" + Environment.NewLine +
                "      Group 2: <empty>" + Environment.NewLine +
                "         Number of Captures: 0" + Environment.NewLine +
                "      Group 3: 'CoA'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: CoA" + Environment.NewLine +
                "      Group 4: <empty>" + Environment.NewLine +
                "         Number of Captures: 0" + Environment.NewLine +
                "Match: 'ABC.x170'" + Environment.NewLine +
                "   Number of Capturing Groups: 5" + Environment.NewLine +
                "      Group 0: 'ABC.x170'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: ABC.x170" + Environment.NewLine +
                "      Group 1: 'ABC'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: ABC" + Environment.NewLine +
                "      Group 2: <empty>" + Environment.NewLine +
                "         Number of Captures: 0" + Environment.NewLine +
                "      Group 3: 'x170'" + Environment.NewLine +
                "         Number of Captures: 1" + Environment.NewLine +
                "            Capture 0: x170" + Environment.NewLine +
                "      Group 4: '0'" + Environment.NewLine +
                "         Number of Captures: 3" + Environment.NewLine +
                "            Capture 0: 1" + Environment.NewLine +
                "            Capture 1: 7" + Environment.NewLine +
                "            Capture 2: 0" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#linear-comparison-without-backtracking
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Backtracking_LinearComparisonWithoutBacktracking(RegexOptions options)
        {
            const string Pattern = @"e{2}\w\b";
            const string Input = "needing a reed";

            MatchCollection matches = Regex.Matches(Input, Pattern, options);
            Assert.Equal(1, matches.Count);
            Assert.Equal("eed", matches[0].Value);
            Assert.Equal(11, matches[0].Index);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#backtracking-with-optional-quantifiers-or-alternation-constructs
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Backtracking_WithOptionalQuantifiersOrAlternationConstructs(RegexOptions options)
        {
            const string Pattern = ".*(es)";
            const string Input = "Essential services are provided by regular expressions.";

            Match m = Regex.Match(Input, Pattern, RegexOptions.IgnoreCase | options);
            Assert.True(m.Success);
            Assert.Equal("Essential services are provided by regular expres", m.Value);
            Assert.Equal(0, m.Index);
            Assert.Equal(47, m.Groups[1].Index);

            Assert.False(m.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#nonbacktracking-subexpression
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Backtracking_WithNestedOptionalQuantifiers(RegexOptions options)
        {
            const string Input = "b51:4:1DB:9EE1:5:27d60:f44:D4:cd:E:5:0A5:4a:D24:41Ad:";
            // Assert.False(Regex.IsMatch(Input, "^(([0-9a-fA-F]{1,4}:)*([0-9a-fA-F]{1,4}))*(::)$")); // takes too long due to backtracking
            Assert.False(Regex.IsMatch(Input, "^((?>[0-9a-fA-F]{1,4}:)*(?>[0-9a-fA-F]{1,4}))*(::)$", options)); // non-backtracking
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#lookbehind-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Backtracking_LookbehindAssertions(RegexOptions options)
        {
            const string Input = "test@contoso.com";

            const string Pattern = @"^[0-9A-Z]([-.\w]*[0-9A-Z])?@";
            Assert.True(Regex.IsMatch(Input, Pattern, RegexOptions.IgnoreCase | options));

            const string BehindPattern = @"^[0-9A-Z][-.\w]*(?<=[0-9A-Z])@";
            Assert.True(Regex.IsMatch(Input, BehindPattern, RegexOptions.IgnoreCase | options));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#lookahead-assertions
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_Backtracking_LookaheadAssertions(RegexOptions options)
        {
            const string Input = "aaaaaaaaaaaaaaaaaaaaaa.";

            //const string Pattern = @"^(([A-Z]\w*)+\.)*[A-Z]\w*$";
            //Assert.False(Regex.IsMatch(Input, Pattern, RegexOptions.IgnoreCase)); // takes too long due to backtracking

            const string AheadPattern = @"^((?=[A-Z])\w+\.)*[A-Z]\w*$";
            Assert.False(Regex.IsMatch(Input, AheadPattern, RegexOptions.IgnoreCase | options));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_LazyQuantifiers(RegexOptions options)
        {
            const string Input = "This sentence ends with the number 107325.";

            const string GreedyPattern = @".+(\d+)\.";
            Match match = Regex.Match(Input, GreedyPattern, options);
            Assert.True(match.Success);
            Assert.Equal("5", match.Groups[1].Value);

            const string LazyPattern = @".+?(\d+)\.";
            match = Regex.Match(Input, LazyPattern, options);
            Assert.True(match.Success);
            Assert.Equal("107325", match.Groups[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_PositiveLookahead(RegexOptions options)
        {
            const string Pattern = @"\b[A-Z]+\b(?=\P{P})";
            const string Input = "If so, what comes next?";
            MatchCollection matches = Regex.Matches(Input, Pattern, RegexOptions.IgnoreCase | options);
            Assert.Equal(3, matches.Count);
            Assert.Equal("If", matches[0].Value);
            Assert.Equal("what", matches[1].Value);
            Assert.Equal("comes", matches[2].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_NegativeLookahead(RegexOptions options)
        {
            const string Pattern = @"\b(?!non)\w+\b";
            const string Input = "Nonsense is not always non-functional.";
            MatchCollection matches = Regex.Matches(Input, Pattern, RegexOptions.IgnoreCase | options);
            Assert.Equal(4, matches.Count);
            Assert.Equal("is", matches[0].Value);
            Assert.Equal("not", matches[1].Value);
            Assert.Equal("always", matches[2].Value);
            Assert.Equal("functional", matches[3].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/alternation-constructs-in-regular-expressions#conditional-matching-with-an-expression
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_ConditionalEvaluation(RegexOptions options)
        {
            const string Pattern = @"\b(?(\d{2}-)\d{2}-\d{7}|\d{3}-\d{2}-\d{4})\b";
            const string Input = "01-9999999 020-333333 777-88-9999";

            MatchCollection matches = Regex.Matches(Input, Pattern, options);
            Assert.Equal(2, matches.Count);

            Assert.Equal("01-9999999", matches[0].Value);
            Assert.Equal(0, matches[0].Index);

            Assert.Equal("777-88-9999", matches[1].Value);
            Assert.Equal(22, matches[1].Index);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_RightToLeftMatching(RegexOptions options)
        {
            const string GreedyPattern = @".+(\d+)\.";
            const string Input = "This sentence ends with the number 107325.";

            // Match from left-to-right using lazy quantifier .+?.
            Match match = Regex.Match(Input, GreedyPattern);
            Assert.True(match.Success);
            Assert.Equal("5", match.Groups[1].Value);

            // Match from right-to-left using greedy quantifier .+.
            match = Regex.Match(Input, GreedyPattern, RegexOptions.RightToLeft | options);
            Assert.True(match.Success);
            Assert.Equal("107325", match.Groups[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EngineCapabilities_PositiveNegativeLookbehind(RegexOptions options)
        {
            const string Pattern = @"^[A-Z0-9]([-!#$%&'.*+/=?^`{}|~\w])*(?<=[A-Z0-9])$";

            Assert.True(Regex.IsMatch("jack.sprat", Pattern, RegexOptions.IgnoreCase | options));
            Assert.False(Regex.IsMatch("dog#", Pattern, RegexOptions.IgnoreCase | options));
            Assert.True(Regex.IsMatch("dog#1", Pattern, RegexOptions.IgnoreCase | options));
            Assert.True(Regex.IsMatch("me.myself", Pattern, RegexOptions.IgnoreCase | options));
            Assert.False(Regex.IsMatch("me.myself!", Pattern, RegexOptions.IgnoreCase | options));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/miscellaneous-constructs-in-regular-expressions#inline-options
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_InlineOptions(RegexOptions options)
        {
            const string Input = "double dare double Double a Drooling dog The Dreaded Deep";

            var actual = new StringBuilder();

            foreach (Match match in Regex.Matches(Input, @"\b(D\w+)\s(d\w+)\b", options))
            {
                actual.AppendLine(match.Value);
                if (match.Groups.Count > 1)
                {
                    for (int ctr = 1; ctr < match.Groups.Count; ctr++)
                    {
                        actual.AppendLine($"   Group {ctr}: {match.Groups[ctr].Value}");
                    }
                }
            }
            actual.AppendLine();

            foreach (Match match in Regex.Matches(Input, @"\b(D\w+)(?ixn) \s (d\w+) \b", options))
            {
                actual.AppendLine(match.Value);
                if (match.Groups.Count > 1)
                {
                    for (int ctr = 1; ctr < match.Groups.Count; ctr++)
                    {
                        actual.AppendLine($"   Group {ctr}: '{match.Groups[ctr].Value}'");
                    }
                }
            }

            string expected =
                "Drooling dog" + Environment.NewLine +
                "   Group 1: Drooling" + Environment.NewLine +
                "   Group 2: dog" + Environment.NewLine +
                Environment.NewLine +
                "Drooling dog" + Environment.NewLine +
                "   Group 1: 'Drooling'" + Environment.NewLine +
                "Dreaded Deep" + Environment.NewLine +
                "   Group 1: 'Dreaded'" + Environment.NewLine;

            Assert.Equal(expected, actual.ToString());
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/miscellaneous-constructs-in-regular-expressions#inline-comment
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_InlineComment(RegexOptions options)
        {
            const string Pattern = @"\b((?# case-sensitive comparison)D\w+)\s(?ixn)((?#case-insensitive comparison)d\w+)\b";
            const string Input = "double dare double Double a Drooling dog The Dreaded Deep";

            Match match = Regex.Match(Input, Pattern, options);
            Assert.True(match.Success);
            Assert.Equal("Drooling dog", match.Value);
            Assert.Equal(2, match.Groups.Count);
            Assert.Equal("Drooling", match.Groups[1].Value);

            match = match.NextMatch();
            Assert.True(match.Success);
            Assert.Equal("Dreaded Deep", match.Value);
            Assert.Equal(2, match.Groups.Count);
            Assert.Equal("Dreaded", match.Groups[1].Value);

            Assert.False(match.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/miscellaneous-constructs-in-regular-expressions#end-of-line-comment
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void Docs_EndOfLineComment(RegexOptions options)
        {
            const string Pattern = @"\{\d+(,-*\d+)*(\:\w{1,4}?)*\}(?x) # Looks for a composite format item.";
            const string Input = "{0,-3:F}";
            Assert.True(Regex.IsMatch(Input, Pattern, options));
        }



        //
        // These patterns come from real-world customer usages
        //

        [Theory]
        [InlineData("https://foo.com:443/bar/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        [InlineData("ftp://443/notproviders/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        [InlineData("ftp://443/providersnot/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        public void RealWorld_ExtractResourceUri(string url, string expected)
        {
            foreach (RegexOptions options in new[] { RegexOptions.Compiled, RegexOptions.None })
            {
                Regex r = new Regex(@"/providers/(.+?)\?", options);
                Match m = r.Match(url);
                Assert.True(m.Success);
                Assert.Equal(2, m.Groups.Count);
                Assert.Equal(expected, m.Groups[1].Value);
            }
        }

        [Theory]
        [InlineData("IsValidCSharpName", true)]
        [InlineData("_IsValidCSharpName", true)]
        [InlineData("__", true)]
        [InlineData("a\u2169", true)] // \u2169 is in {Nl}
        [InlineData("\u2169b", true)] // \u2169 is in {Nl}
        [InlineData("a\u0600", true)] // \u0600 is in {Cf}
        [InlineData("\u0600b", false)] // \u0600 is in {Cf}
        [InlineData("a\u0300", true)] // \u0300 is in {Mn}
        [InlineData("\u0300b", false)] // \u0300 is in {Mn}
        [InlineData("https://foo.com:443/bar/17/groups/0ad1/providers/Network/public/4e-ip?version=16", false)]
        [InlineData("david.jones@proseware.com", false)]
        [InlineData("~david", false)]
        [InlineData("david~", false)]
        public void RealWorld_IsValidCSharpName(string value, bool isExpectedMatch)
        {
            const string StartCharacterRegex = @"_|[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}]";
            const string PartCharactersRegex = @"[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]";

            const string IdentifierRegex = @"^(" + StartCharacterRegex + ")(" + PartCharactersRegex + ")*$";

            foreach (RegexOptions options in new[] { RegexOptions.Compiled, RegexOptions.None })
            {
                Regex r = new Regex(IdentifierRegex, options);
                Assert.Equal(isExpectedMatch, r.IsMatch(value));
            }
        }

        [Theory]
        [InlineData("; this is a comment", true)]
        [InlineData("\t; so is this", true)]
        [InlineData("  ; and this", true)]
        [InlineData(";", true)]
        [InlineData(";comment\nNotThisBecauseOfNewLine", false)]
        [InlineData("-;not a comment", false)]
        public void RealWorld_IsCommentLine(string value, bool isExpectedMatch)
        {
            const string CommentLineRegex = @"^\s*;\s*(.*?)\s*$";

            foreach (RegexOptions options in new[] { RegexOptions.Compiled, RegexOptions.None })
            {
                Regex r = new Regex(CommentLineRegex, options);
                Assert.Equal(isExpectedMatch, r.IsMatch(value));
            }
        }

        [Theory]
        [InlineData("[ThisIsASection]", true)]
        [InlineData(" [ThisIsASection] ", true)]
        [InlineData("\t[ThisIs\\ASection]\t", true)]
        [InlineData("\t[This.Is:(A+Section)]\t", true)]
        [InlineData("[This Is Not]", false)]
        [InlineData("This is not[]", false)]
        [InlineData("[Nor This]/", false)]
        public void RealWorld_IsSectionLine(string value, bool isExpectedMatch)
        {
            const string SectionLineRegex = @"^\s*\[([\w\.\-\+:\/\(\)\\]+)\]\s*$";

            foreach (RegexOptions options in new[] { RegexOptions.Compiled, RegexOptions.None })
            {
                Regex r = new Regex(SectionLineRegex, options);
                Assert.Equal(isExpectedMatch, r.IsMatch(value));
            }
        }
    }
}
