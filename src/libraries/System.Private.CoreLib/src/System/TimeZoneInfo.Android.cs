// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System
{
    public sealed partial class TimeZoneInfo
    {
        // Mitchell - Why isn't this just instantiated in TimeZoneInfo.cs?
        private static readonly TimeZoneInfo s_utcTimeZone = CreateUtcTimeZone();

        /// <summary>
        /// Returns a cloned array of AdjustmentRule objects
        /// </summary>
        public AdjustmentRule[] GetAdjustmentRules()
        {
            return Array.Empty<AdjustmentRule>();
            // TODO, called in tests, implemented in Unix/Win32
        }

        private static void PopulateAllSystemTimeZones(CachedData cachedData)
        {
            // TODO, called in TimeZoneInfo.cs, implemented in Unix/Win32
        }

        /// <summary>
        /// Helper function for retrieving the local system time zone.
        /// May throw COMException, TimeZoneNotFoundException, InvalidTimeZoneException.
        /// Assumes cachedData lock is taken.
        /// </summary>
        /// <returns>A new TimeZoneInfo instance.</returns>
        private static TimeZoneInfo GetLocalTimeZone(CachedData cachedData)
        {
            return Utc;
            // TODO, called in TimeZoneInfo.cs, implemented in Unix/Win32
        }

        private static TimeZoneInfoResult TryGetTimeZoneFromLocalMachine(string id, out TimeZoneInfo? value, out Exception? e)
        {
            value = null;
            e = null;
            return TimeZoneInfoResult.Success;
            // TODO, called in TimeZoneInfo.cs, implemented in Unix/Win32
        }

        /// <summary>
        /// Helper function for retrieving a TimeZoneInfo object by time_zone_name.
        /// This function wraps the logic necessary to keep the private
        /// SystemTimeZones cache in working order
        ///
        /// This function will either return a valid TimeZoneInfo instance or
        /// it will throw 'InvalidTimeZoneException' / 'TimeZoneNotFoundException'.
        /// </summary>
        public static TimeZoneInfo FindSystemTimeZoneById(string id)
        {
            return Utc;
            // TODO, called in TimeZoneInfo.cs, implemented in Unix/Win32
        }

        // DateTime.Now fast path that avoids allocating an historically accurate TimeZoneInfo.Local and just creates a 1-year (current year) accurate time zone
        internal static TimeSpan GetDateTimeNowUtcOffsetFromUtc(DateTime time, out bool isAmbiguousLocalDst)
        {
            bool isDaylightSavings;
            return GetUtcOffsetFromUtc(time, Local, out isDaylightSavings, out isAmbiguousLocalDst);
            // TODO, called in DateTime.cs, implemented in Unix/Win32
        }

        // Helper function for string array search. (LINQ is not available here.)
        private static bool StringArrayContains(string value, string[] source, StringComparison comparison)
        {
            foreach (string s in source)
            {
                if (string.Equals(s, value, comparison))
                {
                    return true;
                }
            }

            return false;
        }
    }
}