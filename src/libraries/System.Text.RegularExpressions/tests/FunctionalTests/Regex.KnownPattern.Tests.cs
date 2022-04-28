// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Examples_ScanningHrefs(RegexEngine engine)
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

            Regex r = await RegexHelpers.GetRegexAsync(engine, HrefPattern, RegexOptions.IgnoreCase);

            Match m = r.Match(InputString);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Examples_MDYtoDMY(RegexEngine engine)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b");

            string dt = new DateTime(2020, 1, 8, 0, 0, 0, DateTimeKind.Utc).ToString("d", DateTimeFormatInfo.InvariantInfo);
            Assert.Equal("08-01-2020", r.Replace(dt, "${day}-${month}-${year}"));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-extract-a-protocol-and-port-number-from-a-url
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Examples_ExtractProtocolPort(RegexEngine engine)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^(?<proto>\w+)://[^/]+?(?<port>:\d+)?/");
            Match m = r.Match("http://www.contoso.com:8080/letters/readme.html");
            Assert.True(m.Success);
            Assert.Equal("http:8080", m.Result("${proto}${port}"));
        }

        public static IEnumerable<object[]> Docs_Examples_ValidateEmail_TestData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                if (RegexHelpers.IsNonBacktracking(engine))
                {
                    // The email pattern uses conditional test-patterns so NonBacktracking is not supported here
                    continue;
                }

                yield return new object[] { engine, "david.jones@proseware.com", true };
                yield return new object[] { engine, "d.j@server1.proseware.com", true };
                yield return new object[] { engine, "jones@ms1.proseware.com", true };
                yield return new object[] { engine, "j.@server1.proseware.com", false };
                yield return new object[] { engine, "j@proseware.com9", true };
                yield return new object[] { engine, "js#internal@proseware.com", true };
                yield return new object[] { engine, "j_9@[129.126.118.1]", true };
                yield return new object[] { engine, "j..s@proseware.com", false };
                yield return new object[] { engine, "js*@proseware.com", false };
                yield return new object[] { engine, "js@proseware..com", false };
                yield return new object[] { engine, "js@proseware.com9", true };
                yield return new object[] { engine, "j.s@server1.proseware.com", true };
                yield return new object[] { engine, "\"j\\\"s\\\"\"@proseware.com", true };
                yield return new object[] { engine, "js@contoso.\u4E2D\u56FD", true };
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/how-to-verify-that-strings-are-in-valid-email-format
        [Theory]
        [MemberData(nameof(Docs_Examples_ValidateEmail_TestData))]
        public async Task Docs_Examples_ValidateEmail(RegexEngine engine, string email, bool expectedIsValid)
        {
            Assert.Equal(expectedIsValid, await IsValidEmailAsync(email, engine));

            async Task<bool> IsValidEmailAsync(string email, RegexEngine engine)
            {
                if (string.IsNullOrWhiteSpace(email))
                {
                    return false;
                }

                Regex r = await RegexHelpers.GetRegexAsync(engine, @"(@)(.+)$", RegexOptions.None);

                try
                {
                    // Normalize the domain part of the email
                    email = r.Replace(email, match =>
                    {
                        // Use IdnMapping class to convert Unicode domain names.
                        var idn = new IdnMapping();

                        // Pull out and process domain name (throws ArgumentException on invalid)
                        string domainName = idn.GetAscii(match.Groups[2].Value);

                        return match.Groups[1].Value + domainName;
                    });
                }
                catch (ArgumentException)
                {
                    return false; // for invalid IDN name with IdnMapping
                }

                r = await RegexHelpers.GetRegexAsync(
                    engine,
                    @"^(?("")("".+?(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                    @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-0-9a-z]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
                    RegexOptions.IgnoreCase);

                return r.IsMatch(email);
            }
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#matched_subexpression
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_MatchedSubexpression(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // backreferences not supported
                return;
            }

            const string Pattern = @"(\w+)\s(\1)";
            const string Input = "He said that that was the the correct answer.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            Match match = r.Match(Input);

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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_NamedMatchedSubexpression1(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // backreferences not supported
                return;
            }

            const string Pattern = @"(?<duplicateWord>\w+)\s\k<duplicateWord>\W(?<nextWord>\w+)";
            const string Input = "He said that that was the the correct answer.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            Match match = r.Match(Input);

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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_NamedMatchedSubexpression2(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // multiple captures not supported
                return;
            }

            const string Pattern = @"\D+(?<digit>\d+)\D+(?<digit>\d+)?";
            string[] inputs = { "abc123def456", "abc123def" };

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            var actual = new StringBuilder();
            foreach (string input in inputs)
            {
                Match m = r.Match(input);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_BalancingGroups(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // balancing groups not supported
                return;
            }

            const string Pattern =
                "^[^<>]*" +
                 "(" +
                 "((?'Open'<)[^<>]*)+" +
                 "((?'Close-Open'>)[^<>]*)+" +
                 ")*" +
                 "(?(Open)(?!))$";
            const string Input = "<abc><mno<xyz>>";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            var actual = new StringBuilder();
            Match m = r.Match(Input);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_NoncapturingGroups(RegexEngine engine)
        {
            const string Pattern = @"(?:\b(?:\w+)\W*)+\.";
            const string Input = "This is a short sentence.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Match match = r.Match(Input);
            Assert.True(match.Success);
            Assert.Equal("This is a short sentence.", match.Value);
            Assert.Equal(1, match.Groups.Count);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#group-options
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_GroupOptions(RegexEngine engine)
        {
            const string Pattern = @"\b(?ix: d \w+)\s";
            const string Input = "Dogs are decidedly good pets.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Match match = r.Match(Input);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_ZeroWidthPositiveLookaheadAssertions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            const string Pattern = @"\b\w+(?=\sis\b)";
            Match match;

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            match = r.Match("The dog is a Malamute.");
            Assert.True(match.Success);
            Assert.Equal("dog", match.Value);

            match = r.Match("The island has beautiful birds.");
            Assert.False(match.Success);

            match = r.Match("The pitch missed home plate.");
            Assert.False(match.Success);

            match = r.Match("Sunday is a weekend day.");
            Assert.True(match.Success);
            Assert.Equal("Sunday", match.Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-negative-lookahead-assertions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_ZeroWidthNegativeLookaheadAssertions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            const string Pattern = @"\b(?!un)\w+\b";
            const string Input = "unite one unethical ethics use untie ultimate";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal("one", matches[0].Value);
            Assert.Equal("ethics", matches[1].Value);
            Assert.Equal("use", matches[2].Value);
            Assert.Equal("ultimate", matches[3].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-positive-lookbehind-assertions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_ZeroWidthPositiveLookbehindAssertions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            const string Pattern = @"(?<=\b20)\d{2}\b";
            const string Input = "2010 1999 1861 2140 2009";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal("10", matches[0].Value);
            Assert.Equal("09", matches[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#zero-width-negative-lookbehind-assertions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_ZeroWidthNegativeLookbehindAssertions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            const string Pattern = @"(?<!(Saturday|Sunday) )\b\w+ \d{1,2}, \d{4}\b";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Assert.Equal("February 1, 2010", r.Match("Monday February 1, 2010").Value);
            Assert.Equal("February 3, 2010", r.Match("Wednesday February 3, 2010").Value);
            Assert.False(r.IsMatch("Saturday February 6, 2010"));
            Assert.False(r.IsMatch("Sunday February 7, 2010"));
            Assert.Equal("February 8, 2010", r.Match("Monday, February 8, 2010").Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#nonbacktracking-subexpressions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_NonbacktrackingSubexpressions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // atomic groups not supported
                return;
            }

            Regex rBack = await RegexHelpers.GetRegexAsync(engine, @"(\w)\1+.\b");
            Regex rNoBack = await RegexHelpers.GetRegexAsync(engine, @"(?>(\w)\1+).\b");
            Match back, noback;

            back = rBack.Match("cccd.");
            noback = rNoBack.Match("cccd.");
            Assert.True(back.Success);
            Assert.True(noback.Success);
            Assert.Equal("cccd", back.Value);
            Assert.Equal("cccd", noback.Value);

            back = rBack.Match("aaad");
            noback = rNoBack.Match("aaad");
            Assert.True(back.Success);
            Assert.True(noback.Success);
            Assert.Equal("aaad", back.Value);
            Assert.Equal("aaad", noback.Value);

            back = rBack.Match("aaaa");
            noback = rNoBack.Match("aaaa");
            Assert.True(back.Success);
            Assert.False(noback.Success);
            Assert.Equal("aaaa", back.Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/grouping-constructs-in-regular-expressions#grouping-constructs-and-regular-expression-objects
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_GroupingConstructs_GroupCaptureRelationship(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // multiple captures not supported
                return;
            }

            const string Pattern = @"(\b(\w+)\W+)+";
            const string Input = "This is a short sentence.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Match match = r.Match(Input);

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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Capture_Sentences(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // multiple captures not supported
                return;
            }

            const string Pattern = @"((\w+)[\s.])+";
            const string Input = "Yes. This dog is very friendly.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            var actual = new StringBuilder();
            foreach (Match match in r.Matches(Input))
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Capture_ProductNumber(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // multiple captures not supported
                return;
            }

            const string Pattern = @"^([a-z]+)(\d+)?\.([a-z]+(\d)*)$";
            string[] values = { "AC10", "Za203.CYM", "XYZ.CoA", "ABC.x170" };

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            var actual = new StringBuilder();
            foreach (var value in values)
            {
                Match m = r.Match(value);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Backtracking_LinearComparisonWithoutBacktracking(RegexEngine engine)
        {
            const string Pattern = @"e{2}\w\b";
            const string Input = "needing a reed";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal(1, matches.Count);
            Assert.Equal("eed", matches[0].Value);
            Assert.Equal(11, matches[0].Index);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#backtracking-with-optional-quantifiers-or-alternation-constructs
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Backtracking_WithOptionalQuantifiersOrAlternationConstructs(RegexEngine engine)
        {
            const string Pattern = ".*(es)";
            const string Input = "Essential services are provided by regular expressions.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            Match m = r.Match(Input);
            Assert.True(m.Success);
            Assert.Equal("Essential services are provided by regular expres", m.Value);
            Assert.Equal(0, m.Index);
            Assert.Equal(47, m.Groups[1].Index);

            Assert.False(m.NextMatch().Success);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#nonbacktracking-subexpression
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Backtracking_WithNestedOptionalQuantifiers_BacktrackingEliminated(RegexEngine engine)
        {
            const string Input = "b51:4:1DB:9EE1:5:27d60:f44:D4:cd:E:5:0A5:4a:D24:41Ad:";

            Regex r = await RegexHelpers.GetRegexAsync(engine, engine == RegexEngine.NonBacktracking ?
                "^(([0-9a-fA-F]{1,4}:)*([0-9a-fA-F]{1,4}))*(::)$" : // Using RegexOptions.NonBacktracking to avoid backtracking
                "^((?>[0-9a-fA-F]{1,4}:)*(?>[0-9a-fA-F]{1,4}))*(::)$"); // Using atomic to avoid backtracking

            Assert.False(r.IsMatch(Input));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#lookbehind-assertions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Backtracking_LookbehindAssertions(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            const string Input = "test@contoso.com";

            Regex rPattern = await RegexHelpers.GetRegexAsync(engine, @"^[0-9A-Z]([-.\w]*[0-9A-Z])?@", RegexOptions.IgnoreCase);
            Assert.True(rPattern.IsMatch(Input));

            Regex rBehindPattern = await RegexHelpers.GetRegexAsync(engine, @"^[0-9A-Z][-.\w]*(?<=[0-9A-Z])@", RegexOptions.IgnoreCase);
            Assert.True(rBehindPattern.IsMatch(Input));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#lookahead-assertions
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Theory]
        [InlineData(RegexEngine.NonBacktracking)]
        public async Task Docs_Backtracking_LookaheadAssertions_ExcessiveBacktracking(RegexEngine engine)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^(([A-Z]\w*)+\.)*[A-Z]\w*$", RegexOptions.IgnoreCase);
            Assert.False(r.IsMatch("aaaaaaaaaaaaaaaaaaaaaa."));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/backtracking-in-regular-expressions#lookahead-assertions
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Backtracking_LookaheadAssertions_BacktrackingEliminated(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^((?=[A-Z])\w+\.)*[A-Z]\w*$", RegexOptions.IgnoreCase);
            Assert.False(r.IsMatch("aaaaaaaaaaaaaaaaaaaaaa."));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_LazyQuantifiers(RegexEngine engine)
        {
            const string Input = "This sentence ends with the number 107325.";

            Regex rGreedy = await RegexHelpers.GetRegexAsync(engine, @".+(\d+)\.");
            Match match = rGreedy.Match(Input);
            Assert.True(match.Success);
            Assert.Equal("5", match.Groups[1].Value);

            Regex rLazy = await RegexHelpers.GetRegexAsync(engine, @".+?(\d+)\.");
            match = rLazy.Match(Input);
            Assert.True(match.Success);
            Assert.Equal("107325", match.Groups[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_PositiveLookahead(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            const string Pattern = @"\b[A-Z]+\b(?=\P{P})";
            const string Input = "If so, what comes next?";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal(3, matches.Count);
            Assert.Equal("If", matches[0].Value);
            Assert.Equal("what", matches[1].Value);
            Assert.Equal("comes", matches[2].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_NegativeLookahead(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookaheads not supported
                return;
            }

            const string Pattern = @"\b(?!non)\w+\b";
            const string Input = "Nonsense is not always non-functional.";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal(4, matches.Count);
            Assert.Equal("is", matches[0].Value);
            Assert.Equal("not", matches[1].Value);
            Assert.Equal("always", matches[2].Value);
            Assert.Equal("functional", matches[3].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/alternation-constructs-in-regular-expressions#conditional-matching-with-an-expression
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_ConditionalEvaluation(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // conditionals not supported
                return;
            }

            const string Pattern = @"\b(?(\d{2}-)\d{2}-\d{7}|\d{3}-\d{2}-\d{4})\b";
            const string Input = "01-9999999 020-333333 777-88-9999";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            MatchCollection matches = r.Matches(Input);
            Assert.Equal(2, matches.Count);

            Assert.Equal("01-9999999", matches[0].Value);
            Assert.Equal(0, matches[0].Index);

            Assert.Equal("777-88-9999", matches[1].Value);
            Assert.Equal(22, matches[1].Index);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_RightToLeftMatching(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // RightToLeft not supported
                return;
            }

            const string GreedyPattern = @".+(\d+)\.";
            const string Input = "This sentence ends with the number 107325.";

            Regex rLTR = await RegexHelpers.GetRegexAsync(engine, GreedyPattern);
            Regex rRTL = await RegexHelpers.GetRegexAsync(engine, GreedyPattern, RegexOptions.RightToLeft);

            // Match from left-to-right using lazy quantifier .+?.
            Match match = rLTR.Match(Input);
            Assert.True(match.Success);
            Assert.Equal("5", match.Groups[1].Value);

            // Match from right-to-left using greedy quantifier .+.
            match = rRTL.Match(Input);
            Assert.True(match.Success);
            Assert.Equal("107325", match.Groups[1].Value);
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/details-of-regular-expression-behavior#net-framework-engine-capabilities
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EngineCapabilities_PositiveNegativeLookbehind(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // lookbehinds not supported
                return;
            }

            const string Pattern = @"^[A-Z0-9]([-!#$%&'.*+/=?^`{}|~\w])*(?<=[A-Z0-9])$";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern, RegexOptions.IgnoreCase);

            Assert.True(r.IsMatch("jack.sprat"));
            Assert.False(r.IsMatch("dog#"));
            Assert.True(r.IsMatch("dog#1"));
            Assert.True(r.IsMatch("me.myself"));
            Assert.False(r.IsMatch("me.myself!"));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/miscellaneous-constructs-in-regular-expressions#inline-options
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_InlineOptions(RegexEngine engine)
        {
            const string Input = "double dare double Double a Drooling dog The Dreaded Deep";

            var actual = new StringBuilder();

            foreach (Match match in (await RegexHelpers.GetRegexAsync(engine, @"\b(D\w+)\s(d\w+)\b")).Matches(Input))
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

            foreach (Match match in (await RegexHelpers.GetRegexAsync(engine, @"\b(D\w+)(?ixn) \s (d\w+) \b")).Matches(Input))
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_InlineComment(RegexEngine engine)
        {
            const string Pattern = @"\b((?# case-sensitive comparison)D\w+)\s(?ixn)((?#case-insensitive comparison)d\w+)\b";
            const string Input = "double dare double Double a Drooling dog The Dreaded Deep";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Match match = r.Match(Input);
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
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_EndOfLineComment(RegexEngine engine)
        {
            const string Pattern = @"\{\d+(,-*\d+)*(\:\w{1,4}?)*\}(?x) # Looks for a composite format item.";
            const string Input = "{0,-3:F}";

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Assert.True(r.IsMatch(Input));
        }

        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/anchors-in-regular-expressions#contiguous-matches-g
        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Docs_Anchors_ContiguousMatches(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // contiguous matches (\G) not supported
                return;
            }

            const string Input = "capybara,squirrel,chipmunk,porcupine";
            const string Pattern = @"\G(\w+\s?\w*),?";
            string[] expected = new[] { "capybara", "squirrel", "chipmunk", "porcupine" };

            Regex r = await RegexHelpers.GetRegexAsync(engine, Pattern);

            Match m = r.Match(Input);

            string[] actual = new string[4];
            for (int i = 0; i < actual.Length; i++)
            {
                Assert.True(m.Success);
                actual[i] = m.Groups[1].Value;
                m = m.NextMatch();
            }
            Assert.False(m.Success);
            Assert.Equal(expected, actual);

            Assert.Equal(
                ",arabypac,lerriuqs,knumpihcenipucrop",
                Regex.Replace(Input, Pattern, m => string.Concat(m.Value.Reverse())));
        }

        //
        // Based on examples from https://blog.stevenlevithan.com/archives/balancing-groups
        //

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Blog_Levithan_BalancingGroups_Palindromes(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // balancing groups not supported
                return;
            }

            Regex r = await RegexHelpers.GetRegexAsync(engine, @"(?<N>.)+.?(?<-N>\k<N>)+(?(N)(?!))");

            // Palindromes
            Assert.All(new[]
            {
                "kayak",
                "racecar",
                "never odd or even",
                "madam im adam"
            }, p => Assert.True(r.IsMatch(p)));

            // Non-Palindromes
            Assert.All(new[]
            {
                "canoe",
                "raceboat"
            }, p => Assert.False(r.IsMatch(p)));
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Blog_Levithan_BalancingGroups_MatchingParentheses(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // balancing groups not supported
                return;
            }

            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^\(
                                                                     (?>
                                                                         [^()]+
                                                                     |
                                                                         \( (?<Depth>)
                                                                     |
                                                                         \) (?<-Depth>)
                                                                     )*
                                                                     (?(Depth)(?!))
                                                                 \)$", RegexOptions.IgnorePatternWhitespace);

            Assert.True(r.IsMatch("()"));
            Assert.True(r.IsMatch("(a(b c(de(f(g)hijkl))mn))"));

            Assert.False(r.IsMatch("("));
            Assert.False(r.IsMatch(")"));
            Assert.False(r.IsMatch("())"));
            Assert.False(r.IsMatch("(()"));
            Assert.False(r.IsMatch("(ab(cd)ef"));
        }

        [Theory]
        [MemberData(nameof(RegexHelpers.AvailableEngines_MemberData), MemberType = typeof(RegexHelpers))]
        public async Task Blog_Levithan_BalancingGroups_WordLengthIncreases(RegexEngine engine)
        {
            if (RegexHelpers.IsNonBacktracking(engine))
            {
                // balancing groups not supported
                return;
            }

            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^(?:
                                                                     (?(A)\s|)
                                                                     (?<B>)
                                                                     (?<C-B>\w)+ (?(B)(?!))
                                                                     (?:
                                                                         \s
                                                                         (?<C>)
                                                                         (?<B-C>\w)+ (?(C)(?!))
                                                                         (?<A>)
                                                                     )?
                                                                 )+ \b$", RegexOptions.IgnorePatternWhitespace);

            Assert.True(r.IsMatch("a bc def ghij klmni"));
            Assert.False(r.IsMatch("a bc def ghi klmn"));
        }

        //
        // These patterns come from real-world customer usages
        //

        public static IEnumerable<object[]> RealWorld_ExtractResourceUri_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "https://foo.com:443/bar/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip" };
                yield return new object[] { engine, "ftp://443/notproviders/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip" };
                yield return new object[] { engine, "ftp://443/providersnot/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip" };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_ExtractResourceUri_MemberData))]
        public async Task RealWorld_ExtractResourceUri(RegexEngine engine, string url, string expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"/providers/(.+?)\?");
            Match m = r.Match(url);
            Assert.True(m.Success);
            Assert.Equal(2, m.Groups.Count);
            Assert.Equal(expected, m.Groups[1].Value);
        }

        public static IEnumerable<object[]> RealWorld_IsValidCSharpName_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "IsValidCSharpName", true };
                yield return new object[] { engine, "_IsValidCSharpName", true };
                yield return new object[] { engine, "__", true };
                yield return new object[] { engine, "a\u2169", true  }; // \u2169 is in {Nl}
                yield return new object[] { engine, "\u2169b", true  }; // \u2169 is in {Nl}
                yield return new object[] { engine, "a\u0600", true  }; // \u0600 is in {Cf}
                yield return new object[] { engine, "\u0600b", false }; // \u0600 is in {Cf}
                yield return new object[] { engine, "a\u0300", true  }; // \u0300 is in {Mn}
                yield return new object[] { engine, "\u0300b", false }; // \u0300 is in {Mn}
                yield return new object[] { engine, "https://foo.com:443/bar/17/groups/0ad1/providers/Network/public/4e-ip?version=16", false };
                yield return new object[] { engine, "david.jones@proseware.com", false };
                yield return new object[] { engine, "~david", false };
                yield return new object[] { engine, "david~", false };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_IsValidCSharpName_MemberData))]
        public async Task RealWorld_IsValidCSharpName(RegexEngine engine, string value, bool isExpectedMatch)
        {
            const string StartCharacterRegex = @"_|[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}]";
            const string PartCharactersRegex = @"[\p{Lu}\p{Ll}\p{Lt}\p{Lm}\p{Lo}\p{Nl}\p{Mn}\p{Mc}\p{Nd}\p{Pc}\p{Cf}]";
            const string IdentifierRegex = @"^(" + StartCharacterRegex + ")(" + PartCharactersRegex + ")*$";

            Regex r = await RegexHelpers.GetRegexAsync(engine, IdentifierRegex);
            Assert.Equal(isExpectedMatch, r.IsMatch(value));
        }

        public static IEnumerable<object[]> RealWorld_IsCommentLine_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "; this is a comment", true };
                yield return new object[] { engine, "\t; so is this", true };
                yield return new object[] { engine, "  ; and this", true };
                yield return new object[] { engine, ";", true };
                yield return new object[] { engine, ";comment\nNotThisBecauseOfNewLine", false };
                yield return new object[] { engine, "-;not a comment", false };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_IsCommentLine_MemberData))]
        public async Task RealWorld_IsCommentLine(RegexEngine engine, string value, bool isExpectedMatch)
        {
            const string CommentLineRegex = @"^\s*;\s*(.*?)\s*$";

            Regex r = await RegexHelpers.GetRegexAsync(engine, CommentLineRegex);
            Assert.Equal(isExpectedMatch, r.IsMatch(value));
        }

        public static IEnumerable<object[]> RealWorld_IsSectionLine_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "[ThisIsASection]", true };
                yield return new object[] { engine, " [ThisIsASection] ", true };
                yield return new object[] { engine, "\t[ThisIs\\ASection]\t", true };
                yield return new object[] { engine, "\t[This.Is:(A+Section)]\t", true };
                yield return new object[] { engine, "[This Is Not]", false };
                yield return new object[] { engine, "This is not[]", false };
                yield return new object[] { engine, "[Nor This]/", false };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_IsSectionLine_MemberData))]
        public async Task RealWorld_IsSectionLine(RegexEngine engine, string value, bool isExpectedMatch)
        {
            const string SectionLineRegex = @"^\s*\[([\w\.\-\+:\/\(\)\\]+)\]\s*$";

            Regex r = await RegexHelpers.GetRegexAsync(engine, SectionLineRegex);
            Assert.Equal(isExpectedMatch, r.IsMatch(value));
        }

        public static IEnumerable<object[]> RealWorld_ValueParse_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "Jiri: 10", "10" };
                yield return new object[] { engine, "jiri: -10.01", "-10.01" };
                yield return new object[] { engine, "jiri: .-22", "-22" };
                yield return new object[] { engine, "jiri: .-22.3", "-22.3" };
                yield return new object[] { engine, "foo15.0", "15.0" };
                yield return new object[] { engine, "foo15", "15" };
                yield return new object[] { engine, "foo16bar", "16" };
                yield return new object[] { engine, "fds:-4", "-4" };
                yield return new object[] { engine, "dsa:-20.04", "-20.04" };
                yield return new object[] { engine, "dsa:15.a", "15" };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_ValueParse_MemberData))]
        public async Task RealWorld_ValueParse(RegexEngine engine, string value, string expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"(?<value>-?\d+(\.\d+)?)");
            Match m = r.Match(value);
            Assert.True(m.Success);
            Assert.Equal(expected, m.Groups["value"].Value);
        }

        public static IEnumerable<object[]> RealWorld_FirebirdVersionString_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "WI-T4.0.0.1963 Firebird 4.0 Beta 2", "4.0.0.1963" };
                yield return new object[] { engine, "WI-V3.0.5.33220 Firebird 3.0", "3.0.5.33220" };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_FirebirdVersionString_MemberData))]
        public async Task RealWorld_FirebirdVersionString(RegexEngine engine, string value, string expected)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"\w{2}-\w(\d+\.\d+\.\d+\.\d+)");
            Match m = r.Match(value);
            Assert.True(m.Success);
            Assert.Equal(expected, m.Groups[1].Value);
        }

        public static IEnumerable<object[]> RealWorld_ExternalEntryPoint_MemberData()
        {
            foreach (RegexEngine engine in RegexHelpers.AvailableEngines)
            {
                yield return new object[] { engine, "Foo!Bar.M", "Foo", "Bar", "M" };
                yield return new object[] { engine, "Foo!Bar.A.B.C", "Foo", "Bar.A.B", "C" };
                yield return new object[] { engine, "Foo1.Foo2.Foo!Bar.A.B.C", "Foo1.Foo2.Foo", "Bar.A.B", "C" };
                yield return new object[] { engine, @"Foo1\Foo2.Foo!Bar.A.B.C", @"Foo1\Foo2.Foo", "Bar.A.B", "C" };
            }
        }

        [Theory]
        [MemberData(nameof(RealWorld_ExternalEntryPoint_MemberData))]
        public async Task RealWorld_ExternalEntryPoint(RegexEngine engine, string value, string a, string b, string c)
        {
            Regex r = await RegexHelpers.GetRegexAsync(engine, @"^(.+)!(.+)\.([^.]+)$");
            Match m = r.Match(value);
            Assert.True(m.Success);
            Assert.Equal(a, m.Groups[1].Value);
            Assert.Equal(b, m.Groups[2].Value);
            Assert.Equal(c, m.Groups[3].Value);
        }

        /// <summary>
        /// Test that these well-known patterns that are hard for backtracking engines
        /// are not a problem with NonBacktracking.
        /// </summary>
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Theory]
        [InlineData("((?:0*)+?(?:.*)+?)?", "0a", 2)]
        [InlineData("(?:(?:0?)+?(?:a?)+?)?", "0a", 2)]
        [InlineData(@"(?i:(\()((?<a>\w+(\.\w+)*)(,(?<a>\w+(\.\w+)*)*)?)(\)))", "some.text(this.is,the.match)", 1)]
        private void DifficultForBacktracking(string pattern, string input, int matchcount)
        {
            var regex = new Regex(pattern, RegexHelpers.RegexOptionNonBacktracking);
            List<Match> matches = new List<Match>();
            var match = regex.Match(input);
            while (match.Success)
            {
                matches.Add(match);
                match = match.NextMatch();
            }
            Assert.Equal(matchcount, matches.Count);
        }

        /// <summary>
        /// Another difficult pattern in backtracking that is fast in NonBacktracking.
        /// </summary>
        [SkipOnTargetFramework(TargetFrameworkMonikers.NetFramework, "Doesn't support NonBacktracking")]
        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void TerminationInNonBacktrackingVsBackTracking(RegexOptions options)
        {
            string input = " 123456789 123456789 123456789 123456789 123456789";
            for (int i = 0; i < 12; i++)
            {
                input += input;
            }

            // The input has 2^12 * 50 = 204800 characters
            string rawregex = @"[\\/]?[^\\/]*?(heythere|hej)[^\\/]*?$";

            // It takes over 4min with backtracking, so it should certainly timeout given a 1 second timeout
            Regex reC = new Regex(rawregex, options, TimeSpan.FromSeconds(1));
            Assert.Throws<RegexMatchTimeoutException>(() => { reC.Match(input); });

            // NonBacktracking needs way less than 1s, but use 10s to account for the slowest possible CI machine
            Regex re = new Regex(rawregex, RegexHelpers.RegexOptionNonBacktracking, TimeSpan.FromSeconds(10));
            Assert.False(re.Match(input).Success);
        }

        //
        // dotnet/runtime-assets contains a set a regular expressions sourced from
        // permissively-licensed packages.  Validate Regex behavior with those expressions.
        //

        [Theory]
        [InlineData(RegexEngine.Interpreter)]
        [InlineData(RegexEngine.Compiled)]
        public async Task PatternsDataSet_ConstructRegexForAll(RegexEngine engine)
        {
            foreach (DataSetExpression exp in s_patternsDataSet.Value)
            {
                await RegexHelpers.GetRegexAsync(engine, exp.Pattern, exp.Options);
            }
        }

        private static Lazy<DataSetExpression[]> s_patternsDataSet = new Lazy<DataSetExpression[]>(() =>
        {
            using Stream json = File.OpenRead("Regex_RealWorldPatterns.json");
            return JsonSerializer.Deserialize<DataSetExpression[]>(json, new JsonSerializerOptions() { ReadCommentHandling = JsonCommentHandling.Skip }).Distinct().ToArray();
        });

        private sealed class DataSetExpression : IEquatable<DataSetExpression>
        {
            public int Count { get; set; }
            public RegexOptions Options { get; set; }
            public string Pattern { get; set; }

            public bool Equals(DataSetExpression? other) =>
                other is not null &&
                other.Pattern == Pattern &&
                (Options & ~RegexOptions.Compiled) == (other.Options & ~RegexOptions.Compiled); // Compiled doesn't affect semantics, so remove it from equality for our purposes
        }

