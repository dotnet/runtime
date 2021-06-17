// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Internal.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
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
            GetLocaleInfoEx(localeName, field, (char*)&value, sizeof(int));
            return value;
        }

        internal static unsafe int GetLocaleInfoEx(string lpLocaleName, uint lcType, char* lpLCData, int cchData)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.Kernel32.GetLocaleInfoEx(lpLocaleName, lcType, lpLCData, cchData);
        }

        private string NlsGetLocaleInfo(LocaleStringData type)
        {
            Debug.Assert(ShouldUseUserOverrideNlsData);
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfo] Expected _sWindowsName to be populated by already");
            return NlsGetLocaleInfo(_sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private string NlsGetLocaleInfo(string localeName, LocaleStringData type)
        {
            Debug.Assert(ShouldUseUserOverrideNlsData);
            uint lctype = (uint)type;

            return GetLocaleInfoFromLCType(localeName, lctype, _bUseOverrides);
        }

        private int NlsGetLocaleInfo(LocaleNumberData type)
        {
            Debug.Assert(IsWin32Installed);
            uint lctype = (uint)type;

            // Fix lctype if we don't want overrides
            if (!_bUseOverrides)
            {
                lctype |= Interop.Kernel32.LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data, note that we presume it returns success, so we have to know that
            // sWindowsName is valid before calling.
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return GetLocaleInfoExInt(_sWindowsName, lctype);
        }

        private int[] NlsGetLocaleInfo(LocaleGroupingData type)
        {
            Debug.Assert(ShouldUseUserOverrideNlsData);
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return ConvertWin32GroupString(GetLocaleInfoFromLCType(_sWindowsName, (uint)type, _bUseOverrides));
        }

        internal static bool NlsIsEnsurePredefinedLocaleName(string name)
        {
            Debug.Assert(GlobalizationMode.UseNls);
            return CultureData.GetLocaleInfoExInt(name, Interop.Kernel32.LOCALE_ICONSTRUCTEDLOCALE) != 1;
        }

        private string? NlsGetTimeFormatString()
        {
            Debug.Assert(ShouldUseUserOverrideNlsData);
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");
            return ReescapeWin32String(GetLocaleInfoFromLCType(_sWindowsName, Interop.Kernel32.LOCALE_STIMEFORMAT, _bUseOverrides));
        }

        private int NlsGetFirstDayOfWeek()
        {
            Debug.Assert(ShouldUseUserOverrideNlsData);
            Debug.Assert(_sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected _sWindowsName to be populated by already");

            int result = GetLocaleInfoExInt(_sWindowsName, Interop.Kernel32.LOCALE_IFIRSTDAYOFWEEK | (!_bUseOverrides ? Interop.Kernel32.LOCALE_NOUSEROVERRIDE : 0));

            // Win32 and .NET disagree on the numbering for days of the week, so we have to convert.
            return ConvertFirstDayOfWeekMonToSun(result);
        }

        // Enumerate all system cultures and then try to find out which culture has
        // region name match the requested region name
        private static CultureData? NlsGetCultureDataFromRegionName(string regionName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);
            Debug.Assert(regionName != null);

            EnumLocaleData context;
            context.cultureName = null;
            context.regionName = regionName;

            unsafe
            {
                Interop.Kernel32.EnumSystemLocalesEx(&EnumSystemLocalesProc, Interop.Kernel32.LOCALE_SPECIFICDATA | Interop.Kernel32.LOCALE_SUPPLEMENTAL, Unsafe.AsPointer(ref context), IntPtr.Zero);
            }

            if (context.cultureName != null)
            {
                // we got a matched culture
                return GetCultureData(context.cultureName, true);
            }

            return null;
        }

        private string NlsGetLanguageDisplayName(string cultureName)
        {
            Debug.Assert(GlobalizationMode.UseNls);

            // Usually the UI culture shouldn't be different than what we got from WinRT except
            // if DefaultThreadCurrentUICulture was set
            CultureInfo? ci;

            if (CultureInfo.DefaultThreadCurrentUICulture != null &&
                ((ci = CultureInfo.GetUserDefaultCulture()) != null) &&
                !CultureInfo.DefaultThreadCurrentUICulture.Name.Equals(ci.Name))
            {
                return NativeName;
            }
            else
            {
                return NlsGetLocaleInfo(cultureName, LocaleStringData.LocalizedDisplayName);
            }
        }

        private string NlsGetRegionDisplayName()
        {
            Debug.Assert(GlobalizationMode.UseNls);

            // If the current UI culture matching the OS UI language, we'll get the display name from the OS.
            // otherwise, we use the native name as we don't carry resources for the region display names anyway.
            if (CultureInfo.CurrentUICulture.Name.Equals(CultureInfo.UserDefaultUICulture.Name))
            {
                return NlsGetLocaleInfo(LocaleStringData.LocalizedCountryName);
            }

            return NativeCountryName;
        }

        // PAL methods end here.

        private static string GetLocaleInfoFromLCType(string localeName, uint lctype, bool useUserOverride)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfoFromLCType] Expected localeName to be not be null");

            // Fix lctype if we don't want overrides
            if (!useUserOverride)
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
        [return: NotNullIfNotNull("str")]
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
                            result ??= new StringBuilder(str, 0, i, str.Length * 2);

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
                    result ??= new StringBuilder(str, 0, i, str.Length * 2);

                    // Append our \\ to the string & continue
                    result.Append("\\\\");
                    continue;
                }

                // If we have a builder we need to add our character
                result?.Append(str[i]);
            }

            // Unchanged string? , just return input string
            if (result == null)
                return str;

            // String changed, need to use the builder
            return result.ToString();
        }

        [return: NotNullIfNotNull("array")]
        internal static string[]? ReescapeWin32Strings(string[]? array)
        {
            if (array != null)
            {
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = ReescapeWin32String(array[i]);
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
            if (string.IsNullOrEmpty(win32Str))
            {
                return new int[] { 3 };
            }

            if (win32Str[0] == '0')
            {
                return new int[] { 0 };
            }

            // Since its in n;n;n;n;n format, we can always get the length quickly
            int[] values;
            if (win32Str[^1] == '0')
            {
                // Trailing 0 gets dropped. 1;0 -> 1
                values = new int[win32Str.Length / 2];
            }
            else
            {
                // Need extra space for trailing zero 1 -> 1;0
                values = new int[(win32Str.Length / 2) + 2];
                values[^1] = 0;
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

            return values;
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
        [UnmanagedCallersOnly]
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
        [UnmanagedCallersOnly]
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
        [UnmanagedCallersOnly]
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
            EnumData data = default;
            data.strings = new List<string>();

            // Now call the enumeration API. Work is done by our callback function
            Interop.Kernel32.EnumTimeFormatsEx(&EnumTimeCallback, localeName, dwFlags, Unsafe.AsPointer(ref data));

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

        private static int NlsLocaleNameToLCID(string cultureName)
        {
            Debug.Assert(!GlobalizationMode.Invariant);

            return Interop.Kernel32.LocaleNameToLCID(cultureName, Interop.Kernel32.LOCALE_ALLOW_NEUTRAL_NAMES);
        }

        private string NlsGetThreeLetterWindowsLanguageName(string cultureName)
        {
            Debug.Assert(GlobalizationMode.UseNls);
            return NlsGetLocaleInfo(cultureName, LocaleStringData.AbbreviatedWindowsLanguageName);
        }

        private static CultureInfo[] NlsEnumCultures(CultureTypes types)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(GlobalizationMode.UseNls);

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

            EnumData context = default;
            context.strings = new List<string>();

            unsafe
            {
                Interop.Kernel32.EnumSystemLocalesEx(&EnumAllSystemLocalesProc, flags, Unsafe.AsPointer(ref context), IntPtr.Zero);
            }

            CultureInfo[] cultures = new CultureInfo[context.strings.Count];
            for (int i = 0; i < cultures.Length; i++)
            {
                cultures[i] = new CultureInfo(context.strings[i]);
            }

            return cultures;
        }

        private string NlsGetConsoleFallbackName(string cultureName)
        {
            Debug.Assert(GlobalizationMode.UseNls);
            return NlsGetLocaleInfo(cultureName, LocaleStringData.ConsoleFallbackName);
        }

        internal bool NlsIsReplacementCulture
        {
            get
            {
                Debug.Assert(GlobalizationMode.UseNls);
                EnumData context = default;
                context.strings = new List<string>();

                unsafe
                {
                    Interop.Kernel32.EnumSystemLocalesEx(&EnumAllSystemLocalesProc, Interop.Kernel32.LOCALE_REPLACEMENT, Unsafe.AsPointer(ref context), IntPtr.Zero);
                }

                for (int i = 0; i < context.strings.Count; i++)
                {
                    if (string.Equals(context.strings[i], _sWindowsName, StringComparison.OrdinalIgnoreCase))
                        return true;
                }

                return false;
            }
        }
    }
}
