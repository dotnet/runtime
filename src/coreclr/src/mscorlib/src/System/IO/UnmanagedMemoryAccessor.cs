// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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

using System;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using System.Runtime.ConstrainedExecution;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;
using System.Diagnostics;
using System.Diagnostics.Contracts;

namespace System.IO
{
    /// Perf notes: ReadXXX, WriteXXX (for basic types) acquire and release the 
    /// SafeBuffer pointer rather than relying on generic Read(T) from SafeBuffer because
    /// this gives better throughput; benchmarks showed about 12-15% better.
    public class UnmanagedMemoryAccessor : IDisposable
    {
        private SafeBuffer _buffer;
        private Int64 _offset;
        [ContractPublicPropertyName("Capacity")]
        private Int64 _capacity;
        private FileAccess _access;
        private bool _isOpen;
        private bool _canRead;
        private bool _canWrite;

        protected UnmanagedMemoryAccessor()
        {
            _isOpen = false;
        }

        #region SafeBuffer ctors and initializers
        // <SecurityKernel Critical="True" Ring="1">
        // <ReferencesCritical Name="Method: Initialize(SafeBuffer, Int64, Int64, FileAccess):Void" Ring="1" />
        // </SecurityKernel>
        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity)
        {
            Initialize(buffer, offset, capacity, FileAccess.Read);
        }

        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity, FileAccess access)
        {
            Initialize(buffer, offset, capacity, access);
        }

        protected void Initialize(SafeBuffer buffer, Int64 offset, Int64 capacity, FileAccess access)
        {
            if (buffer == null)
            {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (offset < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(offset), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (buffer.ByteLength < (UInt64)(offset + capacity))
            {
                throw new ArgumentException(SR.Argument_OffsetAndCapacityOutOfBounds);
            }
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }
            Contract.EndContractBlock();

            if (_isOpen)
            {
                throw new InvalidOperationException(SR.InvalidOperation_CalledTwice);
            }

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    buffer.AcquirePointer(ref pointer);
                    if (((byte*)((Int64)pointer + offset + capacity)) < pointer)
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

        #endregion

        public Int64 Capacity
        {
            get
            {
                return _capacity;
            }
        }

        public bool CanRead
        {
            get
            {
                return _isOpen && _canRead;
            }
        }

        public bool CanWrite
        {
            get
            {
                return _isOpen && _canWrite;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            _isOpen = false;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected bool IsOpen
        {
            get { return _isOpen; }
        }

        public bool ReadBoolean(Int64 position)
        {
            int sizeOfType = sizeof(bool);
            EnsureSafeToRead(position, sizeOfType);

            byte b = InternalReadByte(position);
            return b != 0;
        }

        public byte ReadByte(Int64 position)
        {
            int sizeOfType = sizeof(byte);
            EnsureSafeToRead(position, sizeOfType);

            return InternalReadByte(position);
        }

        public char ReadChar(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(char));

            char result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<char>(pointer + _offset + position);
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

        // See comment above.
        public Int16 ReadInt16(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(Int16));

            Int16 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<Int16>(pointer + _offset + position);
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


        public Int32 ReadInt32(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(Int32));

            Int32 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<Int32>(pointer + _offset + position);
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

        public Int64 ReadInt64(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(Int64));

            Int64 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<Int64>(pointer + _offset + position);
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

        public Decimal ReadDecimal(Int64 position)
        {
            const int ScaleMask = 0x00FF0000;
            const int SignMask = unchecked((int)0x80000000);

            EnsureSafeToRead(position, sizeof(Decimal));

            int lo, mid, hi, flags;

            unsafe
            {
                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    
                    lo = Unsafe.ReadUnaligned<Int32>(pointer + _offset + position);
                    mid = Unsafe.ReadUnaligned<Int32>(pointer + _offset + position + 4);
                    hi = Unsafe.ReadUnaligned<Int32>(pointer + _offset + position + 8);
                    flags = Unsafe.ReadUnaligned<Int32>(pointer + _offset + position + 12);
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

        public Single ReadSingle(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(Single));

            Single result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<Single>(pointer + _offset + position);
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

        public Double ReadDouble(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(Double));

            Double result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<Double>(pointer + _offset + position);
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

        [CLSCompliant(false)]
        public SByte ReadSByte(Int64 position)
        {
            int sizeOfType = sizeof(SByte);
            EnsureSafeToRead(position, sizeOfType);

            SByte result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = *((SByte*)(pointer + _offset + position));
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

        [CLSCompliant(false)]
        public UInt16 ReadUInt16(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(UInt16));

            UInt16 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<UInt16>(pointer + _offset + position);
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

        [CLSCompliant(false)]
        public UInt32 ReadUInt32(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(UInt32));

            UInt32 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<UInt32>(pointer + _offset + position);
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

        [CLSCompliant(false)]
        public UInt64 ReadUInt64(Int64 position)
        {
            EnsureSafeToRead(position, sizeof(UInt64));

            UInt64 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = Unsafe.ReadUnaligned<UInt64>(pointer + _offset + position);
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

        // Reads a struct of type T from unmanaged memory, into the reference pointed to by ref value.  
        // Note: this method is not safe, since it overwrites the contents of a structure, it can be 
        // used to modify the private members of a struct.  Furthermore, using this with a struct that
        // contains reference members will most likely cause the runtime to AV.  Note, that despite 
        // various checks made by the C++ code used by Marshal.PtrToStructure, Marshal.PtrToStructure
        // will still overwrite privates and will also crash the runtime when used with structs 
        // containing reference members.  For this reason, I am sticking an UnmanagedCode requirement
        // on this method to match Marshal.PtrToStructure.

        // Alos note that this method is most performant when used with medium to large sized structs
        // (larger than 8 bytes -- though this is number is JIT and architecture dependent).   As 
        // such, it is best to use the ReadXXX methods for small standard types such as ints, longs, 
        // bools, etc.

        public void Read<T>(Int64 position, out T structure) where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_Reading);
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof(T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Argument_NotEnoughBytesToRead, typeof (T).FullName), nameof(position));
                }
            }

            structure = _buffer.Read<T>((UInt64)(_offset + position));
        }

        // Reads 'count' structs of type T from unmanaged memory, into 'array' starting at 'offset'.  
        // Note: this method is not safe, since it overwrites the contents of structures, it can 
        // be used to modify the private members of a struct.  Furthermore, using this with a 
        // struct that contains reference members will most likely cause the runtime to AV. This
        // is consistent with Marshal.PtrToStructure.

        public int ReadArray<T>(Int64 position, T[] array, Int32 offset, Int32 count) where T : struct
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Buffer cannot be null.");
            }
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
                throw new ArgumentException(SR.Argument_OffsetAndLengthOutOfBounds);
            }
            Contract.EndContractBlock();
            if (!CanRead)
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
                }
                else
                {
                    throw new NotSupportedException(SR.NotSupported_Reading);
                }
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            UInt32 sizeOfT = Marshal.AlignedSizeOf<T>();

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

            _buffer.ReadArray<T>((UInt64)(_offset + position), array, offset, n);

            return n;
        }

        // ************** Write Methods ****************/

        // The following 13 WriteXXX methods write a value of type XXX into unmanaged memory at 'positon'. 
        // The bounds of the unmanaged memory are checked against to ensure that there is enough 
        // space after 'position' to write a value of type XXX.  XXX can be a bool, byte, char, decimal, 
        // double, short, int, long, sbyte, float, ushort, uint, or ulong. 


        public void Write(Int64 position, bool value)
        {
            int sizeOfType = sizeof(bool);
            EnsureSafeToWrite(position, sizeOfType);

            byte b = (byte)(value ? 1 : 0);
            InternalWrite(position, b);
        }

        public void Write(Int64 position, byte value)
        {
            int sizeOfType = sizeof(byte);
            EnsureSafeToWrite(position, sizeOfType);

            InternalWrite(position, value);
        }

        public void Write(Int64 position, char value)
        {
            EnsureSafeToWrite(position, sizeof(char));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<char>(pointer + _offset + position, value);
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


        public void Write(Int64 position, Int16 value)
        {
            EnsureSafeToWrite(position, sizeof(Int16));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<Int16>(pointer + _offset + position, value);
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


        public void Write(Int64 position, Int32 value)
        {
            EnsureSafeToWrite(position, sizeof(Int32));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<Int32>(pointer + _offset + position, value);
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

        public void Write(Int64 position, Int64 value)
        {
            EnsureSafeToWrite(position, sizeof(Int64));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<Int64>(pointer + _offset + position, value);
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

        public void Write(Int64 position, Decimal value)
        {
            EnsureSafeToWrite(position, sizeof(Decimal));

            unsafe
            {
                int* valuePtr = (int*)(&value);
                int flags = *valuePtr;
                int hi = *(valuePtr + 1);
                int lo = *(valuePtr + 2);
                int mid = *(valuePtr + 3);

                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);

                    Unsafe.WriteUnaligned<Int32>(pointer + _offset + position, lo);
                    Unsafe.WriteUnaligned<Int32>(pointer + _offset + position + 4, mid);
                    Unsafe.WriteUnaligned<Int32>(pointer + _offset + position + 8, hi);
                    Unsafe.WriteUnaligned<Int32>(pointer + _offset + position + 12, flags);
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

        public void Write(Int64 position, Single value)
        {
            EnsureSafeToWrite(position, sizeof(Single));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<Single>(pointer + _offset + position, value);
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

        public void Write(Int64 position, Double value)
        {
            EnsureSafeToWrite(position, sizeof(Double));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<Double>(pointer + _offset + position, value);
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

        [CLSCompliant(false)]
        public void Write(Int64 position, SByte value)
        {
            EnsureSafeToWrite(position, sizeof(SByte));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    *((SByte*)(pointer + _offset + position)) = value;
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

        [CLSCompliant(false)]
        public void Write(Int64 position, UInt16 value)
        {
            int sizeOfType = sizeof(UInt16);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<UInt16>(pointer + _offset + position, value);
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

        [CLSCompliant(false)]
        public void Write(Int64 position, UInt32 value)
        {
            EnsureSafeToWrite(position, sizeof(UInt32));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<UInt32>(pointer + _offset + position, value);
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

        [CLSCompliant(false)]
        public void Write(Int64 position, UInt64 value)
        {
            EnsureSafeToWrite(position, sizeof(UInt64));

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    Unsafe.WriteUnaligned<UInt64>(pointer + _offset + position, value);
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

        // Writes the struct pointed to by ref value into unmanaged memory.  Note that this method
        // is most performant when used with medium to large sized structs (larger than 8 bytes 
        // though this is number is JIT and architecture dependent).   As such, it is best to use 
        // the WriteX methods for small standard types such as ints, longs, bools, etc.

        public void Write<T>(Int64 position, ref T structure) where T : struct
        {
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof(T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Argument_NotEnoughBytesToWrite, typeof (T).FullName), nameof(position));
                }
            }

            _buffer.Write<T>((UInt64)(_offset + position), structure);
        }

        // Writes 'count' structs of type T from 'array' (starting at 'offset') into unmanaged memory. 


        public void WriteArray<T>(Int64 position, T[] array, Int32 offset, Int32 count) where T : struct
        {
            if (array == null)
            {
                throw new ArgumentNullException(nameof(array), "Buffer cannot be null.");
            }
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
                throw new ArgumentException(SR.Argument_OffsetAndLengthOutOfBounds);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            if (position >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }

            _buffer.WriteArray<T>((UInt64)(_offset + position), array, offset, count);
        }

        private byte InternalReadByte(Int64 position)
        {
            Debug.Assert(CanRead, "UMA not readable");
            Debug.Assert(position >= 0, "position less than 0");
            Debug.Assert(position <= _capacity - sizeof(byte), "position is greater than capacity - sizeof(byte)");

            byte result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    result = *(pointer + _offset + position);
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

        private void InternalWrite(Int64 position, byte value)
        {
            Debug.Assert(CanWrite, "UMA not writable");
            Debug.Assert(position >= 0, "position less than 0");
            Debug.Assert(position <= _capacity - sizeof(byte), "position is greater than capacity - sizeof(byte)");

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    *(pointer + _offset + position) = value;
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

        private void EnsureSafeToRead(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!CanRead)
            {
                throw new NotSupportedException(SR.NotSupported_Reading);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.EndContractBlock();
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

        private void EnsureSafeToWrite(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", SR.ObjectDisposed_ViewAccessorClosed);
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(SR.NotSupported_Writing);
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_NeedNonNegNum);
            }
            Contract.EndContractBlock();
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), SR.ArgumentOutOfRange_PositionLessThanCapacityRequired);
                }
                else
                {
                    throw new ArgumentException(SR.Format(SR.Argument_NotEnoughBytesToWrite, nameof(Byte)), nameof(position));
                }
            }
        }
    }
}
