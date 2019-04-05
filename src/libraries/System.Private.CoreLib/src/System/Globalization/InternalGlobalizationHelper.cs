// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable
namespace System.Globalization
{
    internal class InternalGlobalizationHelper
    {
        // Copied from the TimeSpan to be used inside the globalization code and avoid internal dependency on TimeSpan class
        internal static long TimeToTicks(int hour, int minute, int second)
        {
            // totalSeconds is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            long totalSeconds = (long)hour * 3600 + (long)minute * 60 + (long)second;
            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new ArgumentOutOfRangeException(null, SR.Overflow_TimeSpanTooLong);
            return totalSeconds * TicksPerSecond;
        }


        //
        // Define needed constants so globalization code can be independant from any other types
        //

        internal const long TicksPerMillisecond = 10000;
        internal const long TicksPerTenthSecond = TicksPerMillisecond * 100;
        internal const long TicksPerSecond = TicksPerMillisecond * 1000;   // 10,000,000
        internal const long MaxSeconds = long.MaxValue / TicksPerSecond;
        internal const long MinSeconds = long.MinValue / TicksPerSecond;
        private const int DaysPerYear = 365;
        private const int DaysPer4Years = DaysPerYear * 4 + 1;       // 1461
        private const int DaysPer100Years = DaysPer4Years * 25 - 1;  // 36524
        private const int DaysPer400Years = DaysPer100Years * 4 + 1; // 146097
        private const int DaysTo10000 = DaysPer400Years * 25 - 366;  // 3652059
        private const long TicksPerMinute = TicksPerSecond * 60;
        private const long TicksPerHour = TicksPerMinute * 60;
        private const long TicksPerDay = TicksPerHour * 24;
        internal const long MaxTicks = DaysTo10000 * TicksPerDay - 1;
        internal const long MinTicks = 0;
        internal const long MaxMilliSeconds = long.MaxValue / TicksPerMillisecond;
        internal const long MinMilliSeconds = long.MinValue / TicksPerMillisecond;

        internal const int StringBuilderDefaultCapacity = 16;

        internal const long MaxOffset = TimeSpan.TicksPerHour * 14;
        internal const long MinOffset = -MaxOffset;
    }
}
