// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Xunit;
using System;
using System.Xml;
using System.Xml.XPath;
using XPathTests.Common;

namespace XPathTests.FunctionalTests
{
    /// <summary>
    /// Globalization
    /// </summary>
    public static partial class GlobalizationTests
    {
        /// <summary>
        /// surrogates : xpath testing, return 7 nodes
        /// </summary>
        [Fact]
        [OuterLoop]
        public static void GlobalizationTest566()
        {
            var xml = "Surrogates_1.xml";
            var testExpression =
                @"//Row[Data/text()=""𠀋"" or Data/text()=""𠂢"" or Data/text()=""𠂤"" or Data/text()=""𪀚"" or Data/text()=""𪂂"" or Data/text()=""𪃹"" or Data/text()=""𥇍""] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    𠀋\n    2000B\n    D840\n    DC0B\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𠂢\n    200A2\n    D840\n    DCA2\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𠂤\n    200A4\n    D840\n    DCA4\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𥇍\n    251CD\n    D854\n    DDCD\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𪀚\n    2A01A\n    D868\n    DC1A\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𪂂\n    2A082\n    D868\n    DC82\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    𪃹\n    2A0F9\n    D868\n    DCF9\n    \n \n   "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// turkish i xpath testing, return 4 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest567()
        {
            var xml = "turkish.xml";
            var testExpression = @"data/a[text()=""ı"" or text()=""I"" or text()=""i"" or text()=""İ""] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "a",
                    Name = "a",
                    HasNameTable = true,
                    Value = "ı"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "a",
                    Name = "a",
                    HasNameTable = true,
                    Value = "I"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "a",
                    Name = "a",
                    HasNameTable = true,
                    Value = "i"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "a",
                    Name = "a",
                    HasNameTable = true,
                    Value = "İ"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Thai Risky : xpath testing, returns 4 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest568()
        {
            var xml = "Thai_risky_chars.xml";
            var testExpression =
                @"//root/node()[.=""น้ำ"" or .=""นี่เป็นภาษาไทยที่ถูกต้อง"" or .=""เป็นมนุษย์สุดประเสริญเลิศคุณค่า กว่าบรรดาฝูงสัตว์เดรัจฉาน จงฝ่าฟันพัฒนาวิชาการ อย่าล้างผลาญฤาเข่นฆ่าบีฑาใคร ไม่ถือโทษโกรธแช่งซัดฮึดฮัดด่า หัดอภัยเหมือนกีฬาอัชฌาสัย ปฏิบัติประพฤติกฎกำหนดใจ พูดจาให้ จ๊ะๆ จ๋าๆ น่าฟังเอย "" or .=""ยู่ยี่ปั่น กุฏิโกฎู วิญญู อูฐฐุน น้ำป้ำ ""] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "WORD",
                    Name = "WORD",
                    HasNameTable = true,
                    Value = "น้ำ"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "LINE",
                    Name = "LINE",
                    HasNameTable = true,
                    Value = "นี่เป็นภาษาไทยที่ถูกต้อง"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "PARAGRAPH",
                    Name = "PARAGRAPH",
                    HasNameTable = true,
                    Value =
                        "เป็นมนุษย์สุดประเสริญเลิศคุณค่า กว่าบรรดาฝูงสัตว์เดรัจฉาน จงฝ่าฟันพัฒนาวิชาการ อย่าล้างผลาญฤาเข่นฆ่าบีฑาใคร ไม่ถือโทษโกรธแช่งซัดฮึดฮัดด่า หัดอภัยเหมือนกีฬาอัชฌาสัย ปฏิบัติประพฤติกฎกำหนดใจ พูดจาให้ จ๊ะๆ จ๋าๆ น่าฟังเอย "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "RISKY",
                    Name = "RISKY",
                    HasNameTable = true,
                    Value = "ยู่ยี่ปั่น กุฏิโกฎู วิญญู อูฐฐุน น้ำป้ำ "
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Japanese 1: xpath testing problem char, returns 19 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest569()
        {
            var xml = "JPN_problem_chars_1.xml";
            var testExpression =
                @"//char[@Expression=""ｱ"" or @Expression=""ｲ"" or @Expression=""ｳ"" or @Expression=""ｴ"" or @Expression=""ｵ"" or @Expression=""あ"" or @Expression=""い"" or @Expression=""う"" or @Expression=""え"" or @Expression=""お"" or @Expression=""ア"" or @Expression=""イ"" or @Expression=""ウ"" or @Expression=""エ"" or @Expression=""オ"" or @Expression=""亜"" or @Expression=""伊"" or @Expression=""宇"" or @Expression=""江"" ]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasAttributes = true,
                    IsEmptyElement = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Japanese 2: xpath testing problem char, returns 7 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest5610()
        {
            var xml = "JPN_problem_chars_2.xml";
            var testExpression =
                @"//char[contains(., ""太平洋"") or contains(., ""毫歇歉"") or contains(., ""滌漾黑"") or contains(., ""十歃濬藹犾表"") or contains(., ""蛞毫烟礰竹"") or contains(., ""ａＢａ"") or contains(., ""ﾐﾑﾒ"")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nNon-Problem 太平洋\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nDOS Restricted 毫歇歉\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nBoundary 滌漾黑\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\n5C Trail Byte 十歃濬藹犾表 \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\n7C Trail Byte 蛞毫烟礰竹 \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nDB Romaji ａＢａ \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nKana ﾐﾑﾒ \n"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Korean: xpath testing problem char, return 6 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest5613()
        {
            var xml = "KOR_problem_chars_b.xml";
            var testExpression =
                @"root/V[contains(text(), ""한글"") or text()=""は꾜は꾜は꾜は꾜は꾜"" or contains(text(), ""詰詰"") or contains(text(), ""檍也"") or contains(text(), ""가나 가"") or contains(text(), ""Ｊｕｎｊａ"")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "한글"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "は꾜は꾜は꾜は꾜は꾜"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "詰詰詰詰"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "言檍也言檍也言檍也"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "가나 가나 가나 가나 가나 가나"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "Ｊｕｎｊａ"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// Single byte char: xpath testing problem char, return 6 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest5614()
        {
            var xml = "Single_byte_problem_chars_b.xml";
            var testExpression =
                @"//V[contains(text(), ""öÜß"") or text()=""© ®"" or contains(text(), ""¿¾Õ"") or contains(text(), ""ÄäÖ§²³@µ"") or contains(text(), ""åE5å"") or contains(text(), ""™"")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "öÜß"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "© ®"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "¿¾Õ"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "ÄäÖ§²³@µ"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "åE5å"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "™ ©­®"
                });
            ;

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate(): CHS gb18030 char
        /// </summary>
        [Fact]
        public static void GlobalizationTest5615()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"translate('+\Ÿ\à\å\û\•\å\š\å\‘\å\~\+', '\Ÿ\à\å\û\•\å\š\å\‘\å\~\', '1*2*3*4*5*6*7*8*9*a*b*c*d*e*')";
            var expected = @"+1*2*3*4*5*6*4*8*4*a*4*c*+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate(): Single byte problem char
        /// </summary>
        [Fact]
        public static void GlobalizationTest5616()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"translate('+öÜß©¿¾ÕÄäÖ§²³@µåE5å™­®+', 'öÜß©¿¾ÕÄäÖ§²³@µåE5å™­®', '0123456789abcdefghijklm')";
            var expected = @"+0123456789abcdefghfjkl+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate(): Japanese problem char
        /// </summary>
        [Fact]
        public static void GlobalizationTest5617()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"translate('+ｱｲｳｴｵあいうえおアイウエオ亜伊宇江太平洋毫歇歉滌漾黑十歃濬藹犾表蛞毫烟礰竹ａＢａ+', 'ｱｲｳｴｵあいうえおアイウエオ亜伊宇江太平洋毫歇歉滌漾黑十歃濬藹犾表蛞毫烟礰竹ａＢａ', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
            var expected = @"+0123456789abcdefghijklmnopqrstuvwxym@#?%^%+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate(): CHT problem char
        /// </summary>
        [Fact]
        public static void GlobalizationTest5618()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"translate('+ñòéÑäçÓçåååþ¡¡ªþ¨ïå~¡¢+', 'ñòéÑäçÓçåååþ¡¡ªþ¨ïå~¡¢', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
            var expected = @"+01234565888bccebgh8jcl+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }

        /// <summary>
        /// translate(): CHS problem char
        /// </summary>
        [Fact]
        public static void GlobalizationTest5619()
        {
            var xml = "dummy.xml";
            var testExpression =
                @"translate('+åþ¡¡°¡åŸàû’Ÿàåû•åšå‘å~+', 'åþ¡¡°¡åŸàû’Ÿàåû•åšå‘å~', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
            var expected = @"+0122420789ab7809g0i0k0m+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }
    }
}
