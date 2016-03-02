// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Collections.Generic;

// TimeSpanSupport.cs
// Support file for parsing and formatting tests
// The desktop changes 2009/04/21 KatyK are ported 2009/06/08 DidemG
public static class Support
{
    public const String NegativeSign = "-";
    public const String TimeSeparator = ":";
    public const String CDaySep = ".";
    public const String CDecSep = ".";
    public const String GDaySep = ":";

    static List<TimeSpan> _interestingTimeSpans = null;
    static List<String> _errorFormats = null;

    public static List<TimeSpan> InterestingTimeSpans
    {
        get
        {
            if (_interestingTimeSpans != null)
                return _interestingTimeSpans;

            // else, populate the list
            List<TimeSpan> list = new List<TimeSpan>();
            list.Add(TimeSpan.Zero);
            // positive
            list.Add(TimeSpan.MaxValue);
            list.Add(TimeSpan.FromDays(1));
            list.Add(TimeSpan.FromDays(1234));
            list.Add(TimeSpan.FromHours(2));
            list.Add(TimeSpan.FromHours(12));
            list.Add(TimeSpan.FromMinutes(3));
            list.Add(TimeSpan.FromMinutes(34));
            list.Add(TimeSpan.FromSeconds(5));
            list.Add(TimeSpan.FromSeconds(56));
            list.Add(TimeSpan.FromMilliseconds(7));
            list.Add(TimeSpan.FromMilliseconds(78));
            list.Add(TimeSpan.FromMilliseconds(789));
            list.Add(TimeSpan.FromMilliseconds(780));
            list.Add(new TimeSpan(0, 2, 3, 4));
            list.Add(new TimeSpan(1, 2, 3, 4));
            list.Add(new TimeSpan(0, 2, 3, 4, 56));
            list.Add(new TimeSpan(1, 2, 3, 4, 56));
            list.Add(new TimeSpan(1, 2, 3, 4, 560));
            list.Add(new TimeSpan(1, 2, 3, 4, 567));
            list.Add(new TimeSpan(1, 2, 3, 4, 567).Add(new TimeSpan(8900)));
            list.Add(new TimeSpan(1, 2, 3, 4, 567).Add(new TimeSpan(8901)));
            // negative
            list.Add(TimeSpan.MinValue);
            list.Add(TimeSpan.FromDays(-1));
            list.Add(TimeSpan.FromDays(-1234));
            list.Add(TimeSpan.FromHours(-2));
            list.Add(TimeSpan.FromHours(-12));
            list.Add(TimeSpan.FromMinutes(-3));
            list.Add(TimeSpan.FromMinutes(-34));
            list.Add(TimeSpan.FromSeconds(-5));
            list.Add(TimeSpan.FromSeconds(-56));
            list.Add(TimeSpan.FromMilliseconds(-7));
            list.Add(TimeSpan.FromMilliseconds(-78));
            list.Add(TimeSpan.FromMilliseconds(-789));
            list.Add(TimeSpan.FromMilliseconds(-780));
            list.Add(new TimeSpan(-0, -2, -3, -4));
            list.Add(new TimeSpan(-1, -2, -3, -4));
            list.Add(new TimeSpan(-0, -2, -3, -4, -56));
            list.Add(new TimeSpan(-1, -2, -3, -4, -56));
            list.Add(new TimeSpan(-1, -2, -3, -4, -560));
            list.Add(new TimeSpan(-1, -2, -3, -4, -567));
            list.Add(new TimeSpan(-1, -2, -3, -4, -567).Add(new TimeSpan(-8900)));
            list.Add(new TimeSpan(-1, -2, -3, -4, -567).Add(new TimeSpan(-8901)));

            _interestingTimeSpans = list;
            return _interestingTimeSpans;
        }
    }

