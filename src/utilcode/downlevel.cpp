// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
////////////////////////////////////////////////////////////////////////////
//
//  File:    downlevel.cpp
//


//
//  Purpose:  functions that need to be emulated on downlevel platforms.
//
////////////////////////////////////////////////////////////////////////////

#include "stdafx.h"

#if defined(ENABLE_DOWNLEVEL_FOR_NLS)

#ifndef LOCALE_SNAME
#define LOCALE_SNAME                0x0000005c
#endif
#include "downlevel.h"
#include "newapis.h"
#include "utilcode.h"
#include "sstring.h"
#include "ex.h"

#define LCID_AZ_CYRL_AZ 0x0082c

//
//  Macro to check if more than one bit is set.
//  Returns 1 if more than one bit set, 0 otherwise.
//
#define MORE_THAN_ONE(f, bits)    (((f & bits) - 1) & (f & bits))

struct LCIDEntry
{
    LCID lcid;
    LPCWSTR wszName;
};

// Known (ie: <= Vista) Name/LCID lookup table, sorted by LCID
const static LCIDEntry s_lcids[]=
{
    // Neutrals
    { 0x00001, W("ar") },        // Neutral Locale
    { 0x00002, W("bg") },        // Neutral Locale
    { 0x00003, W("ca") },        // Neutral Locale
    { 0x00004, W("zh-Hans") },   // Neutral Locale
    { 0x00005, W("cs") },        // Neutral Locale
    { 0x00006, W("da") },        // Neutral Locale
    { 0x00007, W("de") },        // Neutral Locale
    { 0x00008, W("el") },        // Neutral Locale
    { 0x00009, W("en") },        // Neutral Locale
    { 0x0000a, W("es") },        // Neutral Locale
    { 0x0000b, W("fi") },        // Neutral Locale
    { 0x0000c, W("fr") },        // Neutral Locale
    { 0x0000d, W("he") },        // Neutral Locale
    { 0x0000e, W("hu") },        // Neutral Locale
    { 0x0000f, W("is") },        // Neutral Locale
    { 0x00010, W("it") },        // Neutral Locale
    { 0x00011, W("ja") },        // Neutral Locale
    { 0x00012, W("ko") },        // Neutral Locale
    { 0x00013, W("nl") },        // Neutral Locale
    { 0x00014, W("no") },        // Neutral Locale
    { 0x00015, W("pl") },        // Neutral Locale
    { 0x00016, W("pt") },        // Neutral Locale
    { 0x00017, W("rm") },        // Neutral Locale
    { 0x00018, W("ro") },        // Neutral Locale
    { 0x00019, W("ru") },        // Neutral Locale
    { 0x0001a, W("hr") },        // Neutral Locale
    { 0x0001b, W("sk") },        // Neutral Locale
    { 0x0001c, W("sq") },        // Neutral Locale
    { 0x0001d, W("sv") },        // Neutral Locale
    { 0x0001e, W("th") },        // Neutral Locale
    { 0x0001f, W("tr") },        // Neutral Locale
    { 0x00020, W("ur") },        // Neutral Locale
    { 0x00021, W("id") },        // Neutral Locale
    { 0x00022, W("uk") },        // Neutral Locale
    { 0x00023, W("be") },        // Neutral Locale
    { 0x00024, W("sl") },        // Neutral Locale
    { 0x00025, W("et") },        // Neutral Locale
    { 0x00026, W("lv") },        // Neutral Locale
    { 0x00027, W("lt") },        // Neutral Locale
    { 0x00028, W("tg") },        // Neutral Locale
    { 0x00029, W("fa") },        // Neutral Locale
    { 0x0002a, W("vi") },        // Neutral Locale
    { 0x0002b, W("hy") },        // Neutral Locale
    { 0x0002c, W("az") },        // Neutral Locale
    { 0x0002d, W("eu") },        // Neutral Locale
    { 0x0002e, W("hsb") },       // Neutral Locale
    { 0x0002f, W("mk") },        // Neutral Locale
    { 0x00032, W("tn") },        // Neutral Locale
    { 0x00034, W("xh") },        // Neutral Locale
    { 0x00035, W("zu") },        // Neutral Locale
    { 0x00036, W("af") },        // Neutral Locale
    { 0x00037, W("ka") },        // Neutral Locale
    { 0x00038, W("fo") },        // Neutral Locale
    { 0x00039, W("hi") },        // Neutral Locale
    { 0x0003a, W("mt") },        // Neutral Locale
    { 0x0003b, W("se") },        // Neutral Locale
    { 0x0003c, W("ga") },        // Neutral Locale
    { 0x0003e, W("ms") },        // Neutral Locale
    { 0x0003f, W("kk") },        // Neutral Locale
    { 0x00040, W("ky") },        // Neutral Locale
    { 0x00041, W("sw") },        // Neutral Locale
    { 0x00042, W("tk") },        // Neutral Locale
    { 0x00043, W("uz") },        // Neutral Locale
    { 0x00044, W("tt") },        // Neutral Locale
    { 0x00045, W("bn") },        // Neutral Locale
    { 0x00046, W("pa") },        // Neutral Locale
    { 0x00047, W("gu") },        // Neutral Locale
    { 0x00048, W("or") },        // Neutral Locale
    { 0x00049, W("ta") },        // Neutral Locale
    { 0x0004a, W("te") },        // Neutral Locale
    { 0x0004b, W("kn") },        // Neutral Locale
    { 0x0004c, W("ml") },        // Neutral Locale
    { 0x0004d, W("as") },        // Neutral Locale
    { 0x0004e, W("mr") },        // Neutral Locale
    { 0x0004f, W("sa") },        // Neutral Locale
    { 0x00050, W("mn") },        // Neutral Locale
    { 0x00051, W("bo") },        // Neutral Locale
    { 0x00052, W("cy") },        // Neutral Locale
    { 0x00053, W("km") },        // Neutral Locale
    { 0x00054, W("lo") },        // Neutral Locale
    { 0x00056, W("gl") },        // Neutral Locale
    { 0x00057, W("kok") },       // Neutral Locale
    { 0x0005a, W("syr") },       // Neutral Locale
    { 0x0005b, W("si") },        // Neutral Locale
    { 0x0005d, W("iu") },        // Neutral Locale
    { 0x0005e, W("am") },        // Neutral Locale
    { 0x0005f, W("tzm") },       // Neutral Locale
    { 0x00061, W("ne") },        // Neutral Locale
    { 0x00062, W("fy") },        // Neutral Locale
    { 0x00063, W("ps") },        // Neutral Locale
    { 0x00064, W("fil") },       // Neutral Locale
    { 0x00065, W("dv") },        // Neutral Locale
    { 0x00068, W("ha") },        // Neutral Locale
    { 0x0006a, W("yo") },        // Neutral Locale
    { 0x0006b, W("quz") },       // Neutral Locale
    { 0x0006c, W("nso") },       // Neutral Locale
    { 0x0006d, W("ba") },        // Neutral Locale
    { 0x0006e, W("lb") },        // Neutral Locale
    { 0x0006f, W("kl") },        // Neutral Locale
    { 0x00070, W("ig") },        // Neutral Locale
    { 0x00078, W("ii") },        // Neutral Locale
    { 0x0007a, W("arn") },       // Neutral Locale
    { 0x0007c, W("moh") },       // Neutral Locale
    { 0x0007e, W("br") },        // Neutral Locale
    { 0x00080, W("ug") },        // Neutral Locale
    { 0x00081, W("mi") },        // Neutral Locale
    { 0x00082, W("oc") },        // Neutral Locale
    { 0x00083, W("co") },        // Neutral Locale
    { 0x00084, W("gsw") },       // Neutral Locale
    { 0x00085, W("sah") },       // Neutral Locale
    { 0x00086, W("qut") },       // Neutral Locale
    { 0x00087, W("rw") },        // Neutral Locale
    { 0x00088, W("wo") },        // Neutral Locale
    { 0x0008c, W("prs") },       // Neutral Locale

    // Specific Cultures
    { 0x00401, W("ar-SA") },
    { 0x00402, W("bg-BG") },
    { 0x00403, W("ca-ES") },
    { 0x00404, W("zh-TW") },
    { 0x00405, W("cs-CZ") },
    { 0x00406, W("da-DK") },
    { 0x00407, W("de-DE") },
    { 0x00408, W("el-GR") },
    { 0x00409, W("en-US") },
    // es-ES_tradnl only gets used if specifically asked for because
    // GetCultures() won't return it.  (It's not a real locale, its an alt sort)
    { 0x0040a, W("es-ES_tradnl") },
    { 0x0040b, W("fi-FI") },
    { 0x0040c, W("fr-FR") },
    { 0x0040d, W("he-IL") },
    { 0x0040e, W("hu-HU") },
    { 0x0040f, W("is-IS") },
    { 0x00410, W("it-IT") },
    { 0x00411, W("ja-JP") },
    { 0x00412, W("ko-KR") },
    { 0x00413, W("nl-NL") },
    { 0x00414, W("nb-NO") },
    { 0x00415, W("pl-PL") },
    { 0x00416, W("pt-BR") },
    { 0x00417, W("rm-CH") },
    { 0x00418, W("ro-RO") },
    { 0x00419, W("ru-RU") },
    { 0x0041a, W("hr-HR") },
    { 0x0041b, W("sk-SK") },
    { 0x0041c, W("sq-AL") },
    { 0x0041d, W("sv-SE") },
    { 0x0041e, W("th-TH") },
    { 0x0041f, W("tr-TR") },
    { 0x00420, W("ur-PK") },
    { 0x00421, W("id-ID") },
    { 0x00422, W("uk-UA") },
    { 0x00423, W("be-BY") },
    { 0x00424, W("sl-SI") },
    { 0x00425, W("et-EE") },
    { 0x00426, W("lv-LV") },
    { 0x00427, W("lt-LT") },
    { 0x00428, W("tg-Cyrl-TJ") },
    { 0x00429, W("fa-IR") },
    { 0x0042a, W("vi-VN") },
    { 0x0042b, W("hy-AM") },
    { 0x0042c, W("az-Latn-AZ") },
    { 0x0042d, W("eu-ES") },
    { 0x0042e, W("hsb-DE") },
    { 0x0042f, W("mk-MK") },
    { 0x00432, W("tn-ZA") },
    { 0x00434, W("xh-ZA") },
    { 0x00435, W("zu-ZA") },
    { 0x00436, W("af-ZA") },
    { 0x00437, W("ka-GE") },
    { 0x00438, W("fo-FO") },
    { 0x00439, W("hi-IN") },
    { 0x0043a, W("mt-MT") },
    { 0x0043b, W("se-NO") },
    { 0x0043e, W("ms-MY") },
    { 0x0043f, W("kk-KZ") },
    { 0x00440, W("ky-KG") },
    { 0x00441, W("sw-KE") },
    { 0x00442, W("tk-TM") },
    { 0x00443, W("uz-Latn-UZ") },
    { 0x00444, W("tt-RU") },
    { 0x00445, W("bn-IN") },
    { 0x00446, W("pa-IN") },
    { 0x00447, W("gu-IN") },
    { 0x00448, W("or-IN") },
    { 0x00449, W("ta-IN") },
    { 0x0044a, W("te-IN") },
    { 0x0044b, W("kn-IN") },
    { 0x0044c, W("ml-IN") },
    { 0x0044d, W("as-IN") },
    { 0x0044e, W("mr-IN") },
    { 0x0044f, W("sa-IN") },
    { 0x00450, W("mn-MN") },
    { 0x00451, W("bo-CN") },
    { 0x00452, W("cy-GB") },
    { 0x00453, W("km-KH") },
    { 0x00454, W("lo-LA") },
    { 0x00456, W("gl-ES") },
    { 0x00457, W("kok-IN") },
    { 0x0045a, W("syr-SY") },
    { 0x0045b, W("si-LK") },
    { 0x0045d, W("iu-Cans-CA") },
    { 0x0045e, W("am-ET") },
    { 0x00461, W("ne-NP") },
    { 0x00462, W("fy-NL") },
    { 0x00463, W("ps-AF") },
    { 0x00464, W("fil-PH") },
    { 0x00465, W("dv-MV") },
    { 0x00468, W("ha-Latn-NG") },
    { 0x0046a, W("yo-NG") },
    { 0x0046b, W("quz-BO") },
    { 0x0046c, W("nso-ZA") },
    { 0x0046d, W("ba-RU") },
    { 0x0046e, W("lb-LU") },
    { 0x0046f, W("kl-GL") },
    { 0x00470, W("ig-NG") },
    { 0x00478, W("ii-CN") },
    { 0x0047a, W("arn-CL") },
    { 0x0047c, W("moh-CA") },
    { 0x0047e, W("br-FR") },
    { 0x00480, W("ug-CN") },
    { 0x00481, W("mi-NZ") },
    { 0x00482, W("oc-FR") },
    { 0x00483, W("co-FR") },
    { 0x00484, W("gsw-FR") },
    { 0x00485, W("sah-RU") },
    { 0x00486, W("qut-GT") },
    { 0x00487, W("rw-RW") },
    { 0x00488, W("wo-SN") },
    { 0x0048c, W("prs-AF") },
    { 0x00501, W("qps-ploc") },
    { 0x005fe, W("qps-ploca") },
    { 0x00801, W("ar-IQ") },
    { 0x00804, W("zh-CN") },
    { 0x00807, W("de-CH") },
    { 0x00809, W("en-GB") },
    { 0x0080a, W("es-MX") },
    { 0x0080c, W("fr-BE") },
    { 0x00810, W("it-CH") },
    { 0x00813, W("nl-BE") },
    { 0x00814, W("nn-NO") },
    { 0x00816, W("pt-PT") },
    { 0x0081a, W("sr-Latn-CS") },
    { 0x0081d, W("sv-FI") },
    { 0x0082c, W("az-Cyrl-AZ") },
    { 0x0082e, W("dsb-DE") },
    { 0x0083b, W("se-SE") },
    { 0x0083c, W("ga-IE") },
    { 0x0083e, W("ms-BN") },
    { 0x00843, W("uz-Cyrl-UZ") },
    { 0x00845, W("bn-BD") },
    { 0x00850, W("mn-Mong-CN") },
    { 0x0085d, W("iu-Latn-CA") },
    { 0x0085f, W("tzm-Latn-DZ") },
    { 0x0086b, W("quz-EC") },
    { 0x009ff, W("qps-plocm") },
    { 0x00c01, W("ar-EG") },
    { 0x00c04, W("zh-HK") },
    { 0x00c07, W("de-AT") },
    { 0x00c09, W("en-AU") },
    { 0x00c0a, W("es-ES") },
    { 0x00c0c, W("fr-CA") },
    { 0x00c1a, W("sr-Cyrl-CS") },
    { 0x00c3b, W("se-FI") },
    { 0x00c6b, W("quz-PE") },
    { 0x01001, W("ar-LY") },
    { 0x01004, W("zh-SG") },
    { 0x01007, W("de-LU") },
    { 0x01009, W("en-CA") },
    { 0x0100a, W("es-GT") },
    { 0x0100c, W("fr-CH") },
    { 0x0101a, W("hr-BA") },
    { 0x0103b, W("smj-NO") },
    { 0x01401, W("ar-DZ") },
    { 0x01404, W("zh-MO") },
    { 0x01407, W("de-LI") },
    { 0x01409, W("en-NZ") },
    { 0x0140a, W("es-CR") },
    { 0x0140c, W("fr-LU") },
    { 0x0141a, W("bs-Latn-BA") },
    { 0x0143b, W("smj-SE") },
    { 0x01801, W("ar-MA") },
    { 0x01809, W("en-IE") },
    { 0x0180a, W("es-PA") },
    { 0x0180c, W("fr-MC") },
    { 0x0181a, W("sr-Latn-BA") },
    { 0x0183b, W("sma-NO") },
    { 0x01c01, W("ar-TN") },
    { 0x01c09, W("en-ZA") },
    { 0x01c0a, W("es-DO") },
    { 0x01c1a, W("sr-Cyrl-BA") },
    { 0x01c3b, W("sma-SE") },
    { 0x02001, W("ar-OM") },
    { 0x02009, W("en-JM") },
    { 0x0200a, W("es-VE") },
    { 0x0201a, W("bs-Cyrl-BA") },
    { 0x0203b, W("sms-FI") },
    { 0x02401, W("ar-YE") },
    { 0x02409, W("en-029") },
    { 0x0240a, W("es-CO") },
    { 0x0243b, W("smn-FI") },
    { 0x02801, W("ar-SY") },
    { 0x02809, W("en-BZ") },
    { 0x0280a, W("es-PE") },
    { 0x02c01, W("ar-JO") },
    { 0x02c09, W("en-TT") },
    { 0x02c0a, W("es-AR") },
    { 0x03001, W("ar-LB") },
    { 0x03009, W("en-ZW") },
    { 0x0300a, W("es-EC") },
    { 0x03401, W("ar-KW") },
    { 0x03409, W("en-PH") },
    { 0x0340a, W("es-CL") },
    { 0x03801, W("ar-AE") },
    { 0x0380a, W("es-UY") },
    { 0x03c01, W("ar-BH") },
    { 0x03c0a, W("es-PY") },
    { 0x04001, W("ar-QA") },
    { 0x04009, W("en-IN") },
    { 0x0400a, W("es-BO") },
    { 0x04409, W("en-MY") },
    { 0x0440a, W("es-SV") },
    { 0x04809, W("en-SG") },
    { 0x0480a, W("es-HN") },
    { 0x04c0a, W("es-NI") },
    { 0x0500a, W("es-PR") },
    { 0x0540a, W("es-US") },

    // Multiple neutrals
    { 0x0781a, W("bs") },        // Neutral Locale
    { 0x07c04, W("zh-Hant") },   // Neutral Locale
    { 0x07c1a, W("sr") },        // Neutral Locale

    // Alt Sorts
    { 0x1007f, W("x-IV_mathan") },
    { 0x10407, W("de-DE_phoneb") },
    { 0x1040e, W("hu-HU_technl") },
    { 0x10437, W("ka-GE_modern") },
    { 0x20804, W("zh-CN_stroke") },
    { 0x21004, W("zh-SG_stroke") },
    { 0x21404, W("zh-MO_stroke") },
    { 0x30404, W("zh-TW_pronun") },
    { 0x40411, W("ja-JP_radstr") }

    // TODO: Turkic?  (Necessary ?)
};

