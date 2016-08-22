namespace System.Globalization
{
    public partial class CompareInfo : System.Runtime.Serialization.IDeserializationCallback
    {
        public int LCID { get { throw new NotImplementedException(); } }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture) { throw new NotImplementedException(); }
        public static System.Globalization.CompareInfo GetCompareInfo(int culture, System.Reflection.Assembly assembly) { throw new NotImplementedException(); }
    }

    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public CultureInfo(int culture) { throw new NotImplementedException(); }
        public CultureInfo(int culture, bool useUserOverride) { throw new NotImplementedException(); }
        public virtual int LCID { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterISOLanguageName { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw new NotImplementedException(); } }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfo(int culture) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfoByIetfLanguageTag(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo[] GetCultures(System.Globalization.CultureTypes types) { throw new NotImplementedException(); }
    }

    public partial class CultureNotFoundException : System.ArgumentException, System.Runtime.Serialization.ISerializable
    {
        public CultureNotFoundException(string message, int invalidCultureId, System.Exception innerException) { throw new NotImplementedException(); }
        public CultureNotFoundException(string paramName, int invalidCultureId, string message) { throw new NotImplementedException(); }
        public virtual System.Nullable<int> InvalidCultureId { get { throw new NotImplementedException(); } }
    }

    /*public partial class DateTimeFormatInfo
    {
        Can't do partials here so implement the stub in the main class
        public String DateSeparator { set { throw null; } }
        public String TimeSeparator { set { throw null; } }
    }*/

    public sealed partial class NumberFormatInfo : System.ICloneable, System.IFormatProvider
    {
        [System.Runtime.InteropServices.ComVisibleAttribute(false)]
        public System.Globalization.DigitShapes DigitSubstitution { get { throw new NotImplementedException(); } set { throw new NotImplementedException(); } }
    }

    public partial class RegionInfo
    {
        public RegionInfo(int culture) { throw new NotImplementedException(); }
        public virtual string ThreeLetterISORegionName { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterWindowsRegionName { get { throw new NotImplementedException(); } }
    }

    public partial class SortKey
    {
        internal SortKey() { throw new NotImplementedException(); }
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