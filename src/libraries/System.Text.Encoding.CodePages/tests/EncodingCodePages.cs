// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.Serialization.Formatters.Tests;
using Xunit;

namespace System.Text.Tests
{
    public partial class EncodingTest : IClassFixture<CultureSetup>
    {
        public EncodingTest(CultureSetup setup)
        {
            // Setting up the culture happens externally, and only once, which is what we want.
            // xUnit will keep track of it, do nothing.
        }

        public static IEnumerable<object[]> CodePageInfo()
        {
            // The layout is code page, IANA(web) name, and query string.
            // Query strings may be undocumented, and IANA names will be returned from Encoding objects.
            // Entries are sorted by code page.
            yield return new object[] { 37, "ibm037", "ibm037" };
            yield return new object[] { 37, "ibm037", "cp037" };
            yield return new object[] { 37, "ibm037", "csibm037" };
            yield return new object[] { 37, "ibm037", "ebcdic-cp-ca" };
            yield return new object[] { 37, "ibm037", "ebcdic-cp-nl" };
            yield return new object[] { 37, "ibm037", "ebcdic-cp-us" };
            yield return new object[] { 37, "ibm037", "ebcdic-cp-wt" };
            yield return new object[] { 437, "ibm437", "ibm437" };
            yield return new object[] { 437, "ibm437", "437" };
            yield return new object[] { 437, "ibm437", "cp437" };
            yield return new object[] { 437, "ibm437", "cspc8codepage437" };
            yield return new object[] { 500, "ibm500", "ibm500" };
            yield return new object[] { 500, "ibm500", "cp500" };
            yield return new object[] { 500, "ibm500", "csibm500" };
            yield return new object[] { 500, "ibm500", "ebcdic-cp-be" };
            yield return new object[] { 500, "ibm500", "ebcdic-cp-ch" };
            yield return new object[] { 708, "asmo-708", "asmo-708" };
            yield return new object[] { 720, "dos-720", "dos-720" };
            yield return new object[] { 737, "ibm737", "ibm737" };
            yield return new object[] { 775, "ibm775", "ibm775" };
            yield return new object[] { 850, "ibm850", "ibm850" };
            yield return new object[] { 850, "ibm850", "cp850" };
            yield return new object[] { 852, "ibm852", "ibm852" };
            yield return new object[] { 852, "ibm852", "cp852" };
            yield return new object[] { 855, "ibm855", "ibm855" };
            yield return new object[] { 855, "ibm855", "cp855" };
            yield return new object[] { 857, "ibm857", "ibm857" };
            yield return new object[] { 857, "ibm857", "cp857" };
            yield return new object[] { 858, "ibm00858", "ibm00858" };
            yield return new object[] { 858, "ibm00858", "ccsid00858" };
            yield return new object[] { 858, "ibm00858", "cp00858" };
            yield return new object[] { 858, "ibm00858", "cp858" };
            yield return new object[] { 858, "ibm00858", "pc-multilingual-850+euro" };
            yield return new object[] { 860, "ibm860", "ibm860" };
            yield return new object[] { 860, "ibm860", "cp860" };
            yield return new object[] { 861, "ibm861", "ibm861" };
            yield return new object[] { 861, "ibm861", "cp861" };
            yield return new object[] { 862, "dos-862", "dos-862" };
            yield return new object[] { 862, "dos-862", "cp862" };
            yield return new object[] { 862, "dos-862", "ibm862" };
            yield return new object[] { 863, "ibm863", "ibm863" };
            yield return new object[] { 863, "ibm863", "cp863" };
            yield return new object[] { 864, "ibm864", "ibm864" };
            yield return new object[] { 864, "ibm864", "cp864" };
            yield return new object[] { 865, "ibm865", "ibm865" };
            yield return new object[] { 865, "ibm865", "cp865" };
            yield return new object[] { 866, "cp866", "cp866" };
            yield return new object[] { 866, "cp866", "ibm866" };
            yield return new object[] { 869, "ibm869", "ibm869" };
            yield return new object[] { 869, "ibm869", "cp869" };
            yield return new object[] { 870, "ibm870", "ibm870" };
            yield return new object[] { 870, "ibm870", "cp870" };
            yield return new object[] { 870, "ibm870", "csibm870" };
            yield return new object[] { 870, "ibm870", "ebcdic-cp-roece" };
            yield return new object[] { 870, "ibm870", "ebcdic-cp-yu" };
            yield return new object[] { 874, "windows-874", "windows-874" };
            yield return new object[] { 874, "windows-874", "dos-874" };
            yield return new object[] { 874, "windows-874", "iso-8859-11" };
            yield return new object[] { 874, "windows-874", "tis-620" };
            yield return new object[] { 875, "cp875", "cp875" };
            yield return new object[] { 932, "shift_jis", "shift_jis" };
            yield return new object[] { 932, "shift_jis", "csshiftjis" };
            yield return new object[] { 932, "shift_jis", "cswindows31j" };
            yield return new object[] { 932, "shift_jis", "ms_kanji" };
            yield return new object[] { 932, "shift_jis", "shift-jis" };
            yield return new object[] { 932, "shift_jis", "sjis" };
            yield return new object[] { 932, "shift_jis", "x-ms-cp932" };
            yield return new object[] { 932, "shift_jis", "x-sjis" };
            yield return new object[] { 936, "gb2312", "gb2312" };
            yield return new object[] { 936, "gb2312", "chinese" };
            yield return new object[] { 936, "gb2312", "cn-gb" };
            yield return new object[] { 936, "gb2312", "csgb2312" };
            yield return new object[] { 936, "gb2312", "csgb231280" };
            yield return new object[] { 936, "gb2312", "csiso58gb231280" };
            yield return new object[] { 936, "gb2312", "gb_2312-80" };
            yield return new object[] { 936, "gb2312", "gb231280" };
            yield return new object[] { 936, "gb2312", "gb2312-80" };
            yield return new object[] { 936, "gb2312", "gbk" };
            yield return new object[] { 936, "gb2312", "iso-ir-58" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks_c_5601-1987" };
            yield return new object[] { 949, "ks_c_5601-1987", "csksc56011987" };
            yield return new object[] { 949, "ks_c_5601-1987", "iso-ir-149" };
            yield return new object[] { 949, "ks_c_5601-1987", "korean" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks_c_5601" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks_c_5601_1987" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks_c_5601-1989" };
            yield return new object[] { 949, "ks_c_5601-1987", "ksc_5601" };
            yield return new object[] { 949, "ks_c_5601-1987", "ksc5601" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks-c5601" };
            yield return new object[] { 949, "ks_c_5601-1987", "ks-c-5601" };
            yield return new object[] { 950, "big5", "big5" };
            yield return new object[] { 950, "big5", "big5-hkscs" };
            yield return new object[] { 950, "big5", "cn-big5" };
            yield return new object[] { 950, "big5", "csbig5" };
            yield return new object[] { 950, "big5", "x-x-big5" };
            yield return new object[] { 1026, "ibm1026", "ibm1026" };
            yield return new object[] { 1026, "ibm1026", "cp1026" };
            yield return new object[] { 1026, "ibm1026", "csibm1026" };
            yield return new object[] { 1047, "ibm01047", "ibm01047" };
            yield return new object[] { 1140, "ibm01140", "ibm01140" };
            yield return new object[] { 1140, "ibm01140", "ccsid01140" };
            yield return new object[] { 1140, "ibm01140", "cp01140" };
            yield return new object[] { 1140, "ibm01140", "ebcdic-us-37+euro" };
            yield return new object[] { 1141, "ibm01141", "ibm01141" };
            yield return new object[] { 1141, "ibm01141", "ccsid01141" };
            yield return new object[] { 1141, "ibm01141", "cp01141" };
            yield return new object[] { 1141, "ibm01141", "ebcdic-de-273+euro" };
            yield return new object[] { 1142, "ibm01142", "ibm01142" };
            yield return new object[] { 1142, "ibm01142", "ccsid01142" };
            yield return new object[] { 1142, "ibm01142", "cp01142" };
            yield return new object[] { 1142, "ibm01142", "ebcdic-dk-277+euro" };
            yield return new object[] { 1142, "ibm01142", "ebcdic-no-277+euro" };
            yield return new object[] { 1143, "ibm01143", "ibm01143" };
            yield return new object[] { 1143, "ibm01143", "ccsid01143" };
            yield return new object[] { 1143, "ibm01143", "cp01143" };
            yield return new object[] { 1143, "ibm01143", "ebcdic-fi-278+euro" };
            yield return new object[] { 1143, "ibm01143", "ebcdic-se-278+euro" };
            yield return new object[] { 1144, "ibm01144", "ibm01144" };
            yield return new object[] { 1144, "ibm01144", "ccsid01144" };
            yield return new object[] { 1144, "ibm01144", "cp01144" };
            yield return new object[] { 1144, "ibm01144", "ebcdic-it-280+euro" };
            yield return new object[] { 1145, "ibm01145", "ibm01145" };
            yield return new object[] { 1145, "ibm01145", "ccsid01145" };
            yield return new object[] { 1145, "ibm01145", "cp01145" };
            yield return new object[] { 1145, "ibm01145", "ebcdic-es-284+euro" };
            yield return new object[] { 1146, "ibm01146", "ibm01146" };
            yield return new object[] { 1146, "ibm01146", "ccsid01146" };
            yield return new object[] { 1146, "ibm01146", "cp01146" };
            yield return new object[] { 1146, "ibm01146", "ebcdic-gb-285+euro" };
            yield return new object[] { 1147, "ibm01147", "ibm01147" };
            yield return new object[] { 1147, "ibm01147", "ccsid01147" };
            yield return new object[] { 1147, "ibm01147", "cp01147" };
            yield return new object[] { 1147, "ibm01147", "ebcdic-fr-297+euro" };
            yield return new object[] { 1148, "ibm01148", "ibm01148" };
            yield return new object[] { 1148, "ibm01148", "ccsid01148" };
            yield return new object[] { 1148, "ibm01148", "cp01148" };
            yield return new object[] { 1148, "ibm01148", "ebcdic-international-500+euro" };
            yield return new object[] { 1149, "ibm01149", "ibm01149" };
            yield return new object[] { 1149, "ibm01149", "ccsid01149" };
            yield return new object[] { 1149, "ibm01149", "cp01149" };
            yield return new object[] { 1149, "ibm01149", "ebcdic-is-871+euro" };
            yield return new object[] { 1250, "windows-1250", "windows-1250" };
            yield return new object[] { 1250, "windows-1250", "x-cp1250" };
            yield return new object[] { 1251, "windows-1251", "windows-1251" };
            yield return new object[] { 1251, "windows-1251", "x-cp1251" };
            yield return new object[] { 1252, "windows-1252", "windows-1252" };
            yield return new object[] { 1252, "windows-1252", "x-ansi" };
            yield return new object[] { 1253, "windows-1253", "windows-1253" };
            yield return new object[] { 1254, "windows-1254", "windows-1254" };
            yield return new object[] { 1255, "windows-1255", "windows-1255" };
            yield return new object[] { 1256, "windows-1256", "windows-1256" };
            yield return new object[] { 1256, "windows-1256", "cp1256" };
            yield return new object[] { 1257, "windows-1257", "windows-1257" };
            yield return new object[] { 1258, "windows-1258", "windows-1258" };
            yield return new object[] { 1361, "johab", "johab" };
            yield return new object[] { 10000, "macintosh", "macintosh" };
            yield return new object[] { 10001, "x-mac-japanese", "x-mac-japanese" };
            yield return new object[] { 10002, "x-mac-chinesetrad", "x-mac-chinesetrad" };
            yield return new object[] { 10003, "x-mac-korean", "x-mac-korean" };
            yield return new object[] { 10004, "x-mac-arabic", "x-mac-arabic" };
            yield return new object[] { 10005, "x-mac-hebrew", "x-mac-hebrew" };
            yield return new object[] { 10006, "x-mac-greek", "x-mac-greek" };
            yield return new object[] { 10007, "x-mac-cyrillic", "x-mac-cyrillic" };
            yield return new object[] { 10008, "x-mac-chinesesimp", "x-mac-chinesesimp" };
            yield return new object[] { 10010, "x-mac-romanian", "x-mac-romanian" };
            yield return new object[] { 10017, "x-mac-ukrainian", "x-mac-ukrainian" };
            yield return new object[] { 10021, "x-mac-thai", "x-mac-thai" };
            yield return new object[] { 10029, "x-mac-ce", "x-mac-ce" };
            yield return new object[] { 10079, "x-mac-icelandic", "x-mac-icelandic" };
            yield return new object[] { 10081, "x-mac-turkish", "x-mac-turkish" };
            yield return new object[] { 10082, "x-mac-croatian", "x-mac-croatian" };
            yield return new object[] { 20000, "x-chinese-cns", "x-chinese-cns" };
            yield return new object[] { 20001, "x-cp20001", "x-cp20001" };
            yield return new object[] { 20002, "x-chinese-eten", "x-chinese-eten" };
            yield return new object[] { 20003, "x-cp20003", "x-cp20003" };
            yield return new object[] { 20004, "x-cp20004", "x-cp20004" };
            yield return new object[] { 20005, "x-cp20005", "x-cp20005" };
            yield return new object[] { 20105, "x-ia5", "x-ia5" };
            yield return new object[] { 20105, "x-ia5", "irv" };
            yield return new object[] { 20106, "x-ia5-german", "x-ia5-german" };
            yield return new object[] { 20106, "x-ia5-german", "din_66003" };
            yield return new object[] { 20106, "x-ia5-german", "german" };
            yield return new object[] { 20107, "x-ia5-swedish", "x-ia5-swedish" };
            yield return new object[] { 20107, "x-ia5-swedish", "sen_850200_b" };
            yield return new object[] { 20107, "x-ia5-swedish", "swedish" };
            yield return new object[] { 20108, "x-ia5-norwegian", "x-ia5-norwegian" };
            yield return new object[] { 20108, "x-ia5-norwegian", "norwegian" };
            yield return new object[] { 20108, "x-ia5-norwegian", "ns_4551-1" };
            yield return new object[] { 20261, "x-cp20261", "x-cp20261" };
            yield return new object[] { 20269, "x-cp20269", "x-cp20269" };
            yield return new object[] { 20273, "ibm273", "ibm273" };
            yield return new object[] { 20273, "ibm273", "cp273" };
            yield return new object[] { 20273, "ibm273", "csibm273" };
            yield return new object[] { 20277, "ibm277", "ibm277" };
            yield return new object[] { 20277, "ibm277", "csibm277" };
            yield return new object[] { 20277, "ibm277", "ebcdic-cp-dk" };
            yield return new object[] { 20277, "ibm277", "ebcdic-cp-no" };
            yield return new object[] { 20278, "ibm278", "ibm278" };
            yield return new object[] { 20278, "ibm278", "cp278" };
            yield return new object[] { 20278, "ibm278", "csibm278" };
            yield return new object[] { 20278, "ibm278", "ebcdic-cp-fi" };
            yield return new object[] { 20278, "ibm278", "ebcdic-cp-se" };
            yield return new object[] { 20280, "ibm280", "ibm280" };
            yield return new object[] { 20280, "ibm280", "cp280" };
            yield return new object[] { 20280, "ibm280", "csibm280" };
            yield return new object[] { 20280, "ibm280", "ebcdic-cp-it" };
            yield return new object[] { 20284, "ibm284", "ibm284" };
            yield return new object[] { 20284, "ibm284", "cp284" };
            yield return new object[] { 20284, "ibm284", "csibm284" };
            yield return new object[] { 20284, "ibm284", "ebcdic-cp-es" };
            yield return new object[] { 20285, "ibm285", "ibm285" };
            yield return new object[] { 20285, "ibm285", "cp285" };
            yield return new object[] { 20285, "ibm285", "csibm285" };
            yield return new object[] { 20285, "ibm285", "ebcdic-cp-gb" };
            yield return new object[] { 20290, "ibm290", "ibm290" };
            yield return new object[] { 20290, "ibm290", "cp290" };
            yield return new object[] { 20290, "ibm290", "csibm290" };
            yield return new object[] { 20290, "ibm290", "ebcdic-jp-kana" };
            yield return new object[] { 20297, "ibm297", "ibm297" };
            yield return new object[] { 20297, "ibm297", "cp297" };
            yield return new object[] { 20297, "ibm297", "csibm297" };
            yield return new object[] { 20297, "ibm297", "ebcdic-cp-fr" };
            yield return new object[] { 20420, "ibm420", "ibm420" };
            yield return new object[] { 20420, "ibm420", "cp420" };
            yield return new object[] { 20420, "ibm420", "csibm420" };
            yield return new object[] { 20420, "ibm420", "ebcdic-cp-ar1" };
            yield return new object[] { 20423, "ibm423", "ibm423" };
            yield return new object[] { 20423, "ibm423", "cp423" };
            yield return new object[] { 20423, "ibm423", "csibm423" };
            yield return new object[] { 20423, "ibm423", "ebcdic-cp-gr" };
            yield return new object[] { 20424, "ibm424", "ibm424" };
            yield return new object[] { 20424, "ibm424", "cp424" };
            yield return new object[] { 20424, "ibm424", "csibm424" };
            yield return new object[] { 20424, "ibm424", "ebcdic-cp-he" };
            yield return new object[] { 20833, "x-ebcdic-koreanextended", "x-ebcdic-koreanextended" };
            yield return new object[] { 20838, "ibm-thai", "ibm-thai" };
            yield return new object[] { 20838, "ibm-thai", "csibmthai" };
            yield return new object[] { 20866, "koi8-r", "koi8-r" };
            yield return new object[] { 20866, "koi8-r", "cskoi8r" };
            yield return new object[] { 20866, "koi8-r", "koi" };
            yield return new object[] { 20866, "koi8-r", "koi8" };
            yield return new object[] { 20866, "koi8-r", "koi8r" };
            yield return new object[] { 20871, "ibm871", "ibm871" };
            yield return new object[] { 20871, "ibm871", "cp871" };
            yield return new object[] { 20871, "ibm871", "csibm871" };
            yield return new object[] { 20871, "ibm871", "ebcdic-cp-is" };
            yield return new object[] { 20880, "ibm880", "ibm880" };
            yield return new object[] { 20880, "ibm880", "cp880" };
            yield return new object[] { 20880, "ibm880", "csibm880" };
            yield return new object[] { 20880, "ibm880", "ebcdic-cyrillic" };
            yield return new object[] { 20905, "ibm905", "ibm905" };
            yield return new object[] { 20905, "ibm905", "cp905" };
            yield return new object[] { 20905, "ibm905", "csibm905" };
            yield return new object[] { 20905, "ibm905", "ebcdic-cp-tr" };
            yield return new object[] { 20924, "ibm00924", "ibm00924" };
            yield return new object[] { 20924, "ibm00924", "ccsid00924" };
            yield return new object[] { 20924, "ibm00924", "cp00924" };
            yield return new object[] { 20924, "ibm00924", "ebcdic-latin9--euro" };
            yield return new object[] { 20932, "euc-jp", "euc-jp" };
            yield return new object[] { 20936, "x-cp20936", "x-cp20936" };
            yield return new object[] { 20949, "x-cp20949", "x-cp20949" };
            yield return new object[] { 21025, "cp1025", "cp1025" };
            yield return new object[] { 21866, "koi8-u", "koi8-u" };
            yield return new object[] { 21866, "koi8-u", "koi8-ru" };
            yield return new object[] { 28592, "iso-8859-2", "iso-8859-2" };
            yield return new object[] { 28592, "iso-8859-2", "csisolatin2" };
            yield return new object[] { 28592, "iso-8859-2", "iso_8859-2" };
            yield return new object[] { 28592, "iso-8859-2", "iso_8859-2:1987" };
            yield return new object[] { 28592, "iso-8859-2", "iso8859-2" };
            yield return new object[] { 28592, "iso-8859-2", "iso-ir-101" };
            yield return new object[] { 28592, "iso-8859-2", "l2" };
            yield return new object[] { 28592, "iso-8859-2", "latin2" };
            yield return new object[] { 28593, "iso-8859-3", "iso-8859-3" };
            yield return new object[] { 28593, "iso-8859-3", "csisolatin3" };
            yield return new object[] { 28593, "iso-8859-3", "iso_8859-3" };
            yield return new object[] { 28593, "iso-8859-3", "iso_8859-3:1988" };
            yield return new object[] { 28593, "iso-8859-3", "iso-ir-109" };
            yield return new object[] { 28593, "iso-8859-3", "l3" };
            yield return new object[] { 28593, "iso-8859-3", "latin3" };
            yield return new object[] { 28594, "iso-8859-4", "iso-8859-4" };
            yield return new object[] { 28594, "iso-8859-4", "csisolatin4" };
            yield return new object[] { 28594, "iso-8859-4", "iso_8859-4" };
            yield return new object[] { 28594, "iso-8859-4", "iso_8859-4:1988" };
            yield return new object[] { 28594, "iso-8859-4", "iso-ir-110" };
            yield return new object[] { 28594, "iso-8859-4", "l4" };
            yield return new object[] { 28594, "iso-8859-4", "latin4" };
            yield return new object[] { 28595, "iso-8859-5", "iso-8859-5" };
            yield return new object[] { 28595, "iso-8859-5", "csisolatincyrillic" };
            yield return new object[] { 28595, "iso-8859-5", "cyrillic" };
            yield return new object[] { 28595, "iso-8859-5", "iso_8859-5" };
            yield return new object[] { 28595, "iso-8859-5", "iso_8859-5:1988" };
            yield return new object[] { 28595, "iso-8859-5", "iso-ir-144" };
            yield return new object[] { 28596, "iso-8859-6", "iso-8859-6" };
            yield return new object[] { 28596, "iso-8859-6", "arabic" };
            yield return new object[] { 28596, "iso-8859-6", "csisolatinarabic" };
            yield return new object[] { 28596, "iso-8859-6", "ecma-114" };
            yield return new object[] { 28596, "iso-8859-6", "iso_8859-6" };
            yield return new object[] { 28596, "iso-8859-6", "iso_8859-6:1987" };
            yield return new object[] { 28596, "iso-8859-6", "iso-ir-127" };
            yield return new object[] { 28597, "iso-8859-7", "iso-8859-7" };
            yield return new object[] { 28597, "iso-8859-7", "csisolatingreek" };
            yield return new object[] { 28597, "iso-8859-7", "ecma-118" };
            yield return new object[] { 28597, "iso-8859-7", "elot_928" };
            yield return new object[] { 28597, "iso-8859-7", "greek" };
            yield return new object[] { 28597, "iso-8859-7", "greek8" };
            yield return new object[] { 28597, "iso-8859-7", "iso_8859-7" };
            yield return new object[] { 28597, "iso-8859-7", "iso_8859-7:1987" };
            yield return new object[] { 28597, "iso-8859-7", "iso-ir-126" };
            yield return new object[] { 28598, "iso-8859-8", "iso-8859-8" };
            yield return new object[] { 28598, "iso-8859-8", "csisolatinhebrew" };
            yield return new object[] { 28598, "iso-8859-8", "hebrew" };
            yield return new object[] { 28598, "iso-8859-8", "iso_8859-8" };
            yield return new object[] { 28598, "iso-8859-8", "iso_8859-8:1988" };
            yield return new object[] { 28598, "iso-8859-8", "iso-8859-8 visual" };
            yield return new object[] { 28598, "iso-8859-8", "iso-ir-138" };
            yield return new object[] { 28598, "iso-8859-8", "logical" };
            yield return new object[] { 28598, "iso-8859-8", "visual" };
            yield return new object[] { 28599, "iso-8859-9", "iso-8859-9" };
            yield return new object[] { 28599, "iso-8859-9", "csisolatin5" };
            yield return new object[] { 28599, "iso-8859-9", "iso_8859-9" };
            yield return new object[] { 28599, "iso-8859-9", "iso_8859-9:1989" };
            yield return new object[] { 28599, "iso-8859-9", "iso-ir-148" };
            yield return new object[] { 28599, "iso-8859-9", "l5" };
            yield return new object[] { 28599, "iso-8859-9", "latin5" };
            yield return new object[] { 28603, "iso-8859-13", "iso-8859-13" };
            yield return new object[] { 28605, "iso-8859-15", "iso-8859-15" };
            yield return new object[] { 28605, "iso-8859-15", "csisolatin9" };
            yield return new object[] { 28605, "iso-8859-15", "iso_8859-15" };
            yield return new object[] { 28605, "iso-8859-15", "l9" };
            yield return new object[] { 28605, "iso-8859-15", "latin9" };
            yield return new object[] { 29001, "x-europa", "x-europa" };
            yield return new object[] { 38598, "iso-8859-8-i", "iso-8859-8-i" };
            yield return new object[] { 50220, "iso-2022-jp", "iso-2022-jp" };
            yield return new object[] { 50221, "csiso2022jp", "csiso2022jp" };
            yield return new object[] { 50222, "iso-2022-jp", "iso-2022-jp" };
            yield return new object[] { 50225, "iso-2022-kr", "iso-2022-kr" };
            yield return new object[] { 50225, "iso-2022-kr", "csiso2022kr" };
            yield return new object[] { 50225, "iso-2022-kr", "iso-2022-kr-7" };
            yield return new object[] { 50225, "iso-2022-kr", "iso-2022-kr-7bit" };
            yield return new object[] { 50227, "x-cp50227", "x-cp50227" };
            yield return new object[] { 50227, "x-cp50227", "cp50227" };
            yield return new object[] { 51932, "euc-jp", "euc-jp" };
            yield return new object[] { 51932, "euc-jp", "cseucpkdfmtjapanese" };
            yield return new object[] { 51932, "euc-jp", "extended_unix_code_packed_format_for_japanese" };
            yield return new object[] { 51932, "euc-jp", "iso-2022-jpeuc" };
            yield return new object[] { 51932, "euc-jp", "x-euc" };
            yield return new object[] { 51932, "euc-jp", "x-euc-jp" };
            yield return new object[] { 51936, "euc-cn", "euc-cn" };
            yield return new object[] { 51936, "euc-cn", "x-euc-cn" };
            yield return new object[] { 51949, "euc-kr", "euc-kr" };
            yield return new object[] { 51949, "euc-kr", "cseuckr" };
            yield return new object[] { 51949, "euc-kr", "iso-2022-kr-8" };
            yield return new object[] { 51949, "euc-kr", "iso-2022-kr-8bit" };
            yield return new object[] { 52936, "hz-gb-2312", "hz-gb-2312" };
            yield return new object[] { 54936, "gb18030", "gb18030" };
            yield return new object[] { 57002, "x-iscii-de", "x-iscii-de" };
            yield return new object[] { 57003, "x-iscii-be", "x-iscii-be" };
            yield return new object[] { 57004, "x-iscii-ta", "x-iscii-ta" };
            yield return new object[] { 57005, "x-iscii-te", "x-iscii-te" };
            yield return new object[] { 57006, "x-iscii-as", "x-iscii-as" };
            yield return new object[] { 57007, "x-iscii-or", "x-iscii-or" };
            yield return new object[] { 57008, "x-iscii-ka", "x-iscii-ka" };
            yield return new object[] { 57009, "x-iscii-ma", "x-iscii-ma" };
            yield return new object[] { 57010, "x-iscii-gu", "x-iscii-gu" };
            yield return new object[] { 57011, "x-iscii-pa", "x-iscii-pa" };
        }

        public static IEnumerable<object[]> SpecificCodepageEncodings()
        {
            // Layout is codepage encoding, bytes, and matching unicode string.
            yield return new object[] { "Windows-1256", new byte[] { 0xC7, 0xE1, 0xE1, 0xE5, 0x20, 0xC7, 0xCD, 0xCF }, "\x0627\x0644\x0644\x0647\x0020\x0627\x062D\x062F" };
            yield return new object[] {"Windows-1252", new byte[] { 0xD0, 0xD1, 0xD2, 0xD3, 0xD4, 0xD5, 0xD6, 0xD7, 0xD8, 0xD9, 0xDA, 0xDB, 0xDC, 0xDD, 0xDE, 0xDF } ,
                    "\x00D0\x00D1\x00D2\x00D3\x00D4\x00D5\x00D6\x00D7\x00D8\x00D9\x00DA\x00DB\x00DC\x00DD\x00DE\x00DF"};
            yield return new object[] { "GB2312", new byte[] { 0xCD, 0xE2, 0xCD, 0xE3, 0xCD, 0xE4 }, "\x5916\x8C4C\x5F2F" };
            yield return new object[] {"GB18030", new byte[] { 0x81, 0x30, 0x89, 0x37, 0x81, 0x30, 0x89, 0x38, 0xA8, 0xA4, 0xA8, 0xA2, 0x81, 0x30, 0x89, 0x39, 0x81, 0x30, 0x8A, 0x30 } ,
                    "\x00DE\x00DF\x00E0\x00E1\x00E2\x00E3"};
        }

        public static IEnumerable<object[]> MultibyteCharacterEncodings()
        {
            // Layout is the encoding, bytes, and expected result.
            yield return new object[] { "iso-2022-jp",
                new byte[] { 0xA,
                    0x1B, 0x24, 0x42, 0x25, 0x4A, 0x25, 0x4A,
                    0x1B, 0x28, 0x42,
                    0x1B, 0x24, 0x42, 0x25, 0x4A,
                    0x1B, 0x28, 0x42,
                    0x1B, 0x24, 0x42, 0x25, 0x4A,
                    0x1B, 0x28, 0x42,
                    0x1B, 0x1, 0x2, 0x3, 0x4,
                    0x1B, 0x24, 0x42, 0x25, 0x4A, 0x0E, 0x25, 0x4A,
                    0x1B, 0x28, 0x42, 0x41, 0x42, 0x0E, 0x25, 0x0F, 0x43 },
                new int[] { 0xA, 0x30CA, 0x30CA, 0x30CA, 0x30CA, 0x1B, 0x1, 0x2, 0x3, 0x4,
                    0x30CA, 0xFF65, 0xFF8A, 0x41, 0x42, 0xFF65, 0x43}
            };

            yield return new object[] { "GB18030",
                new byte[] { 0x41, 0x42, 0x43, 0x81, 0x40, 0x82, 0x80, 0x81, 0x30, 0x82, 0x31, 0x81, 0x20 },
                 new int[] { 0x41, 0x42, 0x43, 0x4E02, 0x500B, 0x8B, 0x3F, 0x20 }
            };

            yield return new object[] { "shift_jis",
                new byte[] { 0x41, 0x42, 0x43, 0x81, 0x42, 0xE0, 0x43, 0x44, 0x45 },
                new int[] { 0x41, 0x42, 0x43, 0x3002, 0x6F86, 0x44, 0x45 }
            };

            yield return new object[] { "iso-2022-kr",
                new byte[] { 0x0E, 0x21, 0x7E, 0x1B, 0x24, 0x29, 0x43, 0x21, 0x7E, 0x0F, 0x21, 0x7E, 0x1B, 0x24, 0x29, 0x43, 0x21, 0x7E },
                new int[] { 0xFFE2, 0xFFE2, 0x21, 0x7E, 0x21, 0x7E }
            };

            yield return new object[] { "hz-gb-2312",
                new byte[] { 0x7E, 0x42, 0x7E, 0x7E, 0x7E, 0x7B, 0x21, 0x7E, 0x7E, 0x7D, 0x42, 0x42, 0x7E, 0xA, 0x43, 0x43 },
                new int[] { 0x7E, 0x42, 0x7E, 0x3013, 0x42, 0x42, 0x43, 0x43, }
            };
        }

        private static IEnumerable<KeyValuePair<int, string>> CrossplatformDefaultEncodings()
        {
            yield return Map(1200, "utf-16");
            yield return Map(12000, "utf-32");
            yield return Map(20127, "us-ascii");
            yield return Map(65000, "utf-7");
            yield return Map(65001, "utf-8");
        }
 
        private static KeyValuePair<int, string> Map(int codePage, string webName)
        {
            return new KeyValuePair<int, string>(codePage, webName);
        }

        [Fact]
        public static void TestDefaultEncodings()
        {
            ValidateDefaultEncodings();

            // The default encoding should be something from the known list.
            Encoding defaultEncoding = Encoding.GetEncoding(0);
            Assert.NotNull(defaultEncoding);
            KeyValuePair<int, string> mappedEncoding = Map(defaultEncoding.CodePage, defaultEncoding.WebName);

            if (defaultEncoding.CodePage == Encoding.UTF8.CodePage)
            {
                // if the default encoding is not UTF8 that means either we are running on the full framework
                // or the encoding provider is registered throw the call Encoding.RegisterProvider. 
                // at that time we shouldn't expect exceptions when creating the following encodings.
                foreach (object[] mapping in CodePageInfo())
                {
                    Assert.Throws<NotSupportedException>(() => Encoding.GetEncoding((int)mapping[0]));
                    AssertExtensions.Throws<ArgumentException>("name", () => Encoding.GetEncoding((string)mapping[2]));
                }

                // Currently the class EncodingInfo isn't present in corefx, so this checks none of the code pages are present.
                // When it is, comment out this line and remove the previous foreach/assert.
                // Assert.Equal(CrossplatformDefaultEncodings, Encoding.GetEncodings().OrderBy(i => i.CodePage).Select(i => Map(i.CodePage, i.WebName)));

                Assert.Contains(mappedEncoding, CrossplatformDefaultEncodings());
            }

            // Add the code page provider.
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            // Make sure added code pages are identical between the provider and the Encoding class.
            foreach (object[] mapping in CodePageInfo())
            {
                Encoding encoding = Encoding.GetEncoding((int)mapping[0]);

                Encoding codePageEncoding = CodePagesEncodingProvider.Instance.GetEncoding((int)mapping[0]);
                Assert.Equal(encoding, codePageEncoding);
                Assert.Equal(encoding.CodePage, (int)mapping[0]);
                Assert.Equal(encoding.WebName, (string)mapping[1]);

                // Get encoding via query string.
                Assert.Equal(Encoding.GetEncoding((string)mapping[2]), CodePagesEncodingProvider.Instance.GetEncoding((string)mapping[2]));
            }
            // Adding the code page provider should keep the originals, too.
            ValidateDefaultEncodings();
            // Currently the class EncodingInfo isn't present in corefx, so this checks the complete list
            // When it is, comment out this line and remove the previous foreach/assert.
            // Assert.Equal(CrossplatformDefaultEncodings().Union(CodePageInfo().Select(i => Map((int)i[0], (string)i[1])).OrderBy(i => i.Key)),
            //               Encoding.GetEncodings().OrderBy(i => i.CodePage).Select(i => Map(i.CodePage, i.WebName)));

            // Default encoding may have changed, should still be something on the combined list.
            defaultEncoding = Encoding.GetEncoding(0);
            Assert.NotNull(defaultEncoding);
            mappedEncoding = Map(defaultEncoding.CodePage, defaultEncoding.WebName);
            Assert.Contains(mappedEncoding, CrossplatformDefaultEncodings().Union(CodePageInfo().Select(i => Map((int)i[0], (string)i[1]))));

            TestRegister1252();
        }

        private static void ValidateDefaultEncodings()
        {
            foreach (var mapping in CrossplatformDefaultEncodings())
            {
                Encoding encoding = Encoding.GetEncoding(mapping.Key);
                Assert.NotNull(encoding);
                Assert.Equal(encoding, Encoding.GetEncoding(mapping.Value));
                Assert.Equal(mapping.Value, encoding.WebName);
            }
        }

        [Theory]
        [MemberData(nameof(SpecificCodepageEncodings))]
        public static void TestRoundtrippingSpecificCodepageEncoding(string encodingName, byte[] bytes, string expected)
        {
            Encoding encoding = CodePagesEncodingProvider.Instance.GetEncoding(encodingName);
            string encoded = encoding.GetString(bytes, 0, bytes.Length);
            Assert.Equal(expected, encoded);
            Assert.Equal(bytes, encoding.GetBytes(encoded));
            byte[] resultBytes = encoding.GetBytes(encoded);
        }

        [Theory]
        [MemberData(nameof(CodePageInfo))]
        public static void TestCodepageEncoding(int codePage, string webName, string queryString)
        {
            Encoding encoding;
            // There are two names that have duplicate associated CodePages. For those two names,
            // we have to test with the expectation that querying the name will always return the
            // same codepage.
            if (codePage != 20932 && codePage != 50222)
            {
                encoding = CodePagesEncodingProvider.Instance.GetEncoding(queryString);
                Assert.Equal(encoding, CodePagesEncodingProvider.Instance.GetEncoding(codePage));
                Assert.Equal(encoding, CodePagesEncodingProvider.Instance.GetEncoding(webName));
            }
            else
            {
                encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage);
                Assert.NotEqual(encoding, CodePagesEncodingProvider.Instance.GetEncoding(queryString));
                Assert.NotEqual(encoding, CodePagesEncodingProvider.Instance.GetEncoding(webName));
            }

            Assert.NotNull(encoding);
            Assert.Equal(codePage, encoding.CodePage);
            Assert.Equal(webName, encoding.WebName);

            // Small round-trip for ASCII alphanumeric range (some code pages use different punctuation!)
            // Start with space.
            string asciiPrintable = " 0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";
            char[] traveled = encoding.GetChars(encoding.GetBytes(asciiPrintable));
            Assert.Equal(asciiPrintable.ToCharArray(), traveled);
        }

        [Theory]
        [MemberData(nameof(MultibyteCharacterEncodings))]
        public static void TestSpecificMultibyteCharacterEncodings(string codepageName, byte[] bytes, int[] expected)
        {
            Decoder decoder = CodePagesEncodingProvider.Instance.GetEncoding(codepageName).GetDecoder();
            char[] buffer = new char[expected.Length];

            for (int byteIndex = 0, charIndex = 0, charCount = 0; byteIndex < bytes.Length; byteIndex++, charIndex += charCount)
            {
                charCount = decoder.GetChars(bytes, byteIndex, 1, buffer, charIndex);
            }

            Assert.Equal(expected, buffer.Select(c => (int)c));
        }

        [Theory]
        [MemberData(nameof(CodePageInfo))]
        public static void TestEncodingDisplayNames(int codePage, string webName, string queryString)
        {
            var encoding = CodePagesEncodingProvider.Instance.GetEncoding(codePage);

            string name = encoding.EncodingName;

            // Names can't be empty, and must be printable characters.
            Assert.False(string.IsNullOrWhiteSpace(name));
            Assert.All(name, c => Assert.True(c >= ' ' && c < '~' + 1, "Name: " + name + " contains character: " + c));
        }

        // This test is run as part of the default mappings test, since it modifies global state which that test
        // depends on.
        public static void TestRegister1252()
        {
            // This test case ensure we can map all 1252 codepage codepoints without any exception.
            string s1252Result = 
            "\u0000\u0001\u0002\u0003\u0004\u0005\u0006\u0007\u0008\u0009\u000a\u000b\u000c\u000d\u000e\u000f" +
            "\u0010\u0011\u0012\u0013\u0014\u0015\u0016\u0017\u0018\u0019\u001a\u001b\u001c\u001d\u001e\u001f" +
            "\u0020\u0021\u0022\u0023\u0024\u0025\u0026\u0027\u0028\u0029\u002a\u002b\u002c\u002d\u002e\u002f" +
            "\u0030\u0031\u0032\u0033\u0034\u0035\u0036\u0037\u0038\u0039\u003a\u003b\u003c\u003d\u003e\u003f" +
            "\u0040\u0041\u0042\u0043\u0044\u0045\u0046\u0047\u0048\u0049\u004a\u004b\u004c\u004d\u004e\u004f" +
            "\u0050\u0051\u0052\u0053\u0054\u0055\u0056\u0057\u0058\u0059\u005a\u005b\u005c\u005d\u005e\u005f" +
            "\u0060\u0061\u0062\u0063\u0064\u0065\u0066\u0067\u0068\u0069\u006a\u006b\u006c\u006d\u006e\u006f" +
            "\u0070\u0071\u0072\u0073\u0074\u0075\u0076\u0077\u0078\u0079\u007a\u007b\u007c\u007d\u007e\u007f" +
            "\u20ac\u0081\u201a\u0192\u201e\u2026\u2020\u2021\u02c6\u2030\u0160\u2039\u0152\u008d\u017d\u008f" +
            "\u0090\u2018\u2019\u201c\u201d\u2022\u2013\u2014\u02dc\u2122\u0161\u203a\u0153\u009d\u017e\u0178" +
            "\u00a0\u00a1\u00a2\u00a3\u00a4\u00a5\u00a6\u00a7\u00a8\u00a9\u00aa\u00ab\u00ac\u00ad\u00ae\u00af" +
            "\u00b0\u00b1\u00b2\u00b3\u00b4\u00b5\u00b6\u00b7\u00b8\u00b9\u00ba\u00bb\u00bc\u00bd\u00be\u00bf" +
            "\u00c0\u00c1\u00c2\u00c3\u00c4\u00c5\u00c6\u00c7\u00c8\u00c9\u00ca\u00cb\u00cc\u00cd\u00ce\u00cf" +
            "\u00d0\u00d1\u00d2\u00d3\u00d4\u00d5\u00d6\u00d7\u00d8\u00d9\u00da\u00db\u00dc\u00dd\u00de\u00df" +
            "\u00e0\u00e1\u00e2\u00e3\u00e4\u00e5\u00e6\u00e7\u00e8\u00e9\u00ea\u00eb\u00ec\u00ed\u00ee\u00ef" +
            "\u00f0\u00f1\u00f2\u00f3\u00f4\u00f5\u00f6\u00f7\u00f8\u00f9\u00fa\u00fb\u00fc\u00fd\u00fe\u00ff";

            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
            Encoding win1252 = Encoding.GetEncoding("windows-1252", EncoderFallback.ExceptionFallback, DecoderFallback.ExceptionFallback);
            byte[] enc = new byte[256];
            for (int j = 0; j < 256; j++)
            {
                enc[j] = (byte)j;
            }

            Assert.Equal(s1252Result, win1252.GetString(enc));
        }

    }

    public class CultureSetup : IDisposable
    {
        private readonly CultureInfo _originalUICulture;

        public CultureSetup()
        {
            _originalUICulture = CultureInfo.CurrentUICulture;
            CultureInfo.CurrentUICulture = new CultureInfo("en-US");
        }

        public void Dispose()
        {
            CultureInfo.CurrentUICulture = _originalUICulture;
        }
    }
}
