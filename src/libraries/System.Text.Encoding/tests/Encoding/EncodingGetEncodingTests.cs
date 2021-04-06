// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Tests;
using Microsoft.DotNet.RemoteExecutor;
using Xunit;

namespace System.Text.Tests
{
    public class EncodingGetEncodingTest
    {
        [Fact]
        public void GetEncoding_String_Invalid()
        {
            AssertExtensions.Throws<ArgumentNullException>("name", () => Encoding.GetEncoding(null));
            AssertExtensions.Throws<ArgumentException>("name", () => Encoding.GetEncoding("no-such-encoding-name"));
        }

        [Fact]
        public void GetEncoding_Int_Invalid()
        {
            // Codepage is out of range
            AssertExtensions.Throws<ArgumentOutOfRangeException>("codepage", () => Encoding.GetEncoding(-1));
            AssertExtensions.Throws<ArgumentOutOfRangeException>("codepage", () => Encoding.GetEncoding(65536));

            // Codepage doesn't exist
            AssertExtensions.Throws<ArgumentException>("codepage", () => Encoding.GetEncoding(42));
            Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding(54321));
        }

        public class CodePageMapping
        {
            public CodePageMapping(string name, int codepage)
            {
                Name = name;
                CodePage = codepage;
            }

            public string Name { set; get; }
            public int CodePage { set; get; }
        }

        private static CodePageMapping[] s_mapping = new CodePageMapping[]
        {
            new CodePageMapping("ANSI_X3.4-1968", 20127 ),
            new CodePageMapping("ANSI_X3.4-1986", 20127 ),
            new CodePageMapping("ascii", 20127 ),
            new CodePageMapping("cp367", 20127 ),
            new CodePageMapping("cp819", 28591 ),
            new CodePageMapping("csASCII", 20127 ),
            new CodePageMapping("csISOLatin1", 28591 ),
            new CodePageMapping("csUnicode11UTF7", 65000 ),
            new CodePageMapping("IBM367", 20127 ),
            new CodePageMapping("ibm819", 28591 ),
            new CodePageMapping("ISO-10646-UCS-2", 1200),
            new CodePageMapping("iso-8859-1", 28591),
            new CodePageMapping("iso-ir-100", 28591),
            new CodePageMapping("iso-ir-6", 20127),
            new CodePageMapping("ISO646-US", 20127),
            new CodePageMapping("iso8859-1", 28591),
            new CodePageMapping("ISO_646.irv:1991", 20127),
            new CodePageMapping("iso_8859-1", 28591),
            new CodePageMapping("iso_8859-1:1987", 28591),
            new CodePageMapping("l1", 28591),
            new CodePageMapping("latin1", 28591),
            new CodePageMapping("ucs-2", 1200),
            new CodePageMapping("unicode", 1200),
            new CodePageMapping("unicode-1-1-utf-7", 65000),
            new CodePageMapping("unicode-1-1-utf-8", 65001),
            new CodePageMapping("unicode-2-0-utf-7", 65000),
            new CodePageMapping("unicode-2-0-utf-8", 65001),
            new CodePageMapping("unicodeFFFE", 1201),
            new CodePageMapping("us", 20127),
            new CodePageMapping("us-ascii", 20127),
            new CodePageMapping("utf-16", 1200),
            new CodePageMapping("UTF-16BE", 1201),
            new CodePageMapping("UTF-16LE", 1200),
            new CodePageMapping("utf-32", 12000),
            new CodePageMapping("UTF-32BE", 12001),
            new CodePageMapping("UTF-32LE", 12000),
            new CodePageMapping("utf-7", 65000),
            new CodePageMapping("utf-8", 65001),
            new CodePageMapping("x-unicode-1-1-utf-7", 65000),
            new CodePageMapping("x-unicode-1-1-utf-8", 65001),
            new CodePageMapping("x-unicode-2-0-utf-7", 65000),
            new CodePageMapping("x-unicode-2-0-utf-8", 65001)
        };

        [ActiveIssue("https://github.com/dotnet/runtime/issues/38433", TestPlatforms.Browser)] // wasm doesn't honor runtimeconfig.json
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50573", TestPlatforms.Android)]
        public void TestEncodingNameAndCopdepageNumber()
        {
            foreach (var map in s_mapping)
            {
                Encoding encoding = Encoding.GetEncoding(map.Name);
                Assert.True(encoding.CodePage == map.CodePage);
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/38433", TestPlatforms.Browser)] // wasm doesn't honor runtimeconfig.json
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50573", TestPlatforms.Android)]
        public void GetEncoding_EncodingName()
        {
            using (new ThreadCultureChange(CultureInfo.InvariantCulture))
            {
                foreach (CodePageMapping map in s_mapping)
                {
                    Encoding encoding = Encoding.GetEncoding(map.Name);

                    string name = encoding.EncodingName;

                    Assert.NotNull(name);
                    Assert.NotEqual(string.Empty, name);

                    Assert.All(name, ch => Assert.InRange(ch, 0, 127));
                }
            }
        }

        [ActiveIssue("https://github.com/dotnet/runtime/issues/38433", TestPlatforms.Browser)] // wasm doesn't honor runtimeconfig.json
        [Fact]
        [ActiveIssue("https://github.com/dotnet/runtime/issues/50573", TestPlatforms.Android)]
        public void GetEncoding_WebName()
        {
            foreach (var mapping in s_codePageToWebNameMappings)
            {
                Encoding encoding = Encoding.GetEncoding(mapping.CodePage);
                Assert.True(string.Equals(mapping.WebName, encoding.WebName, StringComparison.OrdinalIgnoreCase));
            }
        }

        internal class CodePageToWebNameMapping
        {
            public CodePageToWebNameMapping(int codePage, string webName)
            {
                CodePage = codePage;
                WebName = webName;
            }

            public int CodePage { get; }
            public string WebName { get; }
        }

        private static readonly CodePageToWebNameMapping[] s_codePageToWebNameMappings = new[]
        {
            new CodePageToWebNameMapping(1200, "utf-16"),
            new CodePageToWebNameMapping(1201, "utf-16be"),
            new CodePageToWebNameMapping(12000, "utf-32"),
            new CodePageToWebNameMapping(12001, "utf-32be"),
            new CodePageToWebNameMapping(20127, "us-ascii"),
            new CodePageToWebNameMapping(28591, "iso-8859-1"),
            new CodePageToWebNameMapping(65000, "utf-7"),
            new CodePageToWebNameMapping(65001, "utf-8")
        };
    }
}
