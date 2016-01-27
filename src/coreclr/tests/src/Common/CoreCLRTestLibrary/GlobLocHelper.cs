// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
//

using System;
using System.Text;
using System.Security;
using System.Threading;
using System.Reflection;
using System.Globalization;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace TestLibrary
{
#if !FEATURE_NOPINVOKES
    public static class GlobLocHelper
    {
        #region OS Interop

        #region Windows P/Invokes
        [DllImport("api-ms-win-core-string-l1-1-0.dll")]
        private extern static int CompareStringW(int lcid, int flags, [MarshalAs(UnmanagedType.LPWStr)] string str1, int length1, [MarshalAs(UnmanagedType.LPWStr)]string str2, int length2);

        [DllImportAttribute("api-ms-win-core-string-l1-1-0.dll")]
        public static extern int CompareStringEx([MarshalAs(UnmanagedType.LPWStr)]string lpLocaleName, int dwCmpFlags, [MarshalAs(UnmanagedType.LPWStr)]string lpString1, int cchCount1, [MarshalAs(UnmanagedType.LPWStr)] string lpString2, int cchCount2, System.IntPtr lpVersionInformation, System.IntPtr lpReserved, IntPtr lParam);

        [DllImportAttribute("api-ms-win-core-string-l1-1-0.dll")]
        public static extern int CompareStringOrdinal([MarshalAs(UnmanagedType.LPWStr)]string lpString1, int cchCount1, [MarshalAs(UnmanagedType.LPWStr)] string lpString2, int cchCount2, bool bIgnoreCase);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll")]
        private extern static int LCMapStringW(int lcid, int flags, [MarshalAs(UnmanagedType.LPWStr)] string str1, int length1, [MarshalAs(UnmanagedType.LPWStr)] string str2, int length2);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll")]
        private extern static int LCMapStringEx([MarshalAs(UnmanagedType.LPWStr)]string lpLocaleName, int dwMapFlags, [MarshalAs(UnmanagedType.LPWStr)] string lpSrcStr, int cchSrc, [MarshalAs(UnmanagedType.LPWStr)] string lpDestStr, int cchDest, System.IntPtr lpVersionInformation, System.IntPtr lpReserved, IntPtr lParam);

        [DllImport("api-ms-win-core-datetime-l1-1-0.dll", CharSet = CharSet.Unicode)]
        static extern int GetDateFormatW(uint locale, uint dwFlags, ref SystemTime date, string format, StringBuilder sb, int sbSize);

        [DllImport("api-ms-win-core-datetime-l1-1-0.dll", CharSet = CharSet.Unicode)]
        static extern int GetTimeFormatW(uint locale, uint dwFlags, ref SystemTime time, string format, StringBuilder sb, int sbSize);

        [DllImport("api-ms-win-core-localization-l1-2-0.dll", EntryPoint = "GetLocaleInfoW", CharSet = CharSet.Unicode)]
        static extern int GetLocaleInfo(uint Locale, uint LCType,
           [Out] StringBuilder lpLCData, int cchData);

        public delegate bool DateTimeFormatDelegate(string infoStr);

        [DllImport("api-ms-win-core-errorhandling-l1-1-0.dll")]
        static extern int GetLastError();
        #endregion

        #region Interop Structs

        internal struct CFRange
        {
#if X86
			internal int x;
            internal int y;
#else
            internal long x;
            internal long y;
#endif

#if X86
            public CFRange(int a, int b)
#else
            public CFRange(long a, long b)
#endif
            {
                x = a; y = b;
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct SystemTime
        {
            [MarshalAs(UnmanagedType.U2)]
            public ushort Year;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Month;
            [MarshalAs(UnmanagedType.U2)]
            public ushort DayOfWeek;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Day;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Hour;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Minute;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Second;
            [MarshalAs(UnmanagedType.U2)]
            public ushort Millisecond;

            public SystemTime(DateTime dt)
            {
                Year = (ushort)dt.Year;
                Month = (ushort)dt.Month;
                DayOfWeek = (ushort)dt.DayOfWeek;
                Day = (ushort)dt.Day;
                Hour = (ushort)dt.Hour;
                Minute = (ushort)dt.Minute;
                Second = (ushort)dt.Second;
                Millisecond = (ushort)dt.Millisecond;
            }
        }
        #endregion

        #region Constants, Enums, and Constant-Retrieving Methods
        [SecuritySafeCritical]
        private static unsafe IntPtr GetCFFunction(string input)
        {
#if REMOVE
            string bundleName = "com.apple.CoreFoundation";
            IntPtr cfBundleName = CFStringCreateWithCharacters(IntPtr.Zero, bundleName, bundleName.Length);
            IntPtr cfDataName = CFStringCreateWithCharacters(IntPtr.Zero, input, input.Length);

            IntPtr cfBundle = CFBundleGetBundleWithIdentifier(cfBundleName);
            IntPtr ret = (IntPtr)(CFBundleGetFunctionPointerForName(cfBundle, cfDataName));

            // cfBundle can't be released within this function because we don't "own it". Release it may cause memory corruption.
            // http://developer.apple.com/documentation/CoreFoundation/Conceptual/CFMemoryMgmt/Concepts/Ownership.html#//apple_ref/doc/uid/20001148-SW1

            if (cfBundleName != IntPtr.Zero)
                CFRelease(cfBundleName);

            if (cfDataName != IntPtr.Zero)
                CFRelease(cfDataName);

            return ret;
#else
            throw new NotImplementedException();
#endif
        }
        [SecuritySafeCritical]
        private static unsafe IntPtr GetCFString(string input)
        {
#if TEMP
            string bundleName = "com.apple.CoreFoundation";
            IntPtr cfBundleName = CFStringCreateWithCharacters(IntPtr.Zero, bundleName, bundleName.Length);
            IntPtr cfDataName = CFStringCreateWithCharacters(IntPtr.Zero, input, input.Length);

            IntPtr cfBundle = CFBundleGetBundleWithIdentifier(cfBundleName);
#if X86
            IntPtr ret = (IntPtr)(*(int*)(CFBundleGetDataPointerForName(cfBundle, cfDataName)));
#else
            long ptr = *((long*)(CFBundleGetDataPointerForName(cfBundle, cfDataName)));
            IntPtr ret = new IntPtr(ptr);
#endif //X86

            // cfBundle can't be released within this function because we don't "own it". Release it may cause memory corruption.
            // http://developer.apple.com/documentation/CoreFoundation/Conceptual/CFMemoryMgmt/Concepts/Ownership.html#//apple_ref/doc/uid/20001148-SW1

            if (cfBundleName != IntPtr.Zero)
                CFRelease(cfBundleName);

            if (cfDataName != IntPtr.Zero)
                CFRelease(cfDataName);

            return ret;
#else
            throw new NotImplementedException();
#endif
        }

        private const int NORM_IGNORECASE = 0x00000001;       // Ignores case.  
        private const int NORM_IGNOREKANATYPE = 0x00010000;       // Does not differentiate between Hiragana and Katakana characters. Corresponding Hiragana and Katakana will compare as equal. 
        private const int NORM_IGNORENONSPACE = 0x00000002;       // Ignores nonspacing. This flag also removes Japanese accent characters. 
        private const int NORM_IGNORESYMBOLS = 0x00000004;       // Ignores symbols.  
        private const int NORM_IGNOREWIDTH = 0x00020000;       // Does not differentiate between a single-byte character and the same character as a double-byte character. 
        private const int NORM_LINGUISTIC_CASING = 0x08000000; // for Ordinal comparison
        private const int SORT_STRINGSORT = 0x00001000;       // Treats punctuation the same as symbols. 

        private const int LCMAP_UPPERCASE = 0x00000200;
        private const int LCMAP_LOWERCASE = 0x00000100;
        private const int LCMAP_LINGUISTIC_CASING = 0x01000000;

        private const long WIN32_CF_TICKS_RATIO = 10000000;
        private const long WIN32_CF_TICKS_DELTA = 63113904000;

        private static char[] allStandardFormats = 
        {
            'd', 'D', 'f', 'F', 'g', 'G', 
            'm', 'M', 'o', 'O', 'r', 'R', 
            's', 't', 'T', 'u', 'U', 'y', 'Y',
        };

        [Flags]
        private enum CFStringNormalizationForm
        {
            kCFStringNormalizationFormD = 0,
            kCFStringNormalizationFormKD = 1,
            kCFStringNormalizationFormC = 2,
            kCFStringNormalizationFormKC = 3
        };

        [Flags]
        private enum TimeFormatFlags
        {
            TIME_NOMINUTESORSECONDS = 1,
            TIME_NOSECONDS = 2,
            TIME_NOTIMEMARKER = 4,
            TIME_FORCE24HOURFORMAT = 8,
            TIME_LONGTIME = 0,
            TIME_SHORTTIME = 2
        };

        [Flags]
        private enum DateFormatFlags
        {
            DATE_SHORTDATE = 1,
            DATE_LONGDATE = 2,
            DATE_YEARMONTH = 8
        };

        private enum CFNumberFormatterStyle
        {
            kCFNumberFormatterNoStyle = 0,
            kCFNumberFormatterDecimalStyle = 1,
            kCFNumberFormatterCurrencyStyle = 2,
            kCFNumberFormatterPercentStyle = 3,
            kCFNumberFormatterScientificStyle = 4,
            kCFNumberFormatterSpellOutStyle = 5
        } ;


        [Flags]
        private enum CFStringCompareFlags
        {
            kCFCompareCaseInsensitive = 1,
            kCFCompareBackwards = 4,
            kCFCompareAnchored = 8,
            kCFCompareNonliteral = 16,
            kCFCompareLocalized = 32,
            kCFCompareNumerically = 64
        };

        private enum CFDateFormatterStyle
        {
            kCFDateFormatterNoStyle = 0,
            kCFDateFormatterShortStyle = 1,
            kCFDateFormatterMediumStyle = 2,
            kCFDateFormatterLongStyle = 3,
            kCFDateFormatterFullStyle = 4
        };

        private enum CFNumberType
        {
            kCFNumberSInt8Type = 1,
            kCFNumberSInt16Type = 2,
            kCFNumberSInt32Type = 3,
            kCFNumberSInt64Type = 4,
            kCFNumberFloat32Type = 5,
            kCFNumberFloat64Type = 6,
            kCFNumberCharType = 7,
            kCFNumberShortType = 8,
            kCFNumberIntType = 9,
            kCFNumberLongType = 10,
            kCFNumberLongLongType = 11,
            kCFNumberFloatType = 12,
            kCFNumberDoubleType = 13,
            kCFNumberCFIndexType = 14,
            kCFNumberMaxType = 14
        };

        private static bool IsIntType(CFNumberType numType)
        {
            return (numType == CFNumberType.kCFNumberSInt8Type ||
                    numType == CFNumberType.kCFNumberSInt16Type ||
                    numType == CFNumberType.kCFNumberSInt32Type ||
                    numType == CFNumberType.kCFNumberSInt64Type ||
                    numType == CFNumberType.kCFNumberCharType ||
                    numType == CFNumberType.kCFNumberShortType ||
                    numType == CFNumberType.kCFNumberIntType ||
                    numType == CFNumberType.kCFNumberLongType ||
                    numType == CFNumberType.kCFNumberLongLongType);
        }

        private enum LCType
        {
            LOCALE_SENGLANGUAGE = 0x00001001, /* English name of language */
            LOCALE_SABBREVLANGNAME = 0x00000003, /* abbreviated language name */
            LOCALE_SNATIVELANGNAME = 0x00000004, /* native name of language */

            LOCALE_SENGCOUNTRY = 0x00001002, /* English name of country */
            LOCALE_SABBREVCTRYNAME = 0x00000007, /* abbreviated country name */
            LOCALE_SNATIVECTRYNAME = 0x00000008, /* native name of country */

            LOCALE_SLIST = 0x0000000C, /* list item separator */
            LOCALE_IMEASURE = 0x0000000D, /* 0 = metric, 1 = US */

            LOCALE_SDECIMAL = 0x0000000E, /* decimal separator */
            LOCALE_STHOUSAND = 0x0000000F, /* thousand separator */
            LOCALE_SGROUPING = 0x00000010, /* digit grouping */
            LOCALE_IDIGITS = 0x00000011, /* number of fractional digits */
            LOCALE_ILZERO = 0x00000012, /* leading zeros for decimal */
            LOCALE_INEGNUMBER = 0x00001010, /* negative number mode */
            LOCALE_SNATIVEDIGITS = 0x00000013, /* native ascii 0-9 */

            LOCALE_SCURRENCY = 0x00000014, /* local monetary symbol */
            LOCALE_SINTLSYMBOL = 0x00000015, /* intl monetary symbol */
            LOCALE_SMONDECIMALSEP = 0x00000016, /* monetary decimal separator */
            LOCALE_SMONTHOUSANDSEP = 0x00000017, /* monetary thousand separator */
            LOCALE_SMONGROUPING = 0x00000018, /* monetary grouping */
            LOCALE_ICURRDIGITS = 0x00000019, /* # local monetary digits */
            LOCALE_IINTLCURRDIGITS = 0x0000001A, /* # intl monetary digits */
            LOCALE_ICURRENCY = 0x0000001B, /* positive currency mode */
            LOCALE_INEGCURR = 0x0000001C, /* negative currency mode */

            LOCALE_SPERCENT = 0x00000076,   // Win7orLater-Symbol used to indicate percentage, for example, "%".
            LOCALE_IPOSITIVEPERCENT = 0x00000075, // Win7orLater-Positive percentage formatting pattern for the locale.
            LOCALE_INEGATIVEPERCENT = 0x00000074, // Win7orLater-Negative percentage formatting pattern for the locale.

            LOCALE_SSHORTDATE = 0x0000001F, /* short date format string */
            LOCALE_SLONGDATE = 0x00000020, /* long date format string */
            LOCALE_STIMEFORMAT = 0x00001003, /* time format string */
            LOCALE_S1159 = 0x00000028, /* AM designator */
            LOCALE_S2359 = 0x00000029, /* PM designator */

            LOCALE_ICALENDARTYPE = 0x00001009, /* type of calendar specifier */
            LOCALE_IFIRSTDAYOFWEEK = 0x0000100C, /* first day of week specifier */
            LOCALE_IFIRSTWEEKOFYEAR = 0x0000100D, /* first week of year specifier */

            LOCALE_SDAYNAME1 = 0x0000002A, /* long name for Monday */
            LOCALE_SDAYNAME2 = 0x0000002B, /* long name for Tuesday */
            LOCALE_SDAYNAME3 = 0x0000002C, /* long name for Wednesday */
            LOCALE_SDAYNAME4 = 0x0000002D, /* long name for Thursday */
            LOCALE_SDAYNAME5 = 0x0000002E, /* long name for Friday */
            LOCALE_SDAYNAME6 = 0x0000002F, /* long name for Saturday */
            LOCALE_SDAYNAME7 = 0x00000030, /* long name for Sunday */
            LOCALE_SABBREVDAYNAME1 = 0x00000031, /* abbreviated name for Monday */
            LOCALE_SABBREVDAYNAME2 = 0x00000032, /* abbreviated name for Tuesday */
            LOCALE_SABBREVDAYNAME3 = 0x00000033, /* abbreviated name for Wednesday */
            LOCALE_SABBREVDAYNAME4 = 0x00000034, /* abbreviated name for Thursday */
            LOCALE_SABBREVDAYNAME5 = 0x00000035, /* abbreviated name for Friday */
            LOCALE_SABBREVDAYNAME6 = 0x00000036, /* abbreviated name for Saturday */
            LOCALE_SABBREVDAYNAME7 = 0x00000037, /* abbreviated name for Sunday */
            LOCALE_SMONTHNAME1 = 0x00000038, /* long name for January */
            LOCALE_SMONTHNAME2 = 0x00000039, /* long name for February */
            LOCALE_SMONTHNAME3 = 0x0000003A, /* long name for March */
            LOCALE_SMONTHNAME4 = 0x0000003B, /* long name for April */
            LOCALE_SMONTHNAME5 = 0x0000003C, /* long name for May */
            LOCALE_SMONTHNAME6 = 0x0000003D, /* long name for June */
            LOCALE_SMONTHNAME7 = 0x0000003E, /* long name for July */
            LOCALE_SMONTHNAME8 = 0x0000003F, /* long name for August */
            LOCALE_SMONTHNAME9 = 0x00000040, /* long name for September */
            LOCALE_SMONTHNAME10 = 0x00000041, /* long name for October */
            LOCALE_SMONTHNAME11 = 0x00000042, /* long name for November */
            LOCALE_SMONTHNAME12 = 0x00000043, /* long name for December */
            LOCALE_SMONTHNAME13 = 0x0000100E, /* long name for 13th month (if exists) */
            LOCALE_SABBREVMONTHNAME1 = 0x00000044, /* abbreviated name for January */
            LOCALE_SABBREVMONTHNAME2 = 0x00000045, /* abbreviated name for February */
            LOCALE_SABBREVMONTHNAME3 = 0x00000046, /* abbreviated name for March */
            LOCALE_SABBREVMONTHNAME4 = 0x00000047, /* abbreviated name for April */
            LOCALE_SABBREVMONTHNAME5 = 0x00000048, /* abbreviated name for May */
            LOCALE_SABBREVMONTHNAME6 = 0x00000049, /* abbreviated name for June */
            LOCALE_SABBREVMONTHNAME7 = 0x0000004A, /* abbreviated name for July */
            LOCALE_SABBREVMONTHNAME8 = 0x0000004B, /* abbreviated name for August */
            LOCALE_SABBREVMONTHNAME9 = 0x0000004C, /* abbreviated name for September */
            LOCALE_SABBREVMONTHNAME10 = 0x0000004D, /* abbreviated name for October */
            LOCALE_SABBREVMONTHNAME11 = 0x0000004E, /* abbreviated name for November */
            LOCALE_SABBREVMONTHNAME12 = 0x0000004F, /* abbreviated name for December */
            LOCALE_SABBREVMONTHNAME13 = 0x0000100F, /* abbreviated name for 13th month (if exists) */

            LOCALE_SPOSITIVESIGN = 0x00000050, /* positive sign */
            LOCALE_SNEGATIVESIGN = 0x00000051, /* negative sign */

            LOCALE_FONTSIGNATURE = 0x00000058, /* font signature */
            LOCALE_SISO639LANGNAME = 0x00000059, /* ISO abbreviated language name */
            LOCALE_SISO3166CTRYNAME = 0x0000005A, /* ISO abbreviated country name */

            LOCALE_SENGCURRNAME = 0x00001007, /* english name of currency */
            LOCALE_SNATIVECURRNAME = 0x00001008, /* native name of currency */
            LOCALE_SYEARMONTH = 0x00001006, /* year month format string */
            LOCALE_IDIGITSUBSTITUTION = 0x00001014, /* 0 = context, 1 = none, 2 = national */

            LOCALE_SNAME = 0x0000005C    /* locale name <language>[-<Script>][-<REGION>[_<sort order>]] */
        };
        #endregion
        #endregion

        #region Number Formatting Methods

        #region Number -> String methods

        // Do not use these methods for all verification. These should be used mainly for simple formats ("C", "G", etc).
        // These methods will verify that all locale information is being retrieved correctly from the OS.
        // They will not, however, test the actual interpretation of that information (the formatting). Therefore, there must be some
        // tests (both with standard and with custom format strings) that do not use these helpers. These are, obviously
        // not meant for testing custom nfi formatting.

        public static string OSDoubleToString(double n) { return OSDoubleToString(n, "G", null); }
        public static string OSDoubleToString(double n, string s) { return OSDoubleToString(n, s, null); }
        public static string OSDoubleToString(double n, CultureInfo ci) { return OSDoubleToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSDoubleToString(double n, string s, CultureInfo ci)
        {
            // DDT #965
            if (double.IsPositiveInfinity(n)) { return "Infinity"; }
            if (double.IsNegativeInfinity(n)) { return "-Infinity"; }
            if (double.IsNaN(n)) { return "NaN"; };

            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberDoubleType, n);
            }
        }

        public static string OSDecimalToString(decimal n) { return OSDecimalToString(n, "G", null); }
        public static string OSDecimalToString(decimal n, string s) { return OSDecimalToString(n, s, null); }
        public static string OSDecimalToString(decimal n, CultureInfo ci) { return OSDecimalToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSDecimalToString(decimal n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                double d1 = (double)n;
                return OSNumberToStringMac(new IntPtr((void*)&d1), s, ci, CFNumberType.kCFNumberFloat64Type, n);
            }
        }

        public static string OSFloatToString(float n) { return OSFloatToString(n, "G", null); }
        public static string OSFloatToString(float n, string s) { return OSFloatToString(n, s, null); }
        public static string OSFloatToString(float n, CultureInfo ci) { return OSFloatToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSFloatToString(float n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberFloat32Type, n);
            }
        }

        public static string OSInt32ToString(Int32 n) { return OSInt32ToString(n, "G", null); }
        public static string OSInt32ToString(Int32 n, string s) { return OSInt32ToString(n, s, null); }
        public static string OSInt32ToString(Int32 n, CultureInfo ci) { return OSInt32ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSInt32ToString(Int32 n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberIntType, n);
            }
        }

        public static string OSInt64ToString(Int64 n) { return OSInt64ToString(n, "G", null); }
        public static string OSInt64ToString(Int64 n, string s) { return OSInt64ToString(n, s, null); }
        public static string OSInt64ToString(Int64 n, CultureInfo ci) { return OSInt64ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSInt64ToString(Int64 n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberSInt64Type, n);
            }
        }

        public static string OSInt16ToString(Int16 n) { return OSInt16ToString(n, "G", null); }
        public static string OSInt16ToString(Int16 n, string s) { return OSInt16ToString(n, s, null); }
        public static string OSInt16ToString(Int16 n, CultureInfo ci) { return OSInt16ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSInt16ToString(Int16 n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberSInt16Type, n);
            }
        }

        public static string OSUInt32ToString(UInt32 n) { return OSUInt32ToString(n, "G", null); }
        public static string OSUInt32ToString(UInt32 n, string s) { return OSUInt32ToString(n, s, null); }
        public static string OSUInt32ToString(UInt32 n, CultureInfo ci) { return OSUInt32ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSUInt32ToString(UInt32 n1, string s, CultureInfo ci)
        {
            Int64 n = (Int64)n1;
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                //return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberIntType, n);
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberSInt64Type, n);
            }
        }

        public static string OSUInt64ToString(UInt64 n) { return OSUInt64ToString(n, "G", null); }
        public static string OSUInt64ToString(UInt64 n, string s) { return OSUInt64ToString(n, s, null); }
        public static string OSUInt64ToString(UInt64 n, CultureInfo ci) { return OSUInt64ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSUInt64ToString(UInt64 n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberLongLongType, n);
            }
        }

        public static string OSUInt16ToString(UInt16 n) { return OSUInt16ToString(n, "G", null); }
        public static string OSUInt16ToString(UInt16 n, string s) { return OSUInt16ToString(n, s, null); }
        public static string OSUInt16ToString(UInt16 n, CultureInfo ci) { return OSUInt16ToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSUInt16ToString(UInt16 n1, string s, CultureInfo ci)
        {
            Int32 n = (Int32)n1;
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberSInt32Type, n);
            }
        }

        public static string OSByteToString(Byte n) { return OSByteToString(n, "G", null); }
        public static string OSByteToString(Byte n, string s) { return OSByteToString(n, s, null); }
        public static string OSByteToString(Byte n, CultureInfo ci) { return OSByteToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSByteToString(Byte n1, string s, CultureInfo ci)
        {
            short n = (short)n1;
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberShortType, n);
            }
        }

        public static string OSSByteToString(SByte n) { return OSSByteToString(n, "G", null); }
        public static string OSSByteToString(SByte n, string s) { return OSSByteToString(n, s, null); }
        public static string OSSByteToString(SByte n, CultureInfo ci) { return OSSByteToString(n, "G", ci); }

        [SecuritySafeCritical]
        public static unsafe string OSSByteToString(SByte n, string s, CultureInfo ci)
        {
            if (Utilities.IsWindows)
            {
                NumberFormatInfo nfi = NumberFormatInfoFromLCID(LCIDFromCultureInfo(ci));
                return n.ToString(s, nfi);
            }
            else
            {
                return OSNumberToStringMac(new IntPtr((void*)&n), s, ci, CFNumberType.kCFNumberSInt8Type, n);
            }
        }
        #endregion

        public static NumberFormatInfo NumberFormatInfoFromLCID(int LCID)
        {
            // Use GetLocaleInfo to construct NFI
            NumberFormatInfo ret = new NumberFormatInfo();
            ret.CurrencyDecimalDigits = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_ICURRDIGITS));
            ret.CurrencyDecimalSeparator = GetLocalizationInfo(LCID, LCType.LOCALE_SMONDECIMALSEP);
            ret.CurrencyGroupSeparator = GetLocalizationInfo(LCID, LCType.LOCALE_SMONTHOUSANDSEP);
            string[] monGroupSizesStrs = GetLocalizationInfo(LCID, LCType.LOCALE_SMONGROUPING).Split(new char[] { ';' });
            int[] monGroupSizes = new int[monGroupSizesStrs.Length - 1];
            for (int i = 0; i < (monGroupSizes.Length); i++) { monGroupSizes[i] = int.Parse(monGroupSizesStrs[i]); }
            ret.CurrencyGroupSizes = monGroupSizes;
            ret.CurrencyNegativePattern = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_INEGCURR));
            ret.CurrencyPositivePattern = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_ICURRENCY));
            ret.CurrencySymbol = GetLocalizationInfo(LCID, LCType.LOCALE_SCURRENCY);

            //ret.DigitSubstitution = (DigitShapes)int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_IDIGITSUBSTITUTION));
            //ret.NativeDigits = GetLocalizationInfo(LCID, LCType.LOCALE_SNATIVEDIGITS).ToCharArray();
            ret.NegativeSign = GetLocalizationInfo(LCID, LCType.LOCALE_SNEGATIVESIGN);
            ret.NumberDecimalDigits = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_IDIGITS));
            ret.NumberDecimalSeparator = GetLocalizationInfo(LCID, LCType.LOCALE_SDECIMAL);
            ret.NumberGroupSeparator = GetLocalizationInfo(LCID, LCType.LOCALE_STHOUSAND);
            string[] groupSizesStrs = GetLocalizationInfo(LCID, LCType.LOCALE_SGROUPING).Split(new char[] { ';' });
            int[] groupSizes = new int[groupSizesStrs.Length - 1];
            for (int i = 0; i < groupSizes.Length; i++) { groupSizes[i] = int.Parse(groupSizesStrs[i]); }
            ret.NumberGroupSizes = groupSizes;
            ret.NumberNegativePattern = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_INEGNUMBER));
            ret.PositiveSign = GetLocalizationInfo(LCID, LCType.LOCALE_SPOSITIVESIGN);
            ret.PercentGroupSizes = ret.NumberGroupSizes;
            ret.PercentGroupSeparator = ret.NumberGroupSeparator;
            ret.PercentDecimalSeparator = ret.NumberDecimalSeparator;
            ret.PercentDecimalDigits = ret.NumberDecimalDigits;
            if (string.IsNullOrEmpty(ret.PositiveSign)) ret.PositiveSign = "+";
            if (Utilities.IsWin7OrLater)
            {
                //new in Win7
                ret.PercentSymbol = GetLocalizationInfo(LCID, LCType.LOCALE_SPERCENT);
                ret.PercentPositivePattern = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_IPOSITIVEPERCENT));
                ret.PercentNegativePattern = int.Parse(GetLocalizationInfo(LCID, LCType.LOCALE_INEGATIVEPERCENT));
            }
            return ret;
        }

        [SecuritySafeCritical]
        private static unsafe string OSNumberToStringMac(IntPtr n, string s, CultureInfo ci, CFNumberType numType, object o)
        {
#if REMOVE
            if (string.IsNullOrEmpty(s)) s = "G";
            IntPtr cfLocale = IntPtr.Zero;
            IntPtr cfLocaleName = IntPtr.Zero;
            if (ci == null)
            {
                cfLocale = CFLocaleCopyCurrent();
                ci = Utilities.CurrentCulture;
            }
            else
            {
                cfLocaleName = CFStringCreateWithCharacters(IntPtr.Zero, ci.Name, ci.Name.Length);
                cfLocale = CFLocaleCreate(IntPtr.Zero, cfLocaleName);
            }
            IntPtr cfNumber = CFNumberCreate(IntPtr.Zero, numType, n);
            IntPtr cfFormat = IntPtr.Zero;

            int formatLen = 0;

            if (s.Length > 1)
            {
                formatLen = int.Parse(s.Substring(1));
            }
            if (s[0] == 'Y')
            {
                int dotLoc = double.Parse(o.ToString()).ToString(CultureInfo.InvariantCulture).Trim('-').IndexOf('.');
                if (dotLoc == -1) formatLen = 0;
                else
                {
                    string subStr = double.Parse(o.ToString()).ToString(CultureInfo.InvariantCulture).Trim('-').Substring(dotLoc + 1);
                    formatLen = Math.Min(formatLen - dotLoc, subStr.Length);
                    if (s.Length == 1) formatLen = subStr.Length;
                }
            }
            if (s[0] == 'Z') formatLen--;
            if (formatLen < 0) formatLen = 0;

            int round = 1;
            IntPtr cfFormatLen = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&formatLen));
            IntPtr cfOne = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&round));

            int two = 2;
            IntPtr cfTwo = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&two));

            int three = 3;
            IntPtr cfThree = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&three));

            int zero = 0;
            IntPtr cfZero = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&zero));

            int six = 6;
            IntPtr cfSix = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&six));

            int numberDecimalDigit = ci.NumberFormat.NumberDecimalDigits;
            IntPtr cfNumberDecimalDigits = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&numberDecimalDigit));

            switch (s[0].ToString().ToUpper())
            //' switch (s[0].ToString().ToUpper(CultureInfo.InvariantCulture))
            {
                case "P":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterPercentStyle);
                    if (formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                    }
                    break;
                case "D":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterDecimalStyle);
                    CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterUseGroupingSeparator"), IntPtr.Zero);
                    CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfFormatLen);
                    break;
                case "e":
                    return OSNumberToStringMac(n, s, ci, numType, o).ToLower();
                    //' return OSNumberToStringMac(n, s, ci, numType, o).ToLower(CultureInfo.InvariantCulture);
                case "E":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterScientificStyle);
                    if (formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    else
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfSix);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfSix);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    break;
                case "C":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterCurrencyStyle);
                    if (formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    break;
                case "F":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterNoStyle);
                    if (formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    else
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfNumberDecimalDigits);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfNumberDecimalDigits);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    break;
                case "N":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterDecimalStyle);
                    if (formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    else
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfNumberDecimalDigits);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfNumberDecimalDigits);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    break;
                case "G":
                    if (IsIntType(numType))
