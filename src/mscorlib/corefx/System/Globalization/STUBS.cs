namespace System.Globalization
{
    public partial class CompareInfo : System.Runtime.Serialization.IDeserializationCallback
    {
        public int LCID { get { throw new NotImplementedException(); } }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture) { throw new NotImplementedException(); }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture, System.Reflection.Assembly assembly) { throw new NotImplementedException(); }
        public static System.Globalization.CompareInfo GetCompareInfo(string name, System.Reflection.Assembly assembly) { throw new NotImplementedException(); }
        public virtual System.Globalization.SortKey GetSortKey(string source) { throw new NotImplementedException(); }
        public virtual System.Globalization.SortKey GetSortKey(string source, System.Globalization.CompareOptions options) { throw new NotImplementedException(); }
        public virtual int IndexOf(string source, char value, int startIndex) { throw new NotImplementedException(); }
        public virtual int IndexOf(string source, string value, int startIndex) { throw new NotImplementedException(); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public static bool IsSortable(char ch) { throw new NotImplementedException(); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        [System.Security.SecuritySafeCriticalAttribute]
        public static bool IsSortable(string text) { throw new NotImplementedException(); }
        public virtual int LastIndexOf(string source, char value, int startIndex) { throw new NotImplementedException(); }
        public virtual int LastIndexOf(string source, string value, int startIndex) { throw new NotImplementedException(); }
    }

    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public CultureInfo(int culture) { throw new NotImplementedException(); }
        public CultureInfo(int culture, bool useUserOverride) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo InstalledUICulture { get { throw new NotImplementedException(); } }
        public virtual int LCID { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterISOLanguageName { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw new NotImplementedException(); } }
        public void ClearCachedData() { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfo(int culture) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfo(string name, string altName) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfoByIetfLanguageTag(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo[] GetCultures(System.Globalization.CultureTypes types) { throw new NotImplementedException(); }
    }

    public partial class CultureNotFoundException : System.ArgumentException, System.Runtime.Serialization.ISerializable
    {
        public CultureNotFoundException(string message, int invalidCultureId, System.Exception innerException) { throw new NotImplementedException(); }
        public CultureNotFoundException(string paramName, int invalidCultureId, string message) { throw new NotImplementedException(); }
        public virtual System.Nullable<int> InvalidCultureId { get { throw new NotImplementedException(); } }
    }

    public enum CultureTypes
    {
        AllCultures = 7,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        FrameworkCultures = 64,
        InstalledWin32Cultures = 4,
        NeutralCultures = 1,
        ReplacementCultures = 16,
        SpecificCultures = 2,
        UserCustomCulture = 8,
        [System.ObsoleteAttribute("This value has been deprecated.  Please use other values in CultureTypes.")]
        WindowsOnlyCultures = 32,
    }

    public sealed partial class DateTimeFormatInfo : System.ICloneable, System.IFormatProvider
    {
        // Can't do partial properties so add the setter for DateSeparator and TimeSeparator
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string NativeCalendarName { get { throw new NotImplementedException(); } }
        public string[] GetAllDateTimePatterns() { throw new NotImplementedException(); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string GetShortestDayName(System.DayOfWeek dayOfWeek) { throw new NotImplementedException(); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public void SetAllDateTimePatterns(string[] patterns, char format) { throw new NotImplementedException(); }
    }

    public enum DigitShapes
    {
        Context = 0,
        NativeNational = 2,
        None = 1,
    }

    public sealed partial class IdnMapping
    {
        public IdnMapping() { }
        public bool AllowUnassigned { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public bool UseStd3AsciiRules { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        public override bool Equals(object obj) { throw new NotImplementedException(); }
        public string GetAscii(string unicode) { throw new NotImplementedException(); }
        public string GetAscii(string unicode, int index) { throw new NotImplementedException(); }
        public string GetAscii(string unicode, int index, int count) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }
        public string GetUnicode(string ascii) { throw new NotImplementedException(); }
        public string GetUnicode(string ascii, int index) { throw new NotImplementedException(); }
        public string GetUnicode(string ascii, int index, int count) { throw new NotImplementedException(); }
    }

    public sealed partial class NumberFormatInfo : System.ICloneable, System.IFormatProvider
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Globalization.DigitShapes DigitSubstitution { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public string[] NativeDigits { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }

    public partial class RegionInfo
    {
        public RegionInfo(int culture) { throw new NotImplementedException(); }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string CurrencyEnglishName { get { throw new NotImplementedException(); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual string CurrencyNativeName { get { throw new NotImplementedException(); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public virtual int GeoId { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterISORegionName { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterWindowsRegionName { get { throw new NotImplementedException(); } }
    }

    public partial class SortKey
    {
        internal SortKey() { throw new NotImplementedException(); }
        public virtual byte[] KeyData { get { throw new NotImplementedException(); } }
        public virtual string OriginalString { get { throw new NotImplementedException(); } }
        public static int Compare(System.Globalization.SortKey sortkey1, System.Globalization.SortKey sortkey2) { throw new NotImplementedException(); }
        public override bool Equals(object value) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }
        public override string ToString() { throw new NotImplementedException(); }
    }

    public sealed partial class SortVersion : System.IEquatable<System.Globalization.SortVersion>
    {
        public SortVersion(int fullVersion, System.Guid sortId) { throw new NotImplementedException(); }
        public int FullVersion { get { throw new NotImplementedException(); } }
        public System.Guid SortId { get { throw new NotImplementedException(); } }
        public bool Equals(System.Globalization.SortVersion other) { throw new NotImplementedException(); }
        public override bool Equals(object obj) { throw new NotImplementedException(); }
        public override int GetHashCode() { throw new NotImplementedException(); }
        public static bool operator ==(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw new NotImplementedException(); }
        public static bool operator !=(System.Globalization.SortVersion left, System.Globalization.SortVersion right) { throw new NotImplementedException(); }
    }

    public partial class TextInfo : System.ICloneable, System.Runtime.Serialization.IDeserializationCallback 
    {
        public virtual int ANSICodePage { get { throw new NotImplementedException(); } }
        public virtual int EBCDICCodePage { get { throw new NotImplementedException(); } }
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public int LCID { get { throw new NotImplementedException(); } }
        public virtual int MacCodePage { get { throw new NotImplementedException(); } }
        public virtual int OEMCodePage { get { throw new NotImplementedException(); } }
        public string ToTitleCase(string str) { throw new NotImplementedException(); }
    }
}