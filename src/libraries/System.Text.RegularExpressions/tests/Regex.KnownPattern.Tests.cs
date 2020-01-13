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
    }
}
