using System.Collections.Generic;
using System.Diagnostics.Contracts;
using System.Runtime.InteropServices;
using System.Text;

using Internal.Runtime.Augments;

namespace System.Globalization
{
    internal partial class CultureData
    {
        private const uint LOCALE_NOUSEROVERRIDE = 0x80000000;
        private const uint LOCALE_RETURN_NUMBER = 0x20000000;
        private const uint LOCALE_SISO3166CTRYNAME = 0x0000005A;

        private const uint TIME_NOSECONDS = 0x00000002;
        
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
            const int LOCALE_NAME_MAX_LENGTH = 85;

            const uint LOCALE_ILANGUAGE = 0x00000001;
            const uint LOCALE_INEUTRAL = 0x00000071;
            const uint LOCALE_SNAME = 0x0000005c;
            
            int result;
            string realNameBuffer = this.sRealName;
            char* pBuffer = stackalloc char[LOCALE_NAME_MAX_LENGTH];

            result = Interop.mincore.GetLocaleInfoEx(realNameBuffer, LOCALE_SNAME, pBuffer, LOCALE_NAME_MAX_LENGTH);

            // Did it fail?
            if (result == 0)
            {
                return false;
            }

            // It worked, note that the name is the locale name, so use that (even for neutrals)
            // We need to clean up our "real" name, which should look like the windows name right now
            // so overwrite the input with the cleaned up name
            this.sRealName = new String(pBuffer, 0, result - 1);
            realNameBuffer = this.sRealName;

            // Check for neutrality, don't expect to fail
            // (buffer has our name in it, so we don't have to do the gc. stuff)

            result = Interop.mincore.GetLocaleInfoEx(realNameBuffer, LOCALE_INEUTRAL | LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
            if (result == 0)
            {
                return false;
            }

            // Remember our neutrality
            this.bNeutral = *((uint*)pBuffer) != 0;

            // Note: Parents will be set dynamically

            // Start by assuming the windows name'll be the same as the specific name since windows knows
            // about specifics on all versions.  Only for downlevel Neutral locales does this have to change.
            this.sWindowsName = realNameBuffer;

            // Neutrals and non-neutrals are slightly different
            if (this.bNeutral)
            {
                // Neutral Locale

                // IETF name looks like neutral name
                this.sName = realNameBuffer;

                // Specific locale name is whatever ResolveLocaleName (win7+) returns.
                // (Buffer has our name in it, and we can recycle that because windows resolves it before writing to the buffer)
                result = Interop.mincore.ResolveLocaleName(realNameBuffer, pBuffer, LOCALE_NAME_MAX_LENGTH);

                // 0 is failure, 1 is invariant (""), which we expect
                if (result < 1)
                {
                    return false;
                }
                // We found a locale name, so use it.
                // In vista this should look like a sort name (de-DE_phoneb) or a specific culture (en-US) and be in the "pretty" form
                this.sSpecificCulture = new String(pBuffer, 0, result - 1);
            }
            else
            {
                // Specific Locale

                // Specific culture's the same as the locale name since we know its not neutral
                // On mac we'll use this as well, even for neutrals. There's no obvious specific
                // culture to use and this isn't exposed, but behaviorally this is correct on mac.
                // Note that specifics include the sort name (de-DE_phoneb)
                this.sSpecificCulture = realNameBuffer;

                this.sName = realNameBuffer;

                // We need the IETF name (sname)
                // If we aren't an alt sort locale then this is the same as the windows name.
                // If we are an alt sort locale then this is the same as the part before the _ in the windows name
                // This is for like de-DE_phoneb and es-ES_tradnl that hsouldn't have the _ part

                result = Interop.mincore.GetLocaleInfoEx(realNameBuffer, LOCALE_ILANGUAGE | LOCALE_RETURN_NUMBER, pBuffer, sizeof(int) / sizeof(char));
                if (result == 0)
                {
                    return false;
                }

                this.iLanguage = *((int*)pBuffer);

                if (!IsCustomCultureId(this.iLanguage))
                {
                    // not custom locale
                    int index = realNameBuffer.IndexOf('_');
                    if (index > 0 && index < realNameBuffer.Length)
                    {
                        this.sName = realNameBuffer.Substring(0, index);
                    }
                }
            }

            // It succeeded.
            return true;
        }

