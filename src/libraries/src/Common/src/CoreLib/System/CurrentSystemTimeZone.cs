// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
**
** Purpose: 
** This class represents the current system timezone.  It is
** the only meaningful implementation of the TimeZone class 
** available in this version.
**
** The only TimeZone that we support in version 1 is the 
** CurrentTimeZone as determined by the system timezone.
**
**
============================================================*/

#nullable enable
using System.Collections;
using System.Globalization;

namespace System
{
    [Obsolete("System.CurrentSystemTimeZone has been deprecated.  Please investigate the use of System.TimeZoneInfo.Local instead.")]
    internal class CurrentSystemTimeZone : TimeZone
    {
        // Standard offset in ticks to the Universal time if
        // no daylight saving is in used.
        // E.g. the offset for PST (Pacific Standard time) should be -8 * 60 * 60 * 1000 * 10000.
        // (1 millisecond = 10000 ticks)
        private long m_ticksOffset;
        private string m_standardName;
        private string m_daylightName;

        internal CurrentSystemTimeZone()
        {
            TimeZoneInfo local = TimeZoneInfo.Local;

            m_ticksOffset = local.BaseUtcOffset.Ticks;
            m_standardName = local.StandardName;
            m_daylightName = local.DaylightName;
        }

        public override string StandardName
        {
            get
            {
                return m_standardName;
            }
        }

        public override string DaylightName
        {
            get
            {
                return m_daylightName;
            }
        }

        internal long GetUtcOffsetFromUniversalTime(DateTime time, ref bool isAmbiguousLocalDst)
        {
            // Get the daylight changes for the year of the specified time.
            TimeSpan offset = new TimeSpan(m_ticksOffset);
            DaylightTime daylightTime = GetDaylightChanges(time.Year);
            isAmbiguousLocalDst = false;

            if (daylightTime == null || daylightTime.Delta.Ticks == 0)
            {
                return offset.Ticks;
            }

            // The start and end times represent the range of universal times that are in DST for that year.                
            // Within that there is an ambiguous hour, usually right at the end, but at the beginning in
            // the unusual case of a negative daylight savings delta.
            DateTime startTime = daylightTime.Start - offset;
            DateTime endTime = daylightTime.End - offset - daylightTime.Delta;
            DateTime ambiguousStart;
            DateTime ambiguousEnd;

            if (daylightTime.Delta.Ticks > 0)
            {
                ambiguousStart = endTime - daylightTime.Delta;
                ambiguousEnd = endTime;
            }
            else
            {
                ambiguousStart = startTime;
                ambiguousEnd = startTime - daylightTime.Delta;
            }

            bool isDst = false;
            if (startTime > endTime)
            {
                // In southern hemisphere, the daylight saving time starts later in the year, and ends in the beginning of next year.
                // Note, the summer in the southern hemisphere begins late in the year.
                isDst = (time < endTime || time >= startTime);
            }
            else
            {
                // In northern hemisphere, the daylight saving time starts in the middle of the year.
                isDst = (time >= startTime && time < endTime);
            }

            if (isDst)
            {
                offset += daylightTime.Delta;

                // See if the resulting local time becomes ambiguous. This must be captured here or the
                // DateTime will not be able to round-trip back to UTC accurately.
                if (time >= ambiguousStart && time < ambiguousEnd)
                {
                    isAmbiguousLocalDst = true;
                }
            }
            return offset.Ticks;
        }

        public override DateTime ToLocalTime(DateTime time)
        {
            if (time.Kind == DateTimeKind.Local)
            {
                return time;
            }
            bool isAmbiguousLocalDst = false;
            long offset = GetUtcOffsetFromUniversalTime(time, ref isAmbiguousLocalDst);
            long tick = time.Ticks + offset;
            if (tick > DateTime.MaxTicks)
            {
                return new DateTime(DateTime.MaxTicks, DateTimeKind.Local);
            }
            if (tick < DateTime.MinTicks)
            {
                return new DateTime(DateTime.MinTicks, DateTimeKind.Local);
            }
            return new DateTime(tick, DateTimeKind.Local, isAmbiguousLocalDst);
        }

        public override DaylightTime GetDaylightChanges(int year)
        {
            if (year < 1 || year > 9999)
            {
                throw new ArgumentOutOfRangeException(nameof(year), SR.Format(SR.ArgumentOutOfRange_Range, 1, 9999));
            }

            return GetCachedDaylightChanges(year);
        }

        private static DaylightTime CreateDaylightChanges(int year)
        {
            DaylightTime? currentDaylightChanges = null;

            if (TimeZoneInfo.Local.SupportsDaylightSavingTime)
            {
                DateTime start;
                DateTime end;
                TimeSpan delta;

                foreach (var rule in TimeZoneInfo.Local.GetAdjustmentRules())
                {
                    if (rule.DateStart.Year <= year && rule.DateEnd.Year >= year && rule.DaylightDelta != TimeSpan.Zero)
                    {
                        start = TimeZoneInfo.TransitionTimeToDateTime(year, rule.DaylightTransitionStart);
                        end = TimeZoneInfo.TransitionTimeToDateTime(year, rule.DaylightTransitionEnd);
                        delta = rule.DaylightDelta;

                        currentDaylightChanges = new DaylightTime(start, end, delta);
                        break;
                    }
                }
            }

            if (currentDaylightChanges == null)
            {
                currentDaylightChanges = new DaylightTime(DateTime.MinValue, DateTime.MinValue, TimeSpan.Zero);
            }

            return currentDaylightChanges;
        }

        public override TimeSpan GetUtcOffset(DateTime time)
        {
            if (time.Kind == DateTimeKind.Utc)
            {
                return TimeSpan.Zero;
            }
            else
            {
                return new TimeSpan(TimeZone.CalculateUtcOffset(time, GetDaylightChanges(time.Year)).Ticks + m_ticksOffset);
            }
        }

        private DaylightTime GetCachedDaylightChanges(int year)
        {
            object objYear = (object)year;

            if (!m_CachedDaylightChanges.Contains(objYear))
            {
                DaylightTime currentDaylightChanges = CreateDaylightChanges(year);
                lock (m_CachedDaylightChanges)
                {
                    if (!m_CachedDaylightChanges.Contains(objYear))
                    {
                        m_CachedDaylightChanges.Add(objYear, currentDaylightChanges);
                    }
                }
            }

            return (DaylightTime)m_CachedDaylightChanges[objYear]!;
        }

        // The per-year information is cached in this instance value. As a result it can
        // be cleaned up by CultureInfo.ClearCachedData, which will clear the instance of this object
        private readonly Hashtable m_CachedDaylightChanges = new Hashtable();

    } // class CurrentSystemTimeZone
}