#if NETCOREAPP
        [OuterLoop("Takes many seconds")]
        [Fact]
        public async Task PatternsDataSet_ConstructRegexForAll_NonBacktracking()
        {
            foreach (DataSetExpression exp in s_patternsDataSet.Value)
            {
                if ((exp.Options & (RegexOptions.ECMAScript | RegexOptions.RightToLeft)) != 0)
                {
                    // Unsupported options with NonBacktracking
                    continue;
                }

                try
                {
                    await RegexHelpers.GetRegexAsync(RegexEngine.NonBacktracking, exp.Pattern, exp.Options);
                }
                catch (NotSupportedException e) when (e.Message.Contains(nameof(RegexOptions.NonBacktracking)))
                {
                    // Unsupported patterns
                }
            }
        }

        [OuterLoop("Takes minutes to generate and compile thousands of expressions")]
        [ConditionalFact(typeof(PlatformDetection), nameof(PlatformDetection.Is64BitProcess))] // consumes a lot of memory
        public void PatternsDataSet_ConstructRegexForAll_SourceGenerated()
        {
            Parallel.ForEach(s_patternsDataSet.Value.Chunk(50), chunk =>
            {
                RegexHelpers.GetRegexesAsync(RegexEngine.SourceGenerated,
                    chunk.Select(r => (r.Pattern, (RegexOptions?)r.Options, (TimeSpan?)null)).ToArray()).GetAwaiter().GetResult();
            });
        }
#endif
    }
}
