// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Globalization;
using System.Text;

using Xunit;

public class EncodingTest
{
    // The characters to encode:
    //    Latin Small Letter Z (U+007A)
    //    Latin Small Letter A (U+0061)
    //    Combining Breve (U+0306)
    //    Latin Small Letter AE With Acute (U+01FD)
    //    Greek Small Letter Beta (U+03B2)
    //    a high-surrogate value (U+D8FF)
    //    a low-surrogate value (U+DCFF)
    private static char[] s_myChars = new char[] { 'z', 'a', '\u0306', '\u01FD', '\u03B2', '\uD8FF', '\uDCFF' };
    private static string s_myString = new String(s_myChars);
    private static byte[] s_leBytes = new byte[] { 0x7A, 0x00, 0x61, 0x00, 0x06, 0x03, 0xFD, 0x01, 0xB2, 0x03, 0xFF, 0xD8, 0xFF, 0xDC };
    private static byte[] s_beBytes = new byte[] { 0x00, 0x7A, 0x00, 0x61, 0x03, 0x06, 0x01, 0xFD, 0x03, 0xB2, 0xD8, 0xFF, 0xDC, 0xFF };
    private static byte[] s_utf8Bytes = new byte[] { 0x7A, 0x61, 0xCC, 0x86, 0xC7, 0xBD, 0xCE, 0xB2, 0xF1, 0x8F, 0xB3, 0xBF };

    private static byte[] s_utf8PreambleBytes = new byte[] { 0xEF, 0xBB, 0xBF };
    private static byte[] s_unicodePreambleBytes = new byte[] { 0xFF, 0xFE };
    private static byte[] s_unicodeBigEndianPreambleBytes = new byte[] { 0xFE, 0xFF };

    private static bool s_IsInitialized = false;

