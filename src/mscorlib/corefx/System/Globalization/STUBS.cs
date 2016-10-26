namespace System.Globalization
{
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