// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: 
** This class is used to represent a TimeZone.  It
** has methods for converting a DateTime to UTC from local time
** and to local time from UTC and methods for getting the 
** standard name and daylight name of the time zone.  
**
** The only TimeZone that we support in version 1 is the 
** CurrentTimeZone as determined by the system timezone.
**
**
============================================================*/

#nullable enable
using System;
using System.Text;
using System.Threading;
using System.Collections;
using System.Globalization;

namespace System
{
    [Obsolete("System.TimeZone has been deprecated.  Please investigate the use of System.TimeZoneInfo instead.")]
    public abstract class TimeZone
    {
        private static volatile TimeZone? currentTimeZone = null;

        // Private object for locking instead of locking on a public type for SQL reliability work.
        private static object? s_InternalSyncObject;
        private static object InternalSyncObject
        {
            get
            {
                if (s_InternalSyncObject == null)
                {
                    object o = new object();
                    Interlocked.CompareExchange<object?>(ref s_InternalSyncObject, o, null);
                }
                return s_InternalSyncObject!; // TODO-NULLABLE: https://github.com/dotnet/roslyn/issues/26761
            }
        }


        protected TimeZone()
        {
        }

        public static TimeZone CurrentTimeZone
        {
            get
            {
                //Grabbing the cached value is required at the top of this function so that
                //we don't incur a race condition with the ResetTimeZone method below.
                TimeZone? tz = currentTimeZone;
                if (tz == null)
                {
                    lock (InternalSyncObject)
                    {
                        if (currentTimeZone == null)
                        {
                            currentTimeZone = new CurrentSystemTimeZone();
                        }
                        tz = currentTimeZone;
                    }
                }
                return (tz);
            }
        }

        //This method is called by CultureInfo.ClearCachedData in response to control panel
        //change events.  It must be synchronized because otherwise there is a race condition 
        //with the CurrentTimeZone property above.
        internal static void ResetTimeZone()
        {
            if (currentTimeZone != null)
            {
                lock (InternalSyncObject)
                {
                    currentTimeZone = null;
                }
            }
        }

        public abstract string StandardName
        {
            get;
        }

        public abstract string DaylightName
        {
            get;
        }

        public abstract TimeSpan GetUtcOffset(DateTime time);

        //
        // Converts the specified datatime to the Universal time base on the current timezone 
        //
        public virtual DateTime ToUniversalTime(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc)
            {
                return time;
            }
            long tickCount = time.Ticks - GetUtcOffset(time).Ticks;
            if (tickCount > DateTime.MaxTicks)
            {
                return new DateTime(DateTime.MaxTicks, DateTimeKind.Utc);
            }
            if (tickCount < DateTime.MinTicks)
            {
                return new DateTime(DateTime.MinTicks, DateTimeKind.Utc);
            }
            return new DateTime(tickCount, DateTimeKind.Utc);
        }

        //
        // Convert the specified datetime value from UTC to the local time based on the time zone.
        //
        public virtual DateTime ToLocalTime(DateTime time)
        {
            if (time.Kind == DateTimeKind.Local)
            {
                return time;
            }
            bool isAmbiguousLocalDst = false;
            long offset = ((CurrentSystemTimeZone)(TimeZone.CurrentTimeZone)).GetUtcOffsetFromUniversalTime(time, ref isAmbiguousLocalDst);
            return new DateTime(time.Ticks + offset, DateTimeKind.Local, isAmbiguousLocalDst);
        }

        // Return an array of DaylightTime which reflects the daylight saving periods in a particular year.
        // We currently only support having one DaylightSavingTime per year.
        // If daylight saving time is not used in this timezone, null will be returned.
        public abstract DaylightTime GetDaylightChanges(int year);

        public virtual bool IsDaylightSavingTime(DateTime time)
        {
            return (IsDaylightSavingTime(time, GetDaylightChanges(time.Year)));
        }

        // Check if the specified time is in a daylight saving time.  Allows the user to
        // specify the array of Daylight Saving Times.
        public static bool IsDaylightSavingTime(DateTime time, DaylightTime daylightTimes)
        {
            return CalculateUtcOffset(time, daylightTimes) != TimeSpan.Zero;
        }

