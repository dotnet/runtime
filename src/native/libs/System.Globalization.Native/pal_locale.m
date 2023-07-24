// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#include <stdlib.h>
#include <string.h>
#include "pal_locale_internal.h"
#include "pal_localeStringData.h"
#include "pal_localeNumberData.h"

#import <Foundation/Foundation.h>
#import <Foundation/NSFormatter.h>

char* DetectDefaultAppleLocaleName(void)
{
    NSLocale *currentLocale = [NSLocale currentLocale];
    NSString *localeName = @"";

    if (!currentLocale)
    {
        return strdup([localeName UTF8String]);
    }

    if ([currentLocale.languageCode length] > 0 && [currentLocale.countryCode length] > 0)
    {
        localeName = [NSString stringWithFormat:@"%@-%@", currentLocale.languageCode, currentLocale.countryCode];
    }
    else
    {
        localeName = currentLocale.localeIdentifier;
    }

    return strdup([localeName UTF8String]);
}

#if defined(TARGET_OSX) || defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)

const char* GlobalizationNative_GetLocaleNameNative(const char* localeName)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    const char* value = [currentLocale.localeIdentifier UTF8String];
    return strdup(value);
}

/**
 * Useful constant for the maximum size of the whole locale ID
 * (including the terminating NULL and all keywords).
 */
#define FULLNAME_CAPACITY 157

int static strnicmp(const char *str1, const char *str2, uint32_t n) {
    if (str1 == NULL) {
        if (str2 == NULL) {
            return 0;
        } else {
            return -1;
        }
    } else if (str2 == NULL) {
        return 1;
    } else {
        /* compare non-NULL strings lexically with lowercase */
        int rc;
        unsigned char c1, c2;

        for (; n--;) {
            c1 = (unsigned char)*str1;
            c2 = (unsigned char)*str2;
            if (c1 == 0) {
                if (c2 == 0) {
                    return 0;
                } else {
                    return -1;
                }
            } else if (c2 == 0) {
                return 1;
            } else {
                /* compare non-zero characters with lowercase */
                rc = (int)(unsigned char)tolower(c1) - (int)(unsigned char)tolower(c2);
                if (rc != 0) {
                    return rc;
                }
            }
            ++str1;
            ++str2;
        }
    }

    return 0;
}

void static GetParent(const char* localeID, char* parent, int32_t parentCapacity)
{
    const char *lastUnderscore;
    int32_t i;

    if (localeID == NULL)
        localeID = [NSLocale systemLocale].localeIdentifier.UTF8String;

    lastUnderscore = strrchr(localeID, '-');
    if (lastUnderscore != NULL) {
        i = (int32_t)(lastUnderscore - localeID);
    } else {
        i = 0;
    }

    if (i > 0) {
        // primary lang subtag und (undefined).
        if (strnicmp(localeID, "und-", 4) == 0) {
            localeID += 3;
            i -= 3;
            memmove(parent, localeID, MIN(i, parentCapacity));
        } else if (parent != localeID) {
            memcpy(parent, localeID, MIN(i, parentCapacity));
        }
    }

    // terminate chars 
    if (i >= 0 && i < parentCapacity)
       parent[i] = 0;

    return;
}

/* ### Data tables **************************************************/

/**
 * Table of language codes, both 2- and 3-letter, with preference
 * given to 2-letter codes where possible.  Includes 3-letter codes
 * that lack a 2-letter equivalent.
 *
 * This list must be in sorted order.  This list is returned directly
 * to the user by some API.
 *
 * This list must be kept in sync with LANGUAGES_3, with corresponding
 * entries matched.
 *
 * This table should be terminated with a NULL entry, followed by a
 * second list, and another NULL entry.  The first list is visible to
 * user code when this array is returned by API.  The second list
 * contains codes we support, but do not expose through user API.
 *
 * Notes
 *
 * Tables updated per http://lcweb.loc.gov/standards/iso639-2/ to
 * include the revisions up to 2001/7/27 *CWB*
 *
 * The 3 character codes are the terminology codes like RFC 3066.  This
 * is compatible with prior ICU codes
 *
 * "in" "iw" "ji" "jw" & "sh" have been withdrawn but are still in the
 * table but now at the end of the table because 3 character codes are
 * duplicates.  This avoids bad searches going from 3 to 2 character
 * codes.
 *
 * The range qaa-qtz is reserved for local use
 */
