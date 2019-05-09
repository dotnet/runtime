// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;
using Internal.Runtime.CompilerServices;

#if ENABLE_WINRT
using Internal.Runtime.Augments;
#endif

namespace System.Globalization
{
    internal partial class CultureData
    {
        /// <summary>
        /// Check with the OS to see if this is a valid culture.
        /// If so we populate a limited number of fields.  If its not valid we return false.
        ///
        /// The fields we populate:
        ///
        /// sWindowsName -- The name that windows thinks this culture is, ie:
        ///                            en-US if you pass in en-US
        ///                            de-DE_phoneb if you pass in de-DE_phoneb
        ///                            fj-FJ if you pass in fj (neutral, on a pre-Windows 7 machine)
        ///                            fj if you pass in fj (neutral, post-Windows 7 machine)
        ///
        /// sRealName -- The name you used to construct the culture, in pretty form
        ///                       en-US if you pass in EN-us
        ///                       en if you pass in en
        ///                       de-DE_phoneb if you pass in de-DE_phoneb
        ///
        /// sSpecificCulture -- The specific culture for this culture
        ///                             en-US for en-US
        ///                             en-US for en
        ///                             de-DE_phoneb for alt sort
        ///                             fj-FJ for fj (neutral)
        ///
        /// sName -- The IETF name of this culture (ie: no sort info, could be neutral)
        ///                en-US if you pass in en-US
        ///                en if you pass in en
        ///                de-DE if you pass in de-DE_phoneb
        ///
        /// bNeutral -- TRUE if it is a neutral locale
        ///
        /// For a neutral we just populate the neutral name, but we leave the windows name pointing to the
        /// windows locale that's going to provide data for us.
        /// </summary>
        private unsafe bool InitCultureData()
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            int result;
            string realNameBuffer = _sRealName;
            char* pBuffer = stackalloc char[Interop.Kernel32.LOCALE_NAME_MAX_LENGTH];

            result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_SNAME, pBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH);

            // Did it fail?
            if (result == 0)
            {
                return false;
            }

            // It worked, note that the name is the locale name, so use that (even for neutrals)
            // We need to clean up our "real" name, which should look like the windows name right now
            // so overwrite the input with the cleaned up name
            _sRealName = new string(pBuffer, 0, result - 1);
            realNameBuffer = _sRealName;

            // Check for neutrality, don't expect to fail
            // (buffer has our name in it, so we don't have to do the gc. stuff)

            result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_INEUTRAL | Interop.Kernel32.LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
            if (result == 0)
            {
                return false;
            }

            // Remember our neutrality
            _bNeutral = *((uint*)pBuffer) != 0;

            // Note: Parents will be set dynamically

            // Start by assuming the windows name will be the same as the specific name since windows knows
            // about specifics on all versions. Only for downlevel Neutral locales does this have to change.
            _sWindowsName = realNameBuffer;

            // Neutrals and non-neutrals are slightly different
            if (_bNeutral)
            {
                // Neutral Locale

                // IETF name looks like neutral name
                _sName = realNameBuffer;

                // Specific locale name is whatever ResolveLocaleName (win7+) returns.
                // (Buffer has our name in it, and we can recycle that because windows resolves it before writing to the buffer)
                result = Interop.Kernel32.ResolveLocaleName(realNameBuffer, pBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH);

                // 0 is failure, 1 is invariant (""), which we expect
                if (result < 1)
                {
                    return false;
                }
                // We found a locale name, so use it.
                // In vista this should look like a sort name (de-DE_phoneb) or a specific culture (en-US) and be in the "pretty" form
                _sSpecificCulture = new string(pBuffer, 0, result - 1);
            }
            else
            {
                // Specific Locale

                // Specific culture's the same as the locale name since we know its not neutral
                // On mac we'll use this as well, even for neutrals. There's no obvious specific
                // culture to use and this isn't exposed, but behaviorally this is correct on mac.
                // Note that specifics include the sort name (de-DE_phoneb)
                _sSpecificCulture = realNameBuffer;

                _sName = realNameBuffer;

                // We need the IETF name (sname)
                // If we aren't an alt sort locale then this is the same as the windows name.
                // If we are an alt sort locale then this is the same as the part before the _ in the windows name
                // This is for like de-DE_phoneb and es-ES_tradnl that hsouldn't have the _ part

                result = GetLocaleInfoEx(realNameBuffer, Interop.Kernel32.LOCALE_ILANGUAGE | Interop.Kernel32.LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
                if (result == 0)
                {
                    return false;
                }

                _iLanguage = *((int*)pBuffer);

                if (!IsCustomCultureId(_iLanguage))
                {
                    // not custom locale
                    int index = realNameBuffer.IndexOf('_');
                    if (index > 0 && index < realNameBuffer.Length)
                    {
                        _sName = realNameBuffer.Substring(0, index);
                    }
                }
            }

            // It succeeded.
            return true;
        }