// Known (ie: <= Vista) Name/LCID lookup table, sorted by Name
const static LCIDEntry s_names[]=
{
    { 0x00036, W("af") },            // Neutral Locale
    { 0x00436, W("af-ZA") },
    { 0x0005e, W("am") },            // Neutral Locale
    { 0x0045e, W("am-ET") },
    { 0x00001, W("ar") },            // Neutral Locale
    { 0x03801, W("ar-AE") },
    { 0x03c01, W("ar-BH") },
    { 0x01401, W("ar-DZ") },
    { 0x00c01, W("ar-EG") },
    { 0x00801, W("ar-IQ") },
    { 0x02c01, W("ar-JO") },
    { 0x03401, W("ar-KW") },
    { 0x03001, W("ar-LB") },
    { 0x01001, W("ar-LY") },
    { 0x01801, W("ar-MA") },
    { 0x02001, W("ar-OM") },
    { 0x04001, W("ar-QA") },
    { 0x00401, W("ar-SA") },
    { 0x02801, W("ar-SY") },
    { 0x01c01, W("ar-TN") },
    { 0x02401, W("ar-YE") },
    { 0x0007a, W("arn") },            // Neutral Locale
    { 0x0047a, W("arn-CL") },
    { 0x0004d, W("as") },            // Neutral Locale
    { 0x0044d, W("as-IN") },
    { 0x0002c, W("az") },            // Neutral Locale
    { 0x0082c, W("az-Cyrl-AZ") },
    { 0x0042c, W("az-Latn-AZ") },
    { 0x0006d, W("ba") },            // Neutral Locale
    { 0x0046d, W("ba-RU") },
    { 0x00023, W("be") },            // Neutral Locale
    { 0x00423, W("be-BY") },
    { 0x00002, W("bg") },            // Neutral Locale
    { 0x00402, W("bg-BG") },
    { 0x00045, W("bn") },            // Neutral Locale
    { 0x00845, W("bn-BD") },
    { 0x00445, W("bn-IN") },
    { 0x00051, W("bo") },            // Neutral Locale
    { 0x00451, W("bo-CN") },
    { 0x0007e, W("br") },            // Neutral Locale
    { 0x0047e, W("br-FR") },
    { 0x0781a, W("bs") },            // Neutral Locale
    { 0x0201a, W("bs-Cyrl-BA") },
    { 0x0141a, W("bs-Latn-BA") },
    { 0x00003, W("ca") },            // Neutral Locale
    { 0x00403, W("ca-ES") },
    { 0x00083, W("co") },            // Neutral Locale
    { 0x00483, W("co-FR") },
    { 0x00005, W("cs") },            // Neutral Locale
    { 0x00405, W("cs-CZ") },
    { 0x00052, W("cy") },            // Neutral Locale
    { 0x00452, W("cy-GB") },
    { 0x00006, W("da") },            // Neutral Locale
    { 0x00406, W("da-DK") },
    { 0x00007, W("de") },            // Neutral Locale
    { 0x00c07, W("de-AT") },
    { 0x00807, W("de-CH") },
    { 0x00407, W("de-DE") },
    { 0x10407, W("de-DE_phoneb") },  // Alternate Sort
    { 0x01407, W("de-LI") },
    { 0x01007, W("de-LU") },
    { 0x0082e, W("dsb-DE") },
    { 0x00065, W("dv") },            // Neutral Locale
    { 0x00465, W("dv-MV") },
    { 0x00008, W("el") },            // Neutral Locale
    { 0x00408, W("el-GR") },
    { 0x00009, W("en") },            // Neutral Locale
    { 0x02409, W("en-029") },
    { 0x00c09, W("en-AU") },
    { 0x02809, W("en-BZ") },
    { 0x01009, W("en-CA") },
    { 0x00809, W("en-GB") },
    { 0x01809, W("en-IE") },
    { 0x04009, W("en-IN") },
    { 0x02009, W("en-JM") },
    { 0x04409, W("en-MY") },
    { 0x01409, W("en-NZ") },
    { 0x03409, W("en-PH") },
    { 0x04809, W("en-SG") },
    { 0x02c09, W("en-TT") },
    { 0x00409, W("en-US") },
    { 0x01c09, W("en-ZA") },
    { 0x03009, W("en-ZW") },
    { 0x0000a, W("es") },            // Neutral Locale
    { 0x02c0a, W("es-AR") },
    { 0x0400a, W("es-BO") },
    { 0x0340a, W("es-CL") },
    { 0x0240a, W("es-CO") },
    { 0x0140a, W("es-CR") },
    { 0x01c0a, W("es-DO") },
    { 0x0300a, W("es-EC") },
    { 0x00c0a, W("es-ES") },
    // es-ES_tradnl only gets used if specifically asked for because
    // GetCultures() won't return it.  (It's not a real locale, its an alt sort)
    { 0x0040a, W("es-ES_tradnl") },
    { 0x0100a, W("es-GT") },
    { 0x0480a, W("es-HN") },
    { 0x0080a, W("es-MX") },
    { 0x04c0a, W("es-NI") },
    { 0x0180a, W("es-PA") },
    { 0x0280a, W("es-PE") },
    { 0x0500a, W("es-PR") },
    { 0x03c0a, W("es-PY") },
    { 0x0440a, W("es-SV") },
    { 0x0540a, W("es-US") },
    { 0x0380a, W("es-UY") },
    { 0x0200a, W("es-VE") },
    { 0x00025, W("et") },            // Neutral Locale
    { 0x00425, W("et-EE") },
    { 0x0002d, W("eu") },            // Neutral Locale
    { 0x0042d, W("eu-ES") },
    { 0x00029, W("fa") },            // Neutral Locale
    { 0x00429, W("fa-IR") },
    { 0x0000b, W("fi") },            // Neutral Locale
    { 0x0040b, W("fi-FI") },
    { 0x00064, W("fil") },           // Neutral Locale
    { 0x00464, W("fil-PH") },
    { 0x00038, W("fo") },            // Neutral Locale
    { 0x00438, W("fo-FO") },
    { 0x0000c, W("fr") },            // Neutral Locale
    { 0x0080c, W("fr-BE") },
    { 0x00c0c, W("fr-CA") },
    { 0x0100c, W("fr-CH") },
    { 0x0040c, W("fr-FR") },
    { 0x0140c, W("fr-LU") },
    { 0x0180c, W("fr-MC") },
    { 0x00062, W("fy") },            // Neutral Locale
    { 0x00462, W("fy-NL") },
    { 0x0003c, W("ga") },            // Neutral Locale
    { 0x0083c, W("ga-IE") },
    { 0x00056, W("gl") },            // Neutral Locale
    { 0x00456, W("gl-ES") },
    { 0x00084, W("gsw") },           // Neutral Locale
    { 0x00484, W("gsw-FR") },
    { 0x00047, W("gu") },            // Neutral Locale
    { 0x00447, W("gu-IN") },
    { 0x00068, W("ha") },            // Neutral Locale
    { 0x00468, W("ha-Latn-NG") },
    { 0x0000d, W("he") },            // Neutral Locale
    { 0x0040d, W("he-IL") },
    { 0x00039, W("hi") },            // Neutral Locale
    { 0x00439, W("hi-IN") },
    { 0x0001a, W("hr") },            // Neutral Locale
    { 0x0101a, W("hr-BA") },
    { 0x0041a, W("hr-HR") },
    { 0x0002e, W("hsb") },           // Neutral Locale
    { 0x0042e, W("hsb-DE") },
    { 0x0000e, W("hu") },            // Neutral Locale
    { 0x0040e, W("hu-HU") },
    { 0x1040e, W("hu-HU_technl") },  // Alternate Sort
    { 0x0002b, W("hy") },            // Neutral Locale
    { 0x0042b, W("hy-AM") },
    { 0x00021, W("id") },            // Neutral Locale
    { 0x00421, W("id-ID") },
    { 0x00070, W("ig") },            // Neutral Locale
    { 0x00470, W("ig-NG") },
    { 0x00078, W("ii") },            // Neutral Locale
    { 0x00478, W("ii-CN") },
    { 0x0000f, W("is") },            // Neutral Locale
    { 0x0040f, W("is-IS") },
    { 0x00010, W("it") },            // Neutral Locale
    { 0x00810, W("it-CH") },
    { 0x00410, W("it-IT") },
    { 0x0005d, W("iu") },            // Neutral Locale
    { 0x0045d, W("iu-Cans-CA") },
    { 0x0085d, W("iu-Latn-CA") },
    { 0x00011, W("ja") },            // Neutral Locale
    { 0x00411, W("ja-JP") },
    { 0x40411, W("ja-JP_radstr") },  // Alternate Sort
    { 0x00037, W("ka") },            // Neutral Locale
    { 0x00437, W("ka-GE") },
    { 0x10437, W("ka-GE_modern") },  // Alternate Sort
    { 0x0003f, W("kk") },            // Neutral Locale
    { 0x0043f, W("kk-KZ") },
    { 0x0006f, W("kl") },            // Neutral Locale
    { 0x0046f, W("kl-GL") },
    { 0x00053, W("km") },            // Neutral Locale
    { 0x00453, W("km-KH") },
    { 0x0004b, W("kn") },            // Neutral Locale
    { 0x0044b, W("kn-IN") },
    { 0x00012, W("ko") },            // Neutral Locale
    { 0x00412, W("ko-KR") },
    { 0x00057, W("kok") },           // Neutral Locale
    { 0x00457, W("kok-IN") },
    { 0x00040, W("ky") },            // Neutral Locale
    { 0x00440, W("ky-KG") },
    { 0x0006e, W("lb") },            // Neutral Locale
    { 0x0046e, W("lb-LU") },
    { 0x00054, W("lo") },            // Neutral Locale
    { 0x00454, W("lo-LA") },
    { 0x00027, W("lt") },            // Neutral Locale
    { 0x00427, W("lt-LT") },
    { 0x00026, W("lv") },            // Neutral Locale
    { 0x00426, W("lv-LV") },
    { 0x00081, W("mi") },            // Neutral Locale
    { 0x00481, W("mi-NZ") },
    { 0x0002f, W("mk") },            // Neutral Locale
    { 0x0042f, W("mk-MK") },
    { 0x0004c, W("ml") },            // Neutral Locale
    { 0x0044c, W("ml-IN") },
    { 0x00050, W("mn") },            // Neutral Locale
    { 0x00450, W("mn-MN") },
    { 0x00850, W("mn-Mong-CN") },
    { 0x0007c, W("moh") },           // Neutral Locale
    { 0x0047c, W("moh-CA") },
    { 0x0004e, W("mr") },            // Neutral Locale
    { 0x0044e, W("mr-IN") },
    { 0x0003e, W("ms") },            // Neutral Locale
    { 0x0083e, W("ms-BN") },
    { 0x0043e, W("ms-MY") },
    { 0x0003a, W("mt") },            // Neutral Locale
    { 0x0043a, W("mt-MT") },
    { 0x00414, W("nb-NO") },
    { 0x00061, W("ne") },            // Neutral Locale
    { 0x00461, W("ne-NP") },
    { 0x00013, W("nl") },            // Neutral Locale
    { 0x00813, W("nl-BE") },
    { 0x00413, W("nl-NL") },
    { 0x00814, W("nn-NO") },
    { 0x00014, W("no") },            // Neutral Locale
    { 0x0006c, W("nso") },           // Neutral Locale
    { 0x0046c, W("nso-ZA") },
    { 0x00082, W("oc") },            // Neutral Locale
    { 0x00482, W("oc-FR") },
    { 0x00048, W("or") },            // Neutral Locale
    { 0x00448, W("or-IN") },
    { 0x00046, W("pa") },            // Neutral Locale
    { 0x00446, W("pa-IN") },
    { 0x00015, W("pl") },            // Neutral Locale
    { 0x00415, W("pl-PL") },
    { 0x0008c, W("prs") },           // Neutral Locale
    { 0x0048c, W("prs-AF") },
    { 0x00063, W("ps") },            // Neutral Locale
    { 0x00463, W("ps-AF") },
    { 0x00016, W("pt") },            // Neutral Locale
    { 0x00416, W("pt-BR") },
    { 0x00816, W("pt-PT") },
    { 0x00501, W("qps-ploc") },
    { 0x005fe, W("qps-ploca") },
    { 0x009ff, W("qps-plocm") },
    { 0x00086, W("qut") },           // Neutral Locale
    { 0x00486, W("qut-GT") },
    { 0x0006b, W("quz") },           // Neutral Locale
    { 0x0046b, W("quz-BO") },
    { 0x0086b, W("quz-EC") },
    { 0x00c6b, W("quz-PE") },
    { 0x00017, W("rm") },            // Neutral Locale
    { 0x00417, W("rm-CH") },
    { 0x00018, W("ro") },            // Neutral Locale
    { 0x00418, W("ro-RO") },
    { 0x00019, W("ru") },            // Neutral Locale
    { 0x00419, W("ru-RU") },
    { 0x00087, W("rw") },            // Neutral Locale
    { 0x00487, W("rw-RW") },
    { 0x0004f, W("sa") },            // Neutral Locale
    { 0x0044f, W("sa-IN") },
    { 0x00085, W("sah") },           // Neutral Locale
    { 0x00485, W("sah-RU") },
    { 0x0003b, W("se") },            // Neutral Locale
    { 0x00c3b, W("se-FI") },
    { 0x0043b, W("se-NO") },
    { 0x0083b, W("se-SE") },
    { 0x0005b, W("si") },            // Neutral Locale
    { 0x0045b, W("si-LK") },
    { 0x0001b, W("sk") },            // Neutral Locale
    { 0x0041b, W("sk-SK") },
    { 0x00024, W("sl") },            // Neutral Locale
    { 0x00424, W("sl-SI") },
    { 0x0183b, W("sma-NO") },
    { 0x01c3b, W("sma-SE") },
    { 0x0103b, W("smj-NO") },
    { 0x0143b, W("smj-SE") },
    { 0x0243b, W("smn-FI") },
    { 0x0203b, W("sms-FI") },
    { 0x0001c, W("sq") },            // Neutral Locale
    { 0x0041c, W("sq-AL") },
    { 0x07c1a, W("sr") },            // Neutral Locale
    { 0x01c1a, W("sr-Cyrl-BA") },
    { 0x00c1a, W("sr-Cyrl-CS") },
    { 0x0181a, W("sr-Latn-BA") },
    { 0x0081a, W("sr-Latn-CS") },
    { 0x0001d, W("sv") },            // Neutral Locale
    { 0x0081d, W("sv-FI") },
    { 0x0041d, W("sv-SE") },
    { 0x00041, W("sw") },            // Neutral Locale
    { 0x00441, W("sw-KE") },
    { 0x0005a, W("syr") },           // Neutral Locale
    { 0x0045a, W("syr-SY") },
    { 0x00049, W("ta") },            // Neutral Locale
    { 0x00449, W("ta-IN") },
    { 0x0004a, W("te") },            // Neutral Locale
    { 0x0044a, W("te-IN") },
    { 0x00028, W("tg") },            // Neutral Locale
    { 0x00428, W("tg-Cyrl-TJ") },
    { 0x0001e, W("th") },            // Neutral Locale
    { 0x0041e, W("th-TH") },
    { 0x00042, W("tk") },            // Neutral Locale
    { 0x00442, W("tk-TM") },
    { 0x00032, W("tn") },            // Neutral Locale
    { 0x00432, W("tn-ZA") },
    { 0x0001f, W("tr") },            // Neutral Locale
    { 0x0041f, W("tr-TR") },
    { 0x00044, W("tt") },            // Neutral Locale
    { 0x00444, W("tt-RU") },
    { 0x0005f, W("tzm") },           // Neutral Locale
    { 0x0085f, W("tzm-Latn-DZ") },
    { 0x00080, W("ug") },            // Neutral Locale
    { 0x00480, W("ug-CN") },
    { 0x00022, W("uk") },            // Neutral Locale
    { 0x00422, W("uk-UA") },
    { 0x00020, W("ur") },            // Neutral Locale
    { 0x00420, W("ur-PK") },
    { 0x00043, W("uz") },            // Neutral Locale
    { 0x00843, W("uz-Cyrl-UZ") },
    { 0x00443, W("uz-Latn-UZ") },
    { 0x0002a, W("vi") },            // Neutral Locale
    { 0x0042a, W("vi-VN") },
    { 0x00088, W("wo") },            // Neutral Locale
    { 0x00488, W("wo-SN") },
    { 0x1007f, W("x-IV_mathan") },   // Alternate Sort
    { 0x00034, W("xh") },            // Neutral Locale
    { 0x00434, W("xh-ZA") },
    { 0x0006a, W("yo") },            // Neutral Locale
    { 0x0046a, W("yo-NG") },
    { 0x00804, W("zh-CN") },
    { 0x20804, W("zh-CN_stroke") },  // Alternate Sort
    { 0x00004, W("zh-Hans") },       // Neutral Locale
    { 0x07c04, W("zh-Hant") },       // Neutral Locale
    { 0x00c04, W("zh-HK") },
    { 0x01404, W("zh-MO") },
    { 0x21404, W("zh-MO_stroke") },  // Alternate Sort
    { 0x01004, W("zh-SG") },
    { 0x21004, W("zh-SG_stroke") },  // Alternate Sort
    { 0x00404, W("zh-TW") },
    { 0x30404, W("zh-TW_pronun") },  // Alternate Sort
    { 0x00035, W("zu") },            // Neutral Locale
    { 0x00435, W("zu-ZA") }
};

