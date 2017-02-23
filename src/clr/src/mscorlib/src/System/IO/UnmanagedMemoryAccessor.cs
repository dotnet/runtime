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
                throw new ArgumentOutOfRangeException(nameof(offset), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (capacity < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(capacity), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (buffer.ByteLength < (UInt64)(offset + capacity))
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndCapacityOutOfBounds"));
            }
            if (access < FileAccess.Read || access > FileAccess.ReadWrite)
            {
                throw new ArgumentOutOfRangeException(nameof(access));
            }
            Contract.EndContractBlock();

            if (_isOpen)
            {
                throw new InvalidOperationException(Environment.GetResourceString("InvalidOperation_CalledTwice"));
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
                        throw new ArgumentException(Environment.GetResourceString("Argument_UnmanagedMemAccessorWrapAround"));
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
            int sizeOfType = sizeof(char);
            EnsureSafeToRead(position, sizeOfType);

            char result;

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((char*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        result = (char)( *pointer | *(pointer + 1) << 8 ) ;
                    }
#endif
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
            int sizeOfType = sizeof(Int16);
            EnsureSafeToRead(position, sizeOfType);

            Int16 result;

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((Int16*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        result = (Int16)( *pointer | *(pointer + 1) << 8 );
                    }
#endif
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
            int sizeOfType = sizeof(Int32);
            EnsureSafeToRead(position, sizeOfType);

            Int32 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((Int32*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        result = (Int32)( *pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24 );
                    }
#endif
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
            int sizeOfType = sizeof(Int64);
            EnsureSafeToRead(position, sizeOfType);

            Int64 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((Int64*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        int lo = *pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24;
                        int hi = *(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24;
                        result = (Int64)(((Int64)hi << 32) | (UInt32)lo);
                    }
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe Int32 UnsafeReadInt32(byte* pointer)
        {
            Int32 result;
            // check if pointer is aligned
            if (((int)pointer & (sizeof(Int32) - 1)) == 0)
            {
                result = *((Int32*)pointer);
            }
            else
            {
                result = (Int32)(*(pointer) | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
            }

            return result;
        }
        public Decimal ReadDecimal(Int64 position)
        {
            const int ScaleMask = 0x00FF0000;
            const int SignMask = unchecked((int)0x80000000);

            int sizeOfType = sizeof(Decimal);
            EnsureSafeToRead(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    int lo = UnsafeReadInt32(pointer);
                    int mid = UnsafeReadInt32(pointer + 4);
                    int hi = UnsafeReadInt32(pointer + 8);
                    int flags = UnsafeReadInt32(pointer + 12);

                    // Check for invalid Decimal values
                    if (!((flags & ~(SignMask | ScaleMask)) == 0 && (flags & ScaleMask) <= (28 << 16)))
                    {
                        throw new ArgumentException(Environment.GetResourceString("Arg_BadDecimal")); // Throw same Exception type as Decimal(int[]) ctor for compat
                    }

                    bool isNegative = (flags & SignMask) != 0;
                    byte scale = (byte)(flags >> 16);

                    return new decimal(lo, mid, hi, isNegative, scale);
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

        public Single ReadSingle(Int64 position)
        {
            int sizeOfType = sizeof(Single);
            EnsureSafeToRead(position, sizeOfType);

            Single result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = BitConverter.Int32BitsToSingle(*((int*)(pointer)));
#if ALIGN_ACCESS
                    }
                    else {
                    UInt32 tempResult = (UInt32)( *pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24 );
                    result = *((float*)&tempResult);
                    }
#endif
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
            int sizeOfType = sizeof(Double);
            EnsureSafeToRead(position, sizeOfType);

            Double result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = BitConverter.Int64BitsToDouble(*((long*)(pointer)));
#if ALIGN_ACCESS
                    }
                    else {

                    UInt32 lo = (UInt32)( *pointer | *(pointer + 1) << 8  | *(pointer + 2) << 16 | *(pointer + 3) << 24 );
                    UInt32 hi = (UInt32)( *(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24 );
                    UInt64 tempResult = ((UInt64)hi) << 32 | lo;
                    result = *((double*)&tempResult);

                    }
#endif
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
                    pointer += (_offset + position);
                    result = *((SByte*)pointer);
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
            int sizeOfType = sizeof(UInt16);
            EnsureSafeToRead(position, sizeOfType);

            UInt16 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((UInt16*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        result = (UInt16)( *pointer | *(pointer + 1) << 8 );
                    }
#endif

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
            int sizeOfType = sizeof(UInt32);
            EnsureSafeToRead(position, sizeOfType);

            UInt32 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((UInt32*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        result = (UInt32)( *pointer | *(pointer + 1) << 8  | *(pointer + 2) << 16 | *(pointer + 3) << 24 );
                    }
#endif

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
            int sizeOfType = sizeof(UInt64);
            EnsureSafeToRead(position, sizeOfType);

            UInt64 result;
            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    result = *((UInt64*)(pointer));
#if ALIGN_ACCESS
                    }
                    else {
                        UInt32 lo = (UInt32)( *pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24 );
                        UInt32 hi = (UInt32)( *(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24 );
                        result = (UInt64)(((UInt64)hi << 32) | lo );
                    }
#endif

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
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }
            if (!CanRead)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof(T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead", typeof(T).FullName), nameof(position));
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
                throw new ArgumentOutOfRangeException(nameof(offset), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (array.Length - offset < count)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
            }
            Contract.EndContractBlock();
            if (!CanRead)
            {
                if (!_isOpen)
                {
                    throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
                }
                else
                {
                    throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
                }
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }

            UInt32 sizeOfT = Marshal.AlignedSizeOf<T>();

            // only check position and ask for fewer Ts if count is too big
            if (position >= _capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
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
            int sizeOfType = sizeof(char);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((char*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer+1) = (byte)(value >> 8);
                    }
#endif
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
            int sizeOfType = sizeof(Int16);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((Int16*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                    }
#endif
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
            int sizeOfType = sizeof(Int32);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((Int32*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                    }
#endif
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
            int sizeOfType = sizeof(Int64);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((Int64*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                        *(pointer + 4) = (byte)(value >> 32);
                        *(pointer + 5) = (byte)(value >> 40);
                        *(pointer + 6) = (byte)(value >> 48);
                        *(pointer + 7) = (byte)(value >> 56);
                    }
#endif
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private unsafe void UnsafeWriteInt32(byte* pointer, Int32 value)
        {
            // check if pointer is aligned
            if (((int)pointer & (sizeof(Int32) - 1)) == 0)
            {
                *((Int32*)pointer) = value;
            }
            else
            {
                *(pointer) = (byte)value;
                *(pointer + 1) = (byte)(value >> 8);
                *(pointer + 2) = (byte)(value >> 16);
                *(pointer + 3) = (byte)(value >> 24);
            }
        }

        public void Write(Int64 position, Decimal value)
        {
            int sizeOfType = sizeof(Decimal);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    int* valuePtr = (int*)(&value);
                    int flags = *valuePtr;
                    int hi = *(valuePtr + 1);
                    int lo = *(valuePtr + 2);
                    int mid = *(valuePtr + 3);

                    UnsafeWriteInt32(pointer, lo);
                    UnsafeWriteInt32(pointer + 4, mid);
                    UnsafeWriteInt32(pointer + 8, hi);
                    UnsafeWriteInt32(pointer + 12, flags);
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
            int sizeOfType = sizeof(Single);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((int*)pointer) = BitConverter.SingleToInt32Bits(value);
#if ALIGN_ACCESS
                    }
                    else {
                    UInt32 tmpValue = *(UInt32*)&value;
                    *(pointer) = (byte)tmpValue;
                    *(pointer + 1) = (byte)(tmpValue >> 8);
                    *(pointer + 2) = (byte)(tmpValue >> 16);
                    *(pointer + 3) = (byte)(tmpValue >> 24);

                    }
#endif
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
            int sizeOfType = sizeof(Double);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((long*)pointer) = BitConverter.DoubleToInt64Bits(value);
#if ALIGN_ACCESS
                    }
                    else {
                    UInt64 tmpValue = *(UInt64 *)&value;
                    *(pointer) = (byte) tmpValue;
                    *(pointer + 1) = (byte) (tmpValue >> 8);
                    *(pointer + 2) = (byte) (tmpValue >> 16);
                    *(pointer + 3) = (byte) (tmpValue >> 24);
                    *(pointer + 4) = (byte) (tmpValue >> 32);
                    *(pointer + 5) = (byte) (tmpValue >> 40);
                    *(pointer + 6) = (byte) (tmpValue >> 48);
                    *(pointer + 7) = (byte) (tmpValue >> 56);

                    }
#endif
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
            int sizeOfType = sizeof(SByte);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
                    *((SByte*)pointer) = value;
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
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((UInt16*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                    }
#endif
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
            int sizeOfType = sizeof(UInt32);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((UInt32*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                    }
#endif

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
            int sizeOfType = sizeof(UInt64);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;
                RuntimeHelpers.PrepareConstrainedRegions();
                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);
#if ALIGN_ACCESS
                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0) {
#endif
                    *((UInt64*)pointer) = value;
#if ALIGN_ACCESS
                    }
                    else {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                        *(pointer + 4) = (byte)(value >> 32);
                        *(pointer + 5) = (byte)(value >> 40);
                        *(pointer + 6) = (byte)(value >> 48);
                        *(pointer + 7) = (byte)(value >> 56);
                    }
#endif

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
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
            }

            UInt32 sizeOfT = Marshal.SizeOfType(typeof(T));
            if (position > _capacity - sizeOfT)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", typeof(T).FullName), nameof(position));
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
                throw new ArgumentOutOfRangeException(nameof(offset), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (count < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(count), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (array.Length - offset < count)
            {
                throw new ArgumentException(Environment.GetResourceString("Argument_OffsetAndLengthOutOfBounds"));
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            if (position >= Capacity)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
            }
            Contract.EndContractBlock();

            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
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

        private void EnsureSafeToRead(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }
            if (!CanRead)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Reading"));
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToRead"), nameof(position));
                }
            }
        }

        private void EnsureSafeToWrite(Int64 position, int sizeOfType)
        {
            if (!_isOpen)
            {
                throw new ObjectDisposedException("UnmanagedMemoryAccessor", Environment.GetResourceString("ObjectDisposed_ViewAccessorClosed"));
            }
            if (!CanWrite)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_Writing"));
            }
            if (position < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_NeedNonNegNum"));
            }
            Contract.EndContractBlock();
            if (position > _capacity - sizeOfType)
            {
                if (position >= _capacity)
                {
                    throw new ArgumentOutOfRangeException(nameof(position), Environment.GetResourceString("ArgumentOutOfRange_PositionLessThanCapacityRequired"));
                }
                else
                {
                    throw new ArgumentException(Environment.GetResourceString("Argument_NotEnoughBytesToWrite", nameof(Byte)), nameof(position));
                }
            }
        }
    }
}
