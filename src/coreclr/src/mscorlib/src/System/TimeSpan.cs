// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;
using System;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Diagnostics.Contracts;
using System.Globalization;

namespace System
{
    // TimeSpan represents a duration of time.  A TimeSpan can be negative
    // or positive.
    //
    // TimeSpan is internally represented as a number of milliseconds.  While
    // this maps well into units of time such as hours and days, any
    // periods longer than that aren't representable in a nice fashion.
    // For instance, a month can be between 28 and 31 days, while a year
    // can contain 365 or 364 days.  A decade can have between 1 and 3 leapyears,
    // depending on when you map the TimeSpan into the calendar.  This is why
    // we do not provide Years() or Months().
    //
    // Note: System.TimeSpan needs to interop with the WinRT structure
    // type Windows::Foundation:TimeSpan. These types are currently binary-compatible in
    // memory so no custom marshalling is required. If at any point the implementation
    // details of this type should change, or new fields added, we need to remember to add
    // an appropriate custom ILMarshaler to keep WInRT interop scenarios enabled.
    //
    [Serializable]
    public struct TimeSpan : IComparable
        , IComparable<TimeSpan>, IEquatable<TimeSpan>, IFormattable
    {
        public const long TicksPerMillisecond = 10000;
        private const double MillisecondsPerTick = 1.0 / TicksPerMillisecond;

        public const long TicksPerSecond = TicksPerMillisecond * 1000;   // 10,000,000
        private const double SecondsPerTick = 1.0 / TicksPerSecond;         // 0.0001

        public const long TicksPerMinute = TicksPerSecond * 60;         // 600,000,000
        private const double MinutesPerTick = 1.0 / TicksPerMinute; // 1.6666666666667e-9

        public const long TicksPerHour = TicksPerMinute * 60;        // 36,000,000,000
        private const double HoursPerTick = 1.0 / TicksPerHour; // 2.77777777777777778e-11

        public const long TicksPerDay = TicksPerHour * 24;          // 864,000,000,000
        private const double DaysPerTick = 1.0 / TicksPerDay; // 1.1574074074074074074e-12

        private const int MillisPerSecond = 1000;
        private const int MillisPerMinute = MillisPerSecond * 60; //     60,000
        private const int MillisPerHour = MillisPerMinute * 60;   //  3,600,000
        private const int MillisPerDay = MillisPerHour * 24;      // 86,400,000

        internal const long MaxSeconds = Int64.MaxValue / TicksPerSecond;
        internal const long MinSeconds = Int64.MinValue / TicksPerSecond;

        internal const long MaxMilliSeconds = Int64.MaxValue / TicksPerMillisecond;
        internal const long MinMilliSeconds = Int64.MinValue / TicksPerMillisecond;

        internal const long TicksPerTenthSecond = TicksPerMillisecond * 100;

        public static readonly TimeSpan Zero = new TimeSpan(0);

        public static readonly TimeSpan MaxValue = new TimeSpan(Int64.MaxValue);
        public static readonly TimeSpan MinValue = new TimeSpan(Int64.MinValue);

        // internal so that DateTime doesn't have to call an extra get
        // method for some arithmetic operations.
        internal long _ticks; // Do not rename (binary serialization)

        //public TimeSpan() {
        //    _ticks = 0;
        //}

        public TimeSpan(long ticks)
        {
            this._ticks = ticks;
        }

        public TimeSpan(int hours, int minutes, int seconds)
        {
            _ticks = TimeToTicks(hours, minutes, seconds);
        }

        public TimeSpan(int days, int hours, int minutes, int seconds)
            : this(days, hours, minutes, seconds, 0)
        {
        }

        public TimeSpan(int days, int hours, int minutes, int seconds, int milliseconds)
        {
            Int64 totalMilliSeconds = ((Int64)days * 3600 * 24 + (Int64)hours * 3600 + (Int64)minutes * 60 + seconds) * 1000 + milliseconds;
            if (totalMilliSeconds > MaxMilliSeconds || totalMilliSeconds < MinMilliSeconds)
                throw new ArgumentOutOfRangeException(null, SR.Overflow_TimeSpanTooLong);
            _ticks = (long)totalMilliSeconds * TicksPerMillisecond;
        }

        public long Ticks
        {
            get { return _ticks; }
        }

        public int Days
        {
            get { return (int)(_ticks / TicksPerDay); }
        }

        public int Hours
        {
            get { return (int)((_ticks / TicksPerHour) % 24); }
        }

        public int Milliseconds
        {
            get { return (int)((_ticks / TicksPerMillisecond) % 1000); }
        }

        public int Minutes
        {
            get { return (int)((_ticks / TicksPerMinute) % 60); }
        }

        public int Seconds
        {
            get { return (int)((_ticks / TicksPerSecond) % 60); }
        }

        public double TotalDays
        {
            get { return ((double)_ticks) * DaysPerTick; }
        }

        public double TotalHours
        {
            get { return (double)_ticks * HoursPerTick; }
        }

        public double TotalMilliseconds
        {
            get
            {
                double temp = (double)_ticks * MillisecondsPerTick;
                if (temp > MaxMilliSeconds)
                    return (double)MaxMilliSeconds;

                if (temp < MinMilliSeconds)
                    return (double)MinMilliSeconds;

                return temp;
            }
        }

        public double TotalMinutes
        {
            get { return (double)_ticks * MinutesPerTick; }
        }

        public double TotalSeconds
        {
            get { return (double)_ticks * SecondsPerTick; }
        }

        public TimeSpan Add(TimeSpan ts)
        {
            long result = _ticks + ts._ticks;
            // Overflow if signs of operands was identical and result's
            // sign was opposite.
            // >> 63 gives the sign bit (either 64 1's or 64 0's).
            if ((_ticks >> 63 == ts._ticks >> 63) && (_ticks >> 63 != result >> 63))
                throw new OverflowException(SR.Overflow_TimeSpanTooLong);
            return new TimeSpan(result);
        }


        // Compares two TimeSpan values, returning an integer that indicates their
        // relationship.
        //
        public static int Compare(TimeSpan t1, TimeSpan t2)
        {
            if (t1._ticks > t2._ticks) return 1;
            if (t1._ticks < t2._ticks) return -1;
            return 0;
        }

        // Returns a value less than zero if this  object
        public int CompareTo(Object value)
        {
            if (value == null) return 1;
            if (!(value is TimeSpan))
                throw new ArgumentException(SR.Arg_MustBeTimeSpan);
            long t = ((TimeSpan)value)._ticks;
            if (_ticks > t) return 1;
            if (_ticks < t) return -1;
            return 0;
        }

        public int CompareTo(TimeSpan value)
        {
            long t = value._ticks;
            if (_ticks > t) return 1;
            if (_ticks < t) return -1;
            return 0;
        }

        public static TimeSpan FromDays(double value)
        {
            return Interval(value, MillisPerDay);
        }

        public TimeSpan Duration()
        {
            if (Ticks == TimeSpan.MinValue.Ticks)
                throw new OverflowException(SR.Overflow_Duration);
            Contract.EndContractBlock();
            return new TimeSpan(_ticks >= 0 ? _ticks : -_ticks);
        }

        public override bool Equals(Object value)
        {
            if (value is TimeSpan)
            {
                return _ticks == ((TimeSpan)value)._ticks;
            }
            return false;
        }

        public bool Equals(TimeSpan obj)
        {
            return _ticks == obj._ticks;
        }

        public static bool Equals(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks == t2._ticks;
        }

        public override int GetHashCode()
        {
            return (int)_ticks ^ (int)(_ticks >> 32);
        }

        public static TimeSpan FromHours(double value)
        {
            return Interval(value, MillisPerHour);
        }

        private static TimeSpan Interval(double value, int scale)
        {
            if (Double.IsNaN(value))
                throw new ArgumentException(SR.Arg_CannotBeNaN);
            Contract.EndContractBlock();
            double tmp = value * scale;
            double millis = tmp + (value >= 0 ? 0.5 : -0.5);
            if ((millis > Int64.MaxValue / TicksPerMillisecond) || (millis < Int64.MinValue / TicksPerMillisecond))
                throw new OverflowException(SR.Overflow_TimeSpanTooLong);
            return new TimeSpan((long)millis * TicksPerMillisecond);
        }

        public static TimeSpan FromMilliseconds(double value)
        {
            return Interval(value, 1);
        }

        public static TimeSpan FromMinutes(double value)
        {
            return Interval(value, MillisPerMinute);
        }

        public TimeSpan Negate()
        {
            if (Ticks == TimeSpan.MinValue.Ticks)
                throw new OverflowException(SR.Overflow_NegateTwosCompNum);
            Contract.EndContractBlock();
            return new TimeSpan(-_ticks);
        }

        public static TimeSpan FromSeconds(double value)
        {
            return Interval(value, MillisPerSecond);
        }

        public TimeSpan Subtract(TimeSpan ts)
        {
            long result = _ticks - ts._ticks;
            // Overflow if signs of operands was different and result's
            // sign was opposite from the first argument's sign.
            // >> 63 gives the sign bit (either 64 1's or 64 0's).
            if ((_ticks >> 63 != ts._ticks >> 63) && (_ticks >> 63 != result >> 63))
                throw new OverflowException(SR.Overflow_TimeSpanTooLong);
            return new TimeSpan(result);
        }

        public TimeSpan Multiply(double factor) => this * factor;

        public TimeSpan Divide(double divisor) => this / divisor;

        public double Divide(TimeSpan ts) => this / ts;

        public static TimeSpan FromTicks(long value)
        {
            return new TimeSpan(value);
        }

        internal static long TimeToTicks(int hour, int minute, int second)
        {
            // totalSeconds is bounded by 2^31 * 2^12 + 2^31 * 2^8 + 2^31,
            // which is less than 2^44, meaning we won't overflow totalSeconds.
            long totalSeconds = (long)hour * 3600 + (long)minute * 60 + (long)second;
            if (totalSeconds > MaxSeconds || totalSeconds < MinSeconds)
                throw new ArgumentOutOfRangeException(null, SR.Overflow_TimeSpanTooLong);
            return totalSeconds * TicksPerSecond;
        }

        // See System.Globalization.TimeSpanParse and System.Globalization.TimeSpanFormat 
        #region ParseAndFormat
        public static TimeSpan Parse(String s)
        {
            /* Constructs a TimeSpan from a string.  Leading and trailing white space characters are allowed. */
            return TimeSpanParse.Parse(s, null);
        }
        public static TimeSpan Parse(String input, IFormatProvider formatProvider)
        {
            return TimeSpanParse.Parse(input, formatProvider);
        }
        public static TimeSpan ParseExact(String input, String format, IFormatProvider formatProvider)
        {
            return TimeSpanParse.ParseExact(input, format, formatProvider, TimeSpanStyles.None);
        }
        public static TimeSpan ParseExact(String input, String[] formats, IFormatProvider formatProvider)
        {
            return TimeSpanParse.ParseExactMultiple(input, formats, formatProvider, TimeSpanStyles.None);
        }
        public static TimeSpan ParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanParse.ValidateStyles(styles, nameof(styles));
            return TimeSpanParse.ParseExact(input, format, formatProvider, styles);
        }
        public static TimeSpan ParseExact(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles)
        {
            TimeSpanParse.ValidateStyles(styles, nameof(styles));
            return TimeSpanParse.ParseExactMultiple(input, formats, formatProvider, styles);
        }
        public static Boolean TryParse(String s, out TimeSpan result)
        {
            return TimeSpanParse.TryParse(s, null, out result);
        }
        public static Boolean TryParse(String input, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParse(input, formatProvider, out result);
        }
        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParseExact(input, format, formatProvider, TimeSpanStyles.None, out result);
        }
        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, out TimeSpan result)
        {
            return TimeSpanParse.TryParseExactMultiple(input, formats, formatProvider, TimeSpanStyles.None, out result);
        }
        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanParse.ValidateStyles(styles, nameof(styles));
            return TimeSpanParse.TryParseExact(input, format, formatProvider, styles, out result);
        }
        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, TimeSpanStyles styles, out TimeSpan result)
        {
            TimeSpanParse.ValidateStyles(styles, nameof(styles));
            return TimeSpanParse.TryParseExactMultiple(input, formats, formatProvider, styles, out result);
        }
        public override String ToString()
        {
            return TimeSpanFormat.Format(this, null, null);
        }
        public String ToString(String format)
        {
            return TimeSpanFormat.Format(this, format, null);
        }
        public String ToString(String format, IFormatProvider formatProvider)
        {
            if (LegacyMode)
            {
                return TimeSpanFormat.Format(this, null, null);
            }
            else
            {
                return TimeSpanFormat.Format(this, format, formatProvider);
            }
        }
        #endregion

        public static TimeSpan operator -(TimeSpan t)
        {
            if (t._ticks == TimeSpan.MinValue._ticks)
                throw new OverflowException(SR.Overflow_NegateTwosCompNum);
            return new TimeSpan(-t._ticks);
        }

        public static TimeSpan operator -(TimeSpan t1, TimeSpan t2)
        {
            return t1.Subtract(t2);
        }

        public static TimeSpan operator +(TimeSpan t)
        {
            return t;
        }

        public static TimeSpan operator +(TimeSpan t1, TimeSpan t2)
        {
            return t1.Add(t2);
        }

        public static TimeSpan operator *(TimeSpan timeSpan, double factor)
        {
            if (double.IsNaN(factor))
            {
                throw new ArgumentException(SR.Arg_CannotBeNaN, nameof(factor));
            }

            // Rounding to the nearest tick is as close to the result we would have with unlimited
            // precision as possible, and so likely to have the least potential to surprise.
            double ticks = Math.Round(timeSpan.Ticks * factor);
            if (ticks > long.MaxValue | ticks < long.MinValue)
            {
                throw new OverflowException(SR.Overflow_TimeSpanTooLong);
            }

            return FromTicks((long)ticks);
        }

        public static TimeSpan operator *(double factor, TimeSpan timeSpan) => timeSpan * factor;

        public static TimeSpan operator /(TimeSpan timeSpan, double divisor)
        {
            if (double.IsNaN(divisor))
            {
                throw new ArgumentException(SR.Arg_CannotBeNaN, nameof(divisor));
            }

            double ticks = Math.Round(timeSpan.Ticks / divisor);
            if (ticks > long.MaxValue | ticks < long.MinValue || double.IsNaN(ticks))
            {
                throw new OverflowException(SR.Overflow_TimeSpanTooLong);
            }

            return FromTicks((long)ticks);
        }

        // Using floating-point arithmetic directly means that infinities can be returned, which is reasonable
        // if we consider TimeSpan.FromHours(1) / TimeSpan.Zero asks how many zero-second intervals there are in
        // an hour for which ∞ is the mathematic correct answer. Having TimeSpan.Zero / TimeSpan.Zero return NaN
        // is perhaps less useful, but no less useful than an exception.
        public static double operator /(TimeSpan t1, TimeSpan t2) => t1.Ticks / (double)t2.Ticks;

        public static bool operator ==(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks == t2._ticks;
        }

        public static bool operator !=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks != t2._ticks;
        }

        public static bool operator <(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks < t2._ticks;
        }

        public static bool operator <=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks <= t2._ticks;
        }

        public static bool operator >(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks > t2._ticks;
        }

        public static bool operator >=(TimeSpan t1, TimeSpan t2)
        {
            return t1._ticks >= t2._ticks;
        }


        //
        // In .NET Framework v1.0 - v3.5 System.TimeSpan did not implement IFormattable
        //    The composite formatter ignores format specifiers on types that do not implement
        //    IFormattable, so the following code would 'just work' by using TimeSpan.ToString()
        //    under the hood:
        //        String.Format("{0:_someRandomFormatString_}", myTimeSpan);      
        //    
        // In .NET Framework v4.0 System.TimeSpan implements IFormattable.  This causes the 
        //    composite formatter to call TimeSpan.ToString(string format, FormatProvider provider)
        //    and pass in "_someRandomFormatString_" for the format parameter.  When the format 
        //    parameter is invalid a FormatException is thrown.
        //
        // The 'NetFx40_TimeSpanLegacyFormatMode' per-AppDomain configuration option and the 'TimeSpan_LegacyFormatMode' 
        // process-wide configuration option allows applications to run with the v1.0 - v3.5 legacy behavior.  When
        // either switch is specified the format parameter is ignored and the default output is returned.
        //
        // There are three ways to use the process-wide configuration option:
        //
        // 1) Config file (MyApp.exe.config)
        //        <?xml version ="1.0"?>
        //        <configuration>
        //         <runtime>
        //          <TimeSpan_LegacyFormatMode enabled="true"/>
        //         </runtime>
        //        </configuration>
        // 2) Environment variable
        //        set COMPlus_TimeSpan_LegacyFormatMode=1
        // 3) RegistryKey
        //        [HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\.NETFramework]
        //        "TimeSpan_LegacyFormatMode"=dword:00000001
        //
        private static bool GetLegacyFormatMode()
        {
            return false;
        }

        private static volatile bool _legacyConfigChecked;
        private static volatile bool _legacyMode;

        private static bool LegacyMode
        {
            get
            {
                if (!_legacyConfigChecked)
                {
                    // no need to lock - idempotent
                    _legacyMode = GetLegacyFormatMode();
                    _legacyConfigChecked = true;
                }
                return _legacyMode;
            }
        }
    }
}