/* Generated using org.unicode.cldr.icu.GenerateISO639LanguageTables */
/* ISO639 table version is 20150505 */
/* Subsequent hand addition of selected languages */
static const char * const LANGUAGES[] = {
    "aa",  "ab",  "ace", "ach", "ada", "ady", "ae",  "aeb",
    "af",  "afh", "agq", "ain", "ak",  "akk", "akz", "ale",
    "aln", "alt", "am",  "an",  "ang", "anp", "ar",  "arc",
    "arn", "aro", "arp", "arq", "ars", "arw", "ary", "arz", "as",
    "asa", "ase", "ast", "av",  "avk", "awa", "ay",  "az",
    "ba",  "bal", "ban", "bar", "bas", "bax", "bbc", "bbj",
    "be",  "bej", "bem", "bew", "bez", "bfd", "bfq", "bg",
    "bgc", "bgn", "bho", "bi",  "bik", "bin", "bjn", "bkm", "bla",
    "bm",  "bn",  "bo",  "bpy", "bqi", "br",  "bra", "brh",
    "brx", "bs",  "bss", "bua", "bug", "bum", "byn", "byv",
    "ca",  "cad", "car", "cay", "cch", "ccp", "ce",  "ceb", "cgg",
    "ch",  "chb", "chg", "chk", "chm", "chn", "cho", "chp",
    "chr", "chy", "ckb", "co",  "cop", "cps", "cr",  "crh",
    "cs",  "csb", "cu",  "cv",  "cy",
    "da",  "dak", "dar", "dav", "de",  "del", "den", "dgr",
    "din", "dje", "doi", "dsb", "dtp", "dua", "dum", "dv",
    "dyo", "dyu", "dz",  "dzg",
    "ebu", "ee",  "efi", "egl", "egy", "eka", "el",  "elx",
    "en",  "enm", "eo",  "es",  "esu", "et",  "eu",  "ewo",
    "ext",
    "fa",  "fan", "fat", "ff",  "fi",  "fil", "fit", "fj",
    "fo",  "fon", "fr",  "frc", "frm", "fro", "frp", "frr",
    "frs", "fur", "fy",
    "ga",  "gaa", "gag", "gan", "gay", "gba", "gbz", "gd",
    "gez", "gil", "gl",  "glk", "gmh", "gn",  "goh", "gom",
    "gon", "gor", "got", "grb", "grc", "gsw", "gu",  "guc",
    "gur", "guz", "gv",  "gwi",
    "ha",  "hai", "hak", "haw", "he",  "hi",  "hif", "hil",
    "hit", "hmn", "ho",  "hr",  "hsb", "hsn", "ht",  "hu",
    "hup", "hy",  "hz",
    "ia",  "iba", "ibb", "id",  "ie",  "ig",  "ii",  "ik",
    "ilo", "inh", "io",  "is",  "it",  "iu",  "izh",
    "ja",  "jam", "jbo", "jgo", "jmc", "jpr", "jrb", "jut",
    "jv",
    "ka",  "kaa", "kab", "kac", "kaj", "kam", "kaw", "kbd",
    "kbl", "kcg", "kde", "kea", "ken", "kfo", "kg",  "kgp",
    "kha", "kho", "khq", "khw", "ki",  "kiu", "kj",  "kk",
    "kkj", "kl",  "kln", "km",  "kmb", "kn",  "ko",  "koi",
    "kok", "kos", "kpe", "kr",  "krc", "kri", "krj", "krl",
    "kru", "ks",  "ksb", "ksf", "ksh", "ku",  "kum", "kut",
    "kv",  "kw",  "ky",
    "la",  "lad", "lag", "lah", "lam", "lb",  "lez", "lfn",
    "lg",  "li",  "lij", "liv", "lkt", "lmo", "ln",  "lo",
    "lol", "loz", "lrc", "lt",  "ltg", "lu",  "lua", "lui",
    "lun", "luo", "lus", "luy", "lv",  "lzh", "lzz",
    "mad", "maf", "mag", "mai", "mak", "man", "mas", "mde",
    "mdf", "mdh", "mdr", "men", "mer", "mfe", "mg",  "mga",
    "mgh", "mgo", "mh",  "mi",  "mic", "min", "mis", "mk",
    "ml",  "mn",  "mnc", "mni",
    "moh", "mos", "mr",  "mrj",
    "ms",  "mt",  "mua", "mul", "mus", "mwl", "mwr", "mwv",
    "my",  "mye", "myv", "mzn",
    "na",  "nan", "nap", "naq", "nb",  "nd",  "nds", "ne",
    "new", "ng",  "nia", "niu", "njo", "nl",  "nmg", "nn",
    "nnh", "no",  "nog", "non", "nov", "nqo", "nr",  "nso",
    "nus", "nv",  "nwc", "ny",  "nym", "nyn", "nyo", "nzi",
    "oc",  "oj",  "om",  "or",  "os",  "osa", "ota",
    "pa",  "pag", "pal", "pam", "pap", "pau", "pcd", "pcm", "pdc",
    "pdt", "peo", "pfl", "phn", "pi",  "pl",  "pms", "pnt",
    "pon", "prg", "pro", "ps",  "pt",
    "qu",  "quc", "qug",
    "raj", "rap", "rar", "rgn", "rif", "rm",  "rn",  "ro",
    "rof", "rom", "rtm", "ru",  "rue", "rug", "rup",
    "rw",  "rwk",
    "sa",  "sad", "sah", "sam", "saq", "sas", "sat", "saz",
    "sba", "sbp", "sc",  "scn", "sco", "sd",  "sdc", "sdh",
    "se",  "see", "seh", "sei", "sel", "ses", "sg",  "sga",
    "sgs", "shi", "shn", "shu", "si",  "sid", "sk",
    "sl",  "sli", "sly", "sm",  "sma", "smj", "smn", "sms",
    "sn",  "snk", "so",  "sog", "sq",  "sr",  "srn", "srr",
    "ss",  "ssy", "st",  "stq", "su",  "suk", "sus", "sux",
    "sv",  "sw",  "swb", "syc", "syr", "szl",
    "ta",  "tcy", "te",  "tem", "teo", "ter", "tet", "tg",
    "th",  "ti",  "tig", "tiv", "tk",  "tkl", "tkr",
    "tlh", "tli", "tly", "tmh", "tn",  "to",  "tog", "tpi",
    "tr",  "tru", "trv", "ts",  "tsd", "tsi", "tt",  "ttt",
    "tum", "tvl", "tw",  "twq", "ty",  "tyv", "tzm",
    "udm", "ug",  "uga", "uk",  "umb", "und", "ur",  "uz",
    "vai", "ve",  "vec", "vep", "vi",  "vls", "vmf", "vo",
    "vot", "vro", "vun",
    "wa",  "wae", "wal", "war", "was", "wbp", "wo",  "wuu",
    "xal", "xh",  "xmf", "xog",
    "yao", "yap", "yav", "ybb", "yi",  "yo",  "yrl", "yue",
    "za",  "zap", "zbl", "zea", "zen", "zgh", "zh",  "zu",
    "zun", "zxx", "zza",
NULL,
    "in",  "iw",  "ji",  "jw",  "mo",  "sh",  "swc", "tl",  /* obsolete language codes */
NULL
};

static const char* const DEPRECATED_LANGUAGES[]={
    "in", "iw", "ji", "jw", "mo", NULL, NULL
};
static const char* const REPLACEMENT_LANGUAGES[]={
    "id", "he", "yi", "jv", "ro", NULL, NULL
};

/**
 * Table of 3-letter language codes.
 *
 * This is a lookup table used to convert 3-letter language codes to
 * their 2-letter equivalent, where possible.  It must be kept in sync
 * with LANGUAGES.  For all valid i, LANGUAGES[i] must refer to the
 * same language as LANGUAGES_3[i].  The commented-out lines are
 * copied from LANGUAGES to make eyeballing this baby easier.
 *
 * Where a 3-letter language code has no 2-letter equivalent, the
 * 3-letter code occupies both LANGUAGES[i] and LANGUAGES_3[i].
 *
 * This table should be terminated with a NULL entry, followed by a
 * second list, and another NULL entry.  The two lists correspond to
 * the two lists in LANGUAGES.
 */
