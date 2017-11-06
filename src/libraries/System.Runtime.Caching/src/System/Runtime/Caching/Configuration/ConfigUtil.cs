// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Runtime.Caching.Resources;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;

namespace System.Runtime.Caching.Configuration
{
    internal static class ConfigUtil
    {
        internal const string CacheMemoryLimitMegabytes = "cacheMemoryLimitMegabytes";
        internal const string PhysicalMemoryLimitPercentage = "physicalMemoryLimitPercentage";
        internal const string PollingInterval = "pollingInterval";
        internal const string UseMemoryCacheManager = "useMemoryCacheManager";
        internal const int DefaultPollingTimeMilliseconds = 120000;

        internal static int GetIntValue(NameValueCollection config, string valueName, int defaultValue, bool zeroAllowed, int maxValueAllowed)
        {
            string sValue = config[valueName];

            if (sValue == null)
            {
                return defaultValue;
            }

            int iValue;
            if (!Int32.TryParse(sValue, out iValue)
                || iValue < 0
                || (!zeroAllowed && iValue == 0))
            {
                if (zeroAllowed)
                {
                    throw new ArgumentException(RH.Format(SR.Value_must_be_non_negative_integer, valueName, sValue), "config");
                }

                throw new ArgumentException(RH.Format(SR.Value_must_be_positive_integer, valueName, sValue), "config");
            }

            if (maxValueAllowed > 0 && iValue > maxValueAllowed)
            {
                throw new ArgumentException(RH.Format(SR.Value_too_big,
                                                      valueName,
                                                      sValue,
                                                      maxValueAllowed.ToString(CultureInfo.InvariantCulture)), "config");
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
                return Int32.MaxValue;
            }

            TimeSpan tValue;
            if (!TimeSpan.TryParse(sValue, out tValue) || tValue <= TimeSpan.Zero)
            {
                throw new ArgumentException(RH.Format(SR.TimeSpan_invalid_format, valueName, sValue), "config");
            }

            double milliseconds = tValue.TotalMilliseconds;
            int iValue = (milliseconds < (double)Int32.MaxValue) ? (int)milliseconds : Int32.MaxValue;
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
            if (!Boolean.TryParse(sValue, out bValue))
            {
                throw new ArgumentException(RH.Format(SR.Value_must_be_boolean, valueName, sValue), "config");
            }

            return bValue;
        }
    }
}
