// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Globalization
{
    public abstract partial class Calendar
    {
        public const int CurrentEra = 0;
        protected Calendar() { }
        public abstract int[] Eras { get; }
        public bool IsReadOnly { get { return default(bool); } }
        public virtual System.DateTime MaxSupportedDateTime { get { return default(System.DateTime); } }
        public virtual System.DateTime MinSupportedDateTime { get { return default(System.DateTime); } }
        public virtual int TwoDigitYearMax { get { return default(int); } set { } }
        public virtual System.DateTime AddDays(System.DateTime time, int days) { return default(System.DateTime); }
        public virtual System.DateTime AddHours(System.DateTime time, int hours) { return default(System.DateTime); }
        public virtual System.DateTime AddMilliseconds(System.DateTime time, double milliseconds) { return default(System.DateTime); }
        public virtual System.DateTime AddMinutes(System.DateTime time, int minutes) { return default(System.DateTime); }
        public abstract System.DateTime AddMonths(System.DateTime time, int months);
        public virtual System.DateTime AddSeconds(System.DateTime time, int seconds) { return default(System.DateTime); }
        public virtual System.DateTime AddWeeks(System.DateTime time, int weeks) { return default(System.DateTime); }
        public abstract System.DateTime AddYears(System.DateTime time, int years);
        public abstract int GetDayOfMonth(System.DateTime time);
        public abstract System.DayOfWeek GetDayOfWeek(System.DateTime time);
        public abstract int GetDayOfYear(System.DateTime time);
        public virtual int GetDaysInMonth(int year, int month) { return default(int); }
        public abstract int GetDaysInMonth(int year, int month, int era);
        public virtual int GetDaysInYear(int year) { return default(int); }
        public abstract int GetDaysInYear(int year, int era);
        public abstract int GetEra(System.DateTime time);
        public virtual int GetHour(System.DateTime time) { return default(int); }
        public virtual int GetLeapMonth(int year, int era) { return default(int); }
        public virtual double GetMilliseconds(System.DateTime time) { return default(double); }
        public virtual int GetMinute(System.DateTime time) { return default(int); }
        public abstract int GetMonth(System.DateTime time);
        public virtual int GetMonthsInYear(int year) { return default(int); }
        public abstract int GetMonthsInYear(int year, int era);
        public virtual int GetSecond(System.DateTime time) { return default(int); }
        public virtual int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { return default(int); }
        public abstract int GetYear(System.DateTime time);
        public virtual bool IsLeapDay(int year, int month, int day) { return default(bool); }
        public abstract bool IsLeapDay(int year, int month, int day, int era);
        public virtual bool IsLeapMonth(int year, int month) { return default(bool); }
        public abstract bool IsLeapMonth(int year, int month, int era);
        public virtual bool IsLeapYear(int year) { return default(bool); }
        public abstract bool IsLeapYear(int year, int era);
        public virtual System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { return default(System.DateTime); }
        public abstract System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);
        public virtual int ToFourDigitYear(int year) { return default(int); }
    }
    public enum CalendarWeekRule
    {
        FirstDay = 0,
        FirstFourDayWeek = 2,
        FirstFullWeek = 1,
    }
    public static partial class CharUnicodeInfo
    {
        public static double GetNumericValue(char ch) { return default(double); }
        public static double GetNumericValue(string s, int index) { return default(double); }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(char ch) { return default(System.Globalization.UnicodeCategory); }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(string s, int index) { return default(System.Globalization.UnicodeCategory); }
    }
    public partial class CompareInfo
    {
        internal CompareInfo() { }
        public virtual string Name { get { return default(string); } }
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2) { return default(int); }
        public virtual int Compare(string string1, int offset1, int length1, string string2, int offset2, int length2, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int Compare(string string1, int offset1, string string2, int offset2) { return default(int); }
        public virtual int Compare(string string1, int offset1, string string2, int offset2, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int Compare(string string1, string string2) { return default(int); }
        public virtual int Compare(string string1, string string2, System.Globalization.CompareOptions options) { return default(int); }
        public override bool Equals(object value) { return default(bool); }
        public static System.Globalization.CompareInfo GetCompareInfo(string name) { return default(System.Globalization.CompareInfo); }
        public override int GetHashCode() { return default(int); }
        public virtual int GetHashCode(string source, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, char value) { return default(int); }
        public virtual int IndexOf(string source, char value, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, char value, int startIndex, int count) { return default(int); }
        public virtual int IndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, string value) { return default(int); }
        public virtual int IndexOf(string source, string value, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int IndexOf(string source, string value, int startIndex, int count) { return default(int); }
        public virtual int IndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { return default(int); }
        public virtual bool IsPrefix(string source, string prefix) { return default(bool); }
        public virtual bool IsPrefix(string source, string prefix, System.Globalization.CompareOptions options) { return default(bool); }
        public virtual bool IsSuffix(string source, string suffix) { return default(bool); }
        public virtual bool IsSuffix(string source, string suffix, System.Globalization.CompareOptions options) { return default(bool); }
        public virtual int LastIndexOf(string source, char value) { return default(int); }
        public virtual int LastIndexOf(string source, char value, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int LastIndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int LastIndexOf(string source, char value, int startIndex, int count) { return default(int); }
        public virtual int LastIndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int LastIndexOf(string source, string value) { return default(int); }
        public virtual int LastIndexOf(string source, string value, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int LastIndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { return default(int); }
        public virtual int LastIndexOf(string source, string value, int startIndex, int count) { return default(int); }
        public virtual int LastIndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { return default(int); }
        public override string ToString() { return default(string); }
    }
    [System.FlagsAttribute]
    public enum CompareOptions
    {
        IgnoreCase = 1,
        IgnoreKanaType = 8,
        IgnoreNonSpace = 2,
        IgnoreSymbols = 4,
        IgnoreWidth = 16,
        None = 0,
        Ordinal = 1073741824,
        OrdinalIgnoreCase = 268435456,
        StringSort = 536870912,
    }
    public partial class CultureInfo : System.IFormatProvider
    {
        public CultureInfo(string name) { }
        public virtual System.Globalization.Calendar Calendar { get { return default(System.Globalization.Calendar); } }
        public virtual System.Globalization.CompareInfo CompareInfo { get { return default(System.Globalization.CompareInfo); } }
        public static System.Globalization.CultureInfo CurrentCulture { get { return default(System.Globalization.CultureInfo); } set { } }
        public static System.Globalization.CultureInfo CurrentUICulture { get { return default(System.Globalization.CultureInfo); } set { } }
        public virtual System.Globalization.DateTimeFormatInfo DateTimeFormat { get { return default(System.Globalization.DateTimeFormatInfo); } set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentCulture { get { return default(System.Globalization.CultureInfo); } set { } }
        public static System.Globalization.CultureInfo DefaultThreadCurrentUICulture { get { return default(System.Globalization.CultureInfo); } set { } }
        public virtual string DisplayName { get { return default(string); } }
        public virtual string EnglishName { get { return default(string); } }
        public static System.Globalization.CultureInfo InvariantCulture { get { return default(System.Globalization.CultureInfo); } }
        public virtual bool IsNeutralCulture { get { return default(bool); } }
        public bool IsReadOnly { get { return default(bool); } }
        public virtual string Name { get { return default(string); } }
        public virtual string NativeName { get { return default(string); } }
        public virtual System.Globalization.NumberFormatInfo NumberFormat { get { return default(System.Globalization.NumberFormatInfo); } set { } }
        public virtual System.Globalization.Calendar[] OptionalCalendars { get { return default(System.Globalization.Calendar[]); } }
        public virtual System.Globalization.CultureInfo Parent { get { return default(System.Globalization.CultureInfo); } }
        public virtual System.Globalization.TextInfo TextInfo { get { return default(System.Globalization.TextInfo); } }
        public virtual string TwoLetterISOLanguageName { get { return default(string); } }
        public virtual object Clone() { return default(object); }
        public override bool Equals(object value) { return default(bool); }
        public virtual object GetFormat(System.Type formatType) { return default(object); }
        public override int GetHashCode() { return default(int); }
        public static System.Globalization.CultureInfo ReadOnly(System.Globalization.CultureInfo ci) { return default(System.Globalization.CultureInfo); }
        public override string ToString() { return default(string); }
    }
    public partial class CultureNotFoundException : System.ArgumentException
    {
        public CultureNotFoundException() { }
        public CultureNotFoundException(string message) { }
        public CultureNotFoundException(string message, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, string message) { }
        public CultureNotFoundException(string message, string invalidCultureName, System.Exception innerException) { }
        public CultureNotFoundException(string paramName, string invalidCultureName, string message) { }
        public virtual string InvalidCultureName { get { return default(string); } }
        public override string Message { get { return default(string); } }
    }
    public sealed partial class DateTimeFormatInfo : System.IFormatProvider
    {
        public DateTimeFormatInfo() { }
        public string[] AbbreviatedDayNames { get { return default(string[]); } set { } }
        public string[] AbbreviatedMonthGenitiveNames { get { return default(string[]); } set { } }
        public string[] AbbreviatedMonthNames { get { return default(string[]); } set { } }
        public string AMDesignator { get { return default(string); } set { } }
        public System.Globalization.Calendar Calendar { get { return default(System.Globalization.Calendar); } set { } }
        public System.Globalization.CalendarWeekRule CalendarWeekRule { get { return default(System.Globalization.CalendarWeekRule); } set { } }
        public static System.Globalization.DateTimeFormatInfo CurrentInfo { get { return default(System.Globalization.DateTimeFormatInfo); } }
        public string[] DayNames { get { return default(string[]); } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { return default(System.DayOfWeek); } set { } }
        public string FullDateTimePattern { get { return default(string); } set { } }
        public static System.Globalization.DateTimeFormatInfo InvariantInfo { get { return default(System.Globalization.DateTimeFormatInfo); } }
        public bool IsReadOnly { get { return default(bool); } }
        public string LongDatePattern { get { return default(string); } set { } }
        public string LongTimePattern { get { return default(string); } set { } }
        public string MonthDayPattern { get { return default(string); } set { } }
        public string[] MonthGenitiveNames { get { return default(string[]); } set { } }
        public string[] MonthNames { get { return default(string[]); } set { } }
        public string PMDesignator { get { return default(string); } set { } }
        public string RFC1123Pattern { get { return default(string); } }
        public string ShortDatePattern { get { return default(string); } set { } }
        public string[] ShortestDayNames { get { return default(string[]); } set { } }
        public string ShortTimePattern { get { return default(string); } set { } }
        public string SortableDateTimePattern { get { return default(string); } }
        public string UniversalSortableDateTimePattern { get { return default(string); } }
        public string YearMonthPattern { get { return default(string); } set { } }
        public object Clone() { return default(object); }
        public string GetAbbreviatedDayName(System.DayOfWeek dayofweek) { return default(string); }
        public string GetAbbreviatedEraName(int era) { return default(string); }
        public string GetAbbreviatedMonthName(int month) { return default(string); }
        public string GetDayName(System.DayOfWeek dayofweek) { return default(string); }
        public int GetEra(string eraName) { return default(int); }
        public string GetEraName(int era) { return default(string); }
        public object GetFormat(System.Type formatType) { return default(object); }
        public static System.Globalization.DateTimeFormatInfo GetInstance(System.IFormatProvider provider) { return default(System.Globalization.DateTimeFormatInfo); }
        public string GetMonthName(int month) { return default(string); }
        public static System.Globalization.DateTimeFormatInfo ReadOnly(System.Globalization.DateTimeFormatInfo dtfi) { return default(System.Globalization.DateTimeFormatInfo); }
    }
    public sealed partial class NumberFormatInfo : System.IFormatProvider
    {
        public NumberFormatInfo() { }
        public int CurrencyDecimalDigits { get { return default(int); } set { } }
        public string CurrencyDecimalSeparator { get { return default(string); } set { } }
        public string CurrencyGroupSeparator { get { return default(string); } set { } }
        public int[] CurrencyGroupSizes { get { return default(int[]); } set { } }
        public int CurrencyNegativePattern { get { return default(int); } set { } }
        public int CurrencyPositivePattern { get { return default(int); } set { } }
        public string CurrencySymbol { get { return default(string); } set { } }
        public static System.Globalization.NumberFormatInfo CurrentInfo { get { return default(System.Globalization.NumberFormatInfo); } }
        public static System.Globalization.NumberFormatInfo InvariantInfo { get { return default(System.Globalization.NumberFormatInfo); } }
        public bool IsReadOnly { get { return default(bool); } }
        public string NaNSymbol { get { return default(string); } set { } }
        public string NegativeInfinitySymbol { get { return default(string); } set { } }
        public string NegativeSign { get { return default(string); } set { } }
        public int NumberDecimalDigits { get { return default(int); } set { } }
        public string NumberDecimalSeparator { get { return default(string); } set { } }
        public string NumberGroupSeparator { get { return default(string); } set { } }
        public int[] NumberGroupSizes { get { return default(int[]); } set { } }
        public int NumberNegativePattern { get { return default(int); } set { } }
        public int PercentDecimalDigits { get { return default(int); } set { } }
        public string PercentDecimalSeparator { get { return default(string); } set { } }
        public string PercentGroupSeparator { get { return default(string); } set { } }
        public int[] PercentGroupSizes { get { return default(int[]); } set { } }
        public int PercentNegativePattern { get { return default(int); } set { } }
        public int PercentPositivePattern { get { return default(int); } set { } }
        public string PercentSymbol { get { return default(string); } set { } }
        public string PerMilleSymbol { get { return default(string); } set { } }
        public string PositiveInfinitySymbol { get { return default(string); } set { } }
        public string PositiveSign { get { return default(string); } set { } }
        public object Clone() { return default(object); }
        public object GetFormat(System.Type formatType) { return default(object); }
        public static System.Globalization.NumberFormatInfo GetInstance(System.IFormatProvider formatProvider) { return default(System.Globalization.NumberFormatInfo); }
        public static System.Globalization.NumberFormatInfo ReadOnly(System.Globalization.NumberFormatInfo nfi) { return default(System.Globalization.NumberFormatInfo); }
    }
    public partial class RegionInfo
    {
        public RegionInfo(string name) { }
        public virtual string CurrencySymbol { get { return default(string); } }
        public static System.Globalization.RegionInfo CurrentRegion { get { return default(System.Globalization.RegionInfo); } }
        public virtual string DisplayName { get { return default(string); } }
        public virtual string EnglishName { get { return default(string); } }
        public virtual bool IsMetric { get { return default(bool); } }
        public virtual string ISOCurrencySymbol { get { return default(string); } }
        public virtual string Name { get { return default(string); } }
        public virtual string NativeName { get { return default(string); } }
        public virtual string TwoLetterISORegionName { get { return default(string); } }
        public override bool Equals(object value) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public override string ToString() { return default(string); }
    }
    public partial class StringInfo
    {
        public StringInfo() { }
        public StringInfo(string value) { }
        public int LengthInTextElements { get { return default(int); } }
        public string String { get { return default(string); } set { } }
        public override bool Equals(object value) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static string GetNextTextElement(string str) { return default(string); }
        public static string GetNextTextElement(string str, int index) { return default(string); }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str) { return default(System.Globalization.TextElementEnumerator); }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str, int index) { return default(System.Globalization.TextElementEnumerator); }
        public static int[] ParseCombiningCharacters(string str) { return default(int[]); }
    }
    public partial class TextElementEnumerator : System.Collections.IEnumerator
    {
        internal TextElementEnumerator() { }
        public object Current { get { return default(object); } }
        public int ElementIndex { get { return default(int); } }
        public string GetTextElement() { return default(string); }
        public bool MoveNext() { return default(bool); }
        public void Reset() { }
    }
    public partial class TextInfo
    {
        internal TextInfo() { }
        public string CultureName { get { return default(string); } }
        public bool IsReadOnly { get { return default(bool); } }
        public bool IsRightToLeft { get { return default(bool); } }
        public virtual string ListSeparator { get { return default(string); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public virtual char ToLower(char c) { return default(char); }
        public virtual string ToLower(string str) { return default(string); }
        public override string ToString() { return default(string); }
        public virtual char ToUpper(char c) { return default(char); }
        public virtual string ToUpper(string str) { return default(string); }
    }
    public enum UnicodeCategory
    {
        ClosePunctuation = 21,
        ConnectorPunctuation = 18,
        Control = 14,
        CurrencySymbol = 26,
        DashPunctuation = 19,
        DecimalDigitNumber = 8,
        EnclosingMark = 7,
        FinalQuotePunctuation = 23,
        Format = 15,
        InitialQuotePunctuation = 22,
        LetterNumber = 9,
        LineSeparator = 12,
        LowercaseLetter = 1,
        MathSymbol = 25,
        ModifierLetter = 3,
        ModifierSymbol = 27,
        NonSpacingMark = 5,
        OpenPunctuation = 20,
        OtherLetter = 4,
        OtherNotAssigned = 29,
        OtherNumber = 10,
        OtherPunctuation = 24,
        OtherSymbol = 28,
        ParagraphSeparator = 13,
        PrivateUse = 17,
        SpaceSeparator = 11,
        SpacingCombiningMark = 6,
        Surrogate = 16,
        TitlecaseLetter = 2,
        UppercaseLetter = 0,
    }
}
