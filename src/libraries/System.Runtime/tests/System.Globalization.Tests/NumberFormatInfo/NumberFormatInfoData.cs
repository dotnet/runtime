// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Globalization.Tests
{
    internal static class NumberFormatInfoData
    {
        public static int[] UrINNumberGroupSizes()
        {
            if (PlatformDetection.WindowsVersion >= 10 || PlatformDetection.ICUVersion.Major >= 55 || PlatformDetection.IsHybridGlobalizationOnApplePlatform)
            {
                return new int[] { 3 };
            }
            else
            {
                // Fedora, Ubuntu 14.04, <= Windows 8
                return new int[] { 3, 2 };
            }
        }

        internal static int[] GetCurrencyNegativePatterns(string localeName)
        {
            // CentOS uses an older ICU than Ubuntu, which means the "Linux" values need to allow for
            // multiple values, since we can't tell which version of ICU we are using, or whether we are
            // on CentOS or Ubuntu.
            // When multiple values are returned, the "older" ICU value is returned last.

            switch (localeName)
            {
                case "en-US":
                    return PlatformDetection.IsNlsGlobalization ? new int[] { 0 } : new int[] { 1, 0 };

                case "en-CA":
                    return PlatformDetection.IsNlsGlobalization ? new int[] { 1 } : new int[] { 1, 0 };

                case "fa-IR":
                    if (PlatformDetection.IsNlsGlobalization)
                    {
                        return (PlatformDetection.WindowsVersion < 10) ? new int[] { 3 } : new int[] { 6, 3 };
                    }
                    if (PlatformDetection.ICUVersion.Major == 59 || PlatformDetection.ICUVersion.Major == 58)
                    {
                        return new int[] { 8 };
                    }
                    else if (PlatformDetection.ICUVersion.Major > 59)
                    {
                        return new int[] { 1 };
                    }
                    else
                    {
                        return new int[] { 1, 0 };
                    }

                case "fr-CD":
                    if (PlatformDetection.IsNlsGlobalization)
                    {
                        return (PlatformDetection.WindowsVersion < 10) ? new int[] { 4 } : new int[] { 8 };
                    }
                    else
                    {
                        return new int[] { 8, 15 };
                    }

                case "as":
                    return PlatformDetection.IsNlsGlobalization ? new int[] { 12 } : new int[] { 9 };

                case "es-BO":
                    return (PlatformDetection.IsNlsGlobalization && PlatformDetection.WindowsVersion < 10) ? 
                                new int[] { 14 } : 
                                // Mac OSX used to return 1 which is the format "-$n". OSX Version 12 (Monterey) started
                                // to return a different value 12 "$ -n". 
                                PlatformDetection.IsOSX ? new int[] { 1, 12 } : new int[] { 1 };

                case "fr-CA":
                    return PlatformDetection.IsNlsGlobalization ? new int[] { 15 } : new int[] { 8, 15 };
            }

            throw DateTimeFormatInfoData.GetCultureNotSupportedException(CultureInfo.GetCultureInfo(localeName));
        }
    }
}