        // Wrappers around the GetLocaleInfoEx APIs which handle marshalling the returned
        // data as either and Int or string.
        internal static unsafe string? GetLocaleInfoEx(string localeName, uint field)
        {
            // REVIEW: Determine the maximum size for the buffer
            const int BUFFER_SIZE = 530;

            char* pBuffer = stackalloc char[BUFFER_SIZE];
            int resultCode = GetLocaleInfoEx(localeName, field, pBuffer, BUFFER_SIZE);
            if (resultCode > 0)
            {
                return new string(pBuffer);
            }

            return null;
        }

        internal static unsafe int GetLocaleInfoExInt(string localeName, uint field)
        {
            field |= Interop.Kernel32.LOCALE_RETURN_NUMBER;
            int value = 0;
            GetLocaleInfoEx(localeName, field, (char*) &value, sizeof(int));
            return value;
        }

        internal static unsafe int GetLocaleInfoEx(string lpLocaleName, uint lcType, char* lpLCData, int cchData)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.Kernel32.GetLocaleInfoEx(lpLocaleName, lcType, lpLCData, cchData);
        }

        private string GetLocaleInfo(LocaleStringData type)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfo] Expected _sWindowsName to be populated by already");
            return GetLocaleInfo(_sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private string GetLocaleInfo(string localeName, LocaleStringData type)
        {
            uint lctype = (uint)type;

            return GetLocaleInfoFromLCType(localeName, lctype, UseUserOverride);
        }

        private int GetLocaleInfo(LocaleNumberData type)
        {
            uint lctype = (uint)type;

            // Fix lctype if we don't want overrides
            if (!UseUserOverride)
            {
                lctype |= Interop.Kernel32.LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data, note that we presume it returns success, so we have to know that
            // sWindowsName is valid before calling.
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return GetLocaleInfoExInt(_sWindowsName, lctype);
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return ConvertWin32GroupString(GetLocaleInfoFromLCType(_sWindowsName, (uint)type, UseUserOverride));
        }

        private string? GetTimeFormatString()
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return ReescapeWin32String(GetLocaleInfoFromLCType(_sWindowsName, Interop.Kernel32.LOCALE_STIMEFORMAT, UseUserOverride));
        }

        private int GetFirstDayOfWeek()
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");

            int result = GetLocaleInfoExInt(_sWindowsName, Interop.Kernel32.LOCALE_IFIRSTDAYOFWEEK | (!UseUserOverride ? Interop.Kernel32.LOCALE_NOUSEROVERRIDE : 0));

            // Win32 and .NET disagree on the numbering for days of the week, so we have to convert.
            return ConvertFirstDayOfWeekMonToSun(result);
        }

        private string[]? GetTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Debug.Assert(_sWindowsName != null, "[CultureData.DoEnumTimeFormats] Expected _sWindowsName to be populated by already");
            string[]? result = ReescapeWin32Strings(nativeEnumTimeFormats(_sWindowsName, 0, UseUserOverride));

            return result;
        }

        private string[]? GetShortTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Debug.Assert(_sWindowsName != null, "[CultureData.DoEnumShortTimeFormats] Expected _sWindowsName to be populated by already");
            string[]? result = ReescapeWin32Strings(nativeEnumTimeFormats(_sWindowsName, Interop.Kernel32.TIME_NOSECONDS, UseUserOverride));

            return result;
        }

        // Enumerate all system cultures and then try to find out which culture has
        // region name match the requested region name
        private static CultureData? GetCultureDataFromRegionName(string regionName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(regionName != null);

            EnumLocaleData context = new EnumLocaleData();
            context.cultureName = null;
            context.regionName = regionName;

            unsafe
            {
                Interop.Kernel32.EnumSystemLocalesEx(EnumSystemLocalesProc, Interop.Kernel32.LOCALE_SPECIFICDATA | Interop.Kernel32.LOCALE_SUPPLEMENTAL, Unsafe.AsPointer(ref context), IntPtr.Zero);
            }

            if (context.cultureName != null)
            {
                // we got a matched culture
                return GetCultureData(context.cultureName, true);
            }

            return null;
        }

        private string GetLanguageDisplayName(string cultureName)
        {
#if ENABLE_WINRT
            return WinRTInterop.Callbacks.GetLanguageDisplayName(cultureName);
#else
            // Usually the UI culture shouldn't be different than what we got from WinRT except
            // if DefaultThreadCurrentUICulture was set
            CultureInfo? ci;

            if (CultureInfo.DefaultThreadCurrentUICulture != null &&
                ((ci = GetUserDefaultCulture()) != null) &&
                !CultureInfo.DefaultThreadCurrentUICulture.Name.Equals(ci.Name))
            {
                return NativeName;
            }
            else
            {
                return GetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
            }
#endif // ENABLE_WINRT
        }

        private string GetRegionDisplayName(string isoCountryCode)
        {
#if ENABLE_WINRT
            return WinRTInterop.Callbacks.GetRegionDisplayName(isoCountryCode);
#else
            // If the current UI culture matching the OS UI language, we'll get the display name from the OS.
            // otherwise, we use the native name as we don't carry resources for the region display names anyway.
            if (CultureInfo.CurrentUICulture.Name.Equals(CultureInfo.UserDefaultUICulture.Name))
            {
                return GetLocaleInfo(LocaleStringData.LocalizedCountryName);
            }

            return NativeCountryName;
#endif // ENABLE_WINRT
        }

        private static CultureInfo GetUserDefaultCulture()
        {
#if ENABLE_WINRT
            return (CultureInfo)WinRTInterop.Callbacks.GetUserDefaultCulture();
#else
            return CultureInfo.GetUserDefaultCulture();
#endif // ENABLE_WINRT
        }

        // PAL methods end here.

        private static string GetLocaleInfoFromLCType(string localeName, uint lctype, bool useUserOveride)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfoFromLCType] Expected localeName to be not be null");

            // Fix lctype if we don't want overrides
            if (!useUserOveride)
            {
                lctype |= Interop.Kernel32.LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data
            // Failed? Just use empty string
            return GetLocaleInfoEx(localeName, lctype) ?? string.Empty;
        }

        /// <summary>
        /// Reescape a Win32 style quote string as a NLS+ style quoted string
        ///
        /// This is also the escaping style used by custom culture data files
        ///
        /// NLS+ uses \ to escape the next character, whether in a quoted string or
        /// not, so we always have to change \ to \\.
        ///
        /// NLS+ uses \' to escape a quote inside a quoted string so we have to change
        /// '' to \' (if inside a quoted string)
        ///
        /// We don't build the stringbuilder unless we find something to change
        /// </summary>
        internal static string? ReescapeWin32String(string? str)
        {
            // If we don't have data, then don't try anything
            if (str == null)
            {
                return null;
            }

            StringBuilder? result = null;

            bool inQuote = false;
            for (int i = 0; i < str.Length; i++)
            {
                // Look for quote
                if (str[i] == '\'')
                {
                    // Already in quote?
                    if (inQuote)
                    {
                        // See another single quote.  Is this '' of 'fred''s' or '''', or is it an ending quote?
                        if (i + 1 < str.Length && str[i + 1] == '\'')
                        {
                            // Found another ', so we have ''.  Need to add \' instead.
                            // 1st make sure we have our stringbuilder
                            if (result == null)
                                result = new StringBuilder(str, 0, i, str.Length * 2);

                            // Append a \' and keep going (so we don't turn off quote mode)
                            result.Append("\\'");
                            i++;
                            continue;
                        }

                        // Turning off quote mode, fall through to add it
                        inQuote = false;
                    }
                    else
                    {
                        // Found beginning quote, fall through to add it
                        inQuote = true;
                    }
                }
                // Is there a single \ character?
                else if (str[i] == '\\')
                {
                    // Found a \, need to change it to \\
                    // 1st make sure we have our stringbuilder
                    if (result == null)
                        result = new StringBuilder(str, 0, i, str.Length * 2);

                    // Append our \\ to the string & continue
                    result.Append("\\\\");
                    continue;
                }

                // If we have a builder we need to add our character
                if (result != null)
                    result.Append(str[i]);
            }

            // Unchanged string? , just return input string
            if (result == null)
                return str;

            // String changed, need to use the builder
            return result.ToString();
        }

        internal static string[]? ReescapeWin32Strings(string[]? array)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = ReescapeWin32String(array[i])!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
                }
            }

            return array;
        }

        // If we get a group from windows, then its in 3;0 format with the 0 backwards
        // of how NLS+ uses it (ie: if the string has a 0, then the int[] shouldn't and vice versa)
        // EXCEPT in the case where the list only contains 0 in which NLS and NLS+ have the same meaning.
        private static int[] ConvertWin32GroupString(string win32Str)
        {
            // None of these cases make any sense
            if (win32Str == null || win32Str.Length == 0)
            {
                return (new int[] { 3 });
            }

            if (win32Str[0] == '0')
            {
                return (new int[] { 0 });
            }

            // Since its in n;n;n;n;n format, we can always get the length quickly
            int[] values;
            if (win32Str[win32Str.Length - 1] == '0')
            {
                // Trailing 0 gets dropped. 1;0 -> 1
                values = new int[(win32Str.Length / 2)];
            }
            else
            {
                // Need extra space for trailing zero 1 -> 1;0
                values = new int[(win32Str.Length / 2) + 2];
                values[values.Length - 1] = 0;
            }

            int i;
            int j;
            for (i = 0, j = 0; i < win32Str.Length && j < values.Length; i += 2, j++)
            {
                // Note that this # shouldn't ever be zero, 'cause 0 is only at end
                // But we'll test because its registry that could be anything
                if (win32Str[i] < '1' || win32Str[i] > '9')
                    return new int[] { 3 };

                values[j] = (int)(win32Str[i] - '0');
            }

            return (values);
        }

        private static int ConvertFirstDayOfWeekMonToSun(int iTemp)
        {
            // Convert Mon-Sun to Sun-Sat format
            iTemp++;
            if (iTemp > 6)
            {
                // Wrap Sunday and convert invalid data to Sunday
                iTemp = 0;
            }
            return iTemp;
        }


        // Context for EnumCalendarInfoExEx callback.
        private struct EnumLocaleData
        {
            public string regionName;
            public string? cultureName;
        }

        // EnumSystemLocaleEx callback.
        // [NativeCallable(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumSystemLocalesProc(char* lpLocaleString, uint flags, void* contextHandle)
        {
            ref EnumLocaleData context = ref Unsafe.As<byte, EnumLocaleData>(ref *(byte*)contextHandle);
            try
            {
                string cultureName = new string(lpLocaleString);
                string? regionName = GetLocaleInfoEx(cultureName, Interop.Kernel32.LOCALE_SISO3166CTRYNAME);
                if (regionName != null && regionName.Equals(context.regionName, StringComparison.OrdinalIgnoreCase))
                {
                    context.cultureName = cultureName;
                    return Interop.BOOL.FALSE; // we found a match, then stop the enumeration
                }

                return Interop.BOOL.TRUE;
            }
            catch (Exception)
            {
                return Interop.BOOL.FALSE;
            }
        }

        // EnumSystemLocaleEx callback.
        // [NativeCallable(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumAllSystemLocalesProc(char* lpLocaleString, uint flags, void* contextHandle)
        {
            ref EnumData context = ref Unsafe.As<byte, EnumData>(ref *(byte*)contextHandle);
            try
            {
                context.strings.Add(new string(lpLocaleString));
                return Interop.BOOL.TRUE;
            }
            catch (Exception)
            {
                return Interop.BOOL.FALSE;
            }
        }

        // Context for EnumTimeFormatsEx callback.
        private struct EnumData
        {
            public List<string> strings;
        }

        // EnumTimeFormatsEx callback itself.
        // [NativeCallable(CallingConvention = CallingConvention.StdCall)]
        private static unsafe Interop.BOOL EnumTimeCallback(char* lpTimeFormatString, void* lParam)
        {
            ref EnumData context = ref Unsafe.As<byte, EnumData>(ref *(byte*)lParam);
            try
            {
                context.strings.Add(new string(lpTimeFormatString));
                return Interop.BOOL.TRUE;
            }
            catch (Exception)
            {
                return Interop.BOOL.FALSE;
            }
        }

        private static unsafe string[]? nativeEnumTimeFormats(string localeName, uint dwFlags, bool useUserOverride)
        {
            EnumData data = new EnumData();
            data.strings = new List<string>();

            // Now call the enumeration API. Work is done by our callback function
            Interop.Kernel32.EnumTimeFormatsEx(EnumTimeCallback, localeName, (uint)dwFlags, Unsafe.AsPointer(ref data));

            if (data.strings.Count > 0)
            {
                // Now we need to allocate our stringarray and populate it
                string[] results = data.strings.ToArray();

                if (!useUserOverride && data.strings.Count > 1)
                {
                    // Since there is no "NoUserOverride" aware EnumTimeFormatsEx, we always get an override
                    // The override is the first entry if it is overriden.
                    // We can check if we have overrides by checking the GetLocaleInfo with no override
                    // If we do have an override, we don't know if it is a user defined override or if the
                    // user has just selected one of the predefined formats so we can't just remove it
                    // but we can move it down.
                    uint lcType = (dwFlags == Interop.Kernel32.TIME_NOSECONDS) ? Interop.Kernel32.LOCALE_SSHORTTIME : Interop.Kernel32.LOCALE_STIMEFORMAT;
                    string timeFormatNoUserOverride = GetLocaleInfoFromLCType(localeName, lcType, useUserOverride);
                    if (timeFormatNoUserOverride != "")
                    {
                        string firstTimeFormat = results[0];
                        if (timeFormatNoUserOverride != firstTimeFormat)
                        {
                            results[0] = results[1];
                            results[1] = firstTimeFormat;
                        }
                    }
                }

                return results;
            }

            return null;
        }

        private static int LocaleNameToLCID(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.Kernel32.LocaleNameToLCID(cultureName, Interop.Kernel32.LOCALE_ALLOW_NEUTRAL_NAMES);
        }

        private static unsafe string? LCIDToLocaleName(int culture)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            char *pBuffer = stackalloc char[Interop.Kernel32.LOCALE_NAME_MAX_LENGTH + 1]; // +1 for the null termination
            int length = Interop.Kernel32.LCIDToLocaleName(culture, pBuffer, Interop.Kernel32.LOCALE_NAME_MAX_LENGTH + 1, Interop.Kernel32.LOCALE_ALLOW_NEUTRAL_NAMES);

            if (length > 0)
            {
                return new string(pBuffer);
            }

            return null;
        }

        private int GetAnsiCodePage(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.AnsiCodePage);
        }

        private int GetOemCodePage(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.OemCodePage);
        }

        private int GetMacCodePage(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.MacCodePage);
        }

        private int GetEbcdicCodePage(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.EbcdicCodePage);
        }

        private int GetGeoId(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.GeoId);
        }

        private int GetDigitSubstitution(string cultureName)
        {
            return GetLocaleInfo(LocaleNumberData.DigitSubstitution);
        }

        private string GetThreeLetterWindowsLanguageName(string cultureName)
        {
            return GetLocaleInfo(cultureName, LocaleStringData.AbbreviatedWindowsLanguageName);
        }

        private static CultureInfo[] EnumCultures(CultureTypes types)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            uint flags = 0;

