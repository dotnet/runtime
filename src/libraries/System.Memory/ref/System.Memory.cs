// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public static partial class MemoryExtensions
    {
        public static System.ReadOnlyMemory<char> AsMemory(this string text) { throw null; }
        public static System.ReadOnlyMemory<char> AsMemory(this string text, System.Index startIndex) { throw null; }
        public static System.ReadOnlyMemory<char> AsMemory(this string text, int start) { throw null; }
        public static System.ReadOnlyMemory<char> AsMemory(this string text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<char> AsMemory(this string text, System.Range range) { throw null; }
        public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment) { throw null; }
        public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment, int start) { throw null; }
        public static System.Memory<T> AsMemory<T>(this System.ArraySegment<T> segment, int start, int length) { throw null; }
        public static System.Memory<T> AsMemory<T>(this T[] array) { throw null; }
        public static System.Memory<T> AsMemory<T>(this T[] array, System.Index startIndex) { throw null; }
        public static System.Memory<T> AsMemory<T>(this T[] array, int start) { throw null; }
        public static System.Memory<T> AsMemory<T>(this T[] array, int start, int length) { throw null; }
        public static System.Memory<T> AsMemory<T>(this T[] array, System.Range range) { throw null; }
        public static System.ReadOnlySpan<char> AsSpan(this string text) { throw null; }
        public static System.ReadOnlySpan<char> AsSpan(this string text, int start) { throw null; }
        public static System.ReadOnlySpan<char> AsSpan(this string text, int start, int length) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Index startIndex) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, int start) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, int start, int length) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> segment, System.Range range) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array, System.Index startIndex) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array, int start) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array, int start, int length) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array, System.Range range) { throw null; }
        public static int BinarySearch<T>(this System.ReadOnlySpan<T> span, System.IComparable<T> comparable) { throw null; }
        public static int BinarySearch<T>(this System.Span<T> span, System.IComparable<T> comparable) { throw null; }
        public static int BinarySearch<T, TComparer>(this System.ReadOnlySpan<T> span, T value, TComparer comparer) where TComparer : System.Collections.Generic.IComparer<T> { throw null; }
        public static int BinarySearch<T, TComparable>(this System.ReadOnlySpan<T> span, TComparable comparable) where TComparable : System.IComparable<T> { throw null; }
        public static int BinarySearch<T, TComparer>(this System.Span<T> span, T value, TComparer comparer) where TComparer : System.Collections.Generic.IComparer<T> { throw null; }
        public static int BinarySearch<T, TComparable>(this System.Span<T> span, TComparable comparable) where TComparable : System.IComparable<T> { throw null; }
        public static int CompareTo(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> other, System.StringComparison comparisonType) { throw null; }
        public static bool Contains(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, System.StringComparison comparisonType) { throw null; }
        public static bool Contains<T>(this System.ReadOnlySpan<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static bool Contains<T>(this System.Span<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static void CopyTo<T>(this T[] source, System.Memory<T> destination) { }
        public static void CopyTo<T>(this T[] source, System.Span<T> destination) { }
        public static bool EndsWith(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, System.StringComparison comparisonType) { throw null; }
        public static bool EndsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static bool EndsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static System.Text.SpanRuneEnumerator EnumerateRunes(this System.ReadOnlySpan<char> span) { throw null; }
        public static System.Text.SpanRuneEnumerator EnumerateRunes(this System.Span<char> span) { throw null; }
        public static bool Equals(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> other, System.StringComparison comparisonType) { throw null; }
        public static int IndexOf(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, System.StringComparison comparisonType) { throw null; }
        public static int IndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values) where T : System.IEquatable<T> { throw null; }
        public static int IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1) where T : System.IEquatable<T> { throw null; }
        public static int IndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2) where T : System.IEquatable<T> { throw null; }
        public static int IndexOfAny<T>(this System.Span<T> span, System.ReadOnlySpan<T> values) where T : System.IEquatable<T> { throw null; }
        public static int IndexOfAny<T>(this System.Span<T> span, T value0, T value1) where T : System.IEquatable<T> { throw null; }
        public static int IndexOfAny<T>(this System.Span<T> span, T value0, T value1, T value2) where T : System.IEquatable<T> { throw null; }
        public static int IndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static int IndexOf<T>(this System.ReadOnlySpan<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static int IndexOf<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static int IndexOf<T>(this System.Span<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static bool IsWhiteSpace(this System.ReadOnlySpan<char> span) { throw null; }
        public static int LastIndexOf(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, System.StringComparison comparisonType) { throw null; }
        public static int LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> values) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOfAny<T>(this System.ReadOnlySpan<T> span, T value0, T value1, T value2) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOfAny<T>(this System.Span<T> span, System.ReadOnlySpan<T> values) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOfAny<T>(this System.Span<T> span, T value0, T value1) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOfAny<T>(this System.Span<T> span, T value0, T value1, T value2) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOf<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOf<T>(this System.ReadOnlySpan<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOf<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static int LastIndexOf<T>(this System.Span<T> span, T value) where T : System.IEquatable<T> { throw null; }
        public static bool Overlaps<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other) { throw null; }
        public static bool Overlaps<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other, out int elementOffset) { throw null; }
        public static bool Overlaps<T>(this System.Span<T> span, System.ReadOnlySpan<T> other) { throw null; }
        public static bool Overlaps<T>(this System.Span<T> span, System.ReadOnlySpan<T> other, out int elementOffset) { throw null; }
        public static void Reverse<T>(this System.Span<T> span) { }
        public static int SequenceCompareTo<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other) where T : System.IComparable<T> { throw null; }
        public static int SequenceCompareTo<T>(this System.Span<T> span, System.ReadOnlySpan<T> other) where T : System.IComparable<T> { throw null; }
        public static bool SequenceEqual<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> other) where T : System.IEquatable<T> { throw null; }
        public static bool SequenceEqual<T>(this System.Span<T> span, System.ReadOnlySpan<T> other) where T : System.IEquatable<T> { throw null; }
        public static bool StartsWith(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> value, System.StringComparison comparisonType) { throw null; }
        public static bool StartsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static bool StartsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static int ToLower(this System.ReadOnlySpan<char> source, System.Span<char> destination, System.Globalization.CultureInfo culture) { throw null; }
        public static int ToLowerInvariant(this System.ReadOnlySpan<char> source, System.Span<char> destination) { throw null; }
        public static int ToUpper(this System.ReadOnlySpan<char> source, System.Span<char> destination, System.Globalization.CultureInfo culture) { throw null; }
        public static int ToUpperInvariant(this System.ReadOnlySpan<char> source, System.Span<char> destination) { throw null; }
        public static System.Memory<char> Trim(this System.Memory<char> memory) { throw null; }
        public static System.ReadOnlyMemory<char> Trim(this System.ReadOnlyMemory<char> memory) { throw null; }
        public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span) { throw null; }
        public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span, char trimChar) { throw null; }
        public static System.ReadOnlySpan<char> Trim(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars) { throw null; }
        public static System.Span<char> Trim(this System.Span<char> span) { throw null; }
        public static System.Memory<char> TrimEnd(this System.Memory<char> memory) { throw null; }
        public static System.ReadOnlyMemory<char> TrimEnd(this System.ReadOnlyMemory<char> memory) { throw null; }
        public static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span) { throw null; }
        public static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span, char trimChar) { throw null; }
        public static System.ReadOnlySpan<char> TrimEnd(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars) { throw null; }
        public static System.Span<char> TrimEnd(this System.Span<char> span) { throw null; }
        public static System.Memory<T> TrimEnd<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Memory<T> TrimEnd<T>(this System.Memory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> TrimEnd<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> TrimEnd<T>(this System.ReadOnlyMemory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> TrimEnd<T>(this System.ReadOnlySpan<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> TrimEnd<T>(this System.ReadOnlySpan<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> TrimEnd<T>(this System.Span<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> TrimEnd<T>(this System.Span<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.Memory<char> TrimStart(this System.Memory<char> memory) { throw null; }
        public static System.ReadOnlyMemory<char> TrimStart(this System.ReadOnlyMemory<char> memory) { throw null; }
        public static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span) { throw null; }
        public static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span, char trimChar) { throw null; }
        public static System.ReadOnlySpan<char> TrimStart(this System.ReadOnlySpan<char> span, System.ReadOnlySpan<char> trimChars) { throw null; }
        public static System.Span<char> TrimStart(this System.Span<char> span) { throw null; }
        public static System.Memory<T> TrimStart<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Memory<T> TrimStart<T>(this System.Memory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> TrimStart<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> TrimStart<T>(this System.ReadOnlyMemory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> TrimStart<T>(this System.ReadOnlySpan<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> TrimStart<T>(this System.ReadOnlySpan<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> TrimStart<T>(this System.Span<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> TrimStart<T>(this System.Span<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.Memory<T> Trim<T>(this System.Memory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Memory<T> Trim<T>(this System.Memory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> Trim<T>(this System.ReadOnlyMemory<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlyMemory<T> Trim<T>(this System.ReadOnlyMemory<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> Trim<T>(this System.ReadOnlySpan<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.ReadOnlySpan<T> Trim<T>(this System.ReadOnlySpan<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> Trim<T>(this System.Span<T> memory, System.ReadOnlySpan<T> trimElements) where T : System.IEquatable<T> { throw null; }
        public static System.Span<T> Trim<T>(this System.Span<T> memory, T trimElement) where T : System.IEquatable<T> { throw null; }
    }
    public readonly partial struct SequencePosition : System.IEquatable<System.SequencePosition>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public SequencePosition(object @object, int integer) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.SequencePosition other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public int GetInteger() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute(System.ComponentModel.EditorBrowsableState.Never)]
        public object GetObject() { throw null; }
    }
}
namespace System.Buffers
{
    public sealed partial class ArrayBufferWriter<T> : System.Buffers.IBufferWriter<T>
    {
        public ArrayBufferWriter() { }
        public ArrayBufferWriter(int initialCapacity) { }
        public int Capacity { get { throw null; } }
        public int FreeCapacity { get { throw null; } }
        public int WrittenCount { get { throw null; } }
        public System.ReadOnlyMemory<T> WrittenMemory { get { throw null; } }
        public System.ReadOnlySpan<T> WrittenSpan { get { throw null; } }
        public void Advance(int count) { }
        public void Clear() { }
        public System.Memory<T> GetMemory(int sizeHint = 0) { throw null; }
        public System.Span<T> GetSpan(int sizeHint = 0) { throw null; }
    }
    public static partial class BuffersExtensions
    {
        public static void CopyTo<T>(this in System.Buffers.ReadOnlySequence<T> source, System.Span<T> destination) { }
        public static System.SequencePosition? PositionOf<T>(this in System.Buffers.ReadOnlySequence<T> source, T value) where T : System.IEquatable<T> { throw null; }
        public static T[] ToArray<T>(this in System.Buffers.ReadOnlySequence<T> sequence) { throw null; }
        public static void Write<T>(this System.Buffers.IBufferWriter<T> writer, System.ReadOnlySpan<T> value) { }
    }
    public partial interface IBufferWriter<T>
    {
        void Advance(int count);
        System.Memory<T> GetMemory(int sizeHint = 0);
        System.Span<T> GetSpan(int sizeHint = 0);
    }
    public abstract partial class MemoryPool<T> : System.IDisposable
    {
        protected MemoryPool() { }
        public abstract int MaxBufferSize { get; }
        public static System.Buffers.MemoryPool<T> Shared { get { throw null; } }
        public void Dispose() { }
        protected abstract void Dispose(bool disposing);
        public abstract System.Buffers.IMemoryOwner<T> Rent(int minBufferSize = -1);
    }
    public abstract partial class ReadOnlySequenceSegment<T>
    {
        protected ReadOnlySequenceSegment() { }
        public System.ReadOnlyMemory<T> Memory { get { throw null; } protected set { } }
        public System.Buffers.ReadOnlySequenceSegment<T> Next { get { throw null; } protected set { } }
        public long RunningIndex { get { throw null; } protected set { } }
    }
    public readonly partial struct ReadOnlySequence<T>
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly System.Buffers.ReadOnlySequence<T> Empty;
        public ReadOnlySequence(System.Buffers.ReadOnlySequenceSegment<T> startSegment, int startIndex, System.Buffers.ReadOnlySequenceSegment<T> endSegment, int endIndex) { throw null; }
        public ReadOnlySequence(System.ReadOnlyMemory<T> memory) { throw null; }
        public ReadOnlySequence(T[] array) { throw null; }
        public ReadOnlySequence(T[] array, int start, int length) { throw null; }
        public System.SequencePosition End { get { throw null; } }
        public System.ReadOnlyMemory<T> First { get { throw null; } }
        public System.ReadOnlySpan<T> FirstSpan { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public bool IsSingleSegment { get { throw null; } }
        public long Length { get { throw null; } }
        public System.SequencePosition Start { get { throw null; } }
        public System.Buffers.ReadOnlySequence<T>.Enumerator GetEnumerator() { throw null; }
        public System.SequencePosition GetPosition(long offset) { throw null; }
        public System.SequencePosition GetPosition(long offset, System.SequencePosition origin) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(int start, int length) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(int start, System.SequencePosition end) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(long start) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(long start, long length) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(long start, System.SequencePosition end) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, int length) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, long length) { throw null; }
        public System.Buffers.ReadOnlySequence<T> Slice(System.SequencePosition start, System.SequencePosition end) { throw null; }
        public override string ToString() { throw null; }
        public bool TryGet(ref System.SequencePosition position, out System.ReadOnlyMemory<T> memory, bool advance = true) { throw null; }
        public partial struct Enumerator
        {
            private object _dummy;
            private int _dummyPrimitive;
            public Enumerator(in System.Buffers.ReadOnlySequence<T> sequence) { throw null; }
            public System.ReadOnlyMemory<T> Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public static partial class SequenceReaderExtensions
    {
        public static bool TryReadBigEndian(this ref System.Buffers.SequenceReader<byte> reader, out short value) { throw null; }
        public static bool TryReadBigEndian(this ref System.Buffers.SequenceReader<byte> reader, out int value) { throw null; }
        public static bool TryReadBigEndian(this ref System.Buffers.SequenceReader<byte> reader, out long value) { throw null; }
        public static bool TryReadLittleEndian(this ref System.Buffers.SequenceReader<byte> reader, out short value) { throw null; }
        public static bool TryReadLittleEndian(this ref System.Buffers.SequenceReader<byte> reader, out int value) { throw null; }
        public static bool TryReadLittleEndian(this ref System.Buffers.SequenceReader<byte> reader, out long value) { throw null; }
    }
    public ref partial struct SequenceReader<T> where T : unmanaged, System.IEquatable<T>
    {
        public SequenceReader(System.Buffers.ReadOnlySequence<T> sequence) { throw null; }
        public long Consumed { get { throw null; } }
        public System.ReadOnlySpan<T> CurrentSpan { get { throw null; } }
        public int CurrentSpanIndex { get { throw null; } }
        public bool End { get { throw null; } }
        public long Length { get { throw null; } }
        public System.SequencePosition Position { get { throw null; } }
        public long Remaining { get { throw null; } }
        public System.Buffers.ReadOnlySequence<T> Sequence { get { throw null; } }
        public System.ReadOnlySpan<T> UnreadSpan { get { throw null; } }
        public void Advance(long count) { }
        public long AdvancePast(T value) { throw null; }
        public long AdvancePastAny(System.ReadOnlySpan<T> values) { throw null; }
        public long AdvancePastAny(T value0, T value1) { throw null; }
        public long AdvancePastAny(T value0, T value1, T value2) { throw null; }
        public long AdvancePastAny(T value0, T value1, T value2, T value3) { throw null; }
        public bool IsNext(System.ReadOnlySpan<T> next, bool advancePast = false) { throw null; }
        public bool IsNext(T next, bool advancePast = false) { throw null; }
        public void Rewind(long count) { }
        public bool TryAdvanceTo(T delimiter, bool advancePastDelimiter = true) { throw null; }
        public bool TryAdvanceToAny(System.ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true) { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
        public bool TryPeek(out T value) { throw null; }
        public bool TryRead(out T value) { throw null; }
        public bool TryReadTo(out System.Buffers.ReadOnlySequence<T> sequence, System.ReadOnlySpan<T> delimiter, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadTo(out System.Buffers.ReadOnlySequence<T> sequence, T delimiter, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadTo(out System.Buffers.ReadOnlySequence<T> sequence, T delimiter, T delimiterEscape, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadTo(out System.ReadOnlySpan<T> span, T delimiter, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadTo(out System.ReadOnlySpan<T> span, T delimiter, T delimiterEscape, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadToAny(out System.Buffers.ReadOnlySequence<T> sequence, System.ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true) { throw null; }
        public bool TryReadToAny(out System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> delimiters, bool advancePastDelimiter = true) { throw null; }
    }
    public readonly partial struct StandardFormat : System.IEquatable<System.Buffers.StandardFormat>
    {
        private readonly int _dummyPrimitive;
        public const byte MaxPrecision = (byte)99;
        public const byte NoPrecision = (byte)255;
        public StandardFormat(char symbol, byte precision = (byte)255) { throw null; }
        public bool HasPrecision { get { throw null; } }
        public bool IsDefault { get { throw null; } }
        public byte Precision { get { throw null; } }
        public char Symbol { get { throw null; } }
        public bool Equals(System.Buffers.StandardFormat other) { throw null; }
        public override bool Equals(object obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Buffers.StandardFormat left, System.Buffers.StandardFormat right) { throw null; }
        public static implicit operator System.Buffers.StandardFormat (char symbol) { throw null; }
        public static bool operator !=(System.Buffers.StandardFormat left, System.Buffers.StandardFormat right) { throw null; }
        public static System.Buffers.StandardFormat Parse(System.ReadOnlySpan<char> format) { throw null; }
        public static System.Buffers.StandardFormat Parse(string format) { throw null; }
        public override string ToString() { throw null; }
        public static bool TryParse(System.ReadOnlySpan<char> format, out System.Buffers.StandardFormat result) { throw null; }
    }
}
namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        public static short ReadInt16BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static short ReadInt16LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static int ReadInt32BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static int ReadInt32LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static long ReadInt64BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static long ReadInt64LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ReadUInt16BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ReadUInt16LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ReadUInt32BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ReadUInt32LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ReadUInt64BigEndian(System.ReadOnlySpan<byte> source) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ReadUInt64LittleEndian(System.ReadOnlySpan<byte> source) { throw null; }
        public static byte ReverseEndianness(byte value) { throw null; }
        public static short ReverseEndianness(short value) { throw null; }
        public static int ReverseEndianness(int value) { throw null; }
        public static long ReverseEndianness(long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static sbyte ReverseEndianness(sbyte value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ReverseEndianness(ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ReverseEndianness(uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ReverseEndianness(ulong value) { throw null; }
        public static bool TryReadInt16BigEndian(System.ReadOnlySpan<byte> source, out short value) { throw null; }
        public static bool TryReadInt16LittleEndian(System.ReadOnlySpan<byte> source, out short value) { throw null; }
        public static bool TryReadInt32BigEndian(System.ReadOnlySpan<byte> source, out int value) { throw null; }
        public static bool TryReadInt32LittleEndian(System.ReadOnlySpan<byte> source, out int value) { throw null; }
        public static bool TryReadInt64BigEndian(System.ReadOnlySpan<byte> source, out long value) { throw null; }
        public static bool TryReadInt64LittleEndian(System.ReadOnlySpan<byte> source, out long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt16BigEndian(System.ReadOnlySpan<byte> source, out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt16LittleEndian(System.ReadOnlySpan<byte> source, out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt32BigEndian(System.ReadOnlySpan<byte> source, out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt32LittleEndian(System.ReadOnlySpan<byte> source, out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt64BigEndian(System.ReadOnlySpan<byte> source, out ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt64LittleEndian(System.ReadOnlySpan<byte> source, out ulong value) { throw null; }
        public static bool TryWriteInt16BigEndian(System.Span<byte> destination, short value) { throw null; }
        public static bool TryWriteInt16LittleEndian(System.Span<byte> destination, short value) { throw null; }
        public static bool TryWriteInt32BigEndian(System.Span<byte> destination, int value) { throw null; }
        public static bool TryWriteInt32LittleEndian(System.Span<byte> destination, int value) { throw null; }
        public static bool TryWriteInt64BigEndian(System.Span<byte> destination, long value) { throw null; }
        public static bool TryWriteInt64LittleEndian(System.Span<byte> destination, long value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt16BigEndian(System.Span<byte> destination, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt16LittleEndian(System.Span<byte> destination, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt32BigEndian(System.Span<byte> destination, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt32LittleEndian(System.Span<byte> destination, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt64BigEndian(System.Span<byte> destination, ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt64LittleEndian(System.Span<byte> destination, ulong value) { throw null; }
        public static void WriteInt16BigEndian(System.Span<byte> destination, short value) { }
        public static void WriteInt16LittleEndian(System.Span<byte> destination, short value) { }
        public static void WriteInt32BigEndian(System.Span<byte> destination, int value) { }
        public static void WriteInt32LittleEndian(System.Span<byte> destination, int value) { }
        public static void WriteInt64BigEndian(System.Span<byte> destination, long value) { }
        public static void WriteInt64LittleEndian(System.Span<byte> destination, long value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt16BigEndian(System.Span<byte> destination, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt16LittleEndian(System.Span<byte> destination, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt32BigEndian(System.Span<byte> destination, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt32LittleEndian(System.Span<byte> destination, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt64BigEndian(System.Span<byte> destination, ulong value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt64LittleEndian(System.Span<byte> destination, ulong value) { }
    }
}
namespace System.Buffers.Text
{
    public static partial class Base64
    {
        public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<byte> utf8, System.Span<byte> bytes, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromUtf8InPlace(System.Span<byte> buffer, out int bytesWritten) { throw null; }
        public static System.Buffers.OperationStatus EncodeToUtf8(System.ReadOnlySpan<byte> bytes, System.Span<byte> utf8, out int bytesConsumed, out int bytesWritten, bool isFinalBlock = true) { throw null; }
        public static System.Buffers.OperationStatus EncodeToUtf8InPlace(System.Span<byte> buffer, int dataLength, out int bytesWritten) { throw null; }
        public static int GetMaxDecodedFromUtf8Length(int length) { throw null; }
        public static int GetMaxEncodedToUtf8Length(int length) { throw null; }
    }
    public static partial class Utf8Formatter
    {
        public static bool TryFormat(bool value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(byte value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.DateTime value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.DateTimeOffset value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(decimal value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(double value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.Guid value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(short value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(int value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(long value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(sbyte value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(float value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.TimeSpan value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(ushort value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(uint value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(ulong value, System.Span<byte> destination, out int bytesWritten, System.Buffers.StandardFormat format = default(System.Buffers.StandardFormat)) { throw null; }
    }
    public static partial class Utf8Parser
    {
        public static bool TryParse(System.ReadOnlySpan<byte> source, out bool value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out byte value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out System.DateTime value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out System.DateTimeOffset value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out decimal value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out double value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out System.Guid value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out short value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out int value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out long value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> source, out sbyte value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out float value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> source, out System.TimeSpan value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> source, out ushort value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> source, out uint value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> source, out ulong value, out int bytesConsumed, char standardFormat = '\0') { throw null; }
    }
}
namespace System.Runtime.InteropServices
{
    public static partial class MemoryMarshal
    {
        public static System.ReadOnlySpan<byte> AsBytes<T>(System.ReadOnlySpan<T> span) where T : struct { throw null; }
        public static System.Span<byte> AsBytes<T>(System.Span<T> span) where T : struct { throw null; }
        public static System.Memory<T> AsMemory<T>(System.ReadOnlyMemory<T> memory) { throw null; }
        public static ref readonly T AsRef<T>(System.ReadOnlySpan<byte> span) where T : struct { throw null; }
        public static ref T AsRef<T>(System.Span<byte> span) where T : struct { throw null; }
        public static System.ReadOnlySpan<TTo> Cast<TFrom, TTo>(System.ReadOnlySpan<TFrom> span) where TFrom : struct where TTo : struct { throw null; }
        public static System.Span<TTo> Cast<TFrom, TTo>(System.Span<TFrom> span) where TFrom : struct where TTo : struct { throw null; }
        public static System.Memory<T> CreateFromPinnedArray<T>(T[] array, int start, int length) { throw null; }
        public static System.ReadOnlySpan<T> CreateReadOnlySpan<T>(ref T reference, int length) { throw null; }
        public static System.Span<T> CreateSpan<T>(ref T reference, int length) { throw null; }
        public static ref T GetReference<T>(System.ReadOnlySpan<T> span) { throw null; }
        public static ref T GetReference<T>(System.Span<T> span) { throw null; }
        public static T Read<T>(System.ReadOnlySpan<byte> source) where T : struct { throw null; }
        public static System.Collections.Generic.IEnumerable<T> ToEnumerable<T>(System.ReadOnlyMemory<T> memory) { throw null; }
        public static bool TryGetArray<T>(System.ReadOnlyMemory<T> memory, out System.ArraySegment<T> segment) { throw null; }
        public static bool TryGetMemoryManager<T, TManager>(System.ReadOnlyMemory<T> memory, out TManager manager) where TManager : System.Buffers.MemoryManager<T> { throw null; }
        public static bool TryGetMemoryManager<T, TManager>(System.ReadOnlyMemory<T> memory, out TManager manager, out int start, out int length) where TManager : System.Buffers.MemoryManager<T> { throw null; }
        public static bool TryGetString(System.ReadOnlyMemory<char> memory, out string text, out int start, out int length) { throw null; }
        public static bool TryRead<T>(System.ReadOnlySpan<byte> source, out T value) where T : struct { throw null; }
        public static bool TryWrite<T>(System.Span<byte> destination, ref T value) where T : struct { throw null; }
        public static void Write<T>(System.Span<byte> destination, ref T value) where T : struct { }
    }
    public static partial class SequenceMarshal
    {
        public static bool TryGetArray<T>(System.Buffers.ReadOnlySequence<T> sequence, out System.ArraySegment<T> segment) { throw null; }
        public static bool TryGetReadOnlyMemory<T>(System.Buffers.ReadOnlySequence<T> sequence, out System.ReadOnlyMemory<T> memory) { throw null; }
        public static bool TryGetReadOnlySequenceSegment<T>(System.Buffers.ReadOnlySequence<T> sequence, out System.Buffers.ReadOnlySequenceSegment<T> startSegment, out int startIndex, out System.Buffers.ReadOnlySequenceSegment<T> endSegment, out int endIndex) { throw null; }
        public static bool TryRead<T>(ref System.Buffers.SequenceReader<byte> reader, out T value) where T : unmanaged { throw null; }
    }
}
namespace System.Text
{
    public ref partial struct SpanRuneEnumerator
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Text.Rune Current { get { throw null; } }
        public System.Text.SpanRuneEnumerator GetEnumerator() { throw null; }
        public bool MoveNext() { throw null; }
    }
}
