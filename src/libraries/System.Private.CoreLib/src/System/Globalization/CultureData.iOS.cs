// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Globalization
{
    internal sealed partial class CultureData
    {
        private const int LOC_FULLNAME_CAPACITY = 157;           // max size of locale name

        internal static string GetLocaleNameNative(string localeName)
        {
            return Interop.Globalization.GetLocaleNameNative(localeName);
        }

        private string GetLocaleInfoNative(LocaleStringData type, string? uiLocaleName = null)
        {
            Debug.Assert(!GlobalizationMode.Invariant);
            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfoNative] Expected _sWindowsName to be populated already");

            return GetLocaleInfoNative(_sWindowsName, type, uiLocaleName);
        }

        // For LOCALE_SPARENT we need the option of using the "real" name (forcing neutral names) instead of the
        // "windows" name, which can be specific for downlevel (< windows 7) os's.
        private static string GetLocaleInfoNative(string localeName, LocaleStringData type, string? uiLocaleName = null)
        {
            Debug.Assert(localeName != null, "[CultureData.GetLocaleInfoNative] Expected localeName to be not be null");

            return Interop.Globalization.GetLocaleInfoStringNative(localeName, (uint)type, uiLocaleName);
        }

        private int GetLocaleInfoNative(LocaleNumberData type)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfoNative(LocaleNumberData)] Expected _sWindowsName to be populated already");

            // returning 0 will cause the first supported calendar to be returned, which is the preferred calendar
            if (type == LocaleNumberData.CalendarType)
                return 0;

            int value = Interop.Globalization.GetLocaleInfoIntNative(_sWindowsName, (uint)type);
            const int DEFAULT_VALUE = 0;
            if (value < 0)
            {
                Debug.Fail("[CultureData.GetLocaleInfoNative(LocaleNumberData)] failed");
                return DEFAULT_VALUE;
            }

            return value;
        }

        private int[] GetLocaleInfoNative(LocaleGroupingData type)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.GetLocaleInfoNative(LocaleGroupingData)] Expected _sWindowsName to be populated already");

            int primaryGroupingSize = Interop.Globalization.GetLocaleInfoPrimaryGroupingSizeNative(_sWindowsName, (uint)type);
            int secondaryGroupingSize = Interop.Globalization.GetLocaleInfoSecondaryGroupingSizeNative(_sWindowsName, (uint)type);

            if (secondaryGroupingSize == 0)
            {
                return new int[] { primaryGroupingSize };
            }

            return new int[] { primaryGroupingSize, secondaryGroupingSize };
        }

        private string GetTimeFormatStringNative() => GetTimeFormatStringNative(shortFormat: false);

        private string GetTimeFormatStringNative(bool shortFormat)
        {
            Debug.Assert(_sWindowsName != null, "[CultureData.GetTimeFormatStringNative(bool shortFormat)] Expected _sWindowsName to be populated already");

            string result = Interop.Globalization.GetLocaleTimeFormatNative(_sWindowsName, shortFormat);

            return ConvertNativeTimeFormatString(result);
        }

        private static string ConvertNativeTimeFormatString(string nativeFormatString)
        {
            Span<char> result = stackalloc char[LOC_FULLNAME_CAPACITY];

            bool amPmAdded = false;
            int resultPos = 0;

            for (int i = 0; i < nativeFormatString.Length; i++)
            {
                switch (nativeFormatString[i])
                {
                    case '\'':
                        result[resultPos++] = nativeFormatString[i++];
                        while (i < nativeFormatString.Length)
                        {
                            char current = nativeFormatString[i];
                            result[resultPos++] = current;
                            if (current == '\'')
                            {
                                break;
                            }
                            i++;
                        }
                        break;

                    case ':':
                    case '.':
                    case 'H':
                    case 'h':
                    case 'm':
                    case 's':
                        result[resultPos++] = nativeFormatString[i];
                        break;

                    case ' ':
                    case '\u00A0':
                        // Convert nonbreaking spaces into regular spaces
                        result[resultPos++] = ' ';
                        break;

                    case 'a': // AM/PM
                        if (!amPmAdded)
                        {
                            amPmAdded = true;
                            result[resultPos++] = 't';
                            result[resultPos++] = 't';
                        }
                        break;

                }
            }

            return result.Slice(0, resultPos).ToString();
        }
    }
}
