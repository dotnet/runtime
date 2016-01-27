// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#include "common.h"

#include <mlang.h>

#include "nlsinfo.h"

//
// Encoding data tables
//

//
// Index an encoding name into an codepage in CodePageDataTable.
//
// Please KEEP this table SORTED ALPHABETICALLY! We do a binary search on this array.
const EncodingDataItem COMNlsInfo::EncodingDataTable[] = {
#ifdef FEATURE_CORECLR
    // encoding name, codepage.
    {"ANSI_X3.4-1968", 20127 },
    {"ANSI_X3.4-1986", 20127 },
    {"ascii", 20127 },
    {"cp367", 20127 },
    {"cp819", 28591 },
    {"csASCII", 20127 },
    {"csISOLatin1", 28591 },
    {"csUnicode11UTF7", 65000 },
    {"IBM367", 20127 },
    {"ibm819", 28591 },
    {"ISO-10646-UCS-2", 1200 },
    {"iso-8859-1", 28591 },
    {"iso-ir-100", 28591 },
    {"iso-ir-6", 20127 },
    {"ISO646-US", 20127 },
    {"iso8859-1", 28591 },
    {"ISO_646.irv:1991", 20127 },
    {"iso_8859-1", 28591 },
    {"iso_8859-1:1987", 28591 },
    {"l1", 28591 },
    {"latin1", 28591 },
    {"ucs-2", 1200 },
    {"unicode", 1200}, 
    {"unicode-1-1-utf-7", 65000 },
    {"unicode-1-1-utf-8", 65001 },
    {"unicode-2-0-utf-7", 65000 },
    {"unicode-2-0-utf-8", 65001 },
    // People get confused about the FFFE here.  We can't change this because it'd break existing apps.
    // This has been this way for a long time, including in Mlang.
    {"unicodeFFFE", 1201},             // Big Endian, BOM seems backwards, think of the BOM in little endian order.
    {"us", 20127 },
    {"us-ascii", 20127 },
    {"utf-16", 1200 },
    {"UTF-16BE", 1201}, 
    {"UTF-16LE", 1200},        
    {"utf-32", 12000 },
    {"UTF-32BE", 12001 },
    {"UTF-32LE", 12000 },
    {"utf-7", 65000 },
    {"utf-8", 65001 },
    {"x-unicode-1-1-utf-7", 65000 },
    {"x-unicode-1-1-utf-8", 65001 },
    {"x-unicode-2-0-utf-7", 65000 },
    {"x-unicode-2-0-utf-8", 65001 },
#else
 // Total Items: 455
// encoding name, codepage.
{"437", 437}, 
{"ANSI_X3.4-1968", 20127}, 
{"ANSI_X3.4-1986", 20127}, 
// {L"_autodetect", 50932}, 
// {L"_autodetect_all", 50001}, 
// {L"_autodetect_kr", 50949}, 
{"arabic", 28596}, 
{"ascii", 20127}, 
{"ASMO-708", 708}, 
{"Big5", 950}, 
{"Big5-HKSCS", 950}, 
{"CCSID00858", 858}, 
{"CCSID00924", 20924}, 
{"CCSID01140", 1140}, 
{"CCSID01141", 1141}, 
{"CCSID01142", 1142}, 
{"CCSID01143", 1143}, 
{"CCSID01144", 1144}, 
{"CCSID01145", 1145}, 
{"CCSID01146", 1146}, 
{"CCSID01147", 1147}, 
{"CCSID01148", 1148}, 
{"CCSID01149", 1149}, 
{"chinese", 936}, 
{"cn-big5", 950}, 
{"CN-GB", 936}, 
{"CP00858", 858}, 
{"CP00924", 20924}, 
{"CP01140", 1140}, 
{"CP01141", 1141}, 
{"CP01142", 1142}, 
{"CP01143", 1143}, 
{"CP01144", 1144}, 
{"CP01145", 1145}, 
{"CP01146", 1146}, 
{"CP01147", 1147}, 
{"CP01148", 1148}, 
{"CP01149", 1149}, 
{"cp037", 37}, 
{"cp1025", 21025}, 
{"CP1026", 1026}, 
{"cp1256", 1256}, 
{"CP273", 20273}, 
{"CP278", 20278}, 
{"CP280", 20280}, 
{"CP284", 20284}, 
{"CP285", 20285}, 
{"cp290", 20290}, 
{"cp297", 20297}, 
{"cp367", 20127}, 
{"cp420", 20420}, 
{"cp423", 20423}, 
{"cp424", 20424}, 
{"cp437", 437}, 
{"CP500", 500}, 
{"cp50227", 50227}, 
    //{L"cp50229", 50229}, 
{"cp819", 28591}, 
{"cp850", 850}, 
{"cp852", 852}, 
{"cp855", 855}, 
{"cp857", 857}, 
{"cp858", 858}, 
{"cp860", 860}, 
{"cp861", 861}, 
{"cp862", 862}, 
{"cp863", 863}, 
{"cp864", 864}, 
{"cp865", 865}, 
{"cp866", 866}, 
{"cp869", 869}, 
{"CP870", 870}, 
{"CP871", 20871}, 
{"cp875", 875}, 
{"cp880", 20880}, 
{"CP905", 20905}, 
//{L"cp930", 50930}, 
//{L"cp933", 50933}, 
//{L"cp935", 50935}, 
//{L"cp937", 50937}, 
//{L"cp939", 50939}, 
{"csASCII", 20127}, 
{"csbig5", 950}, 
{"csEUCKR", 51949}, 
{"csEUCPkdFmtJapanese", 51932}, 
{"csGB2312", 936}, 
{"csGB231280", 936}, 
{"csIBM037", 37}, 
{"csIBM1026", 1026}, 
{"csIBM273", 20273}, 
{"csIBM277", 20277}, 
{"csIBM278", 20278}, 
{"csIBM280", 20280}, 
{"csIBM284", 20284}, 
{"csIBM285", 20285}, 
{"csIBM290", 20290}, 
{"csIBM297", 20297}, 
{"csIBM420", 20420}, 
{"csIBM423", 20423}, 
{"csIBM424", 20424}, 
{"csIBM500", 500}, 
{"csIBM870", 870}, 
{"csIBM871", 20871}, 
{"csIBM880", 20880}, 
{"csIBM905", 20905}, 
{"csIBMThai", 20838}, 
{"csISO2022JP", 50221}, 
{"csISO2022KR", 50225}, 
{"csISO58GB231280", 936}, 
{"csISOLatin1", 28591}, 
{"csISOLatin2", 28592}, 
{"csISOLatin3", 28593}, 
{"csISOLatin4", 28594}, 
{"csISOLatin5", 28599}, 
{"csISOLatin9", 28605}, 
{"csISOLatinArabic", 28596}, 
{"csISOLatinCyrillic", 28595}, 
{"csISOLatinGreek", 28597}, 
{"csISOLatinHebrew", 28598}, 
{"csKOI8R", 20866}, 
{"csKSC56011987", 949}, 
{"csPC8CodePage437", 437}, 
{"csShiftJIS", 932}, 
{"csUnicode11UTF7", 65000}, 
{"csWindows31J", 932}, 
{"cyrillic", 28595}, 
{"DIN_66003", 20106}, 
{"DOS-720", 720}, 
{"DOS-862", 862}, 
{"DOS-874", 874}, 
{"ebcdic-cp-ar1", 20420}, 
{"ebcdic-cp-be", 500}, 
{"ebcdic-cp-ca", 37}, 
{"ebcdic-cp-ch", 500}, 
{"EBCDIC-CP-DK", 20277}, 
{"ebcdic-cp-es", 20284}, 
{"ebcdic-cp-fi", 20278}, 
{"ebcdic-cp-fr", 20297}, 
{"ebcdic-cp-gb", 20285}, 
{"ebcdic-cp-gr", 20423}, 
{"ebcdic-cp-he", 20424}, 
{"ebcdic-cp-is", 20871}, 
{"ebcdic-cp-it", 20280}, 
{"ebcdic-cp-nl", 37}, 
{"EBCDIC-CP-NO", 20277}, 
{"ebcdic-cp-roece", 870}, 
{"ebcdic-cp-se", 20278}, 
{"ebcdic-cp-tr", 20905}, 
{"ebcdic-cp-us", 37}, 
{"ebcdic-cp-wt", 37}, 
{"ebcdic-cp-yu", 870}, 
{"EBCDIC-Cyrillic", 20880}, 
{"ebcdic-de-273+euro", 1141}, 
{"ebcdic-dk-277+euro", 1142}, 
{"ebcdic-es-284+euro", 1145}, 
{"ebcdic-fi-278+euro", 1143}, 
{"ebcdic-fr-297+euro", 1147}, 
{"ebcdic-gb-285+euro", 1146}, 
{"ebcdic-international-500+euro", 1148}, 
{"ebcdic-is-871+euro", 1149}, 
{"ebcdic-it-280+euro", 1144}, 
{"EBCDIC-JP-kana", 20290}, 
{"ebcdic-Latin9--euro", 20924}, 
{"ebcdic-no-277+euro", 1142}, 
{"ebcdic-se-278+euro", 1143}, 
{"ebcdic-us-37+euro", 1140}, 
{"ECMA-114", 28596}, 
{"ECMA-118", 28597}, 
{"ELOT_928", 28597}, 
{"euc-cn", 51936}, 
{"euc-jp", 51932}, 
{"euc-kr", 51949}, 
{"Extended_UNIX_Code_Packed_Format_for_Japanese", 51932}, 
{"GB18030", 54936}, 
{"GB2312", 936}, 
{"GB2312-80", 936}, 
{"GB231280", 936}, 
{"GBK", 936}, 
{"GB_2312-80", 936}, 
{"German", 20106}, 
{"greek", 28597}, 
{"greek8", 28597}, 
{"hebrew", 28598}, 
{"hz-gb-2312", 52936}, 
{"IBM-Thai", 20838}, 
{"IBM00858", 858}, 
{"IBM00924", 20924}, 
{"IBM01047", 1047}, 
{"IBM01140", 1140}, 
{"IBM01141", 1141}, 
{"IBM01142", 1142}, 
{"IBM01143", 1143}, 
{"IBM01144", 1144}, 
{"IBM01145", 1145}, 
{"IBM01146", 1146}, 
{"IBM01147", 1147}, 
{"IBM01148", 1148}, 
{"IBM01149", 1149}, 
{"IBM037", 37}, 
{"IBM1026", 1026}, 
{"IBM273", 20273}, 
{"IBM277", 20277}, 
{"IBM278", 20278}, 
{"IBM280", 20280}, 
{"IBM284", 20284}, 
{"IBM285", 20285}, 
{"IBM290", 20290}, 
{"IBM297", 20297}, 
{"IBM367", 20127}, 
{"IBM420", 20420}, 
{"IBM423", 20423}, 
{"IBM424", 20424}, 
{"IBM437", 437}, 
{"IBM500", 500}, 
{"ibm737", 737}, 
{"ibm775", 775}, 
{"ibm819", 28591}, 
{"IBM850", 850}, 
{"IBM852", 852}, 
{"IBM855", 855}, 
{"IBM857", 857}, 
{"IBM860", 860}, 
{"IBM861", 861}, 
{"IBM862", 862}, 
{"IBM863", 863}, 
{"IBM864", 864}, 
{"IBM865", 865}, 
{"IBM866", 866}, 
{"IBM869", 869}, 
{"IBM870", 870}, 
{"IBM871", 20871}, 
{"IBM880", 20880}, 
{"IBM905", 20905}, 
{"irv", 20105}, 
{"ISO-10646-UCS-2", 1200}, 
{"iso-2022-jp", 50220}, 
{"iso-2022-jpeuc", 51932}, 
{"iso-2022-kr", 50225}, 
{"iso-2022-kr-7", 50225}, 
{"iso-2022-kr-7bit", 50225}, 
{"iso-2022-kr-8", 51949}, 
{"iso-2022-kr-8bit", 51949}, 
{"iso-8859-1", 28591}, 
{"iso-8859-11", 874}, 
{"iso-8859-13", 28603}, 
{"iso-8859-15", 28605}, 
{"iso-8859-2", 28592}, 
{"iso-8859-3", 28593}, 
{"iso-8859-4", 28594}, 
{"iso-8859-5", 28595}, 
{"iso-8859-6", 28596}, 
{"iso-8859-7", 28597}, 
{"iso-8859-8", 28598}, 
{"ISO-8859-8 Visual", 28598}, 
{"iso-8859-8-i", 38598}, 
{"iso-8859-9", 28599}, 
{"iso-ir-100", 28591}, 
{"iso-ir-101", 28592}, 
{"iso-ir-109", 28593}, 
{"iso-ir-110", 28594}, 
{"iso-ir-126", 28597}, 
{"iso-ir-127", 28596}, 
{"iso-ir-138", 28598}, 
{"iso-ir-144", 28595}, 
{"iso-ir-148", 28599}, 
{"iso-ir-149", 949}, 
{"iso-ir-58", 936}, 
{"iso-ir-6", 20127}, 
{"ISO646-US", 20127}, 
{"iso8859-1", 28591}, 
{"iso8859-2", 28592}, 
{"ISO_646.irv:1991", 20127}, 
{"iso_8859-1", 28591}, 
{"ISO_8859-15", 28605}, 
{"iso_8859-1:1987", 28591}, 
{"iso_8859-2", 28592}, 
{"iso_8859-2:1987", 28592}, 
{"ISO_8859-3", 28593}, 
{"ISO_8859-3:1988", 28593}, 
{"ISO_8859-4", 28594}, 
{"ISO_8859-4:1988", 28594}, 
{"ISO_8859-5", 28595}, 
{"ISO_8859-5:1988", 28595}, 
{"ISO_8859-6", 28596}, 
{"ISO_8859-6:1987", 28596}, 
{"ISO_8859-7", 28597}, 
{"ISO_8859-7:1987", 28597}, 
{"ISO_8859-8", 28598}, 
{"ISO_8859-8:1988", 28598}, 
{"ISO_8859-9", 28599}, 
{"ISO_8859-9:1989", 28599}, 
{"Johab", 1361}, 
{"koi", 20866}, 
{"koi8", 20866}, 
{"koi8-r", 20866}, 
{"koi8-ru", 21866}, 
{"koi8-u", 21866}, 
{"koi8r", 20866}, 
{"korean", 949}, 
{"ks-c-5601", 949}, 
{"ks-c5601", 949}, 
{"KSC5601", 949}, 
{"KSC_5601", 949}, 
{"ks_c_5601", 949}, 
{"ks_c_5601-1987", 949}, 
{"ks_c_5601-1989", 949}, 
{"ks_c_5601_1987", 949}, 
{"l1", 28591}, 
{"l2", 28592}, 
{"l3", 28593}, 
{"l4", 28594}, 
{"l5", 28599}, 
{"l9", 28605}, 
{"latin1", 28591}, 
{"latin2", 28592}, 
{"latin3", 28593}, 
{"latin4", 28594}, 
{"latin5", 28599}, 
{"latin9", 28605}, 
{"logical", 28598}, 
{"macintosh", 10000}, 
{"ms_Kanji", 932}, 
{"Norwegian", 20108}, 
{"NS_4551-1", 20108}, 
{"PC-Multilingual-850+euro", 858}, 
{"SEN_850200_B", 20107}, 
{"shift-jis", 932}, 
{"shift_jis", 932}, 
{"sjis", 932}, 
{"Swedish", 20107}, 
{"TIS-620", 874}, 
{"ucs-2", 1200}, 
{"unicode", 1200}, 
{"unicode-1-1-utf-7", 65000}, 
{"unicode-1-1-utf-8", 65001}, 
{"unicode-2-0-utf-7", 65000}, 
{"unicode-2-0-utf-8", 65001}, 
// People get confused about the FFFE here.  We can't change this because it'd break existing apps.
// This has been this way for a long time, including in Mlang.
{"unicodeFFFE", 1201},             // Big Endian, BOM seems backwards, think of the BOM in little endian order.
{"us", 20127}, 
{"us-ascii", 20127}, 
{"utf-16", 1200}, 
{"UTF-16BE", 1201}, 
{"UTF-16LE", 1200},
{"utf-32", 12000},
{"UTF-32BE", 12001},
{"UTF-32LE", 12000},
{"utf-7", 65000}, 
{"utf-8", 65001},
{"visual", 28598}, 
{"windows-1250", 1250}, 
{"windows-1251", 1251}, 
{"windows-1252", 1252}, 
{"windows-1253", 1253}, 
{"Windows-1254", 1254}, 
{"windows-1255", 1255}, 
{"windows-1256", 1256}, 
{"windows-1257", 1257}, 
{"windows-1258", 1258}, 
{"windows-874", 874}, 
{"x-ansi", 1252}, 
{"x-Chinese-CNS", 20000}, 
{"x-Chinese-Eten", 20002}, 
{"x-cp1250", 1250}, 
{"x-cp1251", 1251}, 
{"x-cp20001", 20001}, 
{"x-cp20003", 20003}, 
{"x-cp20004", 20004}, 
{"x-cp20005", 20005}, 
{"x-cp20261", 20261}, 
{"x-cp20269", 20269}, 
{"x-cp20936", 20936}, 
{"x-cp20949", 20949},
{"x-cp50227", 50227}, 
//{L"x-cp50229", 50229}, 
//{L"X-EBCDIC-JapaneseAndUSCanada", 50931}, 
{"X-EBCDIC-KoreanExtended", 20833}, 
{"x-euc", 51932}, 
{"x-euc-cn", 51936}, 
{"x-euc-jp", 51932}, 
{"x-Europa", 29001}, 
{"x-IA5", 20105}, 
{"x-IA5-German", 20106}, 
{"x-IA5-Norwegian", 20108}, 
{"x-IA5-Swedish", 20107}, 
{"x-iscii-as", 57006}, 
{"x-iscii-be", 57003}, 
{"x-iscii-de", 57002}, 
{"x-iscii-gu", 57010}, 
{"x-iscii-ka", 57008}, 
{"x-iscii-ma", 57009}, 
{"x-iscii-or", 57007}, 
{"x-iscii-pa", 57011}, 
{"x-iscii-ta", 57004}, 
{"x-iscii-te", 57005}, 
{"x-mac-arabic", 10004}, 
{"x-mac-ce", 10029}, 
{"x-mac-chinesesimp", 10008}, 
{"x-mac-chinesetrad", 10002}, 
{"x-mac-croatian", 10082}, 
{"x-mac-cyrillic", 10007}, 
{"x-mac-greek", 10006}, 
{"x-mac-hebrew", 10005}, 
{"x-mac-icelandic", 10079}, 
{"x-mac-japanese", 10001}, 
{"x-mac-korean", 10003}, 
{"x-mac-romanian", 10010}, 
{"x-mac-thai", 10021}, 
{"x-mac-turkish", 10081}, 
{"x-mac-ukrainian", 10017}, 
{"x-ms-cp932", 932},
{"x-sjis", 932}, 
{"x-unicode-1-1-utf-7", 65000}, 
{"x-unicode-1-1-utf-8", 65001}, 
{"x-unicode-2-0-utf-7", 65000}, 
{"x-unicode-2-0-utf-8", 65001}, 
{"x-x-big5", 950}, 

#endif // FEATURE_CORECLR
    
};

