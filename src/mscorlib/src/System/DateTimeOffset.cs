// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System {
    
    using System;
    using System.Threading;
    using System.Globalization;
    using System.Runtime.InteropServices;
    using System.Runtime.CompilerServices;
    using System.Runtime.Serialization;
    using System.Security.Permissions;
    using System.Diagnostics.Contracts;

    // DateTimeOffset is a value type that consists of a DateTime and a time zone offset, 
    // ie. how far away the time is from GMT. The DateTime is stored whole, and the offset
    // is stored as an Int16 internally to save space, but presented as a TimeSpan. 
    //
    // The range is constrained so that both the represented clock time and the represented
    // UTC time fit within the boundaries of MaxValue. This gives it the same range as DateTime
    // for actual UTC times, and a slightly constrained range on one end when an offset is
    // present. 
    //
    // This class should be substitutable for date time in most cases; so most operations
    // effectively work on the clock time. However, the underlying UTC time is what counts
    // for the purposes of identity, sorting and subtracting two instances.
    //
    //
    // There are theoretically two date times stored, the UTC and the relative local representation
    // or the 'clock' time. It actually does not matter which is stored in m_dateTime, so it is desirable
    // for most methods to go through the helpers UtcDateTime and ClockDateTime both to abstract this
    // out and for internal readability.
    
    [StructLayout(LayoutKind.Auto)]
    [Serializable]
    public struct DateTimeOffset : IComparable, IFormattable,
                                   IComparable<DateTimeOffset>, IEquatable<DateTimeOffset>, ISerializable, IDeserializationCallback
    {
        // Constants
        internal const Int64 MaxOffset = TimeSpan.TicksPerHour * 14;
        internal const Int64 MinOffset = -MaxOffset;

        private const long UnixEpochTicks = TimeSpan.TicksPerDay * DateTime.DaysTo1970; // 621,355,968,000,000,000
        private const long UnixEpochSeconds = UnixEpochTicks / TimeSpan.TicksPerSecond; // 62,135,596,800
        private const long UnixEpochMilliseconds = UnixEpochTicks / TimeSpan.TicksPerMillisecond; // 62,135,596,800,000

        internal const long UnixMinSeconds = DateTime.MinTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;
        internal const long UnixMaxSeconds = DateTime.MaxTicks / TimeSpan.TicksPerSecond - UnixEpochSeconds;

        // Static Fields
        public static readonly DateTimeOffset MinValue = new DateTimeOffset(DateTime.MinTicks, TimeSpan.Zero);
        public static readonly DateTimeOffset MaxValue = new DateTimeOffset(DateTime.MaxTicks, TimeSpan.Zero);        
    
        // Instance Fields
        private DateTime m_dateTime;
        private Int16 m_offsetMinutes;
        
        // Constructors
                                            
        // Constructs a DateTimeOffset from a tick count and offset
        public DateTimeOffset(long ticks, TimeSpan offset) {
            m_offsetMinutes = ValidateOffset(offset);
            // Let the DateTime constructor do the range checks
            DateTime dateTime = new DateTime(ticks);
            m_dateTime = ValidateDate(dateTime, offset);
        }
        
        // Constructs a DateTimeOffset from a DateTime. For Local and Unspecified kinds,
        // extracts the local offset. For UTC, creates a UTC instance with a zero offset.
        public DateTimeOffset(DateTime dateTime) {
            TimeSpan offset;
            if (dateTime.Kind != DateTimeKind.Utc) {
                // Local and Unspecified are both treated as Local
                offset = TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime);
            }
            else {            
                offset = new TimeSpan(0);
            }
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(dateTime, offset);
        }

        // Constructs a DateTimeOffset from a DateTime. And an offset. Always makes the clock time
        // consistent with the DateTime. For Utc ensures the offset is zero. For local, ensures that
        // the offset corresponds to the local.
        public DateTimeOffset(DateTime dateTime, TimeSpan offset) {
            if (dateTime.Kind == DateTimeKind.Local) {
                if (offset != TimeZoneInfo.GetLocalUtcOffset(dateTime, TimeZoneInfoOptions.NoThrowOnInvalidTime)) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_OffsetLocalMismatch"), "offset");
                }
            }
            else if (dateTime.Kind == DateTimeKind.Utc) {
                if (offset != TimeSpan.Zero) {
                    throw new ArgumentException(Environment.GetResourceString("Argument_OffsetUtcMismatch"), "offset");
                }
            }
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(dateTime, offset);
        }
                                                   
        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second and offset.
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, TimeSpan offset) {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second), offset);
        }
                
        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second, millsecond and offset
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, TimeSpan offset) {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond), offset);
        }
        

        // Constructs a DateTimeOffset from a given year, month, day, hour,
        // minute, second, millsecond, Calendar and offset.
        public DateTimeOffset(int year, int month, int day, int hour, int minute, int second, int millisecond, Calendar calendar, TimeSpan offset) {
            m_offsetMinutes = ValidateOffset(offset);
            m_dateTime = ValidateDate(new DateTime(year, month, day, hour, minute, second, millisecond, calendar), offset);
        }
        
        // Returns a DateTimeOffset representing the current date and time. The
        // resolution of the returned value depends on the system timer. For
        // Windows NT 3.5 and later the timer resolution is approximately 10ms,
        // for Windows NT 3.1 it is approximately 16ms, and for Windows 95 and 98
        // it is approximately 55ms.
        //
        public static DateTimeOffset Now {
            get {
                return new DateTimeOffset(DateTime.Now);
            }
        }               

        public static DateTimeOffset UtcNow {
            get {
                return new DateTimeOffset(DateTime.UtcNow);
            }
        }

        public DateTime DateTime {
            get { 
                return ClockDateTime;
            }
        }

        public DateTime UtcDateTime {
            [Pure]
            get {
                Contract.Ensures(Contract.Result<DateTime>().Kind == DateTimeKind.Utc);
                return DateTime.SpecifyKind(m_dateTime, DateTimeKind.Utc);
            }
        }

        public DateTime LocalDateTime {
            [Pure]
            get {
                Contract.Ensures(Contract.Result<DateTime>().Kind == DateTimeKind.Local);
                return UtcDateTime.ToLocalTime();
            }
        }

        // Adjust to a given offset with the same UTC time.  Can throw ArgumentException
        //
        public DateTimeOffset ToOffset(TimeSpan offset) {
            return new DateTimeOffset((m_dateTime + offset).Ticks, offset);
        }
        
        
        // Instance Properties

        // The clock or visible time represented. This is just a wrapper around the internal date because this is
        // the chosen storage mechanism. Going through this helper is good for readability and maintainability.
        // This should be used for display but not identity.
        private DateTime ClockDateTime {
            get {
                return new DateTime((m_dateTime + Offset).Ticks, DateTimeKind.Unspecified);
            }
        }
        
        // Returns the date part of this DateTimeOffset. The resulting value
        // corresponds to this DateTimeOffset with the time-of-day part set to
        // zero (midnight).
        //
        public DateTime Date {
            get { 
                return ClockDateTime.Date;
            }
        }        
        
        // Returns the day-of-month part of this DateTimeOffset. The returned
        // value is an integer between 1 and 31.
        //
        public int Day {
            get {
                Contract.Ensures(Contract.Result<int>() >= 1);
                Contract.Ensures(Contract.Result<int>() <= 31);
                return ClockDateTime.Day;
            }
        }    
        
        // Returns the day-of-week part of this DateTimeOffset. The returned value
        // is an integer between 0 and 6, where 0 indicates Sunday, 1 indicates
        // Monday, 2 indicates Tuesday, 3 indicates Wednesday, 4 indicates
        // Thursday, 5 indicates Friday, and 6 indicates Saturday.
        //
        public DayOfWeek DayOfWeek {
            get {
                Contract.Ensures(Contract.Result<DayOfWeek>() >= DayOfWeek.Sunday);
                Contract.Ensures(Contract.Result<DayOfWeek>() <= DayOfWeek.Saturday);
                return ClockDateTime.DayOfWeek; 
            }
        }         
        
        // Returns the day-of-year part of this DateTimeOffset. The returned value
        // is an integer between 1 and 366.
        //
        public int DayOfYear {
            get {
                Contract.Ensures(Contract.Result<int>() >= 1);
                Contract.Ensures(Contract.Result<int>() <= 366);  // leap year
                return ClockDateTime.DayOfYear;
            }
        }        
        
        // Returns the hour part of this DateTimeOffset. The returned value is an
        // integer between 0 and 23.
        //
        public int Hour {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 24);
                return ClockDateTime.Hour;
            }
        }           
        
            
        // Returns the millisecond part of this DateTimeOffset. The returned value
        // is an integer between 0 and 999.
        //
        public int Millisecond {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 1000);
                return ClockDateTime.Millisecond; 
            }
        }
    
        // Returns the minute part of this DateTimeOffset. The returned value is
        // an integer between 0 and 59.
        //
        public int Minute {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 60);
                return ClockDateTime.Minute;
            }
        }
    
        // Returns the month part of this DateTimeOffset. The returned value is an
        // integer between 1 and 12.
        //
        public int Month {
            get {
                Contract.Ensures(Contract.Result<int>() >= 1);
                return ClockDateTime.Month;
            }
        }
        
        public TimeSpan Offset {
            get {
                return new TimeSpan(0, m_offsetMinutes, 0);
            }            
        }
    
        // Returns the second part of this DateTimeOffset. The returned value is
        // an integer between 0 and 59.
        //
        public int Second {
            get {
                Contract.Ensures(Contract.Result<int>() >= 0);
                Contract.Ensures(Contract.Result<int>() < 60);
                return ClockDateTime.Second;
            }
        }    

        // Returns the tick count for this DateTimeOffset. The returned value is
        // the number of 100-nanosecond intervals that have elapsed since 1/1/0001
        // 12:00am.
        //
        public long Ticks {
            get { 
                return ClockDateTime.Ticks; 
            }
        }

        public long UtcTicks {
            get { 
                return UtcDateTime.Ticks; 
            }
        }
        
        // Returns the time-of-day part of this DateTimeOffset. The returned value
        // is a TimeSpan that indicates the time elapsed since midnight.
        //
        public TimeSpan TimeOfDay {
            get { 
                return ClockDateTime.TimeOfDay;
            }
        }
                    
        // Returns the year part of this DateTimeOffset. The returned value is an
        // integer between 1 and 9999.
        //
        public int Year {
            get {
                Contract.Ensures(Contract.Result<int>() >= 1 && Contract.Result<int>() <= 9999);
                return ClockDateTime.Year;
            }
        }
            
        // Returns the DateTimeOffset resulting from adding the given
        // TimeSpan to this DateTimeOffset.
        //
        public DateTimeOffset Add(TimeSpan timeSpan) {
            return new DateTimeOffset(ClockDateTime.Add(timeSpan), Offset);
        }
    
        // Returns the DateTimeOffset resulting from adding a fractional number of
        // days to this DateTimeOffset. The result is computed by rounding the
        // fractional number of days given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddDays(double days) {
            return new DateTimeOffset(ClockDateTime.AddDays(days), Offset);
        }
    
        // Returns the DateTimeOffset resulting from adding a fractional number of
        // hours to this DateTimeOffset. The result is computed by rounding the
        // fractional number of hours given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddHours(double hours) {
            return new DateTimeOffset(ClockDateTime.AddHours(hours), Offset);
        }
    
        // Returns the DateTimeOffset resulting from the given number of
        // milliseconds to this DateTimeOffset. The result is computed by rounding
        // the number of milliseconds given by value to the nearest integer,
        // and adding that interval to this DateTimeOffset. The value
        // argument is permitted to be negative.
        //
        public DateTimeOffset AddMilliseconds(double milliseconds) {
            return new DateTimeOffset(ClockDateTime.AddMilliseconds(milliseconds), Offset);
        }
    
        // Returns the DateTimeOffset resulting from adding a fractional number of
        // minutes to this DateTimeOffset. The result is computed by rounding the
        // fractional number of minutes given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddMinutes(double minutes) {
            return new DateTimeOffset(ClockDateTime.AddMinutes(minutes), Offset);
        }
    
        public DateTimeOffset AddMonths(int months) {
            return new DateTimeOffset(ClockDateTime.AddMonths(months), Offset);
        }
        
        // Returns the DateTimeOffset resulting from adding a fractional number of
        // seconds to this DateTimeOffset. The result is computed by rounding the
        // fractional number of seconds given by value to the nearest
        // millisecond, and adding that interval to this DateTimeOffset. The
        // value argument is permitted to be negative.
        //
        public DateTimeOffset AddSeconds(double seconds) {
            return new DateTimeOffset(ClockDateTime.AddSeconds(seconds), Offset);
        }
    
        // Returns the DateTimeOffset resulting from adding the given number of
        // 100-nanosecond ticks to this DateTimeOffset. The value argument
        // is permitted to be negative.
        //
        public DateTimeOffset AddTicks(long ticks) {
            return new DateTimeOffset(ClockDateTime.AddTicks(ticks), Offset);
        }
    
        // Returns the DateTimeOffset resulting from adding the given number of
        // years to this DateTimeOffset. The result is computed by incrementing
        // (or decrementing) the year part of this DateTimeOffset by value
        // years. If the month and day of this DateTimeOffset is 2/29, and if the
        // resulting year is not a leap year, the month and day of the resulting
        // DateTimeOffset becomes 2/28. Otherwise, the month, day, and time-of-day
        // parts of the result are the same as those of this DateTimeOffset.
        //
        public DateTimeOffset AddYears(int years) {
            return new DateTimeOffset(ClockDateTime.AddYears(years), Offset);
        }
        
        // Compares two DateTimeOffset values, returning an integer that indicates
        // their relationship.
        //
        public static int Compare(DateTimeOffset first, DateTimeOffset second) {            
            return DateTime.Compare(first.UtcDateTime, second.UtcDateTime);
        }
    
        // Compares this DateTimeOffset to a given object. This method provides an
        // implementation of the IComparable interface. The object
        // argument must be another DateTimeOffset, or otherwise an exception
        // occurs.  Null is considered less than any instance.
        //
        int IComparable.CompareTo(Object obj) {
            if (obj == null) return 1;
            if (!(obj is DateTimeOffset)) {
                throw new ArgumentException(Environment.GetResourceString("Arg_MustBeDateTimeOffset"));
            }
    
            DateTime objUtc = ((DateTimeOffset)obj).UtcDateTime;
            DateTime utc = UtcDateTime;
            if (utc > objUtc) return 1;
            if (utc < objUtc) return -1;
            return 0;
        }

        public int CompareTo(DateTimeOffset other) {
            DateTime otherUtc = other.UtcDateTime;
            DateTime utc = UtcDateTime;
            if (utc > otherUtc) return 1;
            if (utc < otherUtc) return -1;
            return 0;
        }
  

        // Checks if this DateTimeOffset is equal to a given object. Returns
        // true if the given object is a boxed DateTimeOffset and its value
        // is equal to the value of this DateTimeOffset. Returns false
        // otherwise.
        //
        public override bool Equals(Object obj) {
            if (obj is DateTimeOffset) {
                return UtcDateTime.Equals(((DateTimeOffset)obj).UtcDateTime);
            }
            return false;
        }
    
        public bool Equals(DateTimeOffset other) {
            return UtcDateTime.Equals(other.UtcDateTime);
        }

        public bool EqualsExact(DateTimeOffset other) {
            //
            // returns true when the ClockDateTime, Kind, and Offset match
            //
            // currently the Kind should always be Unspecified, but there is always the possibility that a future version
            // of DateTimeOffset overloads the Kind field
            //
            return (ClockDateTime == other.ClockDateTime && Offset == other.Offset && ClockDateTime.Kind == other.ClockDateTime.Kind);
        }

        // Compares two DateTimeOffset values for equality. Returns true if
        // the two DateTimeOffset values are equal, or false if they are
        // not equal.
        //
        public static bool Equals(DateTimeOffset first, DateTimeOffset second) {
            return DateTime.Equals(first.UtcDateTime, second.UtcDateTime);
        }
                    
        // Creates a DateTimeOffset from a Windows filetime. A Windows filetime is
        // a long representing the date and time as the number of
        // 100-nanosecond intervals that have elapsed since 1/1/1601 12:00am.
        //
        public static DateTimeOffset FromFileTime(long fileTime) {
            return new DateTimeOffset(DateTime.FromFileTime(fileTime));
        }

        public static DateTimeOffset FromUnixTimeSeconds(long seconds) {
            if (seconds < UnixMinSeconds || seconds > UnixMaxSeconds) {
                throw new ArgumentOutOfRangeException("seconds",
                    string.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), UnixMinSeconds, UnixMaxSeconds));
            }

            long ticks = seconds * TimeSpan.TicksPerSecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }

        public static DateTimeOffset FromUnixTimeMilliseconds(long milliseconds) {
            const long MinMilliseconds = DateTime.MinTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;
            const long MaxMilliseconds = DateTime.MaxTicks / TimeSpan.TicksPerMillisecond - UnixEpochMilliseconds;

            if (milliseconds < MinMilliseconds || milliseconds > MaxMilliseconds) {
                throw new ArgumentOutOfRangeException("milliseconds",
                    string.Format(Environment.GetResourceString("ArgumentOutOfRange_Range"), MinMilliseconds, MaxMilliseconds));
            }

            long ticks = milliseconds * TimeSpan.TicksPerMillisecond + UnixEpochTicks;
            return new DateTimeOffset(ticks, TimeSpan.Zero);
        }
        
        // ----- SECTION: private serialization instance methods  ----------------*

        void IDeserializationCallback.OnDeserialization(Object sender) {
            try {
                m_offsetMinutes = ValidateOffset(Offset);
                m_dateTime      = ValidateDate(ClockDateTime, Offset);
            }
            catch (ArgumentException e) {
                throw new SerializationException(Environment.GetResourceString("Serialization_InvalidData"), e);
            }
        }


        [System.Security.SecurityCritical]  // auto-generated_required
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) {
            if (info == null) {
                throw new ArgumentNullException("info");
            }

            Contract.EndContractBlock();

            info.AddValue("DateTime", m_dateTime);
            info.AddValue("OffsetMinutes", m_offsetMinutes);
        } 


        DateTimeOffset(SerializationInfo info, StreamingContext context) {
            if (info == null) {
                throw new ArgumentNullException("info");
            }

            m_dateTime      = (DateTime)info.GetValue("DateTime", typeof(DateTime));
            m_offsetMinutes = (Int16)info.GetValue("OffsetMinutes", typeof(Int16));
        }  

        // Returns the hash code for this DateTimeOffset.
        //
        public override int GetHashCode() {
            return UtcDateTime.GetHashCode();
        }    
        
        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        // 
        public static DateTimeOffset Parse(String input) {
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.Parse(input, 
                                                      DateTimeFormatInfo.CurrentInfo, 
                                                      DateTimeStyles.None, 
                                                      out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }
    
        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        // 
        public static DateTimeOffset Parse(String input, IFormatProvider formatProvider) {
            return Parse(input, formatProvider, DateTimeStyles.None);
        }
        
        public static DateTimeOffset Parse(String input, IFormatProvider formatProvider, DateTimeStyles styles) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.Parse(input, 
                                                      DateTimeFormatInfo.GetInstance(formatProvider), 
                                                      styles, 
                                                      out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }
        
        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        // 
        public static DateTimeOffset ParseExact(String input, String format, IFormatProvider formatProvider) {
            return ParseExact(input, format, formatProvider, DateTimeStyles.None);
        }

        // Constructs a DateTimeOffset from a string. The string must specify a
        // date and optionally a time in a culture-specific or universal format.
        // Leading and trailing whitespace characters are allowed.
        // 
        public static DateTimeOffset ParseExact(String input, String format, IFormatProvider formatProvider, DateTimeStyles styles) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.ParseExact(input, 
                                                           format, 
                                                           DateTimeFormatInfo.GetInstance(formatProvider), 
                                                           styles, 
                                                           out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }    

        public static DateTimeOffset ParseExact(String input, String[] formats, IFormatProvider formatProvider, DateTimeStyles styles) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult = DateTimeParse.ParseExactMultiple(input, 
                                                                   formats, 
                                                                   DateTimeFormatInfo.GetInstance(formatProvider), 
                                                                   styles, 
                                                                   out offset);
            return new DateTimeOffset(dateResult.Ticks, offset);
        }

        public TimeSpan Subtract(DateTimeOffset value) {
            return UtcDateTime.Subtract(value.UtcDateTime);
        }
    
        public DateTimeOffset Subtract(TimeSpan value) {
            return new DateTimeOffset(ClockDateTime.Subtract(value), Offset);
        }
       
       
        public long ToFileTime() {
            return UtcDateTime.ToFileTime();
        }

        public long ToUnixTimeSeconds() {
            // Truncate sub-second precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times.
            //
            // For example, consider the DateTimeOffset 12/31/1969 12:59:59.001 +0
            //   ticks            = 621355967990010000
            //   ticksFromEpoch   = ticks - UnixEpochTicks                   = -9990000
            //   secondsFromEpoch = ticksFromEpoch / TimeSpan.TicksPerSecond = 0
            //
            // Notice that secondsFromEpoch is rounded *up* by the truncation induced by integer division,
            // whereas we actually always want to round *down* when converting to Unix time. This happens
            // automatically for positive Unix time values. Now the example becomes:
            //   seconds          = ticks / TimeSpan.TicksPerSecond = 62135596799
            //   secondsFromEpoch = seconds - UnixEpochSeconds      = -1
            //
            // In other words, we want to consistently round toward the time 1/1/0001 00:00:00,
            // rather than toward the Unix Epoch (1/1/1970 00:00:00).
            long seconds = UtcDateTime.Ticks / TimeSpan.TicksPerSecond;
            return seconds - UnixEpochSeconds;
        }

        public long ToUnixTimeMilliseconds() {
            // Truncate sub-millisecond precision before offsetting by the Unix Epoch to avoid
            // the last digit being off by one for dates that result in negative Unix times
            long milliseconds = UtcDateTime.Ticks / TimeSpan.TicksPerMillisecond;
            return milliseconds - UnixEpochMilliseconds;
        }
    
        public DateTimeOffset ToLocalTime() {
            return ToLocalTime(false);
        }

        internal DateTimeOffset ToLocalTime(bool throwOnOverflow)
        {
            return new DateTimeOffset(UtcDateTime.ToLocalTime(throwOnOverflow));
        }

        public override String ToString() {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, null, DateTimeFormatInfo.CurrentInfo, Offset);
        }

        public String ToString(String format) {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, format, DateTimeFormatInfo.CurrentInfo, Offset);
        }

        public String ToString(IFormatProvider formatProvider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, null, DateTimeFormatInfo.GetInstance(formatProvider), Offset);
        }
         
        public String ToString(String format, IFormatProvider formatProvider) {
            Contract.Ensures(Contract.Result<String>() != null);
            return DateTimeFormat.Format(ClockDateTime, format, DateTimeFormatInfo.GetInstance(formatProvider), Offset);
        }
    
        public DateTimeOffset ToUniversalTime() {
            return new DateTimeOffset(UtcDateTime);
        }
        
        public static Boolean TryParse(String input, out DateTimeOffset result) {                   
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParse(input, 
                                                    DateTimeFormatInfo.CurrentInfo, 
                                                    DateTimeStyles.None, 
                                                    out dateResult, 
                                                    out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }
        
        public static Boolean TryParse(String input, IFormatProvider formatProvider, DateTimeStyles styles, out DateTimeOffset result) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParse(input, 
                                                    DateTimeFormatInfo.GetInstance(formatProvider), 
                                                    styles, 
                                                    out dateResult, 
                                                    out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }    
            
        public static Boolean TryParseExact(String input, String format, IFormatProvider formatProvider, DateTimeStyles styles,
                                            out DateTimeOffset result) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParseExact(input, 
                                                         format,
                                                         DateTimeFormatInfo.GetInstance(formatProvider), 
                                                         styles, 
                                                         out dateResult, 
                                                         out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }    

        public static Boolean TryParseExact(String input, String[] formats, IFormatProvider formatProvider, DateTimeStyles styles,
                                            out DateTimeOffset result) {
            styles = ValidateStyles(styles, "styles");
            TimeSpan offset;
            DateTime dateResult;
            Boolean parsed = DateTimeParse.TryParseExactMultiple(input, 
                                                                 formats,
                                                                 DateTimeFormatInfo.GetInstance(formatProvider), 
                                                                 styles, 
                                                                 out dateResult, 
                                                                 out offset);
            result = new DateTimeOffset(dateResult.Ticks, offset);
            return parsed;
        }
        
        // Ensures the TimeSpan is valid to go in a DateTimeOffset.
        private static Int16 ValidateOffset(TimeSpan offset) {
            Int64 ticks = offset.Ticks;
            if (ticks % TimeSpan.TicksPerMinute != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetPrecision"), "offset");
            }
            if (ticks < MinOffset || ticks > MaxOffset) {
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("Argument_OffsetOutOfRange"));
            }
            return (Int16)(offset.Ticks / TimeSpan.TicksPerMinute);
        }

        // Ensures that the time and offset are in range.
        private static DateTime ValidateDate(DateTime dateTime, TimeSpan offset) {
            // The key validation is that both the UTC and clock times fit. The clock time is validated
            // by the DateTime constructor.
            Contract.Assert(offset.Ticks >= MinOffset && offset.Ticks <= MaxOffset, "Offset not validated.");
            // This operation cannot overflow because offset should have already been validated to be within
            // 14 hours and the DateTime instance is more than that distance from the boundaries of Int64.
            Int64 utcTicks = dateTime.Ticks - offset.Ticks;
            if (utcTicks < DateTime.MinTicks || utcTicks > DateTime.MaxTicks) {                
                throw new ArgumentOutOfRangeException("offset", Environment.GetResourceString("Argument_UTCOutOfRange"));                
            }
            // make sure the Kind is set to Unspecified
            //
            return new DateTime(utcTicks, DateTimeKind.Unspecified);
        }
        
        private static DateTimeStyles ValidateStyles(DateTimeStyles style, String parameterName) {
            if ((style & DateTimeFormatInfo.InvalidDateTimeStyles) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_InvalidDateTimeStyles"), parameterName);
            }
            if (((style & (DateTimeStyles.AssumeLocal)) != 0) && ((style & (DateTimeStyles.AssumeUniversal)) != 0)) {
                throw new ArgumentException(Environment.GetResourceString("Argument_ConflictingDateTimeStyles"), parameterName);
            }
            if ((style & DateTimeStyles.NoCurrentDateDefault) != 0) {
                throw new ArgumentException(Environment.GetResourceString("Argument_DateTimeOffsetInvalidDateTimeStyles"), parameterName);
            }

            Contract.EndContractBlock();
            // RoundtripKind does not make sense for DateTimeOffset; ignore this flag for backward compatibility with DateTime
            style &= ~DateTimeStyles.RoundtripKind; 
        
            // AssumeLocal is also ignored as that is what we do by default with DateTimeOffset.Parse             
            style &= ~DateTimeStyles.AssumeLocal;

            return style;
        }                
        
        // Operators
  
        public static implicit operator DateTimeOffset (DateTime dateTime) {
            return new DateTimeOffset(dateTime);
        }

        public static DateTimeOffset operator +(DateTimeOffset dateTimeOffset, TimeSpan timeSpan) {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime + timeSpan, dateTimeOffset.Offset);
        }
    

        public static DateTimeOffset operator -(DateTimeOffset dateTimeOffset, TimeSpan timeSpan) {
            return new DateTimeOffset(dateTimeOffset.ClockDateTime - timeSpan, dateTimeOffset.Offset);
        }
   
        public static TimeSpan operator -(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime - right.UtcDateTime;
        }
        
        public static bool operator ==(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime == right.UtcDateTime;
        }

        public static bool operator !=(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime != right.UtcDateTime;
        }
        
        public static bool operator <(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime < right.UtcDateTime;
        }

        public static bool operator <=(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime <= right.UtcDateTime;
        }

        public static bool operator >(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime > right.UtcDateTime;
        }

        public static bool operator >=(DateTimeOffset left, DateTimeOffset right) {
            return left.UtcDateTime >= right.UtcDateTime;
        }

    }
}