// This is the data from l_intl.nls as released for XP for the uppercase table.
// This is used for casing with OrdinalCompareStringIgnoreCase
// since XP is the only scenario that needs data and the only data that it needs
// is uppercasing, we duplicate the table here.
const static WORD s_pUppercaseIndexTableXP[] = {
0x0110, 0x0120, 0x0130, 0x0140, 0x0150, 0x0160, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0170, 0x0180, 0x0100, 0x0190, 0x0100, 0x0100, 0x01a0, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100,
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x01b0, 0x01c0, 0x01c0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01d0, 0x01e0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01f0, 0x0200, 0x0210, 0x0220,
0x0230, 0x0240, 0x0250, 0x0260, 0x0270, 0x0280, 0x0290, 0x02a0, 0x02b0, 0x02c0,
0x02d0, 0x02e0, 0x02f0, 0x0300, 0x0310, 0x0320, 0x01c0, 0x01c0, 0x01c0, 0x0330,
0x0340, 0x0350, 0x0360, 0x0370, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x0380,
0x0390, 0x03a0, 0x03b0, 0x03c0, 0x03d0, 0x03e0, 0x01c0, 0x01c0, 0x01c0, 0x03f0,
0x0400, 0x0410, 0x0420, 0x0430, 0x0440, 0x0450, 0x0460, 0x0470, 0x0480, 0x0490,
0x04a0, 0x04b0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x04c0, 0x04d0,
0x04e0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x04f0, 0x0500,
0x0510, 0x0520, 0x0530, 0x0540, 0x0550, 0x0560, 0x0570, 0x0580, 0x0590, 0x05a0,
0x05b0, 0x05c0, 0x05d0, 0x05e0, 0x05f0, 0x0600, 0x0610, 0x0620, 0x0630, 0x0640,
0x0650, 0x0660, 0x01c0, 0x01c0, 0x01c0, 0x0670, 0x01c0, 0x0680, 0x0690, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06a0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06b0,
0x06c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06d0, 0x06e0, 0x01c0, 0x01c0,
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0x0000,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0x0079, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000,
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000,
0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000,
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000,
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000,
0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfffe, 0x0000, 0x0000, 0xfffe,
0x0000, 0x0000, 0xfffe, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000,
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0xffb1,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000,
0x0000, 0xfffe, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff2e,
0xff32, 0x0000, 0xff33, 0xff33, 0x0000, 0xff36, 0x0000, 0xff35, 0x0000, 0x0000,
0x0000, 0x0000, 0xff33, 0x0000, 0x0000, 0xff31, 0x0000, 0x0000, 0x0000, 0x0000,
0xff2f, 0xff2d, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff2d, 0x0000, 0x0000,
0xff2b, 0x0000, 0x0000, 0xff2a, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff26, 0x0000, 0x0000,
0x0000, 0x0000, 0xff26, 0x0000, 0xff27, 0xff27, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0xff25, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0xffda, 0xffdb, 0xffdb, 0xffdb, 0x0000, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe1, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffc0, 0xffc1, 0xffc1, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0x0000, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0,
0xffb0, 0xffb0, 0xffb0, 0x0000, 0xffb0, 0xffb0, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000,
0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffd0, 0xffd0, 0xffd0,
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0,
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0,
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0,
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff,
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008,
0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008,
0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008,
0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x004a, 0x004a, 0x0056, 0x0056, 0x0056, 0x0056, 0x0064, 0x0064,
0x0080, 0x0080, 0x0070, 0x0070, 0x007e, 0x007e, 0x0000, 0x0000, 0x0008, 0x0008,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000,
0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0007, 0x0000, 0x0000, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfff0, 0xfff0, 0xfff0, 0xfff0,
0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0,
0xfff0, 0xfff0, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6,
0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6,
0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0x0000, 0x0000,
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0,
0xffe0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
};