/* Generated using org.unicode.cldr.icu.GenerateISO639LanguageTables */
/* ISO639 table version is 20150505 */
/* Subsequent hand addition of selected languages */
static const char * const LANGUAGES_3[] = {
    "aar", "abk", "ace", "ach", "ada", "ady", "ave", "aeb",
    "afr", "afh", "agq", "ain", "aka", "akk", "akz", "ale",
    "aln", "alt", "amh", "arg", "ang", "anp", "ara", "arc",
    "arn", "aro", "arp", "arq", "ars", "arw", "ary", "arz", "asm",
    "asa", "ase", "ast", "ava", "avk", "awa", "aym", "aze",
    "bak", "bal", "ban", "bar", "bas", "bax", "bbc", "bbj",
    "bel", "bej", "bem", "bew", "bez", "bfd", "bfq", "bul",
    "bgc", "bgn", "bho", "bis", "bik", "bin", "bjn", "bkm", "bla",
    "bam", "ben", "bod", "bpy", "bqi", "bre", "bra", "brh",
    "brx", "bos", "bss", "bua", "bug", "bum", "byn", "byv",
    "cat", "cad", "car", "cay", "cch", "ccp", "che", "ceb", "cgg",
    "cha", "chb", "chg", "chk", "chm", "chn", "cho", "chp",
    "chr", "chy", "ckb", "cos", "cop", "cps", "cre", "crh",
    "ces", "csb", "chu", "chv", "cym",
    "dan", "dak", "dar", "dav", "deu", "del", "den", "dgr",
    "din", "dje", "doi", "dsb", "dtp", "dua", "dum", "div",
    "dyo", "dyu", "dzo", "dzg",
    "ebu", "ewe", "efi", "egl", "egy", "eka", "ell", "elx",
    "eng", "enm", "epo", "spa", "esu", "est", "eus", "ewo",
    "ext",
    "fas", "fan", "fat", "ful", "fin", "fil", "fit", "fij",
    "fao", "fon", "fra", "frc", "frm", "fro", "frp", "frr",
    "frs", "fur", "fry",
    "gle", "gaa", "gag", "gan", "gay", "gba", "gbz", "gla",
    "gez", "gil", "glg", "glk", "gmh", "grn", "goh", "gom",
    "gon", "gor", "got", "grb", "grc", "gsw", "guj", "guc",
    "gur", "guz", "glv", "gwi",
    "hau", "hai", "hak", "haw", "heb", "hin", "hif", "hil",
    "hit", "hmn", "hmo", "hrv", "hsb", "hsn", "hat", "hun",
    "hup", "hye", "her",
    "ina", "iba", "ibb", "ind", "ile", "ibo", "iii", "ipk",
    "ilo", "inh", "ido", "isl", "ita", "iku", "izh",
    "jpn", "jam", "jbo", "jgo", "jmc", "jpr", "jrb", "jut",
    "jav",
    "kat", "kaa", "kab", "kac", "kaj", "kam", "kaw", "kbd",
    "kbl", "kcg", "kde", "kea", "ken", "kfo", "kon", "kgp",
    "kha", "kho", "khq", "khw", "kik", "kiu", "kua", "kaz",
    "kkj", "kal", "kln", "khm", "kmb", "kan", "kor", "koi",
    "kok", "kos", "kpe", "kau", "krc", "kri", "krj", "krl",
    "kru", "kas", "ksb", "ksf", "ksh", "kur", "kum", "kut",
    "kom", "cor", "kir",
    "lat", "lad", "lag", "lah", "lam", "ltz", "lez", "lfn",
    "lug", "lim", "lij", "liv", "lkt", "lmo", "lin", "lao",
    "lol", "loz", "lrc", "lit", "ltg", "lub", "lua", "lui",
    "lun", "luo", "lus", "luy", "lav", "lzh", "lzz",
    "mad", "maf", "mag", "mai", "mak", "man", "mas", "mde",
    "mdf", "mdh", "mdr", "men", "mer", "mfe", "mlg", "mga",
    "mgh", "mgo", "mah", "mri", "mic", "min", "mis", "mkd",
    "mal", "mon", "mnc", "mni",
    "moh", "mos", "mar", "mrj",
    "msa", "mlt", "mua", "mul", "mus", "mwl", "mwr", "mwv",
    "mya", "mye", "myv", "mzn",
    "nau", "nan", "nap", "naq", "nob", "nde", "nds", "nep",
    "new", "ndo", "nia", "niu", "njo", "nld", "nmg", "nno",
    "nnh", "nor", "nog", "non", "nov", "nqo", "nbl", "nso",
    "nus", "nav", "nwc", "nya", "nym", "nyn", "nyo", "nzi",
    "oci", "oji", "orm", "ori", "oss", "osa", "ota",
    "pan", "pag", "pal", "pam", "pap", "pau", "pcd", "pcm", "pdc",
    "pdt", "peo", "pfl", "phn", "pli", "pol", "pms", "pnt",
    "pon", "prg", "pro", "pus", "por",
    "que", "quc", "qug",
    "raj", "rap", "rar", "rgn", "rif", "roh", "run", "ron",
    "rof", "rom", "rtm", "rus", "rue", "rug", "rup",
    "kin", "rwk",
    "san", "sad", "sah", "sam", "saq", "sas", "sat", "saz",
    "sba", "sbp", "srd", "scn", "sco", "snd", "sdc", "sdh",
    "sme", "see", "seh", "sei", "sel", "ses", "sag", "sga",
    "sgs", "shi", "shn", "shu", "sin", "sid", "slk",
    "slv", "sli", "sly", "smo", "sma", "smj", "smn", "sms",
    "sna", "snk", "som", "sog", "sqi", "srp", "srn", "srr",
    "ssw", "ssy", "sot", "stq", "sun", "suk", "sus", "sux",
    "swe", "swa", "swb", "syc", "syr", "szl",
    "tam", "tcy", "tel", "tem", "teo", "ter", "tet", "tgk",
    "tha", "tir", "tig", "tiv", "tuk", "tkl", "tkr",
    "tlh", "tli", "tly", "tmh", "tsn", "ton", "tog", "tpi",
    "tur", "tru", "trv", "tso", "tsd", "tsi", "tat", "ttt",
    "tum", "tvl", "twi", "twq", "tah", "tyv", "tzm",
    "udm", "uig", "uga", "ukr", "umb", "und", "urd", "uzb",
    "vai", "ven", "vec", "vep", "vie", "vls", "vmf", "vol",
    "vot", "vro", "vun",
    "wln", "wae", "wal", "war", "was", "wbp", "wol", "wuu",
    "xal", "xho", "xmf", "xog",
    "yao", "yap", "yav", "ybb", "yid", "yor", "yrl", "yue",
    "zha", "zap", "zbl", "zea", "zen", "zgh", "zho", "zul",
    "zun", "zxx", "zza",
NULL,
/*  "in",  "iw",  "ji",  "jw",  "mo",  "sh",  "swc", "tl",  */
    "ind", "heb", "yid", "jaw", "mol", "srp", "swc", "tgl",
NULL
};

