// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*============================================================
**
**
**
**
** Purpose: Provides a fast, AV free, cross-language way of
**          accessing unmanaged memory in a random fashion.
**
**
===========================================================*/

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;

namespace System.IO
{
    /// Perf notes: ReadXXX, WriteXXX (for basic types) acquire and release the
    /// SafeBuffer pointer rather than relying on generic Read(T) from SafeBuffer because
    /// this gives better throughput; benchmarks showed about 12-15% better.
    public class UnmanagedMemoryAccessor : IDisposable
    {
        private SafeBuffer _buffer = null!; // initialized in helper called by ctor, but also not initialized by protected ctor
        private long _offset;
        private long _capacity;
        private FileAccess _access;
        private bool _isOpen;
        private bool _canRead;
        private bool _canWrite;

        protected UnmanagedMemoryAccessor()
        {
        }

        public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity)
        {
            Initialize(buffer, offset, capacity, FileAccess.Read);
        }

        public UnmanagedMemoryAccessor(SafeBuffer buffer, long offset, long capacity, FileAccess access)
        {
            Initialize(buffer, offset, capacity, access);
        }

        protected void Initialize(SafeBuffer buffer, long offset, long capacity, FileAccess access)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.ByteLength < (ulong)(offset + capacity))
            {
                throw new ArgumentException(SR.Argument_OffsetAndCapacityOutOfBounds);
            }
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }

            if (_isOpen)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
            }

            unsafe
            {
                byte* pointer = null;

                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if (((byte*)((long)pointer + offset + capacity)) < pointer)
                    {
                        throw new ArgumentException(SR.Argument_UnmanagedMemAccessorWrapAround);
                    }
                }
                finally
                {
                    if (pointer != null)
                    {
                        buffer.ReleasePointer();
                    }
                }
            }

            _offset = offset;
            _buffer = buffer;
            _capacity = capacity;
            _access = access;
            _isOpen = true;
            _canRead = (_access & FileAccess.Read) != 0;
            _canWrite = (_access & FileAccess.Write) != 0;
        }

        public long Capacity => _capacity;

        public bool CanRead => _isOpen && _canRead;

        public bool CanWrite => _isOpen && _canWrite;

        protected virtual void Dispose(bool disposing)
        {
            _isOpen = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsOpen => _isOpen;

        // ************** Read Methods ****************/

        public bool ReadBoolean(long position) => ReadByte(position) != 0;

        public byte ReadByte(long position)
        {
            EnsureSafeToRead(position, sizeof(byte));

            byte result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = *((byte*)(pointer + _offset + position));
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
            return result;
        }

        public char ReadChar(long position) => unchecked((char)ReadInt16(position));

        public short ReadInt16(long position)
        {
            EnsureSafeToRead(position, sizeof(short));

            short result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<short>(pointer + _offset + position);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
            return result;
        }

        public int ReadInt32(long position)
        {
            EnsureSafeToRead(position, sizeof(int));

            int result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<int>(pointer + _offset + position);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
            return result;
        }

        public long ReadInt64(long position)
        {
            EnsureSafeToRead(position, sizeof(long));

            long result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<long>(pointer + _offset + position);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
            return result;
        }

        public decimal ReadDecimal(long position)
        {
            const int ScaleMask = 0x00FF0000;
            const int SignMask = unchecked((int)0x80000000);

            EnsureSafeToRead(position, sizeof(decimal));

            int lo, mid, hi, flags;

            unsafe
            {
                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    lo = Unsafe.ReadUnaligned<int>(pointer);
                    mid = Unsafe.ReadUnaligned<int>(pointer + 4);
                    hi = Unsafe.ReadUnaligned<int>(pointer + 8);
                    flags = Unsafe.ReadUnaligned<int>(pointer + 12);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }

            // Check for invalid Decimal values
            if (!((flags & ~(SignMask | ScaleMask)) == 0 && (flags & ScaleMask) <= (28 << 16)))
            {
                throw new ArgumentException(SR.Arg_BadDecimal); // Throw same Exception type as Decimal(int[]) ctor for compat
            }

            bool isNegative = (flags & SignMask) != 0;
            byte scale = (byte)(flags >> 16);

            return new decimal(lo, mid, hi, isNegative, scale);
        }

        public float ReadSingle(long position) => BitConverter.Int32BitsToSingle(ReadInt32(position));

        public double ReadDouble(long position) => BitConverter.Int64BitsToDouble(ReadInt64(position));

        [CLSCompliant(false)]
        public sbyte ReadSByte(long position) => unchecked((sbyte)ReadByte(position));

        [CLSCompliant(false)]
        public ushort ReadUInt16(long position) => unchecked((ushort)ReadInt16(position));

        [CLSCompliant(false)]
        public uint ReadUInt32(long position) => unchecked((uint)ReadInt32(position));

        [CLSCompliant(false)]
        public ulong ReadUInt64(long position) => unchecked((ulong)ReadInt64(position));

        // Reads a struct of type T from unmanaged memory, into the reference pointed to by ref value.
        // Note: this method is not safe, since it overwrites the contents of a structure, it can be
        // used to modify the private members of a struct.
        // This method is most performant when used with medium to large sized structs
        // (larger than 8 bytes -- though this is number is JIT and architecture dependent).   As
        // such, it is best to use the ReadXXX methods for small standard types such as ints, longs,
        // bools, etc.
        public void Read<T>(long position, out T structure) where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canRead)
            {
                throw new NotSupportedException(SR.NotSupported_Reading);
            }

            uint sizeOfT = SafeBuffer.SizeOf<T>();
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Argument_NotEnoughBytesToRead, nameof(position));
                }
            }

            structure = _buffer.Read<T>((ulong)(_offset + position));
        }

        // Reads 'count' structs of type T from unmanaged memory, into 'array' starting at 'offset'.
        // Note: this method is not safe, since it overwrites the contents of structures, it can
        // be used to modify the private members of a struct.
        public int ReadArray<T>(long position, T[] array, int offset, int count) where T : struct
        {
            ArgumentNullException.ThrowIfNull(array);

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (array.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canRead)
            {
                throw new NotSupportedException(SR.NotSupported_Reading);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            uint sizeOfT = SafeBuffer.AlignedSizeOf<T>();

            // only check position and ask for fewer Ts if count is too big
            if (position >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
            }

            int n = count;
            long spaceLeft = _capacity - position;
            if (spaceLeft < 0)
            {
                n = 0;
            }
            else
            {
                ulong spaceNeeded = (ulong)(sizeOfT * count);
                if ((ulong)spaceLeft < spaceNeeded)
                {
                    n = (int)(spaceLeft / sizeOfT);
                }
            }

            _buffer.ReadArray<T>((ulong)(_offset + position), array, offset, n);

            return n;
        }

        // ************** Write Methods ****************/

        public void Write(long position, bool value) => Write(position, (byte)(value ? 1 : 0));

        public void Write(long position, byte value)
        {
            EnsureSafeToWrite(position, sizeof(byte));

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    *((byte*)(pointer + _offset + position)) = value;
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(long position, char value) => Write(position, unchecked((short)value));

        public void Write(long position, short value)
        {
            EnsureSafeToWrite(position, sizeof(short));

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<short>(pointer + _offset + position, value);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(long position, int value)
        {
            EnsureSafeToWrite(position, sizeof(int));

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<int>(pointer + _offset + position, value);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(long position, long value)
        {
            EnsureSafeToWrite(position, sizeof(long));

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<long>(pointer + _offset + position, value);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(long position, decimal value)
        {
            EnsureSafeToWrite(position, sizeof(decimal));

            Span<int> bits = stackalloc int[4];
            decimal.TryGetBits(value, bits, out int intsWritten);
            Debug.Assert(intsWritten == 4);

            unsafe
            {
                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    Unsafe.WriteUnaligned<int>(pointer, bits[0]);
                    Unsafe.WriteUnaligned<int>(pointer + 4, bits[1]);
                    Unsafe.WriteUnaligned<int>(pointer + 8, bits[2]);
                    Unsafe.WriteUnaligned<int>(pointer + 12, bits[3]);
                }
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        public void Write(long position, float value) => Write(position, BitConverter.SingleToInt32Bits(value));

        public void Write(long position, double value) => Write(position, BitConverter.DoubleToInt64Bits(value));

        [CLSCompliant(false)]
        public void Write(long position, sbyte value) => Write(position, unchecked((byte)value));

        [CLSCompliant(false)]
        public void Write(long position, ushort value) => Write(position, unchecked((short)value));

        [CLSCompliant(false)]
        public void Write(long position, uint value) => Write(position, unchecked((int)value));

        [CLSCompliant(false)]
        public void Write(long position, ulong value) => Write(position, unchecked((long)value));

        // Writes the struct pointed to by ref value into unmanaged memory.  Note that this method
        // is most performant when used with medium to large sized structs (larger than 8 bytes
        // though this is number is JIT and architecture dependent).   As such, it is best to use
        // the WriteX methods for small standard types such as ints, longs, bools, etc.
        public void Write<T>(long position, ref T structure) where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }

            uint sizeOfT = SafeBuffer.SizeOf<T>();
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Argument_NotEnoughBytesToWrite, nameof(position));
                }
            }

            _buffer.Write<T>((ulong)(_offset + position), structure);
        }

        // Writes 'count' structs of type T from 'array' (starting at 'offset') into unmanaged memory.
        public void WriteArray<T>(long position, T[] array, int offset, int count) where T : struct
        {
            ArgumentNullException.ThrowIfNull(array);

            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (array.Length - offset < count)
            {
                throw new ArgumentException(SR.Argument_InvalidOffLen);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (position >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
            }

            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }

            _buffer.WriteArray<T>((ulong)(_offset + position), array, offset, count);
        }

        private void EnsureSafeToRead(long position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canRead)
            {
                throw new NotSupportedException(SR.NotSupported_Reading);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Argument_NotEnoughBytesToRead, nameof(position));
                }
            }
        }

        private void EnsureSafeToWrite(long position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException(nameof(UnmanagedMemoryAccessor), SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!_canWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Argument_NotEnoughBytesToWrite, nameof(position));
                }
            }
        }
    }
}