return OSNumberToStringMac(n, s.ToUpper().Replace('G', 'D'), ci, numType, o); ;
                        //' return OSNumberToStringMac(n, s.ToUpper(CultureInfo.InvariantCulture).Replace('G', 'D'), ci, numType, o); ;

                    string zReplace = OSNumberToStringMac(n, s.ToUpper().Replace('G', 'Z'), ci, numType, o);
                    string yReplace = OSNumberToStringMac(n, s.ToUpper().Replace('G', 'Y'), ci, numType, o);
                    //' string zReplace = OSNumberToStringMac(n, s.ToUpper(CultureInfo.InvariantCulture).Replace('G', 'Z'), ci, numType, o);
                    //' string yReplace = OSNumberToStringMac(n, s.ToUpper(CultureInfo.InvariantCulture).Replace('G', 'Y'), ci, numType, o);
                    if ((numType == CFNumberType.kCFNumberDoubleType) && (yReplace.Trim(ci.NumberFormat.NegativeSign.ToCharArray()).Replace(ci.NumberFormat.NumberDecimalSeparator, "").Trim('0').Length > 15)) return zReplace;
                    if ((numType == CFNumberType.kCFNumberFloat32Type) && (yReplace.Trim(ci.NumberFormat.NegativeSign.ToCharArray()).Replace(ci.NumberFormat.NumberDecimalSeparator, "").Trim('0').Length > 7)) return zReplace;

                    if (zReplace.Length < yReplace.Length) return zReplace;
                    return yReplace;
                case "X":
                    // TODO
                    throw new NotSupportedException("GlobLocHelper does not currently handle hex formatting.");
                case "Y":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterDecimalStyle);
                    CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterUseGroupingSeparator"), IntPtr.Zero);
                    if (s.Length > 1 || formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                    }
                    else
                    {
                        int largeNum = (numType == CFNumberType.kCFNumberFloat32Type) ? 7 : 15;
                        IntPtr cfLargeNum = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&largeNum));
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfLargeNum);
                        if (cfLargeNum != IntPtr.Zero)
                            CFRelease(cfLargeNum);
                    }
                    break;
                case "Z":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterScientificStyle);

                    if (s.Length > 1 || formatLen > 0)
                    {
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfFormatLen);
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMinIntegerDigits"), cfOne);
                    }
                    else
                    {
                        int largeNum = (numType == CFNumberType.kCFNumberFloat32Type) ? 7 : 15;
                        IntPtr cfLargeNum = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&largeNum));
                        CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfLargeNum);
                        if (cfLargeNum != IntPtr.Zero)
                            CFRelease(cfLargeNum);
                    }
                    break;
                case "R":
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterDecimalStyle);

                    int precision = (numType == CFNumberType.kCFNumberDoubleType) ? 15 : 7;
                    IntPtr cfPrecision = CFNumberCreate(IntPtr.Zero, CFNumberType.kCFNumberSInt32Type, new IntPtr((void*)&precision));
                    CFNumberFormatterSetProperty(cfFormat, GetCFString("kCFNumberFormatterMaxFractionDigits"), cfPrecision);
                    if (cfPrecision != IntPtr.Zero)
                        CFRelease(cfPrecision);

                    break;
                default:
                    cfFormat = CFNumberFormatterCreate(IntPtr.Zero, cfLocale, CFNumberFormatterStyle.kCFNumberFormatterNoStyle);
                    break;
            }

            IntPtr cfReturn = CFNumberFormatterCreateStringWithNumber(IntPtr.Zero, cfFormat, cfNumber);

            int newLength = CFStringGetLength(cfReturn);
            CFRange cfRange = new CFRange();
            cfRange.x = 0;
            cfRange.y = newLength;

            StringBuilder sb = new StringBuilder(newLength);
            CFStringGetCharacters(cfReturn, cfRange, sb);
            string ret = sb.ToString().Substring(0, newLength);
            if (s[0] == 'e' || s[0] == 'g') ret = ret.Replace('E', 'e');

            if (cfOne != IntPtr.Zero)
                CFRelease(cfOne);
            if (cfThree != IntPtr.Zero)
                CFRelease(cfThree);
            if (cfFormatLen != IntPtr.Zero)
                CFRelease(cfFormatLen);
            if (cfReturn != IntPtr.Zero)
                CFRelease(cfReturn);
            if (cfLocaleName != IntPtr.Zero)
                CFRelease(cfLocaleName);
            if (cfLocale != IntPtr.Zero)
                CFRelease(cfLocale);
            if (cfFormat != IntPtr.Zero)
                CFRelease(cfFormat);
            if (cfNumber != IntPtr.Zero)
                CFRelease(cfNumber);
            if (cfTwo != IntPtr.Zero)
                CFRelease(cfTwo);
            if (cfZero != IntPtr.Zero)
                CFRelease(cfZero);
            if (cfSix != IntPtr.Zero)
                CFRelease(cfSix);
            if (cfNumberDecimalDigits != IntPtr.Zero)
                CFRelease(cfNumberDecimalDigits);
            return ret;
