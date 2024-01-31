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
 * The 3 character codes are the terminology codes like RFC 3066.
 *
 * "in" "iw" "ji" "jw" & "sh" have been withdrawn but are still in the
 * table but now at the end of the table because 3 character codes are
 * duplicates.  This avoids bad searches going from 3 to 2 character
 * codes.
 *
 * The range qaa-qtz is reserved for local use
 */
/* Subsequent hand addition of selected languages */
static const char * const LANGUAGES[] = {
    "aa",  "ab",  "ace", "ach", "ada", "ady", "ae",  "aeb",
    "af",  "afh", "agq", "ain", "ak",  "akk", "akz", "ale",
    "aln", "alt", "am",  "an",  "ang", "anp", "ar",  "arc",
    "arn", "aro", "arp", "arq", "ars", "arw", "ary", "arz", "as",
    "asa", "ase", "ast", "av",  "avk", "awa", "ay",  "az",
    "ba",  "bal", "ban", "bar", "bas", "bax", "bbc", "bbj",
    "be",  "bej", "bem", "bew", "bez", "bfd", "bfq", "bg",
    "bgn", "bho", "bi",  "bik", "bin", "bjn", "bkm", "bla",
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
    "ml",  "mn",  "mnc", "mni", "mo",
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
    "sv",  "sw",  "swb", "swc", "syc", "syr", "szl",
    "ta",  "tcy", "te",  "tem", "teo", "ter", "tet", "tg",
    "th",  "ti",  "tig", "tiv", "tk",  "tkl", "tkr", "tl",
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
    "in",  "iw",  "ji",  "jw",  "sh",    /* obsolete language codes */
NULL
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
/* Subsequent hand addition of selected languages */
static const char * const LANGUAGES_3[] = {
    "aar", "abk", "ace", "ach", "ada", "ady", "ave", "aeb",
    "afr", "afh", "agq", "ain", "aka", "akk", "akz", "ale",
    "aln", "alt", "amh", "arg", "ang", "anp", "ara", "arc",
    "arn", "aro", "arp", "arq", "ars", "arw", "ary", "arz", "asm",
    "asa", "ase", "ast", "ava", "avk", "awa", "aym", "aze",
    "bak", "bal", "ban", "bar", "bas", "bax", "bbc", "bbj",
    "bel", "bej", "bem", "bew", "bez", "bfd", "bfq", "bul",
    "bgn", "bho", "bis", "bik", "bin", "bjn", "bkm", "bla",
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
    "mal", "mon", "mnc", "mni", "mol",
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
    "swe", "swa", "swb", "swc", "syc", "syr", "szl",
    "tam", "tcy", "tel", "tem", "teo", "ter", "tet", "tgk",
    "tha", "tir", "tig", "tiv", "tuk", "tkl", "tkr", "tgl",
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
/*  "in",  "iw",  "ji",  "jw",  "sh",                          */
    "ind", "heb", "yid", "jaw", "srp",
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
    "CU",  "CV",  "CW",  "CX",  "CY",  "CZ",  "DE",  "DJ",  "DK",
    "DM",  "DO",  "DZ",  "EC",  "EE",  "EG",  "EH",  "ER",
    "ES",  "ET",  "FI",  "FJ",  "FK",  "FM",  "FO",  "FR",
    "GA",  "GB",  "GD",  "GE",  "GF",  "GG",  "GH",  "GI",  "GL",
    "GM",  "GN",  "GP",  "GQ",  "GR",  "GS",  "GT",  "GU",
    "GW",  "GY",  "HK",  "HM",  "HN",  "HR",  "HT",  "HU",
    "ID",  "IE",  "IL",  "IM",  "IN",  "IO",  "IQ",  "IR",  "IS",
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
    "WS",  "YE",  "YT",  "ZA",  "ZM",  "ZW",
NULL,
    "AN",  "BU", "CS", "FX", "RO", "SU", "TP", "YD", "YU", "ZR",   /* obsolete country codes */
NULL
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
/*  "CU",  "CV",  "CW",  "CX",  "CY",  "CZ",  "DE",  "DJ",  "DK",     */
    "CUB", "CPV", "CUW", "CXR", "CYP", "CZE", "DEU", "DJI", "DNK",
/*  "DM",  "DO",  "DZ",  "EC",  "EE",  "EG",  "EH",  "ER",     */
    "DMA", "DOM", "DZA", "ECU", "EST", "EGY", "ESH", "ERI",
/*  "ES",  "ET",  "FI",  "FJ",  "FK",  "FM",  "FO",  "FR",     */
    "ESP", "ETH", "FIN", "FJI", "FLK", "FSM", "FRO", "FRA",
/*  "GA",  "GB",  "GD",  "GE",  "GF",  "GG",  "GH",  "GI",  "GL",     */
    "GAB", "GBR", "GRD", "GEO", "GUF", "GGY", "GHA", "GIB", "GRL",
/*  "GM",  "GN",  "GP",  "GQ",  "GR",  "GS",  "GT",  "GU",     */
    "GMB", "GIN", "GLP", "GNQ", "GRC", "SGS", "GTM", "GUM",
/*  "GW",  "GY",  "HK",  "HM",  "HN",  "HR",  "HT",  "HU",     */
    "GNB", "GUY", "HKG", "HMD", "HND", "HRV", "HTI", "HUN",
/*  "ID",  "IE",  "IL",  "IM",  "IN",  "IO",  "IQ",  "IR",  "IS" */
    "IDN", "IRL", "ISR", "IMN", "IND", "IOT", "IRQ", "IRN", "ISL",
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
/*  "WS",  "YE",  "YT",  "ZA",  "ZM",  "ZW",          */
    "WSM", "YEM", "MYT", "ZAF", "ZMB", "ZWE",
NULL,
/*  "AN",  "BU",  "CS",  "FX",  "RO", "SU",  "TP",  "YD",  "YU",  "ZR" */
    "ANT", "BUR", "SCG", "FXX", "ROM", "SUN", "TMP", "YMD", "YUG", "ZAR",
NULL
};

/**
 * Append a code point to a string, overwriting 1 or 2 code units.
 * The offset points to the current end of the string contents
 * and is advanced (post-increment).
 * "Safe" macro, checks for a valid code point.
 * Converts code points outside of Basic Multilingual Plane into
 * corresponding surrogate pairs if sufficient space in the string.
 * High surrogate range: 0xD800 - 0xDBFF 
 * Low surrogate range: 0xDC00 - 0xDFFF
 * If the code point is not valid or a trail surrogate does not fit,
 * then isError is set to true.
 *
 * @param buffer const uint16_t * string buffer
 * @param offset string offset, must be offset<capacity
 * @param capacity size of the string buffer
 * @param codePoint code point to append
 * @param isError output bool set to true if an error occurs, otherwise not modified
 */
#define Append(buffer, offset, capacity, codePoint, isError) { \
    if ((offset) >= (capacity)) /* insufficiently sized destination buffer */ { \
        (isError) = InsufficientBuffer; \
    } else if ((uint32_t)(codePoint) > 0x10ffff) /* invalid code point */  { \
        (isError) = InvalidCodePoint; \
    } else if ((uint32_t)(codePoint) <= 0xffff) { \
        (buffer)[(offset)++] = (uint16_t)(codePoint); \
    } else { \
        (buffer)[(offset)++] = (uint16_t)(((codePoint) >> 10) + 0xd7c0); \
        (buffer)[(offset)++] = (uint16_t)(((codePoint)&0x3ff) | 0xdc00); \
    } \
}
