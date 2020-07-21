// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public readonly partial struct Char8 : System.IComparable<System.Char8>, System.IEquatable<System.Char8>
    {
        private readonly int _dummyPrimitive;
        public int CompareTo(System.Char8 other) { throw null; }
        public bool Equals(System.Char8 other) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Char8 left, System.Char8 right) { throw null; }
        public static explicit operator System.Char8 (char value) { throw null; }
        public static explicit operator char (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte (System.Char8 value) { throw null; }
        public static explicit operator System.Char8 (short value) { throw null; }
        public static explicit operator System.Char8 (int value) { throw null; }
        public static explicit operator System.Char8 (long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8 (ulong value) { throw null; }
        public static bool operator >(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator >=(System.Char8 left, System.Char8 right) { throw null; }
        public static implicit operator System.Char8 (byte value) { throw null; }
        public static implicit operator byte (System.Char8 value) { throw null; }
        public static implicit operator short (System.Char8 value) { throw null; }
        public static implicit operator int (System.Char8 value) { throw null; }
        public static implicit operator long (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ushort (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator uint (System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ulong (System.Char8 value) { throw null; }
        public static bool operator !=(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <=(System.Char8 left, System.Char8 right) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct Index : System.IEquatable<System.Index>
    {
        private readonly int _dummyPrimitive;
        public Index(int value, bool fromEnd = false) { throw null; }
        public static System.Index End { get { throw null; } }
        public bool IsFromEnd { get { throw null; } }
        public static System.Index Start { get { throw null; } }
        public int Value { get { throw null; } }
        public bool Equals(System.Index other) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public static System.Index FromEnd(int value) { throw null; }
        public static System.Index FromStart(int value) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetOffset(int length) { throw null; }
        public static implicit operator System.Index (int value) { throw null; }
        public override string ToString() { throw null; }
    }
    public readonly partial struct Range : System.IEquatable<System.Range>
    {
        private readonly int _dummyPrimitive;
        public Range(System.Index start, System.Index end) { throw null; }
        public static System.Range All { get { throw null; } }
        public System.Index End { get { throw null; } }
        public System.Index Start { get { throw null; } }
        public static System.Range EndAt(System.Index end) { throw null; }
        public override bool Equals(object? value) { throw null; }
        public bool Equals(System.Range other) { throw null; }
        public override int GetHashCode() { throw null; }
        public (int Offset, int Length) GetOffsetAndLength(int length) { throw null; }
        public static System.Range StartAt(System.Index start) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Utf8Extensions
    {
        public static System.ReadOnlySpan<byte> AsBytes(this System.ReadOnlySpan<System.Char8> text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text, int start) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, int start) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<System.Char8> AsMemory(this System.Utf8String? text, System.Range range) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, int start) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<byte> AsMemoryBytes(this System.Utf8String? text, System.Range range) { throw null; }
        public static System.Text.Utf8Span AsSpan(this System.Utf8String? text) { throw null; }
        public static System.Text.Utf8Span AsSpan(this System.Utf8String? text, int start) { throw null; }
        public static System.Text.Utf8Span AsSpan(this System.Utf8String? text, int start, int length) { throw null; }
        public static System.Utf8String ToUtf8String(this System.Text.Rune rune) { throw null; }
    }
    public sealed partial class Utf8String : System.IComparable<System.Utf8String?>, System.IEquatable<System.Utf8String?>
    {
        public static readonly System.Utf8String Empty;
        [System.CLSCompliantAttribute(false)]
        public unsafe Utf8String(byte* value) { }
        public Utf8String(byte[] value, int startIndex, int length) { }
        [System.CLSCompliantAttribute(false)]
        public unsafe Utf8String(char* value) { }
        public Utf8String(char[] value, int startIndex, int length) { }
        public Utf8String(System.ReadOnlySpan<byte> value) { }
        public Utf8String(System.ReadOnlySpan<char> value) { }
        public Utf8String(string value) { }
        public System.Utf8String.ByteEnumerable Bytes { get { throw null; } }
        public System.Utf8String.CharEnumerable Chars { get { throw null; } }
        public System.Utf8String this[System.Range range] { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Utf8String.RuneEnumerable Runes { get { throw null; } }
        public static bool AreEquivalent(System.ReadOnlySpan<byte> utf8Text, System.ReadOnlySpan<char> utf16Text) { throw null; }
        public static bool AreEquivalent(System.Text.Utf8Span utf8Text, System.ReadOnlySpan<char> utf16Text) { throw null; }
        public static bool AreEquivalent(System.Utf8String? utf8Text, string? utf16Text) { throw null; }
        public int CompareTo(System.Utf8String? other) { throw null; }
        public int CompareTo(System.Utf8String? other, System.StringComparison comparison) { throw null; }
        public bool Contains(char value) { throw null; }
        public bool Contains(char value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Text.Rune value) { throw null; }
        public bool Contains(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Utf8String value) { throw null; }
        public bool Contains(System.Utf8String value, System.StringComparison comparison) { throw null; }
        public static System.Utf8String CreateFromRelaxed(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static System.Utf8String CreateFromRelaxed(System.ReadOnlySpan<char> buffer) { throw null; }
        public static System.Utf8String CreateRelaxed<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public static System.Utf8String Create<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public bool EndsWith(char value) { throw null; }
        public bool EndsWith(char value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Text.Rune value) { throw null; }
        public bool EndsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Utf8String value) { throw null; }
        public bool EndsWith(System.Utf8String value, System.StringComparison comparison) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Utf8String? value) { throw null; }
        public bool Equals(System.Utf8String? value, System.StringComparison comparison) { throw null; }
        public static bool Equals(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public static bool Equals(System.Utf8String? a, System.Utf8String? b, System.StringComparison comparison) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.StringComparison comparison) { throw null; }
        public ref readonly byte GetPinnableReference() { throw null; }
        public bool IsAscii() { throw null; }
        public bool IsNormalized(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(false)] System.Utf8String? value) { throw null; }
        public static bool IsNullOrWhiteSpace([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(false)] System.Utf8String? value) { throw null; }
        public System.Utf8String Normalize(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool operator ==(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public static implicit operator System.Text.Utf8Span (System.Utf8String? value) { throw null; }
        public static bool operator !=(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public System.Utf8String.SplitResult Split(char separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Utf8String.SplitResult Split(System.Text.Rune separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Utf8String.SplitResult Split(System.Utf8String separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(char separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(char separator, System.StringComparison comparisonType) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(System.Text.Rune separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(System.Utf8String separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOn(System.Utf8String separator, System.StringComparison comparisonType) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(char separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(char separator, System.StringComparison comparisonType) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(System.Text.Rune separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(System.Utf8String separator) { throw null; }
        public System.Utf8String.SplitOnResult SplitOnLast(System.Utf8String separator, System.StringComparison comparisonType) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(char value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Rune value) { throw null; }
        public bool StartsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Utf8String value) { throw null; }
        public bool StartsWith(System.Utf8String value, System.StringComparison comparison) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public char[] ToCharArray() { throw null; }
        public System.Utf8String ToLower(System.Globalization.CultureInfo culture) { throw null; }
        public System.Utf8String ToLowerInvariant() { throw null; }
        public override string ToString() { throw null; }
        public System.Utf8String ToUpper(System.Globalization.CultureInfo culture) { throw null; }
        public System.Utf8String ToUpperInvariant() { throw null; }
        public System.Utf8String Trim() { throw null; }
        public System.Utf8String TrimEnd() { throw null; }
        public System.Utf8String TrimStart() { throw null; }
        public static bool TryCreateFrom(System.ReadOnlySpan<byte> buffer, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Utf8String? value) { throw null; }
        public static bool TryCreateFrom(System.ReadOnlySpan<char> buffer, [System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out System.Utf8String? value) { throw null; }
        public bool TryFind(char value, out System.Range range) { throw null; }
        public bool TryFind(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Utf8String value, out System.Range range) { throw null; }
        public bool TryFind(System.Utf8String value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(char value, out System.Range range) { throw null; }
        public bool TryFindLast(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Utf8String value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Utf8String value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public static System.Utf8String UnsafeCreateWithoutValidation(System.ReadOnlySpan<byte> utf8Contents) { throw null; }
        public static System.Utf8String UnsafeCreateWithoutValidation<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
        public readonly partial struct ByteEnumerable : System.Collections.Generic.IEnumerable<byte>, System.Collections.IEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Utf8String.ByteEnumerable.Enumerator GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<byte> System.Collections.Generic.IEnumerable<System.Byte>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<byte>, System.Collections.IEnumerator, System.IDisposable
            {
                private object _dummy;
                private int _dummyPrimitive;
                public readonly byte Current { get { throw null; } }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
                void System.IDisposable.Dispose() { }
            }
        }
        public readonly partial struct CharEnumerable : System.Collections.Generic.IEnumerable<char>, System.Collections.IEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Utf8String.CharEnumerable.Enumerator GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<char> System.Collections.Generic.IEnumerable<System.Char>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<char>, System.Collections.IEnumerator, System.IDisposable
            {
                private object _dummy;
                private int _dummyPrimitive;
                public char Current { get { throw null; } }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
                void System.IDisposable.Dispose() { }
            }
        }
        public readonly partial struct RuneEnumerable : System.Collections.Generic.IEnumerable<System.Text.Rune>, System.Collections.IEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Utf8String.RuneEnumerable.Enumerator GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<System.Text.Rune> System.Collections.Generic.IEnumerable<System.Text.Rune>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Text.Rune>, System.Collections.IEnumerator, System.IDisposable
            {
                private object _dummy;
                private int _dummyPrimitive;
                public System.Text.Rune Current { get { throw null; } }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
                void System.IDisposable.Dispose() { }
            }
        }
        public readonly partial struct SplitOnResult
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Utf8String? After { get { throw null; } }
            public System.Utf8String Before { get { throw null; } }
            public void Deconstruct(out System.Utf8String before, out System.Utf8String? after) { throw null; }
        }
        public readonly partial struct SplitResult : System.Collections.Generic.IEnumerable<System.Utf8String?>, System.Collections.IEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6, out System.Utf8String? item7) { throw null; }
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6, out System.Utf8String? item7, out System.Utf8String? item8) { throw null; }
            public System.Utf8String.SplitResult.Enumerator GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<System.Utf8String?>? System.Collections.Generic.IEnumerable<System.Utf8String?>.GetEnumerator() { throw null; }
            System.Collections.IEnumerator? System.Collections.IEnumerable.GetEnumerator() { throw null; }
            public partial struct Enumerator : System.Collections.Generic.IEnumerator<System.Utf8String?>, System.Collections.IEnumerator, System.IDisposable
            {
                private object _dummy;
                private int _dummyPrimitive;
                public System.Utf8String? Current { get { throw null; } }
                object? System.Collections.IEnumerator.Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.Collections.IEnumerator.Reset() { }
                void System.IDisposable.Dispose() { }
            }
        }
    }
    [System.FlagsAttribute]
    public enum Utf8StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
        TrimEntries = 2,
    }
}
namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : System.Net.Http.HttpContent
    {
        public Utf8StringContent(System.Utf8String content) { }
        public Utf8StringContent(System.Utf8String content, string? mediaType) { }
        protected override System.IO.Stream CreateContentReadStream(System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override void SerializeToStream(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context, System.Threading.CancellationToken cancellationToken) { throw null; }
        protected override bool TryComputeLength(out long length) { throw null; }
    }
}
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
        public static explicit operator System.Text.Rune (char ch) { throw null; }
        public static explicit operator System.Text.Rune (int value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Text.Rune (uint value) { throw null; }
        public static bool operator >(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator >=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator !=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator <(System.Text.Rune left, System.Text.Rune right) { throw null; }
        public static bool operator <=(System.Text.Rune left, System.Text.Rune right) { throw null; }
        int System.IComparable.CompareTo(object obj) { throw null; }
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
    }
    public readonly ref partial struct Utf8Span
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Utf8Span(System.Utf8String? value) { throw null; }
        public System.ReadOnlySpan<byte> Bytes { get { throw null; } }
        public System.Text.Utf8Span.CharEnumerable Chars { get { throw null; } }
        public static System.Text.Utf8Span Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public System.Text.Utf8Span this[System.Range range] { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Text.Utf8Span.RuneEnumerable Runes { get { throw null; } }
        public int CompareTo(System.Text.Utf8Span other) { throw null; }
        public int CompareTo(System.Text.Utf8Span other, System.StringComparison comparison) { throw null; }
        public bool Contains(char value) { throw null; }
        public bool Contains(char value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Text.Rune value) { throw null; }
        public bool Contains(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Text.Utf8Span value) { throw null; }
        public bool Contains(System.Text.Utf8Span value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(char value) { throw null; }
        public bool EndsWith(char value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Text.Rune value) { throw null; }
        public bool EndsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Text.Utf8Span value) { throw null; }
        public bool EndsWith(System.Text.Utf8Span value, System.StringComparison comparison) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Text.Utf8Span other) { throw null; }
        public bool Equals(System.Text.Utf8Span other, System.StringComparison comparison) { throw null; }
        public static bool Equals(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public static bool Equals(System.Text.Utf8Span left, System.Text.Utf8Span right, System.StringComparison comparison) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.StringComparison comparison) { throw null; }
        public ref readonly byte GetPinnableReference() { throw null; }
        public bool IsAscii() { throw null; }
        public bool IsEmptyOrWhiteSpace() { throw null; }
        public bool IsNormalized(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public int Normalize(System.Span<byte> destination, System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public System.Utf8String Normalize(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool operator ==(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public static bool operator !=(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public System.Text.Utf8Span.SplitResult Split(char separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Text.Utf8Span.SplitResult Split(System.Text.Rune separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Text.Utf8Span.SplitResult Split(System.Text.Utf8Span separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(char separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(char separator, System.StringComparison comparisonType) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(System.Text.Rune separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(System.Text.Utf8Span separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOn(System.Text.Utf8Span separator, System.StringComparison comparisonType) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(char separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(char separator, System.StringComparison comparisonType) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(System.Text.Rune separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(System.Text.Utf8Span separator) { throw null; }
        public System.Text.Utf8Span.SplitOnResult SplitOnLast(System.Text.Utf8Span separator, System.StringComparison comparisonType) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(char value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Rune value) { throw null; }
        public bool StartsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Utf8Span value) { throw null; }
        public bool StartsWith(System.Text.Utf8Span value, System.StringComparison comparison) { throw null; }
        public byte[] ToByteArray() { throw null; }
        public char[] ToCharArray() { throw null; }
        public int ToChars(System.Span<char> destination) { throw null; }
        public System.Utf8String ToLower(System.Globalization.CultureInfo culture) { throw null; }
        public int ToLower(System.Span<byte> destination, System.Globalization.CultureInfo culture) { throw null; }
        public System.Utf8String ToLowerInvariant() { throw null; }
        public int ToLowerInvariant(System.Span<byte> destination) { throw null; }
        public override string ToString() { throw null; }
        public System.Utf8String ToUpper(System.Globalization.CultureInfo culture) { throw null; }
        public int ToUpper(System.Span<byte> destination, System.Globalization.CultureInfo culture) { throw null; }
        public System.Utf8String ToUpperInvariant() { throw null; }
        public int ToUpperInvariant(System.Span<byte> destination) { throw null; }
        public System.Utf8String ToUtf8String() { throw null; }
        public System.Text.Utf8Span Trim() { throw null; }
        public System.Text.Utf8Span TrimEnd() { throw null; }
        public System.Text.Utf8Span TrimStart() { throw null; }
        public bool TryFind(char value, out System.Range range) { throw null; }
        public bool TryFind(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Utf8Span value, out System.Range range) { throw null; }
        public bool TryFind(System.Text.Utf8Span value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(char value, out System.Range range) { throw null; }
        public bool TryFindLast(char value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Rune value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Utf8Span value, out System.Range range) { throw null; }
        public bool TryFindLast(System.Text.Utf8Span value, System.StringComparison comparisonType, out System.Range range) { throw null; }
        public static System.Text.Utf8Span UnsafeCreateWithoutValidation(System.ReadOnlySpan<byte> buffer) { throw null; }
        public readonly ref partial struct CharEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Text.Utf8Span.CharEnumerable.Enumerator GetEnumerator() { throw null; }
            public ref partial struct Enumerator
            {
                private object _dummy;
                private int _dummyPrimitive;
                public char Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
        public readonly ref partial struct RuneEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Text.Utf8Span.RuneEnumerable.Enumerator GetEnumerator() { throw null; }
            public ref partial struct Enumerator
            {
                private object _dummy;
                private int _dummyPrimitive;
                public System.Text.Rune Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
        public readonly ref partial struct SplitOnResult
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public System.Text.Utf8Span After { get { throw null; } }
            public System.Text.Utf8Span Before { get { throw null; } }
            public void Deconstruct(out System.Text.Utf8Span before, out System.Text.Utf8Span after) { throw null; }
        }
        public readonly ref partial struct SplitResult
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6, out System.Text.Utf8Span item7) { throw null; }
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6, out System.Text.Utf8Span item7, out System.Text.Utf8Span item8) { throw null; }
            public System.Text.Utf8Span.SplitResult.Enumerator GetEnumerator() { throw null; }
            public ref partial struct Enumerator
            {
                private object _dummy;
                private int _dummyPrimitive;
                public System.Text.Utf8Span Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
    }
}