        //
        // NOTENOTE: Implementation detail
        // In the transition from standard time to daylight saving time, 
        // if we convert local time to Universal time, we can have the
        // following (take PST as an example):
        //      Local               Universal       UTC Offset
        //      -----               ---------       ----------
        //      01:00AM             09:00           -8:00
        //      02:00 (=> 03:00)    10:00           -8:00   [This time doesn't actually exist, but it can be created from DateTime]
        //      03:00               10:00           -7:00
        //      04:00               11:00           -7:00
        //      05:00               12:00           -7:00
        //      
        //      So from 02:00 - 02:59:59, we should return the standard offset, instead of the daylight saving offset.
        //
        // In the transition from daylight saving time to standard time,
        // if we convert local time to Universal time, we can have the
        // following (take PST as an example):
        //      Local               Universal       UTC Offset
        //      -----               ---------       ----------
        //      01:00AM             08:00           -7:00
        //      02:00 (=> 01:00)    09:00           -8:00   
        //      02:00               10:00           -8:00
        //      03:00               11:00           -8:00
        //      04:00               12:00           -8:00
        //      
        //      So in this case, the 02:00 does exist after the first 2:00 rolls back to 01:00. We don't need to special case this.
        //      But note that there are two 01:00 in the local time.

        //
        // And imagine if the daylight saving offset is negative (although this does not exist in real life)
        // In the transition from standard time to daylight saving time, 
        // if we convert local time to Universal time, we can have the
        // following (take PST as an example, but the daylight saving offset is -01:00):
        //      Local               Universal       UTC Offset
        //      -----               ---------       ----------
        //      01:00AM             09:00           -8:00
        //      02:00 (=> 01:00)    10:00           -9:00
        //      02:00               11:00           -9:00
        //      03:00               12:00           -9:00
        //      04:00               13:00           -9:00
        //      05:00               14:00           -9:00
        //      
        //      So in this case, the 02:00 does exist after the first 2:00 rolls back to 01:00. We don't need to special case this.
        //
        // In the transition from daylight saving time to standard time,
        // if we convert local time to Universal time, we can have the
        // following (take PST as an example, daylight saving offset is -01:00):
        //
        //      Local               Universal       UTC Offset
        //      -----               ---------       ----------
        //      01:00AM             10:00           -9:00
        //      02:00 (=> 03:00)    11:00           -9:00
        //      03:00               11:00           -8:00
        //      04:00               12:00           -8:00
        //      05:00               13:00           -8:00
        //      06:00               14:00           -8:00
        //      
        //      So from 02:00 - 02:59:59, we should return the daylight saving offset, instead of the standard offset.
        //
        internal static TimeSpan CalculateUtcOffset(DateTime time, DaylightTime daylightTimes)
        {
            if (daylightTimes == null)
            {
                return TimeSpan.Zero;
            }
            DateTimeKind kind = time.Kind;
            if (kind == DateTimeKind.Utc)
            {
                return TimeSpan.Zero;
            }

            DateTime startTime;
            DateTime endTime;

            // startTime and endTime represent the period from either the start of DST to the end and includes the 
            // potentially overlapped times
            startTime = daylightTimes.Start + daylightTimes.Delta;
            endTime = daylightTimes.End;

            // For normal time zones, the ambiguous hour is the last hour of daylight saving when you wind the 
            // clock back. It is theoretically possible to have a positive delta, (which would really be daylight
            // reduction time), where you would have to wind the clock back in the begnning.
            DateTime ambiguousStart;
            DateTime ambiguousEnd;
            if (daylightTimes.Delta.Ticks > 0)
            {
                ambiguousStart = endTime - daylightTimes.Delta;
                ambiguousEnd = endTime;
            }
            else
            {
                ambiguousStart = startTime;
                ambiguousEnd = startTime - daylightTimes.Delta;
            }

            bool isDst = false;
            if (startTime > endTime)
            {
                // In southern hemisphere, the daylight saving time starts later in the year, and ends in the beginning of next year.
                // Note, the summer in the southern hemisphere begins late in the year.
                if (time >= startTime || time < endTime)
                {
                    isDst = true;
                }
            }
            else if (time >= startTime && time < endTime)
            {
                // In northern hemisphere, the daylight saving time starts in the middle of the year.
                isDst = true;
            }

            // If this date was previously converted from a UTC date and we were able to detect that the local
            // DateTime would be ambiguous, this data is stored in the DateTime to resolve this ambiguity. 
            if (isDst && time >= ambiguousStart && time < ambiguousEnd)
            {
                isDst = time.IsAmbiguousDaylightSavingTime();
            }

            if (isDst)
            {
                return daylightTimes.Delta;
            }
            return TimeSpan.Zero;
        }
    }
}
