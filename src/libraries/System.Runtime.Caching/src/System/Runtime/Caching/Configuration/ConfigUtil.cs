// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.Caching.Resources;

namespace System.Runtime.Caching.Configuration
{
    internal static class ConfigUtil
    {
        internal const string CacheMemoryLimitMegabytes = "cacheMemoryLimitMegabytes";
        internal const string PhysicalMemoryLimitPercentage = "physicalMemoryLimitPercentage";
        internal const string PollingInterval = "pollingInterval";
        internal const string UseMemoryCacheManager = "useMemoryCacheManager";
        internal const string ThrowOnDisposed = "throwOnDisposed";
        internal const int DefaultPollingTimeMilliseconds = 120000;

        internal static int GetIntValue(NameValueCollection config, string valueName, int defaultValue, bool zeroAllowed, int maxValueAllowed)
        {
            string sValue = config[valueName];

            if (sValue == null)
            {
                return defaultValue;
            }

            int iValue;
            if (!int.TryParse(sValue, out iValue)
                || iValue < 0
                || (!zeroAllowed && iValue == 0))
            {
                if (zeroAllowed)
                {
                    throw new ArgumentException(RH.Format(SR.Value_must_be_non_negative_integer, valueName, sValue), nameof(config));
                }

                throw new ArgumentException(RH.Format(SR.Value_must_be_positive_integer, valueName, sValue), nameof(config));
            }

            if (maxValueAllowed > 0 && iValue > maxValueAllowed)
            {
                throw new ArgumentException(RH.Format(SR.Value_too_big,
                                                      valueName,
                                                      sValue,
                                                      maxValueAllowed.ToString(CultureInfo.InvariantCulture)), nameof(config));
            }

            return iValue;
        }

        internal static int GetIntValueFromTimeSpan(NameValueCollection config, string valueName, int defaultValue)
        {
            string sValue = config[valueName];

            if (sValue == null)
            {
                return defaultValue;
            }

            if (sValue == "Infinite")
            {
                return int.MaxValue;
            }

            TimeSpan tValue;
            if (!TimeSpan.TryParse(sValue, out tValue) || tValue <= TimeSpan.Zero)
            {
                throw new ArgumentException(RH.Format(SR.TimeSpan_invalid_format, valueName, sValue), nameof(config));
            }

            double milliseconds = tValue.TotalMilliseconds;
            int iValue = (milliseconds < (double)int.MaxValue) ? (int)milliseconds : int.MaxValue;
            return iValue;
        }

        internal static bool GetBooleanValue(NameValueCollection config, string valueName, bool defaultValue)
        {
            string sValue = config[valueName];

            if (sValue == null)
            {
                return defaultValue;
            }

            bool bValue;
            if (!bool.TryParse(sValue, out bValue))
            {
                throw new ArgumentException(RH.Format(SR.Value_must_be_boolean, valueName, sValue), nameof(config));
            }

            return bValue;
        }
    }
}