// This is the data from casing.nls as released for Win2K3/XP for the lowercase table.
// This is used for casing when linguistic casing is needed
// since Win2K3/XP is the only scenario that needs data, we duplicate the table here.
// It does NOT contain Turkic I exception behavior
const static WORD s_pLowercaseIndexTableLinguisticXP[] = {
0x0110, 0x0120, 0x0130, 0x0140, 0x0150, 0x0160, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0170, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0180, 0x0190, 0x0100, 0x01a0, 0x0100, 0x0100, 0x01b0, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x01c0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01e0, 0x01f0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x0200, 0x0210, 0x01d0, 0x01d0, 0x0220, 0x0230, 
0x0240, 0x0250, 0x0260, 0x0270, 0x0280, 0x0290, 0x02a0, 0x02b0, 0x02c0, 0x02d0, 
0x02e0, 0x02f0, 0x0300, 0x0310, 0x0320, 0x0330, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x0340, 0x0350, 
0x0360, 0x01d0, 0x01d0, 0x0370, 0x0380, 0x01d0, 0x0390, 0x03a0, 0x03b0, 0x01d0, 
0x01d0, 0x01d0, 0x03c0, 0x03d0, 0x03e0, 0x03f0, 0x0400, 0x0410, 0x0420, 0x0430, 
0x0440, 0x0450, 0x01d0, 0x01d0, 0x01d0, 0x0460, 0x0470, 0x0480, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x0490, 0x04a0, 
0x04b0, 0x01d0, 0x01d0, 0x01d0, 0x04c0, 0x04d0, 0x04e0, 0x04f0, 0x0500, 0x0510, 
0x0520, 0x0530, 0x0540, 0x0550, 0x0560, 0x0570, 0x0580, 0x0590, 0x05a0, 0x05b0, 
0x05c0, 0x05d0, 0x05e0, 0x05f0, 0x0600, 0x0610, 0x0620, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x0630, 0x0640, 0x0650, 0x0660, 0x0670, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x0680, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x0690, 0x06a0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x06b0, 0x06c0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x01d0, 
0x01d0, 0x01d0, 0x01d0, 0x01d0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0000, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0xff39, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 
0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0xff87, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 
0x0000, 0x0000, 0x0000, 0x00d2, 0x0001, 0x0000, 0x0001, 0x0000, 0x00ce, 0x0001, 
0x0000, 0x00cd, 0x00cd, 0x0001, 0x0000, 0x0000, 0x004f, 0x00ca, 0x00cb, 0x0001, 
0x0000, 0x00cd, 0x00cf, 0x0000, 0x00d3, 0x00d1, 0x0001, 0x0000, 0x0000, 0x0000, 
0x00d3, 0x00d5, 0x0000, 0x00d6, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0000, 0x0001, 0x0000, 0x00da, 0x0000, 0x0000, 0x0001, 0x0000, 0x00da, 0x0001, 
0x0000, 0x00d9, 0x00d9, 0x0001, 0x0000, 0x0001, 0x0000, 0x00db, 0x0001, 0x0000, 
0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0002, 0x0001, 0x0000, 0x0002, 0x0001, 0x0000, 0x0002, 0x0001, 0x0000, 0x0001, 
0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 
0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0002, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0026, 0x0000, 
0x0025, 0x0025, 0x0025, 0x0000, 0x0040, 0x0000, 0x003f, 0x003f, 0x0000, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0000, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0xfff3, 0xfffa, 0xfff7, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0000, 0x0050, 0x0050, 0x0050, 0x0050, 0x0050, 0x0050, 0x0050, 
0x0050, 0x0050, 0x0050, 0x0050, 0x0050, 0x0000, 0x0050, 0x0050, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 
0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0000, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 
0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0030, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 
0x0001, 0x0000, 0x0001, 0x0000, 0x0001, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 
0xfff8, 0xfff8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfff8, 0xfff8, 
0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0xfff8, 0x0000, 0xfff8, 0x0000, 0xfff8, 0x0000, 0xfff8, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfff8, 0xfff8, 0xfff8, 0xfff8, 
0xfff8, 0xfff8, 0xfff8, 0xfff8, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0xfff8, 0xfff8, 0xffb6, 0xffb6, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffaa, 0xffaa, 
0xffaa, 0xffaa, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0xfff8, 0xfff8, 0xff9c, 0xff9c, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0xfff8, 0xfff8, 0xff90, 0xff90, 0xfff9, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff80, 0xff80, 0xff82, 0xff82, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 
0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 0x0010, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x001a, 0x001a, 0x001a, 0x001a, 
0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 
0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 0x001a, 
0x001a, 0x001a, 0x0000, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 
0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0020, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000
};

// This is the data from casing.nls as released for Win2K3/XP for the uppercase table.
// This is used for casing when linguistic casing is needed
// since Win2K3/XP is the only scenario that needs data, we duplicate the table here.
// It does NOT contain Turkic I exception behavior
const static WORD s_pUppercaseIndexTableLinguisticXP[] = {
0x0110, 0x0120, 0x0130, 0x0140, 0x0150, 0x0160, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0170, 0x0180, 0x0100, 0x0190, 0x0100, 0x0100, 0x01a0, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 
0x0100, 0x0100, 0x0100, 0x0100, 0x0100, 0x01b0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01d0, 0x01e0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01f0, 0x0200, 0x0210, 0x0220, 
0x0230, 0x0240, 0x0250, 0x0260, 0x0270, 0x0280, 0x0290, 0x02a0, 0x02b0, 0x02c0, 
0x02d0, 0x02e0, 0x02f0, 0x0300, 0x0310, 0x0320, 0x01c0, 0x01c0, 0x01c0, 0x0330, 
0x0340, 0x0350, 0x0360, 0x0370, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x0380, 
0x0390, 0x03a0, 0x03b0, 0x03c0, 0x03d0, 0x03e0, 0x01c0, 0x01c0, 0x01c0, 0x03f0, 
0x0400, 0x0410, 0x0420, 0x0430, 0x0440, 0x0450, 0x0460, 0x0470, 0x0480, 0x0490, 
0x04a0, 0x04b0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x04c0, 0x04d0, 
0x04e0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x04f0, 0x0500, 
0x0510, 0x0520, 0x0530, 0x0540, 0x0550, 0x0560, 0x0570, 0x0580, 0x0590, 0x05a0, 
0x05b0, 0x05c0, 0x05d0, 0x05e0, 0x05f0, 0x0600, 0x0610, 0x0620, 0x0630, 0x0640, 
0x0650, 0x0660, 0x01c0, 0x01c0, 0x01c0, 0x0670, 0x01c0, 0x0680, 0x0690, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06a0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06b0, 
0x06c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x06d0, 0x06e0, 0x01c0, 0x01c0, 
0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x01c0, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0x0000, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0x0079, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xff18, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 
0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 
0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0xfffe, 0x0000, 0xffff, 0xfffe, 
0x0000, 0xffff, 0xfffe, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 
0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0xffb1, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 
0xffff, 0xfffe, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff2e, 
0xff32, 0x0000, 0xff33, 0xff33, 0x0000, 0xff36, 0x0000, 0xff35, 0x0000, 0x0000, 
0x0000, 0x0000, 0xff33, 0x0000, 0x0000, 0xff31, 0x0000, 0x0000, 0x0000, 0x0000, 
0xff2f, 0xff2d, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff2d, 0x0000, 0x0000, 
0xff2b, 0x0000, 0x0000, 0xff2a, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xff26, 0x0000, 0x0000, 
0x0000, 0x0000, 0xff26, 0x0000, 0xff27, 0xff27, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0xff25, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x001a, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0xffda, 0xffdb, 0xffdb, 0xffdb, 0xfffb, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe1, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffc0, 0xffc1, 0xffc1, 0x0000, 
0xffc2, 0xffc7, 0x0000, 0x0000, 0x0000, 0xffd1, 0xffca, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0xffaa, 0xffb0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0x0000, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 0xffb0, 
0xffb0, 0xffb0, 0xffb0, 0x0000, 0xffb0, 0xffb0, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 
0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0xffff, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffd0, 0xffd0, 0xffd0, 
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 
0xffd0, 0xffd0, 0xffd0, 0xffd0, 0xffd0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0xffff, 
0x0000, 0xffff, 0x0000, 0xffff, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 
0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 
0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 0x0008, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0008, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0008, 0x0008, 
0x0008, 0x0008, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x004a, 0x004a, 0x0056, 0x0056, 0x0056, 0x0056, 0x0064, 0x0064, 
0x0080, 0x0080, 0x0070, 0x0070, 0x007e, 0x007e, 0x0000, 0x0000, 0x0008, 0x0008, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0008, 0x0008, 0x0000, 0x0000, 0x0000, 0x0007, 0x0000, 0x0000, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 
0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 0xfff0, 
0xfff0, 0xfff0, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 
0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 
0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0xffe6, 0x0000, 0x0000, 
0x0000, 0x0000, 0x0000, 0x0000, 0x0000, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 0xffe0, 
0xffe0, 0x0000, 0x0000, 0x0000, 0x0000, 0x0000
};

static CRITSEC_COOKIE g_pDownlevelNlsCrst = NULL;

static void DelayCreateCriticalSection()
{
    // Lazily allocate a Crst to serialize update access to the info structure.
    // Carefully synchronize to ensure we don't leak a Crst in race conditions.
    if (g_pDownlevelNlsCrst == NULL)
    {
        CRITSEC_COOKIE pCrst = ClrCreateCriticalSection(CrstNls,
                               (CrstFlags)(CRST_UNSAFE_COOPGC));

        if (InterlockedCompareExchangeT(&g_pDownlevelNlsCrst, pCrst, NULL) != NULL)
        {
            ClrDeleteCriticalSection(pCrst);
        }
    }
}

static void NormalizeCultureName(SString& sName)
{
    for (SString::Iterator i = sName.Begin(); i[0] != W('\0'); ++i)
    {
        sName.Replace(i,towlower(i[0]));
    }
}

static inline BOOL IsTurkicILcid(LCID lcid)
{
  return (PRIMARYLANGID(lcid) == LANG_TURKISH
       || (PRIMARYLANGID(lcid) == LANG_AZERI
            && lcid != LCID_AZ_CYRL_AZ));
}

namespace DownLevel
{