#else
            throw new NotImplementedException();
#endif
        }
        #endregion
        #region DateTime Formatting Methods
        public static string OSDateToString(DateTime date, string format)
        {
            return OSDateToString(date, format, Utilities.CurrentCulture.DateTimeFormat, Utilities.CurrentCulture);
        }

        public static string OSDateToString(DateTime date, string format, CultureInfo ci)
        {
            return OSDateToString(date, format, ci, DateTimeStyles.None);
        }

        public static string OSDateToString(DateTime date)
        {
            return OSDateToString(date, "G");
        }

        public static string OSDateToString(DateTime date, string format, CultureInfo ci, DateTimeStyles styles)
        {
            if (ci == null) ci = Utilities.CurrentCulture;
            if ((styles & DateTimeStyles.AssumeLocal) == DateTimeStyles.AssumeLocal)
            {
                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Local);
            }
            if ((styles & DateTimeStyles.AssumeUniversal) == DateTimeStyles.AssumeUniversal)
            {
                date = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, date.Millisecond, DateTimeKind.Utc);
            }
            if ((styles & DateTimeStyles.AdjustToUniversal) == DateTimeStyles.AdjustToUniversal)
            {
                date = date.ToUniversalTime();
            }
            return OSDateToString(date, format, ci.DateTimeFormat, ci);
        }


        [SecuritySafeCritical]
        public static string OSDateToString(DateTime date, string format, DateTimeFormatInfo dtfi, CultureInfo cultInfo)
        {
            string ret = string.Empty;

#if TEMP
            PropertyInfo cultureName = null;

            foreach (var pi in typeof(DateTimeFormatInfo).GetTypeInfo().DeclaredProperties)
            {
                if (pi.Name == "CultureName") cultureName = pi;
            }

            if (cultureName == null) throw new ArgumentException("Can't find CultureName property on DateTimeFormatInfo");

            // PropertyInfo cultureName = typeof(DateTimeFormatInfo).GetProperty("CultureName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            string culture = (string)cultureName.GetValue(dtfi, null);
#endif
            String culture = cultInfo.Name;
            StringBuilder dateSB = new StringBuilder();
            StringBuilder timeSB = new StringBuilder();
            string connector = " ";
            bool useDate = false;
            bool useTime = false;
            int locale = LCIDFromCultureInfo(new CultureInfo(culture));
            string formatDate = null;
            string formatTime = null;
            TimeFormatFlags timeFlag = 0;
            DateFormatFlags dateFlag = 0;

            #region Format switch
            switch (format)
            {
                case "d":
                    useDate = true;
                    dateFlag |= DateFormatFlags.DATE_SHORTDATE;
                    break;
                case "D":
                    useDate = true;
                    dateFlag |= DateFormatFlags.DATE_LONGDATE;
                    break;
                case "f":
                    useDate = true; useTime = true;
                    dateFlag |= DateFormatFlags.DATE_LONGDATE;
                    timeFlag |= TimeFormatFlags.TIME_SHORTTIME;
                    break;
                case "F":
                    useDate = true; useTime = true;
                    dateFlag |= DateFormatFlags.DATE_LONGDATE;
                    timeFlag |= TimeFormatFlags.TIME_LONGTIME;
                    break;
                case "g":
                    useDate = true; useTime = true;
                    dateFlag |= DateFormatFlags.DATE_SHORTDATE;
                    timeFlag |= TimeFormatFlags.TIME_SHORTTIME;
                    break;
                case "G":
                    useDate = true; useTime = true;
                    dateFlag |= DateFormatFlags.DATE_SHORTDATE;
                    timeFlag |= TimeFormatFlags.TIME_LONGTIME;
                    break;
                case "m":
                case "M":
                    formatDate = "MMMM dd";
                    break;
                case "y":
                case "Y":
                    useDate = true;
                    dateFlag |= DateFormatFlags.DATE_YEARMONTH;
                    break;
                case "s":
                    formatDate = "yyyy'-'MM'-'dd'";
                    formatTime = "HH':'mm':'ss";
                    connector = "T";
                    break;
                case "R":
                case "r":
                    formatDate = "ddd, dd MMM yyyy";
                    formatTime = "HH':'mm':'ss 'GMT'";
                    break;
                case "u":
                    formatDate = "yyyy'-'MM'-'dd";
                    formatTime = "HH':'mm':'ss'Z'";
                    break;
                case "U":
                    date = date.ToUniversalTime();
                    useDate = true; useTime = true;
                    dateFlag |= DateFormatFlags.DATE_LONGDATE;
                    timeFlag |= TimeFormatFlags.TIME_LONGTIME;
                    break;
                case "o":
                case "O":
                    formatDate = "yyyy-MM-dd";
                    formatTime = "HH:mm:ss.fffffffK";
                    connector = "T";
                    break;
                case "t":
                    useTime = true;
                    //timeFlag |= TimeFormatFlags.TIME_LONGTIME;  // By design :( DDT #1472
                    timeFlag |= TimeFormatFlags.TIME_SHORTTIME;  // Fixed on SL FC 75
                    break;
                case "T":
                    useTime = true;
                    timeFlag |= TimeFormatFlags.TIME_LONGTIME;
                    break;
                default:
                    connector = "";
                    format = format.Replace("%", string.Empty);
                    int pos = 0;
                    while (pos < format.Length && format[pos] != 'h' && format[pos] != 'H' && format[pos] != 'm' && format[pos] != 's'
                        && format[pos] != 'f' && format[pos] != 'K' && format[pos] != 'F' && format[pos] != 't' && format[pos] != 'z')
                        pos++;
                    if (pos == 0)
                    {
                        formatTime = format;
                    }
                    else if (pos < format.Length)
                    {
                        formatDate = format.Substring(0, pos);
                        formatTime = format.Substring(pos);
                    }
                    else
                    {
                        formatDate = format;
                    }
                    break;
            }
            #endregion

            SystemTime st = new SystemTime(date);
            if (!string.IsNullOrEmpty(formatDate) || !string.IsNullOrEmpty(formatTime))
            {
                ret = string.Empty;
                if (!string.IsNullOrEmpty(formatDate))
                {
                    int cap = GetDateFormatW((uint)locale, 0, ref st, formatDate, dateSB, 0);
                    dateSB = new StringBuilder(cap);
                    GetDateFormatW((uint)locale, 0, ref st, formatDate, dateSB, cap);
                    ret = dateSB.ToString();
                }
                if (!string.IsNullOrEmpty(formatDate) && !string.IsNullOrEmpty(formatTime))
                {
                    ret = ret + connector;
                }
                if (!string.IsNullOrEmpty(formatTime))
                {
                    while (formatTime.Contains("f"))
                    {
                        int beginFS = formatTime.IndexOf("f");
                        int fCount = 0;
                        while (beginFS < formatTime.Length && formatTime[beginFS] == 'f') { fCount++; beginFS++; }
                        long fraction = date.Ticks % 10000000;
                        string fractionalString = fraction.ToString().Substring(0, Math.Min(fCount, fraction.ToString().Length));
                        if (fCount > fractionalString.Length) fractionalString = fractionalString + new string('0', fCount - fractionalString.Length);
                        formatTime = formatTime.Replace(new string('f', fCount), fractionalString);
                    }
                    if (formatTime.Contains("K"))
                    {
                        string tz = null;
                        switch (date.Kind)
                        {
                            case DateTimeKind.Local:
                                TimeSpan offset = TimeZoneInfo.Local.GetUtcOffset(date);
                                if (offset.Ticks >= 0)
                                {
                                    tz = String.Format(CultureInfo.InvariantCulture, "+{0:00}:{1:00}", offset.Hours, offset.Minutes);
                                }
                                else
                                {
                                    tz = String.Format(CultureInfo.InvariantCulture, "-{0:00}:{1:00}", -offset.Hours, -offset.Minutes);
                                }
                                break;
                            case DateTimeKind.Utc:
                                tz = "Z";
                                break;
                            default:
                                tz = string.Empty;
                                break;
                        }
                        formatTime = formatTime.Replace("K", tz);
                    }
                    int cap = GetTimeFormatW((uint)locale, 0, ref st, formatTime, timeSB, 0);
                    timeSB = new StringBuilder(cap);
                    GetTimeFormatW((uint)locale, 0, ref st, formatTime, timeSB, cap);
                    ret = ret + timeSB.ToString();
                }
            }
            else
            {
                ret = string.Empty;
                if (useDate)
                {
                    int cap = GetDateFormatW((uint)locale, (uint)dateFlag, ref st, null, dateSB, 0);
                    dateSB = new StringBuilder(cap);
                    GetDateFormatW((uint)locale, (uint)dateFlag, ref st, null, dateSB, cap);
                    ret = dateSB.ToString();
                }
                if (useDate && useTime) ret = ret + connector;
                if (useTime)
                {
                    int cap = GetTimeFormatW((uint)locale, (uint)timeFlag, ref st, null, timeSB, 0);
                    timeSB = new StringBuilder(cap);
                    GetTimeFormatW((uint)locale, (uint)timeFlag, ref st, null, timeSB, cap);
                    ret = ret + timeSB.ToString();
                }
            }


            if (string.IsNullOrEmpty(ret)) throw new InvalidOperationException("Formatted DateTime unexpectedly empty\nDate: " + date.ToString() + "\nFormat: " + format);
            return ret;
        }
        #endregion

        #region Retrieving DateTimeFormats
        public static string[] OSGetDateTimeFormats(DateTime dt)
        {
            return OSGetDateTimeFormats(dt, Utilities.CurrentCulture.DateTimeFormat);
        }

        public static string[] OSGetDateTimeFormats(DateTime dt, char c)
        {
            return OSGetDateTimeFormats(dt, c, Utilities.CurrentCulture.DateTimeFormat);
        }

        public static string[] OSGetDateTimeFormats(DateTime dt, DateTimeFormatInfo dtfi)
        {
            List<string> ret = new List<string>();
            foreach (char c in allStandardFormats)
            {
                ret.AddRange(OSGetDateTimeFormats(dt, c, dtfi));
            }
            return ret.ToArray();
        }

        [SecuritySafeCritical]
        public static string[] OSGetDateTimeFormats(DateTime dt, char format, DateTimeFormatInfo dtfi)
        {
#if TEMP
            string[] results = null;
            string[] formats = null;
            ResetDatesAndTimes();

            PropertyInfo cultureName = typeof(DateTimeFormatInfo).GetProperty("CultureName", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);
            string cultureStr = (string)cultureName.GetValue(dtfi, null);
            uint culture = (uint)LCIDFromCultureName(cultureStr);

            if (Utilities.IsWindows)
            {
                switch (format)
                {
                    case 'd':
                        EnumDateFormats(new DateTimeFormatDelegate(GetDateFormats), culture, (uint)(DateFormatFlags.DATE_SHORTDATE));
                        results = new string[Dates.Count];
                        for (int i = 0; i < Dates.Count; i++) { results[i] = dt.ToString(Dates[i], dtfi); }
                        break;
                    case 'D':
                        EnumDateFormats(new DateTimeFormatDelegate(GetDateFormats), culture, (uint)(DateFormatFlags.DATE_LONGDATE));
                        results = new string[Dates.Count];
                        for (int i = 0; i < Dates.Count; i++) { results[i] = dt.ToString(Dates[i], dtfi); }
                        break;
                    case 'f':
                    case 'F':
                        EnumDateFormats(new DateTimeFormatDelegate(GetDateFormats), culture, (uint)(DateFormatFlags.DATE_LONGDATE));
                        EnumTimeFormats(new DateTimeFormatDelegate(GetTimeFormats), culture, (uint)(TimeFormatFlags.TIME_LONGTIME));
                        formats = ComposeDatesAndTimes();
                        results = new string[formats.Length];
                        for (int i = 0; i < formats.Length; i++) { results[i] = dt.ToString(formats[i], dtfi); }
                        break;
                    case 'g':
                    case 'G':
                        EnumDateFormats(new DateTimeFormatDelegate(GetDateFormats), culture, (uint)(DateFormatFlags.DATE_SHORTDATE));
                        EnumTimeFormats(new DateTimeFormatDelegate(GetTimeFormats), culture, (uint)(TimeFormatFlags.TIME_LONGTIME));
                        formats = ComposeDatesAndTimes();
                        results = new string[formats.Length];
                        for (int i = 0; i < formats.Length; i++) { results[i] = dt.ToString(formats[i], dtfi); }
                        break;
                    case 't':
                    case 'T':
                        EnumTimeFormats(new DateTimeFormatDelegate(GetTimeFormats), culture, (uint)(TimeFormatFlags.TIME_LONGTIME));
                        results = new string[Times.Count];
                        for (int i = 0; i < Times.Count; i++) { results[i] = dt.ToString(Times[i], dtfi); }
                        break;
                    case 'y':
                    case 'Y':
                        EnumDateFormats(new DateTimeFormatDelegate(GetDateFormats), culture, (uint)(DateFormatFlags.DATE_YEARMONTH));
                        results = new string[Dates.Count];
                        for (int i = 0; i < Dates.Count; i++) { results[i] = dt.ToString(Dates[i], dtfi); }
                        break;
                    case 'U':
                        DateTime universalTime = dt.ToUniversalTime();
                        return OSGetDateTimeFormats(universalTime, 'F', dtfi);
                    case 'm':
                    case 'M':
                    case 'r':
                    case 'R':
                    case 'o':
                    case 'O':
                    case 's':
                    case 'u':
                        results = new String[] { OSDateToString(dt, format.ToString(), dtfi) };
                        break;
                }
            }
            else
            {
                // Work around a Mac OS bug that only is present in local time without custom format strings (so not O and s)
                if (format != 's' && char.ToUpper(format, CultureInfo.InvariantCulture) != 'O' && dt.IsDaylightSavingTime()) dt = dt - new TimeSpan(1, 0, 0);
                switch (format)
                {
                    case 'd':
                        results = new string[1];
                        results[0] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterShortStyle, CFDateFormatterStyle.kCFDateFormatterNoStyle);
                        break;
                    case 'D':
                        results = new string[3];
                        results[0] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterNoStyle);
                        results[1] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterNoStyle);
                        results[2] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterNoStyle);
                        break;
                    case 'f':
                    case 'F':
                        results = new string[12];
                        results[0] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[1] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[2] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[3] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        results[4] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[5] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[6] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[7] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        results[8] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[9] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[10] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[11] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        break;
                    case 'g':
                    case 'G':
                        results = new string[4];
                        results[0] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterShortStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[1] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterShortStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[2] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterShortStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[3] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterShortStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        break;
                    case 't':
                    case 'T':
                        results = new string[4];
                        results[0] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterNoStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[1] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterNoStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[2] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterNoStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[3] = GetMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterNoStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        break;
                    case 'U':
                        results = new string[12];
                        results[0] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[1] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[2] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[3] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterFullStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        results[4] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[5] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[6] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[7] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterLongStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        results[8] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterFullStyle);
                        results[9] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterLongStyle);
                        results[10] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterMediumStyle);
                        results[11] = GetUnivMacDateTimeString(dt, cultureStr, CFDateFormatterStyle.kCFDateFormatterMediumStyle, CFDateFormatterStyle.kCFDateFormatterShortStyle);
                        break;
                    case 'y':
                    case 'Y':
                    case 'm':
                    case 'M':
                    case 'r':
                    case 'R':
                    case 'o':
                    case 'O':
                    case 's':
                    case 'u':
                        results = new String[] { OSDateToString(dt, format.ToString(), dtfi) };
                        break;
                }
            }

            return results;
