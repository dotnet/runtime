// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;

namespace System.Management
{
    /// <summary>
    ///    <para> Provides methods to convert DMTF datetime and time interval to CLR compliant
    ///    <see cref='System.DateTime'/> and <see cref='System.TimeSpan'/> format and vice versa.
    ///    </para>
    /// </summary>
    /// <example>
    ///    <code lang='C#'>
    /// using System;
    /// using System.Management;
    ///
    /// // The sample below demonstrates the various conversions that can be done using ManagementDateTimeConverter class
    /// class Sample_ManagementDateTimeConverterClass
    /// {
    ///     public static int Main(string[] args)
    ///     {
    ///         string dmtfDate = "20020408141835.999999-420";
    ///         string dmtfTimeInterval = "00000010122532:123456:000";
    ///
    ///         // Converting DMTF datetime to System.DateTime
    ///         DateTime dt = ManagementDateTimeConverter.ToDateTime(dmtfDate);
    ///
    ///         // Converting System.DateTime to DMTF datetime
    ///         string dmtfDate = ManagementDateTimeConverter.ToDateTime(DateTime.Now);
    ///
    ///         // Converting DMTF timeinterval to System.TimeSpan
    ///         System.TimeSpan tsRet = ManagementDateTimeConverter. ToTimeSpan(dmtfTimeInterval);
    ///
    ///         //Converting System.TimeSpan to DMTF time interval format
    ///         System.TimeSpan ts = new System.TimeSpan(10,12,25,32,456);
    ///         string dmtfTimeInt  = ManagementDateTimeConverter.ToDmtfTimeInterval(ts);
    ///
    ///         return 0;
    ///
    ///     }
    /// }
    ///    </code>
    ///    <code lang='VB'>
    /// Imports System
    /// Imports System.Management
    ///
    /// 'The sample below demonstrates the various conversions that can be done using ManagementDateTimeConverter class
    /// Class Sample_ManagementClass
    ///     Overloads Public Shared Function Main(args() As String) As Integer
    ///         Dim dmtfDate As String = "20020408141835.999999-420"
    ///         Dim dmtfTimeInterval As String = "00000010122532:123456:000"
    ///
    ///         'Converting DMTF datetime and intervals to System.DateTime
    ///         Dim dt As DateTime = ManagementDateTimeConverter.ToDateTime(dmtfDate)
    ///
    ///         'Converting System.DateTime to DMTF datetime
    ///         dmtfDate = ManagementDateTimeConverter.ToDateTime(DateTime.Now)
    ///
    ///         ' Converting DMTF timeinterval to System.TimeSpan
    ///         Dim tsRet As System.TimeSpan = ManagementDateTimeConverter.ToTimeSpan(dmtfTimeInterval)
    ///
    ///         'Converting System.TimeSpan to DMTF time interval format
    ///         Dim ts As System.TimeSpan = New System.TimeSpan(10, 12, 25, 32, 456)
    ///         String dmtfTimeInt = ManagementDateTimeConverter.ToDmtfTimeInterval(ts)
    ///
    ///         Return 0
    ///     End Function
    /// End Class
    ///
    ///    </code>
    /// </example>
    public sealed class ManagementDateTimeConverter
    {
        // constants
        private const int SIZEOFDMTFDATETIME = 25;
        private const int MAXSIZE_UTC_DMTF = 999;
        private const long MAXDATE_INTIMESPAN = 99999999;

        private ManagementDateTimeConverter()
        {
        }