    public static List<String> ErrorFormats
    {
        get
        {
            if (_errorFormats != null)
                return _errorFormats;

            // else, populate the list
            String[] errorValues = {
                   "a", "A", "b", "B", "C", "d", "D", "e", "E", "f", "F", "h", "H",  
		           "i", "I", "j", "J", "k", "l", "L", "m", "M", "n", "N", "o", "O",
		           "p", "q", "Q", "r", "R", "s", "S", "u", "U", "v", "V", "w", "W",
                   "x", "X", "y", "Y", "z", "Z",
		           ".", ":", ",", "/", "\\", " ", "-", "%", "*", "  ", "\t",
                   "ddddddddd", "hhh", "mmm", "sss", "ffffffff", "FFFFFFFF",
                   "literal", "d ",
            };
            _errorFormats = new List<String>(errorValues);
            return _errorFormats;
        }
    }

    public static String CFormat(TimeSpan ts)
    {
        return Neg(ts) + OptionalDay(ts, CDaySep) + HHMMSS(ts) + OptionalFraction_f(ts, CDecSep);
    }
    public static String gFormat(TimeSpan ts, String GDecSep)
    {
        return Neg(ts) + OptionalDay(ts, GDaySep) + HMMSS(ts) + OptionalFraction_F(ts, GDecSep);
    }
    public static String GFormat(TimeSpan ts, String GDecSep)
    {
        return Neg(ts) + Day(ts, GDaySep) + HHMMSS(ts) + Fraction(ts, GDecSep);
    }

    public static String Neg(TimeSpan timeSpan)
    {
        if (timeSpan < TimeSpan.Zero)
            return NegativeSign;
        else
            return String.Empty;
    }
    public static String Day(TimeSpan timeSpan, String daySeparator)
    {
        return Math.Abs(timeSpan.Days).ToString() + daySeparator;
    }
    public static String OptionalDay(TimeSpan timeSpan, String daySeparator)
    {
        return timeSpan.Days == 0 ? "" : Day(timeSpan, daySeparator);
    }
    public static String HHMMSS(TimeSpan timeSpan)
    { 
        return String.Format("{0:d2}{3}{1:d2}{3}{2:d2}", Math.Abs(timeSpan.Hours), Math.Abs(timeSpan.Minutes), Math.Abs(timeSpan.Seconds), TimeSeparator);
    }
    public static String HMMSS(TimeSpan timeSpan)
    {
        return String.Format("{0:d}{3}{1:d2}{3}{2:d2}", Math.Abs(timeSpan.Hours), Math.Abs(timeSpan.Minutes), Math.Abs(timeSpan.Seconds), TimeSeparator);
    }
    public static String Fraction(TimeSpan timeSpan, String decimalSeparator)
    {
        int ticksonly = Math.Abs((int)(timeSpan.Ticks % TimeSpan.TicksPerSecond));
        return decimalSeparator + ticksonly.ToString("d7");
    }
    public static String OptionalFraction_f(TimeSpan timeSpan, String decimalSeparator)
    {
        int ticksonly = Math.Abs((int)(timeSpan.Ticks % TimeSpan.TicksPerSecond));
        if (ticksonly == 0)
            return String.Empty;
        else
            return decimalSeparator + ticksonly.ToString("d7");
    }
    public static String OptionalFraction_F(TimeSpan timeSpan, String decimalSeparator)
    {
        int ticksonly = Math.Abs((int)(timeSpan.Ticks % TimeSpan.TicksPerSecond));
        DateTime dt = new DateTime(ticksonly);
        String str = dt.ToString("FFFFFFF");
        if (str.Length == 0)
            return str;
        else
            return decimalSeparator + str;
    }

    public static String PrintTimeSpan(TimeSpan ts)
    {
        String ret = String.Format("{0}d {1}h {2}m {3}s {4:F4}ms", ts.Days, ts.Hours, ts.Minutes, ts.Seconds, (ts.Ticks % TimeSpan.TicksPerSecond / (float)TimeSpan.TicksPerMillisecond));
        return ret;
    }
}
