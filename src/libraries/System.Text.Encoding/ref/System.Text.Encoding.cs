// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text
{
    public abstract partial class Decoder
    {
        protected Decoder() { }
        public System.Text.DecoderFallback? Fallback { get { throw null; } set { } }
        public System.Text.DecoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual void Convert(byte* bytes, int byteCount, char* chars, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        public virtual void Convert(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, int charCount, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        public virtual void Convert(System.ReadOnlySpan<byte> bytes, System.Span<char> chars, bool flush, out int bytesUsed, out int charsUsed, out bool completed) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetCharCount(byte* bytes, int count, bool flush) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual int GetCharCount(byte[] bytes, int index, int count, bool flush) { throw null; }
        public virtual int GetCharCount(System.ReadOnlySpan<byte> bytes, bool flush) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount, bool flush) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex, bool flush) { throw null; }
        public virtual int GetChars(System.ReadOnlySpan<byte> bytes, System.Span<char> chars, bool flush) { throw null; }
        public virtual void Reset() { }
    }
    public sealed partial class DecoderExceptionFallback : System.Text.DecoderFallback
    {
        public DecoderExceptionFallback() { }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.DecoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class DecoderFallback
    {
        protected DecoderFallback() { }
        public static System.Text.DecoderFallback ExceptionFallback { get { throw null; } }
        public abstract int MaxCharCount { get; }
        public static System.Text.DecoderFallback ReplacementFallback { get { throw null; } }
        public abstract System.Text.DecoderFallbackBuffer CreateFallbackBuffer();
    }
    public abstract partial class DecoderFallbackBuffer
    {
        protected DecoderFallbackBuffer() { }
        public abstract int Remaining { get; }
        public abstract bool Fallback(byte[] bytesUnknown, int index);
        public abstract char GetNextChar();
        public abstract bool MovePrevious();
        public virtual void Reset() { }
    }
    public sealed partial class DecoderFallbackException : System.ArgumentException
    {
        public DecoderFallbackException() { }
        public DecoderFallbackException(string? message) { }
        public DecoderFallbackException(string? message, byte[]? bytesUnknown, int index) { }
        public DecoderFallbackException(string? message, System.Exception? innerException) { }
        public byte[]? BytesUnknown { get { throw null; } }
        public int Index { get { throw null; } }
    }
    public sealed partial class DecoderReplacementFallback : System.Text.DecoderFallback
    {
        public DecoderReplacementFallback() { }
        public DecoderReplacementFallback(string replacement) { }
        public string DefaultString { get { throw null; } }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.DecoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class Encoder
    {
        protected Encoder() { }
        public System.Text.EncoderFallback? Fallback { get { throw null; } set { } }
        public System.Text.EncoderFallbackBuffer FallbackBuffer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual void Convert(char* chars, int charCount, byte* bytes, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        public virtual void Convert(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, int byteCount, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        public virtual void Convert(System.ReadOnlySpan<char> chars, System.Span<byte> bytes, bool flush, out int charsUsed, out int bytesUsed, out bool completed) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetByteCount(char* chars, int count, bool flush) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count, bool flush);
        public virtual int GetByteCount(System.ReadOnlySpan<char> chars, bool flush) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount, bool flush) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex, bool flush);
        public virtual int GetBytes(System.ReadOnlySpan<char> chars, System.Span<byte> bytes, bool flush) { throw null; }
        public virtual void Reset() { }
    }
    public sealed partial class EncoderExceptionFallback : System.Text.EncoderFallback
    {
        public EncoderExceptionFallback() { }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.EncoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class EncoderFallback
    {
        protected EncoderFallback() { }
        public static System.Text.EncoderFallback ExceptionFallback { get { throw null; } }
        public abstract int MaxCharCount { get; }
        public static System.Text.EncoderFallback ReplacementFallback { get { throw null; } }
        public abstract System.Text.EncoderFallbackBuffer CreateFallbackBuffer();
    }
    public abstract partial class EncoderFallbackBuffer
    {
        protected EncoderFallbackBuffer() { }
        public abstract int Remaining { get; }
        public abstract bool Fallback(char charUnknownHigh, char charUnknownLow, int index);
        public abstract bool Fallback(char charUnknown, int index);
        public abstract char GetNextChar();
        public abstract bool MovePrevious();
        public virtual void Reset() { }
    }
    public sealed partial class EncoderFallbackException : System.ArgumentException
    {
        public EncoderFallbackException() { }
        public EncoderFallbackException(string? message) { }
        public EncoderFallbackException(string? message, System.Exception? innerException) { }
        public char CharUnknown { get { throw null; } }
        public char CharUnknownHigh { get { throw null; } }
        public char CharUnknownLow { get { throw null; } }
        public int Index { get { throw null; } }
        public bool IsUnknownSurrogate() { throw null; }
    }
    public sealed partial class EncoderReplacementFallback : System.Text.EncoderFallback
    {
        public EncoderReplacementFallback() { }
        public EncoderReplacementFallback(string replacement) { }
        public string DefaultString { get { throw null; } }
        public override int MaxCharCount { get { throw null; } }
        public override System.Text.EncoderFallbackBuffer CreateFallbackBuffer() { throw null; }
        public override bool Equals(object? value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class Encoding : System.ICloneable
    {
        protected Encoding() { }
        protected Encoding(int codePage) { }
        protected Encoding(int codePage, System.Text.EncoderFallback? encoderFallback, System.Text.DecoderFallback? decoderFallback) { }
        public static System.Text.Encoding ASCII { get { throw null; } }
        public static System.Text.Encoding BigEndianUnicode { get { throw null; } }
        public virtual string BodyName { get { throw null; } }
        public virtual int CodePage { get { throw null; } }
        public System.Text.DecoderFallback DecoderFallback { get { throw null; } set { } }
        public static System.Text.Encoding Default { get { throw null; } }
        public System.Text.EncoderFallback EncoderFallback { get { throw null; } set { } }
        public virtual string EncodingName { get { throw null; } }
        public virtual string HeaderName { get { throw null; } }
        public virtual bool IsBrowserDisplay { get { throw null; } }
        public virtual bool IsBrowserSave { get { throw null; } }
        public virtual bool IsMailNewsDisplay { get { throw null; } }
        public virtual bool IsMailNewsSave { get { throw null; } }
        public bool IsReadOnly { get { throw null; } }
        public virtual bool IsSingleByte { get { throw null; } }
        public static System.Text.Encoding Latin1 { get { throw null; } }
        public virtual System.ReadOnlySpan<byte> Preamble { get { throw null; } }
        public static System.Text.Encoding Unicode { get { throw null; } }
        public static System.Text.Encoding UTF32 { get { throw null; } }
        public static System.Text.Encoding UTF7 { get { throw null; } }
        public static System.Text.Encoding UTF8 { get { throw null; } }
        public virtual string WebName { get { throw null; } }
        public virtual int WindowsCodePage { get { throw null; } }
        public virtual object Clone() { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes) { throw null; }
        public static byte[] Convert(System.Text.Encoding srcEncoding, System.Text.Encoding dstEncoding, byte[] bytes, int index, int count) { throw null; }
        public static System.IO.Stream CreateTranscodingStream(System.IO.Stream innerStream, System.Text.Encoding innerStreamEncoding, System.Text.Encoding outerStreamEncoding, bool leaveOpen = false) { throw null; }
        public override bool Equals(object? value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetByteCount(char* chars, int count) { throw null; }
        public virtual int GetByteCount(char[] chars) { throw null; }
        public abstract int GetByteCount(char[] chars, int index, int count);
        public virtual int GetByteCount(System.ReadOnlySpan<char> chars) { throw null; }
        public virtual int GetByteCount(string s) { throw null; }
        public int GetByteCount(string s, int index, int count) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetBytes(char* chars, int charCount, byte* bytes, int byteCount) { throw null; }
        public virtual byte[] GetBytes(char[] chars) { throw null; }
        public virtual byte[] GetBytes(char[] chars, int index, int count) { throw null; }
        public abstract int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex);
        public virtual int GetBytes(System.ReadOnlySpan<char> chars, System.Span<byte> bytes) { throw null; }
        public virtual byte[] GetBytes(string s) { throw null; }
        public byte[] GetBytes(string s, int index, int count) { throw null; }
        public virtual int GetBytes(string s, int charIndex, int charCount, byte[] bytes, int byteIndex) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetCharCount(byte* bytes, int count) { throw null; }
        public virtual int GetCharCount(byte[] bytes) { throw null; }
        public abstract int GetCharCount(byte[] bytes, int index, int count);
        public virtual int GetCharCount(System.ReadOnlySpan<byte> bytes) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe virtual int GetChars(byte* bytes, int byteCount, char* chars, int charCount) { throw null; }
        public virtual char[] GetChars(byte[] bytes) { throw null; }
        public virtual char[] GetChars(byte[] bytes, int index, int count) { throw null; }
        public abstract int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex);
        public virtual int GetChars(System.ReadOnlySpan<byte> bytes, System.Span<char> chars) { throw null; }
        public virtual System.Text.Decoder GetDecoder() { throw null; }
        public virtual System.Text.Encoder GetEncoder() { throw null; }
        public static System.Text.Encoding GetEncoding(int codepage) { throw null; }
        public static System.Text.Encoding GetEncoding(int codepage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public static System.Text.Encoding GetEncoding(string name) { throw null; }
        public static System.Text.Encoding GetEncoding(string name, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public static System.Text.EncodingInfo[] GetEncodings() { throw null; }
        public override int GetHashCode() { throw null; }
        public abstract int GetMaxByteCount(int charCount);
        public abstract int GetMaxCharCount(int byteCount);
        public virtual byte[] GetPreamble() { throw null; }
        [System.CLSCompliantAttribute(false)]
        public unsafe string GetString(byte* bytes, int byteCount) { throw null; }
        public virtual string GetString(byte[] bytes) { throw null; }
        public virtual string GetString(byte[] bytes, int index, int count) { throw null; }
        public string GetString(System.ReadOnlySpan<byte> bytes) { throw null; }
        public bool IsAlwaysNormalized() { throw null; }
        public virtual bool IsAlwaysNormalized(System.Text.NormalizationForm form) { throw null; }
        public static void RegisterProvider(System.Text.EncodingProvider provider) { }
    }
    public abstract partial class EncodingProvider
    {
        public EncodingProvider() { }
        public abstract System.Text.Encoding? GetEncoding(int codepage);
        public virtual System.Text.Encoding? GetEncoding(int codepage, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public abstract System.Text.Encoding? GetEncoding(string name);
        public virtual System.Text.Encoding? GetEncoding(string name, System.Text.EncoderFallback encoderFallback, System.Text.DecoderFallback decoderFallback) { throw null; }
        public virtual System.Collections.Generic.IEnumerable<System.Text.EncodingInfo> GetEncodings() { throw null; }
    }
}