        /// <summary>
        /// <para>Converts a given DMTF datetime to <see cref='System.DateTime'/> object. The returned DateTime will be in the
        ///            current TimeZone of the system.</para>
        /// </summary>
        /// <param name='dmtfDate'>A string representing the datetime in DMTF format.</param>
        /// <returns>
        /// <para>A <see cref='System.DateTime'/> object that represents the given DMTF datetime.</para>
        /// </returns>
        /// <remarks>
        ///            <para> Date and time in WMI is represented in DMTF datetime format. This format is explained in WMI SDK documentation.
        ///                DMTF datetime string has an UTC offset which this datetime string represents.
        ///                 During conversion to <see cref='System.DateTime'/>, UTC offset is used to convert the date to the
        ///                current timezone. According to DMTF format a particular field can be represented by the character
        ///                '*'. This will be converted to the MinValue of this field that can be represented in <see cref='System.DateTime'/>.
        ///            </para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>
        /// // Convert a DMTF datetime to System.DateTime
        /// DateTime date = ManagementDateTimeConverter.ToDateTime("20020408141835.999999-420");
        ///    </code>
        ///    <code lang='VB'>
        /// ' Convert a DMTF datetime to System.DateTime
        /// Dim date as DateTime = ManagementDateTimeConverter.ToDateTime("20020408141835.999999-420")
        ///    </code>
        /// </example>
        public static DateTime ToDateTime(string dmtfDate)
        {
            int year = DateTime.MinValue.Year;
            int month = DateTime.MinValue.Month;
            int day = DateTime.MinValue.Day;
            int hour = DateTime.MinValue.Hour;
            int minute = DateTime.MinValue.Minute;
            int second = DateTime.MinValue.Second;
            string dmtf = dmtfDate;

            // If the string passed is empty or null then throw
            // an exception
            if (dmtf == null)
            {
                throw new ArgumentOutOfRangeException(nameof(dmtfDate));
            }
            if (dmtf.Length == 0)
            {
                throw new ArgumentOutOfRangeException(nameof(dmtfDate));
            }

            // if the length of the string is not equal to the
            // standard length of the DMTF datetime then throw an exception
            if (dmtf.Length != SIZEOFDMTFDATETIME)
            {
                throw new ArgumentOutOfRangeException(nameof(dmtfDate));
            }

            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));
            long ticks = 0;
            int utcOffset = 0;
            try
            {
                var tempString = dmtf.Substring(0, 4);
                if (("****" != tempString))
                {
                    year = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(4, 2);
                if (("**" != tempString))
                {
                    month = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(6, 2);
                if (("**" != tempString))
                {
                    day = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(8, 2);
                if (("**" != tempString))
                {
                    hour = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(10, 2);
                if (("**" != tempString))
                {
                    minute = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(12, 2);
                if (("**" != tempString))
                {
                    second = int.Parse(tempString, frmInt32);
                }
                tempString = dmtf.Substring(15, 6);
                if (("******" != tempString))
                {
                    ticks = (long.Parse(tempString, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(long)))) * (TimeSpan.TicksPerMillisecond / 1000);
                }
                tempString = dmtf.Substring(22, 3);
                if (("***" != tempString))
                {
                    tempString = dmtf.Substring(21, 4);
                    utcOffset = int.Parse(tempString, frmInt32);
                }

                if (year < 0 || month < 0 || day < 0 || hour < 0 || minute < 0 || second < 0 || ticks < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(dmtfDate));
                }
            }
            catch
            {
                throw new ArgumentOutOfRangeException(nameof(dmtfDate));
            }

            // codeql[cs/leap-year/unsafe-date-construction-from-two-elements] - DateTime not constructed from multiple elements - it's parsed from a string.
            var datetime = new DateTime(year, month, day, hour, minute, second, 0, DateTimeKind.Local);
            // Then add the ticks calculated from the microseconds
            datetime = datetime.AddTicks(ticks);
            // Then adjust the offset, using a manual calculation to keep the same possible range as netfx
            datetime = datetime.AddMinutes(-(utcOffset - TimeZoneInfo.Local.GetUtcOffset(datetime).Ticks / TimeSpan.TicksPerMinute));

            return datetime;
        }

        /// <summary>
        /// <para>Converts a given <see cref='System.DateTime'/> object to DMTF format.</para>
        ///
        /// </summary>
        /// <param name='date'>A <see cref='System.DateTime'/> object representing the datetime to be converted to DMTF datetime.</param>
        /// <returns>
        /// <para>A string that represents the DMTF datetime for the given DateTime object.</para>
        /// </returns>
        /// <remarks>
        ///            <para> Date and time in WMI is represented in DMTF datetime format. This format is explained in WMI SDK documentation.
        ///                The DMTF datetime string represented will be with respect to the UTC offset of the
        ///                current timezone. The lowest precision in DMTF is microseconds and
        ///                in <see cref='System.DateTime'/> is Ticks , which is equivalent to 100 of nanoseconds.
        ///                 During conversion these Ticks are converted to microseconds and rounded
        ///                 off to the nearest microsecond.
        ///            </para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>
        /// // Convert the current time in System.DateTime to DMTF format
        /// string dmtfDateTime = ManagementDateTimeConverter.ToDmtfDateTime(DateTime.Now);
        ///    </code>
        ///    <code lang='VB'>
        /// ' Convert the current time in System.DateTime to DMTF format
        /// Dim dmtfDateTime as String = ManagementDateTimeConverter.ToDmtfDateTime(DateTime.Now)
        ///    </code>
        /// </example>
        public static string ToDmtfDateTime(DateTime date)
        {
            string UtcString = string.Empty;
            // Fill up the UTC field in the DMTF date with the current
            // zones UTC value. If date kind is UTC use offset of zero to match .NET Framework (i.e.: TimeZone.GetUtcOffset)
            TimeSpan tickOffset = date.Kind == DateTimeKind.Utc ? TimeSpan.Zero : TimeZoneInfo.Local.GetUtcOffset(date);
            long OffsetMins = (tickOffset.Ticks / System.TimeSpan.TicksPerMinute);
            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));

            // If the offset is more than that what can be specified in DMTF format, then
            // convert the date to UniversalTime
            if (Math.Abs(OffsetMins) > MAXSIZE_UTC_DMTF)
            {
                date = date.ToUniversalTime();
                UtcString = "+000";
            }
            else
            if ((tickOffset.Ticks >= 0))
            {
                UtcString = "+" + ((tickOffset.Ticks / System.TimeSpan.TicksPerMinute)).ToString(frmInt32).PadLeft(3, '0');
            }
            else
            {
                string strTemp = OffsetMins.ToString(frmInt32);
                UtcString = "-" + strTemp.Substring(1).PadLeft(3, '0');
            }

            string dmtfDateTime = date.Year.ToString(frmInt32).PadLeft(4, '0');

            dmtfDateTime += date.Month.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Day.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Hour.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Minute.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += date.Second.ToString(frmInt32).PadLeft(2, '0');
            dmtfDateTime += ".";

            // Construct a DateTime with the precision to Second as same as the passed DateTime and so get
            // the ticks difference so that the microseconds can be calculated
            DateTime dtTemp = new DateTime(date.Year, date.Month, date.Day, date.Hour, date.Minute, date.Second, 0);
            long microsec = ((date.Ticks - dtTemp.Ticks) * 1000) / System.TimeSpan.TicksPerMillisecond;

            // fill the microseconds field
            string strMicrosec = microsec.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(long)));
            if (strMicrosec.Length > 6)
            {
                strMicrosec = strMicrosec.Substring(0, 6);
            }
            dmtfDateTime += strMicrosec.PadLeft(6, '0');
            // adding the UTC offset
            dmtfDateTime += UtcString;

            return dmtfDateTime;
        }
        /// <summary>
        /// <para>Converts a given DMTF time interval to <see cref='System.TimeSpan'/> object.</para>
        /// </summary>
        /// <param name='dmtfTimespan'>A string represesentation of the DMTF time interval.</param>
        /// <returns>
        /// <para>A <see cref='System.TimeSpan'/> object that represents the given DMTF time interval.</para>
        /// </returns>
        /// <remarks>
        ///            <para> Time interval in WMI is represented in DMTF format. This format is explained in WMI SDK documentation.
        ///                    If the DMTF time interval value is more than that of
        ///                    <see cref='System.TimeSpan.MaxValue'/> then <see cref='System.ArgumentOutOfRangeException'/> is thrown.
        ///            </para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>
        /// // Convert a DMTF time interval to System.TimeSpan
        /// TimeSpan dmtfTimeInterval = ManagementDateTimeConverter.ToTimeSpan("00000010122532:123456:000");
        ///    </code>
        ///    <code lang='VB'>
        /// ' Convert a DMTF time interval to System.TimeSpan
        /// Dim ts as TimeSpan = ManagementDateTimeConverter.ToTimeSpan("00000010122532:123456:000")
        ///    </code>
        /// </example>
        public static TimeSpan ToTimeSpan(string dmtfTimespan)
        {
            int days = 0;
            int hours = 0;
            int minutes = 0;
            int seconds = 0;
            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));

            string dmtfts = dmtfTimespan;
            TimeSpan timespan = TimeSpan.MinValue;

            if (dmtfts == null)
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }
            if (dmtfts.Length == 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }
            if (dmtfts.Length != SIZEOFDMTFDATETIME)
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }
            if (dmtfts.Substring(21, 4) != ":000")
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }

            long ticks = 0;
            try
            {
                string tempString = string.Empty;

                tempString = dmtfts.Substring(0, 8);
                days = int.Parse(tempString, frmInt32);

                tempString = dmtfts.Substring(8, 2);
                hours = int.Parse(tempString, frmInt32);

                tempString = dmtfts.Substring(10, 2);
                minutes = int.Parse(tempString, frmInt32);

                tempString = dmtfts.Substring(12, 2);
                seconds = int.Parse(tempString, frmInt32);

                tempString = dmtfts.Substring(15, 6);
                ticks = (long.Parse(tempString, (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(long)))) * (System.TimeSpan.TicksPerMillisecond / 1000);

            }
            catch
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }

            if (days < 0 || hours < 0 || minutes < 0 || seconds < 0 || ticks < 0)
            {
                throw new System.ArgumentOutOfRangeException(nameof(dmtfTimespan));
            }

            timespan = new System.TimeSpan(days, hours, minutes, seconds, 0);
            // Get a timepan for the additional ticks obtained for the microsecond part of DMTF time interval
            // and then add it to the original timespan
            TimeSpan tsTemp = System.TimeSpan.FromTicks(ticks);
            timespan += tsTemp;

            return timespan;
        }

        /// <summary>
        /// <para>Converts a given <see cref='System.TimeSpan'/> object to DMTF time interval.</para>
        /// </summary>
        /// <param name='timespan'> A <see cref='System.TimeSpan'/> object representing the datetime to be converted to DMTF time interval.
        /// </param>
        /// <returns>
        /// <para>A string that represents the DMTF time interval for the given TimeSpan object.</para>
        /// </returns>
        /// <remarks>
        ///            <para> Time interval in WMI is represented in DMTF datetime format. This format
        ///                is explained in WMI SDK documentation. The lowest precision in
        ///                DMTF is microseconds and in <see cref='System.TimeSpan'/> is Ticks , which is equivalent
        ///                to 100 of nanoseconds.During conversion these Ticks are converted to
        ///                microseconds and rounded off to the nearest microsecond.
        ///            </para>
        /// </remarks>
        /// <example>
        ///    <code lang='C#'>
        /// // Construct a Timespan object and convert it to DMTF format
        /// System.TimeSpan ts = new System.TimeSpan(10,12,25,32,456);
        /// String dmtfTimeInterval = ManagementDateTimeConverter.ToDmtfTimeInterval(ts);
        ///    </code>
        ///    <code lang='VB'>
        /// // Construct a Timespan object and convert it to DMTF format
        /// Dim ts as System.TimeSpan = new System.TimeSpan(10,12,25,32,456)
        /// Dim dmtfTimeInterval as String = ManagementDateTimeConverter.ToDmtfTimeInterval(ts)
        ///    </code>
        /// </example>
        public static string ToDmtfTimeInterval(TimeSpan timespan)
        {

            string dmtftimespan = timespan.Days.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int))).PadLeft(8, '0');
            IFormatProvider frmInt32 = (IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(int));

            // Days that can be represented is more than what can be represented
            // then throw an exception
            // and also negative timespan cannot be represented in DMTF
            if (timespan.Days > MAXDATE_INTIMESPAN || timespan < TimeSpan.Zero)
            {
                throw new System.ArgumentOutOfRangeException(nameof(timespan));
            }

            dmtftimespan += timespan.Hours.ToString(frmInt32).PadLeft(2, '0');
            dmtftimespan += timespan.Minutes.ToString(frmInt32).PadLeft(2, '0');
            dmtftimespan += timespan.Seconds.ToString(frmInt32).PadLeft(2, '0');
            dmtftimespan += ".";

            // Construct a DateTime with the precision to Second as same as the passed DateTime and so get
            // the ticks difference so that the microseconds can be calculated
            TimeSpan tsTemp = new TimeSpan(timespan.Days, timespan.Hours, timespan.Minutes, timespan.Seconds, 0);
            long microsec = ((timespan.Ticks - tsTemp.Ticks) * 1000) / System.TimeSpan.TicksPerMillisecond;

            // fill the microseconds field
            string strMicrosec = microsec.ToString((IFormatProvider)CultureInfo.InvariantCulture.GetFormat(typeof(long)));
            if (strMicrosec.Length > 6)
            {
                strMicrosec = strMicrosec.Substring(0, 6);
            }
            dmtftimespan += strMicrosec.PadLeft(6, '0');

            dmtftimespan += ":000";

            return dmtftimespan;
        }
    } // ManagementDateTimeConverter
}
