// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;

namespace System.Globalization
{
    // This class represents a starting/ending time for a period of daylight saving time.


    internal class DaylightTime
    {
        internal DateTime m_start;
        internal DateTime m_end;
        internal TimeSpan m_delta;

        private DaylightTime()
        {
        }

        public DaylightTime(DateTime start, DateTime end, TimeSpan delta)
        {
            m_start = start;
            m_end = end;
            m_delta = delta;
        }

        // The start date of a daylight saving period.
        public DateTime Start
        {
            get
            {
                return m_start;
            }
        }

        // The end date of a daylight saving period.
        public DateTime End
        {
            get
            {
                return m_end;
            }
        }

        // Delta to stardard offset in ticks.
        public TimeSpan Delta
        {
            get
            {
                return m_delta;
            }
        }
    }
}