/**
 * Table of 2-letter country codes.
 *
 * This list must be in sorted order.  This list is returned directly
 * to the user by some API.
 *
 * This list must be kept in sync with COUNTRIES_3, with corresponding
 * entries matched.
 *
 * This table should be terminated with a NULL entry, followed by a
 * second list, and another NULL entry.  The first list is visible to
 * user code when this array is returned by API.  The second list
 * contains codes we support, but do not expose through user API.
 *
 * Notes:
 *
 * ZR(ZAR) is now CD(COD) and FX(FXX) is PS(PSE) as per
 * http://www.evertype.com/standards/iso3166/iso3166-1-en.html added
 * new codes keeping the old ones for compatibility updated to include
 * 1999/12/03 revisions *CWB*
 *
 * RO(ROM) is now RO(ROU) according to
 * http://www.iso.org/iso/en/prods-services/iso3166ma/03updates-on-iso-3166/nlv3e-rou.html
 */
static const char * const COUNTRIES[] = {
    "AD",  "AE",  "AF",  "AG",  "AI",  "AL",  "AM",
    "AO",  "AQ",  "AR",  "AS",  "AT",  "AU",  "AW",  "AX",  "AZ",
    "BA",  "BB",  "BD",  "BE",  "BF",  "BG",  "BH",  "BI",
    "BJ",  "BL",  "BM",  "BN",  "BO",  "BQ",  "BR",  "BS",  "BT",  "BV",
    "BW",  "BY",  "BZ",  "CA",  "CC",  "CD",  "CF",  "CG",
    "CH",  "CI",  "CK",  "CL",  "CM",  "CN",  "CO",  "CR",
    "CU",  "CV",  "CW",  "CX",  "CY",  "CZ",  "DE",  "DG",  "DJ",  "DK",
    "DM",  "DO",  "DZ",  "EA",  "EC",  "EE",  "EG",  "EH",  "ER",
    "ES",  "ET",  "FI",  "FJ",  "FK",  "FM",  "FO",  "FR",
    "GA",  "GB",  "GD",  "GE",  "GF",  "GG",  "GH",  "GI",  "GL",
    "GM",  "GN",  "GP",  "GQ",  "GR",  "GS",  "GT",  "GU",
    "GW",  "GY",  "HK",  "HM",  "HN",  "HR",  "HT",  "HU",
    "IC",  "ID",  "IE",  "IL",  "IM",  "IN",  "IO",  "IQ",  "IR",  "IS",
    "IT",  "JE",  "JM",  "JO",  "JP",  "KE",  "KG",  "KH",  "KI",
    "KM",  "KN",  "KP",  "KR",  "KW",  "KY",  "KZ",  "LA",
    "LB",  "LC",  "LI",  "LK",  "LR",  "LS",  "LT",  "LU",
    "LV",  "LY",  "MA",  "MC",  "MD",  "ME",  "MF",  "MG",  "MH",  "MK",
    "ML",  "MM",  "MN",  "MO",  "MP",  "MQ",  "MR",  "MS",
    "MT",  "MU",  "MV",  "MW",  "MX",  "MY",  "MZ",  "NA",
    "NC",  "NE",  "NF",  "NG",  "NI",  "NL",  "NO",  "NP",
    "NR",  "NU",  "NZ",  "OM",  "PA",  "PE",  "PF",  "PG",
    "PH",  "PK",  "PL",  "PM",  "PN",  "PR",  "PS",  "PT",
    "PW",  "PY",  "QA",  "RE",  "RO",  "RS",  "RU",  "RW",  "SA",
    "SB",  "SC",  "SD",  "SE",  "SG",  "SH",  "SI",  "SJ",
    "SK",  "SL",  "SM",  "SN",  "SO",  "SR",  "SS",  "ST",  "SV",
    "SX",  "SY",  "SZ",  "TC",  "TD",  "TF",  "TG",  "TH",  "TJ",
    "TK",  "TL",  "TM",  "TN",  "TO",  "TR",  "TT",  "TV",
    "TW",  "TZ",  "UA",  "UG",  "UM",  "US",  "UY",  "UZ",
    "VA",  "VC",  "VE",  "VG",  "VI",  "VN",  "VU",  "WF",
    "WS",  "XK",  "YE",  "YT",  "ZA",  "ZM",  "ZW",
NULL,
    "AN",  "BU", "CS", "FX", "RO", "SU", "TP", "YD", "YU", "ZR",   /* obsolete country codes */
NULL
};

static const char* const DEPRECATED_COUNTRIES[] = {
    "AN", "BU", "CS", "DD", "DY", "FX", "HV", "NH", "RH", "SU", "TP", "UK", "VD", "YD", "YU", "ZR", NULL, NULL /* deprecated country list */
};
static const char* const REPLACEMENT_COUNTRIES[] = {
/*  "AN", "BU", "CS", "DD", "DY", "FX", "HV", "NH", "RH", "SU", "TP", "UK", "VD", "YD", "YU", "ZR" */
    "CW", "MM", "RS", "DE", "BJ", "FR", "BF", "VU", "ZW", "RU", "TL", "GB", "VN", "YE", "RS", "CD", NULL, NULL  /* replacement country codes */
};

/**
 * Table of 3-letter country codes.
 *
 * This is a lookup table used to convert 3-letter country codes to
 * their 2-letter equivalent.  It must be kept in sync with COUNTRIES.
 * For all valid i, COUNTRIES[i] must refer to the same country as
 * COUNTRIES_3[i].  The commented-out lines are copied from COUNTRIES
 * to make eyeballing this baby easier.
 *
 * This table should be terminated with a NULL entry, followed by a
 * second list, and another NULL entry.  The two lists correspond to
 * the two lists in COUNTRIES.
 */
