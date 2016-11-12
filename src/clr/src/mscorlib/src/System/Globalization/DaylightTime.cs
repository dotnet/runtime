// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Globalization
{
    // This class represents a starting/ending time for a period of daylight saving time.
    [Serializable]
    [ComVisible(true)]
    public class DaylightTime
    {
        internal DateTime m_start;
        internal DateTime m_end;
        internal TimeSpan m_delta;

        private DaylightTime() {
        }

        public DaylightTime(DateTime start, DateTime end, TimeSpan delta) {
            m_start = start;
            m_end = end;
            m_delta = delta;
        }    

        // The start date of a daylight saving period.
        public DateTime Start {
            get {
                return m_start;
            }
        }

        // The end date of a daylight saving period.
        public DateTime End {
            get {
                return m_end;
            }
        }

        // Delta to stardard offset in ticks.
        public TimeSpan Delta {
            get {
                return m_delta;
            }
        }
    
    }

    // Value type version of DaylightTime
    internal struct DaylightTimeStruct
    {
        public DaylightTimeStruct(DateTime start, DateTime end, TimeSpan delta)
        {
            Start = start;
            End = end;
            Delta = delta;
        }

        public DateTime Start { get; }
        public DateTime End { get; }
        public TimeSpan Delta { get; }
    }
}
