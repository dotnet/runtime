// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Xml.Schema.DateAndTime.Helpers
{
    internal struct TimeInfo
    {
        public int Fraction { get; }
        public int Hour { get; }
        public int Microsecond
        {
            get
            {
                return Convert.ToInt32((Fraction % TimeSpan.TicksPerMillisecond) / TimeSpan.TicksPerMicrosecond);
            }
        }

        public int Millisecond
        {
            get
            {
                return Convert.ToInt32(Fraction / TimeSpan.TicksPerMillisecond);
            }
        }

        public int Minute { get; }
        public int Second { get; }

        public TimeInfo(int fraction, int hour, int minute, int second)
        {
            Fraction = fraction;
            Hour = hour;
            Minute = minute;
            Second = second;
        }
    }
}