static const char * const COUNTRIES_3[] = {
/*  "AD",  "AE",  "AF",  "AG",  "AI",  "AL",  "AM",      */
    "AND", "ARE", "AFG", "ATG", "AIA", "ALB", "ARM",
/*  "AO",  "AQ",  "AR",  "AS",  "AT",  "AU",  "AW",  "AX",  "AZ",     */
    "AGO", "ATA", "ARG", "ASM", "AUT", "AUS", "ABW", "ALA", "AZE",
/*  "BA",  "BB",  "BD",  "BE",  "BF",  "BG",  "BH",  "BI",     */
    "BIH", "BRB", "BGD", "BEL", "BFA", "BGR", "BHR", "BDI",
/*  "BJ",  "BL",  "BM",  "BN",  "BO",  "BQ",  "BR",  "BS",  "BT",  "BV",     */
    "BEN", "BLM", "BMU", "BRN", "BOL", "BES", "BRA", "BHS", "BTN", "BVT",
/*  "BW",  "BY",  "BZ",  "CA",  "CC",  "CD",  "CF",  "CG",     */
    "BWA", "BLR", "BLZ", "CAN", "CCK", "COD", "CAF", "COG",
/*  "CH",  "CI",  "CK",  "CL",  "CM",  "CN",  "CO",  "CR",     */
    "CHE", "CIV", "COK", "CHL", "CMR", "CHN", "COL", "CRI",
/*  "CU",  "CV",  "CW",  "CX",  "CY",  "CZ",  "DE",  "DG",  "DJ",  "DK",     */
    "CUB", "CPV", "CUW", "CXR", "CYP", "CZE", "DEU", "DGA", "DJI", "DNK",
/*  "DM",  "DO",  "DZ",  "EA",  "EC",  "EE",  "EG",  "EH",  "ER",     */
    "DMA", "DOM", "DZA", "XEA", "ECU", "EST", "EGY", "ESH", "ERI",
/*  "ES",  "ET",  "FI",  "FJ",  "FK",  "FM",  "FO",  "FR",     */
    "ESP", "ETH", "FIN", "FJI", "FLK", "FSM", "FRO", "FRA",
/*  "GA",  "GB",  "GD",  "GE",  "GF",  "GG",  "GH",  "GI",  "GL",     */
    "GAB", "GBR", "GRD", "GEO", "GUF", "GGY", "GHA", "GIB", "GRL",
/*  "GM",  "GN",  "GP",  "GQ",  "GR",  "GS",  "GT",  "GU",     */
    "GMB", "GIN", "GLP", "GNQ", "GRC", "SGS", "GTM", "GUM",
/*  "GW",  "GY",  "HK",  "HM",  "HN",  "HR",  "HT",  "HU",     */
    "GNB", "GUY", "HKG", "HMD", "HND", "HRV", "HTI", "HUN",
/*  "IC",  "ID",  "IE",  "IL",  "IM",  "IN",  "IO",  "IQ",  "IR",  "IS" */
    "XIC", "IDN", "IRL", "ISR", "IMN", "IND", "IOT", "IRQ", "IRN", "ISL",
/*  "IT",  "JE",  "JM",  "JO",  "JP",  "KE",  "KG",  "KH",  "KI",     */
    "ITA", "JEY", "JAM", "JOR", "JPN", "KEN", "KGZ", "KHM", "KIR",
/*  "KM",  "KN",  "KP",  "KR",  "KW",  "KY",  "KZ",  "LA",     */
    "COM", "KNA", "PRK", "KOR", "KWT", "CYM", "KAZ", "LAO",
/*  "LB",  "LC",  "LI",  "LK",  "LR",  "LS",  "LT",  "LU",     */
    "LBN", "LCA", "LIE", "LKA", "LBR", "LSO", "LTU", "LUX",
/*  "LV",  "LY",  "MA",  "MC",  "MD",  "ME",  "MF",  "MG",  "MH",  "MK",     */
    "LVA", "LBY", "MAR", "MCO", "MDA", "MNE", "MAF", "MDG", "MHL", "MKD",
/*  "ML",  "MM",  "MN",  "MO",  "MP",  "MQ",  "MR",  "MS",     */
    "MLI", "MMR", "MNG", "MAC", "MNP", "MTQ", "MRT", "MSR",
/*  "MT",  "MU",  "MV",  "MW",  "MX",  "MY",  "MZ",  "NA",     */
    "MLT", "MUS", "MDV", "MWI", "MEX", "MYS", "MOZ", "NAM",
/*  "NC",  "NE",  "NF",  "NG",  "NI",  "NL",  "NO",  "NP",     */
    "NCL", "NER", "NFK", "NGA", "NIC", "NLD", "NOR", "NPL",
/*  "NR",  "NU",  "NZ",  "OM",  "PA",  "PE",  "PF",  "PG",     */
    "NRU", "NIU", "NZL", "OMN", "PAN", "PER", "PYF", "PNG",
/*  "PH",  "PK",  "PL",  "PM",  "PN",  "PR",  "PS",  "PT",     */
    "PHL", "PAK", "POL", "SPM", "PCN", "PRI", "PSE", "PRT",
/*  "PW",  "PY",  "QA",  "RE",  "RO",  "RS",  "RU",  "RW",  "SA",     */
    "PLW", "PRY", "QAT", "REU", "ROU", "SRB", "RUS", "RWA", "SAU",
/*  "SB",  "SC",  "SD",  "SE",  "SG",  "SH",  "SI",  "SJ",     */
    "SLB", "SYC", "SDN", "SWE", "SGP", "SHN", "SVN", "SJM",
/*  "SK",  "SL",  "SM",  "SN",  "SO",  "SR",  "SS",  "ST",  "SV",     */
    "SVK", "SLE", "SMR", "SEN", "SOM", "SUR", "SSD", "STP", "SLV",
/*  "SX",  "SY",  "SZ",  "TC",  "TD",  "TF",  "TG",  "TH",  "TJ",     */
    "SXM", "SYR", "SWZ", "TCA", "TCD", "ATF", "TGO", "THA", "TJK",
/*  "TK",  "TL",  "TM",  "TN",  "TO",  "TR",  "TT",  "TV",     */
    "TKL", "TLS", "TKM", "TUN", "TON", "TUR", "TTO", "TUV",
/*  "TW",  "TZ",  "UA",  "UG",  "UM",  "US",  "UY",  "UZ",     */
    "TWN", "TZA", "UKR", "UGA", "UMI", "USA", "URY", "UZB",
/*  "VA",  "VC",  "VE",  "VG",  "VI",  "VN",  "VU",  "WF",     */
    "VAT", "VCT", "VEN", "VGB", "VIR", "VNM", "VUT", "WLF",
/*  "WS",  "XK",  "YE",  "YT",  "ZA",  "ZM",  "ZW",          */
    "WSM", "XKK", "YEM", "MYT", "ZAF", "ZMB", "ZWE",
NULL,
/*  "AN",  "BU",  "CS",  "FX",  "RO", "SU",  "TP",  "YD",  "YU",  "ZR" */
    "ANT", "BUR", "SCG", "FXX", "ROM", "SUN", "TMP", "YMD", "YUG", "ZAR",
NULL
};