        [System.Security.SecurityCritical]
        private string GetLocaleInfo(LocaleStringData type)
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfo] Expected this.sWindowsName to be populated by already");
            return GetLocaleInfo(this.sWindowsName, type);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        [System.Security.SecurityCritical]
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
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data, note that we presume it returns success, so we have to know that
            // sWindowsName is valid before calling.
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected this.sWindowsName to be populated by already");
            int result = Interop.mincore.GetLocaleInfoExInt(this.sWindowsName, lctype);

            return result;
        }

        private int[] GetLocaleInfo(LocaleGroupingData type)
        {
            return ConvertWin32GroupString(GetLocaleInfoFromLCType(this.sWindowsName, (uint)type, UseUserOverride));
        }

        private string GetTimeFormatString()
        {
            const uint LOCALE_STIMEFORMAT = 0x00001003;

            return ReescapeWin32String(GetLocaleInfoFromLCType(this.sWindowsName, LOCALE_STIMEFORMAT, UseUserOverride));
        }

        private int GetFirstDayOfWeek()
        {
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoGetLocaleInfoInt] Expected this.sWindowsName to be populated by already");

            int result = Interop.mincore.GetLocaleInfoExInt(this.sWindowsName, 
                LocaleNumberData.FirstDayOfWeek | (!UseUserOverride ? LOCALE_NOUSEROVERRIDE : 0));

            // Win32 and .NET disagree on the numbering for days of the week, so we have to convert.
            return ConvertFirstDayOfWeekMonToSun(result);
        }

        private String[] GetTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumTimeFormats] Expected this.sWindowsName to be populated by already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, 0, UseUserOverride));

            return result;
        }

        private String[] GetShortTimeFormats()
        {
            // Note that this gets overrides for us all the time
            Contract.Assert(this.sWindowsName != null, "[CultureData.DoEnumShortTimeFormats] Expected this.sWindowsName to be populated by already");
            String[] result = ReescapeWin32Strings(nativeEnumTimeFormats(this.sWindowsName, TIME_NOSECONDS, UseUserOverride));

            return result;
        }

        // Enumerate all system cultures and then try to find out which culture has 
        // region name match the requested region name
        private static CultureData GetCultureDataFromRegionName(String regionName)
        {
            Contract.Assert(regionName != null);
            
            const uint LOCALE_SUPPLEMENTAL = 0x00000002;
            const uint LOCALE_SPECIFICDATA = 0x00000020;

            EnumLocaleData context = new EnumLocaleData();
            context.cultureName = null;
            context.regionName = regionName;

            GCHandle contextHandle = GCHandle.Alloc(context);
            try
            {
                EnumLocalesProcEx callback = new EnumLocalesProcEx(EnumSystemLocalesProc);
                Interop.mincore_private.LParamCallbackContext ctx = new Interop.mincore_private.LParamCallbackContext();
                ctx.lParam = (IntPtr)contextHandle;

                Interop.mincore_obsolete.EnumSystemLocalesEx(callback, LOCALE_SPECIFICDATA | LOCALE_SUPPLEMENTAL, ctx, IntPtr.Zero);
            }
            finally
            {
                contextHandle.Free();
            }

            if (context.cultureName != null)
            {
                // we got a matched culture
                return GetCultureData(context.cultureName, true);
            }

            return null;
        }

        private static string GetLanguageDisplayName(string cultureName)
        {
            return WinRTInterop.Callbacks.GetLanguageDisplayName(cultureName);
        }

        private static string GetRegionDisplayName(string isoCountryCode)
        {
            return WinRTInterop.Callbacks.GetRegionDisplayName(isoCountryCode);
        }

        private static CultureInfo GetUserDefaultCulture()
        {
            return (CultureInfo)WinRTInterop.Callbacks.GetUserDefaultCulture(); 
        }

        // PAL methods end here.

        [System.Security.SecurityCritical]
        private static string GetLocaleInfoFromLCType(string localeName, uint lctype, bool useUserOveride)
        {
            Contract.Assert(localeName != null, "[CultureData.GetLocaleInfoFromLCType] Expected localeName to be not be null");

            // Fix lctype if we don't want overrides
            if (!useUserOveride)
            {
                lctype |= LOCALE_NOUSEROVERRIDE;
            }

            // Ask OS for data
            string result = Interop.mincore.GetLocaleInfoEx(localeName, lctype);
            if (result == null)
            {
                // Failed, just use empty string
                result = String.Empty;
            }

            return result;
        }

        ////////////////////////////////////////////////////////////////////////////
        //
        // Reescape a Win32 style quote string as a NLS+ style quoted string
        //
        // This is also the escaping style used by custom culture data files
        //
        // NLS+ uses \ to escape the next character, whether in a quoted string or
        // not, so we always have to change \ to \\.
        //
        // NLS+ uses \' to escape a quote inside a quoted string so we have to change
        // '' to \' (if inside a quoted string)
        //
        // We don't build the stringbuilder unless we find something to change
        ////////////////////////////////////////////////////////////////////////////
        internal static String ReescapeWin32String(String str)
        {
            // If we don't have data, then don't try anything
            if (str == null)
                return null;

            StringBuilder result = null;

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

        internal static String[] ReescapeWin32Strings(String[] array)
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
        private static int[] ConvertWin32GroupString(String win32Str)
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
        class EnumLocaleData
        {
            public string regionName;
            public string cultureName;
        }

        // EnumSystemLocaleEx callback.
        static unsafe bool EnumSystemLocalesProc(IntPtr lpLocaleString, uint flags, Interop.mincore_private.LParamCallbackContext contextHandle)
        {
            EnumLocaleData context = (EnumLocaleData)((GCHandle)contextHandle.lParam).Target;
            try
            {
                string cultureName = new string((char*)lpLocaleString);
                string regionName = Interop.mincore.GetLocaleInfoEx(cultureName, LOCALE_SISO3166CTRYNAME);
                if (regionName != null && regionName.Equals(context.regionName, StringComparison.OrdinalIgnoreCase))
                {
                    context.cultureName = cultureName;
                    return false; // we found a match, then stop the enumeration
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        // Context for EnumTimeFormatsEx callback.
        class EnumData
        {
            public LowLevelList<string> strings;
        }

        // EnumTimeFormatsEx callback itself.
        private static unsafe bool EnumTimeCallback(IntPtr lpTimeFormatString, Interop.mincore_private.LParamCallbackContext contextHandle)
        {
            EnumData context = (EnumData)((GCHandle)contextHandle.lParam).Target;

            try
            {
                context.strings.Add(new string((char*)lpTimeFormatString));
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        private static unsafe String[] nativeEnumTimeFormats(String localeName, uint dwFlags, bool useUserOverride)
        {
            const uint LOCALE_SSHORTTIME = 0x00000079;
            const uint LOCALE_STIMEFORMAT = 0x00001003;

            EnumData data = new EnumData();
            data.strings = new LowLevelList<string>();

            GCHandle dataHandle = GCHandle.Alloc(data);

            try
            {
                EnumTimeFormatsProcEx callback = new EnumTimeFormatsProcEx(EnumTimeCallback);
                Interop.mincore_private.LParamCallbackContext cxt = new Interop.mincore_private.LParamCallbackContext();
                cxt.lParam = (IntPtr)dataHandle;

                // Now call the enumeration API. Work is done by our callback function
                Interop.mincore_private.EnumTimeFormatsEx(callback, localeName, (uint)dwFlags, cxt);
            }
            finally
            {
                dataHandle.Free();
            }

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
                    uint lcType = (dwFlags == TIME_NOSECONDS) ? LOCALE_SSHORTTIME : LOCALE_STIMEFORMAT;
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


    }
}