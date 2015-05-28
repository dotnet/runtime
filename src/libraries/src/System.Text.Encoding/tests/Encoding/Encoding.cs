// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;
using Xunit;

namespace System.Text.EncodingTests
{
    public class EncodingTest
    {
        private static byte[] s_UTF32LEBom = new byte[] { 0xFF, 0xFE, 0x0, 0x0 };
        private static byte[] s_UTF32BEBom = new byte[] { 0x0, 0x0, 0xFE, 0xFF, };
        private static byte[] s_UTF8Bom = new byte[] { 0xEF, 0xBB, 0xBF };
        private static byte[] s_UTF16LEBom = new byte[] { 0xFF, 0xFE };
        private static byte[] s_UTF8BEBom = new byte[] { 0xFE, 0xFF };

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void TestGetEncoding()
        {
            Encoding encoding = Encoding.GetEncoding("UTF-32LE");
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF32LEBom);

            encoding = Encoding.UTF32;
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF32LEBom);

            encoding = Encoding.GetEncoding("UTF-32BE");
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF32BEBom);

            encoding = Encoding.UTF8;
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF8Bom);

            encoding = Encoding.GetEncoding("UTF-16BE");
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF8BEBom);

            encoding = Encoding.GetEncoding("UTF-16LE");
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF16LEBom);

            encoding = Encoding.Unicode;
            Assert.Equal<byte>(encoding.GetPreamble(), s_UTF16LEBom);
        }

        private static byte[] s_asciiBytes = new byte[] { (byte)'A', (byte)'B', (byte)'C', (byte)'D', (byte)'E', (byte)'F', (byte)'G', (byte)'H', };
        private static string s_asciiString = "ABCDEFGH";

        [Fact]
        public static void TestEncodingDecoding()
        {
            Encoding encoding = Encoding.ASCII;
            byte[] bytes = encoding.GetBytes(s_asciiString);
            Assert.Equal<byte>(bytes, s_asciiBytes);
            string s = encoding.GetString(bytes, 0, bytes.Length);
            Assert.True(s.Equals(s_asciiString));
            s = encoding.GetString(bytes);
            Assert.True(s.Equals(s_asciiString));

            encoding = Encoding.GetEncoding("us-ascii");
            bytes = encoding.GetBytes(s_asciiString);
            Assert.Equal<byte>(bytes, s_asciiBytes);
            s = encoding.GetString(bytes, 0, bytes.Length);
            Assert.True(s.Equals(s_asciiString));
            s = encoding.GetString(bytes);
            Assert.True(s.Equals(s_asciiString));

            encoding = Encoding.GetEncoding("latin1");
            bytes = encoding.GetBytes(s_asciiString);
            Assert.Equal<byte>(bytes, s_asciiBytes);
            s = encoding.GetString(bytes, 0, bytes.Length);
            Assert.True(s.Equals(s_asciiString));
            s = encoding.GetString(bytes);
            Assert.True(s.Equals(s_asciiString));
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

        private static CodePageMapping[] s_mapping = new CodePageMapping[] {
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

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void TestEncodingNameAndCopdepageNumber()
        {
            foreach (var map in s_mapping)
            {
                Encoding encoding = Encoding.GetEncoding(map.Name);
                Assert.True(encoding.CodePage == map.CodePage);
            }
        }

        [Fact]
        public static void TestEncodingDisplayNames()
        {
            CultureInfo originalUICulture = CultureInfo.CurrentUICulture;
            try
            {
                CultureInfo.CurrentCulture = new CultureInfo("en-US");

                foreach (var map in s_mapping)
                {
                    Encoding encoding = Encoding.GetEncoding(map.Name);

                    string name = encoding.EncodingName;

                    if (string.IsNullOrEmpty(name))
                    {
                        Assert.False(true, "failed to get the display name of the encoding: " + encoding.WebName);
                    }

                    for (int i = 0; i < name.Length; ++i)
                    {
                        int ch = (int)name[i];

                        if (ch > 127)
                        {
                            Assert.False(true,
                                string.Format("English name '{0}' for encoding '{1}' contains non-ASCII character: {2}",
                                    name, encoding.WebName, ch));
                        }
                    }
                }
            }
            finally
            {
                CultureInfo.CurrentUICulture = originalUICulture;
            }
        }

        [Fact]
        [ActiveIssue(846, PlatformID.AnyUnix)]
        public static void TestCodePageToWebNameMappings()
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
                _codePage = codePage;
                _webName = webName;
            }

            public int CodePage { get { return _codePage; } }
            public string WebName { get { return _webName; } }

            private int _codePage;
            private string _webName;
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
