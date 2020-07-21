// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Globalization
{
    public partial class ChineseLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int ChineseEra = 1;
        public ChineseLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public abstract partial class EastAsianLunisolarCalendar : System.Globalization.Calendar
    {
        internal EastAsianLunisolarCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public int GetCelestialStem(int sexagenaryYear) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public virtual int GetSexagenaryYear(System.DateTime time) { throw null; }
        public int GetTerrestrialBranch(int sexagenaryYear) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class GregorianCalendar : System.Globalization.Calendar
    {
        public const int ADEra = 1;
        public GregorianCalendar() { }
        public GregorianCalendar(System.Globalization.GregorianCalendarTypes type) { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public virtual System.Globalization.GregorianCalendarTypes CalendarType { get { throw null; } set { } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public enum GregorianCalendarTypes
    {
        Localized = 1,
        USEnglish = 2,
        MiddleEastFrench = 9,
        Arabic = 10,
        TransliteratedEnglish = 11,
        TransliteratedFrench = 12,
    }
    public partial class HebrewCalendar : System.Globalization.Calendar
    {
        public static readonly int HebrewEra;
        public HebrewCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class HijriCalendar : System.Globalization.Calendar
    {
        public static readonly int HijriEra;
        public HijriCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public int HijriAdjustment { get { throw null; } set { } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class JapaneseCalendar : System.Globalization.Calendar
    {
        public JapaneseCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class JapaneseLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int JapaneseEra = 1;
        public JapaneseLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class JulianCalendar : System.Globalization.Calendar
    {
        public static readonly int JulianEra;
        public JulianCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class KoreanCalendar : System.Globalization.Calendar
    {
        public const int KoreanEra = 1;
        public KoreanCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class KoreanLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public const int GregorianEra = 1;
        public KoreanLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class PersianCalendar : System.Globalization.Calendar
    {
        public static readonly int PersianEra;
        public PersianCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class TaiwanCalendar : System.Globalization.Calendar
    {
        public TaiwanCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class TaiwanLunisolarCalendar : System.Globalization.EastAsianLunisolarCalendar
    {
        public TaiwanLunisolarCalendar() { }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int GetEra(System.DateTime time) { throw null; }
    }
    public partial class ThaiBuddhistCalendar : System.Globalization.Calendar
    {
        public const int ThaiBuddhistEra = 1;
        public ThaiBuddhistCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
    public partial class UmAlQuraCalendar : System.Globalization.Calendar
    {
        public const int UmAlQuraEra = 1;
        public UmAlQuraCalendar() { }
        public override System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected override int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public override int[] Eras { get { throw null; } }
        public override System.DateTime MaxSupportedDateTime { get { throw null; } }
        public override System.DateTime MinSupportedDateTime { get { throw null; } }
        public override int TwoDigitYearMax { get { throw null; } set { } }
        public override System.DateTime AddMonths(System.DateTime time, int months) { throw null; }
        public override System.DateTime AddYears(System.DateTime time, int years) { throw null; }
        public override int GetDayOfMonth(System.DateTime time) { throw null; }
        public override System.DayOfWeek GetDayOfWeek(System.DateTime time) { throw null; }
        public override int GetDayOfYear(System.DateTime time) { throw null; }
        public override int GetDaysInMonth(int year, int month, int era) { throw null; }
        public override int GetDaysInYear(int year, int era) { throw null; }
        public override int GetEra(System.DateTime time) { throw null; }
        public override int GetLeapMonth(int year, int era) { throw null; }
        public override int GetMonth(System.DateTime time) { throw null; }
        public override int GetMonthsInYear(int year, int era) { throw null; }
        public override int GetYear(System.DateTime time) { throw null; }
        public override bool IsLeapDay(int year, int month, int day, int era) { throw null; }
        public override bool IsLeapMonth(int year, int month, int era) { throw null; }
        public override bool IsLeapYear(int year, int era) { throw null; }
        public override System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era) { throw null; }
        public override int ToFourDigitYear(int year) { throw null; }
    }
}
