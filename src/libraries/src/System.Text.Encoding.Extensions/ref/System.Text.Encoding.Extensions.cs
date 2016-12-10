// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Text
{
    public partial class ASCIIEncoding : System.Text.Encoding
    {
        public ASCIIEncoding() { }
        public override bool IsSingleByte { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string chars) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override string GetString(byte[] bytes, int byteIndex, int byteCount) { throw null; }
    }
    public partial class UnicodeEncoding : System.Text.Encoding
    {
        public const int CharSize = 2;
        public UnicodeEncoding() { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark) { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    public sealed partial class UTF32Encoding : System.Text.Encoding
    {
        public UTF32Encoding() { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark) { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    public partial class UTF7Encoding : System.Text.Encoding
    {
        public UTF7Encoding() { }
        public UTF7Encoding(bool allowOptionals) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string s) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
    public partial class UTF8Encoding : System.Text.Encoding
    {
        public UTF8Encoding() { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier) { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetByteCount(char* chars, int count) { throw null; }
        public override int GetByteCount(char[] chars, int index, int count) { throw null; }
        public override int GetByteCount(string chars) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetCharCount(byte* bytes, int count) { throw null; }
        public override int GetCharCount(byte[] bytes, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { throw null; }
        public override System.Text.Decoder GetDecoder() { throw null; }
        public override System.Text.Encoder GetEncoder() { throw null; }
        public override int GetHashCode() { throw null; }
        public override int GetMaxByteCount(int charCount) { throw null; }
        public override int GetMaxCharCount(int byteCount) { throw null; }
        public override byte[] GetPreamble() { throw null; }
        public override string GetString(byte[] bytes, int index, int count) { throw null; }
    }
}
