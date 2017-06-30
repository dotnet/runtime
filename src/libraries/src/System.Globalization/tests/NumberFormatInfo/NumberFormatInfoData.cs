// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization.Tests
{
    internal static class NumberFormatInfoData
    {
        public static int[] UrINNumberGroupSizes()
        {
            if (
                (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion >= 10)
#if !uap
                ||
                (PlatformDetection.IsOSX && PlatformDetection.OSXKernelVersion >= new Version(15, 0))
#endif
                || (PlatformDetection.IsUbuntu && !PlatformDetection.IsUbuntu1404) || PlatformDetection.IsFedora
               )
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
                    return PlatformDetection.IsWindows ? new int[] { 0 } : new int[] { 1, 0 };

                case "en-CA":
                    return PlatformDetection.IsWindows ? new int[] { 1 } : new int[] { 1, 0 };

                case "fa-IR":
                    if (PlatformDetection.IsWindows)
                    {
                        return (PlatformDetection.WindowsVersion < 10) ? new int[] { 3 } : new int[] { 6, 3 };
                    }
                    else
                    {
                        return new int[] { 1, 0 };
                    }

                case "fr-CD":
                    if (PlatformDetection.IsWindows)
                    {
                        return (PlatformDetection.WindowsVersion < 10) ? new int[] { 4 } : new int[] { 8 };
                    }
                    else
                    {
                        return new int[] { 8, 15 };
                    }

                case "as":
                    return PlatformDetection.IsWindows ? new int[] { 12 } : new int[] { 9 };

                case "es-BO":
                    return (PlatformDetection.IsWindows && PlatformDetection.WindowsVersion < 10) ? new int[] { 14 } : new int[] { 1 };

                case "fr-CA":
                    return PlatformDetection.IsWindows ? new int[] { 15 } : new int[] { 8, 15 };
            }

            throw DateTimeFormatInfoData.GetCultureNotSupportedException(new CultureInfo(localeName));
        }
    }
}