const int COMNlsInfo::m_nEncodingDataTableItems =
    sizeof(COMNlsInfo::EncodingDataTable)/sizeof(EncodingDataItem);

// Working set optimization: 
// 1. code page, family code page stored as unsigned short
// 2. if web/header/body names are the same, only web name is stored; otherwise, we store "|webname|headername|bodyname"
// 3. Move flags before names to fill gap on 64-bit platforms

#define MapCodePageDataItem(cp, fcp, names, flags)  { cp, fcp, flags, names }
//
// Information about codepages.
//
const CodePageDataItem COMNlsInfo::CodePageDataTable[] = {

#ifdef FEATURE_CORECLR

// Total Items: 
// code page, family code page, web name, header name, body name, flags

    MapCodePageDataItem(  1200,  1200, "utf-16",      MIMECONTF_SAVABLE_BROWSER), // "Unicode"
    MapCodePageDataItem(  1201,  1200, "utf-16BE",    0), // Big Endian, old FFFE BOM seems backwards, think of the BOM in little endian order.
    MapCodePageDataItem(  12000, 1200, "utf-32", 0), // "Unicode (UTF-32)"
    MapCodePageDataItem(  12001, 1200, "utf-32BE", 0), // "Unicode (UTF-32 Big Endian)"
    MapCodePageDataItem(  20127, 1252, "us-ascii", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "US-ASCII"
    MapCodePageDataItem(  28591,  1252, "iso-8859-1",  MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Western European (ISO)"
    MapCodePageDataItem(  65000, 1200, "utf-7", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Unicode (UTF-7)"
    MapCodePageDataItem(  65001, 1200, "utf-8", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Unicode (UTF-8)"

#else //FEATURE_CORECLR

// Total Items: 146
// code page, family code page, web name, header name, body name, flags


    MapCodePageDataItem(    37,  1252, "IBM037",      0), // "IBM EBCDIC (US-Canada)"
    MapCodePageDataItem(   437,  1252, "IBM437",      0), // "OEM United States"
    MapCodePageDataItem(   500,  1252, "IBM500",      0), // "IBM EBCDIC (International)"
    MapCodePageDataItem(   708,  1256, "ASMO-708",    MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Arabic (ASMO 708)"
    MapCodePageDataItem(   720,  1256, "DOS-720",     MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Arabic (DOS)"
    MapCodePageDataItem(   737,  1253, "ibm737",      0), // "Greek (DOS)"
    MapCodePageDataItem(   775,  1257, "ibm775",      0), // "Baltic (DOS)"
    MapCodePageDataItem(   850,  1252, "ibm850",      0), // "Western European (DOS)"
    MapCodePageDataItem(   852,  1250, "ibm852",      MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Central European (DOS)"
    MapCodePageDataItem(   855,  1252, "IBM855",      0), // "OEM Cyrillic"
    MapCodePageDataItem(   857,  1254, "ibm857",      0), // "Turkish (DOS)"
    MapCodePageDataItem(   858,  1252, "IBM00858",    0), // "OEM Multilingual Latin I"
    MapCodePageDataItem(   860,  1252, "IBM860",      0), // "Portuguese (DOS)"
    MapCodePageDataItem(   861,  1252, "ibm861",      0), // "Icelandic (DOS)"
    MapCodePageDataItem(   862,  1255, "DOS-862",     MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Hebrew (DOS)"
    MapCodePageDataItem(   863,  1252, "IBM863",      0), // "French Canadian (DOS)"
    MapCodePageDataItem(   864,  1256, "IBM864",      0), // "Arabic (864)"
    MapCodePageDataItem(   865,  1252, "IBM865",      0), // "Nordic (DOS)"
    MapCodePageDataItem(   866,  1251, "cp866",       MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Cyrillic (DOS)"
    MapCodePageDataItem(   869,  1253, "ibm869",      0), // "Greek, Modern (DOS)"
    MapCodePageDataItem(   870,  1250, "IBM870",      0), // "IBM EBCDIC (Multilingual Latin-2)"
    MapCodePageDataItem(   874,   874, "windows-874", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Thai (Windows)"
    MapCodePageDataItem(   875,  1253, "cp875",       0), // "IBM EBCDIC (Greek Modern)"
    MapCodePageDataItem(   932,   932, "|shift_jis|iso-2022-jp|iso-2022-jp",   MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Japanese (Shift-JIS)"
    MapCodePageDataItem(   936,   936, "gb2312",      MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Chinese Simplified (GB2312)"
    MapCodePageDataItem(   949,   949, "ks_c_5601-1987", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Korean"
    MapCodePageDataItem(   950,   950, "big5",        MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Chinese Traditional (Big5)"
    MapCodePageDataItem(  1026,  1254, "IBM1026",     0), // "IBM EBCDIC (Turkish Latin-5)"
    MapCodePageDataItem(  1047,  1252, "IBM01047",    0), // "IBM Latin-1"
    MapCodePageDataItem(  1140,  1252, "IBM01140",    0), // "IBM EBCDIC (US-Canada-Euro)"
    MapCodePageDataItem(  1141,  1252, "IBM01141",    0), // "IBM EBCDIC (Germany-Euro)"
    MapCodePageDataItem(  1142,  1252, "IBM01142",    0), // "IBM EBCDIC (Denmark-Norway-Euro)"
    MapCodePageDataItem(  1143,  1252, "IBM01143",    0), // "IBM EBCDIC (Finland-Sweden-Euro)"
    MapCodePageDataItem(  1144,  1252, "IBM01144",    0), // "IBM EBCDIC (Italy-Euro)"
    MapCodePageDataItem(  1145,  1252, "IBM01145",    0), // "IBM EBCDIC (Spain-Euro)"
    MapCodePageDataItem(  1146,  1252, "IBM01146",    0), // "IBM EBCDIC (UK-Euro)"
    MapCodePageDataItem(  1147,  1252, "IBM01147",    0), // "IBM EBCDIC (France-Euro)"
    MapCodePageDataItem(  1148,  1252, "IBM01148",    0), // "IBM EBCDIC (International-Euro)"
    MapCodePageDataItem(  1149,  1252, "IBM01149",    0), // "IBM EBCDIC (Icelandic-Euro)"
    MapCodePageDataItem(  1200,  1200, "utf-16",      MIMECONTF_SAVABLE_BROWSER), // "Unicode"
    MapCodePageDataItem(  1201,  1200, "utf-16BE",    0), // Big Endian, old FFFE BOM seems backwards, think of the BOM in little endian order.
    MapCodePageDataItem(  1250,  1250, "|windows-1250|windows-1250|iso-8859-2", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Central European (Windows)"
    MapCodePageDataItem(  1251,  1251, "|windows-1251|windows-1251|koi8-r", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Cyrillic (Windows)"
    MapCodePageDataItem(  1252,  1252, "|Windows-1252|Windows-1252|iso-8859-1", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Western European (Windows)"
    MapCodePageDataItem(  1253,  1253, "|windows-1253|windows-1253|iso-8859-7", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Greek (Windows)"
    MapCodePageDataItem(  1254,  1254, "|windows-1254|windows-1254|iso-8859-9", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Turkish (Windows)"
    MapCodePageDataItem(  1255,  1255, "windows-1255", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Hebrew (Windows)"
    MapCodePageDataItem(  1256,  1256, "windows-1256", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Arabic (Windows)"
    MapCodePageDataItem(  1257,  1257, "windows-1257", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Baltic (Windows)"
    MapCodePageDataItem(  1258,  1258, "windows-1258", MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Vietnamese (Windows)"
    MapCodePageDataItem(  1361,   949, "Johab",        0), // "Korean (Johab)"
    MapCodePageDataItem( 10000,  1252, "macintosh",    0), // "Western European (Mac)"
    MapCodePageDataItem( 10001,   932, "x-mac-japanese", 0), // "Japanese (Mac)"
    MapCodePageDataItem( 10002,   950, "x-mac-chinesetrad",   0), // "Chinese Traditional (Mac)"
    MapCodePageDataItem( 10003,   949, "x-mac-korean",        0), // "Korean (Mac)"
    MapCodePageDataItem( 10004,  1256, "x-mac-arabic",        0), // "Arabic (Mac)"
    MapCodePageDataItem( 10005,  1255, "x-mac-hebrew",        0), // "Hebrew (Mac)"
    MapCodePageDataItem( 10006,  1253, "x-mac-greek",         0), // "Greek (Mac)"
    MapCodePageDataItem( 10007,  1251, "x-mac-cyrillic",      0), // "Cyrillic (Mac)"
    MapCodePageDataItem( 10008,   936, "x-mac-chinesesimp",   0), // "Chinese Simplified (Mac)"
    MapCodePageDataItem( 10010,  1250, "x-mac-romanian",      0), // "Romanian (Mac)"
    MapCodePageDataItem( 10017,  1251, "x-mac-ukrainian",     0), // "Ukrainian (Mac)"
    MapCodePageDataItem( 10021,   874, "x-mac-thai",          0), // "Thai (Mac)"
    MapCodePageDataItem( 10029,  1250, "x-mac-ce",            0), // "Central European (Mac)"
    MapCodePageDataItem( 10079,  1252, "x-mac-icelandic",     0), // "Icelandic (Mac)"
    MapCodePageDataItem( 10081,  1254, "x-mac-turkish",       0), // "Turkish (Mac)"
    MapCodePageDataItem( 10082,  1250, "x-mac-croatian",      0), // "Croatian (Mac)"
    MapCodePageDataItem( 12000,  1200, "utf-32",              0), // "Unicode (UTF-32)"
    MapCodePageDataItem( 12001,  1200, "utf-32BE",            0), // "Unicode (UTF-32 Big Endian)"
    MapCodePageDataItem( 20000,   950, "x-Chinese-CNS",       0), // "Chinese Traditional (CNS)"
    MapCodePageDataItem( 20001,   950, "x-cp20001",           0), // "TCA Taiwan"
    MapCodePageDataItem( 20002,   950, "x-Chinese-Eten",      0), // "Chinese Traditional (Eten)"
    MapCodePageDataItem( 20003,   950, "x-cp20003",           0), // "IBM5550 Taiwan"
    MapCodePageDataItem( 20004,   950, "x-cp20004",           0), // "TeleText Taiwan"
    MapCodePageDataItem( 20005,   950, "x-cp20005",           0), // "Wang Taiwan"
    MapCodePageDataItem( 20105,  1252, "x-IA5",               0), // "Western European (IA5)"
    MapCodePageDataItem( 20106,  1252, "x-IA5-German",        0), // "German (IA5)"
    MapCodePageDataItem( 20107,  1252, "x-IA5-Swedish",       0), // "Swedish (IA5)"
    MapCodePageDataItem( 20108,  1252, "x-IA5-Norwegian",     0), // "Norwegian (IA5)"
    MapCodePageDataItem( 20127,  1252, "us-ascii",            MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "US-ASCII"
    MapCodePageDataItem( 20261,  1252, "x-cp20261",           0), // "T.61"
    MapCodePageDataItem( 20269,  1252, "x-cp20269",           0), // "ISO-6937"
    MapCodePageDataItem( 20273,  1252, "IBM273",              0), // "IBM EBCDIC (Germany)"
    MapCodePageDataItem( 20277,  1252, "IBM277",              0), // "IBM EBCDIC (Denmark-Norway)"
    MapCodePageDataItem( 20278,  1252, "IBM278",              0), // "IBM EBCDIC (Finland-Sweden)"
    MapCodePageDataItem( 20280,  1252, "IBM280",              0), // "IBM EBCDIC (Italy)"
    MapCodePageDataItem( 20284,  1252, "IBM284",              0), // "IBM EBCDIC (Spain)"
    MapCodePageDataItem( 20285,  1252, "IBM285",              0), // "IBM EBCDIC (UK)"
    MapCodePageDataItem( 20290,   932, "IBM290",              0), // "IBM EBCDIC (Japanese katakana)"
    MapCodePageDataItem( 20297,  1252, "IBM297",              0), // "IBM EBCDIC (France)"
    MapCodePageDataItem( 20420,  1256, "IBM420",              0), // "IBM EBCDIC (Arabic)"
    MapCodePageDataItem( 20423,  1253, "IBM423",              0), // "IBM EBCDIC (Greek)"
    MapCodePageDataItem( 20424,  1255, "IBM424",              0), // "IBM EBCDIC (Hebrew)"
    MapCodePageDataItem( 20833,   949, "x-EBCDIC-KoreanExtended", 0), // "IBM EBCDIC (Korean Extended)"
    MapCodePageDataItem( 20838,   874, "IBM-Thai",            0), // "IBM EBCDIC (Thai)"
    MapCodePageDataItem( 20866,  1251, "koi8-r",              MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Cyrillic (KOI8-R)"
    MapCodePageDataItem( 20871,  1252, "IBM871",              0), // "IBM EBCDIC (Icelandic)"
    MapCodePageDataItem( 20880,  1251, "IBM880",              0), // "IBM EBCDIC (Cyrillic Russian)"
    MapCodePageDataItem( 20905,  1254, "IBM905",              0), // "IBM EBCDIC (Turkish)"
    MapCodePageDataItem( 20924,  1252, "IBM00924",            0), // "IBM Latin-1"
    MapCodePageDataItem( 20932,   932, "EUC-JP",              0), // "Japanese (JIS 0208-1990 and 0212-1990)"
    MapCodePageDataItem( 20936,   936, "x-cp20936",           0), // "Chinese Simplified (GB2312-80)"
    MapCodePageDataItem( 20949,   949, "x-cp20949",           0), // "Korean Wansung"
    MapCodePageDataItem( 21025,  1251, "cp1025",              0), // "IBM EBCDIC (Cyrillic Serbian-Bulgarian)"
    MapCodePageDataItem( 21866,  1251, "koi8-u",              MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Cyrillic (KOI8-U)"
    MapCodePageDataItem( 28591,  1252, "iso-8859-1",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Western European (ISO)"
    MapCodePageDataItem( 28592,  1250, "iso-8859-2",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Central European (ISO)"
    MapCodePageDataItem( 28593,  1254, "iso-8859-3",          MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Latin 3 (ISO)"
    MapCodePageDataItem( 28594,  1257, "iso-8859-4",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Baltic (ISO)"
    MapCodePageDataItem( 28595,  1251, "iso-8859-5",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Cyrillic (ISO)"
    MapCodePageDataItem( 28596,  1256, "iso-8859-6",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Arabic (ISO)"
    MapCodePageDataItem( 28597,  1253, "iso-8859-7",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Greek (ISO)"
    MapCodePageDataItem( 28598,  1255, "iso-8859-8",          MIMECONTF_BROWSER | MIMECONTF_SAVABLE_BROWSER), // "Hebrew (ISO-Visual)"
    MapCodePageDataItem( 28599,  1254, "iso-8859-9",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Turkish (ISO)"
    MapCodePageDataItem( 28603,  1257, "iso-8859-13",         MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Estonian (ISO)"
    MapCodePageDataItem( 28605,  1252, "iso-8859-15",         MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Latin 9 (ISO)"
    MapCodePageDataItem( 29001,  1252, "x-Europa",            0), // "Europa"
    MapCodePageDataItem( 38598,  1255, "iso-8859-8-i",        MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Hebrew (ISO-Logical)"
    MapCodePageDataItem( 50220,   932, "iso-2022-jp",         MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Japanese (JIS)"
    MapCodePageDataItem( 50221,   932, "|csISO2022JP|iso-2022-jp|iso-2022-jp", MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Japanese (JIS-Allow 1 byte Kana)"
    MapCodePageDataItem( 50222,   932, "iso-2022-jp",         0), // "Japanese (JIS-Allow 1 byte Kana - SO/SI)"
    MapCodePageDataItem( 50225,   949, "|iso-2022-kr|euc-kr|iso-2022-kr", MIMECONTF_MAILNEWS), // "Korean (ISO)"
    MapCodePageDataItem( 50227,   936, "x-cp50227",           0), // "Chinese Simplified (ISO-2022)"
//MapCodePageDataItem( 50229,   950, L"x-cp50229", L"x-cp50229", L"x-cp50229", 0}, // "Chinese Traditional (ISO-2022)"
//MapCodePageDataItem( 50930,   932, L"cp930", L"cp930", L"cp930", 0}, // "IBM EBCDIC (Japanese and Japanese Katakana)"
//MapCodePageDataItem( 50931,   932, L"x-EBCDIC-JapaneseAndUSCanada", L"x-EBCDIC-JapaneseAndUSCanada", L"x-EBCDIC-JapaneseAndUSCanada", 0}, // "IBM EBCDIC (Japanese and US-Canada)"
//MapCodePageDataItem( 50933,   949, L"cp933", L"cp933", L"cp933", 0}, // "IBM EBCDIC (Korean and Korean Extended)"
//MapCodePageDataItem( 50935,   936, L"cp935", L"cp935", L"cp935", 0}, // "IBM EBCDIC (Simplified Chinese)"
//MapCodePageDataItem( 50937,   950, L"cp937", L"cp937", L"cp937", 0}, // "IBM EBCDIC (Traditional Chinese)"
//MapCodePageDataItem( 50939,   932, L"cp939", L"cp939", L"cp939", 0}, // "IBM EBCDIC (Japanese and Japanese-Latin)"
    MapCodePageDataItem( 51932,   932, "euc-jp",              MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Japanese (EUC)"
    MapCodePageDataItem( 51936,   936, "EUC-CN",              0), // "Chinese Simplified (EUC)"
    MapCodePageDataItem( 51949,   949, "euc-kr",              MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Korean (EUC)"
    MapCodePageDataItem( 52936,   936, "hz-gb-2312",          MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Chinese Simplified (HZ)"
    MapCodePageDataItem( 54936,   936, "GB18030",             MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Chinese Simplified (GB18030)"
    MapCodePageDataItem( 57002, 57002, "x-iscii-de",          0), // "ISCII Devanagari"
    MapCodePageDataItem( 57003, 57003, "x-iscii-be",          0), // "ISCII Bengali"
    MapCodePageDataItem( 57004, 57004, "x-iscii-ta",          0), // "ISCII Tamil"
    MapCodePageDataItem( 57005, 57005, "x-iscii-te",          0), // "ISCII Telugu"
    MapCodePageDataItem( 57006, 57006, "x-iscii-as",          0), // "ISCII Assamese"
    MapCodePageDataItem( 57007, 57007, "x-iscii-or",          0), // "ISCII Oriya"
    MapCodePageDataItem( 57008, 57008, "x-iscii-ka",          0), // "ISCII Kannada"
    MapCodePageDataItem( 57009, 57009, "x-iscii-ma",          0), // "ISCII Malayalam"
    MapCodePageDataItem( 57010, 57010, "x-iscii-gu",          0), // "ISCII Gujarati"
    MapCodePageDataItem( 57011, 57011, "x-iscii-pa",          0), // "ISCII Punjabi"
    MapCodePageDataItem( 65000,  1200, "utf-7",               MIMECONTF_MAILNEWS | MIMECONTF_SAVABLE_MAILNEWS), // "Unicode (UTF-7)"
    MapCodePageDataItem( 65001,  1200, "utf-8",               MIMECONTF_MAILNEWS | MIMECONTF_BROWSER | MIMECONTF_SAVABLE_MAILNEWS | MIMECONTF_SAVABLE_BROWSER), // "Unicode (UTF-8)"
#endif // FEATURE_CORECLR

    // End of data.
    MapCodePageDataItem( 0, 0, NULL, 0),

};

const int COMNlsInfo::m_nCodePageTableItems = 
    sizeof(COMNlsInfo::CodePageDataTable)/sizeof(CodePageDataItem);

