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
        public override bool IsSingleByte { get { return default(bool); } }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { return default(int); }
        public override int GetByteCount(char[] chars, int index, int count) { return default(int); }
        public override int GetByteCount(string chars) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { return default(int); }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetBytes(string chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { return default(int); }
        public override int GetCharCount(byte[] bytes, int index, int count) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { return default(int); }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return default(int); }
        public override System.Text.Decoder GetDecoder() { return default(System.Text.Decoder); }
        public override System.Text.Encoder GetEncoder() { return default(System.Text.Encoder); }
        public override int GetMaxByteCount(int charCount) { return default(int); }
        public override int GetMaxCharCount(int byteCount) { return default(int); }
        public override string GetString(byte[] bytes, int byteIndex, int byteCount) { return default(string); }
    }
    public partial class UnicodeEncoding : System.Text.Encoding
    {
        public UnicodeEncoding() { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark) { }
        public UnicodeEncoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { return default(bool); }
        public override int GetByteCount(char[] chars, int index, int count) { return default(int); }
        public override int GetByteCount(string s) { return default(int); }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetCharCount(byte[] bytes, int index, int count) { return default(int); }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return default(int); }
        public override System.Text.Decoder GetDecoder() { return default(System.Text.Decoder); }
        public override System.Text.Encoder GetEncoder() { return default(System.Text.Encoder); }
        public override int GetHashCode() { return default(int); }
        public override int GetMaxByteCount(int charCount) { return default(int); }
        public override int GetMaxCharCount(int byteCount) { return default(int); }
        public override byte[] GetPreamble() { return default(byte[]); }
        public override string GetString(byte[] bytes, int index, int count) { return default(string); }
    }
    public sealed partial class UTF32Encoding : System.Text.Encoding
    {
        public UTF32Encoding() { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark) { }
        public UTF32Encoding(bool bigEndian, bool byteOrderMark, bool throwOnInvalidCharacters) { }
        public override bool Equals(object value) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { return default(int); }
        public override int GetByteCount(char[] chars, int index, int count) { return default(int); }
        public override int GetByteCount(string s) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { return default(int); }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { return default(int); }
        public override int GetCharCount(byte[] bytes, int index, int count) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { return default(int); }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return default(int); }
        public override System.Text.Decoder GetDecoder() { return default(System.Text.Decoder); }
        public override System.Text.Encoder GetEncoder() { return default(System.Text.Encoder); }
        public override int GetHashCode() { return default(int); }
        public override int GetMaxByteCount(int charCount) { return default(int); }
        public override int GetMaxCharCount(int byteCount) { return default(int); }
        public override byte[] GetPreamble() { return default(byte[]); }
        public override string GetString(byte[] bytes, int index, int count) { return default(string); }
    }
    public partial class UTF7Encoding : System.Text.Encoding
    {
        public UTF7Encoding() { }
        public UTF7Encoding(bool allowOptionals) { }
        public override bool Equals(object value) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { return default(int); }
        public override int GetByteCount(char[] chars, int index, int count) { return default(int); }
        public override int GetByteCount(string s) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { return default(int); }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { return default(int); }
        public override int GetCharCount(byte[] bytes, int index, int count) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { return default(int); }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return default(int); }
        public override System.Text.Decoder GetDecoder() { return default(System.Text.Decoder); }
        public override System.Text.Encoder GetEncoder() { return default(System.Text.Encoder); }
        public override int GetHashCode() { return default(int); }
        public override int GetMaxByteCount(int charCount) { return default(int); }
        public override int GetMaxCharCount(int byteCount) { return default(int); }
        public override string GetString(byte[] bytes, int index, int count) { return default(string); }
    }
    public partial class UTF8Encoding : System.Text.Encoding
    {
        public UTF8Encoding() { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier) { }
        public UTF8Encoding(bool encoderShouldEmitUTF8Identifier, bool throwOnInvalidBytes) { }
        public override bool Equals(object value) { return default(bool); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetByteCount(char* chars, int count) { return default(int); }
        public override int GetByteCount(char[] chars, int index, int count) { return default(int); }
        public override int GetByteCount(string chars) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { return default(int); }
        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        public override int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetCharCount(byte* bytes, int count) { return default(int); }
        public override int GetCharCount(byte[] bytes, int index, int count) { return default(int); }
        [System.CLSCompliantAttribute(false)]
        [System.Security.SecurityCriticalAttribute]
        public unsafe override int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { return default(int); }
        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex) { return default(int); }
        public override System.Text.Decoder GetDecoder() { return default(System.Text.Decoder); }
        public override System.Text.Encoder GetEncoder() { return default(System.Text.Encoder); }
        public override int GetHashCode() { return default(int); }
        public override int GetMaxByteCount(int charCount) { return default(int); }
        public override int GetMaxCharCount(int byteCount) { return default(int); }
        public override byte[] GetPreamble() { return default(byte[]); }
        public override string GetString(byte[] bytes, int index, int count) { return default(string); }
    }
}
