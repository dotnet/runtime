// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Globalization
{
    public sealed partial class IdnMapping
    {
        public IdnMapping() { }
        public bool AllowUnassigned { get { return default(bool); } set { } }
        public bool UseStd3AsciiRules { get { return default(bool); } set { } }
        public override bool Equals(object obj) { return default(bool); }
        public string GetAscii(string unicode) { return default(string); }
        public string GetAscii(string unicode, int index) { return default(string); }
        public string GetAscii(string unicode, int index, int count) { return default(string); }
        public override int GetHashCode() { return default(int); }
        public string GetUnicode(string ascii) { return default(string); }
        public string GetUnicode(string ascii, int index) { return default(string); }
        public string GetUnicode(string ascii, int index, int count) { return default(string); }
    }
}
namespace System.Text
{
    public enum NormalizationForm
    {
        FormC = 1,
        FormD = 2,
        FormKC = 5,
        FormKD = 6,
    }
}