    int GetSystemDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
        LCID lcid=GetSystemDefaultLCID();
        if (lcid == 0)
            return 0;
        return NewApis::LCIDToLocaleName(lcid,lpLocaleName,cchLocaleName,0);
    };

    // Xp returns lcid 0x0404 (zh-TW) for GetUserDefaultUILanguage and GetSystemDefaultUILanguage
    // on a CHH sku (expected 0x0c04) as well as a CHT sku (as expected)
    inline LCID WorkAroundXpTaiwanHongKongBug(LCID lcid)
    {
        const LCID LANGID_ZH_TW = 0x0404;
        const LCID LANGID_ZH_HK = 0x0c04;

        if(lcid == LANGID_ZH_TW && !NewApis::IsZhTwSku())
        {
            lcid = LANGID_ZH_HK;
        }
        return lcid;
    }

    DWORD GetUserPreferredUILanguages (__in DWORD dwFlags, __out PULONG pulNumLanguages, __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer, __in PULONG pcchLanguagesBuffer)
    {
        _ASSERTE(dwFlags==MUI_LANGUAGE_NAME);

        LCID lcid=GetUserDefaultUILanguage();
        if (lcid == 0)
            return 0;

        lcid = WorkAroundXpTaiwanHongKongBug(lcid);

         WCHAR wszBuffer[LOCALE_NAME_MAX_LENGTH];
         if (NewApis::LCIDToLocaleName(lcid, wszBuffer, NumItems(wszBuffer), 0) == 0)
            return 0;

        SIZE_T sLen=wcslen(wszBuffer)+2;
        ULONG uLen = (ULONG)sLen;
        if (uLen != sLen)
        {
            SetLastError(ERROR_ARITHMETIC_OVERFLOW);
            return 0;
        }

         *pulNumLanguages=1;
         if (pwszLanguagesBuffer == NULL)
         {
            *pcchLanguagesBuffer=uLen;
            return 1;
         }

         if (sLen > *pcchLanguagesBuffer)
         {
            *pcchLanguagesBuffer=uLen;
            SetLastError(ERROR_BUFFER_OVERFLOW);
            return 0;
         }

         *pcchLanguagesBuffer=uLen;
         wcscpy_s(pwszLanguagesBuffer, sLen, wszBuffer);
         pwszLanguagesBuffer[sLen-1]=W('\0');
         SetLastError(0);
         return 1;
    }


    int GetUserDefaultLocaleName(__out_ecount(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
        LCID lcid=GetUserDefaultLCID();
        if (lcid == 0)
            return 0;
        return NewApis::LCIDToLocaleName(lcid,lpLocaleName,cchLocaleName,0);
    }

    int GetDateFormatEx(__in LPCWSTR lpLocaleName, __in DWORD dwFlags, __in_opt CONST SYSTEMTIME* lpDate, __in_opt LPCWSTR lpFormat,
                             __out_ecount(cchDate) LPWSTR lpDateStr, __in int cchDate, __in_opt LPCWSTR lpCalendar)
    {
        _ASSERTE(lpCalendar==NULL);
        LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
        if (lcid == 0)
            return 0;
        return GetDateFormatW(lcid,  dwFlags,  lpDate, lpFormat,  lpDateStr, cchDate);
    }


    int GetLocaleInfoEx (__in LPCWSTR lpLocaleName, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData)
    {
        _ASSERTE((lpLCData == NULL && cchData == 0) || (lpLCData != NULL && cchData > 0));

        // Note that this'll return neutral LCIDs
        LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
        if (lcid == 0)
            return 0;

        int iRetCode = 0;
        // Special casing LOCALE_SPARENT to do Uplevel fallback.
        if ( (LCType & ~LOCALE_NOUSEROVERRIDE) == LOCALE_SPARENT )
        {
            // OS doesn't know some LCTypes
            iRetCode = UplevelFallback::GetLocaleInfoEx(lpLocaleName, lcid, LCType, lpLCData, cchData);
        }
        else
        {
            iRetCode=GetLocaleInfoW(lcid,LCType,lpLCData,cchData);
            if (iRetCode == 0 && GetLastError() == ERROR_INVALID_FLAGS)
            {
                // OS doesn't know some LCTypes
                iRetCode = UplevelFallback::GetLocaleInfoEx(lpLocaleName, lcid, LCType, lpLCData, cchData);
            }
        }

        return iRetCode;
    };

        //
    // TurkishCompareStringIgnoreCase
    // In downlevel platforms CompareString doesn't support Turkish I comparison. TurkishCompareStringIgnoreCase is to work around for this.
    // In Turkish and Azeri cultures:
    //      ToUpper(i)  = u0130 (Upper case I with dot above it)
    //      ToLower(I)  = u0131 (Lower case i with no dot above).
    //      Toupper(i) != I
    //      ToLower(I) != i
    // TurkishCompareStringIgnoreCase will scan the input strings and convert every i to u0130 and every I to u0131 and then call
    // the system CompareString.
    // if lpString1 not include i, I,  u0130, and u0131 then we'll just call the system CompareString otherwise we'll scan the lpString2
    // to detect if we need to do the conversions mentioned above.
    //

    #define TURKISH_CAPITAL_I_DOT_ABOVE ((WCHAR) 0x0130)
    #define TURKISH_LOWERCASE_DOTLESS_I ((WCHAR) 0x0131)
    #define LATIN_LOWERCASE_I_DOT_ABOVE ('i')
    #define LATIN_CAPITAL_DOTLESS_I ('I')

    int TurkishCompareStringIgnoreCase(LCID lcid, DWORD dwCmpFlags, LPCWSTR lpString1, int cchCount1, LPCWSTR lpString2, int cchCount2)
    {
        int str1Index = 0;
        int str2Index = 0;
        BOOL fScanStr2 = FALSE;

        for (str1Index=0; str1Index<cchCount1; str1Index++)
        {
            if (lpString1[str1Index] == LATIN_LOWERCASE_I_DOT_ABOVE
             || lpString1[str1Index] == LATIN_CAPITAL_DOTLESS_I)
            {
                break;
            }
            else if (lpString1[str1Index] == TURKISH_CAPITAL_I_DOT_ABOVE
                  || lpString1[str1Index] == TURKISH_LOWERCASE_DOTLESS_I)
            {
                fScanStr2 = TRUE;
            }
        }

        if (str1Index >= cchCount1)
        {
            if (!fScanStr2)
            {
                return ::CompareStringW(lcid, dwCmpFlags, lpString1, cchCount1, lpString2, cchCount2);
            }

            for (str2Index=0; str2Index<cchCount2; str2Index++)
            {
                if (lpString2[str2Index] == LATIN_LOWERCASE_I_DOT_ABOVE
                 || lpString2[str2Index] == LATIN_CAPITAL_DOTLESS_I)
                {
                    break;
                }
            }

            if (str2Index >= cchCount2)
            {
                return ::CompareStringW(lcid, dwCmpFlags, lpString1, cchCount1, lpString2, cchCount2);
            }
        }

        NewArrayHolder<WCHAR> pBuffer = new WCHAR[cchCount1 + cchCount2];

        if (str1Index>0)
        {
            memcpy_s(pBuffer, cchCount1 * sizeof(WCHAR), lpString1, str1Index * sizeof(WCHAR));
        }

        for (; str1Index<cchCount1; str1Index++)
        {
            pBuffer[str1Index] = (lpString1[str1Index] == LATIN_LOWERCASE_I_DOT_ABOVE)
                                 ? TURKISH_CAPITAL_I_DOT_ABOVE
                                 : ((lpString1[str1Index] == LATIN_CAPITAL_DOTLESS_I)
                                    ? TURKISH_LOWERCASE_DOTLESS_I
                                    : lpString1[str1Index]);
        }

        if (str2Index>0)
        {
            memcpy_s(&pBuffer[cchCount1], cchCount2 * sizeof(WCHAR), lpString2, str2Index * sizeof(WCHAR));
        }

        for (; str2Index<cchCount2; str2Index++)
        {
            pBuffer[cchCount1 + str2Index] = (lpString2[str2Index] == LATIN_LOWERCASE_I_DOT_ABOVE)
                                             ? TURKISH_CAPITAL_I_DOT_ABOVE
                                             : ((lpString2[str2Index] == LATIN_CAPITAL_DOTLESS_I)
                                                ? TURKISH_LOWERCASE_DOTLESS_I
                                                : lpString2[str2Index]);
        }

        return ::CompareStringW(lcid, dwCmpFlags, pBuffer, cchCount1, &pBuffer[cchCount1], cchCount2);
    }

    int CompareStringEx(__in LPCWSTR lpLocaleName, __in DWORD dwCmpFlags, __in_ecount(cchCount1) LPCWSTR lpString1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR lpString2,
                                                __in int cchCount2, __in_opt LPNLSVERSIONINFO lpVersionInformation, __in_opt LPVOID lpReserved, __in_opt LPARAM lParam )
    {
        CONTRACTL
        {
            THROWS;
            GC_NOTRIGGER;
            SO_TOLERANT;
            PRECONDITION(CheckPointer(lpLocaleName));
            PRECONDITION(CheckPointer(lpString1));
            PRECONDITION(CheckPointer(lpString2));
        } CONTRACTL_END;

        // Invariant is like en-US (default table) on downlevel (pre-vista) OS's
        if (lpLocaleName[0] == W('\0'))
        {
            // For now, use "en" instead.
            lpLocaleName = W("en-US");
        }

        // Get an LCID to call the downlevel function
        LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
        if (lcid == 0)
            return 0;

        // Need to remap flags to get rid of Vista flags and replace with downlevel flags
        // (not used at this moment)
        if ((dwCmpFlags & LINGUISTIC_IGNOREDIACRITIC)!=0)
        {
            dwCmpFlags -= LINGUISTIC_IGNOREDIACRITIC;
            dwCmpFlags |= NORM_IGNORENONSPACE;
        }
        if ((dwCmpFlags & LINGUISTIC_IGNORECASE)!=0)
        {
            dwCmpFlags -= LINGUISTIC_IGNORECASE;
            dwCmpFlags |= NORM_IGNORECASE;
        }
        dwCmpFlags &= (~NORM_LINGUISTIC_CASING);

        if (((dwCmpFlags & NORM_IGNORECASE)!=0) && IsTurkicILcid(lcid))
        {
            return TurkishCompareStringIgnoreCase(lcid, dwCmpFlags, lpString1, cchCount1, lpString2, cchCount2);
        }

        return ::CompareStringW(lcid,dwCmpFlags,lpString1,cchCount1,lpString2,cchCount2);
    }

    __inline WCHAR MapCase(const WORD * table, WCHAR wch)
    {
        WORD value = table[wch >> 8];
        value = table[value + ((wch >> 4) & 0xf)];
        value = table[value + (wch & 0xf)];
        return (wch + (int)((short)value));
    }

    // since this should only be run on XP, we just use the same table as XP
    __inline WCHAR ToUpperXP(WCHAR wch)
    {
        return MapCase(s_pUppercaseIndexTableXP, wch);
    }

    INT32 CompareOrdinalIgnoreCaseHelper(__in_ecount(count) DWORD* strAChars, __in_ecount(count) DWORD* strBChars, int count)
    {
        if( count == 0)
            return 0;

        int temp = 0;

        _ASSERTE( count >0);

        // Only go through fast code path if two strings have the same alignment
        if (((size_t)strAChars & 3) == ((size_t)strBChars & 3)) {
            int unalignedBytesA = (size_t)strAChars & 3;

            _ASSERTE(unalignedBytesA == 0 || unalignedBytesA == 2);
            // First try to make the strings aligned at DWORD boundary.
            if( unalignedBytesA != 0 ) {
                LPWSTR ptr1 = (LPWSTR)strAChars;
                LPWSTR ptr2 = (LPWSTR)strBChars;

                if (*ptr1 != *ptr2) {
                    temp = ((int)ToUpperXP(*ptr1) - (int)ToUpperXP(*ptr2));
                    if( temp != 0) {
                        return temp;
                    }
                }

                --count;
                strAChars = (DWORD *)(ptr1 + 1);
                strBChars = (DWORD *)(ptr2 + 1);
            }

            // Loop comparing a DWORD at a time.
            while (count >= 2) {
                _ASSERTE(IS_ALIGNED((size_t)strAChars, 4) && IS_ALIGNED((size_t)strBChars, 4));
                if ((*strAChars - *strBChars) != 0) {
                    LPCWSTR ptr1 = (WCHAR*)strAChars;
                    LPCWSTR ptr2 = (WCHAR*)strBChars;

                    if (*ptr1 != *ptr2) {
                        temp = ((int)ToUpperXP(*ptr1) - (int)ToUpperXP(*ptr2));
                    }
                    if (temp != 0) {
                        return (temp);
                    }

                    temp = (int)ToUpperXP(*(ptr1+1)) - (int)ToUpperXP(*(ptr2+1));
                    if (temp != 0) {
                        return (temp);
                    }
                }
                ++strBChars;
                ++strAChars;
                count -= 2;
            }
        }

        // We can exit the loop when we see two different DWORDs and one of them contains surrogate
        // or they are equal after case conversion.
        // We can also exit the loop when there is no or only one character left.
        if( count == 0) {
            return 0;
        }

        // we need to handle one special case here. Say we have two strings like:
        //  A HS1 LS1 HS2 LS2  or A HS1 LS1
        //  A HS1 LS2 HS2 LS2  or A HS1 NS
        // we need to go back a char to decide the order
        LPCWSTR pwStrB = (LPWSTR)strBChars;
        LPCWSTR pwStrA = (LPWSTR)strAChars;

        temp = 0;
        while ((count--) > 0)
        {
            WCHAR charA = *pwStrA++;
            WCHAR charB = *pwStrB++;

            if( charA != charB) {
                charA = ToUpperXP(charA);
                charB = ToUpperXP(charB);

                temp = (int)charA - (int)charB;

                if (temp != 0) {
                    return (temp);
                }
            }
        }

        return 0;
    }


    int CompareStringOrdinal(__in_ecount(cchCount1) LPCWSTR string1, __in int cchCount1, __in_ecount(cchCount2) LPCWSTR string2, __in int cchCount2, __in BOOL bIgnoreCase)
    {
        // This should only happen for IgnoreCase == true.  The rest are hard coded
        if (!bIgnoreCase)
        {
            return 0;
        }

        DWORD *strAChars, *strBChars;
        strAChars = (DWORD*)string1;
        strBChars = (DWORD*)string2;

        // If the strings are the same length, compare exactly the right # of chars.
        // If they are different, compare the shortest # + 1 (the '\0').
        int count = cchCount1;
        if( count > cchCount2)
            count = cchCount2;

        INT32 ret = CompareOrdinalIgnoreCaseHelper(strAChars, strBChars, count);
        if( ret == 0) {
            ret = cchCount1 - cchCount2;
        }

        if (ret > 0)
        {
            return CSTR_GREATER_THAN;
        }
        if (ret < 0)
        {
            return CSTR_LESS_THAN;
        }

        return CSTR_EQUAL;
    }

    inline bool HasOnlyUppercaseOrLowercaseFlag(__in DWORD flags)
    {
        if(((flags & (LCMAP_UPPERCASE | LCMAP_LOWERCASE))== 0)
           || MORE_THAN_ONE(flags, LCMAP_UPPERCASE)
           || MORE_THAN_ONE(flags, LCMAP_LOWERCASE))
        {
            return false;
        }
        return true;
    }

    int TurkicICasing( __in DWORD flags,
                      __in_ecount(cchSrc) LPCWSTR source,
                      __in int cchSrc,
                      __out_ecount(cchDest) LPWSTR destination,
                      __in int cchDest)
    {
        _ASSERTE(source != NULL);
        _ASSERTE(cchSrc > 0);
        _ASSERTE(destination != NULL);
        _ASSERTE(cchSrc == cchDest);
        _ASSERTE(HasOnlyUppercaseOrLowercaseFlag(flags));

        const WORD * table;
        WCHAR dottedI, mappedDottedI;
        WCHAR dotlessI, mappedDotlessI;

        if ((flags & LCMAP_UPPERCASE) != 0)
        {
            table = s_pUppercaseIndexTableLinguisticXP;
            dottedI = LATIN_LOWERCASE_I_DOT_ABOVE;
            mappedDottedI = TURKISH_CAPITAL_I_DOT_ABOVE;
            dotlessI = TURKISH_LOWERCASE_DOTLESS_I;
            mappedDotlessI = LATIN_CAPITAL_DOTLESS_I;
        }
        else
        {
            table = s_pLowercaseIndexTableLinguisticXP;
            dottedI = TURKISH_CAPITAL_I_DOT_ABOVE;
            mappedDottedI = LATIN_LOWERCASE_I_DOT_ABOVE;
            dotlessI = LATIN_CAPITAL_DOTLESS_I;
            mappedDotlessI = TURKISH_LOWERCASE_DOTLESS_I;
        }

        for (int i = 0; i < cchSrc && i < cchDest; ++i)
        {
            if (source[i] == dottedI)
            {
                destination[i] = mappedDottedI;
            }
            else if (source[i] == dotlessI)
            {
                destination[i] = mappedDotlessI;
            }
            else {
                destination[i] = MapCase(table, source[i]);
            }
        }

        return cchSrc;
    }

    int DefaultLinguisticCasing( __in DWORD flags,
                      __in_ecount(cchSrc) LPCWSTR source,
                      __in int cchSrc,
                      __out_ecount(cchDest) LPWSTR destination,
                      __in int cchDest)
    {
        _ASSERTE(source != NULL);
        _ASSERTE(cchSrc > 0);
        _ASSERTE(destination != NULL);
        _ASSERTE(cchSrc == cchDest);
        _ASSERTE(HasOnlyUppercaseOrLowercaseFlag(flags));

        const WORD * table;

        if ((flags & LCMAP_UPPERCASE) != 0)
        {
            table = s_pUppercaseIndexTableLinguisticXP;
        }
        else
        {
            table = s_pLowercaseIndexTableLinguisticXP;
        }

        for (int i = 0; i < cchSrc && i < cchDest; ++i)
        {
            destination[i] = MapCase(table, source[i]);
        }

        return cchSrc;
    }

	__success(return != 0)
    int LinguisticCaseString(__in LCID lcid,__in DWORD flags,
                      __in_ecount(cchSrc) const WCHAR * source,
                      __in int cchSrc,
                      __out_ecount_opt(cchDest) WCHAR * destination,
                      __in int cchDest)
    {
        _ASSERTE(lcid != 0);

        if ( (cchSrc == 0) || (cchDest < 0) || (source == NULL) ||
             ((cchDest != 0) && (destination == NULL)) )
        {
            SetLastError(ERROR_INVALID_PARAMETER);
            return (0);
        }

        if(!HasOnlyUppercaseOrLowercaseFlag(flags))
        {
            SetLastError(ERROR_INVALID_FLAGS);
            return (0);
        }

        if(cchSrc < 0)
        {
            cchSrc = (int)wcslen(source);
        }

        if(destination == NULL)
        {
            return cchSrc;
        }

        if(IsTurkicILcid(lcid))
        {
            return TurkicICasing(flags,
                            source, cchSrc,
                            destination, cchDest);
        }
        else
        {
            return DefaultLinguisticCasing(flags,
                            source, cchSrc,
                            destination, cchDest);
        }

    }

#ifdef _MSC_VER
// Get rid of size OACR requirement.
#pragma warning(push)
#pragma warning(disable:25057)
#endif
    int LCMapStringEx(__in LPCWSTR lpLocaleName,
                      __in DWORD dwMapFlags,
                      __in_ecount(cchSrc) LPCWSTR lpSrcStr,
                      __in int cchSrc,
                      __out_xcount_opt(cchDest) LPWSTR lpDestStr,
                      __in int cchDest,
                      __in_opt LPNLSVERSIONINFO lpVersionInformation,
                      __in_opt LPVOID lpReserved,
                      __in_opt LPARAM lParam)
#ifdef _MSC_VER
#pragma warning(pop)
#endif
    {
        LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
        if (lcid == 0)
            return 0;

        // Need to remap flags to get rid of Vista flags and replace with downlevel flags
        // (not used at this moment)
        if ((dwMapFlags & LINGUISTIC_IGNOREDIACRITIC)!=0)
        {
            dwMapFlags -= LINGUISTIC_IGNOREDIACRITIC;
            dwMapFlags |= NORM_IGNORENONSPACE;
        }
        if ((dwMapFlags & LINGUISTIC_IGNORECASE)!=0)
        {
            dwMapFlags -= LINGUISTIC_IGNORECASE;
            dwMapFlags |= NORM_IGNORECASE;
        }
        dwMapFlags &= (~NORM_LINGUISTIC_CASING);

// START WORK_AROUND_WIN2K3_LCMAPSTRING_BUG
        if((dwMapFlags & LCMAP_LINGUISTIC_CASING) != 0
           && (dwMapFlags & (LCMAP_UPPERCASE | LCMAP_LOWERCASE)) != 0)
        {
            dwMapFlags &= (~LCMAP_LINGUISTIC_CASING);

            return LinguisticCaseString(lcid, dwMapFlags, lpSrcStr, cchSrc, lpDestStr, cchDest);
        }
// END WORK_AROUND_WIN2K3_LCMAPSTRING_BUG
        return ::LCMapStringW(lcid,dwMapFlags, lpSrcStr, cchSrc, lpDestStr, cchDest);
    }

    int FindNLSStringEx(__in LPCWSTR lpLocaleName,
                        __in DWORD dwFindNLSStringFlags,
                        __in_ecount(cchSource) LPCWSTR lpStringSource,
                        __in int cchSource,
                        __in_ecount(cchValue) LPCWSTR lpStringValue,
                        __in int cchValue,
                        __out_opt LPINT pcchFound,
                        __in_opt LPNLSVERSIONINFO lpVersionInformation,
                        __in_opt LPVOID lpReserved,
                        __in_opt LPARAM lParam)
    {
        int retValue = -1;

        if (lpLocaleName == NULL || lpStringSource == NULL || lpStringValue == NULL)
        {
            return retValue;
        }

        if (dwFindNLSStringFlags & (FIND_ENDSWITH | FIND_FROMEND))
        {
            retValue = NewApis::LastIndexOfString(lpLocaleName, lpStringSource, cchSource, lpStringValue, cchValue, (int) dwFindNLSStringFlags & FIND_NLS_STRING_FLAGS_NEGATION, dwFindNLSStringFlags & FIND_ENDSWITH);
        }
        else
        {
            retValue = NewApis::IndexOfString(lpLocaleName, lpStringSource, cchSource, lpStringValue, cchValue, (int) dwFindNLSStringFlags & FIND_NLS_STRING_FLAGS_NEGATION, dwFindNLSStringFlags & FIND_STARTSWITH);
        }
        return retValue;
    }


    // Mac and Windows <= Windows XP
    // Note: "Function" is unused, always handles sorting for now
    // Note: "dwFlags" is unused, we don't have flags for now
    // Note: "lpVersionInfo" is unused, we always presume the current version
    BOOL IsNLSDefinedString(NLS_FUNCTION Function, DWORD dwFlags, LPNLSVERSIONINFOEX lpVersionInfo, LPCWSTR pString, int nStringLen )
    {
        // Ported downlevel code from comnlsinfo.cpp

        CQuickBytes buffer;
        buffer.AllocThrows(16);
        int ich = 0;

        while(ich < nStringLen)
        {
            WCHAR wch = pString[ich];

            int dwBufSize=NewApis::LCMapStringEx(W("en-US"),LCMAP_SORTKEY|SORT_STRINGSORT,pString+ich,1,(LPWSTR)buffer.Ptr(),
                                                 (int)(buffer.Size()/sizeof(WCHAR)),NULL,NULL,0);

            if (dwBufSize == 0)
            {
                buffer.AllocThrows(buffer.Size()*2);
                continue; // try again
            }

            if (LPBYTE(buffer.Ptr())[0] == 0x1)  // no weight
            {
                //
                // Check for the NULL case and formatting characters case. Not
                // defined but valid.
                //
                switch(wch)
                {
                    case 0x0000:    // NULL
                    case 0x0640:    // TATWEEL
                    case 0x180b:    // MONGOLIAN FVS 1
                    case 0x180c:    // MONGOLIAN FVS 2
                    case 0x180d:    // MONGOLIAN FVS 3
                    case 0x180e:    // MONGOLIAN VOWEL SEPERATOR
                    case 0x200c:    // ZWNJ
                    case 0x200d:    // ZWJ
                    case 0x200e:    // LRM
                    case 0x200f:    // RLM
                    case 0x202a:    // LRE
                    case 0x202b:    // RLE
                    case 0x202c:    // PDF
                    case 0x202d:    // LRO
                    case 0x202e:    // RLO
                    case 0x206a:    // ISS
                    case 0x206b:    // SSS
                    case 0x206c:    // IAFS
                    case 0x206d:    // AAFS
                    case 0x206e:    // NATIONAL DS
                    case 0x206f:    // NOMINAL DS
                    case 0xfeff:    // ZWNBSP
                    case 0xfff9:    // IAA
                    case 0xfffa:    // IAS
                    case 0xfffb:    // IAT
                    case 0xfffc:    // ORC
                    case 0xfffd:    // RC
                        ich++;
                        continue;

                    default:
                        return (FALSE);
                }
            }

            //
            //  Eliminate Private Use characters. They are defined but cannot be considered
            //  valid because AD-style apps should not use them in identifiers.
            //
            if ((wch >= PRIVATE_USE_BEGIN) && (wch <= PRIVATE_USE_END))
            {
                return (FALSE);
            }

            //
            //  Eliminate invalid surogates pairs or single surrogates. Basically, all invalid
            //  high surrogates have aleady been filtered (above) since they are unsortable.
            //  All that is left is to check for standalone low surrogates and valid high
            //  surrogates without corresponding low surrogates.
            //

            if ((wch >= LOW_SURROGATE_START) && (wch <= LOW_SURROGATE_END))
            {
                // Leading low surrogate
                return (FALSE);
            }
            else if ((wch >= HIGH_SURROGATE_START) && (wch <= HIGH_SURROGATE_END))
            {
                // Leading high surrogate
                if ( ((ich + 1) < nStringLen) &&  // Surrogates not the last character
                     (pString[ich+1] >= LOW_SURROGATE_START) && (pString[ich+1] <= LOW_SURROGATE_END)) // Low surrogate
                {
                    // Valid surrogates pair, High followed by a low surrogate. Skip the pair!
                    ich++;
                }
                else
                {
                    // High surrogate without low surrogate, so exit.
                    return (FALSE);
                }
            }

            ich++;

        }
        return (TRUE);
    }


    int GetCalendarInfoEx(__in LPCWSTR lpLocaleName,
                          __in CALID Calendar,
                          __in_opt LPCWSTR pReserved,
                          __in CALTYPE CalType,
                          __out_ecount_opt(cchData) LPWSTR lpCalData,
                          __in int cchData,
                          __out_opt LPDWORD lpValue)
    {

        _ASSERTE((lpCalData == NULL && cchData == 0) || (lpCalData != NULL && cchData > 0));
        if ((CalType & CAL_RETURN_NUMBER))
        {
            // If CAL_RETURN_NUMBER, lpValue must be non-null and lpCalData must be null
            _ASSERTE((lpValue != NULL) && (lpCalData == NULL));
        }

        LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);

        // zh-HK has English month/day names in older OS's, so we need to fix that (pre-Vista OS's)
        if (lcid == 0x0c04 && Calendar == CAL_GREGORIAN &&
            ((CalType >= CAL_SDAYNAME1 && CalType <= CAL_SABBREVMONTHNAME13) ||
             (CalType >= CAL_SSHORTESTDAYNAME1 && CalType <= CAL_SSHORTESTDAYNAME7)))
        {
            // zh-TW has the English names for those month/day name values
            lcid = 0x0404;
        }

        if (lcid == 0)
            return 0;
        return ::GetCalendarInfoW(lcid, Calendar, CalType, lpCalData, cchData, lpValue );
    }


    namespace LegacyCallbacks
    {
        LPARAM                lDateFormatParam = NULL;
        DATEFMT_ENUMPROCEXEX  realDateCallback = NULL;

        BOOL CALLBACK EnumDateFormatsProcWrapper(__in_z LPTSTR lpDateFormatString, __in CALID Calendar)
        {
            if (realDateCallback != NULL && lDateFormatParam != NULL)
                return realDateCallback(lpDateFormatString, Calendar, lDateFormatParam);

            // Didn't have the globals, fail
            return false;
        };

        BOOL EnumDateFormatsExEx(DATEFMT_ENUMPROCEXEX lpDateFmtEnumProcExEx, LPCWSTR lpLocaleName, DWORD dwFlags, LPARAM lParam)
        {
            LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
            if (lcid == 0)
                return 0;

            DelayCreateCriticalSection();

            BOOL ret = false;
            {
                CRITSEC_Holder sCrstHolder(g_pDownlevelNlsCrst);

                // Store our real callback and lParam
                lDateFormatParam = lParam;
                realDateCallback  = lpDateFmtEnumProcExEx;
                ret = EnumDateFormatsExW(EnumDateFormatsProcWrapper, lcid, dwFlags);
            }
            return ret;
        };

        LPARAM              lTimeFormatParam = NULL;
        TIMEFMT_ENUMPROCEX  realTimeCallback = NULL;

        BOOL CALLBACK EnumTimeFormatsProcWrapper(__in_z LPTSTR lpTimeFormatString)
        {
            if (realTimeCallback != NULL && lTimeFormatParam != NULL)
                return realTimeCallback(lpTimeFormatString, lTimeFormatParam);

            // Didn't have the globals, fail
            return false;
        };

        BOOL EnumTimeFormatsEx(TIMEFMT_ENUMPROCEX lpTimeFmtEnumProcEx, LPCWSTR lpLocaleName,  DWORD dwFlags, LPARAM lParam)
        {
            LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
            if (lcid == 0)
                return 0;

            DelayCreateCriticalSection();

            BOOL ret = false;
            {
                CRITSEC_Holder sCrstHolder(g_pDownlevelNlsCrst);

                // Store our real callback and lParam
                lTimeFormatParam = lParam;
                realTimeCallback  = lpTimeFmtEnumProcEx;
                ret = EnumTimeFormatsW(EnumTimeFormatsProcWrapper, lcid, dwFlags);
            }
            return ret;
        };

        LPARAM                lCalendarInfoParam = NULL;
        CALINFO_ENUMPROCEXEX  realCalendarInfoCallback = NULL;

        BOOL CALLBACK EnumCalendarInfoProcWrapper(__in_z LPTSTR lpCalendarInfoString, __in CALID Calendar)
        {
            if (realCalendarInfoCallback != NULL && lCalendarInfoParam != NULL)
                return realCalendarInfoCallback(lpCalendarInfoString, Calendar, NULL, lCalendarInfoParam);

            // Didn't have the globals, fail
            return false;
        };

        BOOL EnumCalendarInfoExEx(CALINFO_ENUMPROCEXEX pCalInfoEnumProcExEx, LPCWSTR lpLocaleName, CALID Calendar, LPCWSTR lpReserved, CALTYPE CalType, LPARAM lParam)
        {
            LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
            if (lcid == 0)
                return 0;

            DelayCreateCriticalSection();

            BOOL ret = false;
            {
                CRITSEC_Holder sCrstHolder(g_pDownlevelNlsCrst);

                // Store our real callback and lParam
                lCalendarInfoParam = lParam;
                realCalendarInfoCallback  = pCalInfoEnumProcExEx;
                ret = EnumCalendarInfoExW(EnumCalendarInfoProcWrapper, lcid, Calendar, CalType);
            }
            return ret;
        };
    }

    // This is where we fudge data the OS doesn't know (even on Vista)
    namespace UplevelFallback
    {
        // Some properties are unknown to downlevel OS's (pre windows 7), so synthesize them
        // Pass in LCID if calling from the downlevel APIs.
        // Note that Vista gets here for neutrals as well as specifics
        // The only neutral Vista properties we support are SNAME, SPARENT & INEUTRAL (which assumes its a locale)
        // if lpLCData is NULL, caller wants required size to be returned. So we check before assigning and return
        // buffer size.
        int GetLocaleInfoEx(__in LPCWSTR lpLocaleName, __in LCID lcid, __in LCTYPE LCType, __out_ecount_opt(cchData) LPWSTR lpLCData, __in int cchData)
        {
            _ASSERTE((lpLCData == NULL && cchData == 0) || (lpLCData != NULL && cchData > 0));

            LPCWSTR useString = NULL;
            WCHAR buffer[80];

            // We don't differentiate user overrides for these types
            LCType &= ~LOCALE_NOUSEROVERRIDE;

            // TODO: NLS Arrowhead -Find better ways of handling these properties:
            // Right now we'll just fill them in with constant data
            switch (LCType)
            {
                case LOCALE_SPERCENT:                             // Percent symbol
                    useString = W("%");
                    break;
                case LOCALE_IPOSITIVEPERCENT | LOCALE_RETURN_NUMBER: // Positive percent format
                    if (lpLCData)
                    {
                        *((DWORD*)lpLCData) = 0;
                    }
                    return 2;
                case LOCALE_INEGATIVEPERCENT | LOCALE_RETURN_NUMBER: // Negative percent format
                    if (lpLCData)
                    {
                        *((DWORD*)lpLCData) = 0;
                    }
                    return 2;
                case LOCALE_SPERMILLE:                              // Per mille symbol
                    useString = W("\x2030");
                    break;
                case LOCALE_SSHORTTIME:                             // Short time format (default)
                // CultureData synthesizes short time from long time

                case LOCALE_SENGLISHDISPLAYNAME:                    // English display name (ie: Fijiian (Fiji))
                case LOCALE_SNATIVEDISPLAYNAME:                     // Native dispaly name (ie: Deutsch (Deutschland))
                // native & english names are built more easily in managed code

                //case LOCALE_SMONTHDAY:                            // month/day format
                //we get month/day patterns from the calendar data.  This would be override, but that's not assigned
                // TODO: NLS Arrowhead - in windows 7 if we add overrides
                    break;
                case LOCALE_IREADINGLAYOUT | LOCALE_RETURN_NUMBER:  // Is Right To Left?
                    // Use the RTL bit in the font signature
                    LOCALESIGNATURE LocSig;
                    if (NewApis::GetLocaleInfoEx( lpLocaleName, LOCALE_FONTSIGNATURE, (LPWSTR)&LocSig, sizeof(LocSig) / sizeof(WCHAR) ) ==
                        sizeof(LocSig) / sizeof(WCHAR))
                    {
                        // Got the locale signature information, get the isrtl bit 123 to see if its RTL.
                        if (lpLCData)
                        {
                            *((DWORD*)lpLCData) = ((LocSig.lsUsb[3] & 0x0800) != 0) ? 1 : 0;
                        }
                        return 2;
                    }
                    // Failed, just return 0
                    return 0;

                case LOCALE_SNAME:
                    // If we don't have an LCID, find one, this is < Vista or neutrals
                    if (lcid == 0) lcid = NewApis::LocaleNameToLCID(lpLocaleName,0);

                    // Make sure windows recognizes this LCID, or that its zh-Hant, sr, or bs
                    if (GetLocaleInfoW(lcid, LOCALE_IDIGITS | LOCALE_RETURN_NUMBER, NULL, 0) == 0 &&
                        lcid != 0x7c04 && lcid != 0x7c1a && lcid != 0x781a)
                    {
                        // Not a real locale, fail, don't fail for neutrals zh-Hant, sr, or bs.
                        return 0;
                    }

                    // Convert name to LCID (so we get pretty name)
                    if (lcid != 0)
                        return NewApis::LCIDToLocaleName(lcid, lpLCData, cchData, 0);
                    else
                        return 0;

                case LOCALE_INEUTRAL | LOCALE_RETURN_NUMBER:
                    // If its XP/Win2K3 or lower, then the lcid can tell us

                    if (lcid != 0)
                    {
                        if (lpLCData)
                            *((DWORD*)lpLCData) = ((lcid < 0x0400 || lcid > 0x7000) && (lcid < 0x10000)) ? 1 : 0;
                        return 2;
                    }

                    // Vista or Win2K8 fail for neutrals
                    // Note this assumes that neutral or not it is a valid locale.
                    if (NewApis::GetLocaleInfoEx(lpLocaleName, LOCALE_IDIGITS | LOCALE_RETURN_NUMBER, NULL, 0) == 0)
                    {
                        // Failed, its a neutral
                        // Note, we assumed it is a valid locale.  (Caller lookind in our name/lcid tables undoubtedly)
                        if (lpLCData)
                            *((DWORD*)lpLCData) = 1;
                    }
                    else
                    {
                        // Succeeded, its not neutral
                        if (lpLCData)
                            *((DWORD*)lpLCData) = 0;
                    }

                    // Return "success"
                    return 2;

                case LOCALE_SPARENT:
                    // Should only get here for neutrals or downlevel
                    // Downlevel only needs work if its not neutral
                    if (lcid != 0)
                    {
                        // Downlevel
                        // If its a neutral LCID then its "" Invariant
                        if ((lcid < 0x0400 || lcid > 0x7000) && (lcid < 0x10000))
                        {
                            useString = W("");
                        }
                        else
                        {
                            // Parent is same as LCID & 0x3ff, except for a few cases
                            switch (lcid)
                            {
                                case 0x0404:        // zh-TW
                                case 0x0c04:        // zh-HK
                                case 0x1404:        // zh-MO
                                    lcid = 0x7c04;  // zh-Hant
                                    break;
                                case 0x081a:        // sr-Latn-CS
                                case 0x0c1a:        // sr-Cryl-CS
                                    lcid = 0x7c1a;  // sr
                                    break;
                                case 0x201a:        // bs-Cyrl-BA
                                case 0x141a:        // bs-Latn-BA
                                    lcid = 0x781a;  // bs
                                    break;
                                default:
                                    lcid &= 0x03ff;
                                    break;
                            }

                            // Get the name from LCIDToName
                            if (NewApis::LCIDToLocaleName(lcid, buffer, NumItems(buffer), 0))
                            {
                                useString = buffer;
                            }
                        }
                    }
                    else
                    {
                        // Neutral on Vista / W2K8.  Always "" Invariant
                        // or neutral LCID
                        useString = W("");
                    }
                    break;

                case LOCALE_SNAN:
                    useString = W("NaN");
                    break;
                case LOCALE_SPOSINFINITY:
                    useString = W("Infinity");
                    break;
                case LOCALE_SNEGINFINITY:
                    useString = W("-Infinity");
                    break;
            }

            // Return number already returned, so we should have a string, else its unknown
            if (useString == NULL) return 0;

            // Copy our string to the output & return
            int size = (int)(wcslen(useString) + 1);
            // if cchData is 0, then caller wants us to return size
            if (size > cchData && cchData != 0) return 0;
            if (lpLCData)
            {
                memcpy(lpLCData, useString, size * sizeof(WCHAR));
            }

            return size;
        }

        LPCWSTR const arabicSuperShortDayNames[] =
        {
            W("\x0646"),  // Day name for Monday
            W("\x062b"),
            W("\x0631"),
            W("\x062e"),
            W("\x062c"),
            W("\x0633"),
            W("\x062d")   // Day name for Sunday
        };

        LPCWSTR const chineseSuperShortDayNames[] =
        {
            W("\x4e00"),  // Day name for Monday
            W("\x4e8c"),
            W("\x4e09"),
            W("\x56db"),
            W("\x4e94"),
            W("\x516d"),
            W("\x65e5")   // Day name for Sunday
        };

        LPCWSTR const hebrewSuperShortDayNames[] =
        {
            W("\x05d1"),  // Day name for Monday
            W("\x05d2"),
            W("\x05d3"),
            W("\x05d4"),
            W("\x05d5"),
            W("\x05e9"),
            W("\x05d0")   // Day name for Sunday
        };

        LPCWSTR const mongolianSuperShortDayNames[] =
        {
            W("\x0414\x0430"),  // Day name for Monday
            W("\x041c\x044f"),
            W("\x041b\x0445"),
            W("\x041f\x04af"),
            W("\x0411\x0430"),
            W("\x0411\x044f"),
            W("\x041d\x044f")   // Day name for Sunday
        };

        int GetCalendarInfoEx(__in_opt LPCWSTR lpLocaleName,
                              __in CALID Calendar,
                              __in_opt LPCWSTR pReserved,
                              __in CALTYPE CalType,
                              __out_ecount_opt(cchData) LPWSTR lpCalData,
                              __in int cchData,
                              __out_opt LPDWORD lpValue)
        {

            _ASSERTE((lpCalData == NULL && cchData == 0) || (lpCalData != NULL && cchData > 0));
            if ((CalType & CAL_RETURN_NUMBER))
            {
                // If CAL_RETURN_NUMBER, lpValue must be non-null and lpCalData must be null
                _ASSERTE((lpValue != NULL) && (lpCalData == NULL));
            }

            // We don't differentiate user overrides for these types
            CalType &= ~CAL_NOUSEROVERRIDE;

            LCID lcid=NewApis::LocaleNameToLCID(lpLocaleName,0);
            int ret = 0;
            LPCWSTR pUseString = W("");
            LPCWSTR const *pDays = NULL;

            //
            // The requested info is a string.
            //

            // NOTE: Genitive names will skip this and just return empty strings
            // not much we can do for those.
            switch (CalType)
            {
                case CAL_SMONTHDAY:
                    // Special cases for older locales with names that can't be truncated
                    pUseString = W("MMMM dd");

                    // Special case for CJK locales
                    if ((lcid & 0x3ff) == 0x11 ||    // Japanese
                        (lcid & 0x3ff) == 0x04)      // Chinese
                    {
                        // Japanese & Chinese
                        pUseString = W("M'\x6708'd'\x65e5'");
                    }
                    else if ((lcid & 0x3ff) == 0x012) // Korean
                    {
                        // Korean
                        pUseString = W("M'\xc6d4' d'\xc77c'");
                    }
                    break;

                case CAL_SSHORTESTDAYNAME1:
                case CAL_SSHORTESTDAYNAME2:
                case CAL_SSHORTESTDAYNAME3:
                case CAL_SSHORTESTDAYNAME4:
                case CAL_SSHORTESTDAYNAME5:
                case CAL_SSHORTESTDAYNAME6:
                case CAL_SSHORTESTDAYNAME7:
                    // Special cases for older locales with names that can't be truncated

                    // Arabic
                    if (((lcid & 0x3ff) == 0x01) && (Calendar == CAL_GREGORIAN || Calendar == CAL_HIJRI || Calendar == CAL_UMALQURA))
                        pDays = arabicSuperShortDayNames;

                    // Chinese
                    if (((lcid & 0x3ff) == 0x04) && (Calendar == CAL_GREGORIAN || Calendar == CAL_TAIWAN))
                        pDays = chineseSuperShortDayNames;

                    // Hebrew
                    if (((lcid & 0x3ff) == 0x0d) && (Calendar == CAL_GREGORIAN || Calendar == CAL_HEBREW))
                        pDays = hebrewSuperShortDayNames;

                    // Mongolian
                    if ((lcid & 0x3ff) == 0x50) pDays = mongolianSuperShortDayNames;

                    if (pDays)
                    {
                        // If we have a special case string then use that
                        pUseString = pDays[CalType - CAL_SSHORTESTDAYNAME1];
                    }
                    else
                    {
                        // If lpCalData is null they just want the size
                        // NOTE: We actually always know the size so we never ask.
                        if (lpCalData == NULL)
                        {
                            ret = 5;
                        }
                        else
                        {
                            ret = NewApis::GetCalendarInfoEx(lpLocaleName, Calendar, pReserved, CAL_SABBREVDAYNAME1 + (CalType - CAL_SSHORTESTDAYNAME1), lpCalData, cchData, lpValue);
                            if (ret > 0)
                            {
                                if (ret > 3)
                                {
                                    // Just get the first two character, and NULL-terminate it.
                                    PREFIX_ASSUME(3 < cchData); // we can assume this; otherwise call would have failed
                                    lpCalData[3] = W('\0');
                                    ret = 3;
                                }
                            }
                        }

                        // Done
                        return ret;
                    }
                    break;
                default:
                    //
                    // Not a CALTYPE that this function provides. Just returns 0.
                    //
                    return 0;
                    break;
            }

            // If we have a special case string, copy to the output & return
            ret = (int)(wcslen(pUseString) + 1);
            if (lpCalData && cchData >= ret)
            {
                // If they wanted string (not just count), then return it
                memcpy(lpCalData, pUseString, ret * sizeof(WCHAR));
            }

            return ret;
        }

        // Handle the to titlecaseflag
        int LCMapStringEx(__in LPCWSTR lpLocaleName,
                          __in DWORD dwMapFlags,
                          __in_ecount(cchSrc) LPCWSTR lpSrcStr,
                          __in int cchSrc,
                          __out_ecount_opt(cchDest) LPWSTR lpDestStr,
                          __in int cchDest,
                          __in_opt LPNLSVERSIONINFO lpVersionInformation,
                          __in_opt LPVOID lpReserved,
                          __in_opt LPARAM lParam)
        {
            // We only know about the title case flag...
            if (dwMapFlags != LCMAP_TITLECASE)
            {
                return 0;
            }

            // Should be unused, we'll just workaround it to upper case in case there's a real problem
            // Just call NewAPIs with the upper case flag
            return NewApis::LCMapStringEx(lpLocaleName, LCMAP_UPPERCASE, lpSrcStr, cchSrc,
                                          lpDestStr, cchDest,
                                          lpVersionInformation, lpReserved, lParam);
        }

    }

    static int __cdecl compareLcidToLCIDEntry ( const void *key, const void *value)
    {
        LCID lcid=*((LCID*)key);
        LCIDEntry* entry=(LCIDEntry*)value;
        return lcid-entry->lcid;
    }

    int LCIDToLocaleName(__in LCID Locale, __out_ecount_opt(cchName) LPWSTR lpName, __in int cchName, __in DWORD dwFlags)
    {
        _ASSERTE((lpName == NULL && cchName == 0) || (lpName != NULL && cchName > 0));
        if (Locale==LOCALE_INVARIANT)
        {
            if (lpName != NULL)
            {
                *lpName=0;
            }
            return 1;
        }

        LCIDEntry* entry=(LCIDEntry*)bsearch(&Locale,s_lcids,NumItems(s_lcids),sizeof(*s_lcids),compareLcidToLCIDEntry);

        if (entry == NULL)
        {
            //_ASSERTE(entry);
            return 0;
        }

        int length = 0;
        if (cchName > 0)
        {
            PREFIX_ASSUME(lpName != NULL); // checked above, but prefix can't figure it out
            wcscpy_s(lpName,cchName,entry->wszName);
            length = (int)wcslen(entry->wszName) + 1;
        }
        return length;
    }

    LCID LocaleNameToLCID(__in LPCWSTR lpName, __in DWORD dwFlags)
    {
        if (lpName == NULL || *lpName==0)
        {
            return LOCALE_INVARIANT;
        }

        // Try the last one first, just in case
        static int cachedEntry = 0;
        PREFIX_ASSUME(cachedEntry < NumItems(s_names));

        int test = VolatileLoad(&cachedEntry);
        if (_wcsicmp(lpName, s_names[test].wszName) == 0)
        {
            _ASSERTE(s_names[test].lcid != 0);
            return s_names[test].lcid;
        }

        // Just do a binary lookup for the name
        int iBottom =0;
        int iTop = NumItems(s_names) - 1;

        while (iBottom <= iTop)
        {
            int iMiddle = (iBottom + iTop) / 2;
            int result = _wcsicmp(lpName, s_names[iMiddle].wszName);
            if (result == 0)
            {
                _ASSERTE(s_names[iMiddle].lcid != 0);
                cachedEntry = iMiddle;
                return s_names[iMiddle].lcid;
            }
            if (result < 0)
            {
                // pLocaleName was < s_names[iMiddle]
                iTop = iMiddle - 1;
            }
            else
            {
                // pLocaleName was > s_names[iMiddle]
                iBottom = iMiddle + 1;
            }
        }

        // Failed, return 0
            return 0;
        }

    // Fallback for pre windows 7 machines
    int ResolveLocaleName(__in LPCWSTR lpNameToResolve, __in_ecount_opt(cchLocaleName) LPWSTR lpLocaleName, __in int cchLocaleName)
    {
        PWSTR pSpecific = NULL;
        int retVal = 0;

        // Doesn't matter for mac, for windows map the name to LCID, then ask for LOCALE_ILANGUAGE to map the
        // LCID to a specific (legacy GetLocaleInfo behavior), then map the LCID to a name
        LCID lcid = LocaleNameToLCID(lpNameToResolve, 0);
        DWORD specific;

        // Some neutrals have specific values that downlevel OS's can't provide easily:
        retVal = 2;
        switch (lcid)
        {
            case 0x0004: specific = 0x0804; break; // zh-Hans::zh-CN::0804
            case 0x000a: specific = 0x0c0a; break; // es::es-ES::0c0a
            case 0x003c: specific = 0x083c; break; // ga::ga-IE::083c
            case 0x005d: specific = 0x085d; break; // iu::iu-Latn-CA:085d
            case 0x005f: specific = 0x085f; break; // tzm::tzm-Latn-DZ:085f
            case 0x703b: specific = 0x243b; break; // smn::smn-FI::243b
            case 0x743b: specific = 0x203b; break; // sms::sms-FI::203b
            case 0x7804: specific = 0x0804; break; // zh::zh-CN::0804
            case 0x7814: specific = 0x0814; break; // nn::nn-NO::0814
            case 0x781a: specific = 0x141a; break; // bs::bs-Latn-BA:141a
            case 0x783b: specific = 0x1c3b; break; // sma::sma-SE::1c3b
            case 0x7c04: specific = 0x0c04; break; // zh-Hant::zh-HK::0c04
            case 0x7c1a: specific = 0x081a; break; // sr::sr-Latn-CS:081a (this changes in win7)
            case 0x7c2e: specific = 0x082e; break; // dsb::dsb-DE::082e
            case 0x7c3b: specific = 0x143b; break; // smj::smj-SE::143b
            default:
                // Note this won't call our Downlevel API with the undesired LOCALE_ILANGUAGE
                retVal = GetLocaleInfoW(lcid, LOCALE_ILANGUAGE | LOCALE_RETURN_NUMBER, (LPWSTR)&specific, sizeof(specific)/sizeof(WCHAR));
                break;
        }

        if (retVal > 0)
        {
            retVal = LCIDToLocaleName(specific, lpLocaleName, cchLocaleName, 0);
            if (retVal > 0)
                return retVal;
        }

        // If we didn't have a specific, then use the locale name passed in
        if (!pSpecific)
        {
            pSpecific = (PWSTR)lpNameToResolve;
        }

        // Copy our string to the output & return
        int size = (int)(wcslen(pSpecific) + 1);
        if (size > cchLocaleName) return 0;
        memcpy(lpLocaleName, pSpecific, size * sizeof(WCHAR));
        return size;
    }


    BOOL GetThreadPreferredUILanguages(__in DWORD dwFlags,
                                       __out PULONG pulNumLanguages,
                                       __out_ecount_opt(*pcchLanguagesBuffer) PWSTR pwszLanguagesBuffer,
                                       __inout PULONG pcchLanguagesBuffer)
    {
        const WCHAR str[]=W("\0");
        ULONG nBufSize=*pcchLanguagesBuffer;
        *pcchLanguagesBuffer=NumItems(str);

        if (nBufSize == 0 && pwszLanguagesBuffer == NULL)
        {
            return TRUE;
        }

        if(nBufSize<NumItems(str))
        {
            SetLastError(ERROR_INSUFFICIENT_BUFFER);
            return FALSE;
        }
        *pulNumLanguages=0;
        memcpy(pwszLanguagesBuffer,str,sizeof(str));
        return TRUE;

    }

}
#endif //  ENABLE_DOWNLEVEL_FOR_NLS