#else
            throw new NotImplementedException();
#endif
        }
        #endregion

        #region Casing Methods
        public static char OSToUpper(char c) { return OSToUpper(c.ToString())[0]; }

        public static char OSToUpper(char c, CultureInfo ci) { return OSToUpper(c.ToString(), ci)[0]; }

        public static string OSToUpper(string str)
        {
            return OSToUpper(str, null);
        }

        [SecuritySafeCritical]
        public static string OSToUpper(string str, CultureInfo ci)
        {
            if (string.IsNullOrEmpty(str)) return str;
            string output = new string(' ', str.Length);
            int lcid = LCIDFromCultureInfo(ci); // ci.LCID;
            int length = str.Length;

            int nativeFlags = LCMAP_UPPERCASE;
            if (ci != CultureInfo.InvariantCulture) nativeFlags |= LCMAP_LINGUISTIC_CASING;

            int retValue = 0;
            if (Utilities.IsVistaOrLater)
                retValue = LCMapStringEx(ci == null ? Utilities.CurrentCulture.ToString() : ci.ToString(), nativeFlags, str, length, output, length, (IntPtr)null, (IntPtr)null, IntPtr.Zero);
            else
                retValue = LCMapStringW(lcid, nativeFlags, str, length, output, length);

            if (retValue == 0) throw new InvalidOperationException("Unexpected return value from LCMapStringW: " + retValue.ToString());
            return output;
        }

        public static char OSToLower(char c) { return OSToLower(c.ToString())[0]; }

        public static char OSToLower(char c, CultureInfo ci) { return OSToLower(c.ToString(), ci)[0]; }

        public static string OSToLower(string str)
        {
            return OSToLower(str, null);
        }

        [SecuritySafeCritical]
        public static string OSToLower(string str, CultureInfo ci)
        {
            if (str.Contains("\0"))
            {
                string[] subStrs = str.Split(new char[] { '\0' });
                StringBuilder sbOut = new StringBuilder(OSToLower(subStrs[0], ci));
                for (int i = 1; i < subStrs.Length; i++)
                {
                    sbOut.Append("\0");
                    sbOut.Append(OSToLower(subStrs[i], ci));
                }
                return sbOut.ToString();
            }

            if (string.IsNullOrEmpty(str)) return str;
            string output = new string(' ', str.Length);
            int lcid = LCIDFromCultureInfo(ci); // ci.LCID;
            int length = str.Length;

            int nativeFlags = LCMAP_LOWERCASE;
            if (ci != CultureInfo.InvariantCulture) nativeFlags |= LCMAP_LINGUISTIC_CASING;

            int retValue = 0;
            if (Utilities.IsVistaOrLater)
                retValue = LCMapStringEx(ci == null ? Utilities.CurrentCulture.ToString() : ci.ToString(), nativeFlags, str, length, output, length, (IntPtr)null, (IntPtr)null, IntPtr.Zero);
            else
                retValue = LCMapStringW(lcid, nativeFlags, str, length, output, length);

            if (retValue == 0) throw new InvalidOperationException("Unexpected return value from LCMapStringW/LCMapStringEx: " + retValue.ToString());
            return output;
        }
        #endregion

        #region String Comparing, Indexing, Prefix, and Suffix Methods
        public static int OSIndexOfAny(string str1, char[] chars)
        {
            int ret = str1.Length + 1;
            foreach (char c in chars)
            {
                int val = OSIndexOf(str1, c);
                if (val > -1 && val < ret) ret = val;
            }
            if (ret == str1.Length + 1) ret = -1;
            return ret;
        }

        public static int OSIndexOfAny(string str1, char[] chars, int index)
        {
            int ret = str1.Length + 1;
            foreach (char c in chars)
            {
                int val = OSIndexOf(str1, c, index);
                if (val > -1 && val < ret) ret = val;
            }
            if (ret == str1.Length + 1) ret = -1;
            return ret;
        }

        public static int OSIndexOfAny(string str1, char[] chars, int index, int count)
        {
            int ret = str1.Length + 1;
            foreach (char c in chars)
            {
                int val = OSIndexOf(str1, c, index, count);
                if (val > -1 && val < ret) ret = val;
            }
            if (ret == str1.Length + 1) ret = -1;
            return ret;
        }

        public static int OSIndexOf(string str1, string str2, StringComparison sc)
        {
            switch (sc)
            {
                case StringComparison.CurrentCulture:
                    return OSIndexOf(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return OSIndexOf(Utilities.CurrentCulture, str1, str2, CompareOptions.IgnoreCase);
#if REMOVE
                case StringComparison.InvariantCulture:
                    return OSIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return OSIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.IgnoreCase);
#endif
                case StringComparison.Ordinal:
                    return OSIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    return OSIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException("Invalid StringComparison value");
            }
        }

        public static int OSIndexOf(string str1, string str2, int index, int count)
        {
            return OSIndexOf(str1, str2, index, count, StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, char c)
        {
            return OSIndexOf(str1, c.ToString(), StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, char c, int index)
        {
            return OSIndexOf(str1, c.ToString(), index, StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, char c, int index, int count)
        {
            return OSIndexOf(str1, c.ToString(), index, count, StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, string str2)
        {
            return OSIndexOf(str1, str2, StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, string str2, int index)
        {
            return OSIndexOf(str1, str2, index, StringComparison.CurrentCulture);
        }

        public static int OSIndexOf(string str1, string str2, int index, StringComparison options)
        {
            int ret = OSIndexOf(str1.Substring(index, str1.Length - index), str2, options);
            if (ret == -1) return -1;
            else return index + ret;
        }

        public static int OSIndexOf(string str1, string str2, int index, int count, StringComparison sc)
        {
            int ret = OSIndexOf(str1.Substring(index, count), str2, sc);
            if (ret == -1) return -1;
            else return index + ret;
        }

        public static int OSIndexOf(CultureInfo ci, string str1, string str2, CompareOptions options)
        {
            if (!Utilities.IsWindows)
            {
                // This hack works around a Mac OS X 10.4 bug (it incorrectly ignores nulls)
                str1 = str1.Replace("\0", "*");
                str2 = str2.Replace("\0", "*");
            }
            if (str2.Length == 0) return 0;
            for (int i = 0; i < str1.Length; i++)
            {
                for (int j = Math.Max(1, str2.Length - 5); j <= Math.Min(str1.Length - i, str2.Length + 5); j++)
                {
                    if (0 == OSCompare(ci, str1, i, j, str2, 0, str2.Length, options)) return i;
                }
            }

            return -1;
        }

        public static int OSLastIndexOfAny(string str1, char[] chars)
        {
            int ret = -1;
            foreach (char c in chars)
            {
                int val = OSLastIndexOf(str1, c);
                if (val > ret) ret = val;
            }
            return ret;
        }

        public static int OSLastIndexOfAny(string str1, char[] chars, int index)
        {
            int ret = -1; ;
            foreach (char c in chars)
            {
                int val = OSLastIndexOf(str1, c, index);
                if (val > ret) ret = val;
            }
            return ret;
        }

        public static int OSLastIndexOfAny(string str1, char[] chars, int index, int count)
        {
            int ret = -1;
            foreach (char c in chars)
            {
                int val = OSLastIndexOf(str1, c, index, count);
                if (val > ret) ret = val;
            }
            return ret;
        }

        public static int OSLastIndexOf(string str1, string str2, int index, int count)
        {
            return OSLastIndexOf(str1, str2, index, count, StringComparison.CurrentCulture);
        }

        public static int OSLastIndexOf(string str1, char c)
        {
            return OSLastIndexOf(str1, c.ToString(), StringComparison.Ordinal);
        }

        public static int OSLastIndexOf(string str1, char c, int index)
        {
            return OSLastIndexOf(str1, c.ToString(), index, StringComparison.Ordinal);
        }

        public static int OSLastIndexOf(string str1, char c, int index, int count)
        {
            return OSLastIndexOf(str1, c.ToString(), index, count, StringComparison.Ordinal);
        }

        public static int OSLastIndexOf(string str1, string str2)
        {
            return OSLastIndexOf(str1, str2, StringComparison.CurrentCulture);
        }

        public static int OSLastIndexOf(string str1, string str2, int index)
        {
            return OSLastIndexOf(str1, str2, index, StringComparison.CurrentCulture);
        }

        public static int OSLastIndexOf(string str1, string str2, int index, StringComparison options)
        {
            if (str1.Length > index && str2.Length == 0) return index;
            return OSLastIndexOf(str1.Substring(0, Math.Min(index + 1, str1.Length)), str2, options);
        }

        public static int OSLastIndexOf(string str1, string str2, int index, int count, StringComparison sc)
        {
            if (index == str1.Length)
            {
                index = Math.Max(index - 1, 0);
                count = Math.Max(count - 1, 0);
            }

            if (str2.Length == 0) return index;
            int start = index - (count - 1);
            int ret = OSLastIndexOf(str1.Substring(start), str2, count - 1, sc);
            if (ret == -1) return -1;
            else return start + ret;
        }

        public static int OSLastIndexOf(string str1, string str2, StringComparison sc)
        {
            switch (sc)
            {
                case StringComparison.CurrentCulture:
                    return OSLastIndexOf(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return OSLastIndexOf(Utilities.CurrentCulture, str1, str2, CompareOptions.IgnoreCase);
#if REMOVE
                case StringComparison.InvariantCulture:
                    return OSLastIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return OSLastIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.IgnoreCase);
#endif
                case StringComparison.Ordinal:
                    return OSLastIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    return OSLastIndexOf(CultureInfo.InvariantCulture, str1, str2, CompareOptions.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException("Invalid StringComparison value");
            }
        }

        public static int OSLastIndexOf(CultureInfo ci, string str1, string str2, CompareOptions options)
        {
            str1 = str1.Replace("\0", "*");
            str2 = str2.Replace("\0", "*");
            if ((str1 != null) && (str2.Length == 0)) return Math.Max(0, str1.Length - 1);
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                for (int j = Math.Max(1, str2.Length - 5); j <= Math.Min(str1.Length - i, str2.Length + 5); j++)
                {
                    if (0 == OSCompare(ci, str1, i, j, str2, 0, str2.Length, options)) return i;
                }
            }
            return -1;
        }

        public static bool OSIsPrefix(string str1, string str2, StringComparison sc)
        {
            switch (sc)
            {
                case StringComparison.CurrentCulture:
                    return OSIsPrefix(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return OSIsPrefix(Utilities.CurrentCulture, str1, str2, CompareOptions.IgnoreCase);
#if REMOVE
                case StringComparison.InvariantCulture:
                    return OSIsPrefix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return OSIsPrefix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.IgnoreCase);
#endif
                case StringComparison.Ordinal:
                    return OSIsPrefix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    return OSIsPrefix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException("Invalid StringComparison value");
            }
        }

        public static bool OSIsPrefix(CultureInfo ci, string str1, string str2, bool ignoreCase)
        {
            return OSIsPrefix(ci, str1, str2, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public static bool OSIsPrefix(string str1, string str2)
        {
            return OSIsPrefix(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
        }

        public static bool OSIsPrefix(CultureInfo ci, string str1, string str2, CompareOptions options)
        {
            if (ci == null) ci = CultureInfo.InvariantCulture;
            if (str2.Length == 0 && str1 != null) return true;
            return (0 == OSIndexOf(ci, str1, str2, options));
        }

        public static bool OSIsSuffix(string str1, string str2, StringComparison sc)
        {
            switch (sc)
            {
                case StringComparison.CurrentCulture:
                    return OSIsSuffix(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return OSIsSuffix(Utilities.CurrentCulture, str1, str2, CompareOptions.IgnoreCase);
#if REMOVE
                case StringComparison.InvariantCulture:
                    return OSIsSuffix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return OSIsSuffix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.IgnoreCase);
#endif
                case StringComparison.Ordinal:
                    return OSIsSuffix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    return OSIsSuffix(CultureInfo.InvariantCulture, str1, str2, CompareOptions.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException("Invalid StringComparison value");
            }
        }

        public static bool OSIsSuffix(CultureInfo ci, string str1, string str2, bool ignoreCase)
        {
            return OSIsSuffix(ci, str1, str2, ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None);
        }

        public static bool OSIsSuffix(string str1, string str2)
        {
            return OSIsSuffix(Utilities.CurrentCulture, str1, str2, CompareOptions.None);
        }

        public static bool OSIsSuffix(CultureInfo ci, string str1, string str2, CompareOptions options)
        {
            if (ci == null) ci = CultureInfo.InvariantCulture;
            if (str2.Length == 0 && str1 != null) return true;
            for (int i = str1.Length - 1; i >= 0; i--)
            {
                if (0 == OSCompare(ci, str1, i, str1.Length - i, str2, 0, str2.Length, options)) return true;
            }
            return false;
        }

        public static int OSCompare(string str1, string str2)
        {
            return OSCompare(Utilities.CurrentCulture, str1, 0, str1.Length, str2, 0, str2.Length, CompareOptions.None);
        }

        public static int OSCompare(string str1, string str2, bool ignoreCase)
        {
            return OSCompare(Utilities.CurrentCulture, str1, 0, str1.Length, str2, 0, str2.Length, (ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None));
        }

        public static int OSCompare(string str1, string str2, StringComparison sc)
        {
            return OSCompare(str1, 0, str1.Length, str2, 0, str2.Length, sc);
        }

        public static int OSCompare(string str1, string str2, CultureInfo ci)
        {
            return OSCompare(ci, str1, 0, str1.Length, str2, 0, str2.Length, CompareOptions.None);
        }

        public static int OSCompare(string str1, string str2, bool ignoreCase, CultureInfo ci)
        {
            return OSCompare(ci, str1, 0, str1.Length, str2, 0, str2.Length, (ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None));
        }

        public static int OSCompare(string str1, int offset1, string str2, int offset2, int length)
        {
            return OSCompare(Utilities.CurrentCulture, str1, offset1, length, str2, offset2, length, CompareOptions.None);
        }

        public static int OSCompare(string str1, int offset1, string str2, int offset2, int length, bool ignoreCase)
        {
            return OSCompare(Utilities.CurrentCulture, str1, offset1, length, str2, offset2, length, (ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None));
        }

        public static int OSCompare(string str1, int offset1, string str2, int offset2, int length, bool ignoreCase, CultureInfo ci)
        {
            return OSCompare(ci, str1, offset1, length, str2, offset2, length, (ignoreCase ? CompareOptions.IgnoreCase : CompareOptions.None));
        }

        public static int OSCompare(string str1, int offset1, string str2, int offset2, int length, StringComparison sc)
        {
            return OSCompare(str1, offset1, length, str2, offset2, length, sc);
        }

        public static int OSCompare(string str1, int offset1, int length1, string str2, int offset2, int length2, StringComparison sc)
        {
            switch (sc)
            {
                case StringComparison.CurrentCulture:
                    return OSCompare(Utilities.CurrentCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.None);
                case StringComparison.CurrentCultureIgnoreCase:
                    return OSCompare(Utilities.CurrentCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.IgnoreCase);
#if REMOVE
                case StringComparison.InvariantCulture:
                    return OSCompare(CultureInfo.InvariantCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.None);
                case StringComparison.InvariantCultureIgnoreCase:
                    return OSCompare(CultureInfo.InvariantCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.IgnoreCase);
#endif
                case StringComparison.Ordinal:
                    return OSCompare(CultureInfo.InvariantCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.Ordinal);
                case StringComparison.OrdinalIgnoreCase:
                    return OSCompare(CultureInfo.InvariantCulture, str1, offset1, length1, str2, offset2, length2, CompareOptions.OrdinalIgnoreCase);
                default:
                    throw new NotSupportedException("Invalid StringComparison value");
            }
        }

        [SecuritySafeCritical]
        public static int OSCompare(CultureInfo ci, string str1, int offset1, int length1, string str2, int offset2, int length2, CompareOptions options)
        {
            // The OS blows up on nulls. So should we. These are handled in managed code and should be tested as ordinal, even if the ordinal
            // compare option has not been selected.
            if (str1 == null || str2 == null)
                throw new NotSupportedException("Null strings should not be tested by the OS; these are managed code platform-agnostic cases.");

            string str1offset = str1.Substring(offset1);
            string str2offset = str2.Substring(offset2);

            if (length1 > str1offset.Length) length1 = str1offset.Length;
            if (length2 > str2offset.Length) length2 = str2offset.Length;

            int ret;
            if ((options & CompareOptions.Ordinal) != 0) // these flags cannot be used with other flags
            {
                if (Utilities.IsVistaOrLater)
                    ret = CompareStringOrdinal(str1offset, length1, str2offset, length2, false);    // Supported VistaOrLater
                else
                    ret = GlobLocHelperCompareOrdinalForXP(str1offset, length1, str2offset, length2);
            }
            else if ((options & CompareOptions.OrdinalIgnoreCase) != 0) // these flags cannot be used with other flags
            {
                if (Utilities.IsVistaOrLater)
                    ret = CompareStringOrdinal(str1offset, length1, str2offset, length2, true);    // Supported VistaOrLater
                else
                    throw new NotSupportedException("GlobLocHelper doesn't handle OrdinalIgnoreCase for earlier OSs than Vista");
            }
            else
            {
                int nativeCompareFlags = 0;

                if ((options & CompareOptions.IgnoreCase) != 0) { nativeCompareFlags |= NORM_IGNORECASE; }
                if ((options & CompareOptions.IgnoreKanaType) != 0) { nativeCompareFlags |= NORM_IGNOREKANATYPE; }
                if ((options & CompareOptions.IgnoreNonSpace) != 0) { nativeCompareFlags |= NORM_IGNORENONSPACE; }
                if ((options & CompareOptions.IgnoreSymbols) != 0) { nativeCompareFlags |= NORM_IGNORESYMBOLS; }
                if ((options & CompareOptions.IgnoreWidth) != 0) { nativeCompareFlags |= NORM_IGNOREWIDTH; }
                if ((options & CompareOptions.StringSort) != 0) { nativeCompareFlags |= SORT_STRINGSORT; }

                if (Utilities.IsVistaOrLater)
                {
                    // NORM_LINGUISTIC_CASING: Windows Vista and later. Uses linguistic rules for casing, rather than file system rules (the default)
                    if (CultureInfo.InvariantCulture == ci) nativeCompareFlags |= NORM_LINGUISTIC_CASING;
                    ret = CompareStringEx(ci.ToString(), nativeCompareFlags, str1offset, length1, str2offset, length2, (IntPtr)null, (IntPtr)null, IntPtr.Zero);
                }
                else
                    ret = CompareStringW(LCIDFromCultureInfo(ci), nativeCompareFlags, str1offset, length1, str2offset, length2);
            }

            switch (ret)
            {
                case 0: return -5;
                case 1: return -1;
                case 2: return 0;
                case 3: return 1;
                default: return -6;
            }
        }

        /// <summary>
        /// compare strA and strB char by char
        /// if one of the given length is less than strings, it catches the exception and return 0 as error
        /// </summary>
        /// <param name="strA"></param>
        /// <param name="strB"></param>
        /// <param name="ignoreCase"></param>
        /// <returns>retValue is compatible with CompareStringW</returns>
        private static int GlobLocHelperCompareOrdinalForXP(string strA, int lengthA, string strB, int lengthB)
        {
            try
            {
                int retValue = 0;
                int length = Math.Min(lengthA, lengthB);
                for (int charA, charB, index = 0; index < length; index++)
                {
                    charA = strA[index];
                    charB = strB[index];
                    if (charA == charB)
                        continue;

                    retValue = (charA - charB);
                    break;
                }
                if (retValue == 0)
                    retValue = (lengthA - lengthB);

                if (retValue == 0) return 2;
                else if (retValue > 0) return 3;
                else return 1;
            }
            catch (Exception e)
            {
                TestLibrary.TestFramework.LogError("GlobLocHelperCompareOrdinalForXP", "Unexpected Exception: " + e);
            }
            return 0;
        }
        #endregion

        #region Character Info Helpers
        public static bool CharIsHighSurrogate(char c)
        {
            return (c >= '\ud800' && c <= '\udbff');
        }

        public static bool CharIsLowSurrogate(char c)
        {
            return (c >= '\udc00' && c <= '\udfff');
        }

        public static bool CharIsSurrogate(char c)
        {
            return (CharIsHighSurrogate(c) || CharIsLowSurrogate(c));
        }

        public static int CharConvertToUtf32(char highChar, char lowChar)
        {
            return (((highChar - '\ud800') * 0x400) + (lowChar - '\udc00') + 0x1000);
        }
        #endregion

        #region Encoding Helpers

        public static Encoding GetEncoding(string id)
        {
            return Encoding.GetEncoding(id);
        }

        #endregion

        #region DateTimeFormat Retrieval Helpers
        private static List<string> Dates;
        private static List<string> Times;

        private static void ResetDatesAndTimes() { Dates = new List<string>(); Times = new List<string>(); }

        //' [AllowReversePInvokeCalls]
        private static bool GetDateFormats(string str)
        {
            Dates.Add(str);
            return true;
        }

        //' [AllowReversePInvokeCalls]
        private static bool GetTimeFormats(string str)
        {
            Times.Add(str);
            return true;
        }

        private static string[] ComposeDatesAndTimes()
        {
            string[] ret = new string[Dates.Count * Times.Count];

            for (int i = 0; i < Dates.Count; i++)
            {
                for (int j = 0; j < Times.Count; j++)
                {
                    ret[i * Times.Count + j] = Dates[i] + " " + Times[j];
                }
            }
            return ret;
        }

        private static string GetMacDateTimeString(DateTime dt, string cultureStr, CFDateFormatterStyle dateStyle, CFDateFormatterStyle timeStyle)
        {
            return GetMacDateTimeStringHelper(dt, cultureStr, dateStyle, timeStyle, false);
        }

        private static string GetUnivMacDateTimeString(DateTime dt, string cultureStr, CFDateFormatterStyle dateStyle, CFDateFormatterStyle timeStyle)
        {
            return GetMacDateTimeStringHelper(dt, cultureStr, dateStyle, timeStyle, true);
        }

        [SecuritySafeCritical]
        private static string GetMacDateTimeStringHelper(DateTime dt, string cultureStr, CFDateFormatterStyle dateStyle, CFDateFormatterStyle timeStyle, bool isUniv)
        {
#if REMOVE
            IntPtr cfLocale = IntPtr.Zero;
            IntPtr cfLocaleName = IntPtr.Zero;

            cfLocaleName = CFStringCreateWithCharacters(IntPtr.Zero, cultureStr, cultureStr.Length);
            cfLocale = CFLocaleCreate(IntPtr.Zero, cfLocaleName);

            IntPtr cfDateFormatter = CFDateFormatterCreate(IntPtr.Zero, cfLocale, dateStyle, timeStyle);
            if (isUniv)
            {
                IntPtr cfTZName = CFStringCreateWithCharacters(IntPtr.Zero, "GMT", 3);
                IntPtr cfTZ = CFTimeZoneCreateWithName(IntPtr.Zero, cfTZName, true);
                CFDateFormatterSetProperty(cfDateFormatter, GetCFString("kCFDateFormatterTimeZone"), cfTZ);
                if (cfTZName != IntPtr.Zero)
                    CFRelease(cfTZName);
                if (cfTZ != IntPtr.Zero)
                    CFRelease(cfTZ);
            }
            DateTime date = dt.ToUniversalTime();
            IntPtr cfReturn = CFDateFormatterCreateStringWithAbsoluteTime(IntPtr.Zero, cfDateFormatter, ((double)date.Ticks) / WIN32_CF_TICKS_RATIO - WIN32_CF_TICKS_DELTA);
            int newLength = CFStringGetLength(cfReturn);
            CFRange cfRange = new CFRange();
            cfRange.x = 0;
            cfRange.y = newLength;
            StringBuilder sb = new StringBuilder(newLength);
            CFStringGetCharacters(cfReturn, cfRange, sb);
            string ret = sb.ToString().Substring(0, newLength);

            if (cfLocale != IntPtr.Zero)
                CFRelease(cfLocale);
            if (cfLocaleName != IntPtr.Zero)
                CFRelease(cfLocaleName);
            if (cfDateFormatter != IntPtr.Zero)
                CFRelease(cfDateFormatter);
            if (cfReturn != IntPtr.Zero)
                CFRelease(cfReturn);
            return ret;
#else
            throw new NotImplementedException();
#endif
        }
        #endregion

        #region Helper Methods
        private static int LCIDFromCultureInfo(CultureInfo ci)
        {
            if (ci == null) ci = Utilities.CurrentCulture;
            else //W2K do not support InvariantCulture, however, using "en-us" culture we will get the same results.
                if (TestLibrary.Utilities.IsWin2K && ci == CultureInfo.InvariantCulture) ci = new CultureInfo("en-US");
            return (LCIDFromCultureName(ci.ToString()));
        }

        public static int LCIDFromCultureName(string ci)
        {
            switch (ci.ToLower())
            //' switch (ci.ToLower(CultureInfo.InvariantCulture))
            {
                case "ar": return 1;
                case "bg": return 2;
                case "ca": return 3;
                case "zh-chs": return 4;
                case "cs": return 5;
                case "da": return 6;
                case "de": return 7;
                case "el": return 8;
                case "en": return 9;
                case "es": return 10;
                case "fi": return 11;
                case "fr": return 12;
                case "he": return 13;
                case "hu": return 14;
                case "is": return 15;
                case "it": return 16;
                case "ja": return 17;
                case "ko": return 18;
                case "nl": return 19;
                case "no": return 20;
                case "pl": return 21;
                case "pt": return 22;
                case "ro": return 24;
                case "ru": return 25;
                case "hr": return 26;
                case "sk": return 27;
                case "sq": return 28;
                case "sv": return 29;
                case "th": return 30;
                case "tr": return 31;
                case "ur": return 32;
                case "id": return 33;
                case "uk": return 34;
                case "be": return 35;
                case "sl": return 36;
                case "et": return 37;
                case "lv": return 38;
                case "lt": return 39;
                case "fa": return 41;
                case "vi": return 42;
                case "hy": return 43;
                case "az": return 44;
                case "eu": return 45;
                case "mk": return 47;
                case "af": return 54;
                case "ka": return 55;
                case "fo": return 56;
                case "hi": return 57;
                case "ms": return 62;
                case "kk": return 63;
                case "ky": return 64;
                case "sw": return 65;
                case "uz": return 67;
                case "tt": return 68;
                case "pa": return 70;
                case "gu": return 71;
                case "ta": return 73;
                case "te": return 74;
                case "kn": return 75;
                case "mr": return 78;
                case "sa": return 79;
                case "mn": return 80;
                case "gl": return 86;
                case "kok": return 87;
                case "syr": return 90;
                case "div": return 101;
                case "": return 127;
                case "inv": return 127;
                case "ar-sa": return 0x00401;
                case "bg-bg": return 0x00402;
                case "ca-es": return 0x00403;
                case "zh-tw": return 0x00404;
                case "cs-cz": return 0x00405;
                case "da-dk": return 0x00406;
                case "de-de": return 0x00407;
                case "el-gr": return 0x00408;
                case "en-us": return 0x00409;
                case "es-es_tradnl": return 0x0040a;
                case "fi-fi": return 0x0040b;
                case "fr-fr": return 0x0040c;
                case "he-icase": return 0x0040d;
                case "hu-hu": return 0x0040e;
                case "is-is": return 0x0040f;
                case "it-it": return 0x00410;
                case "ja-jp": return 0x00411;
                case "ko-kr": return 0x00412;
                case "nl-ncase": return 0x00413;
                case "nb-no": return 0x00414;
                case "pl-pcase": return 0x00415;
                case "pt-br": return 0x00416;
                case "rm-ch": return 0x00417;
                case "ro-ro": return 0x00418;
                case "ru-ru": return 0x00419;
                case "hr-hr": return 0x0041a;
                case "sk-sk": return 0x0041b;
                case "sq-acase": return 0x0041c;
                case "sv-se": return 0x0041d;
                case "th-th": return 0x0041e;
                case "tr-tr": return 0x0041f;
                case "ur-pk": return 0x00420;
                case "id-id": return 0x00421;
                case "uk-ua": return 0x00422;
                case "be-by": return 0x00423;
                case "sl-si": return 0x00424;
                case "et-ee": return 0x00425;
                case "lv-lv": return 0x00426;
                case "lt-lt": return 0x00427;
                case "tg-cyrl-tj": return 0x00428;
                case "fa-ir": return 0x00429;
                case "vi-vn": return 0x0042a;
                case "hy-am": return 0x0042b;
                case "az-latn-az": return 0x0042c;
                case "eu-es": return 0x0042d;
                case "hsb-de": return 0x0042e;
                case "mk-mk": return 0x0042f;
                case "tn-za": return 0x00432;
                case "xh-za": return 0x00434;
                case "zu-za": return 0x00435;
                case "af-za": return 0x00436;
                case "ka-ge": return 0x00437;
                case "fo-fo": return 0x00438;
                case "hi-in": return 0x00439;
                case "mt-mt": return 0x0043a;
                case "se-no": return 0x0043b;
                case "ms-my": return 0x0043e;
                case "kk-kz": return 0x0043f;
                case "ky-kg": return 0x00440;
                case "sw-ke": return 0x00441;
                case "tk-tm": return 0x00442;
                case "uz-latn-uz": return 0x00443;
                case "tt-ru": return 0x00444;
                case "bn-in": return 0x00445;
                case "pa-in": return 0x00446;
                case "gu-in": return 0x00447;
                case "or-in": return 0x00448;
                case "ta-in": return 0x00449;
                case "te-in": return 0x0044a;
                case "kn-in": return 0x0044b;
                case "ml-in": return 0x0044c;
                case "as-in": return 0x0044d;
                case "mr-in": return 0x0044e;
                case "sa-in": return 0x0044f;
                case "mn-mn": return 0x00450;
                case "bo-cn": return 0x00451;
                case "cy-gb": return 0x00452;
                case "km-kh": return 0x00453;
                case "lo-la": return 0x00454;
                case "gl-es": return 0x00456;
                case "kok-in": return 0x00457;
                case "syr-sy": return 0x0045a;
                case "si-lk": return 0x0045b;
                case "iu-cans-ca": return 0x0045d;
                case "am-et": return 0x0045e;
                case "ne-np": return 0x00461;
                case "fy-ncase": return 0x00462;
                case "ps-af": return 0x00463;
                case "fil-ph": return 0x00464;
                case "dv-mv": return 0x00465;
                case "ha-latn-ng": return 0x00468;
                case "yo-ng": return 0x0046a;
                case "quz-bo": return 0x0046b;
                case "nso-za": return 0x0046c;
                case "ba-ru": return 0x0046d;
                case "lb-lu": return 0x0046e;
                case "kl-gcase": return 0x0046f;
                case "ig-ng": return 0x00470;
                case "ii-cn": return 0x00478;
                case "arn-ccase": return 0x0047a;
                case "moh-ca": return 0x0047c;
                case "br-fr": return 0x0047e;
                case "ug-cn": return 0x00480;
                case "mi-nz": return 0x00481;
                case "oc-fr": return 0x00482;
                case "co-fr": return 0x00483;
                case "gsw-fr": return 0x00484;
                case "sah-ru": return 0x00485;
                case "qut-gt": return 0x00486;
                case "rw-rw": return 0x00487;
                case "wo-sn": return 0x00488;
                case "prs-af": return 0x0048c;
                case "ar-iq": return 0x00801;
                case "zh-cn": return 0x00804;
                case "de-ch": return 0x00807;
                case "en-gb": return 0x00809;
                case "es-mx": return 0x0080a;
                case "fr-be": return 0x0080c;
                case "it-ch": return 0x00810;
                case "nl-be": return 0x00813;
                case "nn-no": return 0x00814;
                case "pt-pt": return 0x00816;
                case "sr-latn-cs": return 0x0081a;
                case "sv-fi": return 0x0081d;
                case "az-cyrl-az": return 0x0082c;
                case "dsb-de": return 0x0082e;
                case "se-se": return 0x0083b;
                case "ga-ie": return 0x0083c;
                case "ms-bn": return 0x0083e;
                case "uz-cyrl-uz": return 0x00843;
                case "bn-bd": return 0x00845;
                case "mn-mong-cn": return 0x00850;
                case "iu-latn-ca": return 0x0085d;
                case "tzm-latn-dz": return 0x0085f;
                case "quz-ec": return 0x0086b;
                case "ar-eg": return 0x00c01;
                case "zh-hk": return 0x00c04;
                case "de-at": return 0x00c07;
                case "en-au": return 0x00c09;
                case "es-es": return 0x00c0a;
                case "fr-ca": return 0x00c0c;
                case "sr-cyrl-cs": return 0x00c1a;
                case "se-fi": return 0x00c3b;
                case "quz-pe": return 0x00c6b;
                case "ar-ly": return 0x01001;
                case "zh-sg": return 0x01004;
                case "de-lu": return 0x01007;
                case "en-ca": return 0x01009;
                case "es-gt": return 0x0100a;
                case "fr-ch": return 0x0100c;
                case "hr-ba": return 0x0101a;
                case "smj-no": return 0x0103b;
                case "ar-dz": return 0x01401;
                case "zh-mo": return 0x01404;
                case "de-li": return 0x01407;
                case "en-nz": return 0x01409;
                case "es-cr": return 0x0140a;
                case "fr-lu": return 0x0140c;
                case "bs-latn-ba": return 0x0141a;
                case "smj-se": return 0x0143b;
                case "ar-ma": return 0x01801;
                case "en-ie": return 0x01809;
                case "es-pa": return 0x0180a;
                case "fr-mc": return 0x0180c;
                case "sr-latn-ba": return 0x0181a;
                case "sma-no": return 0x0183b;
                case "ar-tn": return 0x01c01;
                case "en-za": return 0x01c09;
                case "es-do": return 0x01c0a;
                case "sr-cyrl-ba": return 0x01c1a;
                case "sma-se": return 0x01c3b;
                case "ar-om": return 0x02001;
                case "en-jm": return 0x02009;
                case "es-ve": return 0x0200a;
                case "bs-cyrl-ba": return 0x0201a;
                case "sms-fi": return 0x0203b;
                case "ar-ye": return 0x02401;
                case "en-029": return 0x02409;
                case "es-co": return 0x0240a;
                case "smn-fi": return 0x0243b;
                case "ar-sy": return 0x02801;
                case "en-bz": return 0x02809;
                case "es-pe": return 0x0280a;
                case "ar-jo": return 0x02c01;
                case "en-tt": return 0x02c09;
                case "es-ar": return 0x02c0a;
                case "ar-lb": return 0x03001;
                case "en-zw": return 0x03009;
                case "es-ec": return 0x0300a;
                case "ar-kw": return 0x03401;
                case "en-ph": return 0x03409;
                case "es-ccase": return 0x0340a;
                case "ar-ae": return 0x03801;
                case "es-uy": return 0x0380a;
                case "ar-bh": return 0x03c01;
                case "es-py": return 0x03c0a;
                case "ar-qa": return 0x04001;
                case "en-in": return 0x04009;
                case "es-bo": return 0x0400a;
                case "en-my": return 0x04409;
                case "es-sv": return 0x0440a;
                case "en-sg": return 0x04809;
                case "es-hn": return 0x0480a;
                case "es-ni": return 0x04c0a;
                case "es-pr": return 0x0500a;
                case "es-us": return 0x0540a;
                case "x-iv_mathan": return 0x1007f;
                case "de-de_phoneb": return 0x10407;
                case "hu-hu_techncase": return 0x1040e;
                case "ka-ge_modern": return 0x10437;
                case "zh-cn_stroke": return 0x20804;
                case "zh-sg_stroke": return 0x21004;
                case "zh-mo_stroke": return 0x21404;
                case "zh-tw_pronun": return 0x30404;
                case "ja-jp_radstr": return 0x40411;

                case "he-il": return 1037;
                case "nl-nl": return 1043;
                case "pl-pl": return 1045;
                case "sq-al": return 1052;
                case "az-az-latn": return 1068;
                case "uz-uz-latn": return 1091;
                case "div-mv": return 1125;
                case "sr-sp-latn": return 2074;
                case "az-az-cyrl": return 2092;
                case "uz-uz-cyrl": return 2115;
                case "sr-sp-cyrl": return 3098;
                case "en-cb": return 9225;
                case "es-cl": return 13322;
                case "zh-cht": return 31748;
                case "sr": return 31770;

                default: return 127; // throw new NotSupportedException("No LCID available for that culture");
            }
        }

        [SecuritySafeCritical]
        private static string GetLocalizationInfo(int LCID, LCType type)
        {
            StringBuilder sb = new StringBuilder();
            int count = GetLocaleInfo((uint)LCID, (uint)type, sb, 0);
            sb = new StringBuilder(count);
            GetLocaleInfo((uint)LCID, (uint)type, sb, count);
            return sb.ToString();
        }
        #endregion
    }
#endif // if !FEATURE_NOPINVOKES
}
