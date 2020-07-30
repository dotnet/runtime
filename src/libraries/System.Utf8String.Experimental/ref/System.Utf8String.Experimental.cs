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
        public static explicit operator System.Char8(char value) { throw null; }
        public static explicit operator char(System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator sbyte(System.Char8 value) { throw null; }
        public static explicit operator System.Char8(short value) { throw null; }
        public static explicit operator System.Char8(int value) { throw null; }
        public static explicit operator System.Char8(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static explicit operator System.Char8(ulong value) { throw null; }
        public static bool operator >(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator >=(System.Char8 left, System.Char8 right) { throw null; }
        public static implicit operator System.Char8(byte value) { throw null; }
        public static implicit operator byte(System.Char8 value) { throw null; }
        public static implicit operator short(System.Char8 value) { throw null; }
        public static implicit operator int(System.Char8 value) { throw null; }
        public static implicit operator long(System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ushort(System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator uint(System.Char8 value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static implicit operator ulong(System.Char8 value) { throw null; }
        public static bool operator !=(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <(System.Char8 left, System.Char8 right) { throw null; }
        public static bool operator <=(System.Char8 left, System.Char8 right) { throw null; }
        public override string ToString() { throw null; }
    }
    public static partial class Utf8Extensions
    {
        public static System.ReadOnlySpan<byte> AsBytes(this System.ReadOnlySpan<System.Char8> text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text, int start) { throw null; }
        public static System.ReadOnlySpan<byte> AsBytes(this System.Utf8String? text, int start, int length) { throw null; }
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
        public ByteEnumerable Bytes { get { throw null; } }
        public CharEnumerable Chars { get { throw null; } }
        public int Length { get { throw null; } }
        public RuneEnumerable Runes { get { throw null; } }
        public static bool AreEquivalent(System.Utf8String? utf8Text, string? utf16Text) { throw null; }
        public static bool AreEquivalent(System.Text.Utf8Span utf8Text, System.ReadOnlySpan<char> utf16Text) { throw null; }
        public static bool AreEquivalent(System.ReadOnlySpan<byte> utf8Text, System.ReadOnlySpan<char> utf16Text) { throw null; }
        public int CompareTo(System.Utf8String? other) { throw null; }
        public int CompareTo(System.Utf8String? other, System.StringComparison comparison) { throw null; }
        public bool Contains(char value) { throw null; }
        public bool Contains(char value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Text.Rune value) { throw null; }
        public bool Contains(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool Contains(System.Utf8String value) { throw null; }
        public bool Contains(System.Utf8String value, System.StringComparison comparison) { throw null; }
#if !NETSTANDARD2_0 && !NETFRAMEWORK
        public static System.Utf8String Create<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
#endif
        public static System.Utf8String CreateFromRelaxed(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static System.Utf8String CreateFromRelaxed(System.ReadOnlySpan<char> buffer) { throw null; }
#if !NETSTANDARD2_0 && !NETFRAMEWORK
        public static System.Utf8String CreateRelaxed<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
#endif
        public bool EndsWith(char value) { throw null; }
        public bool EndsWith(char value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Text.Rune value) { throw null; }
        public bool EndsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool EndsWith(System.Utf8String value) { throw null; }
        public bool EndsWith(System.Utf8String value, System.StringComparison comparison) { throw null; }
        public override bool Equals(object? obj) { throw null; }
        public static bool Equals(System.Utf8String? a, System.Utf8String? b, System.StringComparison comparison) { throw null; }
        public static bool Equals(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public bool Equals(System.Utf8String? value) { throw null; }
        public bool Equals(System.Utf8String? value, System.StringComparison comparison) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.StringComparison comparison) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref readonly byte GetPinnableReference() { throw null; }
        public static implicit operator System.Text.Utf8Span(System.Utf8String? value) { throw null; }
        public bool IsAscii() { throw null; }
        public bool IsNormalized(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool IsNullOrEmpty([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(false)] System.Utf8String? value) { throw null; }
        public static bool IsNullOrWhiteSpace([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(false)] System.Utf8String? value) { throw null; }
        public System.Utf8String Normalize(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool operator !=(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public static bool operator ==(System.Utf8String? left, System.Utf8String? right) { throw null; }
        public SplitResult Split(char separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitResult Split(System.Text.Rune separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitResult Split(System.Utf8String separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitOnResult SplitOn(char separator) { throw null; }
        public SplitOnResult SplitOn(char separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOn(System.Text.Rune separator) { throw null; }
        public SplitOnResult SplitOn(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOn(System.Utf8String separator) { throw null; }
        public SplitOnResult SplitOn(System.Utf8String separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(char separator) { throw null; }
        public SplitOnResult SplitOnLast(char separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Rune separator) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(System.Utf8String separator) { throw null; }
        public SplitOnResult SplitOnLast(System.Utf8String separator, System.StringComparison comparisonType) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(char value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Rune value) { throw null; }
        public bool StartsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Utf8String value) { throw null; }
        public bool StartsWith(System.Utf8String value, System.StringComparison comparison) { throw null; }
        public System.Utf8String this[System.Range range] { get { throw null; } }
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
#if !NETSTANDARD2_0 && !NETFRAMEWORK
        public static System.Utf8String UnsafeCreateWithoutValidation<TState>(int length, TState state, System.Buffers.SpanAction<byte, TState> action) { throw null; }
#endif
        public readonly partial struct ByteEnumerable : System.Collections.Generic.IEnumerable<byte>
        {
            private readonly object _dummy;
            public Enumerator GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<byte> System.Collections.Generic.IEnumerable<byte>.GetEnumerator() { throw null; }
            public struct Enumerator : System.Collections.Generic.IEnumerator<byte>
            {
                private readonly object _dummy;
                private readonly int _dummyPrimitive;
                public readonly byte Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.IDisposable.Dispose() { }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        public readonly partial struct CharEnumerable : System.Collections.Generic.IEnumerable<char>
        {
            private readonly object _dummy;
            public Enumerator GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<char> System.Collections.Generic.IEnumerable<char>.GetEnumerator() { throw null; }
            public struct Enumerator : System.Collections.Generic.IEnumerator<char>
            {
                private readonly object _dummy;
                private readonly int _dummyPrimitive;
                public char Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.IDisposable.Dispose() { }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        public readonly partial struct RuneEnumerable : System.Collections.Generic.IEnumerable<System.Text.Rune>
        {
            private readonly object _dummy;
            public Enumerator GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<System.Text.Rune> System.Collections.Generic.IEnumerable<System.Text.Rune>.GetEnumerator() { throw null; }
            public struct Enumerator : System.Collections.Generic.IEnumerator<System.Text.Rune>
            {
                private readonly object _dummy;
                private readonly int _dummyPrimitive;
                public System.Text.Rune Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.IDisposable.Dispose() { }
                object System.Collections.IEnumerator.Current { get { throw null; } }
                void System.Collections.IEnumerator.Reset() { }
            }
        }
        public readonly struct SplitResult : System.Collections.Generic.IEnumerable<Utf8String?>
        {
            private readonly object _dummy;
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6, out System.Utf8String? item7) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String? item1, out System.Utf8String? item2, out System.Utf8String? item3, out System.Utf8String? item4, out System.Utf8String? item5, out System.Utf8String? item6, out System.Utf8String? item7, out System.Utf8String? item8) { throw null; }
            public Enumerator GetEnumerator() { throw null; }
            System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
            System.Collections.Generic.IEnumerator<System.Utf8String?> System.Collections.Generic.IEnumerable<System.Utf8String?>.GetEnumerator() { throw null; }
            public struct Enumerator : System.Collections.Generic.IEnumerator<System.Utf8String?>
            {
                private readonly object _dummy;
                public System.Utf8String? Current { get { throw null; } }
                public bool MoveNext() { throw null; }
                void System.IDisposable.Dispose() { }
                object? System.Collections.IEnumerator.Current { get { throw null; } }
                void System.Collections.IEnumerator.Reset() { throw null; }
            }
        }
        public readonly struct SplitOnResult
        {
            private readonly object _dummy;
            public System.Utf8String? After { get { throw null; } }
            public System.Utf8String Before { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Utf8String before, out System.Utf8String? after) { throw null; }
        }
    }
    [System.FlagsAttribute]
    public enum Utf8StringSplitOptions
    {
        None = 0,
        RemoveEmptyEntries = 1,
        TrimEntries = 2
    }
}
namespace System.Net.Http
{
    public sealed partial class Utf8StringContent : System.Net.Http.HttpContent
    {
        public Utf8StringContent(System.Utf8String content) { }
        public Utf8StringContent(System.Utf8String content, string? mediaType) { }
        protected override System.Threading.Tasks.Task<System.IO.Stream> CreateContentReadStreamAsync() { throw null; }
        protected override System.Threading.Tasks.Task SerializeToStreamAsync(System.IO.Stream stream, System.Net.TransportContext? context) { throw null; }
        protected override bool TryComputeLength(out long length) { throw null; }
    }
}
namespace System.Text
{
    public readonly ref partial struct Utf8Span
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public Utf8Span(System.Utf8String? value) { throw null; }
        public System.ReadOnlySpan<byte> Bytes { get { throw null; } }
        public CharEnumerable Chars { get { throw null; } }
        public static System.Text.Utf8Span Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public RuneEnumerable Runes { get { throw null; } }
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
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        [System.ObsoleteAttribute("Equals(object) on Utf8Span will always throw an exception. Use Equals(Utf8Span) or == instead.")]
        public override bool Equals(object? obj) { throw null; }
        public bool Equals(System.Text.Utf8Span other) { throw null; }
        public bool Equals(System.Text.Utf8Span other, System.StringComparison comparison) { throw null; }
        public static bool Equals(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public static bool Equals(System.Text.Utf8Span left, System.Text.Utf8Span right, System.StringComparison comparison) { throw null; }
        public override int GetHashCode() { throw null; }
        public int GetHashCode(System.StringComparison comparison) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public ref readonly byte GetPinnableReference() { throw null; }
        public bool IsAscii() { throw null; }
        public bool IsEmptyOrWhiteSpace() { throw null; }
        public bool IsNormalized(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public System.Utf8String Normalize(System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public int Normalize(System.Span<byte> destination, System.Text.NormalizationForm normalizationForm = System.Text.NormalizationForm.FormC) { throw null; }
        public static bool operator !=(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public static bool operator ==(System.Text.Utf8Span left, System.Text.Utf8Span right) { throw null; }
        public System.Text.Utf8Span this[System.Range range] { get { throw null; } }
        public SplitResult Split(char separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitResult Split(System.Text.Rune separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitResult Split(System.Text.Utf8Span separator, System.Utf8StringSplitOptions options = System.Utf8StringSplitOptions.None) { throw null; }
        public SplitOnResult SplitOn(char separator) { throw null; }
        public SplitOnResult SplitOn(char separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOn(System.Text.Rune separator) { throw null; }
        public SplitOnResult SplitOn(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOn(System.Text.Utf8Span separator) { throw null; }
        public SplitOnResult SplitOn(System.Text.Utf8Span separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(char separator) { throw null; }
        public SplitOnResult SplitOnLast(char separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Rune separator) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Rune separator, System.StringComparison comparisonType) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Utf8Span separator) { throw null; }
        public SplitOnResult SplitOnLast(System.Text.Utf8Span separator, System.StringComparison comparisonType) { throw null; }
        public bool StartsWith(char value) { throw null; }
        public bool StartsWith(char value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Rune value) { throw null; }
        public bool StartsWith(System.Text.Rune value, System.StringComparison comparison) { throw null; }
        public bool StartsWith(System.Text.Utf8Span value) { throw null; }
        public bool StartsWith(System.Text.Utf8Span value, System.StringComparison comparison) { throw null; }
        public System.Text.Utf8Span Trim() { throw null; }
        public System.Text.Utf8Span TrimEnd() { throw null; }
        public System.Text.Utf8Span TrimStart() { throw null; }
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
        public readonly ref struct CharEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public Enumerator GetEnumerator() { throw null; }
            public ref struct Enumerator
            {
                private object _dummy;
                private int _dummyPrimitive;
                public char Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
        public readonly ref struct RuneEnumerable
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public Enumerator GetEnumerator() { throw null; }
            public ref struct Enumerator
            {
                private object _dummy;
                private int _dummyPrimitive;
                public System.Text.Rune Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
        public readonly ref struct SplitResult
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6, out System.Text.Utf8Span item7) { throw null; }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span item1, out System.Text.Utf8Span item2, out System.Text.Utf8Span item3, out System.Text.Utf8Span item4, out System.Text.Utf8Span item5, out System.Text.Utf8Span item6, out System.Text.Utf8Span item7, out System.Text.Utf8Span item8) { throw null; }
            public Enumerator GetEnumerator() { throw null; }
            public ref struct Enumerator
            {
                private readonly object _dummy;
                private readonly int _dummyPrimitive;
                public System.Text.Utf8Span Current { get { throw null; } }
                public bool MoveNext() { throw null; }
            }
        }
        public readonly ref struct SplitOnResult
        {
            private readonly object _dummy;
            private readonly int _dummyPrimitive;
            public Utf8Span After { get { throw null; } }
            public Utf8Span Before { get { throw null; } }
            [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
            public void Deconstruct(out System.Text.Utf8Span before, out System.Text.Utf8Span after) { throw null; }
        }
    }
}
