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
        public static System.ReadOnlySpan<byte> AsBytes<T>(this System.ReadOnlySpan<T> source) where T : struct { throw null; }
        public static System.Span<byte> AsBytes<T>(this System.Span<T> source) where T : struct { throw null; }
        public static System.ReadOnlyMemory<char> AsReadOnlyMemory(this string text) { throw null; }
        public static System.ReadOnlyMemory<char> AsReadOnlyMemory(this string text, int start) { throw null; }
        public static System.ReadOnlyMemory<char> AsReadOnlyMemory(this string text, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<T> AsReadOnlyMemory<T>(this System.Memory<T> memory) { throw null; }
        public static System.ReadOnlySpan<char> AsReadOnlySpan(this string text) { throw null; }
        public static System.ReadOnlySpan<char> AsReadOnlySpan(this string text, int start) { throw null; }
        public static System.ReadOnlySpan<char> AsReadOnlySpan(this string text, int start, int length) { throw null; }
        public static System.ReadOnlySpan<T> AsReadOnlySpan<T>(this System.ArraySegment<T> arraySegment) { throw null; }
        public static System.ReadOnlySpan<T> AsReadOnlySpan<T>(this System.Span<T> span) { throw null; }
        public static System.ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array) { throw null; }
        public static System.Span<T> AsSpan<T>(this System.ArraySegment<T> arraySegment) { throw null; }
        public static System.Span<T> AsSpan<T>(this T[] array) { throw null; }
        public static int BinarySearch<T>(this System.ReadOnlySpan<T> span, System.IComparable<T> comparable) { throw null; }
        public static int BinarySearch<T>(this System.Span<T> span, System.IComparable<T> comparable) { throw null; }
        public static int BinarySearch<T, TComparer>(this System.ReadOnlySpan<T> span, T value, TComparer comparer) where TComparer : System.Collections.Generic.IComparer<T> { throw null; }
        public static int BinarySearch<T, TComparable>(this System.ReadOnlySpan<T> span, TComparable comparable) where TComparable : System.IComparable<T> { throw null; }
        public static int BinarySearch<T, TComparer>(this System.Span<T> span, T value, TComparer comparer) where TComparer : System.Collections.Generic.IComparer<T> { throw null; }
        public static int BinarySearch<T, TComparable>(this System.Span<T> span, TComparable comparable) where TComparable : System.IComparable<T> { throw null; }
        public static void CopyTo<T>(this T[] array, System.Memory<T> destination) { }
        public static void CopyTo<T>(this T[] array, System.Span<T> destination) { }
        public static bool EndsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static bool EndsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
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
        public static System.ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this System.ReadOnlySpan<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
        public static System.Span<TTo> NonPortableCast<TFrom, TTo>(this System.Span<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
        public static bool Overlaps<T>(this System.ReadOnlySpan<T> first, System.ReadOnlySpan<T> second) { throw null; }
        public static bool Overlaps<T>(this System.ReadOnlySpan<T> first, System.ReadOnlySpan<T> second, out int elementOffset) { throw null; }
        public static bool Overlaps<T>(this System.Span<T> first, System.ReadOnlySpan<T> second) { throw null; }
        public static bool Overlaps<T>(this System.Span<T> first, System.ReadOnlySpan<T> second, out int elementOffset) { throw null; }
        public static void Reverse<T>(this System.Span<T> span) { }
        public static int SequenceCompareTo<T>(this System.ReadOnlySpan<T> first, System.ReadOnlySpan<T> second) where T : System.IComparable<T> { throw null; }
        public static int SequenceCompareTo<T>(this System.Span<T> first, System.ReadOnlySpan<T> second) where T : System.IComparable<T> { throw null; }
        public static bool SequenceEqual<T>(this System.ReadOnlySpan<T> first, System.ReadOnlySpan<T> second) where T : System.IEquatable<T> { throw null; }
        public static bool SequenceEqual<T>(this System.Span<T> first, System.ReadOnlySpan<T> second) where T : System.IEquatable<T> { throw null; }
        public static bool StartsWith<T>(this System.ReadOnlySpan<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static bool StartsWith<T>(this System.Span<T> span, System.ReadOnlySpan<T> value) where T : System.IEquatable<T> { throw null; }
        public static bool TryGetString(this System.ReadOnlyMemory<char> readOnlyMemory, out string text, out int start, out int length) { throw null; }
    }
    public readonly partial struct Memory<T>
    {
        private readonly object _dummy;
        public Memory(T[] array) { throw null; }
        public Memory(T[] array, int start, int length) { throw null; }
        public static System.Memory<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public System.Span<T> Span { get { throw null; } }
        public void CopyTo(System.Memory<T> destination) { }
        public bool Equals(System.Memory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override bool Equals(object obj) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.Memory<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (System.Memory<T> memory) { throw null; }
        public static implicit operator System.Memory<T> (T[] array) { throw null; }
        public System.Buffers.MemoryHandle Retain(bool pin=false) { throw null; }
        public System.Memory<T> Slice(int start) { throw null; }
        public System.Memory<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Memory<T> destination) { throw null; }
        public bool TryGetArray(out System.ArraySegment<T> arraySegment) { throw null; }
    }
    public readonly partial struct ReadOnlyMemory<T>
    {
        private readonly object _dummy;
        public ReadOnlyMemory(T[] array) { throw null; }
        public ReadOnlyMemory(T[] array, int start, int length) { throw null; }
        public static System.ReadOnlyMemory<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public System.ReadOnlySpan<T> Span { get { throw null; } }
        public void CopyTo(System.Memory<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override bool Equals(object obj) { throw null; }
        public bool Equals(System.ReadOnlyMemory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public override int GetHashCode() { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlyMemory<T> (T[] array) { throw null; }
        public System.Buffers.MemoryHandle Retain(bool pin=false) { throw null; }
        public System.ReadOnlyMemory<T> Slice(int start) { throw null; }
        public System.ReadOnlyMemory<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Memory<T> destination) { throw null; }
    }
    public readonly ref partial struct ReadOnlySpan<T>
    {
        private readonly object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe ReadOnlySpan(void* pointer, int length) { throw null; }
        public ReadOnlySpan(T[] array) { throw null; }
        public ReadOnlySpan(T[] array, int start, int length) { throw null; }
        public static System.ReadOnlySpan<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref readonly T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void CopyTo(System.Span<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
        public override bool Equals(object obj) { throw null; }
        public System.ReadOnlySpan<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (T[] array) { throw null; }
        public static bool operator !=(System.ReadOnlySpan<T> left, System.ReadOnlySpan<T> right) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start) { throw null; }
        public System.ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            public ref readonly T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
    public readonly ref partial struct Span<T>
    {
        private readonly object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe Span(void* pointer, int length) { throw null; }
        public Span(T[] array) { throw null; }
        public Span(T[] array, int start, int length) { throw null; }
        public static System.Span<T> Empty { get { throw null; } }
        public bool IsEmpty { get { throw null; } }
        public ref T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void Clear() { }
        public void CopyTo(System.Span<T> destination) { }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        public static System.Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("Equals() on Span will always throw an exception. Use == instead.")]
        public override bool Equals(object obj) { throw null; }
        public void Fill(T value) { }
        public System.Span<T>.Enumerator GetEnumerator() { throw null; }
        [System.ComponentModel.EditorBrowsableAttribute((System.ComponentModel.EditorBrowsableState)(1))]
        [System.ObsoleteAttribute("GetHashCode() on Span will always throw an exception.")]
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.Span<T> left, System.Span<T> right) { throw null; }
        public static implicit operator System.Span<T> (System.ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator System.ReadOnlySpan<T> (System.Span<T> span) { throw null; }
        public static implicit operator System.Span<T> (T[] array) { throw null; }
        public static bool operator !=(System.Span<T> left, System.Span<T> right) { throw null; }
        public System.Span<T> Slice(int start) { throw null; }
        public System.Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(System.Span<T> destination) { throw null; }
        public ref partial struct Enumerator
        {
            private object _dummy;
            public ref T Current { get { throw null; } }
            public bool MoveNext() { throw null; }
        }
    }
}
namespace System.Buffers
{
    public partial interface IRetainable
    {
        bool Release();
        void Retain();
    }
    public partial struct MemoryHandle : System.IDisposable
    {
        private object _dummy;
        [System.CLSCompliantAttribute(false)]
        public unsafe MemoryHandle(System.Buffers.IRetainable retainable, void* pointer=null, System.Runtime.InteropServices.GCHandle handle=default(System.Runtime.InteropServices.GCHandle)) { throw null; }
        public bool HasPointer { get { throw null; } }
        [System.CLSCompliantAttribute(false)]
        public unsafe void* Pointer { get { throw null; } }
        public void Dispose() { }
    }
    public enum OperationStatus
    {
        DestinationTooSmall = 1,
        Done = 0,
        InvalidData = 3,
        NeedMoreData = 2,
    }
    public abstract partial class OwnedMemory<T> : System.Buffers.IRetainable, System.IDisposable
    {
        protected OwnedMemory() { }
        public abstract bool IsDisposed { get; }
        protected abstract bool IsRetained { get; }
        public abstract int Length { get; }
        public System.Memory<T> Memory { get { throw null; } }
        public abstract System.Span<T> Span { get; }
        public void Dispose() { }
        protected abstract void Dispose(bool disposing);
        public abstract System.Buffers.MemoryHandle Pin();
        public abstract bool Release();
        public abstract void Retain();
        protected internal abstract bool TryGetArray(out System.ArraySegment<T> arraySegment);
    }
    public readonly partial struct StandardFormat : System.IEquatable<System.Buffers.StandardFormat>
    {
        private readonly int _dummy;
        public const byte MaxPrecision = (byte)99;
        public const byte NoPrecision = (byte)255;
        public StandardFormat(char symbol, byte precision=(byte)255) { throw null; }
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
    }
}
namespace System.Buffers.Binary
{
    public static partial class BinaryPrimitives
    {
        public static short ReadInt16BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static short ReadInt16LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static int ReadInt32BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static int ReadInt32LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static long ReadInt64BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static long ReadInt64LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        public static T ReadMachineEndian<T>(System.ReadOnlySpan<byte> buffer) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ReadUInt16BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ushort ReadUInt16LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ReadUInt32BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static uint ReadUInt32LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ReadUInt64BigEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static ulong ReadUInt64LittleEndian(System.ReadOnlySpan<byte> buffer) { throw null; }
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
        public static bool TryReadInt16BigEndian(System.ReadOnlySpan<byte> buffer, out short value) { throw null; }
        public static bool TryReadInt16LittleEndian(System.ReadOnlySpan<byte> buffer, out short value) { throw null; }
        public static bool TryReadInt32BigEndian(System.ReadOnlySpan<byte> buffer, out int value) { throw null; }
        public static bool TryReadInt32LittleEndian(System.ReadOnlySpan<byte> buffer, out int value) { throw null; }
        public static bool TryReadInt64BigEndian(System.ReadOnlySpan<byte> buffer, out long value) { throw null; }
        public static bool TryReadInt64LittleEndian(System.ReadOnlySpan<byte> buffer, out long value) { throw null; }
        public static bool TryReadMachineEndian<T>(System.ReadOnlySpan<byte> buffer, out T value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt16BigEndian(System.ReadOnlySpan<byte> buffer, out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt16LittleEndian(System.ReadOnlySpan<byte> buffer, out ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt32BigEndian(System.ReadOnlySpan<byte> buffer, out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt32LittleEndian(System.ReadOnlySpan<byte> buffer, out uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt64BigEndian(System.ReadOnlySpan<byte> buffer, out ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryReadUInt64LittleEndian(System.ReadOnlySpan<byte> buffer, out ulong value) { throw null; }
        public static bool TryWriteInt16BigEndian(System.Span<byte> buffer, short value) { throw null; }
        public static bool TryWriteInt16LittleEndian(System.Span<byte> buffer, short value) { throw null; }
        public static bool TryWriteInt32BigEndian(System.Span<byte> buffer, int value) { throw null; }
        public static bool TryWriteInt32LittleEndian(System.Span<byte> buffer, int value) { throw null; }
        public static bool TryWriteInt64BigEndian(System.Span<byte> buffer, long value) { throw null; }
        public static bool TryWriteInt64LittleEndian(System.Span<byte> buffer, long value) { throw null; }
        public static bool TryWriteMachineEndian<T>(System.Span<byte> buffer, ref T value) where T : struct { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt16BigEndian(System.Span<byte> buffer, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt16LittleEndian(System.Span<byte> buffer, ushort value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt32BigEndian(System.Span<byte> buffer, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt32LittleEndian(System.Span<byte> buffer, uint value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt64BigEndian(System.Span<byte> buffer, ulong value) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryWriteUInt64LittleEndian(System.Span<byte> buffer, ulong value) { throw null; }
        public static void WriteInt16BigEndian(System.Span<byte> buffer, short value) { }
        public static void WriteInt16LittleEndian(System.Span<byte> buffer, short value) { }
        public static void WriteInt32BigEndian(System.Span<byte> buffer, int value) { }
        public static void WriteInt32LittleEndian(System.Span<byte> buffer, int value) { }
        public static void WriteInt64BigEndian(System.Span<byte> buffer, long value) { }
        public static void WriteInt64LittleEndian(System.Span<byte> buffer, long value) { }
        public static void WriteMachineEndian<T>(System.Span<byte> buffer, ref T value) where T : struct { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt16BigEndian(System.Span<byte> buffer, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt16LittleEndian(System.Span<byte> buffer, ushort value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt32BigEndian(System.Span<byte> buffer, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt32LittleEndian(System.Span<byte> buffer, uint value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt64BigEndian(System.Span<byte> buffer, ulong value) { }
        [System.CLSCompliantAttribute(false)]
        public static void WriteUInt64LittleEndian(System.Span<byte> buffer, ulong value) { }
    }
}
namespace System.Buffers.Text
{
    public static partial class Base64
    {
        public static System.Buffers.OperationStatus DecodeFromUtf8(System.ReadOnlySpan<byte> utf8, System.Span<byte> bytes, out int consumed, out int written, bool isFinalBlock=true) { throw null; }
        public static System.Buffers.OperationStatus DecodeFromUtf8InPlace(System.Span<byte> buffer, out int written) { throw null; }
        public static System.Buffers.OperationStatus EncodeToUtf8(System.ReadOnlySpan<byte> bytes, System.Span<byte> utf8, out int consumed, out int written, bool isFinalBlock=true) { throw null; }
        public static System.Buffers.OperationStatus EncodeToUtf8InPlace(System.Span<byte> buffer, int dataLength, out int written) { throw null; }
        public static int GetMaxDecodedFromUtf8Length(int length) { throw null; }
        public static int GetMaxEncodedToUtf8Length(int length) { throw null; }
    }
    public static partial class Utf8Formatter
    {
        public static bool TryFormat(bool value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(byte value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.DateTime value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.DateTimeOffset value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(decimal value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(double value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.Guid value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(short value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(int value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(long value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(sbyte value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(float value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        public static bool TryFormat(System.TimeSpan value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(ushort value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(uint value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryFormat(ulong value, System.Span<byte> buffer, out int bytesWritten, System.Buffers.StandardFormat format=default(System.Buffers.StandardFormat)) { throw null; }
    }
    public static partial class Utf8Parser
    {
        public static bool TryParse(System.ReadOnlySpan<byte> text, out bool value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out byte value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out System.DateTime value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out System.DateTimeOffset value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out decimal value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out double value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out System.Guid value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out short value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out int value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out long value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> text, out sbyte value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out float value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        public static bool TryParse(System.ReadOnlySpan<byte> text, out System.TimeSpan value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> text, out ushort value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> text, out uint value, out int bytesConsumed, char standardFormat='\0') { throw null; }
        [System.CLSCompliantAttribute(false)]
        public static bool TryParse(System.ReadOnlySpan<byte> text, out ulong value, out int bytesConsumed, char standardFormat='\0') { throw null; }
    }
}
namespace System.Runtime.InteropServices
{
    public static partial class MemoryMarshal
    {
        public static System.Memory<T> AsMemory<T>(System.ReadOnlyMemory<T> readOnlyMemory) { throw null; }
        public static ref T GetReference<T>(System.ReadOnlySpan<T> span) { throw null; }
        public static ref T GetReference<T>(System.Span<T> span) { throw null; }
        public static bool TryGetArray<T>(System.ReadOnlyMemory<T> readOnlyMemory, out System.ArraySegment<T> arraySegment) { throw null; }
    }
}
