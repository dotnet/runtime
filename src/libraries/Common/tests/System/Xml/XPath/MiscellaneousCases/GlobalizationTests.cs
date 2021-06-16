// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
                "//Row[Data/text()=\"\uD840\uDC0B\" or Data/text()=\"\uD840\uDCA2\" or Data/text()=\"\uD840\uDCA4\" or Data/text()=\"\uD868\uDC1A\" or Data/text()=\"\uD868\uDC82\" or Data/text()=\"\uD868\uDCF9\" or Data/text()=\"\uD854\uDDCD\"] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \uD840\uDC0B\n    2000B\n    D840\n    DC0B\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD840\uDCA2\n    200A2\n    D840\n    DCA2\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD840\uDCA4\n    200A4\n    D840\n    DCA4\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD854\uDDCD\n    251CD\n    D854\n    DDCD\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD868\uDC1A\n    2A01A\n    D868\n    DC1A\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD868\uDC82\n    2A082\n    D868\n    DC82\n    \n    \n    \n   "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "Row",
                    Name = "Row",
                    HasNameTable = true,
                    Value = "\n    \n    \uD868\uDCF9\n    2A0F9\n    D868\n    DCF9\n    \n \n   "
                });

            Utils.XPathNodesetTest(xml, testExpression, expected);
        }

        /// <summary>
        /// turkish i xpath testing, return 4 nodes
        /// </summary>
        [Fact]
        public static void GlobalizationTest567()
        {
            var xml = "turkish.xml";
            var testExpression = "data/a[text()=\"\u0131\" or text()=\"I\" or text()=\"i\" or text()=\"\u0130\"] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "a",
                    Name = "a",
                    HasNameTable = true,
                    Value = "\u0131"
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
                    Value = "\u0130"
                });

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
                "//root/node()[.=\"\u0E19\u0E49\u0E33\" or .=\"\u0E19\u0E35\u0E48\u0E40\u0E1B\u0E47\u0E19\u0E20\u0E32\u0E29\u0E32\u0E44\u0E17\u0E22\u0E17\u0E35\u0E48\u0E16\u0E39\u0E01\u0E15\u0E49\u0E2D\u0E07\" or .=\"\u0E40\u0E1B\u0E47\u0E19\u0E21\u0E19\u0E38\u0E29\u0E22\u0E4C\u0E2A\u0E38\u0E14\u0E1B\u0E23\u0E30\u0E40\u0E2A\u0E23\u0E34\u0E0D\u0E40\u0E25\u0E34\u0E28\u0E04\u0E38\u0E13\u0E04\u0E48\u0E32 \u0E01\u0E27\u0E48\u0E32\u0E1A\u0E23\u0E23\u0E14\u0E32\u0E1D\u0E39\u0E07\u0E2A\u0E31\u0E15\u0E27\u0E4C\u0E40\u0E14\u0E23\u0E31\u0E08\u0E09\u0E32\u0E19 \u0E08\u0E07\u0E1D\u0E48\u0E32\u0E1F\u0E31\u0E19\u0E1E\u0E31\u0E12\u0E19\u0E32\u0E27\u0E34\u0E0A\u0E32\u0E01\u0E32\u0E23 \u0E2D\u0E22\u0E48\u0E32\u0E25\u0E49\u0E32\u0E07\u0E1C\u0E25\u0E32\u0E0D\u0E24\u0E32\u0E40\u0E02\u0E48\u0E19\u0E06\u0E48\u0E32\u0E1A\u0E35\u0E11\u0E32\u0E43\u0E04\u0E23 \u0E44\u0E21\u0E48\u0E16\u0E37\u0E2D\u0E42\u0E17\u0E29\u0E42\u0E01\u0E23\u0E18\u0E41\u0E0A\u0E48\u0E07\u0E0B\u0E31\u0E14\u0E2E\u0E36\u0E14\u0E2E\u0E31\u0E14\u0E14\u0E48\u0E32 \u0E2B\u0E31\u0E14\u0E2D\u0E20\u0E31\u0E22\u0E40\u0E2B\u0E21\u0E37\u0E2D\u0E19\u0E01\u0E35\u0E2C\u0E32\u0E2D\u0E31\u0E0A\u0E0C\u0E32\u0E2A\u0E31\u0E22 \u0E1B\u0E0F\u0E34\u0E1A\u0E31\u0E15\u0E34\u0E1B\u0E23\u0E30\u0E1E\u0E24\u0E15\u0E34\u0E01\u0E0E\u0E01\u0E33\u0E2B\u0E19\u0E14\u0E43\u0E08 \u0E1E\u0E39\u0E14\u0E08\u0E32\u0E43\u0E2B\u0E49 \u0E08\u0E4A\u0E30\u0E46 \u0E08\u0E4B\u0E32\u0E46 \u0E19\u0E48\u0E32\u0E1F\u0E31\u0E07\u0E40\u0E2D\u0E22 \" or .=\"\u0E22\u0E39\u0E48\u0E22\u0E35\u0E48\u0E1B\u0E31\u0E48\u0E19 \u0E01\u0E38\u0E0F\u0E34\u0E42\u0E01\u0E0E\u0E39 \u0E27\u0E34\u0E0D\u0E0D\u0E39 \u0E2D\u0E39\u0E10\u0E10\u0E38\u0E19 \u0E19\u0E49\u0E33\u0E1B\u0E49\u0E33 \"] ";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "WORD",
                    Name = "WORD",
                    HasNameTable = true,
                    Value = "\u0E19\u0E49\u0E33"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "LINE",
                    Name = "LINE",
                    HasNameTable = true,
                    Value = "\u0E19\u0E35\u0E48\u0E40\u0E1B\u0E47\u0E19\u0E20\u0E32\u0E29\u0E32\u0E44\u0E17\u0E22\u0E17\u0E35\u0E48\u0E16\u0E39\u0E01\u0E15\u0E49\u0E2D\u0E07"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "PARAGRAPH",
                    Name = "PARAGRAPH",
                    HasNameTable = true,
                    Value =
                        "\u0E40\u0E1B\u0E47\u0E19\u0E21\u0E19\u0E38\u0E29\u0E22\u0E4C\u0E2A\u0E38\u0E14\u0E1B\u0E23\u0E30\u0E40\u0E2A\u0E23\u0E34\u0E0D\u0E40\u0E25\u0E34\u0E28\u0E04\u0E38\u0E13\u0E04\u0E48\u0E32 \u0E01\u0E27\u0E48\u0E32\u0E1A\u0E23\u0E23\u0E14\u0E32\u0E1D\u0E39\u0E07\u0E2A\u0E31\u0E15\u0E27\u0E4C\u0E40\u0E14\u0E23\u0E31\u0E08\u0E09\u0E32\u0E19 \u0E08\u0E07\u0E1D\u0E48\u0E32\u0E1F\u0E31\u0E19\u0E1E\u0E31\u0E12\u0E19\u0E32\u0E27\u0E34\u0E0A\u0E32\u0E01\u0E32\u0E23 \u0E2D\u0E22\u0E48\u0E32\u0E25\u0E49\u0E32\u0E07\u0E1C\u0E25\u0E32\u0E0D\u0E24\u0E32\u0E40\u0E02\u0E48\u0E19\u0E06\u0E48\u0E32\u0E1A\u0E35\u0E11\u0E32\u0E43\u0E04\u0E23 \u0E44\u0E21\u0E48\u0E16\u0E37\u0E2D\u0E42\u0E17\u0E29\u0E42\u0E01\u0E23\u0E18\u0E41\u0E0A\u0E48\u0E07\u0E0B\u0E31\u0E14\u0E2E\u0E36\u0E14\u0E2E\u0E31\u0E14\u0E14\u0E48\u0E32 \u0E2B\u0E31\u0E14\u0E2D\u0E20\u0E31\u0E22\u0E40\u0E2B\u0E21\u0E37\u0E2D\u0E19\u0E01\u0E35\u0E2C\u0E32\u0E2D\u0E31\u0E0A\u0E0C\u0E32\u0E2A\u0E31\u0E22 \u0E1B\u0E0F\u0E34\u0E1A\u0E31\u0E15\u0E34\u0E1B\u0E23\u0E30\u0E1E\u0E24\u0E15\u0E34\u0E01\u0E0E\u0E01\u0E33\u0E2B\u0E19\u0E14\u0E43\u0E08 \u0E1E\u0E39\u0E14\u0E08\u0E32\u0E43\u0E2B\u0E49 \u0E08\u0E4A\u0E30\u0E46 \u0E08\u0E4B\u0E32\u0E46 \u0E19\u0E48\u0E32\u0E1F\u0E31\u0E07\u0E40\u0E2D\u0E22 "
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "RISKY",
                    Name = "RISKY",
                    HasNameTable = true,
                    Value = "\u0E22\u0E39\u0E48\u0E22\u0E35\u0E48\u0E1B\u0E31\u0E48\u0E19 \u0E01\u0E38\u0E0F\u0E34\u0E42\u0E01\u0E0E\u0E39 \u0E27\u0E34\u0E0D\u0E0D\u0E39 \u0E2D\u0E39\u0E10\u0E10\u0E38\u0E19 \u0E19\u0E49\u0E33\u0E1B\u0E49\u0E33 "
                });

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
                "//char[@Expression=\"\uFF71\" or @Expression=\"\uFF72\" or @Expression=\"\uFF73\" or @Expression=\"\uFF74\" or @Expression=\"\uFF75\" or @Expression=\"\u3042\" or @Expression=\"\u3044\" or @Expression=\"\u3046\" or @Expression=\"\u3048\" or @Expression=\"\u304A\" or @Expression=\"\u30A2\" or @Expression=\"\u30A4\" or @Expression=\"\u30A6\" or @Expression=\"\u30A8\" or @Expression=\"\u30AA\" or @Expression=\"\u4E9C\" or @Expression=\"\u4F0A\" or @Expression=\"\u5B87\" or @Expression=\"\u6C5F\" ]";
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
                "//char[contains(., \"\u592A\u5E73\u6D0B\") or contains(., \"\u6BEB\u6B47\u6B49\") or contains(., \"\u6ECC\u6F3E\u9ED1\") or contains(., \"\u5341\u6B43\u6FEC\u85F9\u72BE\u8868\") or contains(., \"\u86DE\u6BEB\u70DF\u7930\u7AF9\") or contains(., \"\uFF41\uFF22\uFF41\") or contains(., \"\uFF90\uFF91\uFF92\")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nNon-Problem \u592A\u5E73\u6D0B\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nDOS Restricted \u6BEB\u6B47\u6B49\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nBoundary \u6ECC\u6F3E\u9ED1\n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\n5C Trail Byte \u5341\u6B43\u6FEC\u85F9\u72BE\u8868 \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\n7C Trail Byte \u86DE\u6BEB\u70DF\u7930\u7AF9 \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nDB Romaji \uFF41\uFF22\uFF41 \n"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "char",
                    Name = "char",
                    HasNameTable = true,
                    Value = "\nKana \uFF90\uFF91\uFF92 \n"
                });

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
                "root/V[contains(text(), \"\uD55C\uAE00\") or text()=\"\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C\" or contains(text(), \"\u8A70\u8A70\") or contains(text(), \"\u6A8D\u4E5F\") or contains(text(), \"\uAC00\uB098 \uAC00\") or contains(text(), \"\uFF2A\uFF55\uFF4E\uFF4A\uFF41\")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\uD55C\uAE00"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C\u306F\uAF9C"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u8A70\u8A70\u8A70\u8A70"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u8A00\u6A8D\u4E5F\u8A00\u6A8D\u4E5F\u8A00\u6A8D\u4E5F"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\uAC00\uB098 \uAC00\uB098 \uAC00\uB098 \uAC00\uB098 \uAC00\uB098 \uAC00\uB098"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\uFF2A\uFF55\uFF4E\uFF4A\uFF41"
                });

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
                "//V[contains(text(), \"\u00F6\u00DC\u00DF\") or text()=\"\u00A9 \u00AE\" or contains(text(), \"\u00BF\u00BE\u00D5\") or contains(text(), \"\u00C4\u00E4\u00D6\u00A7\u00B2\u00B3@\u00B5\") or contains(text(), \"\u00E5E5\u00E5\") or contains(text(), \"\u2122\")]";
            var expected = new XPathResult(0,
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u00F6\u00DC\u00DF"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u00A9 \u00AE"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u00BF\u00BE\u00D5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u00C4\u00E4\u00D6\u00A7\u00B2\u00B3@\u00B5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u00E5E5\u00E5"
                },
                new XPathResultToken
                {
                    NodeType = XPathNodeType.Element,
                    HasChildren = true,
                    LocalName = "V",
                    Name = "V",
                    HasNameTable = true,
                    Value = "\u2122 \u00A9\u00AD\u00AE"
                });

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
                "translate('+\u008F\\\u0178\\\u00E0\\\u00E5\\\u00FB\\\u2022\\\u00E5\\\u0161\\\u00E5\\\u2018\\\u00E5\\~\\+', '\u008F\\\u0178\\\u00E0\\\u00E5\\\u00FB\\\u2022\\\u00E5\\\u0161\\\u00E5\\\u2018\\\u00E5\\~\\', '1*2*3*4*5*6*7*8*9*a*b*c*d*e*')";
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
                "translate('+\u00F6\u00DC\u00DF\u00A9\u00BF\u00BE\u00D5\u00C4\u00E4\u00D6\u00A7\u00B2\u00B3@\u00B5\u00E5E5\u00E5\u2122\u00AD\u00AE+', '\u00F6\u00DC\u00DF\u00A9\u00BF\u00BE\u00D5\u00C4\u00E4\u00D6\u00A7\u00B2\u00B3@\u00B5\u00E5E5\u00E5\u2122\u00AD\u00AE', '0123456789abcdefghijklm')";
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
                "translate('+\uFF71\uFF72\uFF73\uFF74\uFF75\u3042\u3044\u3046\u3048\u304A\u30A2\u30A4\u30A6\u30A8\u30AA\u4E9C\u4F0A\u5B87\u6C5F\u592A\u5E73\u6D0B\u6BEB\u6B47\u6B49\u6ECC\u6F3E\u9ED1\u5341\u6B43\u6FEC\u85F9\u72BE\u8868\u86DE\u6BEB\u70DF\u7930\u7AF9\uFF41\uFF22\uFF41+', '\uFF71\uFF72\uFF73\uFF74\uFF75\u3042\u3044\u3046\u3048\u304A\u30A2\u30A4\u30A6\u30A8\u30AA\u4E9C\u4F0A\u5B87\u6C5F\u592A\u5E73\u6D0B\u6BEB\u6B47\u6B49\u6ECC\u6F3E\u9ED1\u5341\u6B43\u6FEC\u85F9\u72BE\u8868\u86DE\u6BEB\u70DF\u7930\u7AF9\uFF41\uFF22\uFF41', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
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
                "translate('+\u00F1\u00F2\u00E9\u00D1\u00E4\u00E7\u00D3\u00E7\u00E5\u00E5\u00E5\u00FE\u00A1\u00A1\u00AA\u00FE\u00A8\u00EF\u00E5~\u00A1\u00A2+', '\u00F1\u00F2\u00E9\u00D1\u00E4\u00E7\u00D3\u00E7\u00E5\u00E5\u00E5\u00FE\u00A1\u00A1\u00AA\u00FE\u00A8\u00EF\u00E5~\u00A1\u00A2', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
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
                "translate('+\u00E5\u00FE\u00A1\u00A1\u00B0\u00A1\u00E5\u0178\u00E0\u00FB\u2019\u008F\u0178\u00E0\u00E5\u00FB\u2022\u00E5\u0161\u00E5\u2018\u00E5~+', '\u00E5\u00FE\u00A1\u00A1\u00B0\u00A1\u00E5\u0178\u00E0\u00FB\u2019\u008F\u0178\u00E0\u00E5\u00FB\u2022\u00E5\u0161\u00E5\u2018\u00E5~', '0123456789abcdefghijklmnopqrstuvwxyz@#?%^*()_=')";
            var expected = @"+0122420789ab7809g0i0k0m+";

            Utils.XPathStringTest(xml, testExpression, expected);
        }
    }
}
