// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Text
{
    public readonly partial struct Rune : System.IComparable, System.IComparable<System.Text.Rune>, System.IEquatable<System.Text.Rune>
    {
        private readonly int _dummyPrimitive;
        public Rune(char ch) { throw null; }
        public Rune(char highSurrogate, char lowSurrogate) { throw null; }
        public Rune(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public Rune(uint value) { throw null; }
        public bool IsAscii { get { throw null; } }
        public bool IsBmp { get { throw null; } }
        public int Plane { get { throw null; } }
        public static System.Text.Rune ReplacementChar { get { throw null; } }
        public int Utf16SequenceLength { get { throw null; } }
        public int Utf8SequenceLength { get { throw null; } }
        public int Value { get { throw null; } }
        public int CompareTo(System.Text.Rune other) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromUtf16(System.ReadOnlySpan<char> source, out System.Text.Rune result, out int charsConsumed) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<byte> source, out System.Text.Rune result, out int bytesConsumed) { throw null; }
        public static System.Buffers.OperationStatus DecodeLastFromUtf16(System.ReadOnlySpan<char> source, out System.Text.Rune result, out int charsConsumed) { throw null; }
        public static System.Buffers.OperationStatus DecodeLastFromUtf8(System.ReadOnlySpan<byte> source, out System.Text.Rune value, out int bytesConsumed) { throw null; }
        public int EncodeToUtf16(System.Span<char> destination) { throw null; }
        public int EncodeToUtf8(System.Span<byte> destination) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Text.Rune other) { throw null; }
        public override int GetHashCode() { throw null; }
        public static double GetNumericValue(System.Text.Rune value) { throw null; }
        public static System.Text.Rune GetRuneAt(string input, int index) { throw null; }
        public static System.Globalization.UnicodeCategory GetUnicodeCategory(System.Text.Rune value) { throw null; }
        public static bool IsControl(System.Text.Rune value) { throw null; }
        public static bool IsDigit(System.Text.Rune value) { throw null; }
        public static bool IsLetter(System.Text.Rune value) { throw null; }
        public static bool IsLetterOrDigit(System.Text.Rune value) { throw null; }
        public static bool IsLower(System.Text.Rune value) { throw null; }
        public static bool IsNumber(System.Text.Rune value) { throw null; }
        public static bool IsPunctuation(System.Text.Rune value) { throw null; }
        public static bool IsSeparator(System.Text.Rune value) { throw null; }
        public static bool IsSymbol(System.Text.Rune value) { throw null; }
        public static bool IsUpper(System.Text.Rune value) { throw null; }
        public static bool IsValid(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool IsValid(uint value) { throw null; }
        public static bool IsWhiteSpace(System.Text.Rune value) { throw null; }
        public static bool operator ==(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static explicit operator System.Text.Rune(char ch) { throw null; }
        public static explicit operator System.Text.Rune(int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Text.Rune(uint value) { throw null; }
        public static bool operator >(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator >=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator !=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator <(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator <=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static System.Text.Rune ToLower(System.Text.Rune value, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Text.Rune ToLowerInvariant(System.Text.Rune value) { throw null; }
        public override string ToString() { throw null; }
        public static System.Text.Rune ToUpper(System.Text.Rune value, System.Globalization.CultureInfo culture) { throw null; }
        public static System.Text.Rune ToUpperInvariant(System.Text.Rune value) { throw null; }
        public static bool TryCreate(char highSurrogate, char lowSurrogate, out System.Text.Rune result) { throw null; }
        public static bool TryCreate(char ch, out System.Text.Rune result) { throw null; }
        public static bool TryCreate(int value, out System.Text.Rune result) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryCreate(uint value, out System.Text.Rune result) { throw null; }
        public bool TryEncodeToUtf16(System.Span<char> destination, out int charsWritten) { throw null; }
        public bool TryEncodeToUtf8(System.Span<byte> destination, out int bytesWritten) { throw null; }
        public static bool TryGetRuneAt(string input, int index, out System.Text.Rune value) { throw null; }
        int IComparable.CompareTo(object? obj) { throw null; }
    }
}