/**
 * Useful constant for the maximum size of the language part of a locale ID.
 * (including the terminating NULL).
 */
#define ULOC_LANG_CAPACITY 12

/**
 * Lookup 'key' in the array 'list'.  The array 'list' should contain
 * a NULL entry, followed by more entries, and a second NULL entry.
 *
 * The 'list' param should be LANGUAGES, LANGUAGES_3, COUNTRIES, or
 * COUNTRIES_3.
 */
static int16_t _findIndex(const char* const* list, const char* key)
{
    const char* const* anchor = list;
    int32_t pass = 0;

    /* Make two passes through two NULL-terminated arrays at 'list' */
    while (pass++ < 2) {
        while (*list) {
            if (strcmp(key, *list) == 0) {
                return (int16_t)(list - anchor);
            }
            list++;
        }
        ++list;     /* skip final NULL *CWB*/
    }
    return -1;
}

static const char* GetISO3Country(const char* countryCode)
{
    int16_t offset = _findIndex(COUNTRIES, countryCode);
    if (offset < 0)
        return "";

    return COUNTRIES_3[offset];
}

static const char* GetISO3Language(const char* languageCode)
{
    int16_t offset = _findIndex(LANGUAGES, languageCode);
    if (offset < 0)
        return "";
    return LANGUAGES_3[offset];
}

