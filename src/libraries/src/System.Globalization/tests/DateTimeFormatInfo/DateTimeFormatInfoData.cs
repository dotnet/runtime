// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

namespace System.Globalization.Tests
{
    internal static class DateTimeFormatInfoData
    {
        private static bool s_isWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);

        public static string GetEraName(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return s_isWindows ? "A.D." : "AD";
            }
            if (string.Equals(cultureInfo.Name, "fr-FR", StringComparison.OrdinalIgnoreCase))
            {
                return "ap. J.-C.";
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        public static string GetAbbreviatedEraName(CultureInfo cultureInfo)
        {
            if (string.Equals(cultureInfo.Name, "en-US", StringComparison.OrdinalIgnoreCase))
            {
                return s_isWindows ? "AD" : "A";
            }
            if (string.Equals(cultureInfo.Name, "ja-JP", StringComparison.OrdinalIgnoreCase) &&
                cultureInfo.Calendar.GetType() == typeof(GregorianCalendar))
            {
                //For Windows<Win7 and others, the default calendar is Gregorian Calendar, AD is expected to be the Era Name
                // CLDR has the Japanese abbreviated era name for the Gregorian Calendar in English - "AD",
                // so for non-Windows machines it will be "AD".
                return s_isWindows ? "\u897F\u66A6" : "AD";
            }

            throw GetCultureNotSupportedException(cultureInfo);
        }

        private static Exception GetCultureNotSupportedException(CultureInfo cultureInfo)
        {
            return new NotSupportedException(string.Format("The culture '{0}' with calendar '{1}' is not supported.", 
                cultureInfo.Name, 
                cultureInfo.Calendar.GetType().Name));
        }
    }
}