    static internal void EnsureInitialization()
    {
        if (!s_IsInitialized)
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            s_IsInitialized = true;
        }
    }

    internal class EncodingId
    {
        internal EncodingId(string name, int codepage)
        {
            _name = name;
            _codepage = codepage;
        }

        internal string Name { get { return _name; } }
        internal int Codepage { get { return _codepage; } }

        private int _codepage;
        private string _name;
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

    [Fact]
    public static void TestDefaultCodePage()
    {

        Assert.False(s_IsInitialized);
        Encoding utf8Encoding = Encoding.GetEncoding(0);
        Assert.Equal(Encoding.UTF8.CodePage, utf8Encoding.CodePage);
        Assert.Equal(Encoding.UTF8.WebName, utf8Encoding.WebName);

        EnsureInitialization();

        Encoding encoding = Encoding.GetEncoding(0);
        Console.WriteLine("System default non-unicode code page: {0} ({1})", encoding.WebName, encoding.CodePage);
        Assert.True(encoding.CodePage != 0);
        Assert.True(encoding.WebName != Encoding.UTF8.WebName);
    }

    private static void TestRoundtrippingCodepageEncoding(string encodingName, byte[] bytes, string s)
    {
        Console.WriteLine("Testing codepage: {0}", encodingName);
        Encoding enc = Encoding.GetEncoding(encodingName);
        string resultString = enc.GetString(bytes, 0, bytes.Length);
        Assert.True(s.Equals(resultString));
        byte[] resultBytes = enc.GetBytes(resultString);

        Assert.Equal(bytes.Length, resultBytes.Length);
        for (int i = 0; i < bytes.Length; i++)
        {
            Assert.Equal<byte>(bytes[i], resultBytes[i]);
        }
    }

    [Fact]
    public static void TestSpecificCodepageEncodings()
    {
        EnsureInitialization();

        byte[] bytes = new byte[] {
            0xA0, 0xA1, 0xA2, 0xA3, 0xA4, 0xA5, 0xA6, 0xA7, 0xA8, 0xA9, 0xAA, 0xAB, 0xAC, 0xAD, 0xAE, 0xAF,
            0xC0, 0xC1, 0xC2, 0xC3, 0xC4, 0xC5, 0xC6, 0xC7, 0xC8, 0xC9, 0xCA, 0xCB, 0xCC, 0xCD, 0xCE, 0xCF
            };
        string unicodeStr = "\x00A0\x00A1\x00A2\x00A3\x00A4\x00A5\x00A6\x00A7\x00A8\x00A9\x00AA\x00AB\x00AC\x00AD\x00AE\x00AF\x00C0\x00C1\x00C2\x00C3\x00C4" +
                            "\x00C5\x00C6\x00C7\x00C8\x00C9\x00CA\x00CB\x00CC\x00CD\x00CE\x00CF";

        TestRoundtrippingCodepageEncoding("iso-8859-1", bytes, unicodeStr);

        bytes = new byte[] { 0xC7, 0xE1, 0xE1, 0xE5, 0x20, 0xC7, 0xCD, 0xCF };
        unicodeStr = "\x0627\x0644\x0644\x0647\x0020\x0627\x062D\x062F";
        TestRoundtrippingCodepageEncoding("Windows-1256", bytes, unicodeStr);

        bytes = new byte[] { 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF };
        unicodeStr = "\x00D0\x00D1\x00D2\x00D3\x00D4\x00D5\x00D6\x00D7\x00D8\x00D9\x00DA\x00DB\x00DC\x00DD\x00DE\x00DF";
        TestRoundtrippingCodepageEncoding("Windows-1252", bytes, unicodeStr);

        bytes = new byte[] { 0xCD, 0xE2, 0xCD, 0xE3, 0xCD, 0xE4 };
        unicodeStr = "\x5916\x8C4C\x5F2F";
        TestRoundtrippingCodepageEncoding("GB2312", bytes, unicodeStr);

        bytes = new byte[] { 0x81, 0x30, 0x89, 0x37, 0x81, 0x30, 0x89, 0x38, 0xA8, 0xA4, 0xA8, 0xA2, 0x81, 0x30, 0x89, 0x39, 0x81, 0x30, 0x8A, 0x30 };
        unicodeStr = "\x00DE\x00DF\x00E0\x00E1\x00E2\x00E3";
        TestRoundtrippingCodepageEncoding("GB18030", bytes, unicodeStr);

        bytes = new byte[] { 0x5B, 0x5C, 0x5D, 0x5E, 0x7B, 0x7C, 0x7D, 0x7E };
        unicodeStr = "\x005B\x005C\x005D\x005E\x007B\x007C\x007D\x007E";
        TestRoundtrippingCodepageEncoding("us-ascii", bytes, unicodeStr);
    }

    [Fact]
    public static void TestCodepageEncoding()
    {
        EnsureInitialization();

        foreach (var id in s_encodingIdTable)
        {
            // Console.WriteLine(id.Name);
            Encoding enc = Encoding.GetEncoding(id.Name);
            byte[] bytes = enc.GetBytes("Az");
            char[] chars = enc.GetChars(bytes);
            Assert.True(chars.Length == 2 && chars[0] == 'A' && chars[1] == 'z');
            // Encoding.GetEncoding(id.Codepage);
        }
    }

    [Fact]
    public static void TestDecodingIso2022()
    {
        EnsureInitialization();

        byte[] japaneseEncodedBuffer = new byte[]
            {
                0xA,
                0x1B, 0x24, 0x42, 0x25, 0x4A, 0x25, 0x4A,
                0x1B, 0x28, 0x42,
                0x1B, 0x24, 0x42, 0x25, 0x4A,
                0x1B, 0x28, 0x42,
                0x1B, 0x24, 0x42, 0x25, 0x4A,
                0x1B, 0x28, 0x42,
                0x1B, 0x1, 0x2, 0x3, 0x4,
                0x1B, 0x24, 0x42, 0x25, 0x4A, 0x0E, 0x25, 0x4A,
                0x1B, 0x28, 0x42, 0x41, 0x42, 0x0E, 0x25, 0x0F, 0x43
            };

        string codepageName = "iso-2022-jp";
        Decoder jpDecoder = Encoding.GetEncoding(codepageName).GetDecoder();
        const int BUFFER_SIZE = 100;
        char[] buffer = new char[BUFFER_SIZE];

        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < japaneseEncodedBuffer.Length)
        {
            int charCount = jpDecoder.GetChars(japaneseEncodedBuffer, byteIndex, 1, buffer, charIndex);
            if (charCount > 0)
            {
                for (int i = 0; i < charCount; i++)
                {
                    Console.Write("0x{0:X}, ", (int)buffer[charIndex + i]);
                }
                Console.WriteLine();
                charIndex += charCount;
            }
            byteIndex++;
        }

        int[] expectedResult = new int[] { 0xA, 0x30CA, 0x30CA, 0x30CA, 0x30CA, 0x1B, 0x1, 0x2, 0x3, 0x4, 0x30CA, 0xFF65,
                                                 0xFF8A, 0x41, 0x42, 0xFF65, 0x43};
        Assert.True(charIndex == expectedResult.Length);
        for (int i = 0; i < charIndex; i++)
            Assert.True(expectedResult[i] == (int)buffer[i]);
    }

    [Fact]
    public static void TestDecodingGB18030()
    {
        EnsureInitialization();

        string codepageName = "GB18030"; // 54936 codepage 

        byte[] chineseBuffer = new byte[]
            {
                0x41, 0x42, 0x43,
                0x81, 0x40,
                0x82, 0x80,
                0x81, 0x30, 0x82, 0x31,
                0x81, 0x20
            };

        Decoder chineseDecoder = Encoding.GetEncoding(codepageName).GetDecoder();
        const int BUFFER_SIZE = 100;
        char[] buffer = new char[BUFFER_SIZE];

        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < chineseBuffer.Length)
        {
            int charCount = chineseDecoder.GetChars(chineseBuffer, byteIndex, 1, buffer, charIndex);
            if (charCount > 0)
            {
                for (int i = 0; i < charCount; i++)
                {
                    Console.Write("0x{0:X}, ", (int)buffer[charIndex + i]);
                }
                Console.WriteLine();
                charIndex += charCount;
            }
            byteIndex++;
        }

        int[] expectedResult = new int[] { 0x41, 0x42, 0x43, 0x4E02, 0x500B, 0x8B, 0x3F, 0x20 };
        Assert.True(charIndex == expectedResult.Length);
        for (int i = 0; i < charIndex; i++)
            Assert.True(expectedResult[i] == (int)buffer[i]);
    }

    [Fact]
    public static void TestDecodingDBCS()
    {
        EnsureInitialization();

        string codepageName = "shift_jis"; // 932 codepage 

        byte[] shiftJapaneseBuffer = new byte[]
        {
            0x41, 0x42, 0x43,   // Single byte
            0x81, 0x42,         // Double bytes
            0xE0, 0x43,         // Double bytes
            0x44, 0x45          // Single byte
        };

        Decoder jpDecoder = Encoding.GetEncoding(codepageName).GetDecoder();
        const int BUFFER_SIZE = 100;
        char[] buffer = new char[BUFFER_SIZE];

        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < shiftJapaneseBuffer.Length)
        {
            int charCount = jpDecoder.GetChars(shiftJapaneseBuffer, byteIndex, 1, buffer, charIndex);
            if (charCount > 0)
            {
                for (int i = 0; i < charCount; i++)
                {
                    Console.Write("0x{0:X}, ", (int)buffer[charIndex + i]);
                }
                Console.WriteLine();
                charIndex += charCount;
            }
            byteIndex++;
        }

        int[] expectedResult = new int[] { 0x41, 0x42, 0x43, 0x3002, 0x6F86, 0x44, 0x45 };
        Assert.True(charIndex == expectedResult.Length);
        for (int i = 0; i < charIndex; i++)
            Assert.True(expectedResult[i] == (int)buffer[i]);
    }

    [Fact]
    public static void TestDecodingIso2022KR()
    {
        EnsureInitialization();

        byte[] koreanEncodedBuffer = new byte[]
            {
                0x0E, 0x21, 0x7E,
                0x1B, 0x24, 0x29, 0x43, 0x21, 0x7E,
                0x0F, 0x21, 0x7E,
                0x1B, 0x24, 0x29, 0x43, 0x21, 0x7E
            };

        string codepageName = "iso-2022-kr";
        Decoder krDecoder = Encoding.GetEncoding(codepageName).GetDecoder();
        const int BUFFER_SIZE = 100;
        char[] buffer = new char[BUFFER_SIZE];

        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < koreanEncodedBuffer.Length)
        {
            int charCount = krDecoder.GetChars(koreanEncodedBuffer, byteIndex, 1, buffer, charIndex);
            if (charCount > 0)
            {
                for (int i = 0; i < charCount; i++)
                {
                    Console.Write("0x{0:X}, ", (int)buffer[charIndex + i]);
                }
                Console.WriteLine();
                charIndex += charCount;
            }
            byteIndex++;
        }

        int[] expectedResult = new int[] { 0xFFE2, 0xFFE2, 0x21, 0x7E, 0x21, 0x7E };
        Assert.True(charIndex == expectedResult.Length);
        for (int i = 0; i < charIndex; i++)
            Assert.True(expectedResult[i] == (int)buffer[i]);
    }

    [Fact]
    public static void TestDecoding2312HZ()
    {
        EnsureInitialization();

        byte[] hzEncodedBuffer = new byte[]
            {
                0x7E, 0x42,
                0x7E, 0x7E,
                0x7E, 0x7B, 0x21, 0x7E,
                0x7E, 0x7D, 0x42, 0x42,
                0x7E, 0xA, 0x43, 0x43
            };

        string codepageName = "hz-gb-2312";
        Decoder krDecoder = Encoding.GetEncoding(codepageName).GetDecoder();
        const int BUFFER_SIZE = 100;
        char[] buffer = new char[BUFFER_SIZE];

        int byteIndex = 0;
        int charIndex = 0;

        while (byteIndex < hzEncodedBuffer.Length)
        {
            int charCount = krDecoder.GetChars(hzEncodedBuffer, byteIndex, 1, buffer, charIndex);
            if (charCount > 0)
            {
                for (int i = 0; i < charCount; i++)
                {
                    Console.Write("0x{0:X}, ", (int)buffer[charIndex + i]);
                }
                Console.WriteLine();
                charIndex += charCount;
            }
            byteIndex++;
        }

        int[] expectedResult = new int[] { 0x7E, 0x42, 0x7E, 0x3013, 0x42, 0x42, 0x43, 0x43, };
        Assert.True(charIndex == expectedResult.Length);
        for (int i = 0; i < charIndex; i++)
            Assert.True(expectedResult[i] == (int)buffer[i]);
    }

    [Fact]
    public static void TestEudcEncoding()
    {
        EnsureInitialization();

        Encoding.GetEncoding(51932);
        // new EncodingId("csEUCPkdFmtJapanese", 51932), 
    }

    [Fact]
    public static void TestCodePageToWebNameMappings()
    {
        EnsureInitialization();

        foreach (var mapping in s_codePageToWebNameTable)
        {
            var encoding = Encoding.GetEncoding(mapping.CodePage);
            Assert.True(string.Equals(mapping.WebName, encoding.WebName, StringComparison.OrdinalIgnoreCase));
        }
    }

    [Fact]
    public static void TestEncodingDisplayNames()
    {
        CultureInfo originalUICulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");

            EnsureInitialization();

            foreach (var mapping in s_codePageToWebNameTable)
            {
                var encoding = Encoding.GetEncoding(mapping.CodePage);

                string name = encoding.EncodingName;

                if (string.IsNullOrEmpty(name))
                {
                    Assert.True(false, "failed to get the display name of the encoding: " + encoding.WebName);
                }

                for (int i = 0; i < name.Length; ++i)
                {
                    int ch = (int)name[i];

                    if (ch > 127)
                    {
                        Assert.True(false,
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

    private static EncodingId[] s_encodingIdTable = new EncodingId[]
    {
        new EncodingId("437", 437),
        new EncodingId("arabic", 28596),
        new EncodingId("asmo-708", 708),
        new EncodingId("big5", 950),
        new EncodingId("big5-hkscs", 950),
        new EncodingId("ccsid00858", 858),
        new EncodingId("ccsid00924", 20924),
        new EncodingId("ccsid01140", 1140),
        new EncodingId("ccsid01141", 1141),
        new EncodingId("ccsid01142", 1142),
        new EncodingId("ccsid01143", 1143),
        new EncodingId("ccsid01144", 1144),
        new EncodingId("ccsid01145", 1145),
        new EncodingId("ccsid01146", 1146),
        new EncodingId("ccsid01147", 1147),
        new EncodingId("ccsid01148", 1148),
        new EncodingId("ccsid01149", 1149),
        new EncodingId("chinese", 936),
        new EncodingId("cn-big5", 950),
        new EncodingId("cn-gb", 936),
        new EncodingId("cp00858", 858),
        new EncodingId("cp00924", 20924),
        new EncodingId("cp01140", 1140),
        new EncodingId("cp01141", 1141),
        new EncodingId("cp01142", 1142),
        new EncodingId("cp01143", 1143),
        new EncodingId("cp01144", 1144),
        new EncodingId("cp01145", 1145),
        new EncodingId("cp01146", 1146),
        new EncodingId("cp01147", 1147),
        new EncodingId("cp01148", 1148),
        new EncodingId("cp01149", 1149),
        new EncodingId("cp037", 37),
        new EncodingId("cp1025", 21025),
        new EncodingId("cp1026", 1026),
        new EncodingId("cp1256", 1256),
        new EncodingId("cp273", 20273),
        new EncodingId("cp278", 20278),
        new EncodingId("cp280", 20280),
        new EncodingId("cp284", 20284),
        new EncodingId("cp285", 20285),
        new EncodingId("cp290", 20290),
        new EncodingId("cp297", 20297),
        new EncodingId("cp420", 20420),
        new EncodingId("cp423", 20423),
        new EncodingId("cp424", 20424),
        new EncodingId("cp437", 437),
        new EncodingId("cp500", 500),
        new EncodingId("cp50227", 50227),
        new EncodingId("cp850", 850),
        new EncodingId("cp852", 852),
        new EncodingId("cp855", 855),
        new EncodingId("cp857", 857),
        new EncodingId("cp858", 858),
        new EncodingId("cp860", 860),
        new EncodingId("cp861", 861),
        new EncodingId("cp862", 862),
        new EncodingId("cp863", 863),
        new EncodingId("cp864", 864),
        new EncodingId("cp865", 865),
        new EncodingId("cp866", 866),
        new EncodingId("cp869", 869),
        new EncodingId("cp870", 870),
        new EncodingId("cp871", 20871),
        new EncodingId("cp875", 875),
        new EncodingId("cp880", 20880),
        new EncodingId("cp905", 20905),
        new EncodingId("csbig5", 950),
        new EncodingId("cseuckr", 51949),
        new EncodingId("cseucpkdfmtjapanese", 51932),
        new EncodingId("csgb2312", 936),
        new EncodingId("csgb231280", 936),
        new EncodingId("csibm037", 37),
        new EncodingId("csibm1026", 1026),
        new EncodingId("csibm273", 20273),
        new EncodingId("csibm277", 20277),
        new EncodingId("csibm278", 20278),
        new EncodingId("csibm280", 20280),
        new EncodingId("csibm284", 20284),
        new EncodingId("csibm285", 20285),
        new EncodingId("csibm290", 20290),
        new EncodingId("csibm297", 20297),
        new EncodingId("csibm420", 20420),
        new EncodingId("csibm423", 20423),
        new EncodingId("csibm424", 20424),
        new EncodingId("csibm500", 500),
        new EncodingId("csibm870", 870),
        new EncodingId("csibm871", 20871),
        new EncodingId("csibm880", 20880),
        new EncodingId("csibm905", 20905),
        new EncodingId("csibmthai", 20838),
        new EncodingId("csiso2022jp", 50221),
        new EncodingId("csiso2022kr", 50225),
        new EncodingId("csiso58gb231280", 936),
        new EncodingId("csisolatin2", 28592),
        new EncodingId("csisolatin3", 28593),
        new EncodingId("csisolatin4", 28594),
        new EncodingId("csisolatin5", 28599),
        new EncodingId("csisolatin9", 28605),
        new EncodingId("csisolatinarabic", 28596),
        new EncodingId("csisolatincyrillic", 28595),
        new EncodingId("csisolatingreek", 28597),
        new EncodingId("csisolatinhebrew", 28598),
        new EncodingId("cskoi8r", 20866),
        new EncodingId("csksc56011987", 949),
        new EncodingId("cspc8codepage437", 437),
        new EncodingId("csshiftjis", 932),
        new EncodingId("cswindows31j", 932),
        new EncodingId("cyrillic", 28595),
        new EncodingId("din_66003", 20106),
        new EncodingId("dos-720", 720),
        new EncodingId("dos-862", 862),
        new EncodingId("dos-874", 874),
        new EncodingId("ebcdic-cp-ar1", 20420),
        new EncodingId("ebcdic-cp-be", 500),
        new EncodingId("ebcdic-cp-ca", 37),
        new EncodingId("ebcdic-cp-ch", 500),
        new EncodingId("ebcdic-cp-dk", 20277),
        new EncodingId("ebcdic-cp-es", 20284),
        new EncodingId("ebcdic-cp-fi", 20278),
        new EncodingId("ebcdic-cp-fr", 20297),
        new EncodingId("ebcdic-cp-gb", 20285),
        new EncodingId("ebcdic-cp-gr", 20423),
        new EncodingId("ebcdic-cp-he", 20424),
        new EncodingId("ebcdic-cp-is", 20871),
        new EncodingId("ebcdic-cp-it", 20280),
        new EncodingId("ebcdic-cp-nl", 37),
        new EncodingId("ebcdic-cp-no", 20277),
        new EncodingId("ebcdic-cp-roece", 870),
        new EncodingId("ebcdic-cp-se", 20278),
        new EncodingId("ebcdic-cp-tr", 20905),
        new EncodingId("ebcdic-cp-us", 37),
        new EncodingId("ebcdic-cp-wt", 37),
        new EncodingId("ebcdic-cp-yu", 870),
        new EncodingId("ebcdic-cyrillic", 20880),
        new EncodingId("ebcdic-de-273+euro", 1141),
        new EncodingId("ebcdic-dk-277+euro", 1142),
        new EncodingId("ebcdic-es-284+euro", 1145),
        new EncodingId("ebcdic-fi-278+euro", 1143),
        new EncodingId("ebcdic-fr-297+euro", 1147),
        new EncodingId("ebcdic-gb-285+euro", 1146),
        new EncodingId("ebcdic-international-500+euro", 1148),
        new EncodingId("ebcdic-is-871+euro", 1149),
        new EncodingId("ebcdic-it-280+euro", 1144),
        new EncodingId("ebcdic-jp-kana", 20290),
        new EncodingId("ebcdic-latin9--euro", 20924),
        new EncodingId("ebcdic-no-277+euro", 1142),
        new EncodingId("ebcdic-se-278+euro", 1143),
        new EncodingId("ebcdic-us-37+euro", 1140),
        new EncodingId("ecma-114", 28596),
        new EncodingId("ecma-118", 28597),
        new EncodingId("elot_928", 28597),
        new EncodingId("euc-cn", 51936),
        new EncodingId("euc-jp", 51932),
        new EncodingId("euc-kr", 51949),
        new EncodingId("extended_unix_code_packed_format_for_japanese", 51932),
        new EncodingId("gb18030", 54936),
        new EncodingId("gb2312", 936),
        new EncodingId("gb2312-80", 936),
        new EncodingId("gb231280", 936),
        new EncodingId("gbk", 936),
        new EncodingId("gb_2312-80", 936),
        new EncodingId("german", 20106),
        new EncodingId("greek", 28597),
        new EncodingId("greek8", 28597),
        new EncodingId("hebrew", 28598),
        new EncodingId("hz-gb-2312", 52936),
        new EncodingId("ibm-thai", 20838),
        new EncodingId("ibm00858", 858),
        new EncodingId("ibm00924", 20924),
        new EncodingId("ibm01047", 1047),
        new EncodingId("ibm01140", 1140),
        new EncodingId("ibm01141", 1141),
        new EncodingId("ibm01142", 1142),
        new EncodingId("ibm01143", 1143),
        new EncodingId("ibm01144", 1144),
        new EncodingId("ibm01145", 1145),
        new EncodingId("ibm01146", 1146),
        new EncodingId("ibm01147", 1147),
        new EncodingId("ibm01148", 1148),
        new EncodingId("ibm01149", 1149),
        new EncodingId("ibm037", 37),
        new EncodingId("ibm1026", 1026),
        new EncodingId("ibm273", 20273),
        new EncodingId("ibm277", 20277),
        new EncodingId("ibm278", 20278),
        new EncodingId("ibm280", 20280),
        new EncodingId("ibm284", 20284),
        new EncodingId("ibm285", 20285),
        new EncodingId("ibm290", 20290),
        new EncodingId("ibm297", 20297),
        new EncodingId("ibm420", 20420),
        new EncodingId("ibm423", 20423),
        new EncodingId("ibm424", 20424),
        new EncodingId("ibm437", 437),
        new EncodingId("ibm500", 500),
        new EncodingId("ibm737", 737),
        new EncodingId("ibm775", 775),
        new EncodingId("ibm850", 850),
        new EncodingId("ibm852", 852),
        new EncodingId("ibm855", 855),
        new EncodingId("ibm857", 857),
        new EncodingId("ibm860", 860),
        new EncodingId("ibm861", 861),
        new EncodingId("ibm862", 862),
        new EncodingId("ibm863", 863),
        new EncodingId("ibm864", 864),
        new EncodingId("ibm865", 865),
        new EncodingId("ibm866", 866),
        new EncodingId("ibm869", 869),
        new EncodingId("ibm870", 870),
        new EncodingId("ibm871", 20871),
        new EncodingId("ibm880", 20880),
        new EncodingId("ibm905", 20905),
        new EncodingId("irv", 20105),
        new EncodingId("iso-2022-jp", 50220),
        new EncodingId("iso-2022-jpeuc", 51932),
        new EncodingId("iso-2022-kr", 50225),
        new EncodingId("iso-2022-kr-7", 50225),
        new EncodingId("iso-2022-kr-7bit", 50225),
        new EncodingId("iso-2022-kr-8", 51949),
        new EncodingId("iso-2022-kr-8bit", 51949),
        new EncodingId("iso-8859-11", 874),
        new EncodingId("iso-8859-13", 28603),
        new EncodingId("iso-8859-15", 28605),
        new EncodingId("iso-8859-2", 28592),
        new EncodingId("iso-8859-3", 28593),
        new EncodingId("iso-8859-4", 28594),
        new EncodingId("iso-8859-5", 28595),
        new EncodingId("iso-8859-6", 28596),
        new EncodingId("iso-8859-7", 28597),
        new EncodingId("iso-8859-8", 28598),
        new EncodingId("iso-8859-8 visual", 28598),
        new EncodingId("iso-8859-8-i", 38598),
        new EncodingId("iso-8859-9", 28599),
        new EncodingId("iso-ir-101", 28592),
        new EncodingId("iso-ir-109", 28593),
        new EncodingId("iso-ir-110", 28594),
        new EncodingId("iso-ir-126", 28597),
        new EncodingId("iso-ir-127", 28596),
        new EncodingId("iso-ir-138", 28598),
        new EncodingId("iso-ir-144", 28595),
        new EncodingId("iso-ir-148", 28599),
        new EncodingId("iso-ir-149", 949),
        new EncodingId("iso-ir-58", 936),
        new EncodingId("iso8859-2", 28592),
        new EncodingId("iso_8859-15", 28605),
        new EncodingId("iso_8859-2", 28592),
        new EncodingId("iso_8859-2:1987", 28592),
        new EncodingId("iso_8859-3", 28593),
        new EncodingId("iso_8859-3:1988", 28593),
        new EncodingId("iso_8859-4", 28594),
        new EncodingId("iso_8859-4:1988", 28594),
        new EncodingId("iso_8859-5", 28595),
        new EncodingId("iso_8859-5:1988", 28595),
        new EncodingId("iso_8859-6", 28596),
        new EncodingId("iso_8859-6:1987", 28596),
        new EncodingId("iso_8859-7", 28597),
        new EncodingId("iso_8859-7:1987", 28597),
        new EncodingId("iso_8859-8", 28598),
        new EncodingId("iso_8859-8:1988", 28598),
        new EncodingId("iso_8859-9", 28599),
        new EncodingId("iso_8859-9:1989", 28599),
        new EncodingId("johab", 1361),
        new EncodingId("koi", 20866),
        new EncodingId("koi8", 20866),
        new EncodingId("koi8-r", 20866),
        new EncodingId("koi8-ru", 21866),
        new EncodingId("koi8-u", 21866),
        new EncodingId("koi8r", 20866),
        new EncodingId("korean", 949),
        new EncodingId("ks-c-5601", 949),
        new EncodingId("ks-c5601", 949),
        new EncodingId("ksc5601", 949),
        new EncodingId("ksc_5601", 949),
        new EncodingId("ks_c_5601", 949),
        new EncodingId("ks_c_5601-1987", 949),
        new EncodingId("ks_c_5601-1989", 949),
        new EncodingId("ks_c_5601_1987", 949),
        new EncodingId("l2", 28592),
        new EncodingId("l3", 28593),
        new EncodingId("l4", 28594),
        new EncodingId("l5", 28599),
        new EncodingId("l9", 28605),
        new EncodingId("latin2", 28592),
        new EncodingId("latin3", 28593),
        new EncodingId("latin4", 28594),
        new EncodingId("latin5", 28599),
        new EncodingId("latin9", 28605),
        new EncodingId("logical", 28598),
        new EncodingId("macintosh", 10000),
        new EncodingId("ms_kanji", 932),
        new EncodingId("norwegian", 20108),
        new EncodingId("ns_4551-1", 20108),
        new EncodingId("pc-multilingual-850+euro", 858),
        new EncodingId("sen_850200_b", 20107),
        new EncodingId("shift-jis", 932),
        new EncodingId("shift_jis", 932),
        new EncodingId("sjis", 932),
        new EncodingId("swedish", 20107),
        new EncodingId("tis-620", 874),
        new EncodingId("visual", 28598),
        new EncodingId("windows-1250", 1250),
        new EncodingId("windows-1251", 1251),
        new EncodingId("windows-1252", 1252),
        new EncodingId("windows-1253", 1253),
        new EncodingId("windows-1254", 1254),
        new EncodingId("windows-1255", 1255),
        new EncodingId("windows-1256", 1256),
        new EncodingId("windows-1257", 1257),
        new EncodingId("windows-1258", 1258),
        new EncodingId("windows-874", 874),
        new EncodingId("x-ansi", 1252),
        new EncodingId("x-chinese-cns", 20000),
        new EncodingId("x-chinese-eten", 20002),
        new EncodingId("x-cp1250", 1250),
        new EncodingId("x-cp1251", 1251),
        new EncodingId("x-cp20001", 20001),
        new EncodingId("x-cp20003", 20003),
        new EncodingId("x-cp20004", 20004),
        new EncodingId("x-cp20005", 20005),
        new EncodingId("x-cp20261", 20261),
        new EncodingId("x-cp20269", 20269),
        new EncodingId("x-cp20936", 20936),
        new EncodingId("x-cp20949", 20949),
        new EncodingId("x-cp50227", 50227),
        new EncodingId("x-ebcdic-koreanextended", 20833),
        new EncodingId("x-euc", 51932),
        new EncodingId("x-euc-cn", 51936),
        new EncodingId("x-euc-jp", 51932),
        new EncodingId("x-europa", 29001),
        new EncodingId("x-ia5", 20105),
        new EncodingId("x-ia5-german", 20106),
        new EncodingId("x-ia5-norwegian", 20108),
        new EncodingId("x-ia5-swedish", 20107),
        new EncodingId("x-iscii-as", 57006),
        new EncodingId("x-iscii-be", 57003),
        new EncodingId("x-iscii-de", 57002),
        new EncodingId("x-iscii-gu", 57010),
        new EncodingId("x-iscii-ka", 57008),
        new EncodingId("x-iscii-ma", 57009),
        new EncodingId("x-iscii-or", 57007),
        new EncodingId("x-iscii-pa", 57011),
        new EncodingId("x-iscii-ta", 57004),
        new EncodingId("x-iscii-te", 57005),
        new EncodingId("x-mac-arabic", 10004),
        new EncodingId("x-mac-ce", 10029),
        new EncodingId("x-mac-chinesesimp", 10008),
        new EncodingId("x-mac-chinesetrad", 10002),
        new EncodingId("x-mac-croatian", 10082),
        new EncodingId("x-mac-cyrillic", 10007),
        new EncodingId("x-mac-greek", 10006),
        new EncodingId("x-mac-hebrew", 10005),
        new EncodingId("x-mac-icelandic", 10079),
        new EncodingId("x-mac-japanese", 10001),
        new EncodingId("x-mac-korean", 10003),
        new EncodingId("x-mac-romanian", 10010),
        new EncodingId("x-mac-thai", 10021),
        new EncodingId("x-mac-turkish", 10081),
        new EncodingId("x-mac-ukrainian", 10017),
        new EncodingId("x-ms-cp932", 932),
        new EncodingId("x-sjis", 932),
        new EncodingId("x-x-big5", 950)
    };

    private static readonly CodePageToWebNameMapping[] s_codePageToWebNameTable = new[]
    {
        new CodePageToWebNameMapping(37, "ibm037"),
        new CodePageToWebNameMapping(437, "ibm437"),
        new CodePageToWebNameMapping(500, "ibm500"),
        new CodePageToWebNameMapping(708, "asmo-708"),
        new CodePageToWebNameMapping(720, "dos-720"),
        new CodePageToWebNameMapping(737, "ibm737"),
        new CodePageToWebNameMapping(775, "ibm775"),
        new CodePageToWebNameMapping(850, "ibm850"),
        new CodePageToWebNameMapping(852, "ibm852"),
        new CodePageToWebNameMapping(855, "ibm855"),
        new CodePageToWebNameMapping(857, "ibm857"),
        new CodePageToWebNameMapping(858, "ibm00858"),
        new CodePageToWebNameMapping(860, "ibm860"),
        new CodePageToWebNameMapping(861, "ibm861"),
        new CodePageToWebNameMapping(862, "dos-862"),
        new CodePageToWebNameMapping(863, "ibm863"),
        new CodePageToWebNameMapping(864, "ibm864"),
        new CodePageToWebNameMapping(865, "ibm865"),
        new CodePageToWebNameMapping(866, "cp866"),
        new CodePageToWebNameMapping(869, "ibm869"),
        new CodePageToWebNameMapping(870, "ibm870"),
        new CodePageToWebNameMapping(874, "windows-874"),
        new CodePageToWebNameMapping(875, "cp875"),
        new CodePageToWebNameMapping(932, "shift_jis"),
        new CodePageToWebNameMapping(936, "gb2312"),
        new CodePageToWebNameMapping(949, "ks_c_5601-1987"),
        new CodePageToWebNameMapping(950, "big5"),
        new CodePageToWebNameMapping(1026, "ibm1026"),
        new CodePageToWebNameMapping(1047, "ibm01047"),
        new CodePageToWebNameMapping(1140, "ibm01140"),
        new CodePageToWebNameMapping(1141, "ibm01141"),
        new CodePageToWebNameMapping(1142, "ibm01142"),
        new CodePageToWebNameMapping(1143, "ibm01143"),
        new CodePageToWebNameMapping(1144, "ibm01144"),
        new CodePageToWebNameMapping(1145, "ibm01145"),
        new CodePageToWebNameMapping(1146, "ibm01146"),
        new CodePageToWebNameMapping(1147, "ibm01147"),
        new CodePageToWebNameMapping(1148, "ibm01148"),
        new CodePageToWebNameMapping(1149, "ibm01149"),
        new CodePageToWebNameMapping(1250, "windows-1250"),
        new CodePageToWebNameMapping(1251, "windows-1251"),
        new CodePageToWebNameMapping(1252, "windows-1252"),
        new CodePageToWebNameMapping(1253, "windows-1253"),
        new CodePageToWebNameMapping(1254, "windows-1254"),
        new CodePageToWebNameMapping(1255, "windows-1255"),
        new CodePageToWebNameMapping(1256, "windows-1256"),
        new CodePageToWebNameMapping(1257, "windows-1257"),
        new CodePageToWebNameMapping(1258, "windows-1258"),
        new CodePageToWebNameMapping(1361, "johab"),
        new CodePageToWebNameMapping(10000, "macintosh"),
        new CodePageToWebNameMapping(10001, "x-mac-japanese"),
        new CodePageToWebNameMapping(10002, "x-mac-chinesetrad"),
        new CodePageToWebNameMapping(10003, "x-mac-korean"),
        new CodePageToWebNameMapping(10004, "x-mac-arabic"),
        new CodePageToWebNameMapping(10005, "x-mac-hebrew"),
        new CodePageToWebNameMapping(10006, "x-mac-greek"),
        new CodePageToWebNameMapping(10007, "x-mac-cyrillic"),
        new CodePageToWebNameMapping(10008, "x-mac-chinesesimp"),
        new CodePageToWebNameMapping(10010, "x-mac-romanian"),
        new CodePageToWebNameMapping(10017, "x-mac-ukrainian"),
        new CodePageToWebNameMapping(10021, "x-mac-thai"),
        new CodePageToWebNameMapping(10029, "x-mac-ce"),
        new CodePageToWebNameMapping(10079, "x-mac-icelandic"),
        new CodePageToWebNameMapping(10081, "x-mac-turkish"),
        new CodePageToWebNameMapping(10082, "x-mac-croatian"),
        new CodePageToWebNameMapping(20000, "x-chinese-cns"),
        new CodePageToWebNameMapping(20001, "x-cp20001"),
        new CodePageToWebNameMapping(20002, "x-chinese-eten"),
        new CodePageToWebNameMapping(20003, "x-cp20003"),
        new CodePageToWebNameMapping(20004, "x-cp20004"),
        new CodePageToWebNameMapping(20005, "x-cp20005"),
        new CodePageToWebNameMapping(20105, "x-ia5"),
        new CodePageToWebNameMapping(20106, "x-ia5-german"),
        new CodePageToWebNameMapping(20107, "x-ia5-swedish"),
        new CodePageToWebNameMapping(20108, "x-ia5-norwegian"),
        new CodePageToWebNameMapping(20261, "x-cp20261"),
        new CodePageToWebNameMapping(20269, "x-cp20269"),
        new CodePageToWebNameMapping(20273, "ibm273"),
        new CodePageToWebNameMapping(20277, "ibm277"),
        new CodePageToWebNameMapping(20278, "ibm278"),
        new CodePageToWebNameMapping(20280, "ibm280"),
        new CodePageToWebNameMapping(20284, "ibm284"),
        new CodePageToWebNameMapping(20285, "ibm285"),
        new CodePageToWebNameMapping(20290, "ibm290"),
        new CodePageToWebNameMapping(20297, "ibm297"),
        new CodePageToWebNameMapping(20420, "ibm420"),
        new CodePageToWebNameMapping(20423, "ibm423"),
        new CodePageToWebNameMapping(20424, "ibm424"),
        new CodePageToWebNameMapping(20833, "x-ebcdic-koreanextended"),
        new CodePageToWebNameMapping(20838, "ibm-thai"),
        new CodePageToWebNameMapping(20866, "koi8-r"),
        new CodePageToWebNameMapping(20871, "ibm871"),
        new CodePageToWebNameMapping(20880, "ibm880"),
        new CodePageToWebNameMapping(20905, "ibm905"),
        new CodePageToWebNameMapping(20924, "ibm00924"),
        new CodePageToWebNameMapping(20932, "euc-jp"),
        new CodePageToWebNameMapping(20936, "x-cp20936"),
        new CodePageToWebNameMapping(20949, "x-cp20949"),
        new CodePageToWebNameMapping(21025, "cp1025"),
        new CodePageToWebNameMapping(21866, "koi8-u"),
        new CodePageToWebNameMapping(28592, "iso-8859-2"),
        new CodePageToWebNameMapping(28593, "iso-8859-3"),
        new CodePageToWebNameMapping(28594, "iso-8859-4"),
        new CodePageToWebNameMapping(28595, "iso-8859-5"),
        new CodePageToWebNameMapping(28596, "iso-8859-6"),
        new CodePageToWebNameMapping(28597, "iso-8859-7"),
        new CodePageToWebNameMapping(28598, "iso-8859-8"),
        new CodePageToWebNameMapping(28599, "iso-8859-9"),
        new CodePageToWebNameMapping(28603, "iso-8859-13"),
        new CodePageToWebNameMapping(28605, "iso-8859-15"),
        new CodePageToWebNameMapping(38598, "iso-8859-8-i"),
        new CodePageToWebNameMapping(50220, "iso-2022-jp"),
        new CodePageToWebNameMapping(50221, "csiso2022jp"),
        new CodePageToWebNameMapping(50222, "iso-2022-jp"),
        new CodePageToWebNameMapping(50225, "iso-2022-kr"),
        new CodePageToWebNameMapping(50227, "x-cp50227"),
        new CodePageToWebNameMapping(51932, "euc-jp"),
        new CodePageToWebNameMapping(51936, "euc-cn"),
        new CodePageToWebNameMapping(51949, "euc-kr"),
        new CodePageToWebNameMapping(52936, "hz-gb-2312"),
        new CodePageToWebNameMapping(54936, "gb18030"),
        new CodePageToWebNameMapping(57002, "x-iscii-de"),
        new CodePageToWebNameMapping(57003, "x-iscii-be"),
        new CodePageToWebNameMapping(57004, "x-iscii-ta"),
        new CodePageToWebNameMapping(57005, "x-iscii-te"),
        new CodePageToWebNameMapping(57006, "x-iscii-as"),
        new CodePageToWebNameMapping(57007, "x-iscii-or"),
        new CodePageToWebNameMapping(57008, "x-iscii-ka"),
        new CodePageToWebNameMapping(57009, "x-iscii-ma"),
        new CodePageToWebNameMapping(57010, "x-iscii-gu"),
        new CodePageToWebNameMapping(57011, "x-iscii-pa")
    };
}