const char* GlobalizationNative_GetLocaleInfoStringNative(const char* localeName, LocaleStringData localeStringData)
{
    const char* value;
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    numberFormatter.locale = currentLocale;
    NSDateFormatter* dateFormatter = [[NSDateFormatter alloc] init];
    [dateFormatter setLocale:currentLocale];
    NSLocale *gbLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_GB"];

    switch (localeStringData)
    {
        ///// <summary>localized name of locale, eg "German (Germany)" in UI language (corresponds to LOCALE_SLOCALIZEDDISPLAYNAME)</summary>
        case LocaleString_LocalizedDisplayName:
        /// <summary>Display name (language + country usually) in English, eg "German (Germany)" (corresponds to LOCALE_SENGLISHDISPLAYNAME)</summary>
        case LocaleString_EnglishDisplayName:
            value = [[gbLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier] UTF8String];
           break;
        /// <summary>Display name in native locale language, eg "Deutsch (Deutschland) (corresponds to LOCALE_SNATIVEDISPLAYNAME)</summary>
        case LocaleString_NativeDisplayName:
            value = [[currentLocale displayNameForKey:NSLocaleIdentifier value:currentLocale.localeIdentifier] UTF8String];
            break;
        /// <summary>Language Display Name for a language, eg "German" in UI language (corresponds to LOCALE_SLOCALIZEDLANGUAGENAME)</summary>
        case LocaleString_LocalizedLanguageName:
        /// <summary>English name of language, eg "German" (corresponds to LOCALE_SENGLISHLANGUAGENAME)</summary>
        case LocaleString_EnglishLanguageName:
            value = [[gbLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
            break;
        /// <summary>native name of language, eg "Deutsch" (corresponds to LOCALE_SNATIVELANGUAGENAME)</summary>
        case LocaleString_NativeLanguageName:
            value = [[currentLocale localizedStringForLanguageCode:currentLocale.languageCode] UTF8String];
           break;
        /// <summary>English name of country, eg "Germany" (corresponds to LOCALE_SENGLISHCOUNTRYNAME)</summary>
        case LocaleString_EnglishCountryName:
            value = [[gbLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        /// <summary>native name of country, eg "Deutschland" (corresponds to LOCALE_SNATIVECOUNTRYNAME)</summary>
        case LocaleString_NativeCountryName:
            value = [[currentLocale localizedStringForCountryCode:currentLocale.countryCode] UTF8String];
            break;
        case LocaleString_ThousandSeparator:
            value = [currentLocale.groupingSeparator UTF8String];
            break;
        case LocaleString_DecimalSeparator:
            value = [currentLocale.decimalSeparator UTF8String];
            // or value = [[currentLocale objectForKey:NSLocaleDecimalSeparator] UTF8String];
            break;
        case LocaleString_Digits:
        {
            NSString *digitsString = @"0123456789";
            NSNumberFormatter *nf1 = [[NSNumberFormatter alloc] init];
            [nf1 setLocale:currentLocale];

            NSNumber *newNum = [nf1 numberFromString:digitsString];
            value = [[newNum stringValue] UTF8String];
            break;
        }
        case LocaleString_MonetarySymbol:
            value = [currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_Iso4217MonetarySymbol:
            // check if this is correct, check currencyISOCode
            value = [currentLocale.currencySymbol UTF8String];
            break;
        case LocaleString_CurrencyEnglishName:
            value = [[gbLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_CurrencyNativeName:
            value = [[currentLocale localizedStringForCurrencyCode:currentLocale.currencyCode] UTF8String];
            break;
        case LocaleString_MonetaryDecimalSeparator:
            value = [numberFormatter.currencyDecimalSeparator UTF8String];
            break;
        case LocaleString_MonetaryThousandSeparator:
            value = [numberFormatter.currencyGroupingSeparator UTF8String];
            break;
        case LocaleString_AMDesignator:
            value = [dateFormatter.AMSymbol UTF8String];
            break;
        case LocaleString_PMDesignator:
            value = [dateFormatter.PMSymbol UTF8String];
            break;
        case LocaleString_PositiveSign:
            value = [numberFormatter.plusSign UTF8String];
            break;
        case LocaleString_NegativeSign:
            value = [numberFormatter.minusSign UTF8String];
            break;
        case LocaleString_Iso639LanguageTwoLetterName:
            value = [[currentLocale objectForKey:NSLocaleLanguageCode] UTF8String];
            break;
        case LocaleString_Iso639LanguageThreeLetterName:
        {
            NSString *iso639_2 = [currentLocale objectForKey:NSLocaleLanguageCode];
            value = GetISO3Language([iso639_2 UTF8String]);
            break;
        }
        case LocaleString_Iso3166CountryName:
            value = [[currentLocale objectForKey:NSLocaleCountryCode] UTF8String];
            break;
        case LocaleString_Iso3166CountryName2:
        {
            const char *countryCode = strdup([[currentLocale objectForKey:NSLocaleCountryCode] UTF8String]);
            value = GetISO3Country(countryCode);
            break;
        }
        case LocaleString_NaNSymbol:
            value = [numberFormatter.notANumberSymbol UTF8String];
            break;
        case LocaleString_PositiveInfinitySymbol:
            value = [numberFormatter.positiveInfinitySymbol UTF8String];
            break;
        case LocaleString_NegativeInfinitySymbol:
            value = [numberFormatter.negativeInfinitySymbol UTF8String];
            break;
        case LocaleString_PercentSymbol:
            value = [numberFormatter.percentSymbol UTF8String];
            break;
        case LocaleString_PerMilleSymbol:
            value = [numberFormatter.perMillSymbol UTF8String];
            break;
        case LocaleString_ParentName:
        {
            char localeNameTemp[FULLNAME_CAPACITY];
            const char* lName = [currentLocale.localeIdentifier UTF8String];
            GetParent(lName, localeNameTemp, FULLNAME_CAPACITY);
            value = strdup(localeNameTemp);
            break;
        }
        default:
            value = "";
            break;
    }

    return value ? strdup(value) : "";
}

// invariant character definitions
#define CHAR_CURRENCY ((char)0x00A4)   // international currency
#define CHAR_SPACE ((char)0x0020)      // space
#define CHAR_NBSPACE ((char)0x00A0)    // no-break space
#define CHAR_DIGIT ((char)0x0023)      // '#'
#define CHAR_MINUS ((char)0x002D)      // '-'
#define CHAR_PERCENT ((char)0x0025)    // '%'
#define CHAR_OPENPAREN ((char)0x0028)  // '('
#define CHAR_CLOSEPAREN ((char)0x0029) // ')'
#define CHAR_ZERO ((char)0x0030)       // '0'

/*
Function:
NormalizeNumericPattern

Returns a numeric string pattern in a format that we can match against the
appropriate managed pattern. Examples:
For PositiveMonetaryNumberFormat "¤#,##0.00" becomes "Cn"
For NegativeNumberFormat "#,##0.00;(#,##0.00)" becomes "(n)"
*/
static char* NormalizeNumericPattern(const char* srcPattern, int isNegative)
{
    int iStart = 0;
    int iEnd = strlen(srcPattern);

    // ';'  separates positive and negative subpatterns.
    // When there is no explicit negative subpattern,
    // an implicit negative subpattern is formed from the positive pattern with a prefixed '-'.
    char * ptrNegativePattern = strrchr(srcPattern,';');
    if (ptrNegativePattern)
    {
        int32_t iNegativePatternStart = ptrNegativePattern - srcPattern;
        if (isNegative)
        {
            iStart = iNegativePatternStart + 1;
        }
        else
        {
            iEnd = iNegativePatternStart - 1;
        }
    }

    int minusAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        switch (srcPattern[i])
        {
            case CHAR_MINUS:
            case CHAR_OPENPAREN:
            case CHAR_CLOSEPAREN:
                minusAdded = true;
                break;
        }

        if (minusAdded)
           break;
    }

    // international currency symbol (CHAR_CURRENCY)
    // The positive pattern comes first, then an optional negative pattern
    // separated by a semicolon
    // A destPattern example: "(C n)" where C represents the currency symbol, and
    // n is the number
    char* destPattern;
    int index = 0;

    // if there is no negative subpattern, prefix the minus sign
    if (isNegative && !minusAdded)
    {
        int length = (iEnd - iStart) + 2;
        destPattern = (char*)calloc((size_t)length, sizeof(char));
        if (!destPattern)
        {
            return NULL;
        }
        destPattern[index++] = '-';
    }
    else
    {
        int length = (iEnd - iStart) + 1;
        destPattern = (char*)calloc((size_t)length, sizeof(char));
        if (!destPattern)
        {
            return NULL;
        }
    }

    int digitAdded = false;
    int currencyAdded = false;
    int spaceAdded = false;

    for (int i = iStart; i <= iEnd; i++)
    {
        char ch = srcPattern[i];
        switch (ch)
        {
            case CHAR_DIGIT:
            case CHAR_ZERO:
                if (!digitAdded)
                {
                    digitAdded = true;
                    destPattern[index++] = 'n';
                }
                break;

            case CHAR_CURRENCY:
                if (!currencyAdded)
                {
                    currencyAdded = true;
                    destPattern[index++] = 'C';
                }
                break;

            case CHAR_SPACE:
            case CHAR_NBSPACE:
                if (!spaceAdded)
                {
                    spaceAdded = true;
                    destPattern[index++] = ' ';
                }
                break;

            case CHAR_MINUS:
            case CHAR_OPENPAREN:
            case CHAR_CLOSEPAREN:
            case CHAR_PERCENT:
                destPattern[index++] = ch;
                break;
        }
    }

    const int MAX_DOTNET_NUMERIC_PATTERN_LENGTH = 6; // example: "(C n)" plus terminator

    if (destPattern[0] == '\0' || strlen (destPattern) >= MAX_DOTNET_NUMERIC_PATTERN_LENGTH)
    {
        free (destPattern);
        return NULL;
    }

    return destPattern;
}

/*
Function:
GetNumericPattern

Determines the pattern from the decimalFormat and returns the matching pattern's
index from patterns[].
Returns index -1 if no pattern is found.
*/
static int GetPatternIndex(char* normalizedPattern,const char* patterns[], int patternsCount)
{
    const int INVALID_FORMAT = -1;

    if (!normalizedPattern)
    {
        return INVALID_FORMAT;
    }

    for (int i = 0; i < patternsCount; i++)
    {
        if (strcmp(normalizedPattern, patterns[i]) == 0)
        {
            free(normalizedPattern);
            return i;
        }
    }

    assert(false); // should have found a valid pattern

    free(normalizedPattern);
    return INVALID_FORMAT;
}

static int32_t GetValueForNumberFormat(NSLocale *currentLocale, LocaleNumberData localeNumberData)
{
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    numberFormatter.locale = currentLocale;
    const char *pFormat;
    int32_t value;

    switch(localeNumberData)
    {
        case LocaleNumber_PositiveMonetaryNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            static const char* Patterns[] = {"Cn", "nC", "C n", "n C"};
            pFormat = [[numberFormatter positiveFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, false);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativeMonetaryNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            static const char* Patterns[] = {"(Cn)", "-Cn", "C-n", "Cn-", "(nC)", "-nC", "n-C", "nC-", "-n C",
                                             "-C n", "n C-", "C n-", "C -n", "n- C", "(C n)", "(n C)", "C- n" };
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativeNumberFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
            static const char* Patterns[] = {"(n)", "-n", "- n", "n-", "n -"};
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_NegativePercentFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterPercentStyle;
            static const char* Patterns[] = {"-n %", "-n%", "-%n", "%-n", "%n-", "n-%", "n%-", "-% n", "n %-", "% n-", "% -n", "n- %"};
            pFormat = [[numberFormatter negativeFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, true);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        case LocaleNumber_PositivePercentFormat:
        {
            numberFormatter.numberStyle = NSNumberFormatterPercentStyle;
            static const char* Patterns[] = {"n %", "n%", "%n", "% n"};
            pFormat = [[numberFormatter positiveFormat] UTF8String];
            char* normalizedPattern = NormalizeNumericPattern(pFormat, false);
            value = GetPatternIndex(normalizedPattern, Patterns, sizeof(Patterns)/sizeof(Patterns[0]));
            break;
        }
        default:
            return -1;
    }

    return value;
}

int32_t GlobalizationNative_GetLocaleInfoIntNative(const char* localeName, LocaleNumberData localeNumberData)
{
#ifndef NDEBUG
    bool isSuccess = true;
#endif
    int32_t value;
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];

    switch (localeNumberData)
    {
        case LocaleNumber_MeasurementSystem:
        {
            const char *measurementSystem = [[currentLocale objectForKey:NSLocaleMeasurementSystem] UTF8String];
            NSLocale *usLocale = [[NSLocale alloc] initWithLocaleIdentifier:@"en_US"];
            const char *us_measurementSystem = [[usLocale objectForKey:NSLocaleMeasurementSystem] UTF8String];
            value = (measurementSystem == us_measurementSystem) ? 1 : 0;
            break;
        }
        case LocaleNumber_FractionalDigitsCount:
        {
            NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
            numberFormatter.locale = currentLocale;
            numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
            value = (int32_t)numberFormatter.maximumFractionDigits;
            break;
        }
        case LocaleNumber_MonetaryFractionalDigitsCount:
        {
            NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
            numberFormatter.locale = currentLocale;
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            value = (int32_t)numberFormatter.maximumFractionDigits;
            break;
        }
        case LocaleNumber_PositiveMonetaryNumberFormat:
        case LocaleNumber_NegativeMonetaryNumberFormat:
        case LocaleNumber_NegativeNumberFormat:
        case LocaleNumber_NegativePercentFormat:
        case LocaleNumber_PositivePercentFormat:
        {
            value = GetValueForNumberFormat(currentLocale, localeNumberData);
#ifndef NDEBUG
            if (value < 0)
            {
                isSuccess = false;
            }
#endif
            break;
        }
        case LocaleNumber_FirstWeekOfYear:
        {
            NSCalendar *calendar = [currentLocale objectForKey:NSLocaleCalendar];
            int minDaysInWeek = (int32_t)[calendar minimumDaysInFirstWeek];
            if (minDaysInWeek == 1)
            {
                value = WeekRule_FirstDay;
            }
            else if (minDaysInWeek == 7)
            {
                value = WeekRule_FirstFullWeek;
            }
            else if (minDaysInWeek >= 4)
            {
                value = WeekRule_FirstFourDayWeek;
            }
            else
            {
                value = -1;
#ifndef NDEBUG
                isSuccess = false;
#endif
            }
            break;
        }
        case LocaleNumber_ReadingLayout:
        {
            NSLocaleLanguageDirection langDir = [NSLocale characterDirectionForLanguage:[currentLocale objectForKey:NSLocaleLanguageCode]];
            //  0 - Left to right (such as en-US)
            //  1 - Right to left (such as arabic locales)
            value = NSLocaleLanguageDirectionRightToLeft == langDir ? 1 : 0;
            break;
        }
        case LocaleNumber_FirstDayofWeek:
        {
            NSCalendar *calendar = [currentLocale objectForKey:NSLocaleCalendar];
            value = [calendar firstWeekday] - 1; // .NET is 0-based and in Apple is 1-based;
            break;
        }
        default:
            value = -1;
#ifndef NDEBUG
            isSuccess = false;
#endif
            break;
    }

    assert(isSuccess);

    return value;
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative

Returns primary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoPrimaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    numberFormatter.locale = currentLocale;

    switch (localeGroupingData)
    {
        case LocaleNumber_Digit:
            numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
            break;
        case LocaleNumber_Monetary:
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            break;
        default:
            assert(false);
            break;
    }
    return [numberFormatter groupingSize];
}

/*
PAL Function:
GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative

Returns secondary grouping size for decimal and currency
*/
int32_t GlobalizationNative_GetLocaleInfoSecondaryGroupingSizeNative(const char* localeName, LocaleNumberData localeGroupingData)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    NSNumberFormatter *numberFormatter = [[NSNumberFormatter alloc] init];
    numberFormatter.locale = currentLocale;

    switch (localeGroupingData)
    {
        case LocaleNumber_Digit:
            numberFormatter.numberStyle = NSNumberFormatterDecimalStyle;
            break;
        case LocaleNumber_Monetary:
            numberFormatter.numberStyle = NSNumberFormatterCurrencyStyle;
            break;
        default:
            assert(false);
            break;
    }

    return [numberFormatter secondaryGroupingSize];
}

/*
PAL Function:
GlobalizationNative_GetLocaleTimeFormatNative

Returns time format information (in native format, it needs to be converted to .NET's format).
*/
const char* GlobalizationNative_GetLocaleTimeFormatNative(const char* localeName, int shortFormat)
{
    NSString *locName = [NSString stringWithFormat:@"%s", localeName];
    NSLocale *currentLocale = [[NSLocale alloc] initWithLocaleIdentifier:locName];
    NSDateFormatter* dateFormatter = [[NSDateFormatter alloc] init];
    [dateFormatter setLocale:currentLocale];

    if (shortFormat != 0)
    {
        [dateFormatter setTimeStyle:NSDateFormatterShortStyle];
    }
    else
    {
        [dateFormatter setTimeStyle:NSDateFormatterMediumStyle];
    }

    return strdup([[dateFormatter dateFormat] UTF8String]);
}

#endif

#if defined(TARGET_MACCATALYST) || defined(TARGET_IOS) || defined(TARGET_TVOS)
const char* GlobalizationNative_GetICUDataPathRelativeToAppBundleRoot(const char* path)
{
    NSString *bundlePath = [[NSBundle mainBundle] bundlePath];
    NSString *dataPath = [bundlePath stringByAppendingPathComponent: [NSString stringWithFormat:@"%s", path]];

    return strdup([dataPath UTF8String]);
}

const char* GlobalizationNative_GetICUDataPathFallback(void)
{
    NSString *dataPath = [[NSBundle mainBundle] pathForResource:@"icudt" ofType:@"dat"];
    return strdup([dataPath UTF8String]);
}
#endif
