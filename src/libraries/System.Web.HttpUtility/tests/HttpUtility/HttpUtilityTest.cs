// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// System.Web.HttpUtilityTest.cs - Unit tests for System.Web.HttpUtility
//
// Author:
//  Sebastien Pouliot  <sebastien@ximian.com>
//
// Copyright (C) 2005 Novell, Inc (http://www.novell.com)
//
// Permission is hereby granted, free of charge, to any person obtaining
// a copy of this software and associated documentation files (the
// "Software"), to deal in the Software without restriction, including
// without limitation the rights to use, copy, modify, merge, publish,
// distribute, sublicense, and/or sell copies of the Software, and to
// permit persons to whom the Software is furnished to do so, subject to
// the following conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF
// MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE
// LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION
// OF CONTRACT, TORT OR OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION
// WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE SOFTWARE.
//

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace System.Web.Tests
{
    public class HttpUtilityTest
    {
        const char TestMaxChar = (char) 0x100;

        #region HtmlAttributeEncode

        public static IEnumerable<object[]> HtmlAttributeEncodeData =>
            new[]
            {
                new object[] {string.Empty, string.Empty},
                new object[] {"&lt;script>", "<script>"},
                new object[] {"&quot;a&amp;b&quot;", "\"a&b\""},
                new object[] {"&#39;string&#39;", "'string'"},
                new object[] {"abc + def!", "abc + def!"},
                new object[] {"This is an &lt;element>!", "This is an <element>!"},
            };

        [Theory]
        [InlineData(null, null)]
        [MemberData(nameof(HtmlAttributeEncodeData))]
        public void HtmlAttributeEncode(string expected, string input)
        {
            Assert.Equal(expected, HttpUtility.HtmlAttributeEncode(input));
        }

        [Fact]
        public void HtmlAttributeEncode_TextWriter_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                HttpUtility.HtmlAttributeEncode("", null);
            });
        }

        [Theory]
        [InlineData("", null)]
        [MemberData(nameof(HtmlAttributeEncodeData))]
        public void HtmlAttributeEncode_TextWriter(string expected, string input)
        {
            var sw = new StringWriter();
            HttpUtility.HtmlAttributeEncode(input, sw);
            Assert.Equal(expected, sw.ToString());
        }

        #endregion HtmlAttributeEncode

        public static IEnumerable<object[]> HtmlEncodeDecodeData =>
            new[]
            {
                new object[] {"", ""},
                new object[] {"<script>", "&lt;script&gt;"},
            };

        #region HtmlDecode

        public static IEnumerable<object[]> HtmlDecodingData =>
            new[]
            {
                new object[] {"\u00E1\u00C1\u00E2\u00C2\u00B4", @"&aacute;&Aacute;&acirc;&Acirc;&acute;"},
                new object[] {"\u00E6\u00C6\u00E0\u00C0\u2135", @"&aelig;&AElig;&agrave;&Agrave;&alefsym;"},
                new object[] {"\u03B1\u0391&\u2227\u2220", @"&alpha;&Alpha;&amp;&and;&ang;"},
                new object[] {"\u0027\u0027&\u2227\u2220", @"&apos;&apos;&amp;&and;&ang;"},
                new object[] {"\u00E5\u00C5\u2248\u00E3\u00C3", @"&aring;&Aring;&asymp;&atilde;&Atilde;"},
                new object[] {"\u00E4\u00C4\u201E\u03B2\u0392", @"&auml;&Auml;&bdquo;&beta;&Beta;"},
                new object[] {"\u00A6\u2022\u2229\u00E7\u00C7", @"&brvbar;&bull;&cap;&ccedil;&Ccedil;"},
                new object[] {"\u00B8\u00A2\u03C7\u03A7\u02C6", @"&cedil;&cent;&chi;&Chi;&circ;"},
                new object[] {"\u2663\u2245\u00A9\u21B5\u222A", @"&clubs;&cong;&copy;&crarr;&cup;"},
                new object[] {"\u00A4\u2020\u2021\u2193\u21D3", @"&curren;&dagger;&Dagger;&darr;&dArr;"},
                new object[] {"\u00B0\u03B4\u0394\u2666\u00F7", @"&deg;&delta;&Delta;&diams;&divide;"},
                new object[] {"\u00E9\u00C9\u00EA\u00CA\u00E8", @"&eacute;&Eacute;&ecirc;&Ecirc;&egrave;"},
                new object[] {"\u00C8\u2205\u2003\u2002\u03B5", @"&Egrave;&empty;&emsp;&ensp;&epsilon;"},
                new object[] {"\u0395\u2261\u03B7\u0397\u00F0", @"&Epsilon;&equiv;&eta;&Eta;&eth;"},
                new object[] {"\u00D0\u00EB\u00CB\u20AC\u2203", @"&ETH;&euml;&Euml;&euro;&exist;"},
                new object[] {"\u0192\u2200\u00BD\u00BC\u00BE", @"&fnof;&forall;&frac12;&frac14;&frac34;"},
                new object[] {"\u2044\u03B3\u0393\u2265>", @"&frasl;&gamma;&Gamma;&ge;&gt;"},
                new object[] {"\u2194\u21D4\u2665\u2026\u00ED", @"&harr;&hArr;&hearts;&hellip;&iacute;"},
                new object[] {"\u00CD\u00EE\u00CE\u00A1\u00EC", @"&Iacute;&icirc;&Icirc;&iexcl;&igrave;"},
                new object[] {"\u00CC\u2111\u221E\u222B\u03B9", @"&Igrave;&image;&infin;&int;&iota;"},
                new object[] {"\u0399\u00BF\u2208\u00EF\u00CF", @"&Iota;&iquest;&isin;&iuml;&Iuml;"},
                new object[] {"\u03BA\u039A\u03BB\u039B\u2329", @"&kappa;&Kappa;&lambda;&Lambda;&lang;"},
                new object[] {"\u00AB\u2190\u21D0\u2308\u201C", @"&laquo;&larr;&lArr;&lceil;&ldquo;"},
                new object[] {"\u2264\u230A\u2217\u25CA\u200E", @"&le;&lfloor;&lowast;&loz;&lrm;"},
                new object[] {"\u2039\u2018<\u00AF\u2014", @"&lsaquo;&lsquo;&lt;&macr;&mdash;"},
                new object[] {"\u00B5\u00B7\u2212\u03BC\u039C", @"&micro;&middot;&minus;&mu;&Mu;"},
                new object[] {"\u2207\u00A0\u2013\u2260\u220B", @"&nabla;&nbsp;&ndash;&ne;&ni;"},
                new object[] {"\u00AC\u2209\u2284\u00F1\u00D1", @"&not;&notin;&nsub;&ntilde;&Ntilde;"},
                new object[] {"\u03BD\u039D\u00F3\u00D3\u00F4", @"&nu;&Nu;&oacute;&Oacute;&ocirc;"},
                new object[] {"\u00D4\u0153\u0152\u00F2\u00D2", @"&Ocirc;&oelig;&OElig;&ograve;&Ograve;"},
                new object[] {"\u203E\u03C9\u03A9\u03BF\u039F", @"&oline;&omega;&Omega;&omicron;&Omicron;"},
                new object[] {"\u2295\u2228\u00AA\u00BA\u00F8", @"&oplus;&or;&ordf;&ordm;&oslash;"},
                new object[] {"\u00D8\u00F5\u00D5\u2297\u00F6", @"&Oslash;&otilde;&Otilde;&otimes;&ouml;"},
                new object[] {"\u00D6\u00B6\u2202\u2030\u22A5", @"&Ouml;&para;&part;&permil;&perp;"},
                new object[] {"\u03C6\u03A6\u03C0\u03A0\u03D6", @"&phi;&Phi;&pi;&Pi;&piv;"},
                new object[] {"\u00B1\u00A3\u2032\u2033\u220F", @"&plusmn;&pound;&prime;&Prime;&prod;"},
                new object[] {"\u221D\u03C8\u03A8\"\u221A", @"&prop;&psi;&Psi;&quot;&radic;"},
                new object[] {"\u232A\u00BB\u2192\u21D2\u2309", @"&rang;&raquo;&rarr;&rArr;&rceil;"},
                new object[] {"\u201D\u211C\u00AE\u230B\u03C1", @"&rdquo;&real;&reg;&rfloor;&rho;"},
                new object[] {"\u03A1\u200F\u203A\u2019\u201A", @"&Rho;&rlm;&rsaquo;&rsquo;&sbquo;"},
                new object[] {"\u0161\u0160\u22C5\u00A7\u00AD", @"&scaron;&Scaron;&sdot;&sect;&shy;"},
                new object[] {"\u03C3\u03A3\u03C2\u223C\u2660", @"&sigma;&Sigma;&sigmaf;&sim;&spades;"},
                new object[] {"\u2282\u2286\u2211\u2283\u00B9", @"&sub;&sube;&sum;&sup;&sup1;"},
                new object[] {"\u00B2\u00B3\u2287\u00DF\u03C4", @"&sup2;&sup3;&supe;&szlig;&tau;"},
                new object[] {"\u03A4\u2234\u03B8\u0398\u03D1", @"&Tau;&there4;&theta;&Theta;&thetasym;"},
                new object[] {"\u2009\u00FE\u00DE\u02DC\u00D7", @"&thinsp;&thorn;&THORN;&tilde;&times;"},
                new object[] {"\u2122\u00FA\u00DA\u2191\u21D1", @"&trade;&uacute;&Uacute;&uarr;&uArr;"},
                new object[] {"\u00FB\u00DB\u00F9\u00D9\u00A8", @"&ucirc;&Ucirc;&ugrave;&Ugrave;&uml;"},
                new object[] {"\u03D2\u03C5\u03A5\u00FC\u00DC", @"&upsih;&upsilon;&Upsilon;&uuml;&Uuml;"},
                new object[] {"\u2118\u03BE\u039E\u00FD\u00DD", @"&weierp;&xi;&Xi;&yacute;&Yacute;"},
                new object[] {"\u00A5\u00FF\u0178\u03B6\u0396", @"&yen;&yuml;&Yuml;&zeta;&Zeta;"},
                new object[] {"\u200D\u200C", @"&zwj;&zwnj;"},
                new object[]
                {
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                },
                new object[]
                {
                    "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF",
                    @"&#160;&#161;&#162;&#163;&#164;&#165;&#166;&#167;&#168;&#169;&#170;&#171;&#172;&#173;&#174;&#175;&#176;&#177;&#178;&#179;&#180;&#181;&#182;&#183;&#184;&#185;&#186;&#187;&#188;&#189;&#190;&#191;&#192;&#193;&#194;&#195;&#196;&#197;&#198;&#199;&#200;&#201;&#202;&#203;&#204;&#205;&#206;&#207;&#208;&#209;&#210;&#211;&#212;&#213;&#214;&#215;&#216;&#217;&#218;&#219;&#220;&#221;&#222;&#223;&#224;&#225;&#226;&#227;&#228;&#229;&#230;&#231;&#232;&#233;&#234;&#235;&#236;&#237;&#238;&#239;&#240;&#241;&#242;&#243;&#244;&#245;&#246;&#247;&#248;&#249;&#250;&#251;&#252;&#253;&#254;&#255;",
                },
                new object[]
                {
                    "\0\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                    @"&#000;&#001;&#002;&#003;&#004;&#005;&#006;&#007;&#008;&#009;&#010;&#011;&#012;&#013;&#014;&#015;&#016;&#017;&#018;&#019;&#020;&#021;&#022;&#023;&#024;&#025;&#026;&#027;&#028;&#029;&#030;&#031;&#032;",
                },
                new object[]
                {
                    "\0\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                    @"&#x00;&#x01;&#x02;&#x03;&#x04;&#x05;&#x06;&#x07;&#x08;&#x09;&#x0A;&#x0B;&#x0C;&#x0D;&#x0E;&#x0F;&#x10;&#x11;&#x12;&#x13;&#x14;&#x15;&#x16;&#x17;&#x18;&#x19;&#x1A;&#x1B;&#x1C;&#x1D;&#x1E;&#x1F;&#x20;",
                },
                new object[]
                {
                    "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF",
                    @"&#xA0;&#xA1;&#xA2;&#xA3;&#xA4;&#xA5;&#xA6;&#xA7;&#xA8;&#xA9;&#xAA;&#xAB;&#xAC;&#xAD;&#xAE;&#xAF;&#xB0;&#xB1;&#xB2;&#xB3;&#xB4;&#xB5;&#xB6;&#xB7;&#xB8;&#xB9;&#xBA;&#xBB;&#xBC;&#xBD;&#xBE;&#xBF;&#xC0;&#xC1;&#xC2;&#xC3;&#xC4;&#xC5;&#xC6;&#xC7;&#xC8;&#xC9;&#xCA;&#xCB;&#xCC;&#xCD;&#xCE;&#xCF;&#xD0;&#xD1;&#xD2;&#xD3;&#xD4;&#xD5;&#xD6;&#xD7;&#xD8;&#xD9;&#xDA;&#xDB;&#xDC;&#xDD;&#xDE;&#xDF;&#xE0;&#xE1;&#xE2;&#xE3;&#xE4;&#xE5;&#xE6;&#xE7;&#xE8;&#xE9;&#xEA;&#xEB;&#xEC;&#xED;&#xEE;&#xEF;&#xF0;&#xF1;&#xF2;&#xF3;&#xF4;&#xF5;&#xF6;&#xF7;&#xF8;&#xF9;&#xFA;&#xFB;&#xFC;&#xFD;&#xFE;&#xFF;",
                },
            };

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlDecodingData))]
        public void HtmlDecode(string decoded, string encoded)
        {
            Assert.Equal(decoded, HttpUtility.HtmlDecode(encoded));
        }

        [Fact]
        public void HtmlDecode_TextWriter_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                HttpUtility.HtmlDecode("", null);
            });
        }

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlDecodingData))]
        public void HtmlDecode_TextWriter(string decoded, string encoded)
        {
            var sw = new StringWriter();
            HttpUtility.HtmlDecode(encoded, sw);
            Assert.Equal(decoded, sw.ToString());
        }

        #endregion HtmlDecode

        #region HtmlEncode

        public static IEnumerable<object[]> HtmlEncodeData =>
            new[]
            {
                new object[]
                {
                    "\u00E1\u00C1\u00E2\u00C2\u00B4\u00E6\u00C6\u00E0\u00C0\u2135\u03B1\u0391&\u2227\u2220\u00E5\u00C5\u2248\u00E3\u00C3\u00E4\u00C4\u201E\u03B2\u0392\u00A6\u2022\u2229\u00E7\u00C7\u00B8\u00A2\u03C7\u03A7\u02C6\u2663\u2245\u00A9\u21B5\u222A\u00A4\u2020\u2021\u2193\u21D3\u00B0\u03B4\u0394\u2666\u00F7\u00E9\u00C9\u00EA\u00CA\u00E8\u00C8\u2205\u2003\u2002\u03B5\u0395\u2261\u03B7\u0397\u00F0\u00D0\u00EB\u00CB\u20AC\u2203\u0192\u2200\u00BD\u00BC\u00BE\u2044\u03B3\u0393\u2265>\u2194\u21D4\u2665\u2026\u00ED\u00CD\u00EE\u00CE\u00A1\u00EC\u00CC\u2111\u221E\u222B\u03B9\u0399\u00BF\u2208\u00EF\u00CF\u03BA\u039A\u03BB\u039B\u2329\u00AB\u2190\u21D0\u2308\u201C\u2264\u230A\u2217\u25CA\u200E\u2039\u2018<\u00AF\u2014\u00B5\u00B7\u2212\u03BC\u039C\u2207\u00A0\u2013\u2260\u220B\u00AC\u2209\u2284\u00F1\u00D1\u03BD\u039D\u00F3\u00D3\u00F4\u00D4\u0153\u0152\u00F2\u00D2\u203E\u03C9\u03A9\u03BF\u039F\u2295\u2228\u00AA\u00BA\u00F8\u00D8\u00F5\u00D5\u2297\u00F6\u00D6\u00B6\u2202\u2030\u22A5\u03C6\u03A6\u03C0\u03A0\u03D6\u00B1\u00A3\u2032\u2033\u220F\u221D\u03C8\u03A8\"\u221A\u232A\u00BB\u2192\u21D2\u2309\u201D\u211C\u00AE\u230B\u03C1\u03A1\u200F\u203A\u2019\u201A\u0161\u0160\u22C5\u00A7\u00AD\u03C3\u03A3\u03C2\u223C\u2660\u2282\u2286\u2211\u2283\u00B9\u00B2\u00B3\u2287\u00DF\u03C4\u03A4\u2234\u03B8\u0398\u03D1\u2009\u00FE\u00DE\u02DC\u00D7\u2122\u00FA\u00DA\u2191\u21D1\u00FB\u00DB\u00F9\u00D9\u00A8\u03D2\u03C5\u03A5\u00FC\u00DC\u2118\u03BE\u039E\u00FD\u00DD\u00A5\u00FF\u0178\u03B6\u0396\u200D\u200C",
                    "&#225;&#193;&#226;&#194;&#180;&#230;&#198;&#224;&#192;\u2135\u03B1\u0391&amp;\u2227\u2220&#229;&#197;\u2248&#227;&#195;&#228;&#196;\u201E\u03B2\u0392&#166;\u2022\u2229&#231;&#199;&#184;&#162;\u03C7\u03A7\u02C6\u2663\u2245&#169;\u21B5\u222A&#164;\u2020\u2021\u2193\u21D3&#176;\u03B4\u0394\u2666&#247;&#233;&#201;&#234;&#202;&#232;&#200;\u2205\u2003\u2002\u03B5\u0395\u2261\u03B7\u0397&#240;&#208;&#235;&#203;\u20AC\u2203\u0192\u2200&#189;&#188;&#190;\u2044\u03B3\u0393\u2265&gt;\u2194\u21D4\u2665\u2026&#237;&#205;&#238;&#206;&#161;&#236;&#204;\u2111\u221E\u222B\u03B9\u0399&#191;\u2208&#239;&#207;\u03BA\u039A\u03BB\u039B\u2329&#171;\u2190\u21D0\u2308\u201C\u2264\u230A\u2217\u25CA\u200E\u2039\u2018&lt;&#175;\u2014&#181;&#183;\u2212\u03BC\u039C\u2207&#160;\u2013\u2260\u220B&#172;\u2209\u2284&#241;&#209;\u03BD\u039D&#243;&#211;&#244;&#212;\u0153\u0152&#242;&#210;\u203E\u03C9\u03A9\u03BF\u039F\u2295\u2228&#170;&#186;&#248;&#216;&#245;&#213;\u2297&#246;&#214;&#182;\u2202\u2030\u22A5\u03C6\u03A6\u03C0\u03A0\u03D6&#177;&#163;\u2032\u2033\u220F\u221D\u03C8\u03A8&quot;\u221A\u232A&#187;\u2192\u21D2\u2309\u201D\u211C&#174;\u230B\u03C1\u03A1\u200F\u203A\u2019\u201A\u0161\u0160\u22C5&#167;&#173;\u03C3\u03A3\u03C2\u223C\u2660\u2282\u2286\u2211\u2283&#185;&#178;&#179;\u2287&#223;\u03C4\u03A4\u2234\u03B8\u0398\u03D1\u2009&#254;&#222;\u02DC&#215;\u2122&#250;&#218;\u2191\u21D1&#251;&#219;&#249;&#217;&#168;\u03D2\u03C5\u03A5&#252;&#220;\u2118\u03BE\u039E&#253;&#221;&#165;&#255;\u0178\u03B6\u0396\u200D\u200C",
                },
                new object[]
                {
                    "\u00E19cP!qdO#hU@mg1\u00C1K%0<}*\u00E2\u00C25[Y;lfMQ$4`\u00B4uim7E`%_1zVDk\u00E6[cM{\u00C6t9y:E8Hb;;$;Y'\u00E0Ua6w\u00C0<$@W9$4NL*h#'\u2135k\\z\u03B1G}{}hC-\u0391|=QhyLT%`&wB!@#x51R 4C\u2227]Z3n\u2220y>:{JZ'v|c0;N\"\u00E5zcWM'z\"g\u00C5o-JX!r.e\u2248Z+BT{wF8+\u00E3Q 6P1o?x\"ef}vU\u00C3+</Nt)TI]s\u00E40Eg_'mn&6WY[8\u00C4ay+ u[3kqoZ\u201Ei6r\u03B2UX\\:_y1A^x.p>+\u0392`uf3/HI\u00A67bCRv%o$X3:\u2022\u2229\u00E7|(fgiA|MBLf=y@\u00C7\u00B8\u00A2R,qDW;F9<m\u03C7U]$)Q`w^KF^(h\u03A7?ukX+O!UOft\u02C6ZE\u2663@MLR(vcH]k8\u2245CU;r#(\u00A97DZ`1>r~.\u21B54B&R\u222A+x2T`q[M-lq'\u00A4~3rp%~-Gd\u2020;35wU+II1tQJ\u2021`NGh[\u2193Lr>74~yHB=&EI\u21D3,u@Jx\u00B0\u03B4cC`2,\u0394o2B]6PP8\u2666|{!wZa&,*N'$6\u00F7-{nVSgO]%(I\u00E96\u00C9\u00EAosx-2xDI!\u00CA_]7Ub%\u00E8YG4`Gx{\u00C8H>vwMPJ\u2205\u2003:Z-u#ph\u2002l,s*8(A\u03B5\u0395Onj|Gy|]iYLPR\u22615Wi:(vZUUK.Yl\u03B7D\u03976T\u00F0T!Z:Nq_0797;!\u00D04]QN\u00EB9+>x9>nm-s8Y\u00CBwZ}vY\u20AC:HHf\u2203;=0,?\u0192Ir`I:i5'\u2200z_$Q<\u00BD_sCF;=$43DpDz]\u00BC.aMTIEwx\\ogn7A\u00BECuJD[Hke#\u2044E]M%\u03B3E:IEk}\u0393{qXfzeUS\u2265kqW yxV>\u2194AzJ:$fJ\u21D43IMDqU\\myWjsL\u2665\u2026Ok\u00EDjt$NKbGr\u00CD\"+alp<\u00EER\u00CE%\u00A1y\u00ECz2 A\u00CC-%;jyMK{Umd\u2111i|}+Za8jyWDS#I\u221E]NyqN*v:m-\u222B03A\u03B9f9m.:+z0@OfVo\u0399_gfPilLZ\u00BF6qqb0|BQ$H%p+d\u2208.Wa=YBfS'd-EO\u00EFISG+=W;GH\u00CF3||b-icT\"qA\u03BA*/\u039A\u03BBN>j}\"Wrq\u039Bt]dm-Xe/v\u2329\\\u00AB$F< X\u2190]=8H8\u21D0c\u2308|\u201CJgZ)+(7,}\u2264s8[\"3%C4JvN\u230AH55TAKEZ*%Z)d.\u2217R9z//!q\u25CAD`643eO\u200E&-L>DsUej\u2039C[n]Q<%UoyO\u2018?zUgpr+62sY<T{7n*^\u00AF4CH]6^e/x/\u2014uT->mQh\\\"\u00B5ZSTN!F(U%5\u00B717:Cu<\u2212)*c2\u03BCT\u039C%:6-e&L[ Xos/4\u2207]Xr\u00A01c=qyv4HSw~HL~\u2013{+qG?/}\u22606`S\",+pL\u220B>\u00ACB9\u2209G;6P]xc 0Bs\u22847,j0Sj2/&\u00F1Fs\u00D1=\u03BDKs*?[54bV1\u039DQ%p6P0.Lrc`y\u00F3A/*`6sBH?67\u00D3&\u00F4\u00D4I\"H\u0153~e9\u0152>o\u00F25eZI}iy?}K\u00D2S\u203EanD1nX\u03C9I\u03A9u\"\u03BF:Mz$(\"joU^[m\u039F7M1f$j>N|Q/@(\u2295de6(\u2228WXb<~;tI?bt#\u00AAU:\u00BA+wb(*cA=\u00F8jb c%*?Uj6<T02\u00D8/A}j'M\u00F5jlfYlR~er7D@3W\u00D5e:XTLF?|\"yd7x\u2297eV6Mmw2{K<l\u00F6%B%/o~r9\u00D6c1Q TJnd^\u00B6;\u2202|\u2030_.\u22A5E_bim;gvA{wq\u03C6e\u03A6^-!Dc\u03C08LB6k4P\u03A0(5D |Y3\u03D6ptuh)3Mv\u00B1TAvFo+;JE,2?\u00A3\"'6F9fRp\u2032,0\u2033<\u220FN\u221DC%}JC7qY(7))UW\u03C8 7=rmQa\u03A8eD!G5e>S~kO\"'4\"/i4\\>!]H;T^0o\u221A8_G`*8&An\\rhc)\u232A&UEk\u00BB-(YtC\u2192(zerUTMTe,'@{\u21D2mlzVhU<S,5}9DM\u2309/%R=10*[{'=:\u201DC0\u211C4HoT?-#+l[SnPs\u00AE0 bV\u230BT\u03C1\u03A1jb1}OJ:,0z6\u200FoTxP\"\"FOT[;\u203A'\u2019-:Ll)I0^$p.\u201AS_\u0161NBr9)K[\u01601\u22C5$-S4/G&u\u00A7= _CqlY1O'\u00ADqNf|&\u03C3Gp}\u03A3P3:8\u03C2\u223C[ItI\u26608\u2282BQn~!KO:+~ma\u2286FV.u 4wD\u2211lE+kQ|gZ];Y\u2283DK69EEM$D\u00B9KVO\u00B2%:~Iq?IUcHr4y\u00B3QP@R't!\u2287v\u00DFYnI@FXxT<\u03C4vL[4H95mf\u03A4F0JzQsrxNZry\u2234Bn#t(\u03B8*O\u0398w=Z%\u03D1+*l^3C)5HCNmR\u2009 %`g|*8DEC\u00FE_[\u00DE'8,?\u02DC}gnaz_U\u00D7-F^\u21229ZDO86\u00FA]y\\ecHQS\u00DAk-07/AT|0Ce\u2191F\u21D1*}e|r$6ln!V`\u00FBA!*8H,m\u00DB~6G6w&G\u00F9sPL6\u00D9Q\u00A8}J^NO}=._Mn\u03D2{&\u03C5=\u03A5WD+f>fy|nNyP*J\u00FCo8,lh\\\u00DCN`'g\u2118(sJ8h3P]cF \u03BEcdQ_OC]U#\u039EBby=S\u00FD9tI_\u00DD}p(D51=X\u00A5cH8L)$*]~=I\u00FFdb\u0178f>J^1Dn\u03B6@(drH;91?{6`xJ\u03964N4[u+5\u200D9.W\\v\u200C]GGtKvCC0`A",
                    "&#225;9cP!qdO#hU@mg1&#193;K%0&lt;}*&#226;&#194;5[Y;lfMQ$4`&#180;uim7E`%_1zVDk&#230;[cM{&#198;t9y:E8Hb;;$;Y&#39;&#224;Ua6w&#192;&lt;$@W9$4NL*h#&#39;\u2135k\\z\u03B1G}{}hC-\u0391|=QhyLT%`&amp;wB!@#x51R 4C\u2227]Z3n\u2220y&gt;:{JZ&#39;v|c0;N&quot;&#229;zcWM&#39;z&quot;g&#197;o-JX!r.e\u2248Z+BT{wF8+&#227;Q 6P1o?x&quot;ef}vU&#195;+&lt;/Nt)TI]s&#228;0Eg_&#39;mn&amp;6WY[8&#196;ay+ u[3kqoZ\u201Ei6r\u03B2UX\\:_y1A^x.p&gt;+\u0392`uf3/HI&#166;7bCRv%o$X3:\u2022\u2229&#231;|(fgiA|MBLf=y@&#199;&#184;&#162;R,qDW;F9&lt;m\u03C7U]$)Q`w^KF^(h\u03A7?ukX+O!UOft\u02C6ZE\u2663@MLR(vcH]k8\u2245CU;r#(&#169;7DZ`1&gt;r~.\u21B54B&amp;R\u222A+x2T`q[M-lq&#39;&#164;~3rp%~-Gd\u2020;35wU+II1tQJ\u2021`NGh[\u2193Lr&gt;74~yHB=&amp;EI\u21D3,u@Jx&#176;\u03B4cC`2,\u0394o2B]6PP8\u2666|{!wZa&amp;,*N&#39;$6&#247;-{nVSgO]%(I&#233;6&#201;&#234;osx-2xDI!&#202;_]7Ub%&#232;YG4`Gx{&#200;H&gt;vwMPJ\u2205\u2003:Z-u#ph\u2002l,s*8(A\u03B5\u0395Onj|Gy|]iYLPR\u22615Wi:(vZUUK.Yl\u03B7D\u03976T&#240;T!Z:Nq_0797;!&#208;4]QN&#235;9+&gt;x9&gt;nm-s8Y&#203;wZ}vY\u20AC:HHf\u2203;=0,?\u0192Ir`I:i5&#39;\u2200z_$Q&lt;&#189;_sCF;=$43DpDz]&#188;.aMTIEwx\\ogn7A&#190;CuJD[Hke#\u2044E]M%\u03B3E:IEk}\u0393{qXfzeUS\u2265kqW yxV&gt;\u2194AzJ:$fJ\u21D43IMDqU\\myWjsL\u2665\u2026Ok&#237;jt$NKbGr&#205;&quot;+alp&lt;&#238;R&#206;%&#161;y&#236;z2 A&#204;-%;jyMK{Umd\u2111i|}+Za8jyWDS#I\u221E]NyqN*v:m-\u222B03A\u03B9f9m.:+z0@OfVo\u0399_gfPilLZ&#191;6qqb0|BQ$H%p+d\u2208.Wa=YBfS&#39;d-EO&#239;ISG+=W;GH&#207;3||b-icT&quot;qA\u03BA*/\u039A\u03BBN&gt;j}&quot;Wrq\u039Bt]dm-Xe/v\u2329\\&#171;$F&lt; X\u2190]=8H8\u21D0c\u2308|\u201CJgZ)+(7,}\u2264s8[&quot;3%C4JvN\u230AH55TAKEZ*%Z)d.\u2217R9z//!q\u25CAD`643eO\u200E&amp;-L&gt;DsUej\u2039C[n]Q&lt;%UoyO\u2018?zUgpr+62sY&lt;T{7n*^&#175;4CH]6^e/x/\u2014uT-&gt;mQh\\&quot;&#181;ZSTN!F(U%5&#183;17:Cu&lt;\u2212)*c2\u03BCT\u039C%:6-e&amp;L[ Xos/4\u2207]Xr&#160;1c=qyv4HSw~HL~\u2013{+qG?/}\u22606`S&quot;,+pL\u220B&gt;&#172;B9\u2209G;6P]xc 0Bs\u22847,j0Sj2/&amp;&#241;Fs&#209;=\u03BDKs*?[54bV1\u039DQ%p6P0.Lrc`y&#243;A/*`6sBH?67&#211;&amp;&#244;&#212;I&quot;H\u0153~e9\u0152&gt;o&#242;5eZI}iy?}K&#210;S\u203EanD1nX\u03C9I\u03A9u&quot;\u03BF:Mz$(&quot;joU^[m\u039F7M1f$j&gt;N|Q/@(\u2295de6(\u2228WXb&lt;~;tI?bt#&#170;U:&#186;+wb(*cA=&#248;jb c%*?Uj6&lt;T02&#216;/A}j&#39;M&#245;jlfYlR~er7D@3W&#213;e:XTLF?|&quot;yd7x\u2297eV6Mmw2{K&lt;l&#246;%B%/o~r9&#214;c1Q TJnd^&#182;;\u2202|\u2030_.\u22A5E_bim;gvA{wq\u03C6e\u03A6^-!Dc\u03C08LB6k4P\u03A0(5D |Y3\u03D6ptuh)3Mv&#177;TAvFo+;JE,2?&#163;&quot;&#39;6F9fRp\u2032,0\u2033&lt;\u220FN\u221DC%}JC7qY(7))UW\u03C8 7=rmQa\u03A8eD!G5e&gt;S~kO&quot;&#39;4&quot;/i4\\&gt;!]H;T^0o\u221A8_G`*8&amp;An\\rhc)\u232A&amp;UEk&#187;-(YtC\u2192(zerUTMTe,&#39;@{\u21D2mlzVhU&lt;S,5}9DM\u2309/%R=10*[{&#39;=:\u201DC0\u211C4HoT?-#+l[SnPs&#174;0 bV\u230BT\u03C1\u03A1jb1}OJ:,0z6\u200FoTxP&quot;&quot;FOT[;\u203A&#39;\u2019-:Ll)I0^$p.\u201AS_\u0161NBr9)K[\u01601\u22C5$-S4/G&amp;u&#167;= _CqlY1O&#39;&#173;qNf|&amp;\u03C3Gp}\u03A3P3:8\u03C2\u223C[ItI\u26608\u2282BQn~!KO:+~ma\u2286FV.u 4wD\u2211lE+kQ|gZ];Y\u2283DK69EEM$D&#185;KVO&#178;%:~Iq?IUcHr4y&#179;QP@R&#39;t!\u2287v&#223;YnI@FXxT&lt;\u03C4vL[4H95mf\u03A4F0JzQsrxNZry\u2234Bn#t(\u03B8*O\u0398w=Z%\u03D1+*l^3C)5HCNmR\u2009 %`g|*8DEC&#254;_[&#222;&#39;8,?\u02DC}gnaz_U&#215;-F^\u21229ZDO86&#250;]y\\ecHQS&#218;k-07/AT|0Ce\u2191F\u21D1*}e|r$6ln!V`&#251;A!*8H,m&#219;~6G6w&amp;G&#249;sPL6&#217;Q&#168;}J^NO}=._Mn\u03D2{&amp;\u03C5=\u03A5WD+f&gt;fy|nNyP*J&#252;o8,lh\\&#220;N`&#39;g\u2118(sJ8h3P]cF \u03BEcdQ_OC]U#\u039EBby=S&#253;9tI_&#221;}p(D51=X&#165;cH8L)$*]~=I&#255;db\u0178f&gt;J^1Dn\u03B6@(drH;91?{6`xJ\u03964N4[u+5\u200D9.W\\v\u200C]GGtKvCC0`A",
                },
                new object[]
                {
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                    @"&amp;aacute&amp;Aacute&amp;acirc&amp;Acirc&amp;acute&amp;aelig&amp;AElig&amp;agrave&amp;Agrave&amp;alefsym&amp;alpha&amp;Alpha&amp;amp&amp;and&amp;ang&amp;aring&amp;Aring&amp;asymp&amp;atilde&amp;Atilde&amp;auml&amp;Auml&amp;bdquo&amp;beta&amp;Beta&amp;brvbar&amp;bull&amp;cap&amp;ccedil&amp;Ccedil&amp;cedil&amp;cent&amp;chi&amp;Chi&amp;circ&amp;clubs&amp;cong&amp;copy&amp;crarr&amp;cup&amp;curren&amp;dagger&amp;Dagger&amp;darr&amp;dArr&amp;deg&amp;delta&amp;Delta&amp;diams&amp;divide&amp;eacute&amp;Eacute&amp;ecirc&amp;Ecirc&amp;egrave&amp;Egrave&amp;empty&amp;emsp&amp;ensp&amp;epsilon&amp;Epsilon&amp;equiv&amp;eta&amp;Eta&amp;eth&amp;ETH&amp;euml&amp;Euml&amp;euro&amp;exist&amp;fnof&amp;forall&amp;frac12&amp;frac14&amp;frac34&amp;frasl&amp;gamma&amp;Gamma&amp;ge&amp;gt&amp;harr&amp;hArr&amp;hearts&amp;hellip&amp;iacute&amp;Iacute&amp;icirc&amp;Icirc&amp;iexcl&amp;igrave&amp;Igrave&amp;image&amp;infin&amp;int&amp;iota&amp;Iota&amp;iquest&amp;isin&amp;iuml&amp;Iuml&amp;kappa&amp;Kappa&amp;lambda&amp;Lambda&amp;lang&amp;laquo&amp;larr&amp;lArr&amp;lceil&amp;ldquo&amp;le&amp;lfloor&amp;lowast&amp;loz&amp;lrm&amp;lsaquo&amp;lsquo&amp;lt&amp;macr&amp;mdash&amp;micro&amp;middot&amp;minus&amp;mu&amp;Mu&amp;nabla&amp;nbsp&amp;ndash&amp;ne&amp;ni&amp;not&amp;notin&amp;nsub&amp;ntilde&amp;Ntilde&amp;nu&amp;Nu&amp;oacute&amp;Oacute&amp;ocirc&amp;Ocirc&amp;oelig&amp;OElig&amp;ograve&amp;Ograve&amp;oline&amp;omega&amp;Omega&amp;omicron&amp;Omicron&amp;oplus&amp;or&amp;ordf&amp;ordm&amp;oslash&amp;Oslash&amp;otilde&amp;Otilde&amp;otimes&amp;ouml&amp;Ouml&amp;para&amp;part&amp;permil&amp;perp&amp;phi&amp;Phi&amp;pi&amp;Pi&amp;piv&amp;plusmn&amp;pound&amp;prime&amp;Prime&amp;prod&amp;prop&amp;psi&amp;Psi&amp;quot&amp;radic&amp;rang&amp;raquo&amp;rarr&amp;rArr&amp;rceil&amp;rdquo&amp;real&amp;reg&amp;rfloor&amp;rho&amp;Rho&amp;rlm&amp;rsaquo&amp;rsquo&amp;sbquo&amp;scaron&amp;Scaron&amp;sdot&amp;sect&amp;shy&amp;sigma&amp;Sigma&amp;sigmaf&amp;sim&amp;spades&amp;sub&amp;sube&amp;sum&amp;sup&amp;sup1&amp;sup2&amp;sup3&amp;supe&amp;szlig&amp;tau&amp;Tau&amp;there4&amp;theta&amp;Theta&amp;thetasym&amp;thinsp&amp;thorn&amp;THORN&amp;tilde&amp;times&amp;trade&amp;uacute&amp;Uacute&amp;uarr&amp;uArr&amp;ucirc&amp;Ucirc&amp;ugrave&amp;Ugrave&amp;uml&amp;upsih&amp;upsilon&amp;Upsilon&amp;uuml&amp;Uuml&amp;weierp&amp;xi&amp;Xi&amp;yacute&amp;Yacute&amp;yen&amp;yuml&amp;Yuml&amp;zeta&amp;Zeta&amp;zwj&amp;zwnj",
                },
                new object[]
                {
                    "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF",
                    @"&#160;&#161;&#162;&#163;&#164;&#165;&#166;&#167;&#168;&#169;&#170;&#171;&#172;&#173;&#174;&#175;&#176;&#177;&#178;&#179;&#180;&#181;&#182;&#183;&#184;&#185;&#186;&#187;&#188;&#189;&#190;&#191;&#192;&#193;&#194;&#195;&#196;&#197;&#198;&#199;&#200;&#201;&#202;&#203;&#204;&#205;&#206;&#207;&#208;&#209;&#210;&#211;&#212;&#213;&#214;&#215;&#216;&#217;&#218;&#219;&#220;&#221;&#222;&#223;&#224;&#225;&#226;&#227;&#228;&#229;&#230;&#231;&#232;&#233;&#234;&#235;&#236;&#237;&#238;&#239;&#240;&#241;&#242;&#243;&#244;&#245;&#246;&#247;&#248;&#249;&#250;&#251;&#252;&#253;&#254;&#255;",
                },
                new object[]
                {
                    "&#000;\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                    "&amp;#000;\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                },
                new object[]
                {
                    "&#x00;\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                    "&amp;#x00;\x1\x2\x3\x4\x5\x6\x7\x8\x9\xa\xb\xc\xd\xe\xf\x10\x11\x12\x13\x14\x15\x16\x17\x18\x19\x1a\x1b\x1c\x1d\x1e\x1f ",
                },
                new object[]
                {
                    "\u00A0\u00A1\u00A2\u00A3\u00A4\u00A5\u00A6\u00A7\u00A8\u00A9\u00AA\u00AB\u00AC\u00AD\u00AE\u00AF\u00B0\u00B1\u00B2\u00B3\u00B4\u00B5\u00B6\u00B7\u00B8\u00B9\u00BA\u00BB\u00BC\u00BD\u00BE\u00BF\u00C0\u00C1\u00C2\u00C3\u00C4\u00C5\u00C6\u00C7\u00C8\u00C9\u00CA\u00CB\u00CC\u00CD\u00CE\u00CF\u00D0\u00D1\u00D2\u00D3\u00D4\u00D5\u00D6\u00D7\u00D8\u00D9\u00DA\u00DB\u00DC\u00DD\u00DE\u00DF\u00E0\u00E1\u00E2\u00E3\u00E4\u00E5\u00E6\u00E7\u00E8\u00E9\u00EA\u00EB\u00EC\u00ED\u00EE\u00EF\u00F0\u00F1\u00F2\u00F3\u00F4\u00F5\u00F6\u00F7\u00F8\u00F9\u00FA\u00FB\u00FC\u00FD\u00FE\u00FF",
                    @"&#160;&#161;&#162;&#163;&#164;&#165;&#166;&#167;&#168;&#169;&#170;&#171;&#172;&#173;&#174;&#175;&#176;&#177;&#178;&#179;&#180;&#181;&#182;&#183;&#184;&#185;&#186;&#187;&#188;&#189;&#190;&#191;&#192;&#193;&#194;&#195;&#196;&#197;&#198;&#199;&#200;&#201;&#202;&#203;&#204;&#205;&#206;&#207;&#208;&#209;&#210;&#211;&#212;&#213;&#214;&#215;&#216;&#217;&#218;&#219;&#220;&#221;&#222;&#223;&#224;&#225;&#226;&#227;&#228;&#229;&#230;&#231;&#232;&#233;&#234;&#235;&#236;&#237;&#238;&#239;&#240;&#241;&#242;&#243;&#244;&#245;&#246;&#247;&#248;&#249;&#250;&#251;&#252;&#253;&#254;&#255;",
                },
                new object[]
                {
                    new NullToString(),
                    ""
                }
            };

        private sealed class NullToString
        {
            public override string ToString() => null;
        }

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlEncodeData))]
        [InlineData(2, "2")]
        [InlineData("", "")]
        [InlineData(null, null)]
        public void HtmlEncode(object decoded, string encoded)
        {
            if (decoded is string s)
            {
                Assert.Equal(encoded, HttpUtility.HtmlEncode(s));

                var sw = new StringWriter();
                HttpUtility.HtmlEncode(s, sw);
                Assert.Equal(encoded, sw.ToString());
            }

            Assert.Equal(encoded, HttpUtility.HtmlEncode(decoded));
        }

        [Fact]
        public void HtmlEncode_TextWriter_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                HttpUtility.HtmlEncode("string", null);
            });
        }

        #endregion HtmlEncode

        #region JavaScriptStringEncode

        public static IEnumerable<object[]> JavaScriptStringEncodeData
        {
            get
            {
                yield return new object[] { null, "" };
                yield return new object[] { "", "" };
                yield return new object[] {"No escaping needed.", "No escaping needed."};
                yield return new object[] {"The \t and \n will need to be escaped.", "The \\t and \\n will need to be escaped."};
                for (char c = char.MinValue; c < TestMaxChar; c++)
                {
                    if (c >= 0 && c <= 7 || c == 11 || c >= 14 && c <= 31 || c == 38 || c == 39 || c == 60 || c == 62 || c == 133 || c == 8232 || c == 8233)
                    {
                        yield return new object[] { c.ToString(), $"\\u{(int)c:x4}" };
                    }
                    else
                    {
                        switch ((int)c)
                        {
                            case 8:
                                yield return new object[] { c.ToString(), "\\b" };
                                break;
                            case 9:
                                yield return new object[] { c.ToString(), "\\t" };
                                break;
                            case 10:
                                yield return new object[] { c.ToString(), "\\n" };
                                break;
                            case 12:
                                yield return new object[] { c.ToString(), "\\f" };
                                break;
                            case 13:
                                yield return new object[] { c.ToString(), "\\r" };
                                break;
                            case 34:
                                yield return new object[] { c.ToString(), "\\\"" };
                                break;
                            case 92:
                                yield return new object[] { c.ToString(), "\\\\" };
                                break;
                            default:
                                yield return new object[] { c.ToString(), c.ToString() };
                                break;
                        }
                    }
                }
            }
        }

        [Theory]
        [MemberData(nameof(JavaScriptStringEncodeData))]
        public void JavaScriptStringEncode(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.JavaScriptStringEncode(decoded));
        }

        [Theory]
        [MemberData(nameof(JavaScriptStringEncodeData))]
        public void JavaScriptStringEncode_AddQuotes(string decoded, string encoded)
        {
            Assert.Equal("\"" + encoded + "\"", HttpUtility.JavaScriptStringEncode(decoded, true));
        }


        [Theory]
        [MemberData(nameof(JavaScriptStringEncodeData))]
        public void JavaScriptStringEncode_ExplicitDontAddQuotes(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.JavaScriptStringEncode(decoded, false));
        }

        #endregion JavaScriptStringEncode

        #region ParseQueryString

        private static string UnicodeStr
            => new string(new[] { '\u304a', '\u75b2', '\u308c', '\u69d8', '\u3067', '\u3059' });

        public static IEnumerable<object[]> ParseQueryStringData =>
            new[]
            {
                new object[] {"name=value", new[] {"name"}, new[] {new[] {"value"}}},
                new object[] {"name=value&foo=bar", new[] {"name", "foo"}, new[] {new[] {"value"}, new[] {"bar"}}},
                new object[] {"name=value&name=bar", new[] {"name"}, new[] {new[] {"value", "bar"}}},
                new object[] {"value", new string[] {null}, new[] {new[] {"value"}}},
                new object[] {"name=value&bar", new[] {"name", null}, new[] {new[] {"value"}, new[] {"bar"}}},
                new object[] {"bar&name=value", new[] {null, "name"}, new[] {new[] {"bar"}, new[] {"value"}}},
                new object[] {"value&bar", new string[] {null}, new[] {new[] {"value", "bar"}}},
                new object[] {"", new string[] {}, new string[][] {}},
                new object[] {"=", new[] {""}, new[] {new[] {""}}},
                new object[] {"&", new string[] {null}, new[] {new[] {"", ""}}},
                new object[]
                {
                    HttpUtility.UrlEncode(UnicodeStr) + "=" + HttpUtility.UrlEncode(UnicodeStr),
                    new[] {UnicodeStr},
                    new[] {new[] {UnicodeStr}}
                },
                new object[] {"name=value=test", new[] {"name"}, new[] {new[] {"value=test"}}},
                new object[] { "name=value&#xe9;", new[] {"name", null}, new[] {new[] {"value"}, new[] { "#xe9;" } }},
                new object[] { "name=value&amp;name2=value2", new[] {"name", "amp;name2"}, new[] {new[] {"value"}, new[] { "value2" } }},
                new object[] {"name=value=test+phrase", new[] {"name"}, new[] {new[] {"value=test phrase"}}},
            };

        public static IEnumerable<object[]> ParseQueryStringDataQ =>
            ParseQueryStringData.Select(a => new object[] { "?" + (string)a[0] }.Concat(a.Skip(1)).ToArray())
                .Concat(new[]
                    {
                        new object[] { "??name=value=test", new[] { "?name" }, new[] { new[] { "value=test" }}},
                        new object[] { "?", Array.Empty<string>(), Array.Empty<IList<string>>()}
                    });

        [Theory]
        [MemberData(nameof(ParseQueryStringData))]
        [MemberData(nameof(ParseQueryStringDataQ))]
        public void ParseQueryString(string input, IList<string> keys, IList<IList<string>> values)
        {
            var parsed = HttpUtility.ParseQueryString(input);
            Assert.Equal(keys.Count, parsed.Count);
            for (int i = 0; i < keys.Count; i++)
            {
                Assert.Equal(keys[i], parsed.GetKey(i));
                string[] actualValues = parsed.GetValues(i);
                Assert.Equal<string>(values[i], actualValues);
            }
        }

        [Fact]
        public void ParseQueryString_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                HttpUtility.ParseQueryString(null);
            });
        }

        [Fact]
        public void ParseQueryString_Encoding_null()
        {
            Assert.Throws<ArgumentNullException>(() =>
            {
                HttpUtility.ParseQueryString("", null);
            });
        }

        [Fact]
        public void ParseQueryString_ToString()
        {
            var parsed = HttpUtility.ParseQueryString("");
            Assert.Empty(parsed.ToString());
            parsed.Add("ReturnUrl", @"http://localhost/login/authenticate?ReturnUrl=http://localhost/secured_area&__provider__=google");

            var expected = "ReturnUrl=http%3a%2f%2flocalhost%2flogin%2fauthenticate%3fReturnUrl%3dhttp%3a%2f%2flocalhost%2fsecured_area%26__provider__%3dgoogle";
            Assert.Equal(expected, parsed.ToString());
            Assert.Equal(expected, HttpUtility.ParseQueryString(expected).ToString());
        }

        #endregion ParseQueryString

        #region UrlDecode(ToBytes)

        public static IEnumerable<object[]> UrlDecodeData =>
            new[]
            {
                new object[] { "http://127.0.0.1:8080/appDir/page.aspx?foo=bar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%%61r"},
                new object[] {"http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%%61r"},
                new object[] {"http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r", "http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=ba%r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61%r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=bar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%u0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%%u0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%uu0061r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%uu0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=bar baz", "http://127.0.0.1:8080/appDir/page.aspx?foo=bar+baz"},
                new object[] { "http://example.net/\U00010000", "http://example.net/\U00010000" },
                new object[] { "http://example.net/\uFFFD", "http://example.net/\uD800" },
                new object[] { "http://example.net/\uFFFDa", "http://example.net/\uD800a" },
                new object[] { "http://example.net/\uFFFD", "http://example.net/\uDC00" },
                new object[] { "http://example.net/\uFFFDa", "http://example.net/\uDC00a" },
                // The "Baz" portion of "http://example.net/Baz" has been double-encoded - one iteration of UrlDecode() should produce a once-encoded string.
                new object[] { "http://example.net/%42%61%7A", "http://example.net/%2542%2561%257A"},
                // The second iteration should return the original string
                new object[] { "http://example.net/Baz", "http://example.net/%42%61%7A"}
            };

        public static IEnumerable<object[]> UrlDecodeDataToBytes =>
            new[]
            {
                new object[] { "http://127.0.0.1:8080/appDir/page.aspx?foo=bar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%%61r"},
                new object[] {"http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%ar", "http://127.0.0.1:8080/app%Dir/page.aspx?foo=b%%61r"},
                new object[] {"http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r", "http://127.0.0.1:8080/app%%Dir/page.aspx?foo=b%%r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=ba%r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%61%r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%uu0061r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%uu0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%u0061r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%u0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=b%%u0061r", "http://127.0.0.1:8080/appDir/page.aspx?foo=b%%u0061r"},
                new object[] {"http://127.0.0.1:8080/appDir/page.aspx?foo=bar baz", "http://127.0.0.1:8080/appDir/page.aspx?foo=bar+baz"},
                new object[] { "http://example.net/\U00010000", "http://example.net/\U00010000" },
                new object[] { "http://example.net/\uFFFD", "http://example.net/\uD800" },
                new object[] { "http://example.net/\uFFFDa", "http://example.net/\uD800a" },
                new object[] { "http://example.net/\uFFFD", "http://example.net/\uDC00" },
                new object[] { "http://example.net/\uFFFDa", "http://example.net/\uDC00a" }
            };

        [Theory]
        [MemberData(nameof(UrlDecodeData))]
        public void UrlDecode(string decoded, string encoded)
        {
            Assert.Equal(decoded, HttpUtility.UrlDecode(encoded));
        }

        [Fact]
        public void UrlDecode_null()
        {
            Assert.Null(HttpUtility.UrlDecode(default(string), Encoding.UTF8));
            Assert.Null(HttpUtility.UrlDecode(default(byte[]), Encoding.UTF8));
            Assert.Null(HttpUtility.UrlDecode(null));
            Assert.Null(HttpUtility.UrlDecode(null, 2, 0, Encoding.UTF8));
            Assert.Throws<ArgumentNullException>("bytes", () => HttpUtility.UrlDecode(null, 2, 3, Encoding.UTF8));
        }

        [Fact]
        public void UrlDecode_OutOfRange()
        {
            byte[] bytes = { 0, 1, 2 };
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlDecode(bytes, -1, 2, Encoding.UTF8));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlDecode(bytes, 14, 2, Encoding.UTF8));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlDecode(bytes, 1, 12, Encoding.UTF8));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlDecode(bytes, 1, -12, Encoding.UTF8));
        }

        [Theory]
        [MemberData(nameof(UrlDecodeDataToBytes))]
        public void UrlDecodeToBytes(string decoded, string encoded)
        {
            Assert.Equal(decoded, Encoding.UTF8.GetString(HttpUtility.UrlDecodeToBytes(encoded, Encoding.UTF8)));
        }

        [Theory]
        [MemberData(nameof(UrlDecodeDataToBytes))]
        public void UrlDecodeToBytes_DefaultEncoding(string decoded, string encoded)
        {
            Assert.Equal(decoded, Encoding.UTF8.GetString(HttpUtility.UrlDecodeToBytes(encoded)));
        }

        [Fact]
        public void UrlDecodeToBytes_null()
        {
            Assert.Null(HttpUtility.UrlDecodeToBytes(default(byte[])));
            Assert.Null(HttpUtility.UrlDecodeToBytes(default(string)));
            Assert.Null(HttpUtility.UrlDecodeToBytes(default(string), Encoding.UTF8));
            Assert.Null(HttpUtility.UrlDecodeToBytes(default(byte[]), 2, 0));
            Assert.Throws<ArgumentNullException>("bytes", () => HttpUtility.UrlDecodeToBytes(default(byte[]), 2, 3));
        }

        [Fact]
        public void UrlDecodeToBytes_OutOfRange()
        {
            byte[] bytes = { 0, 1, 2 };
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlDecodeToBytes(bytes, -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlDecodeToBytes(bytes, 14, 2));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlDecodeToBytes(bytes, 1, 12));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlDecodeToBytes(bytes, 1, -12));
        }

        [Theory]
        [MemberData(nameof(UrlDecodeData))]
        public void UrlDecode_ByteArray(string decoded, string encoded)
        {
            Assert.Equal(decoded, HttpUtility.UrlDecode(Encoding.UTF8.GetBytes(encoded), Encoding.UTF8));
        }

        #endregion UrlDecode(ToBytes)

        #region UrlEncode(ToBytes)

        static bool IsUrlSafeChar(char c)
        {
            if (char.IsAsciiLetterOrDigit(c))
            {
                return true;
            }
            switch (c)
            {
                case '-':
                case '_':
                case '.':
                case '!':
                case '*':
                case '(':
                case ')':
                    return true;
            }
            return false;
        }

        static string UrlEncodeChar(char c)
        {
            if (IsUrlSafeChar(c))
            {
                return c.ToString();
            }
            if (c == ' ')
            {
                return "+";
            }
            byte[] bytes = Encoding.UTF8.GetBytes(c.ToString());
            return string.Join("", bytes.Select(b => $"%{b:x2}"));
        }

        public static IEnumerable<object[]> UrlEncodeData
        {
            get
            {
                yield return new object[] { "", "" };
                for (char c = char.MinValue; c < TestMaxChar; c++)
                {
                    yield return new object[] { c.ToString(), UrlEncodeChar(c) };
                }
            }
        }

        [Theory]
        [InlineData(null, null)]
        [MemberData(nameof(UrlEncodeData))]
        public void UrlEncode(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.UrlEncode(decoded));
        }

        [Theory]
        [MemberData(nameof(UrlEncodeData))]
        public void UrlEncodeToBytes(string decoded, string encoded)
        {
            Assert.Equal(encoded, Encoding.UTF8.GetString(HttpUtility.UrlEncodeToBytes(decoded, Encoding.UTF8)));
        }

        [Theory]
        [MemberData(nameof(UrlEncodeData))]
        public void UrlEncodeToBytes_DefaultEncoding(string decoded, string encoded)
        {
            Assert.Equal(encoded, Encoding.UTF8.GetString(HttpUtility.UrlEncodeToBytes(decoded)));
        }

        [Theory, MemberData(nameof(UrlEncodeData))]
        public void UrlEncodeToBytesExplicitSize(string decoded, string encoded)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(decoded);
            Assert.Equal(encoded, Encoding.UTF8.GetString(HttpUtility.UrlEncodeToBytes(bytes, 0, bytes.Length)));
        }


        [Theory]
        [InlineData(" abc defgh", "abc+def", 1, 7)]
        [InlineData(" abc defgh", "", 1, 0)]
        public void UrlEncodeToBytesExplicitSize_WithOffsetAndCount(string decoded, string encoded, int offset, int count)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(decoded);
            Assert.Equal(encoded, Encoding.UTF8.GetString(HttpUtility.UrlEncodeToBytes(bytes, offset, count)));
        }

        [Theory]
        [InlineData("abc def", " abc+defgh", 1, 7)]
        [InlineData("", " abc defgh", 1, 0)]
        public void UrlDecodeToBytesExplicitSize(string decoded, string encoded, int offset, int count)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(encoded);
            Assert.Equal(decoded, Encoding.UTF8.GetString(HttpUtility.UrlDecodeToBytes(bytes, offset, count)));
        }

        [Fact]
        public void UrlEncodeToBytes_null()
        {
            Assert.Null(HttpUtility.UrlEncodeToBytes(null, Encoding.UTF8));
            Assert.Null(HttpUtility.UrlEncodeToBytes(default(byte[])));
            Assert.Null(HttpUtility.UrlEncodeToBytes(default(string)));
            Assert.Null(HttpUtility.UrlEncodeToBytes(null, 2, 0));
            Assert.Throws<ArgumentNullException>("bytes", () => HttpUtility.UrlEncodeToBytes(null, 2, 3));
        }

        [Fact]
        public void UrlEncodeToBytes_OutOfRange()
        {
            byte[] bytes = { 0, 1, 2 };
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlEncodeToBytes(bytes, -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlEncodeToBytes(bytes, 14, 2));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlEncodeToBytes(bytes, 1, 12));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlEncodeToBytes(bytes, 1, -12));
        }

        [Theory]
        [MemberData(nameof(UrlEncodeData))]
        public void UrlEncode_ByteArray(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.UrlEncode(Encoding.UTF8.GetBytes(decoded)));
        }

        [Fact]
        public void UrlEncode_null()
        {
            Assert.Null(HttpUtility.UrlEncode((byte[])null));
            Assert.Null(HttpUtility.UrlEncode((string)null));
            Assert.Null(HttpUtility.UrlEncode(null, Encoding.UTF8));
            Assert.Null(HttpUtility.UrlEncode(null, 2, 3));
        }

        [Fact]
        public void UrlEncode_OutOfRange()
        {
            byte[] bytes = {0, 1, 2};
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlEncode(bytes, -1, 2));
            Assert.Throws<ArgumentOutOfRangeException>("offset", () => HttpUtility.UrlEncode(bytes, 14, 2));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlEncode(bytes, 1, 12));
            Assert.Throws<ArgumentOutOfRangeException>("count", () => HttpUtility.UrlEncode(bytes, 1, -12));
        }

        #endregion UrlEncode(ToBytes)

        #region UrlEncodeUnicode

        public static IEnumerable<object[]> UrlEncodeUnicodeData =>
            new[]
            {
                new object[] {null, null},
                new object[] {"", ""},
                new object[] {" ", "+"},
                new object[] {"a", "a"},
                new object[] {"_", "_"},
                new object[] {"?", "%3f"},
                new object[] {"\u00A0", "%u00a0"},
                new object[] {"\u202E", "%u202e"},
            };

        [Theory]
        [MemberData(nameof(UrlEncodeUnicodeData))]
        public void UrlEncodeUnicode(string decoded, string encoded)
        {
#pragma warning disable 618
            Assert.Equal(encoded, HttpUtility.UrlEncodeUnicode(decoded));
#pragma warning restore 618
        }

        [Theory]
        [MemberData(nameof(UrlEncodeUnicodeData))]
        public void UrlEncodeUnicodeToBytes(string decoded, string encoded)
        {
#pragma warning disable 618
            var bytes = HttpUtility.UrlEncodeUnicodeToBytes(decoded);
            Assert.Equal(encoded, bytes == null ? null : Encoding.ASCII.GetString(bytes));
#pragma warning restore 618
        }

        #endregion UrlEnocdeUnicode

        [Theory]
        [InlineData(null, null)]
        [InlineData(" ", "%20")]
        [InlineData("\n", "%0a")]
        [InlineData("default.xxx?sdsd=sds", "default.xxx?sdsd=sds")]
        [InlineData("?sdsd=sds", "?sdsd=sds")]
        [InlineData("", "")]
        [InlineData("http://example.net/default.xxx?sdsd=sds", "http://example.net/default.xxx?sdsd=sds")]
        [InlineData("http://example.net:8080/default.xxx?sdsd=sds", "http://example.net:8080/default.xxx?sdsd=sds")]
        [InlineData("http://eXample.net:80/default.xxx?sdsd=sds", "http://eXample.net:80/default.xxx?sdsd=sds")]
        [InlineData("http://EXAMPLE.NET/default.xxx?sdsd=sds", "http://EXAMPLE.NET/default.xxx?sdsd=sds")]
        [InlineData("http://EXAMPLE.NET/d\u00E9fault.xxx?sdsd=sds", "http://EXAMPLE.NET/d%c3%a9fault.xxx?sdsd=sds")]
        [InlineData("file:///C/Users", "file:///C/Users")]
        [InlineData("mailto:user@example.net", "mailto:user@example.net")]
        [InlineData("http://example\u200E.net/", "http://example%e2%80%8e.net/")]
        public void UrlPathEncode(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.UrlPathEncode(decoded));
        }

        [Theory]
        [InlineData("")]
        [InlineData("name=foo&desc=foo")]
        [InlineData("type=foo&type=bar")]
        [InlineData("name=&desc=foo")]
        [InlineData("name=&name=foo")]
        [InlineData("foo&bar")]
        [InlineData("foo&name=bar")]
        [InlineData("name=bar&foo&foo")]
        public void ParseAndToStringMaintainAllKeyValuePairs(string input)
        {
            var values = HttpUtility.ParseQueryString(input);
            var output = values.ToString();
            Assert.Equal(input, output);
        }
    }
}
