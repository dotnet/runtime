// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Text
{
    internal static partial class EncodingTable
    {

        // s_encodingNames is the concatenation of all supported IANA names for each codepage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        // Using indices from s_encodingNamesIndices, we binary search this string when mapping
        // an encoding name to a codepage. Note that these names are all lowercase and are
        // sorted alphabetically.
        private const string s_encodingNames =
            "437" + // 437
            "arabic" + // 28596
            "asmo-708" + // 708
            "big5" + // 950
            "big5-hkscs" + // 950
            "ccsid00858" + // 858
            "ccsid00924" + // 20924
            "ccsid01140" + // 1140
            "ccsid01141" + // 1141
            "ccsid01142" + // 1142
            "ccsid01143" + // 1143
            "ccsid01144" + // 1144
            "ccsid01145" + // 1145
            "ccsid01146" + // 1146
            "ccsid01147" + // 1147
            "ccsid01148" + // 1148
            "ccsid01149" + // 1149
            "chinese" + // 936
            "cn-big5" + // 950
            "cn-gb" + // 936
            "cp00858" + // 858
            "cp00924" + // 20924
            "cp01140" + // 1140
            "cp01141" + // 1141
            "cp01142" + // 1142
            "cp01143" + // 1143
            "cp01144" + // 1144
            "cp01145" + // 1145
            "cp01146" + // 1146
            "cp01147" + // 1147
            "cp01148" + // 1148
            "cp01149" + // 1149
            "cp037" + // 37
            "cp1025" + // 21025
            "cp1026" + // 1026
            "cp1256" + // 1256
            "cp273" + // 20273
            "cp278" + // 20278
            "cp280" + // 20280
            "cp284" + // 20284
            "cp285" + // 20285
            "cp290" + // 20290
            "cp297" + // 20297
            "cp420" + // 20420
            "cp423" + // 20423
            "cp424" + // 20424
            "cp437" + // 437
            "cp500" + // 500
            "cp50227" + // 50227
            "cp850" + // 850
            "cp852" + // 852
            "cp855" + // 855
            "cp857" + // 857
            "cp858" + // 858
            "cp860" + // 860
            "cp861" + // 861
            "cp862" + // 862
            "cp863" + // 863
            "cp864" + // 864
            "cp865" + // 865
            "cp866" + // 866
            "cp869" + // 869
            "cp870" + // 870
            "cp871" + // 20871
            "cp875" + // 875
            "cp880" + // 20880
            "cp905" + // 20905
            "csbig5" + // 950
            "cseuckr" + // 51949
            "cseucpkdfmtjapanese" + // 51932
            "csgb2312" + // 936
            "csgb231280" + // 936
            "csibm037" + // 37
            "csibm1026" + // 1026
            "csibm273" + // 20273
            "csibm277" + // 20277
            "csibm278" + // 20278
            "csibm280" + // 20280
            "csibm284" + // 20284
            "csibm285" + // 20285
            "csibm290" + // 20290
            "csibm297" + // 20297
            "csibm420" + // 20420
            "csibm423" + // 20423
            "csibm424" + // 20424
            "csibm500" + // 500
            "csibm870" + // 870
            "csibm871" + // 20871
            "csibm880" + // 20880
            "csibm905" + // 20905
            "csibmthai" + // 20838
            "csiso2022jp" + // 50221
            "csiso2022kr" + // 50225
            "csiso58gb231280" + // 936
            "csisolatin2" + // 28592
            "csisolatin3" + // 28593
            "csisolatin4" + // 28594
            "csisolatin5" + // 28599
            "csisolatin9" + // 28605
            "csisolatinarabic" + // 28596
            "csisolatincyrillic" + // 28595
            "csisolatingreek" + // 28597
            "csisolatinhebrew" + // 28598
            "cskoi8r" + // 20866
            "csksc56011987" + // 949
            "cspc8codepage437" + // 437
            "csshiftjis" + // 932
            "cswindows31j" + // 932
            "cyrillic" + // 28595
            "din_66003" + // 20106
            "dos-720" + // 720
            "dos-862" + // 862
            "dos-874" + // 874
            "ebcdic-cp-ar1" + // 20420
            "ebcdic-cp-be" + // 500
            "ebcdic-cp-ca" + // 37
            "ebcdic-cp-ch" + // 500
            "ebcdic-cp-dk" + // 20277
            "ebcdic-cp-es" + // 20284
            "ebcdic-cp-fi" + // 20278
            "ebcdic-cp-fr" + // 20297
            "ebcdic-cp-gb" + // 20285
            "ebcdic-cp-gr" + // 20423
            "ebcdic-cp-he" + // 20424
            "ebcdic-cp-is" + // 20871
            "ebcdic-cp-it" + // 20280
            "ebcdic-cp-nl" + // 37
            "ebcdic-cp-no" + // 20277
            "ebcdic-cp-roece" + // 870
            "ebcdic-cp-se" + // 20278
            "ebcdic-cp-tr" + // 20905
            "ebcdic-cp-us" + // 37
            "ebcdic-cp-wt" + // 37
            "ebcdic-cp-yu" + // 870
            "ebcdic-cyrillic" + // 20880
            "ebcdic-de-273+euro" + // 1141
            "ebcdic-dk-277+euro" + // 1142
            "ebcdic-es-284+euro" + // 1145
            "ebcdic-fi-278+euro" + // 1143
            "ebcdic-fr-297+euro" + // 1147
            "ebcdic-gb-285+euro" + // 1146
            "ebcdic-international-500+euro" + // 1148
            "ebcdic-is-871+euro" + // 1149
            "ebcdic-it-280+euro" + // 1144
            "ebcdic-jp-kana" + // 20290
            "ebcdic-latin9--euro" + // 20924
            "ebcdic-no-277+euro" + // 1142
            "ebcdic-se-278+euro" + // 1143
            "ebcdic-us-37+euro" + // 1140
            "ecma-114" + // 28596
            "ecma-118" + // 28597
            "elot_928" + // 28597
            "euc-cn" + // 51936
            "euc-jp" + // 51932
            "euc-kr" + // 51949
            "extended_unix_code_packed_format_for_japanese" + // 51932
            "gb18030" + // 54936
            "gb2312" + // 936
            "gb2312-80" + // 936
            "gb231280" + // 936
            "gb_2312-80" + // 936
            "gbk" + // 936
            "german" + // 20106
            "greek" + // 28597
            "greek8" + // 28597
            "hebrew" + // 28598
            "hz-gb-2312" + // 52936
            "ibm-thai" + // 20838
            "ibm00858" + // 858
            "ibm00924" + // 20924
            "ibm01047" + // 1047
            "ibm01140" + // 1140
            "ibm01141" + // 1141
            "ibm01142" + // 1142
            "ibm01143" + // 1143
            "ibm01144" + // 1144
            "ibm01145" + // 1145
            "ibm01146" + // 1146
            "ibm01147" + // 1147
            "ibm01148" + // 1148
            "ibm01149" + // 1149
            "ibm037" + // 37
            "ibm1026" + // 1026
            "ibm273" + // 20273
            "ibm277" + // 20277
            "ibm278" + // 20278
            "ibm280" + // 20280
            "ibm284" + // 20284
            "ibm285" + // 20285
            "ibm290" + // 20290
            "ibm297" + // 20297
            "ibm420" + // 20420
            "ibm423" + // 20423
            "ibm424" + // 20424
            "ibm437" + // 437
            "ibm500" + // 500
            "ibm737" + // 737
            "ibm775" + // 775
            "ibm850" + // 850
            "ibm852" + // 852
            "ibm855" + // 855
            "ibm857" + // 857
            "ibm860" + // 860
            "ibm861" + // 861
            "ibm862" + // 862
            "ibm863" + // 863
            "ibm864" + // 864
            "ibm865" + // 865
            "ibm866" + // 866
            "ibm869" + // 869
            "ibm870" + // 870
            "ibm871" + // 20871
            "ibm880" + // 20880
            "ibm905" + // 20905
            "irv" + // 20105
            "iso-2022-jp" + // 50220
            "iso-2022-jpeuc" + // 51932
            "iso-2022-kr" + // 50225
            "iso-2022-kr-7" + // 50225
            "iso-2022-kr-7bit" + // 50225
            "iso-2022-kr-8" + // 51949
            "iso-2022-kr-8bit" + // 51949
            "iso-8859-11" + // 874
            "iso-8859-13" + // 28603
            "iso-8859-15" + // 28605
            "iso-8859-2" + // 28592
            "iso-8859-3" + // 28593
            "iso-8859-4" + // 28594
            "iso-8859-5" + // 28595
            "iso-8859-6" + // 28596
            "iso-8859-7" + // 28597
            "iso-8859-8" + // 28598
            "iso-8859-8 visual" + // 28598
            "iso-8859-8-i" + // 38598
            "iso-8859-9" + // 28599
            "iso-ir-101" + // 28592
            "iso-ir-109" + // 28593
            "iso-ir-110" + // 28594
            "iso-ir-126" + // 28597
            "iso-ir-127" + // 28596
            "iso-ir-138" + // 28598
            "iso-ir-144" + // 28595
            "iso-ir-148" + // 28599
            "iso-ir-149" + // 949
            "iso-ir-58" + // 936
            "iso8859-2" + // 28592
            "iso_8859-15" + // 28605
            "iso_8859-2" + // 28592
            "iso_8859-2:1987" + // 28592
            "iso_8859-3" + // 28593
            "iso_8859-3:1988" + // 28593
            "iso_8859-4" + // 28594
            "iso_8859-4:1988" + // 28594
            "iso_8859-5" + // 28595
            "iso_8859-5:1988" + // 28595
            "iso_8859-6" + // 28596
            "iso_8859-6:1987" + // 28596
            "iso_8859-7" + // 28597
            "iso_8859-7:1987" + // 28597
            "iso_8859-8" + // 28598
            "iso_8859-8:1988" + // 28598
            "iso_8859-9" + // 28599
            "iso_8859-9:1989" + // 28599
            "johab" + // 1361
            "koi" + // 20866
            "koi8" + // 20866
            "koi8-r" + // 20866
            "koi8-ru" + // 21866
            "koi8-u" + // 21866
            "koi8r" + // 20866
            "korean" + // 949
            "ks-c-5601" + // 949
            "ks-c5601" + // 949
            "ks_c_5601" + // 949
            "ks_c_5601-1987" + // 949
            "ks_c_5601-1989" + // 949
            "ks_c_5601_1987" + // 949
            "ksc5601" + // 949
            "ksc_5601" + // 949
            "l2" + // 28592
            "l3" + // 28593
            "l4" + // 28594
            "l5" + // 28599
            "l9" + // 28605
            "latin2" + // 28592
            "latin3" + // 28593
            "latin4" + // 28594
            "latin5" + // 28599
            "latin9" + // 28605
            "logical" + // 28598
            "macintosh" + // 10000
            "ms_kanji" + // 932
            "norwegian" + // 20108
            "ns_4551-1" + // 20108
            "pc-multilingual-850+euro" + // 858
            "sen_850200_b" + // 20107
            "shift-jis" + // 932
            "shift_jis" + // 932
            "sjis" + // 932
            "swedish" + // 20107
            "tis-620" + // 874
            "visual" + // 28598
            "windows-1250" + // 1250
            "windows-1251" + // 1251
            "windows-1252" + // 1252
            "windows-1253" + // 1253
            "windows-1254" + // 1254
            "windows-1255" + // 1255
            "windows-1256" + // 1256
            "windows-1257" + // 1257
            "windows-1258" + // 1258
            "windows-874" + // 874
            "x-ansi" + // 1252
            "x-chinese-cns" + // 20000
            "x-chinese-eten" + // 20002
            "x-cp1250" + // 1250
            "x-cp1251" + // 1251
            "x-cp20001" + // 20001
            "x-cp20003" + // 20003
            "x-cp20004" + // 20004
            "x-cp20005" + // 20005
            "x-cp20261" + // 20261
            "x-cp20269" + // 20269
            "x-cp20936" + // 20936
            "x-cp20949" + // 20949
            "x-cp50227" + // 50227
            "x-ebcdic-koreanextended" + // 20833
            "x-euc" + // 51932
            "x-euc-cn" + // 51936
            "x-euc-jp" + // 51932
            "x-europa" + // 29001
            "x-ia5" + // 20105
            "x-ia5-german" + // 20106
            "x-ia5-norwegian" + // 20108
            "x-ia5-swedish" + // 20107
            "x-iscii-as" + // 57006
            "x-iscii-be" + // 57003
            "x-iscii-de" + // 57002
            "x-iscii-gu" + // 57010
            "x-iscii-ka" + // 57008
            "x-iscii-ma" + // 57009
            "x-iscii-or" + // 57007
            "x-iscii-pa" + // 57011
            "x-iscii-ta" + // 57004
            "x-iscii-te" + // 57005
            "x-mac-arabic" + // 10004
            "x-mac-ce" + // 10029
            "x-mac-chinesesimp" + // 10008
            "x-mac-chinesetrad" + // 10002
            "x-mac-croatian" + // 10082
            "x-mac-cyrillic" + // 10007
            "x-mac-greek" + // 10006
            "x-mac-hebrew" + // 10005
            "x-mac-icelandic" + // 10079
            "x-mac-japanese" + // 10001
            "x-mac-korean" + // 10003
            "x-mac-romanian" + // 10010
            "x-mac-thai" + // 10021
            "x-mac-turkish" + // 10081
            "x-mac-ukrainian" + // 10017
            "x-ms-cp932" + // 932
            "x-sjis" + // 932
            "x-x-big5" + // 950
            "";

        // s_encodingNameIndices contains the start index of every encoding name in the string
        // s_encodingNames. We infer the length of each string by looking at the start index
        // of the next string.
        private static readonly int[] s_encodingNameIndices = new int[]
        {
            0, // 437 (437)
            3, // arabic (28596)
            9, // asmo-708 (708)
            17, // big5 (950)
            21, // big5-hkscs (950)
            31, // ccsid00858 (858)
            41, // ccsid00924 (20924)
            51, // ccsid01140 (1140)
            61, // ccsid01141 (1141)
            71, // ccsid01142 (1142)
            81, // ccsid01143 (1143)
            91, // ccsid01144 (1144)
            101, // ccsid01145 (1145)
            111, // ccsid01146 (1146)
            121, // ccsid01147 (1147)
            131, // ccsid01148 (1148)
            141, // ccsid01149 (1149)
            151, // chinese (936)
            158, // cn-big5 (950)
            165, // cn-gb (936)
            170, // cp00858 (858)
            177, // cp00924 (20924)
            184, // cp01140 (1140)
            191, // cp01141 (1141)
            198, // cp01142 (1142)
            205, // cp01143 (1143)
            212, // cp01144 (1144)
            219, // cp01145 (1145)
            226, // cp01146 (1146)
            233, // cp01147 (1147)
            240, // cp01148 (1148)
            247, // cp01149 (1149)
            254, // cp037 (37)
            259, // cp1025 (21025)
            265, // cp1026 (1026)
            271, // cp1256 (1256)
            277, // cp273 (20273)
            282, // cp278 (20278)
            287, // cp280 (20280)
            292, // cp284 (20284)
            297, // cp285 (20285)
            302, // cp290 (20290)
            307, // cp297 (20297)
            312, // cp420 (20420)
            317, // cp423 (20423)
            322, // cp424 (20424)
            327, // cp437 (437)
            332, // cp500 (500)
            337, // cp50227 (50227)
            344, // cp850 (850)
            349, // cp852 (852)
            354, // cp855 (855)
            359, // cp857 (857)
            364, // cp858 (858)
            369, // cp860 (860)
            374, // cp861 (861)
            379, // cp862 (862)
            384, // cp863 (863)
            389, // cp864 (864)
            394, // cp865 (865)
            399, // cp866 (866)
            404, // cp869 (869)
            409, // cp870 (870)
            414, // cp871 (20871)
            419, // cp875 (875)
            424, // cp880 (20880)
            429, // cp905 (20905)
            434, // csbig5 (950)
            440, // cseuckr (51949)
            447, // cseucpkdfmtjapanese (51932)
            466, // csgb2312 (936)
            474, // csgb231280 (936)
            484, // csibm037 (37)
            492, // csibm1026 (1026)
            501, // csibm273 (20273)
            509, // csibm277 (20277)
            517, // csibm278 (20278)
            525, // csibm280 (20280)
            533, // csibm284 (20284)
            541, // csibm285 (20285)
            549, // csibm290 (20290)
            557, // csibm297 (20297)
            565, // csibm420 (20420)
            573, // csibm423 (20423)
            581, // csibm424 (20424)
            589, // csibm500 (500)
            597, // csibm870 (870)
            605, // csibm871 (20871)
            613, // csibm880 (20880)
            621, // csibm905 (20905)
            629, // csibmthai (20838)
            638, // csiso2022jp (50221)
            649, // csiso2022kr (50225)
            660, // csiso58gb231280 (936)
            675, // csisolatin2 (28592)
            686, // csisolatin3 (28593)
            697, // csisolatin4 (28594)
            708, // csisolatin5 (28599)
            719, // csisolatin9 (28605)
            730, // csisolatinarabic (28596)
            746, // csisolatincyrillic (28595)
            764, // csisolatingreek (28597)
            779, // csisolatinhebrew (28598)
            795, // cskoi8r (20866)
            802, // csksc56011987 (949)
            815, // cspc8codepage437 (437)
            831, // csshiftjis (932)
            841, // cswindows31j (932)
            853, // cyrillic (28595)
            861, // din_66003 (20106)
            870, // dos-720 (720)
            877, // dos-862 (862)
            884, // dos-874 (874)
            891, // ebcdic-cp-ar1 (20420)
            904, // ebcdic-cp-be (500)
            916, // ebcdic-cp-ca (37)
            928, // ebcdic-cp-ch (500)
            940, // ebcdic-cp-dk (20277)
            952, // ebcdic-cp-es (20284)
            964, // ebcdic-cp-fi (20278)
            976, // ebcdic-cp-fr (20297)
            988, // ebcdic-cp-gb (20285)
            1000, // ebcdic-cp-gr (20423)
            1012, // ebcdic-cp-he (20424)
            1024, // ebcdic-cp-is (20871)
            1036, // ebcdic-cp-it (20280)
            1048, // ebcdic-cp-nl (37)
            1060, // ebcdic-cp-no (20277)
            1072, // ebcdic-cp-roece (870)
            1087, // ebcdic-cp-se (20278)
            1099, // ebcdic-cp-tr (20905)
            1111, // ebcdic-cp-us (37)
            1123, // ebcdic-cp-wt (37)
            1135, // ebcdic-cp-yu (870)
            1147, // ebcdic-cyrillic (20880)
            1162, // ebcdic-de-273+euro (1141)
            1180, // ebcdic-dk-277+euro (1142)
            1198, // ebcdic-es-284+euro (1145)
            1216, // ebcdic-fi-278+euro (1143)
            1234, // ebcdic-fr-297+euro (1147)
            1252, // ebcdic-gb-285+euro (1146)
            1270, // ebcdic-international-500+euro (1148)
            1299, // ebcdic-is-871+euro (1149)
            1317, // ebcdic-it-280+euro (1144)
            1335, // ebcdic-jp-kana (20290)
            1349, // ebcdic-latin9--euro (20924)
            1368, // ebcdic-no-277+euro (1142)
            1386, // ebcdic-se-278+euro (1143)
            1404, // ebcdic-us-37+euro (1140)
            1421, // ecma-114 (28596)
            1429, // ecma-118 (28597)
            1437, // elot_928 (28597)
            1445, // euc-cn (51936)
            1451, // euc-jp (51932)
            1457, // euc-kr (51949)
            1463, // extended_unix_code_packed_format_for_japanese (51932)
            1508, // gb18030 (54936)
            1515, // gb2312 (936)
            1521, // gb2312-80 (936)
            1530, // gb231280 (936)
            1538, // gb_2312-80 (936)
            1548, // gbk (936)
            1551, // german (20106)
            1557, // greek (28597)
            1562, // greek8 (28597)
            1568, // hebrew (28598)
            1574, // hz-gb-2312 (52936)
            1584, // ibm-thai (20838)
            1592, // ibm00858 (858)
            1600, // ibm00924 (20924)
            1608, // ibm01047 (1047)
            1616, // ibm01140 (1140)
            1624, // ibm01141 (1141)
            1632, // ibm01142 (1142)
            1640, // ibm01143 (1143)
            1648, // ibm01144 (1144)
            1656, // ibm01145 (1145)
            1664, // ibm01146 (1146)
            1672, // ibm01147 (1147)
            1680, // ibm01148 (1148)
            1688, // ibm01149 (1149)
            1696, // ibm037 (37)
            1702, // ibm1026 (1026)
            1709, // ibm273 (20273)
            1715, // ibm277 (20277)
            1721, // ibm278 (20278)
            1727, // ibm280 (20280)
            1733, // ibm284 (20284)
            1739, // ibm285 (20285)
            1745, // ibm290 (20290)
            1751, // ibm297 (20297)
            1757, // ibm420 (20420)
            1763, // ibm423 (20423)
            1769, // ibm424 (20424)
            1775, // ibm437 (437)
            1781, // ibm500 (500)
            1787, // ibm737 (737)
            1793, // ibm775 (775)
            1799, // ibm850 (850)
            1805, // ibm852 (852)
            1811, // ibm855 (855)
            1817, // ibm857 (857)
            1823, // ibm860 (860)
            1829, // ibm861 (861)
            1835, // ibm862 (862)
            1841, // ibm863 (863)
            1847, // ibm864 (864)
            1853, // ibm865 (865)
            1859, // ibm866 (866)
            1865, // ibm869 (869)
            1871, // ibm870 (870)
            1877, // ibm871 (20871)
            1883, // ibm880 (20880)
            1889, // ibm905 (20905)
            1895, // irv (20105)
            1898, // iso-2022-jp (50220)
            1909, // iso-2022-jpeuc (51932)
            1923, // iso-2022-kr (50225)
            1934, // iso-2022-kr-7 (50225)
            1947, // iso-2022-kr-7bit (50225)
            1963, // iso-2022-kr-8 (51949)
            1976, // iso-2022-kr-8bit (51949)
            1992, // iso-8859-11 (874)
            2003, // iso-8859-13 (28603)
            2014, // iso-8859-15 (28605)
            2025, // iso-8859-2 (28592)
            2035, // iso-8859-3 (28593)
            2045, // iso-8859-4 (28594)
            2055, // iso-8859-5 (28595)
            2065, // iso-8859-6 (28596)
            2075, // iso-8859-7 (28597)
            2085, // iso-8859-8 (28598)
            2095, // iso-8859-8 visual (28598)
            2112, // iso-8859-8-i (38598)
            2124, // iso-8859-9 (28599)
            2134, // iso-ir-101 (28592)
            2144, // iso-ir-109 (28593)
            2154, // iso-ir-110 (28594)
            2164, // iso-ir-126 (28597)
            2174, // iso-ir-127 (28596)
            2184, // iso-ir-138 (28598)
            2194, // iso-ir-144 (28595)
            2204, // iso-ir-148 (28599)
            2214, // iso-ir-149 (949)
            2224, // iso-ir-58 (936)
            2233, // iso8859-2 (28592)
            2242, // iso_8859-15 (28605)
            2253, // iso_8859-2 (28592)
            2263, // iso_8859-2:1987 (28592)
            2278, // iso_8859-3 (28593)
            2288, // iso_8859-3:1988 (28593)
            2303, // iso_8859-4 (28594)
            2313, // iso_8859-4:1988 (28594)
            2328, // iso_8859-5 (28595)
            2338, // iso_8859-5:1988 (28595)
            2353, // iso_8859-6 (28596)
            2363, // iso_8859-6:1987 (28596)
            2378, // iso_8859-7 (28597)
            2388, // iso_8859-7:1987 (28597)
            2403, // iso_8859-8 (28598)
            2413, // iso_8859-8:1988 (28598)
            2428, // iso_8859-9 (28599)
            2438, // iso_8859-9:1989 (28599)
            2453, // johab (1361)
            2458, // koi (20866)
            2461, // koi8 (20866)
            2465, // koi8-r (20866)
            2471, // koi8-ru (21866)
            2478, // koi8-u (21866)
            2484, // koi8r (20866)
            2489, // korean (949)
            2495, // ks-c-5601 (949)
            2504, // ks-c5601 (949)
            2512, // ks_c_5601 (949)
            2521, // ks_c_5601-1987 (949)
            2535, // ks_c_5601-1989 (949)
            2549, // ks_c_5601_1987 (949)
            2563, // ksc5601 (949)
            2570, // ksc_5601 (949)
            2578, // l2 (28592)
            2580, // l3 (28593)
            2582, // l4 (28594)
            2584, // l5 (28599)
            2586, // l9 (28605)
            2588, // latin2 (28592)
            2594, // latin3 (28593)
            2600, // latin4 (28594)
            2606, // latin5 (28599)
            2612, // latin9 (28605)
            2618, // logical (28598)
            2625, // macintosh (10000)
            2634, // ms_kanji (932)
            2642, // norwegian (20108)
            2651, // ns_4551-1 (20108)
            2660, // pc-multilingual-850+euro (858)
            2684, // sen_850200_b (20107)
            2696, // shift-jis (932)
            2705, // shift_jis (932)
            2714, // sjis (932)
            2718, // swedish (20107)
            2725, // tis-620 (874)
            2732, // visual (28598)
            2738, // windows-1250 (1250)
            2750, // windows-1251 (1251)
            2762, // windows-1252 (1252)
            2774, // windows-1253 (1253)
            2786, // windows-1254 (1254)
            2798, // windows-1255 (1255)
            2810, // windows-1256 (1256)
            2822, // windows-1257 (1257)
            2834, // windows-1258 (1258)
            2846, // windows-874 (874)
            2857, // x-ansi (1252)
            2863, // x-chinese-cns (20000)
            2876, // x-chinese-eten (20002)
            2890, // x-cp1250 (1250)
            2898, // x-cp1251 (1251)
            2906, // x-cp20001 (20001)
            2915, // x-cp20003 (20003)
            2924, // x-cp20004 (20004)
            2933, // x-cp20005 (20005)
            2942, // x-cp20261 (20261)
            2951, // x-cp20269 (20269)
            2960, // x-cp20936 (20936)
            2969, // x-cp20949 (20949)
            2978, // x-cp50227 (50227)
            2987, // x-ebcdic-koreanextended (20833)
            3010, // x-euc (51932)
            3015, // x-euc-cn (51936)
            3023, // x-euc-jp (51932)
            3031, // x-europa (29001)
            3039, // x-ia5 (20105)
            3044, // x-ia5-german (20106)
            3056, // x-ia5-norwegian (20108)
            3071, // x-ia5-swedish (20107)
            3084, // x-iscii-as (57006)
            3094, // x-iscii-be (57003)
            3104, // x-iscii-de (57002)
            3114, // x-iscii-gu (57010)
            3124, // x-iscii-ka (57008)
            3134, // x-iscii-ma (57009)
            3144, // x-iscii-or (57007)
            3154, // x-iscii-pa (57011)
            3164, // x-iscii-ta (57004)
            3174, // x-iscii-te (57005)
            3184, // x-mac-arabic (10004)
            3196, // x-mac-ce (10029)
            3204, // x-mac-chinesesimp (10008)
            3221, // x-mac-chinesetrad (10002)
            3238, // x-mac-croatian (10082)
            3252, // x-mac-cyrillic (10007)
            3266, // x-mac-greek (10006)
            3277, // x-mac-hebrew (10005)
            3289, // x-mac-icelandic (10079)
            3304, // x-mac-japanese (10001)
            3318, // x-mac-korean (10003)
            3330, // x-mac-romanian (10010)
            3344, // x-mac-thai (10021)
            3354, // x-mac-turkish (10081)
            3367, // x-mac-ukrainian (10017)
            3382, // x-ms-cp932 (932)
            3392, // x-sjis (932)
            3398, // x-x-big5 (950)
            3406
        };

        // s_codePagesByName contains the list of supported codepages which match the encoding
        // names listed in s_encodingNames. The way mapping works is we binary search
        // s_encodingNames using s_encodingNamesIndices until we find a match for a given name.
        // The index of the entry in s_encodingNamesIndices will be the index of codepage in s_codePagesByName.
        private static readonly ushort[] s_codePagesByName = new ushort[]
        {
            437, // 437
            28596, // arabic
            708, // asmo-708
            950, // big5
            950, // big5-hkscs
            858, // ccsid00858
            20924, // ccsid00924
            1140, // ccsid01140
            1141, // ccsid01141
            1142, // ccsid01142
            1143, // ccsid01143
            1144, // ccsid01144
            1145, // ccsid01145
            1146, // ccsid01146
            1147, // ccsid01147
            1148, // ccsid01148
            1149, // ccsid01149
            936, // chinese
            950, // cn-big5
            936, // cn-gb
            858, // cp00858
            20924, // cp00924
            1140, // cp01140
            1141, // cp01141
            1142, // cp01142
            1143, // cp01143
            1144, // cp01144
            1145, // cp01145
            1146, // cp01146
            1147, // cp01147
            1148, // cp01148
            1149, // cp01149
            37, // cp037
            21025, // cp1025
            1026, // cp1026
            1256, // cp1256
            20273, // cp273
            20278, // cp278
            20280, // cp280
            20284, // cp284
            20285, // cp285
            20290, // cp290
            20297, // cp297
            20420, // cp420
            20423, // cp423
            20424, // cp424
            437, // cp437
            500, // cp500
            50227, // cp50227
            850, // cp850
            852, // cp852
            855, // cp855
            857, // cp857
            858, // cp858
            860, // cp860
            861, // cp861
            862, // cp862
            863, // cp863
            864, // cp864
            865, // cp865
            866, // cp866
            869, // cp869
            870, // cp870
            20871, // cp871
            875, // cp875
            20880, // cp880
            20905, // cp905
            950, // csbig5
            51949, // cseuckr
            51932, // cseucpkdfmtjapanese
            936, // csgb2312
            936, // csgb231280
            37, // csibm037
            1026, // csibm1026
            20273, // csibm273
            20277, // csibm277
            20278, // csibm278
            20280, // csibm280
            20284, // csibm284
            20285, // csibm285
            20290, // csibm290
            20297, // csibm297
            20420, // csibm420
            20423, // csibm423
            20424, // csibm424
            500, // csibm500
            870, // csibm870
            20871, // csibm871
            20880, // csibm880
            20905, // csibm905
            20838, // csibmthai
            50221, // csiso2022jp
            50225, // csiso2022kr
            936, // csiso58gb231280
            28592, // csisolatin2
            28593, // csisolatin3
            28594, // csisolatin4
            28599, // csisolatin5
            28605, // csisolatin9
            28596, // csisolatinarabic
            28595, // csisolatincyrillic
            28597, // csisolatingreek
            28598, // csisolatinhebrew
            20866, // cskoi8r
            949, // csksc56011987
            437, // cspc8codepage437
            932, // csshiftjis
            932, // cswindows31j
            28595, // cyrillic
            20106, // din_66003
            720, // dos-720
            862, // dos-862
            874, // dos-874
            20420, // ebcdic-cp-ar1
            500, // ebcdic-cp-be
            37, // ebcdic-cp-ca
            500, // ebcdic-cp-ch
            20277, // ebcdic-cp-dk
            20284, // ebcdic-cp-es
            20278, // ebcdic-cp-fi
            20297, // ebcdic-cp-fr
            20285, // ebcdic-cp-gb
            20423, // ebcdic-cp-gr
            20424, // ebcdic-cp-he
            20871, // ebcdic-cp-is
            20280, // ebcdic-cp-it
            37, // ebcdic-cp-nl
            20277, // ebcdic-cp-no
            870, // ebcdic-cp-roece
            20278, // ebcdic-cp-se
            20905, // ebcdic-cp-tr
            37, // ebcdic-cp-us
            37, // ebcdic-cp-wt
            870, // ebcdic-cp-yu
            20880, // ebcdic-cyrillic
            1141, // ebcdic-de-273+euro
            1142, // ebcdic-dk-277+euro
            1145, // ebcdic-es-284+euro
            1143, // ebcdic-fi-278+euro
            1147, // ebcdic-fr-297+euro
            1146, // ebcdic-gb-285+euro
            1148, // ebcdic-international-500+euro
            1149, // ebcdic-is-871+euro
            1144, // ebcdic-it-280+euro
            20290, // ebcdic-jp-kana
            20924, // ebcdic-latin9--euro
            1142, // ebcdic-no-277+euro
            1143, // ebcdic-se-278+euro
            1140, // ebcdic-us-37+euro
            28596, // ecma-114
            28597, // ecma-118
            28597, // elot_928
            51936, // euc-cn
            51932, // euc-jp
            51949, // euc-kr
            51932, // extended_unix_code_packed_format_for_japanese
            54936, // gb18030
            936, // gb2312
            936, // gb2312-80
            936, // gb231280
            936, // gb_2312-80
            936, // gbk
            20106, // german
            28597, // greek
            28597, // greek8
            28598, // hebrew
            52936, // hz-gb-2312
            20838, // ibm-thai
            858, // ibm00858
            20924, // ibm00924
            1047, // ibm01047
            1140, // ibm01140
            1141, // ibm01141
            1142, // ibm01142
            1143, // ibm01143
            1144, // ibm01144
            1145, // ibm01145
            1146, // ibm01146
            1147, // ibm01147
            1148, // ibm01148
            1149, // ibm01149
            37, // ibm037
            1026, // ibm1026
            20273, // ibm273
            20277, // ibm277
            20278, // ibm278
            20280, // ibm280
            20284, // ibm284
            20285, // ibm285
            20290, // ibm290
            20297, // ibm297
            20420, // ibm420
            20423, // ibm423
            20424, // ibm424
            437, // ibm437
            500, // ibm500
            737, // ibm737
            775, // ibm775
            850, // ibm850
            852, // ibm852
            855, // ibm855
            857, // ibm857
            860, // ibm860
            861, // ibm861
            862, // ibm862
            863, // ibm863
            864, // ibm864
            865, // ibm865
            866, // ibm866
            869, // ibm869
            870, // ibm870
            20871, // ibm871
            20880, // ibm880
            20905, // ibm905
            20105, // irv
            50220, // iso-2022-jp
            51932, // iso-2022-jpeuc
            50225, // iso-2022-kr
            50225, // iso-2022-kr-7
            50225, // iso-2022-kr-7bit
            51949, // iso-2022-kr-8
            51949, // iso-2022-kr-8bit
            874, // iso-8859-11
            28603, // iso-8859-13
            28605, // iso-8859-15
            28592, // iso-8859-2
            28593, // iso-8859-3
            28594, // iso-8859-4
            28595, // iso-8859-5
            28596, // iso-8859-6
            28597, // iso-8859-7
            28598, // iso-8859-8
            28598, // iso-8859-8 visual
            38598, // iso-8859-8-i
            28599, // iso-8859-9
            28592, // iso-ir-101
            28593, // iso-ir-109
            28594, // iso-ir-110
            28597, // iso-ir-126
            28596, // iso-ir-127
            28598, // iso-ir-138
            28595, // iso-ir-144
            28599, // iso-ir-148
            949, // iso-ir-149
            936, // iso-ir-58
            28592, // iso8859-2
            28605, // iso_8859-15
            28592, // iso_8859-2
            28592, // iso_8859-2:1987
            28593, // iso_8859-3
            28593, // iso_8859-3:1988
            28594, // iso_8859-4
            28594, // iso_8859-4:1988
            28595, // iso_8859-5
            28595, // iso_8859-5:1988
            28596, // iso_8859-6
            28596, // iso_8859-6:1987
            28597, // iso_8859-7
            28597, // iso_8859-7:1987
            28598, // iso_8859-8
            28598, // iso_8859-8:1988
            28599, // iso_8859-9
            28599, // iso_8859-9:1989
            1361, // johab
            20866, // koi
            20866, // koi8
            20866, // koi8-r
            21866, // koi8-ru
            21866, // koi8-u
            20866, // koi8r
            949, // korean
            949, // ks-c-5601
            949, // ks-c5601
            949, // ks_c_5601
            949, // ks_c_5601-1987
            949, // ks_c_5601-1989
            949, // ks_c_5601_1987
            949, // ksc5601
            949, // ksc_5601
            28592, // l2
            28593, // l3
            28594, // l4
            28599, // l5
            28605, // l9
            28592, // latin2
            28593, // latin3
            28594, // latin4
            28599, // latin5
            28605, // latin9
            28598, // logical
            10000, // macintosh
            932, // ms_kanji
            20108, // norwegian
            20108, // ns_4551-1
            858, // pc-multilingual-850+euro
            20107, // sen_850200_b
            932, // shift-jis
            932, // shift_jis
            932, // sjis
            20107, // swedish
            874, // tis-620
            28598, // visual
            1250, // windows-1250
            1251, // windows-1251
            1252, // windows-1252
            1253, // windows-1253
            1254, // windows-1254
            1255, // windows-1255
            1256, // windows-1256
            1257, // windows-1257
            1258, // windows-1258
            874, // windows-874
            1252, // x-ansi
            20000, // x-chinese-cns
            20002, // x-chinese-eten
            1250, // x-cp1250
            1251, // x-cp1251
            20001, // x-cp20001
            20003, // x-cp20003
            20004, // x-cp20004
            20005, // x-cp20005
            20261, // x-cp20261
            20269, // x-cp20269
            20936, // x-cp20936
            20949, // x-cp20949
            50227, // x-cp50227
            20833, // x-ebcdic-koreanextended
            51932, // x-euc
            51936, // x-euc-cn
            51932, // x-euc-jp
            29001, // x-europa
            20105, // x-ia5
            20106, // x-ia5-german
            20108, // x-ia5-norwegian
            20107, // x-ia5-swedish
            57006, // x-iscii-as
            57003, // x-iscii-be
            57002, // x-iscii-de
            57010, // x-iscii-gu
            57008, // x-iscii-ka
            57009, // x-iscii-ma
            57007, // x-iscii-or
            57011, // x-iscii-pa
            57004, // x-iscii-ta
            57005, // x-iscii-te
            10004, // x-mac-arabic
            10029, // x-mac-ce
            10008, // x-mac-chinesesimp
            10002, // x-mac-chinesetrad
            10082, // x-mac-croatian
            10007, // x-mac-cyrillic
            10006, // x-mac-greek
            10005, // x-mac-hebrew
            10079, // x-mac-icelandic
            10001, // x-mac-japanese
            10003, // x-mac-korean
            10010, // x-mac-romanian
            10021, // x-mac-thai
            10081, // x-mac-turkish
            10017, // x-mac-ukrainian
            932, // x-ms-cp932
            932, // x-sjis
            950, // x-x-big5
        };

        // When retrieving the value for System.Text.Encoding.WebName or
        // System.Text.Encoding.EncodingName given System.Text.Encoding.CodePage,
        // we perform a linear search on s_mappedCodePages to find the index of the
        // given codepage. This is used to index WebNameIndices to get the start
        // index of the web name in the string WebNames, and to index
        // s_englishNameIndices to get the start of the English name in s_englishNames.
        private static readonly ushort[] s_mappedCodePages = new ushort[]
        {
            37, // ibm037
            437, // ibm437
            500, // ibm500
            708, // asmo-708
            720, // dos-720
            737, // ibm737
            775, // ibm775
            850, // ibm850
            852, // ibm852
            855, // ibm855
            857, // ibm857
            858, // ibm00858
            860, // ibm860
            861, // ibm861
            862, // dos-862
            863, // ibm863
            864, // ibm864
            865, // ibm865
            866, // cp866
            869, // ibm869
            870, // ibm870
            874, // windows-874
            875, // cp875
            932, // shift_jis
            936, // gb2312
            949, // ks_c_5601-1987
            950, // big5
            1026, // ibm1026
            1047, // ibm01047
            1140, // ibm01140
            1141, // ibm01141
            1142, // ibm01142
            1143, // ibm01143
            1144, // ibm01144
            1145, // ibm01145
            1146, // ibm01146
            1147, // ibm01147
            1148, // ibm01148
            1149, // ibm01149
            1250, // windows-1250
            1251, // windows-1251
            1252, // windows-1252
            1253, // windows-1253
            1254, // windows-1254
            1255, // windows-1255
            1256, // windows-1256
            1257, // windows-1257
            1258, // windows-1258
            1361, // johab
            10000, // macintosh
            10001, // x-mac-japanese
            10002, // x-mac-chinesetrad
            10003, // x-mac-korean
            10004, // x-mac-arabic
            10005, // x-mac-hebrew
            10006, // x-mac-greek
            10007, // x-mac-cyrillic
            10008, // x-mac-chinesesimp
            10010, // x-mac-romanian
            10017, // x-mac-ukrainian
            10021, // x-mac-thai
            10029, // x-mac-ce
            10079, // x-mac-icelandic
            10081, // x-mac-turkish
            10082, // x-mac-croatian
            20000, // x-chinese-cns
            20001, // x-cp20001
            20002, // x-chinese-eten
            20003, // x-cp20003
            20004, // x-cp20004
            20005, // x-cp20005
            20105, // x-ia5
            20106, // x-ia5-german
            20107, // x-ia5-swedish
            20108, // x-ia5-norwegian
            20261, // x-cp20261
            20269, // x-cp20269
            20273, // ibm273
            20277, // ibm277
            20278, // ibm278
            20280, // ibm280
            20284, // ibm284
            20285, // ibm285
            20290, // ibm290
            20297, // ibm297
            20420, // ibm420
            20423, // ibm423
            20424, // ibm424
            20833, // x-ebcdic-koreanextended
            20838, // ibm-thai
            20866, // koi8-r
            20871, // ibm871
            20880, // ibm880
            20905, // ibm905
            20924, // ibm00924
            20932, // euc-jp
            20936, // x-cp20936
            20949, // x-cp20949
            21025, // cp1025
            21866, // koi8-u
            28592, // iso-8859-2
            28593, // iso-8859-3
            28594, // iso-8859-4
            28595, // iso-8859-5
            28596, // iso-8859-6
            28597, // iso-8859-7
            28598, // iso-8859-8
            28599, // iso-8859-9
            28603, // iso-8859-13
            28605, // iso-8859-15
            29001, // x-europa
            38598, // iso-8859-8-i
            50220, // iso-2022-jp
            50221, // csiso2022jp
            50222, // iso-2022-jp
            50225, // iso-2022-kr
            50227, // x-cp50227
            51932, // euc-jp
            51936, // euc-cn
            51949, // euc-kr
            52936, // hz-gb-2312
            54936, // gb18030
            57002, // x-iscii-de
            57003, // x-iscii-be
            57004, // x-iscii-ta
            57005, // x-iscii-te
            57006, // x-iscii-as
            57007, // x-iscii-or
            57008, // x-iscii-ka
            57009, // x-iscii-ma
            57010, // x-iscii-gu
            57011, // x-iscii-pa
        };

        // s_webNames is a concatenation of the default encoding names
        // for each code page. It is used in retrieving the value for
        // System.Text.Encoding.WebName given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string s_webNames =
            "ibm037" + // 37
            "ibm437" + // 437
            "ibm500" + // 500
            "asmo-708" + // 708
            "dos-720" + // 720
            "ibm737" + // 737
            "ibm775" + // 775
            "ibm850" + // 850
            "ibm852" + // 852
            "ibm855" + // 855
            "ibm857" + // 857
            "ibm00858" + // 858
            "ibm860" + // 860
            "ibm861" + // 861
            "dos-862" + // 862
            "ibm863" + // 863
            "ibm864" + // 864
            "ibm865" + // 865
            "cp866" + // 866
            "ibm869" + // 869
            "ibm870" + // 870
            "windows-874" + // 874
            "cp875" + // 875
            "shift_jis" + // 932
            "gb2312" + // 936
            "ks_c_5601-1987" + // 949
            "big5" + // 950
            "ibm1026" + // 1026
            "ibm01047" + // 1047
            "ibm01140" + // 1140
            "ibm01141" + // 1141
            "ibm01142" + // 1142
            "ibm01143" + // 1143
            "ibm01144" + // 1144
            "ibm01145" + // 1145
            "ibm01146" + // 1146
            "ibm01147" + // 1147
            "ibm01148" + // 1148
            "ibm01149" + // 1149
            "windows-1250" + // 1250
            "windows-1251" + // 1251
            "windows-1252" + // 1252
            "windows-1253" + // 1253
            "windows-1254" + // 1254
            "windows-1255" + // 1255
            "windows-1256" + // 1256
            "windows-1257" + // 1257
            "windows-1258" + // 1258
            "johab" + // 1361
            "macintosh" + // 10000
            "x-mac-japanese" + // 10001
            "x-mac-chinesetrad" + // 10002
            "x-mac-korean" + // 10003
            "x-mac-arabic" + // 10004
            "x-mac-hebrew" + // 10005
            "x-mac-greek" + // 10006
            "x-mac-cyrillic" + // 10007
            "x-mac-chinesesimp" + // 10008
            "x-mac-romanian" + // 10010
            "x-mac-ukrainian" + // 10017
            "x-mac-thai" + // 10021
            "x-mac-ce" + // 10029
            "x-mac-icelandic" + // 10079
            "x-mac-turkish" + // 10081
            "x-mac-croatian" + // 10082
            "x-chinese-cns" + // 20000
            "x-cp20001" + // 20001
            "x-chinese-eten" + // 20002
            "x-cp20003" + // 20003
            "x-cp20004" + // 20004
            "x-cp20005" + // 20005
            "x-ia5" + // 20105
            "x-ia5-german" + // 20106
            "x-ia5-swedish" + // 20107
            "x-ia5-norwegian" + // 20108
            "x-cp20261" + // 20261
            "x-cp20269" + // 20269
            "ibm273" + // 20273
            "ibm277" + // 20277
            "ibm278" + // 20278
            "ibm280" + // 20280
            "ibm284" + // 20284
            "ibm285" + // 20285
            "ibm290" + // 20290
            "ibm297" + // 20297
            "ibm420" + // 20420
            "ibm423" + // 20423
            "ibm424" + // 20424
            "x-ebcdic-koreanextended" + // 20833
            "ibm-thai" + // 20838
            "koi8-r" + // 20866
            "ibm871" + // 20871
            "ibm880" + // 20880
            "ibm905" + // 20905
            "ibm00924" + // 20924
            "euc-jp" + // 20932
            "x-cp20936" + // 20936
            "x-cp20949" + // 20949
            "cp1025" + // 21025
            "koi8-u" + // 21866
            "iso-8859-2" + // 28592
            "iso-8859-3" + // 28593
            "iso-8859-4" + // 28594
            "iso-8859-5" + // 28595
            "iso-8859-6" + // 28596
            "iso-8859-7" + // 28597
            "iso-8859-8" + // 28598
            "iso-8859-9" + // 28599
            "iso-8859-13" + // 28603
            "iso-8859-15" + // 28605
            "x-europa" + // 29001
            "iso-8859-8-i" + // 38598
            "iso-2022-jp" + // 50220
            "csiso2022jp" + // 50221
            "iso-2022-jp" + // 50222
            "iso-2022-kr" + // 50225
            "x-cp50227" + // 50227
            "euc-jp" + // 51932
            "euc-cn" + // 51936
            "euc-kr" + // 51949
            "hz-gb-2312" + // 52936
            "gb18030" + // 54936
            "x-iscii-de" + // 57002
            "x-iscii-be" + // 57003
            "x-iscii-ta" + // 57004
            "x-iscii-te" + // 57005
            "x-iscii-as" + // 57006
            "x-iscii-or" + // 57007
            "x-iscii-ka" + // 57008
            "x-iscii-ma" + // 57009
            "x-iscii-gu" + // 57010
            "x-iscii-pa" + // 57011
            "";

        // s_webNameIndices contains the start index of each code page's default
        // web name in the string s_webNames. It is indexed by an index into
        // s_mappedCodePages.
        private static readonly int[] s_webNameIndices = new int[]
        {
            0, // ibm037 (37)
            6, // ibm437 (437)
            12, // ibm500 (500)
            18, // asmo-708 (708)
            26, // dos-720 (720)
            33, // ibm737 (737)
            39, // ibm775 (775)
            45, // ibm850 (850)
            51, // ibm852 (852)
            57, // ibm855 (855)
            63, // ibm857 (857)
            69, // ibm00858 (858)
            77, // ibm860 (860)
            83, // ibm861 (861)
            89, // dos-862 (862)
            96, // ibm863 (863)
            102, // ibm864 (864)
            108, // ibm865 (865)
            114, // cp866 (866)
            119, // ibm869 (869)
            125, // ibm870 (870)
            131, // windows-874 (874)
            142, // cp875 (875)
            147, // shift_jis (932)
            156, // gb2312 (936)
            162, // ks_c_5601-1987 (949)
            176, // big5 (950)
            180, // ibm1026 (1026)
            187, // ibm01047 (1047)
            195, // ibm01140 (1140)
            203, // ibm01141 (1141)
            211, // ibm01142 (1142)
            219, // ibm01143 (1143)
            227, // ibm01144 (1144)
            235, // ibm01145 (1145)
            243, // ibm01146 (1146)
            251, // ibm01147 (1147)
            259, // ibm01148 (1148)
            267, // ibm01149 (1149)
            275, // windows-1250 (1250)
            287, // windows-1251 (1251)
            299, // windows-1252 (1252)
            311, // windows-1253 (1253)
            323, // windows-1254 (1254)
            335, // windows-1255 (1255)
            347, // windows-1256 (1256)
            359, // windows-1257 (1257)
            371, // windows-1258 (1258)
            383, // johab (1361)
            388, // macintosh (10000)
            397, // x-mac-japanese (10001)
            411, // x-mac-chinesetrad (10002)
            428, // x-mac-korean (10003)
            440, // x-mac-arabic (10004)
            452, // x-mac-hebrew (10005)
            464, // x-mac-greek (10006)
            475, // x-mac-cyrillic (10007)
            489, // x-mac-chinesesimp (10008)
            506, // x-mac-romanian (10010)
            520, // x-mac-ukrainian (10017)
            535, // x-mac-thai (10021)
            545, // x-mac-ce (10029)
            553, // x-mac-icelandic (10079)
            568, // x-mac-turkish (10081)
            581, // x-mac-croatian (10082)
            595, // x-chinese-cns (20000)
            608, // x-cp20001 (20001)
            617, // x-chinese-eten (20002)
            631, // x-cp20003 (20003)
            640, // x-cp20004 (20004)
            649, // x-cp20005 (20005)
            658, // x-ia5 (20105)
            663, // x-ia5-german (20106)
            675, // x-ia5-swedish (20107)
            688, // x-ia5-norwegian (20108)
            703, // x-cp20261 (20261)
            712, // x-cp20269 (20269)
            721, // ibm273 (20273)
            727, // ibm277 (20277)
            733, // ibm278 (20278)
            739, // ibm280 (20280)
            745, // ibm284 (20284)
            751, // ibm285 (20285)
            757, // ibm290 (20290)
            763, // ibm297 (20297)
            769, // ibm420 (20420)
            775, // ibm423 (20423)
            781, // ibm424 (20424)
            787, // x-ebcdic-koreanextended (20833)
            810, // ibm-thai (20838)
            818, // koi8-r (20866)
            824, // ibm871 (20871)
            830, // ibm880 (20880)
            836, // ibm905 (20905)
            842, // ibm00924 (20924)
            850, // euc-jp (20932)
            856, // x-cp20936 (20936)
            865, // x-cp20949 (20949)
            874, // cp1025 (21025)
            880, // koi8-u (21866)
            886, // iso-8859-2 (28592)
            896, // iso-8859-3 (28593)
            906, // iso-8859-4 (28594)
            916, // iso-8859-5 (28595)
            926, // iso-8859-6 (28596)
            936, // iso-8859-7 (28597)
            946, // iso-8859-8 (28598)
            956, // iso-8859-9 (28599)
            966, // iso-8859-13 (28603)
            977, // iso-8859-15 (28605)
            988, // x-europa (29001)
            996, // iso-8859-8-i (38598)
            1008, // iso-2022-jp (50220)
            1019, // csiso2022jp (50221)
            1030, // iso-2022-jp (50222)
            1041, // iso-2022-kr (50225)
            1052, // x-cp50227 (50227)
            1061, // euc-jp (51932)
            1067, // euc-cn (51936)
            1073, // euc-kr (51949)
            1079, // hz-gb-2312 (52936)
            1089, // gb18030 (54936)
            1096, // x-iscii-de (57002)
            1106, // x-iscii-be (57003)
            1116, // x-iscii-ta (57004)
            1126, // x-iscii-te (57005)
            1136, // x-iscii-as (57006)
            1146, // x-iscii-or (57007)
            1156, // x-iscii-ka (57008)
            1166, // x-iscii-ma (57009)
            1176, // x-iscii-gu (57010)
            1186, // x-iscii-pa (57011)
            1196
        };

        // s_englishNames is the concatenation of the English names for each codepage.
        // It is used in retrieving the value for System.Text.Encoding.EncodingName
        // given System.Text.Encoding.CodePage.
        // This is done rather than using a large readonly array of strings to avoid
        // generating a large amount of code in the static constructor.
        private const string s_englishNames =
            "IBM EBCDIC (US-Canada)" + // 37
            "OEM United States" + // 437
            "IBM EBCDIC (International)" + // 500
            "Arabic (ASMO 708)" + // 708
            "Arabic (DOS)" + // 720
            "Greek (DOS)" + // 737
            "Baltic (DOS)" + // 775
            "Western European (DOS)" + // 850
            "Central European (DOS)" + // 852
            "OEM Cyrillic" + // 855
            "Turkish (DOS)" + // 857
            "OEM Multilingual Latin I" + // 858
            "Portuguese (DOS)" + // 860
            "Icelandic (DOS)" + // 861
            "Hebrew (DOS)" + // 862
            "French Canadian (DOS)" + // 863
            "Arabic (864)" + // 864
            "Nordic (DOS)" + // 865
            "Cyrillic (DOS)" + // 866
            "Greek, Modern (DOS)" + // 869
            "IBM EBCDIC (Multilingual Latin-2)" + // 870
            "Thai (Windows)" + // 874
            "IBM EBCDIC (Greek Modern)" + // 875
            "Japanese (Shift-JIS)" + // 932
            "Chinese Simplified (GB2312)" + // 936
            "Korean" + // 949
            "Chinese Traditional (Big5)" + // 950
            "IBM EBCDIC (Turkish Latin-5)" + // 1026
            "IBM Latin-1" + // 1047
            "IBM EBCDIC (US-Canada-Euro)" + // 1140
            "IBM EBCDIC (Germany-Euro)" + // 1141
            "IBM EBCDIC (Denmark-Norway-Euro)" + // 1142
            "IBM EBCDIC (Finland-Sweden-Euro)" + // 1143
            "IBM EBCDIC (Italy-Euro)" + // 1144
            "IBM EBCDIC (Spain-Euro)" + // 1145
            "IBM EBCDIC (UK-Euro)" + // 1146
            "IBM EBCDIC (France-Euro)" + // 1147
            "IBM EBCDIC (International-Euro)" + // 1148
            "IBM EBCDIC (Icelandic-Euro)" + // 1149
            "Central European (Windows)" + // 1250
            "Cyrillic (Windows)" + // 1251
            "Western European (Windows)" + // 1252
            "Greek (Windows)" + // 1253
            "Turkish (Windows)" + // 1254
            "Hebrew (Windows)" + // 1255
            "Arabic (Windows)" + // 1256
            "Baltic (Windows)" + // 1257
            "Vietnamese (Windows)" + // 1258
            "Korean (Johab)" + // 1361
            "Western European (Mac)" + // 10000
            "Japanese (Mac)" + // 10001
            "Chinese Traditional (Mac)" + // 10002
            "Korean (Mac)" + // 10003
            "Arabic (Mac)" + // 10004
            "Hebrew (Mac)" + // 10005
            "Greek (Mac)" + // 10006
            "Cyrillic (Mac)" + // 10007
            "Chinese Simplified (Mac)" + // 10008
            "Romanian (Mac)" + // 10010
            "Ukrainian (Mac)" + // 10017
            "Thai (Mac)" + // 10021
            "Central European (Mac)" + // 10029
            "Icelandic (Mac)" + // 10079
            "Turkish (Mac)" + // 10081
            "Croatian (Mac)" + // 10082
            "Chinese Traditional (CNS)" + // 20000
            "TCA Taiwan" + // 20001
            "Chinese Traditional (Eten)" + // 20002
            "IBM5550 Taiwan" + // 20003
            "TeleText Taiwan" + // 20004
            "Wang Taiwan" + // 20005
            "Western European (IA5)" + // 20105
            "German (IA5)" + // 20106
            "Swedish (IA5)" + // 20107
            "Norwegian (IA5)" + // 20108
            "T.61" + // 20261
            "ISO-6937" + // 20269
            "IBM EBCDIC (Germany)" + // 20273
            "IBM EBCDIC (Denmark-Norway)" + // 20277
            "IBM EBCDIC (Finland-Sweden)" + // 20278
            "IBM EBCDIC (Italy)" + // 20280
            "IBM EBCDIC (Spain)" + // 20284
            "IBM EBCDIC (UK)" + // 20285
            "IBM EBCDIC (Japanese katakana)" + // 20290
            "IBM EBCDIC (France)" + // 20297
            "IBM EBCDIC (Arabic)" + // 20420
            "IBM EBCDIC (Greek)" + // 20423
            "IBM EBCDIC (Hebrew)" + // 20424
            "IBM EBCDIC (Korean Extended)" + // 20833
            "IBM EBCDIC (Thai)" + // 20838
            "Cyrillic (KOI8-R)" + // 20866
            "IBM EBCDIC (Icelandic)" + // 20871
            "IBM EBCDIC (Cyrillic Russian)" + // 20880
            "IBM EBCDIC (Turkish)" + // 20905
            "IBM Latin-1" + // 20924
            "Japanese (JIS 0208-1990 and 0212-1990)" + // 20932
            "Chinese Simplified (GB2312-80)" + // 20936
            "Korean Wansung" + // 20949
            "IBM EBCDIC (Cyrillic Serbian-Bulgarian)" + // 21025
            "Cyrillic (KOI8-U)" + // 21866
            "Central European (ISO)" + // 28592
            "Latin 3 (ISO)" + // 28593
            "Baltic (ISO)" + // 28594
            "Cyrillic (ISO)" + // 28595
            "Arabic (ISO)" + // 28596
            "Greek (ISO)" + // 28597
            "Hebrew (ISO-Visual)" + // 28598
            "Turkish (ISO)" + // 28599
            "Estonian (ISO)" + // 28603
            "Latin 9 (ISO)" + // 28605
            "Europa" + // 29001
            "Hebrew (ISO-Logical)" + // 38598
            "Japanese (JIS)" + // 50220
            "Japanese (JIS-Allow 1 byte Kana)" + // 50221
            "Japanese (JIS-Allow 1 byte Kana - SO/SI)" + // 50222
            "Korean (ISO)" + // 50225
            "Chinese Simplified (ISO-2022)" + // 50227
            "Japanese (EUC)" + // 51932
            "Chinese Simplified (EUC)" + // 51936
            "Korean (EUC)" + // 51949
            "Chinese Simplified (HZ)" + // 52936
            "Chinese Simplified (GB18030)" + // 54936
            "ISCII Devanagari" + // 57002
            "ISCII Bengali" + // 57003
            "ISCII Tamil" + // 57004
            "ISCII Telugu" + // 57005
            "ISCII Assamese" + // 57006
            "ISCII Oriya" + // 57007
            "ISCII Kannada" + // 57008
            "ISCII Malayalam" + // 57009
            "ISCII Gujarati" + // 57010
            "ISCII Punjabi" + // 57011
            "";

        // s_englishNameIndices contains the start index of each code page's English
        // name in the string s_englishNames. It is indexed by an index into s_mappedCodePages.
        private static readonly int[] s_englishNameIndices = new int[]
        {
            0, // IBM EBCDIC (US-Canada) (37)
            22, // OEM United States (437)
            39, // IBM EBCDIC (International) (500)
            65, // Arabic (ASMO 708) (708)
            82, // Arabic (DOS) (720)
            94, // Greek (DOS) (737)
            105, // Baltic (DOS) (775)
            117, // Western European (DOS) (850)
            139, // Central European (DOS) (852)
            161, // OEM Cyrillic (855)
            173, // Turkish (DOS) (857)
            186, // OEM Multilingual Latin I (858)
            210, // Portuguese (DOS) (860)
            226, // Icelandic (DOS) (861)
            241, // Hebrew (DOS) (862)
            253, // French Canadian (DOS) (863)
            274, // Arabic (864) (864)
            286, // Nordic (DOS) (865)
            298, // Cyrillic (DOS) (866)
            312, // Greek, Modern (DOS) (869)
            331, // IBM EBCDIC (Multilingual Latin-2) (870)
            364, // Thai (Windows) (874)
            378, // IBM EBCDIC (Greek Modern) (875)
            403, // Japanese (Shift-JIS) (932)
            423, // Chinese Simplified (GB2312) (936)
            450, // Korean (949)
            456, // Chinese Traditional (Big5) (950)
            482, // IBM EBCDIC (Turkish Latin-5) (1026)
            510, // IBM Latin-1 (1047)
            521, // IBM EBCDIC (US-Canada-Euro) (1140)
            548, // IBM EBCDIC (Germany-Euro) (1141)
            573, // IBM EBCDIC (Denmark-Norway-Euro) (1142)
            605, // IBM EBCDIC (Finland-Sweden-Euro) (1143)
            637, // IBM EBCDIC (Italy-Euro) (1144)
            660, // IBM EBCDIC (Spain-Euro) (1145)
            683, // IBM EBCDIC (UK-Euro) (1146)
            703, // IBM EBCDIC (France-Euro) (1147)
            727, // IBM EBCDIC (International-Euro) (1148)
            758, // IBM EBCDIC (Icelandic-Euro) (1149)
            785, // Central European (Windows) (1250)
            811, // Cyrillic (Windows) (1251)
            829, // Western European (Windows) (1252)
            855, // Greek (Windows) (1253)
            870, // Turkish (Windows) (1254)
            887, // Hebrew (Windows) (1255)
            903, // Arabic (Windows) (1256)
            919, // Baltic (Windows) (1257)
            935, // Vietnamese (Windows) (1258)
            955, // Korean (Johab) (1361)
            969, // Western European (Mac) (10000)
            991, // Japanese (Mac) (10001)
            1005, // Chinese Traditional (Mac) (10002)
            1030, // Korean (Mac) (10003)
            1042, // Arabic (Mac) (10004)
            1054, // Hebrew (Mac) (10005)
            1066, // Greek (Mac) (10006)
            1077, // Cyrillic (Mac) (10007)
            1091, // Chinese Simplified (Mac) (10008)
            1115, // Romanian (Mac) (10010)
            1129, // Ukrainian (Mac) (10017)
            1144, // Thai (Mac) (10021)
            1154, // Central European (Mac) (10029)
            1176, // Icelandic (Mac) (10079)
            1191, // Turkish (Mac) (10081)
            1204, // Croatian (Mac) (10082)
            1218, // Chinese Traditional (CNS) (20000)
            1243, // TCA Taiwan (20001)
            1253, // Chinese Traditional (Eten) (20002)
            1279, // IBM5550 Taiwan (20003)
            1293, // TeleText Taiwan (20004)
            1308, // Wang Taiwan (20005)
            1319, // Western European (IA5) (20105)
            1341, // German (IA5) (20106)
            1353, // Swedish (IA5) (20107)
            1366, // Norwegian (IA5) (20108)
            1381, // T.61 (20261)
            1385, // ISO-6937 (20269)
            1393, // IBM EBCDIC (Germany) (20273)
            1413, // IBM EBCDIC (Denmark-Norway) (20277)
            1440, // IBM EBCDIC (Finland-Sweden) (20278)
            1467, // IBM EBCDIC (Italy) (20280)
            1485, // IBM EBCDIC (Spain) (20284)
            1503, // IBM EBCDIC (UK) (20285)
            1518, // IBM EBCDIC (Japanese katakana) (20290)
            1548, // IBM EBCDIC (France) (20297)
            1567, // IBM EBCDIC (Arabic) (20420)
            1586, // IBM EBCDIC (Greek) (20423)
            1604, // IBM EBCDIC (Hebrew) (20424)
            1623, // IBM EBCDIC (Korean Extended) (20833)
            1651, // IBM EBCDIC (Thai) (20838)
            1668, // Cyrillic (KOI8-R) (20866)
            1685, // IBM EBCDIC (Icelandic) (20871)
            1707, // IBM EBCDIC (Cyrillic Russian) (20880)
            1736, // IBM EBCDIC (Turkish) (20905)
            1756, // IBM Latin-1 (20924)
            1767, // Japanese (JIS 0208-1990 and 0212-1990) (20932)
            1805, // Chinese Simplified (GB2312-80) (20936)
            1835, // Korean Wansung (20949)
            1849, // IBM EBCDIC (Cyrillic Serbian-Bulgarian) (21025)
            1888, // Cyrillic (KOI8-U) (21866)
            1905, // Central European (ISO) (28592)
            1927, // Latin 3 (ISO) (28593)
            1940, // Baltic (ISO) (28594)
            1952, // Cyrillic (ISO) (28595)
            1966, // Arabic (ISO) (28596)
            1978, // Greek (ISO) (28597)
            1989, // Hebrew (ISO-Visual) (28598)
            2008, // Turkish (ISO) (28599)
            2021, // Estonian (ISO) (28603)
            2035, // Latin 9 (ISO) (28605)
            2048, // Europa (29001)
            2054, // Hebrew (ISO-Logical) (38598)
            2074, // Japanese (JIS) (50220)
            2088, // Japanese (JIS-Allow 1 byte Kana) (50221)
            2120, // Japanese (JIS-Allow 1 byte Kana - SO/SI) (50222)
            2160, // Korean (ISO) (50225)
            2172, // Chinese Simplified (ISO-2022) (50227)
            2201, // Japanese (EUC) (51932)
            2215, // Chinese Simplified (EUC) (51936)
            2239, // Korean (EUC) (51949)
            2251, // Chinese Simplified (HZ) (52936)
            2274, // Chinese Simplified (GB18030) (54936)
            2302, // ISCII Devanagari (57002)
            2318, // ISCII Bengali (57003)
            2331, // ISCII Tamil (57004)
            2342, // ISCII Telugu (57005)
            2354, // ISCII Assamese (57006)
            2368, // ISCII Oriya (57007)
            2379, // ISCII Kannada (57008)
            2392, // ISCII Malayalam (57009)
            2407, // ISCII Gujarati (57010)
            2421, // ISCII Punjabi (57011)
            2434
        };

    }
}
