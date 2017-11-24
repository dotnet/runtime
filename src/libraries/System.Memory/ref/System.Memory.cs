// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public readonly ref struct ReadOnlySpan<T>
    {
        public static ReadOnlySpan<T> Empty { get { throw null; } }
        public ReadOnlySpan(T[] array) { throw null; }
        public ReadOnlySpan(T[] array, int start, int length) { throw null; }
        [CLSCompliant(false)]
        public unsafe ReadOnlySpan(void* pointer, int length) { throw null; }
        public bool IsEmpty { get { throw null; } }
        public T this[int index] { get { throw null; }}
        public int Length { get { throw null; } }
        public void CopyTo(Span<T> destination) { }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static ReadOnlySpan<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T DangerousGetPinnableReference() { throw null; }
#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
        [System.ObsoleteAttribute("Equals() on ReadOnlySpan will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        public Enumerator GetEnumerator() { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on ReadOnlySpan will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
#pragma warning restore 0809
        public static bool operator ==(ReadOnlySpan<T> left, ReadOnlySpan<T> right) { throw null; }
        public static implicit operator ReadOnlySpan<T> (T[] array) { throw null; }
        public static implicit operator ReadOnlySpan<T> (ArraySegment<T> arraySegment) { throw null; }
        public static bool operator !=(ReadOnlySpan<T> left, ReadOnlySpan<T> right) { throw null; }
        public ReadOnlySpan<T> Slice(int start) { throw null; }
        public ReadOnlySpan<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(Span<T> destination) { throw null; }
        public ref struct Enumerator
        {
            public bool MoveNext() { throw null; }
            public ref readonly T Current { get { throw null; } }
        }
    }

    public readonly ref struct Span<T>
    {
        public static Span<T> Empty { get { throw null; } }
        public Span(T[] array) { throw null; }
        public Span(T[] array, int start, int length) { throw null; }
        [CLSCompliant(false)]
        public unsafe Span(void* pointer, int length) { throw null; }
        public bool IsEmpty { get { throw null; } }
        public ref T this[int index] { get { throw null; } }
        public int Length { get { throw null; } }
        public void Clear() { }
        public void Fill(T value) { }
        public void CopyTo(Span<T> destination) { }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public static Span<T> DangerousCreate(object obj, ref T objectData, int length) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public ref T DangerousGetPinnableReference() { throw null; }
#pragma warning disable 0809  //warning CS0809: Obsolete member 'Span<T>.Equals(object)' overrides non-obsolete member 'object.Equals(object)'
        [System.ObsoleteAttribute("Equals() on Span will always throw an exception. Use == instead.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        public Enumerator GetEnumerator() { throw null; }
        [System.ObsoleteAttribute("GetHashCode() on Span will always throw an exception.")]
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
#pragma warning restore 0809
        public static bool operator ==(Span<T> left, Span<T> right) { throw null; }
        public static implicit operator Span<T> (T[] array) { throw null; }
        public static implicit operator Span<T> (ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator ReadOnlySpan<T> (Span<T> span) { throw null; }
        public static bool operator !=(Span<T> left, Span<T> right) { throw null; }
        public Span<T> Slice(int start) { throw null; }
        public Span<T> Slice(int start, int length) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryCopyTo(Span<T> destination) { throw null; }
        public ref struct Enumerator
        {
            public bool MoveNext() { throw null; }
            public ref T Current { get { throw null; } }
        }
    }
    
    public static class MemoryExtensions
    {
        public static int IndexOf<T>(this Span<T> span, T value) where T : IEquatable<T> { throw null; }
        public static int IndexOf<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T> { throw null; }

        public static int IndexOfAny(this Span<byte> span, byte value0, byte value1) { throw null; }
        public static int IndexOfAny(this Span<byte> span, byte value0, byte value1, byte value2) { throw null; }
        public static int IndexOfAny(this Span<byte> span, ReadOnlySpan<byte> values) { throw null; }

        public static bool SequenceEqual<T>(this Span<T> first, ReadOnlySpan<T> second) where T : IEquatable<T> { throw null; }
        
        public static bool StartsWith<T>(this Span<T> span, ReadOnlySpan<T> value) where T : IEquatable<T> { throw null; }

        public static Span<byte> AsBytes<T>(this Span<T> source) where T : struct { throw null; }

        public static Span<TTo> NonPortableCast<TFrom, TTo>(this Span<TFrom> source) where TFrom : struct where TTo : struct { throw null; }
        
        public static ReadOnlySpan<char> AsReadOnlySpan(this string text) { throw null; }
        public static ReadOnlyMemory<char> AsReadOnlyMemory(this string text) { throw null; }

        public static Span<T> AsSpan<T>(this T[] array) { throw null; }
        public static Span<T> AsSpan<T>(this ArraySegment<T> arraySegment) { throw null; }
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] array) { throw null; }
        public static ReadOnlySpan<T> AsReadOnlySpan<T>(this ArraySegment<T> arraySegment) { throw null; }

        public static void CopyTo<T>(this T[] array, Span<T> destination) { throw null; }
        public static void CopyTo<T>(this T[] array, Memory<T> destination) { throw null; }

        public static int IndexOf<T>(this ReadOnlySpan<T> span, T value) where T : IEquatable<T> { throw null; }
        public static int IndexOf<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T> { throw null; }

        public static int IndexOfAny(this ReadOnlySpan<byte> span, byte value0, byte value1) { throw null; }
        public static int IndexOfAny(this ReadOnlySpan<byte> span, byte value0, byte value1, byte value2) { throw null; }
        public static int IndexOfAny(this ReadOnlySpan<byte> span, ReadOnlySpan<byte> values) { throw null; }

        public static bool SequenceEqual<T>(this ReadOnlySpan<T> first, ReadOnlySpan<T> second) where T : IEquatable<T> { throw null; }

        public static bool StartsWith<T>(this ReadOnlySpan<T> span, ReadOnlySpan<T> value) where T : IEquatable<T> { throw null; }

        public static ReadOnlySpan<byte> AsBytes<T>(this ReadOnlySpan<T> source) where T : struct { throw null; }
        
        public static ReadOnlySpan<TTo> NonPortableCast<TFrom, TTo>(this ReadOnlySpan<TFrom> source) where TFrom : struct where TTo : struct { throw null; }

        public static bool TryGetString(this ReadOnlyMemory<char> readOnlyMemory, out string text, out int start, out int length) { throw null; }
    }

    public readonly struct ReadOnlyMemory<T>
    {
        public static ReadOnlyMemory<T> Empty { get { throw null; } }
        public ReadOnlyMemory(T[] array) { throw null; }
        public ReadOnlyMemory(T[] array, int start, int length) { throw null; }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public void CopyTo(Memory<T> destination) { }
        public bool TryCopyTo(Memory<T> destination) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        public bool Equals(ReadOnlyMemory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static implicit operator ReadOnlyMemory<T>(T[] array) { throw null; }
        public static implicit operator ReadOnlyMemory<T>(ArraySegment<T> arraySegment) { throw null; }
        public ReadOnlyMemory<T> Slice(int start) { throw null; }
        public ReadOnlyMemory<T> Slice(int start, int length) { throw null; }
        public ReadOnlySpan<T> Span { get { throw null; } }
        public unsafe Buffers.MemoryHandle Retain(bool pin = false) { throw null; }
        public T[] ToArray() { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public bool DangerousTryGetArray(out ArraySegment<T> arraySegment) { throw null; }
    }

    public readonly struct Memory<T>
    {
        public static Memory<T> Empty { get { throw null; } }
        public Memory(T[] array) { throw null; }
        public Memory(T[] array, int start, int length) { throw null; }
        public bool IsEmpty { get { throw null; } }
        public int Length { get { throw null; } }
        public void CopyTo(Memory<T> destination) { }
        public bool TryCopyTo(Memory<T> destination) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override bool Equals(object obj) { throw null; }
        public bool Equals(Memory<T> other) { throw null; }
        [System.ComponentModel.EditorBrowsable(System.ComponentModel.EditorBrowsableState.Never)]
        public override int GetHashCode() { throw null; }
        public static implicit operator Memory<T>(T[] array) { throw null; }
        public static implicit operator Memory<T>(ArraySegment<T> arraySegment) { throw null; }
        public static implicit operator ReadOnlyMemory<T>(Memory<T> memory) { throw null; }
        public Memory<T> Slice(int start) { throw null; }
        public Memory<T> Slice(int start, int length) { throw null; }
        public Span<T> Span { get { throw null; } }
        public unsafe Buffers.MemoryHandle Retain(bool pin = false) { throw null; }
        public T[] ToArray() { throw null; }
        public bool TryGetArray(out ArraySegment<T> arraySegment) { throw null; }
    }
}

namespace System.Buffers
{
    public unsafe struct MemoryHandle : IDisposable 
    {
        [CLSCompliant(false)]
        public MemoryHandle(IRetainable owner, void* pointer = null,  System.Runtime.InteropServices.GCHandle handle = default(System.Runtime.InteropServices.GCHandle))  { throw null; }
        [CLSCompliant(false)]
        public void* Pointer { get { throw null; } }
        public bool HasPointer { get { throw null; } }
        public void Dispose() { throw null; }
    }

    public interface IRetainable 
    {
        bool Release();
        void Retain();
    }
    
    public abstract class OwnedMemory<T> : IDisposable, IRetainable 
    {
        public Memory<T> Memory { get { throw null; } }
        public abstract bool IsDisposed { get; }
        protected abstract bool IsRetained { get; }
        public abstract int Length { get; }
        public abstract Span<T> Span { get; }
        public void Dispose() { throw null; }
        protected abstract void Dispose(bool disposing);
        public abstract MemoryHandle Pin();
        public abstract bool Release();
        public abstract void Retain();
        protected internal abstract bool TryGetArray(out ArraySegment<T> arraySegment);
    }

    public enum OperationStatus
    {
        Done,
        DestinationTooSmall,
        NeedMoreData,
        InvalidData,
    }
}

namespace System.Buffers.Binary
{
    public static class BinaryPrimitives
    {
        [CLSCompliant(false)]
        public static sbyte ReverseEndianness(sbyte value) { throw null; }
        public static byte ReverseEndianness(byte value) { throw null; }
        public static short ReverseEndianness(short value) { throw null; }
        [CLSCompliant(false)]
        public static ushort ReverseEndianness(ushort value) { throw null; }
        public static int ReverseEndianness(int value) { throw null; }
        [CLSCompliant(false)]
        public static uint ReverseEndianness(uint value) { throw null; }
        public static long ReverseEndianness(long value) { throw null; }
        [CLSCompliant(false)]
        public static ulong ReverseEndianness(ulong value) { throw null; }

        public static T ReadMachineEndian<T>(ReadOnlySpan<byte> buffer) where T : struct { throw null; }
        public static bool TryReadMachineEndian<T>(ReadOnlySpan<byte> buffer, out T value) where T : struct { throw null; }

        public static short ReadInt16LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }
        public static int ReadInt32LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }
        public static long ReadInt64LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static ushort ReadUInt16LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static uint ReadUInt32LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static ulong ReadUInt64LittleEndian(ReadOnlySpan<byte> buffer) { throw null; }

        public static bool TryReadInt16LittleEndian(ReadOnlySpan<byte> buffer, out short value) { throw null; }
        public static bool TryReadInt32LittleEndian(ReadOnlySpan<byte> buffer, out int value) { throw null; }
        public static bool TryReadInt64LittleEndian(ReadOnlySpan<byte> buffer, out long value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt16LittleEndian(ReadOnlySpan<byte> buffer, out ushort value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt32LittleEndian(ReadOnlySpan<byte> buffer, out uint value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt64LittleEndian(ReadOnlySpan<byte> buffer, out ulong value) { throw null; }

        public static short ReadInt16BigEndian(ReadOnlySpan<byte> buffer) { throw null; }
        public static int ReadInt32BigEndian(ReadOnlySpan<byte> buffer) { throw null; }
        public static long ReadInt64BigEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static ushort ReadUInt16BigEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static uint ReadUInt32BigEndian(ReadOnlySpan<byte> buffer) { throw null; }
        [CLSCompliant(false)]
        public static ulong ReadUInt64BigEndian(ReadOnlySpan<byte> buffer) { throw null; }

        public static bool TryReadInt16BigEndian(ReadOnlySpan<byte> buffer, out short value) { throw null; }
        public static bool TryReadInt32BigEndian(ReadOnlySpan<byte> buffer, out int value) { throw null; }
        public static bool TryReadInt64BigEndian(ReadOnlySpan<byte> buffer, out long value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt16BigEndian(ReadOnlySpan<byte> buffer, out ushort value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt32BigEndian(ReadOnlySpan<byte> buffer, out uint value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryReadUInt64BigEndian(ReadOnlySpan<byte> buffer, out ulong value) { throw null; }

        public static void WriteMachineEndian<T>(Span<byte> buffer, ref T value) where T : struct { throw null; }
        public static bool TryWriteMachineEndian<T>(Span<byte> buffer, ref T value) where T : struct { throw null; }

        public static void WriteInt16LittleEndian(Span<byte> buffer, short value) { throw null; }
        public static void WriteInt32LittleEndian(Span<byte> buffer, int value) { throw null; }
        public static void WriteInt64LittleEndian(Span<byte> buffer, long value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt16LittleEndian(Span<byte> buffer, ushort value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt32LittleEndian(Span<byte> buffer, uint value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt64LittleEndian(Span<byte> buffer, ulong value) { throw null; }

        public static bool TryWriteInt16LittleEndian(Span<byte> buffer, short value) { throw null; }
        public static bool TryWriteInt32LittleEndian(Span<byte> buffer, int value) { throw null; }
        public static bool TryWriteInt64LittleEndian(Span<byte> buffer, long value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt16LittleEndian(Span<byte> buffer, ushort value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt32LittleEndian(Span<byte> buffer, uint value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt64LittleEndian(Span<byte> buffer, ulong value) { throw null; }

        public static void WriteInt16BigEndian(Span<byte> buffer, short value) { throw null; }
        public static void WriteInt32BigEndian(Span<byte> buffer, int value) { throw null; }
        public static void WriteInt64BigEndian(Span<byte> buffer, long value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt16BigEndian(Span<byte> buffer, ushort value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt32BigEndian(Span<byte> buffer, uint value) { throw null; }
        [CLSCompliant(false)]
        public static void WriteUInt64BigEndian(Span<byte> buffer, ulong value) { throw null; }

        public static bool TryWriteInt16BigEndian(Span<byte> buffer, short value) { throw null; }
        public static bool TryWriteInt32BigEndian(Span<byte> buffer, int value) { throw null; }
        public static bool TryWriteInt64BigEndian(Span<byte> buffer, long value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt16BigEndian(Span<byte> buffer, ushort value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt32BigEndian(Span<byte> buffer, uint value) { throw null; }
        [CLSCompliant(false)]
        public static bool TryWriteUInt64BigEndian(Span<byte> buffer, ulong value) { throw null; }
    }
}

namespace System.Buffers
{
    public readonly struct StandardFormat : IEquatable<StandardFormat>
    {
        public const byte MaxPrecision = (byte)99;
        public const byte NoPrecision = (byte)255;
        public StandardFormat(char symbol, byte precision=(byte)255) => throw null;
        public bool HasPrecision => throw null;
        public bool IsDefault => throw null;
        public byte Precision => throw null;
        public char Symbol => throw null;
        public static implicit operator StandardFormat (char symbol) => throw null;
        public static StandardFormat Parse(ReadOnlySpan<char> format) => throw null;
        public static StandardFormat Parse(string format) => throw null;
        public override bool Equals(object obj) => throw null;
        public override int GetHashCode() => throw null;
        public bool Equals(StandardFormat other) => throw null;
        public static bool operator ==(StandardFormat left, StandardFormat right) => throw null;
        public static bool operator !=(StandardFormat left, StandardFormat right) => throw null;
    }
}

namespace System.Buffers.Text
{
    public static class Base64
    {
        public static OperationStatus EncodeToUtf8(ReadOnlySpan<byte> bytes, Span<byte> utf8, out int consumed, out int written, bool isFinalBlock = true) { throw null; }
        public static OperationStatus EncodeToUtf8InPlace(Span<byte> buffer, int dataLength, out int written) { throw null; }
        public static int GetMaxEncodedToUtf8Length(int length) { throw null; }
        public static OperationStatus DecodeFromUtf8(ReadOnlySpan<byte> utf8, Span<byte> bytes, out int consumed, out int written, bool isFinalBlock = true) { throw null; }
        public static OperationStatus DecodeFromUtf8InPlace(Span<byte> buffer, out int written) { throw null; }
        public static int GetMaxDecodedFromUtf8Length(int length) { throw null; }
    }

    public static class Utf8Formatter
    {
        public static bool TryFormat(bool value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(byte value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(DateTime value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(DateTimeOffset value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(decimal value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(double value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(Guid value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(short value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(int value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(long value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryFormat(sbyte value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(float value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        public static bool TryFormat(TimeSpan value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryFormat(ushort value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryFormat(uint value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryFormat(ulong value, Span<byte> buffer, out int bytesWritten, StandardFormat format = default) => throw null;
    }
    public static class Utf8Parser
    {
        public static bool TryParse(ReadOnlySpan<byte> text, out bool value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out byte value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out DateTime value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out DateTimeOffset value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out decimal value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out double value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out Guid value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out short value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out int value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out long value, out int bytesConsumed, char standardFormat = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> text, out sbyte value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out float value, out int bytesConsumed, char standardFormat = default) => throw null;
        public static bool TryParse(ReadOnlySpan<byte> text, out TimeSpan value, out int bytesConsumed, char standardFormat = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> text, out ushort value, out int bytesConsumed, char standardFormat = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> text, out uint value, out int bytesConsumed, char standardFormat = default) => throw null;
        [CLSCompliant(false)]
        public static bool TryParse(ReadOnlySpan<byte> text, out ulong value, out int bytesConsumed, char standardFormat = default) => throw null;
    }
}

namespace System.Runtime.InteropServices
{
    public static class MemoryMarshal
    {
        public static Memory<T> AsMemory<T>(ReadOnlyMemory<T> readOnlyMemory) { throw null; }
    }
}
