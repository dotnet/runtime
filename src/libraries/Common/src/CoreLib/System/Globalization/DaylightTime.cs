// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Globalization
{
    // This class represents a starting/ending time for a period of daylight saving time.
    public class DaylightTime
    {
        private readonly DateTime _start;
        private readonly DateTime _end;
        private readonly TimeSpan _delta;

        private DaylightTime()
        {
        }

        public DaylightTime(DateTime start, DateTime end, TimeSpan delta)
        {
            _start = start;
            _end = end;
            _delta = delta;
        }

        // The start date of a daylight saving period.
        public DateTime Start => _start;

        // The end date of a daylight saving period.
        public DateTime End => _end;

        // Delta to stardard offset in ticks.
        public TimeSpan Delta => _delta;
    }

    // Value type version of DaylightTime
    internal readonly struct DaylightTimeStruct
    {
        public DaylightTimeStruct(DateTime start, DateTime end, TimeSpan delta)
        {
            Start = start;
            End = end;
            Delta = delta;
        }

        public readonly DateTime Start;
        public readonly DateTime End;
        public readonly TimeSpan Delta;
    }
}
