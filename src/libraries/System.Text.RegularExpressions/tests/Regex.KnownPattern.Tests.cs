// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using Xunit;

namespace System.Text.RegularExpressions.Tests
{
    public class RegexKnownPatternTests
    {
        // These come from the regex docs:
        // https://docs.microsoft.com/en-us/dotnet/standard/base-types/regular-expression-examples

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void ScanningHrefs(RegexOptions options)
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

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void MDYtoDMY(RegexOptions options)
        {
            string dt = new DateTime(2020, 1, 8, 0, 0, 0, DateTimeKind.Utc).ToString("d", DateTimeFormatInfo.InvariantInfo);
            string result = Regex.Replace(dt, @"\b(?<month>\d{1,2})/(?<day>\d{1,2})/(?<year>\d{2,4})\b", "${day}-${month}-${year}", options);
            Assert.Equal("08-01-2020", result);
        }

        [Theory]
        [InlineData(RegexOptions.None)]
        [InlineData(RegexOptions.Compiled)]
        public void ExtractProtocolPort(RegexOptions options)
        {
            string url = "http://www.contoso.com:8080/letters/readme.html";
            Regex r = new Regex(@"^(?<proto>\w+)://[^/]+?(?<port>:\d+)?/", options);
            Match m = r.Match(url);
            Assert.True(m.Success);
            Assert.Equal("http:8080", m.Result("${proto}${port}"));
        }

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
        public void ValidateEmail(string email, bool expectedIsValid)
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

        // These patterns come from real-world customer usages

        [Theory]
        [InlineData("https://foo.com:443/bar/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        [InlineData("ftp://443/notproviders/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        [InlineData("ftp://443/providersnot/17/groups/0ad1/providers/Network/public/4e-ip?version=16", "Network/public/4e-ip")]
        public void ExtractResourceUri(string url, string expected)
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
        public void IsValidCSharpName(string value, bool isExpectedMatch)
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
        public void IsCommentLine(string value, bool isExpectedMatch)
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
        public void IsSectionLine(string value, bool isExpectedMatch)
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
