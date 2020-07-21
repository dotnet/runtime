// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Globalization
{
    public abstract partial class Calendar : System.ICloneable
    {
        public const int CurrentEra = 0;
        protected Calendar() { }
        public virtual System.Globalization.CalendarAlgorithmType AlgorithmType { get { throw null; } }
        protected virtual int DaysInYearBeforeMinSupportedYear { get { throw null; } }
        public abstract int[] Eras { get; }
        public bool IsReadOnly { get { throw null; } }
        public virtual System.DateTime MaxSupportedDateTime { get { throw null; } }
        public virtual System.DateTime MinSupportedDateTime { get { throw null; } }
        public virtual int TwoDigitYearMax { get { throw null; } set { } }
        public virtual System.DateTime AddDays(System.DateTime time, int days) { throw null; }
        public virtual System.DateTime AddHours(System.DateTime time, int hours) { throw null; }
        public virtual System.DateTime AddMilliseconds(System.DateTime time, double milliseconds) { throw null; }
        public virtual System.DateTime AddMinutes(System.DateTime time, int minutes) { throw null; }
        public abstract System.DateTime AddMonths(System.DateTime time, int months);
        public virtual System.DateTime AddSeconds(System.DateTime time, int seconds) { throw null; }
        public virtual System.DateTime AddWeeks(System.DateTime time, int weeks) { throw null; }
        public abstract System.DateTime AddYears(System.DateTime time, int years);
        public virtual object Clone() { throw null; }
        public abstract int GetDayOfMonth(System.DateTime time);
        public abstract System.DayOfWeek GetDayOfWeek(System.DateTime time);
        public abstract int GetDayOfYear(System.DateTime time);
        public virtual int GetDaysInMonth(int year, int month) { throw null; }
        public abstract int GetDaysInMonth(int year, int month, int era);
        public virtual int GetDaysInYear(int year) { throw null; }
        public abstract int GetDaysInYear(int year, int era);
        public abstract int GetEra(System.DateTime time);
        public virtual int GetHour(System.DateTime time) { throw null; }
        public virtual int GetLeapMonth(int year) { throw null; }
        public virtual int GetLeapMonth(int year, int era) { throw null; }
        public virtual double GetMilliseconds(System.DateTime time) { throw null; }
        public virtual int GetMinute(System.DateTime time) { throw null; }
        public abstract int GetMonth(System.DateTime time);
        public virtual int GetMonthsInYear(int year) { throw null; }
        public abstract int GetMonthsInYear(int year, int era);
        public virtual int GetSecond(System.DateTime time) { throw null; }
        public virtual int GetWeekOfYear(System.DateTime time, System.Globalization.CalendarWeekRule rule, System.DayOfWeek firstDayOfWeek) { throw null; }
        public abstract int GetYear(System.DateTime time);
        public virtual bool IsLeapDay(int year, int month, int day) { throw null; }
        public abstract bool IsLeapDay(int year, int month, int day, int era);
        public virtual bool IsLeapMonth(int year, int month) { throw null; }
        public abstract bool IsLeapMonth(int year, int month, int era);
        public virtual bool IsLeapYear(int year) { throw null; }
        public abstract bool IsLeapYear(int year, int era);
        public static System.Globalization.Calendar ReadOnly(System.Globalization.Calendar calendar) { throw null; }
        public virtual System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond) { throw null; }
        public abstract System.DateTime ToDateTime(int year, int month, int day, int hour, int minute, int second, int millisecond, int era);
        public virtual int ToFourDigitYear(int year) { throw null; }
    }
    public enum CalendarWeekRule
    {
        FirstDay = 0,
        FirstFullWeek = 1,
        FirstFourDayWeek = 2,
    }
    public static partial class CharUnicodeInfo
    {
        public static int GetDecimalDigitValue(char ch) { throw null; }
        public static int GetDecimalDigitValue(string s, int index) { throw null; }
        public static int GetDigitValue(char ch) { throw null; }
        public static int GetDigitValue(string s, int index) { throw null; }
        public static double GetNumericValue(char ch) { throw null; }
        public static double GetNumericValue(string s, int index) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(char ch) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(int codePoint) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(string s, int index) { throw null; }
    }
    public sealed partial class CompareInfo : System.Runtime.Serialization.IDeserializationCallback
    {
        internal CompareInfo() { }
        public int LCID { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Globalization.SortVersion Version { get { throw null; } }
        public int Compare(System.ReadOnlySpan<char> string1, System.ReadOnlySpan<char> string2, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2) { throw null; }
        public int Compare(string? string1, int offset1, int length1, string? string2, int offset2, int length2, System.Globalization.CompareOptions options) { throw null; }
        public int Compare(string? string1, int offset1, string? string2, int offset2) { throw null; }
        public int Compare(string? string1, int offset1, string? string2, int offset2, System.Globalization.CompareOptions options) { throw null; }
        public int Compare(string? string1, string? string2) { throw null; }
        public int Compare(string? string1, string? string2, System.Globalization.CompareOptions options) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture, System.Reflection.Assembly assembly) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(string name) { throw null; }
        public static System.Globalization.CompareInfo GetCompareInfo(string name, System.Reflection.Assembly assembly) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.ReadOnlySpan<char> source, System.Globalization.CompareOptions options) { throw null; }
        public int GetHashCode(string source, System.Globalization.CompareOptions options) { throw null; }
        public int GetSortKey(System.ReadOnlySpan<char> source, System.Span<byte> destination, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public System.Globalization.SortKey GetSortKey(string source) { throw null; }
        public System.Globalization.SortKey GetSortKey(string source, System.Globalization.CompareOptions options) { throw null; }
        public int GetSortKeyLength(System.ReadOnlySpan<char> source, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int IndexOf(System.ReadOnlySpan<char> source, System.ReadOnlySpan<char> value, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int IndexOf(System.ReadOnlySpan<char> source, System.Text.Rune value, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int IndexOf(string source, char value) { throw null; }
        public int IndexOf(string source, char value, System.Globalization.CompareOptions options) { throw null; }
        public int IndexOf(string source, char value, int startIndex) { throw null; }
        public int IndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public int IndexOf(string source, char value, int startIndex, int count) { throw null; }
        public int IndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public int IndexOf(string source, string value) { throw null; }
        public int IndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public int IndexOf(string source, string value, int startIndex) { throw null; }
        public int IndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public int IndexOf(string source, string value, int startIndex, int count) { throw null; }
        public int IndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public bool IsPrefix(System.ReadOnlySpan<char> source, System.ReadOnlySpan<char> prefix, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public bool IsPrefix(string source, string prefix) { throw null; }
        public bool IsPrefix(string source, string prefix, System.Globalization.CompareOptions options) { throw null; }
        public static bool IsSortable(char ch) { throw null; }
        public static bool IsSortable(System.ReadOnlySpan<char> text) { throw null; }
        public static bool IsSortable(string text) { throw null; }
        public static bool IsSortable(System.Text.Rune value) { throw null; }
        public bool IsSuffix(System.ReadOnlySpan<char> source, System.ReadOnlySpan<char> suffix, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public bool IsSuffix(string source, string suffix) { throw null; }
        public bool IsSuffix(string source, string suffix, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(System.ReadOnlySpan<char> source, System.ReadOnlySpan<char> value, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int LastIndexOf(System.ReadOnlySpan<char> source, System.Text.Rune value, System.Globalization.CompareOptions options = System.Globalization.CompareOptions.None) { throw null; }
        public int LastIndexOf(string source, char value) { throw null; }
        public int LastIndexOf(string source, char value, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(string source, char value, int startIndex) { throw null; }
        public int LastIndexOf(string source, char value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(string source, char value, int startIndex, int count) { throw null; }
        public int LastIndexOf(string source, char value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(string source, string value) { throw null; }
        public int LastIndexOf(string source, string value, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(string source, string value, int startIndex) { throw null; }
        public int LastIndexOf(string source, string value, int startIndex, System.Globalization.CompareOptions options) { throw null; }
        public int LastIndexOf(string source, string value, int startIndex, int count) { throw null; }
        public int LastIndexOf(string source, string value, int startIndex, int count, System.Globalization.CompareOptions options) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum CompareOptions
    {
        None = 0,
        IgnoreCase = 1,
        IgnoreNonSpace = 2,
        IgnoreSymbols = 4,
        IgnoreKanaType = 8,
        IgnoreWidth = 16,
        OrdinalIgnoreCase = 268435456,
        StringSort = 536870912,
        Ordinal = 1073741824,
    }
    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public CultureInfo(int culture) { }
        public CultureInfo(int culture, bool useUserOverride) { }
        public CultureInfo(string name) { }
        public CultureInfo(string name, bool useUserOverride) { }
        public virtual System.Globalization.Calendar Calendar { get { throw null; } }
        public virtual System.Globalization.CompareInfo CompareInfo { get { throw null; } }
        public System.Globalization.CultureTypes CultureTypes { get { throw null; } }
        public static System.Globalization.CultureInfo CurrentCulture { get { throw null; } set { } }
        public static System.Globalization.CultureInfo CurrentUICulture { get { throw null; } set { } }
        public virtual System.Globalization.DateTimeFormatInfo DateTimeFormat { get { throw null; } set { } }
        public static System.Globalization.CultureInfo? DefaultThreadCurrentCulture { get { throw null; } set { } }
        public static System.Globalization.CultureInfo? DefaultThreadCurrentUICulture { get { throw null; } set { } }
        public virtual string DisplayName { get { throw null; } }
        public virtual string EnglishName { get { throw null; } }
        public string IetfLanguageTag { get { throw null; } }
        public static System.Globalization.CultureInfo InstalledUICulture { get { throw null; } }
        public static System.Globalization.CultureInfo InvariantCulture { get { throw null; } }
        public virtual bool IsNeutralCulture { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public virtual int KeyboardLayoutId { get { throw null; } }
        public virtual int LCID { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string NativeName { get { throw null; } }
        public virtual System.Globalization.NumberFormatInfo NumberFormat { get { throw null; } set { } }
        public virtual System.Globalization.Calendar[] OptionalCalendars { get { throw null; } }
        public virtual System.Globalization.CultureInfo Parent { get { throw null; } }
        public virtual System.Globalization.TextInfo TextInfo { get { throw null; } }
        public virtual string ThreeLetterISOLanguageName { get { throw null; } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw null; } }
        public virtual string TwoLetterISOLanguageName { get { throw null; } }
        public bool UseUserOverride { get { throw null; } }
        public void ClearCachedData() { }
        public virtual object Clone() { throw null; }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public System.Globalization.CultureInfo GetConsoleFallbackUICulture() { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(int culture) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(string name) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(string name, bool predefinedOnly) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfo(string name, string altName) { throw null; }
        public static System.Globalization.CultureInfo GetCultureInfoByIetfLanguageTag(string name) { throw null; }
        public static System.Globalization.CultureInfo[] GetCultures(System.Globalization.CultureTypes types) { throw null; }
        public virtual object? GetFormat(System.Type? formatType) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Globalization.CultureInfo ReadOnly(System.Globalization.CultureInfo ci) { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class CultureNotFoundException : System.ArgumentException
    {
        public CultureNotFoundException() { }
        protected CultureNotFoundException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CultureNotFoundException(string? message) { }
        public CultureNotFoundException(string? message, System.Exception? innerException) { }
        public CultureNotFoundException(string? message, int invalidCultureId, System.Exception? innerException) { }
        public CultureNotFoundException(string? paramName, int invalidCultureId, string? message) { }
        public CultureNotFoundException(string? paramName, string? message) { }
        public CultureNotFoundException(string? message, string? invalidCultureName, System.Exception? innerException) { }
        public CultureNotFoundException(string? paramName, string? invalidCultureName, string? message) { }
        public virtual int? InvalidCultureId { get { throw null; } }
        public virtual string? InvalidCultureName { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class DateTimeFormatInfo : System.ICloneable, System.IFormatProvider
    {
        public DateTimeFormatInfo() { }
        public string[] AbbreviatedDayNames { get { throw null; } set { } }
        public string[] AbbreviatedMonthGenitiveNames { get { throw null; } set { } }
        public string[] AbbreviatedMonthNames { get { throw null; } set { } }
        public string AMDesignator { get { throw null; } set { } }
        public System.Globalization.Calendar Calendar { get { throw null; } set { } }
        public System.Globalization.CalendarWeekRule CalendarWeekRule { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo CurrentInfo { get { throw null; } }
        public string DateSeparator { get { throw null; } set { } }
        public string[] DayNames { get { throw null; } set { } }
        public System.DayOfWeek FirstDayOfWeek { get { throw null; } set { } }
        public string FullDateTimePattern { get { throw null; } set { } }
        public static System.Globalization.DateTimeFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string LongDatePattern { get { throw null; } set { } }
        public string LongTimePattern { get { throw null; } set { } }
        public string MonthDayPattern { get { throw null; } set { } }
        public string[] MonthGenitiveNames { get { throw null; } set { } }
        public string[] MonthNames { get { throw null; } set { } }
        public string NativeCalendarName { get { throw null; } }
        public string PMDesignator { get { throw null; } set { } }
        public string RFC1123Pattern { get { throw null; } }
        public string ShortDatePattern { get { throw null; } set { } }
        public string[] ShortestDayNames { get { throw null; } set { } }
        public string ShortTimePattern { get { throw null; } set { } }
        public string SortableDateTimePattern { get { throw null; } }
        public string TimeSeparator { get { throw null; } set { } }
        public string UniversalSortableDateTimePattern { get { throw null; } }
        public string YearMonthPattern { get { throw null; } set { } }
        public object Clone() { throw null; }
        public string GetAbbreviatedDayName(System.DayOfWeek dayofweek) { throw null; }
        public string GetAbbreviatedEraName(int era) { throw null; }
        public string GetAbbreviatedMonthName(int month) { throw null; }
        public string[] GetAllDateTimePatterns() { throw null; }
        public string[] GetAllDateTimePatterns(char format) { throw null; }
        public string GetDayName(System.DayOfWeek dayofweek) { throw null; }
        public int GetEra(string eraName) { throw null; }
        public string GetEraName(int era) { throw null; }
        public object? GetFormat(System.Type? formatType) { throw null; }
        public static System.Globalization.DateTimeFormatInfo GetInstance(System.IFormatProvider? provider) { throw null; }
        public string GetMonthName(int month) { throw null; }
        public string GetShortestDayName(System.DayOfWeek dayOfWeek) { throw null; }
        public static System.Globalization.DateTimeFormatInfo ReadOnly(System.Globalization.DateTimeFormatInfo dtfi) { throw null; }
        public void SetAllDateTimePatterns(string[] patterns, char format) { }
    }
    public sealed partial class NumberFormatInfo : System.ICloneable, System.IFormatProvider
    {
        public NumberFormatInfo() { }
        public int CurrencyDecimalDigits { get { throw null; } set { } }
        public string CurrencyDecimalSeparator { get { throw null; } set { } }
        public string CurrencyGroupSeparator { get { throw null; } set { } }
        public int[] CurrencyGroupSizes { get { throw null; } set { } }
        public int CurrencyNegativePattern { get { throw null; } set { } }
        public int CurrencyPositivePattern { get { throw null; } set { } }
        public string CurrencySymbol { get { throw null; } set { } }
        public static System.Globalization.NumberFormatInfo CurrentInfo { get { throw null; } }
        public System.Globalization.DigitShapes DigitSubstitution { get { throw null; } set { } }
        public static System.Globalization.NumberFormatInfo InvariantInfo { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public string NaNSymbol { get { throw null; } set { } }
        public string[] NativeDigits { get { throw null; } set { } }
        public string NegativeInfinitySymbol { get { throw null; } set { } }
        public string NegativeSign { get { throw null; } set { } }
        public int NumberDecimalDigits { get { throw null; } set { } }
        public string NumberDecimalSeparator { get { throw null; } set { } }
        public string NumberGroupSeparator { get { throw null; } set { } }
        public int[] NumberGroupSizes { get { throw null; } set { } }
        public int NumberNegativePattern { get { throw null; } set { } }
        public int PercentDecimalDigits { get { throw null; } set { } }
        public string PercentDecimalSeparator { get { throw null; } set { } }
        public string PercentGroupSeparator { get { throw null; } set { } }
        public int[] PercentGroupSizes { get { throw null; } set { } }
        public int PercentNegativePattern { get { throw null; } set { } }
        public int PercentPositivePattern { get { throw null; } set { } }
        public string PercentSymbol { get { throw null; } set { } }
        public string PerMilleSymbol { get { throw null; } set { } }
        public string PositiveInfinitySymbol { get { throw null; } set { } }
        public string PositiveSign { get { throw null; } set { } }
        public object Clone() { throw null; }
        public object? GetFormat(System.Type? formatType) { throw null; }
        public static System.Globalization.NumberFormatInfo GetInstance(System.IFormatProvider? formatProvider) { throw null; }
        public static System.Globalization.NumberFormatInfo ReadOnly(System.Globalization.NumberFormatInfo nfi) { throw null; }
    }
    public partial class RegionInfo
    {
        public RegionInfo(int culture) { }
        public RegionInfo(string name) { }
        public virtual string CurrencyEnglishName { get { throw null; } }
        public virtual string CurrencyNativeName { get { throw null; } }
        public virtual string CurrencySymbol { get { throw null; } }
        public static System.Globalization.RegionInfo CurrentRegion { get { throw null; } }
        public virtual string DisplayName { get { throw null; } }
        public virtual string EnglishName { get { throw null; } }
        public virtual int GeoId { get { throw null; } }
        public virtual bool IsMetric { get { throw null; } }
        public virtual string ISOCurrencySymbol { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual string NativeName { get { throw null; } }
        public virtual string ThreeLetterISORegionName { get { throw null; } }
        public virtual string ThreeLetterWindowsRegionName { get { throw null; } }
        public virtual string TwoLetterISORegionName { get { throw null; } }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public partial class StringInfo
    {
        public StringInfo() { }
        public StringInfo(string value) { }
        public int LengthInTextElements { get { throw null; } }
        public string String { get { throw null; } set { } }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public static string GetNextTextElement(string str) { throw null; }
        public static string GetNextTextElement(string str, int index) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str) { throw null; }
        public static System.Globalization.TextElementEnumerator GetTextElementEnumerator(string str, int index) { throw null; }
        public static int[] ParseCombiningCharacters(string str) { throw null; }
        public string SubstringByTextElements(int startingTextElement) { throw null; }
        public string SubstringByTextElements(int startingTextElement, int lengthInTextElements) { throw null; }
    }
    public partial class TextElementEnumerator : System.Collections.IEnumerator
    {
        internal TextElementEnumerator() { }
        public object Current { get { throw null; } }
        public int ElementIndex { get { throw null; } }
        public string GetTextElement() { throw null; }
        public bool MoveNext() { throw null; }
        public void Reset() { }
    }
    public sealed partial class TextInfo : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback
    {
        internal TextInfo() { }
        public int ANSICodePage { get { throw null; } }
        public string CultureName { get { throw null; } }
        public int EBCDICCodePage { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public bool IsRightToLeft { get { throw null; } }
        public int LCID { get { throw null; } }
        public string ListSeparator { get { throw null; } set { } }
        public int MacCodePage { get { throw null; } }
        public int OEMCodePage { get { throw null; } }
        public object Clone() { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static System.Globalization.TextInfo ReadOnly(System.Globalization.TextInfo textInfo) { throw null; }
        void System.Runtime.Serialization.IDeserializationCallback.OnDeserialization(object sender) { }
        public char ToLower(char c) { throw null; }
        public string ToLower(string str) { throw null; }
        public override string ToString() { throw null; }
        public string ToTitleCase(string str) { throw null; }
        public char ToUpper(char c) { throw null; }
        public string ToUpper(string str) { throw null; }
    }
    public enum UnicodeCategory
    {
        UppercaseLetter = 0,
        LowercaseLetter = 1,
        TitlecaseLetter = 2,
        ModifierLetter = 3,
        OtherLetter = 4,
        NonSpacingMark = 5,
        SpacingCombiningMark = 6,
        EnclosingMark = 7,
        DecimalDigitNumber = 8,
        LetterNumber = 9,
        OtherNumber = 10,
        SpaceSeparator = 11,
        LineSeparator = 12,
        ParagraphSeparator = 13,
        Control = 14,
        Format = 15,
        Surrogate = 16,
        PrivateUse = 17,
        ConnectorPunctuation = 18,
        DashPunctuation = 19,
        OpenPunctuation = 20,
        ClosePunctuation = 21,
        InitialQuotePunctuation = 22,
        FinalQuotePunctuation = 23,
        OtherPunctuation = 24,
        MathSymbol = 25,
        CurrencySymbol = 26,
        ModifierSymbol = 27,
        OtherSymbol = 28,
        OtherNotAssigned = 29,
    }
}
