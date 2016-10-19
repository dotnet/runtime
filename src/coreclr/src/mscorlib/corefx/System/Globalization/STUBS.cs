namespace System.Globalization
{
    public partial class CultureInfo : System.ICloneable, System.IFormatProvider
    {
        public static System.Globalization.CultureInfo InstalledUICulture { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterISOLanguageName { get { throw new NotImplementedException(); } }
        public virtual string ThreeLetterWindowsLanguageName { get { throw new NotImplementedException(); } }
        public void ClearCachedData() { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo CreateSpecificCulture(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfo(string name, string altName) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo GetCultureInfoByIetfLanguageTag(string name) { throw new NotImplementedException(); }
        public static System.Globalization.CultureInfo[] GetCultures(System.Globalization.CultureTypes types) { throw new NotImplementedException(); }
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
}