#pragma warning disable 618
            if ((types & (CultureTypes.FrameworkCultures | CultureTypes.InstalledWin32Cultures | CultureTypes.ReplacementCultures)) != 0)
            {
                flags |= Interop.Kernel32.LOCALE_NEUTRALDATA | Interop.Kernel32.LOCALE_SPECIFICDATA;
            }
#pragma warning restore 618

            if ((types & CultureTypes.NeutralCultures) != 0)
            {
                flags |= Interop.Kernel32.LOCALE_NEUTRALDATA;
            }

            if ((types & CultureTypes.SpecificCultures) != 0)
            {
                flags |= Interop.Kernel32.LOCALE_SPECIFICDATA;
            }

            if ((types & CultureTypes.UserCustomCulture) != 0)
            {
                flags |= Interop.Kernel32.LOCALE_SUPPLEMENTAL;
            }

            if ((types & CultureTypes.ReplacementCultures) != 0)
            {
                flags |= Interop.Kernel32.LOCALE_SUPPLEMENTAL;
            }

            EnumData context = new EnumData();
            context.strings = new List<string>();

            unsafe
            {
                Interop.Kernel32.EnumSystemLocalesEx(EnumAllSystemLocalesProc, flags, Unsafe.AsPointer(ref context), IntPtr.Zero);
            }

            CultureInfo [] cultures = new CultureInfo[context.strings.Count];
            for (int i = 0; i < cultures.Length; i++)
            {
                cultures[i] = new CultureInfo(context.strings[i]);
            }

            return cultures;
        }

        private string GetConsoleFallbackName(string cultureName)
        {
            return GetLocaleInfo(cultureName, LocaleStringData.ConsoleFallbackName);
        }

        internal bool IsFramework
        {
            get { return false; }
        }

        internal bool IsWin32Installed
        {
            get { return true; }
        }

        internal bool IsReplacementCulture
        {
            get
            {
                EnumData context = new EnumData();
                context.strings = new List<string>();

                unsafe
                {
                    Interop.Kernel32.EnumSystemLocalesEx(EnumAllSystemLocalesProc, Interop.Kernel32.LOCALE_REPLACEMENT, Unsafe.AsPointer(ref context), IntPtr.Zero);
                }

                for (int i=0; i<context.strings.Count; i++)
                {
                    if (string.Compare(context.strings[i], _sWindowsName, StringComparison.OrdinalIgnoreCase) == 0)
                        return true;
                }

                return false;
            }
        }
    }
}
