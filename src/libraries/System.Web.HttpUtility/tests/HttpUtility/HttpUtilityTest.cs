// Licensed to the .NET Foundation under one or more agreements.
// See the LICENSE file in the project root for more information.

//
// System.Web.HttpUtilityTest.cs - Unit tests for System.Web.HttpUtility
//
// Author:
//	Sebastien Pouliot  <sebastien@ximian.com>
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
                new object[] {@"áÁâÂ´", @"&aacute;&Aacute;&acirc;&Acirc;&acute;"},
                new object[] {@"æÆàÀℵ", @"&aelig;&AElig;&agrave;&Agrave;&alefsym;"},
                new object[] {@"αΑ&∧∠", @"&alpha;&Alpha;&amp;&and;&ang;"},
                new object[] {@"åÅ≈ãÃ", @"&aring;&Aring;&asymp;&atilde;&Atilde;"},
                new object[] {@"äÄ„βΒ", @"&auml;&Auml;&bdquo;&beta;&Beta;"},
                new object[] {@"¦•∩çÇ", @"&brvbar;&bull;&cap;&ccedil;&Ccedil;"},
                new object[] {@"¸¢χΧˆ", @"&cedil;&cent;&chi;&Chi;&circ;"},
                new object[] {@"♣≅©↵∪", @"&clubs;&cong;&copy;&crarr;&cup;"},
                new object[] {@"¤†‡↓⇓", @"&curren;&dagger;&Dagger;&darr;&dArr;"},
                new object[] {@"°δΔ♦÷", @"&deg;&delta;&Delta;&diams;&divide;"},
                new object[] {@"éÉêÊè", @"&eacute;&Eacute;&ecirc;&Ecirc;&egrave;"},
                new object[] {@"È∅  ε", @"&Egrave;&empty;&emsp;&ensp;&epsilon;"},
                new object[] {@"Ε≡ηΗð", @"&Epsilon;&equiv;&eta;&Eta;&eth;"},
                new object[] {@"ÐëË€∃", @"&ETH;&euml;&Euml;&euro;&exist;"},
                new object[] {@"ƒ∀½¼¾", @"&fnof;&forall;&frac12;&frac14;&frac34;"},
                new object[] {@"⁄γΓ≥>", @"&frasl;&gamma;&Gamma;&ge;&gt;"},
                new object[] {@"↔⇔♥…í", @"&harr;&hArr;&hearts;&hellip;&iacute;"},
                new object[] {@"ÍîÎ¡ì", @"&Iacute;&icirc;&Icirc;&iexcl;&igrave;"},
                new object[] {@"Ìℑ∞∫ι", @"&Igrave;&image;&infin;&int;&iota;"},
                new object[] {@"Ι¿∈ïÏ", @"&Iota;&iquest;&isin;&iuml;&Iuml;"},
                new object[] {@"κΚλΛ〈", @"&kappa;&Kappa;&lambda;&Lambda;&lang;"},
                new object[] {@"«←⇐⌈“", @"&laquo;&larr;&lArr;&lceil;&ldquo;"},
                new object[] {"≤⌊∗◊\u200E", @"&le;&lfloor;&lowast;&loz;&lrm;"},
                new object[] {@"‹‘<¯—", @"&lsaquo;&lsquo;&lt;&macr;&mdash;"},
                new object[] {@"µ·−μΜ", @"&micro;&middot;&minus;&mu;&Mu;"},
                new object[] {"∇\u00A0–≠∋", @"&nabla;&nbsp;&ndash;&ne;&ni;"},
                new object[] {@"¬∉⊄ñÑ", @"&not;&notin;&nsub;&ntilde;&Ntilde;"},
                new object[] {@"νΝóÓô", @"&nu;&Nu;&oacute;&Oacute;&ocirc;"},
                new object[] {@"ÔœŒòÒ", @"&Ocirc;&oelig;&OElig;&ograve;&Ograve;"},
                new object[] {@"‾ωΩοΟ", @"&oline;&omega;&Omega;&omicron;&Omicron;"},
                new object[] {@"⊕∨ªºø", @"&oplus;&or;&ordf;&ordm;&oslash;"},
                new object[] {@"ØõÕ⊗ö", @"&Oslash;&otilde;&Otilde;&otimes;&ouml;"},
                new object[] {@"Ö¶∂‰⊥", @"&Ouml;&para;&part;&permil;&perp;"},
                new object[] {@"φΦπΠϖ", @"&phi;&Phi;&pi;&Pi;&piv;"},
                new object[] {@"±£′″∏", @"&plusmn;&pound;&prime;&Prime;&prod;"},
                new object[] {@"∝ψΨ""√", @"&prop;&psi;&Psi;&quot;&radic;"},
                new object[] {@"〉»→⇒⌉", @"&rang;&raquo;&rarr;&rArr;&rceil;"},
                new object[] {@"”ℜ®⌋ρ", @"&rdquo;&real;&reg;&rfloor;&rho;"},
                new object[] {"Ρ\u200F›’‚", @"&Rho;&rlm;&rsaquo;&rsquo;&sbquo;"},
                new object[] {"šŠ⋅§\u00AD", @"&scaron;&Scaron;&sdot;&sect;&shy;"},
                new object[] {@"σΣς∼♠", @"&sigma;&Sigma;&sigmaf;&sim;&spades;"},
                new object[] {@"⊂⊆∑⊃¹", @"&sub;&sube;&sum;&sup;&sup1;"},
                new object[] {@"²³⊇ßτ", @"&sup2;&sup3;&supe;&szlig;&tau;"},
                new object[] {@"Τ∴θΘϑ", @"&Tau;&there4;&theta;&Theta;&thetasym;"},
                new object[] {@" þÞ˜×", @"&thinsp;&thorn;&THORN;&tilde;&times;"},
                new object[] {@"™úÚ↑⇑", @"&trade;&uacute;&Uacute;&uarr;&uArr;"},
                new object[] {@"ûÛùÙ¨", @"&ucirc;&Ucirc;&ugrave;&Ugrave;&uml;"},
                new object[] {@"ϒυΥüÜ", @"&upsih;&upsilon;&Upsilon;&uuml;&Uuml;"},
                new object[] {@"℘ξΞýÝ", @"&weierp;&xi;&Xi;&yacute;&Yacute;"},
                new object[] {@"¥ÿŸζΖ", @"&yen;&yuml;&Yuml;&zeta;&Zeta;"},
                new object[] {"\u200D\u200C", @"&zwj;&zwnj;"},
                new object[]
                {
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                },
                new object[]
                {
                    "\u00A0¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ",
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
                    "\u00A0¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ",
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
                    "áÁâÂ´æÆàÀℵαΑ&∧∠åÅ≈ãÃäÄ„βΒ¦•∩çÇ¸¢χΧˆ♣≅©↵∪¤†‡↓⇓°δΔ♦÷éÉêÊèÈ∅  εΕ≡ηΗðÐëË€∃ƒ∀½¼¾⁄γΓ≥>↔⇔♥…íÍîÎ¡ìÌℑ∞∫ιΙ¿∈ïÏκΚλΛ〈«←⇐⌈“≤⌊∗◊‎‹‘<¯—µ·−μΜ∇\u00A0–≠∋¬∉⊄ñÑνΝóÓôÔœŒòÒ‾ωΩοΟ⊕∨ªºøØõÕ⊗öÖ¶∂‰⊥φΦπΠϖ±£′″∏∝ψΨ\"√〉»→⇒⌉”ℜ®⌋ρΡ‏›’‚šŠ⋅§­σΣς∼♠⊂⊆∑⊃¹²³⊇ßτΤ∴θΘϑ þÞ˜×™úÚ↑⇑ûÛùÙ¨ϒυΥüÜ℘ξΞýÝ¥ÿŸζΖ‍‌",
                    @"&#225;&#193;&#226;&#194;&#180;&#230;&#198;&#224;&#192;ℵαΑ&amp;∧∠&#229;&#197;≈&#227;&#195;&#228;&#196;„βΒ&#166;•∩&#231;&#199;&#184;&#162;χΧˆ♣≅&#169;↵∪&#164;†‡↓⇓&#176;δΔ♦&#247;&#233;&#201;&#234;&#202;&#232;&#200;∅  εΕ≡ηΗ&#240;&#208;&#235;&#203;€∃ƒ∀&#189;&#188;&#190;⁄γΓ≥&gt;↔⇔♥…&#237;&#205;&#238;&#206;&#161;&#236;&#204;ℑ∞∫ιΙ&#191;∈&#239;&#207;κΚλΛ〈&#171;←⇐⌈“≤⌊∗◊‎‹‘&lt;&#175;—&#181;&#183;−μΜ∇&#160;–≠∋&#172;∉⊄&#241;&#209;νΝ&#243;&#211;&#244;&#212;œŒ&#242;&#210;‾ωΩοΟ⊕∨&#170;&#186;&#248;&#216;&#245;&#213;⊗&#246;&#214;&#182;∂‰⊥φΦπΠϖ&#177;&#163;′″∏∝ψΨ&quot;√〉&#187;→⇒⌉”ℜ&#174;⌋ρΡ‏›’‚šŠ⋅&#167;&#173;σΣς∼♠⊂⊆∑⊃&#185;&#178;&#179;⊇&#223;τΤ∴θΘϑ &#254;&#222;˜&#215;™&#250;&#218;↑⇑&#251;&#219;&#249;&#217;&#168;ϒυΥ&#252;&#220;℘ξΞ&#253;&#221;&#165;&#255;ŸζΖ‍‌",
                },
                new object[]
                {
                    "á9cP!qdO#hU@mg1ÁK%0<}*âÂ5[Y;lfMQ$4`´uim7E`%_1zVDkæ[cM{Æt9y:E8Hb;;$;Y'àUa6wÀ<$@W9$4NL*h#'ℵk\\zαG}{}hC-Α|=QhyLT%`&wB!@#x51R 4C∧]Z3n∠y>:{JZ'v|c0;N\"åzcWM'z\"gÅo-JX!r.e≈Z+BT{wF8+ãQ 6P1o?x\"ef}vUÃ+</Nt)TI]sä0Eg_'mn&6WY[8Äay+ u[3kqoZ„i6rβUX\\:_y1A^x.p>+Β`uf3/HI¦7bCRv%o$X3:•∩ç|(fgiA|MBLf=y@Ç¸¢R,qDW;F9<mχU]$)Q`w^KF^(hΧ?ukX+O!UOftˆZE♣@MLR(vcH]k8≅CU;r#(©7DZ`1>r~.↵4B&R∪+x2T`q[M-lq'¤~3rp%~-Gd†;35wU+II1tQJ‡`NGh[↓Lr>74~yHB=&EI⇓,u@Jx°δcC`2,Δo2B]6PP8♦|{!wZa&,*N'$6÷-{nVSgO]%(Ié6Éêosx-2xDI!Ê_]7Ub%èYG4`Gx{ÈH>vwMPJ∅ :Z-u#ph l,s*8(AεΕOnj|Gy|]iYLPR≡5Wi:(vZUUK.YlηDΗ6TðT!Z:Nq_0797;!Ð4]QNë9+>x9>nm-s8YËwZ}vY€:HHf∃;=0,?ƒIr`I:i5'∀z_$Q<½_sCF;=$43DpDz]¼.aMTIEwx\\ogn7A¾CuJD[Hke#⁄E]M%γE:IEk}Γ{qXfzeUS≥kqW yxV>↔AzJ:$fJ⇔3IMDqU\\myWjsL♥…Okíjt$NKbGrÍ\"+alp<îRÎ%¡yìz2 AÌ-%;jyMK{Umdℑi|}+Za8jyWDS#I∞]NyqN*v:m-∫03Aιf9m.:+z0@OfVoΙ_gfPilLZ¿6qqb0|BQ$H%p+d∈.Wa=YBfS'd-EOïISG+=W;GHÏ3||b-icT\"qAκ*/ΚλN>j}\"WrqΛt]dm-Xe/v〈\\«$F< X←]=8H8⇐c⌈|“JgZ)+(7,}≤s8[\"3%C4JvN⌊H55TAKEZ*%Z)d.∗R9z//!q◊D`643eO‎&-L>DsUej‹C[n]Q<%UoyO‘?zUgpr+62sY<T{7n*^¯4CH]6^e/x/—uT->mQh\\\"µZSTN!F(U%5·17:Cu<−)*c2μTΜ%:6-e&L[ Xos/4∇]Xr\u00A01c=qyv4HSw~HL~–{+qG?/}≠6`S\",+pL∋>¬B9∉G;6P]xc 0Bs⊄7,j0Sj2/&ñFsÑ=νKs*?[54bV1ΝQ%p6P0.Lrc`yóA/*`6sBH?67Ó&ôÔI\"Hœ~e9Œ>oò5eZI}iy?}KÒS‾anD1nXωIΩu\"ο:Mz$(\"joU^[mΟ7M1f$j>N|Q/@(⊕de6(∨WXb<~;tI?bt#ªU:º+wb(*cA=øjb c%*?Uj6<T02Ø/A}j'MõjlfYlR~er7D@3WÕe:XTLF?|\"yd7x⊗eV6Mmw2{K<lö%B%/o~r9Öc1Q TJnd^¶;∂|‰_.⊥E_bim;gvA{wqφeΦ^-!Dcπ8LB6k4PΠ(5D |Y3ϖptuh)3Mv±TAvFo+;JE,2?£\"'6F9fRp′,0″<∏N∝C%}JC7qY(7))UWψ 7=rmQaΨeD!G5e>S~kO\"'4\"/i4\\>!]H;T^0o√8_G`*8&An\\rhc)〉&UEk»-(YtC→(zerUTMTe,'@{⇒mlzVhU<S,5}9DM⌉/%R=10*[{'=:”C0ℜ4HoT?-#+l[SnPs®0 bV⌋TρΡjb1}OJ:,0z6‏oTxP\"\"FOT[;›'’-:Ll)I0^$p.‚S_šNBr9)K[Š1⋅$-S4/G&u§= _CqlY1O'­qNf|&σGp}ΣP3:8ς∼[ItI♠8⊂BQn~!KO:+~ma⊆FV.u 4wD∑lE+kQ|gZ];Y⊃DK69EEM$D¹KVO²%:~Iq?IUcHr4y³QP@R't!⊇vßYnI@FXxT<τvL[4H95mfΤF0JzQsrxNZry∴Bn#t(θ*OΘw=Z%ϑ+*l^3C)5HCNmR  %`g|*8DECþ_[Þ'8,?˜}gnaz_U×-F^™9ZDO86ú]y\\ecHQSÚk-07/AT|0Ce↑F⇑*}e|r$6ln!V`ûA!*8H,mÛ~6G6w&GùsPL6ÙQ¨}J^NO}=._Mnϒ{&υ=ΥWD+f>fy|nNyP*Jüo8,lh\\ÜN`'g℘(sJ8h3P]cF ξcdQ_OC]U#ΞBby=Sý9tI_Ý}p(D51=X¥cH8L)$*]~=IÿdbŸf>J^1Dnζ@(drH;91?{6`xJΖ4N4[u+5‍9.W\\v‌]GGtKvCC0`A",
                    @"&#225;9cP!qdO#hU@mg1&#193;K%0&lt;}*&#226;&#194;5[Y;lfMQ$4`&#180;uim7E`%_1zVDk&#230;[cM{&#198;t9y:E8Hb;;$;Y&#39;&#224;Ua6w&#192;&lt;$@W9$4NL*h#&#39;ℵk\zαG}{}hC-Α|=QhyLT%`&amp;wB!@#x51R 4C∧]Z3n∠y&gt;:{JZ&#39;v|c0;N&quot;&#229;zcWM&#39;z&quot;g&#197;o-JX!r.e≈Z+BT{wF8+&#227;Q 6P1o?x&quot;ef}vU&#195;+&lt;/Nt)TI]s&#228;0Eg_&#39;mn&amp;6WY[8&#196;ay+ u[3kqoZ„i6rβUX\:_y1A^x.p&gt;+Β`uf3/HI&#166;7bCRv%o$X3:•∩&#231;|(fgiA|MBLf=y@&#199;&#184;&#162;R,qDW;F9&lt;mχU]$)Q`w^KF^(hΧ?ukX+O!UOftˆZE♣@MLR(vcH]k8≅CU;r#(&#169;7DZ`1&gt;r~.↵4B&amp;R∪+x2T`q[M-lq&#39;&#164;~3rp%~-Gd†;35wU+II1tQJ‡`NGh[↓Lr&gt;74~yHB=&amp;EI⇓,u@Jx&#176;δcC`2,Δo2B]6PP8♦|{!wZa&amp;,*N&#39;$6&#247;-{nVSgO]%(I&#233;6&#201;&#234;osx-2xDI!&#202;_]7Ub%&#232;YG4`Gx{&#200;H&gt;vwMPJ∅ :Z-u#ph l,s*8(AεΕOnj|Gy|]iYLPR≡5Wi:(vZUUK.YlηDΗ6T&#240;T!Z:Nq_0797;!&#208;4]QN&#235;9+&gt;x9&gt;nm-s8Y&#203;wZ}vY€:HHf∃;=0,?ƒIr`I:i5&#39;∀z_$Q&lt;&#189;_sCF;=$43DpDz]&#188;.aMTIEwx\ogn7A&#190;CuJD[Hke#⁄E]M%γE:IEk}Γ{qXfzeUS≥kqW yxV&gt;↔AzJ:$fJ⇔3IMDqU\myWjsL♥…Ok&#237;jt$NKbGr&#205;&quot;+alp&lt;&#238;R&#206;%&#161;y&#236;z2 A&#204;-%;jyMK{Umdℑi|}+Za8jyWDS#I∞]NyqN*v:m-∫03Aιf9m.:+z0@OfVoΙ_gfPilLZ&#191;6qqb0|BQ$H%p+d∈.Wa=YBfS&#39;d-EO&#239;ISG+=W;GH&#207;3||b-icT&quot;qAκ*/ΚλN&gt;j}&quot;WrqΛt]dm-Xe/v〈\&#171;$F&lt; X←]=8H8⇐c⌈|“JgZ)+(7,}≤s8[&quot;3%C4JvN⌊H55TAKEZ*%Z)d.∗R9z//!q◊D`643eO‎&amp;-L&gt;DsUej‹C[n]Q&lt;%UoyO‘?zUgpr+62sY&lt;T{7n*^&#175;4CH]6^e/x/—uT-&gt;mQh\&quot;&#181;ZSTN!F(U%5&#183;17:Cu&lt;−)*c2μTΜ%:6-e&amp;L[ Xos/4∇]Xr&#160;1c=qyv4HSw~HL~–{+qG?/}≠6`S&quot;,+pL∋&gt;&#172;B9∉G;6P]xc 0Bs⊄7,j0Sj2/&amp;&#241;Fs&#209;=νKs*?[54bV1ΝQ%p6P0.Lrc`y&#243;A/*`6sBH?67&#211;&amp;&#244;&#212;I&quot;Hœ~e9Œ&gt;o&#242;5eZI}iy?}K&#210;S‾anD1nXωIΩu&quot;ο:Mz$(&quot;joU^[mΟ7M1f$j&gt;N|Q/@(⊕de6(∨WXb&lt;~;tI?bt#&#170;U:&#186;+wb(*cA=&#248;jb c%*?Uj6&lt;T02&#216;/A}j&#39;M&#245;jlfYlR~er7D@3W&#213;e:XTLF?|&quot;yd7x⊗eV6Mmw2{K&lt;l&#246;%B%/o~r9&#214;c1Q TJnd^&#182;;∂|‰_.⊥E_bim;gvA{wqφeΦ^-!Dcπ8LB6k4PΠ(5D |Y3ϖptuh)3Mv&#177;TAvFo+;JE,2?&#163;&quot;&#39;6F9fRp′,0″&lt;∏N∝C%}JC7qY(7))UWψ 7=rmQaΨeD!G5e&gt;S~kO&quot;&#39;4&quot;/i4\&gt;!]H;T^0o√8_G`*8&amp;An\rhc)〉&amp;UEk&#187;-(YtC→(zerUTMTe,&#39;@{⇒mlzVhU&lt;S,5}9DM⌉/%R=10*[{&#39;=:”C0ℜ4HoT?-#+l[SnPs&#174;0 bV⌋TρΡjb1}OJ:,0z6‏oTxP&quot;&quot;FOT[;›&#39;’-:Ll)I0^$p.‚S_šNBr9)K[Š1⋅$-S4/G&amp;u&#167;= _CqlY1O&#39;&#173;qNf|&amp;σGp}ΣP3:8ς∼[ItI♠8⊂BQn~!KO:+~ma⊆FV.u 4wD∑lE+kQ|gZ];Y⊃DK69EEM$D&#185;KVO&#178;%:~Iq?IUcHr4y&#179;QP@R&#39;t!⊇v&#223;YnI@FXxT&lt;τvL[4H95mfΤF0JzQsrxNZry∴Bn#t(θ*OΘw=Z%ϑ+*l^3C)5HCNmR  %`g|*8DEC&#254;_[&#222;&#39;8,?˜}gnaz_U&#215;-F^™9ZDO86&#250;]y\ecHQS&#218;k-07/AT|0Ce↑F⇑*}e|r$6ln!V`&#251;A!*8H,m&#219;~6G6w&amp;G&#249;sPL6&#217;Q&#168;}J^NO}=._Mnϒ{&amp;υ=ΥWD+f&gt;fy|nNyP*J&#252;o8,lh\&#220;N`&#39;g℘(sJ8h3P]cF ξcdQ_OC]U#ΞBby=S&#253;9tI_&#221;}p(D51=X&#165;cH8L)$*]~=I&#255;dbŸf&gt;J^1Dnζ@(drH;91?{6`xJΖ4N4[u+5‍9.W\v‌]GGtKvCC0`A",
                },
                new object[]
                {
                    @"&aacute&Aacute&acirc&Acirc&acute&aelig&AElig&agrave&Agrave&alefsym&alpha&Alpha&amp&and&ang&aring&Aring&asymp&atilde&Atilde&auml&Auml&bdquo&beta&Beta&brvbar&bull&cap&ccedil&Ccedil&cedil&cent&chi&Chi&circ&clubs&cong&copy&crarr&cup&curren&dagger&Dagger&darr&dArr&deg&delta&Delta&diams&divide&eacute&Eacute&ecirc&Ecirc&egrave&Egrave&empty&emsp&ensp&epsilon&Epsilon&equiv&eta&Eta&eth&ETH&euml&Euml&euro&exist&fnof&forall&frac12&frac14&frac34&frasl&gamma&Gamma&ge&gt&harr&hArr&hearts&hellip&iacute&Iacute&icirc&Icirc&iexcl&igrave&Igrave&image&infin&int&iota&Iota&iquest&isin&iuml&Iuml&kappa&Kappa&lambda&Lambda&lang&laquo&larr&lArr&lceil&ldquo&le&lfloor&lowast&loz&lrm&lsaquo&lsquo&lt&macr&mdash&micro&middot&minus&mu&Mu&nabla&nbsp&ndash&ne&ni&not&notin&nsub&ntilde&Ntilde&nu&Nu&oacute&Oacute&ocirc&Ocirc&oelig&OElig&ograve&Ograve&oline&omega&Omega&omicron&Omicron&oplus&or&ordf&ordm&oslash&Oslash&otilde&Otilde&otimes&ouml&Ouml&para&part&permil&perp&phi&Phi&pi&Pi&piv&plusmn&pound&prime&Prime&prod&prop&psi&Psi&quot&radic&rang&raquo&rarr&rArr&rceil&rdquo&real&reg&rfloor&rho&Rho&rlm&rsaquo&rsquo&sbquo&scaron&Scaron&sdot&sect&shy&sigma&Sigma&sigmaf&sim&spades&sub&sube&sum&sup&sup1&sup2&sup3&supe&szlig&tau&Tau&there4&theta&Theta&thetasym&thinsp&thorn&THORN&tilde&times&trade&uacute&Uacute&uarr&uArr&ucirc&Ucirc&ugrave&Ugrave&uml&upsih&upsilon&Upsilon&uuml&Uuml&weierp&xi&Xi&yacute&Yacute&yen&yuml&Yuml&zeta&Zeta&zwj&zwnj",
                    @"&amp;aacute&amp;Aacute&amp;acirc&amp;Acirc&amp;acute&amp;aelig&amp;AElig&amp;agrave&amp;Agrave&amp;alefsym&amp;alpha&amp;Alpha&amp;amp&amp;and&amp;ang&amp;aring&amp;Aring&amp;asymp&amp;atilde&amp;Atilde&amp;auml&amp;Auml&amp;bdquo&amp;beta&amp;Beta&amp;brvbar&amp;bull&amp;cap&amp;ccedil&amp;Ccedil&amp;cedil&amp;cent&amp;chi&amp;Chi&amp;circ&amp;clubs&amp;cong&amp;copy&amp;crarr&amp;cup&amp;curren&amp;dagger&amp;Dagger&amp;darr&amp;dArr&amp;deg&amp;delta&amp;Delta&amp;diams&amp;divide&amp;eacute&amp;Eacute&amp;ecirc&amp;Ecirc&amp;egrave&amp;Egrave&amp;empty&amp;emsp&amp;ensp&amp;epsilon&amp;Epsilon&amp;equiv&amp;eta&amp;Eta&amp;eth&amp;ETH&amp;euml&amp;Euml&amp;euro&amp;exist&amp;fnof&amp;forall&amp;frac12&amp;frac14&amp;frac34&amp;frasl&amp;gamma&amp;Gamma&amp;ge&amp;gt&amp;harr&amp;hArr&amp;hearts&amp;hellip&amp;iacute&amp;Iacute&amp;icirc&amp;Icirc&amp;iexcl&amp;igrave&amp;Igrave&amp;image&amp;infin&amp;int&amp;iota&amp;Iota&amp;iquest&amp;isin&amp;iuml&amp;Iuml&amp;kappa&amp;Kappa&amp;lambda&amp;Lambda&amp;lang&amp;laquo&amp;larr&amp;lArr&amp;lceil&amp;ldquo&amp;le&amp;lfloor&amp;lowast&amp;loz&amp;lrm&amp;lsaquo&amp;lsquo&amp;lt&amp;macr&amp;mdash&amp;micro&amp;middot&amp;minus&amp;mu&amp;Mu&amp;nabla&amp;nbsp&amp;ndash&amp;ne&amp;ni&amp;not&amp;notin&amp;nsub&amp;ntilde&amp;Ntilde&amp;nu&amp;Nu&amp;oacute&amp;Oacute&amp;ocirc&amp;Ocirc&amp;oelig&amp;OElig&amp;ograve&amp;Ograve&amp;oline&amp;omega&amp;Omega&amp;omicron&amp;Omicron&amp;oplus&amp;or&amp;ordf&amp;ordm&amp;oslash&amp;Oslash&amp;otilde&amp;Otilde&amp;otimes&amp;ouml&amp;Ouml&amp;para&amp;part&amp;permil&amp;perp&amp;phi&amp;Phi&amp;pi&amp;Pi&amp;piv&amp;plusmn&amp;pound&amp;prime&amp;Prime&amp;prod&amp;prop&amp;psi&amp;Psi&amp;quot&amp;radic&amp;rang&amp;raquo&amp;rarr&amp;rArr&amp;rceil&amp;rdquo&amp;real&amp;reg&amp;rfloor&amp;rho&amp;Rho&amp;rlm&amp;rsaquo&amp;rsquo&amp;sbquo&amp;scaron&amp;Scaron&amp;sdot&amp;sect&amp;shy&amp;sigma&amp;Sigma&amp;sigmaf&amp;sim&amp;spades&amp;sub&amp;sube&amp;sum&amp;sup&amp;sup1&amp;sup2&amp;sup3&amp;supe&amp;szlig&amp;tau&amp;Tau&amp;there4&amp;theta&amp;Theta&amp;thetasym&amp;thinsp&amp;thorn&amp;THORN&amp;tilde&amp;times&amp;trade&amp;uacute&amp;Uacute&amp;uarr&amp;uArr&amp;ucirc&amp;Ucirc&amp;ugrave&amp;Ugrave&amp;uml&amp;upsih&amp;upsilon&amp;Upsilon&amp;uuml&amp;Uuml&amp;weierp&amp;xi&amp;Xi&amp;yacute&amp;Yacute&amp;yen&amp;yuml&amp;Yuml&amp;zeta&amp;Zeta&amp;zwj&amp;zwnj",
                },
                new object[]
                {
                    "\u00A0¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ",
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
                    "\u00A0¡¢£¤¥¦§¨©ª«¬­®¯°±²³´µ¶·¸¹º»¼½¾¿ÀÁÂÃÄÅÆÇÈÉÊËÌÍÎÏÐÑÒÓÔÕÖ×ØÙÚÛÜÝÞßàáâãäåæçèéêëìíîïðñòóôõö÷øùúûüýþÿ",
                    @"&#160;&#161;&#162;&#163;&#164;&#165;&#166;&#167;&#168;&#169;&#170;&#171;&#172;&#173;&#174;&#175;&#176;&#177;&#178;&#179;&#180;&#181;&#182;&#183;&#184;&#185;&#186;&#187;&#188;&#189;&#190;&#191;&#192;&#193;&#194;&#195;&#196;&#197;&#198;&#199;&#200;&#201;&#202;&#203;&#204;&#205;&#206;&#207;&#208;&#209;&#210;&#211;&#212;&#213;&#214;&#215;&#216;&#217;&#218;&#219;&#220;&#221;&#222;&#223;&#224;&#225;&#226;&#227;&#228;&#229;&#230;&#231;&#232;&#233;&#234;&#235;&#236;&#237;&#238;&#239;&#240;&#241;&#242;&#243;&#244;&#245;&#246;&#247;&#248;&#249;&#250;&#251;&#252;&#253;&#254;&#255;",
                }
            };

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlEncodeData))]
        [InlineData(null, null)]
        public void HtmlEncode(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.HtmlEncode(decoded));
        }

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlEncodeData))]
        [InlineData(null, null)]
        [InlineData(2, "2")]
        public void HtmlEncodeObject(string decoded, string encoded)
        {
            Assert.Equal(encoded, HttpUtility.HtmlEncode((object)decoded));
        }

        [Theory]
        [MemberData(nameof(HtmlEncodeDecodeData))]
        [MemberData(nameof(HtmlEncodeData))]
        [InlineData(null, "")]
        public void HtmlEncode_TextWriter(string decoded, string encoded)
        {
            var sw = new StringWriter();
            HttpUtility.HtmlEncode(decoded, sw);
            Assert.Equal(encoded, sw.ToString());
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
                new object[] { "http://example.net/\uFFFDa", "http://example.net/\uDC00a" }
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
            if ((c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9'))
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
        public void UrlEncodeToBytesExplicitSize(string decoded, string encoded, int offset, int count)
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
        [InlineData("http://EXAMPLE.NET/défault.xxx?sdsd=sds", "http://EXAMPLE.NET/d%c3%a9fault.xxx?sdsd=sds")]
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
