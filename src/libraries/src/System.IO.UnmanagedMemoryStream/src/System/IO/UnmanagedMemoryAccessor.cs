// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.IO
{
    /// Perf notes: ReadXXX, WriteXXX (for basic types) acquire and release the 
    /// SafeBuffer pointer rather than relying on generic Read(T) from SafeBuffer because
    /// this gives better throughput; benchmarks showed about 12-15% better.
    public class UnmanagedMemoryAccessor : IDisposable
    {
        [System.Security.SecurityCritical] // auto-generated
        private SafeBuffer _buffer;
        private Int64 _offset;
        [ContractPublicPropertyName("Capacity")]
        private Int64 _capacity;
        private FileAccess _access;
        private bool _isOpen;
        private bool _canRead;
        private bool _canWrite;

        /// <summary>
        /// Allows to efficiently read typed data from memory or SafeBuffer
        /// </summary>
        protected UnmanagedMemoryAccessor()
        {
            _isOpen = false;
        }

        #region SafeBuffer ctors and initializers
        /// <summary>
        /// Creates an instance over a slice of a SafeBuffer.
        /// </summary>
        /// <param name="buffer">Buffer containing raw bytes.</param>
        /// <param name="offset">First byte belonging to the slice.</param>
        /// <param name="capacity">Number of bytes in the slice.</param>
        // <SecurityKernel Critical="True" Ring="1">
        // <ReferencesCritical Name="Method: Initialize(SafeBuffer, Int64, Int64, FileAccess):Void" Ring="1" />
        // </SecurityKernel>
        [System.Security.SecuritySafeCritical]
        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity)
        {
            Initialize(buffer, offset, capacity, FileAccess.Read);
        }

        /// <summary>
        /// Creates an instance over a slice of a SafeBuffer.
        /// </summary>
        /// <param name="buffer">Buffer containing raw bytes.</param>
        /// <param name="offset">First byte belonging to the slice.</param>
        /// <param name="capacity">Number of bytes in the slice.</param>
        /// <param name="access">Access permissions.</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public UnmanagedMemoryAccessor(SafeBuffer buffer, Int64 offset, Int64 capacity, FileAccess access)
        {
            Initialize(buffer, offset, capacity, access);
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
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

        /// <summary>
        /// Number of bytes in the accessor.
        /// </summary>
        public Int64 Capacity
        {
            get
            {
                return _capacity;
            }
        }

        /// <summary>
        /// Returns true if the accessor can be read; otherwise returns false.
        /// </summary>
        public bool CanRead
        {
            get
            {
                return _isOpen && _canRead;
            }
        }

        /// <summary>
        /// Returns true if the accessor can be written to; otherwise returns false.
        /// </summary>
        public bool CanWrite
        {
            get
            {
                return _isOpen && _canWrite;
            }
        }

        /// <summary>
        /// Closes the accessor.
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            _isOpen = false;
        }

        /// <summary>
        /// Closes the accessor.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Returns true if the accessor is open.
        /// </summary>
        protected bool IsOpen
        {
            get { return _isOpen; }
        }

        /// <summary>
        /// reads a Boolean value at given position
        /// </summary>
        public bool ReadBoolean(Int64 position)
        {
            int sizeOfType = sizeof(bool);
            EnsureSafeToRead(position, sizeOfType);

            byte b = InternalReadByte(position);
            return b != 0;
        }

        /// <summary>
        /// reads a Byte value at given position
        /// </summary>
        public byte ReadByte(Int64 position)
        {
            int sizeOfType = sizeof(byte);
            EnsureSafeToRead(position, sizeOfType);

            return InternalReadByte(position);
        }

        /// <summary>
        /// reads a Char value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public char ReadChar(Int64 position)
        {
            int sizeOfType = sizeof(char);
            EnsureSafeToRead(position, sizeOfType);

            char result;

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((char*)(pointer));
                    }
                    else
                    {
                        result = (char)(*pointer | *(pointer + 1) << 8);
                    }
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

        /// <summary>
        /// reads an Int16 value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical] // auto-generated
        public Int16 ReadInt16(Int64 position)
        {
            int sizeOfType = sizeof(Int16);
            EnsureSafeToRead(position, sizeOfType);

            Int16 result;

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);


                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((Int16*)(pointer));
                    }
                    else
                    {
                        result = (Int16)(*pointer | *(pointer + 1) << 8);
                    }
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

        /// <summary>
        /// reads an Int32 value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Int32 ReadInt32(Int64 position)
        {
            int sizeOfType = sizeof(Int32);
            EnsureSafeToRead(position, sizeOfType);

            Int32 result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);


                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((Int32*)(pointer));
                    }
                    else
                    {
                        result = (Int32)(*pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
                    }
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

        /// <summary>
        /// reads an Int64 value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Int64 ReadInt64(Int64 position)
        {
            int sizeOfType = sizeof(Int64);
            EnsureSafeToRead(position, sizeOfType);

            Int64 result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((Int64*)(pointer));
                    }
                    else
                    {
                        int lo = *pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24;
                        int hi = *(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24;
                        result = (Int64)(((Int64)hi << 32) | (UInt32)lo);
                    }
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

        /// <summary>
        /// Reads a Decimal value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte of the value.</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
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
                        throw new ArgumentException(SR.Arg_BadDecimal); // Throw same Exception type as Decimal(int[]) ctor for compat
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

        /// <summary>
        /// reads a Single value at given position
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Single ReadSingle(Int64 position)
        {
            int sizeOfType = sizeof(Single);
            EnsureSafeToRead(position, sizeOfType);

            Single result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((Single*)(pointer));
                    }
                    else
                    {
                        UInt32 tempResult = (UInt32)(*pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
                        result = *((float*)&tempResult);
                    }
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

        /// <summary>
        /// reads a Double value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public Double ReadDouble(Int64 position)
        {
            int sizeOfType = sizeof(Double);
            EnsureSafeToRead(position, sizeOfType);

            Double result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((Double*)(pointer));
                    }
                    else
                    {
                        UInt32 lo = (UInt32)(*pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
                        UInt32 hi = (UInt32)(*(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24);
                        UInt64 tempResult = ((UInt64)hi) << 32 | lo;
                        result = *((double*)&tempResult);
                    }
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

        /// <summary>
        /// Reads an SByte value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte of the value.</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public SByte ReadSByte(Int64 position)
        {
            int sizeOfType = sizeof(SByte);
            EnsureSafeToRead(position, sizeOfType);

            SByte result;
            unsafe
            {
                byte* pointer = null;

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

        /// <summary>
        /// reads a UInt16 value at given position
        /// </summary>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public UInt16 ReadUInt16(Int64 position)
        {
            int sizeOfType = sizeof(UInt16);
            EnsureSafeToRead(position, sizeOfType);

            UInt16 result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((UInt16*)(pointer));
                    }
                    else
                    {
                        result = (UInt16)(*pointer | *(pointer + 1) << 8);
                    }
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

        /// <summary>
        /// Reads a UInt32 value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte of the value.</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public UInt32 ReadUInt32(Int64 position)
        {
            int sizeOfType = sizeof(UInt32);
            EnsureSafeToRead(position, sizeOfType);

            UInt32 result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((UInt32*)(pointer));
                    }
                    else
                    {
                        result = (UInt32)(*pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
                    }
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

        /// <summary>
        /// Reads a UInt64 value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte of the value.</param>
        /// <returns></returns>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public UInt64 ReadUInt64(Int64 position)
        {
            int sizeOfType = sizeof(UInt64);
            EnsureSafeToRead(position, sizeOfType);

            UInt64 result;
            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        result = *((UInt64*)(pointer));
                    }
                    else
                    {
                        UInt32 lo = (UInt32)(*pointer | *(pointer + 1) << 8 | *(pointer + 2) << 16 | *(pointer + 3) << 24);
                        UInt32 hi = (UInt32)(*(pointer + 4) | *(pointer + 5) << 8 | *(pointer + 6) << 16 | *(pointer + 7) << 24);
                        result = (UInt64)(((UInt64)hi << 32) | lo);
                    }
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

        // ************** Write Methods ****************/

        // The following 13 WriteXXX methods write a value of type XXX into unmanaged memory at 'positon'. 
        // The bounds of the unmanaged memory are checked against to ensure that there is enough 
        // space after 'position' to write a value of type XXX.  XXX can be a bool, byte, char, decimal, 
        // double, short, int, long, sbyte, float, ushort, uint, or ulong. 

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        public void Write(Int64 position, bool value)
        {
            int sizeOfType = sizeof(bool);
            EnsureSafeToWrite(position, sizeOfType);

            byte b = (byte)(value ? 1 : 0);
            InternalWrite(position, b);
        }

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        public void Write(Int64 position, byte value)
        {
            int sizeOfType = sizeof(byte);
            EnsureSafeToWrite(position, sizeOfType);

            InternalWrite(position, value);
        }

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, char value)
        {
            int sizeOfType = sizeof(char);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((char*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, Int16 value)
        {
            int sizeOfType = sizeof(Int16);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((Int16*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, Int32 value)
        {
            int sizeOfType = sizeof(Int32);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
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
                finally
                {
                    if (pointer != null)
                    {
                        _buffer.ReleasePointer();
                    }
                }
            }
        }

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, Int64 value)
        {
            int sizeOfType = sizeof(Int64);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((Int64*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                        *(pointer + 4) = (byte)(value >> 32);
                        *(pointer + 5) = (byte)(value >> 40);
                        *(pointer + 6) = (byte)(value >> 48);
                        *(pointer + 7) = (byte)(value >> 56);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, Single value)
        {
            int sizeOfType = sizeof(Single);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((Single*)pointer) = value;
                    }
                    else
                    {
                        UInt32 tmpValue = *(UInt32*)&value;
                        *(pointer) = (byte)tmpValue;
                        *(pointer + 1) = (byte)(tmpValue >> 8);
                        *(pointer + 2) = (byte)(tmpValue >> 16);
                        *(pointer + 3) = (byte)(tmpValue >> 24);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        public void Write(Int64 position, Double value)
        {
            int sizeOfType = sizeof(Double);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((Double*)pointer) = value;
                    }
                    else
                    {
                        UInt64 tmpValue = *(UInt64*)&value;
                        *(pointer) = (byte)tmpValue;
                        *(pointer + 1) = (byte)(tmpValue >> 8);
                        *(pointer + 2) = (byte)(tmpValue >> 16);
                        *(pointer + 3) = (byte)(tmpValue >> 24);
                        *(pointer + 4) = (byte)(tmpValue >> 32);
                        *(pointer + 5) = (byte)(tmpValue >> 40);
                        *(pointer + 6) = (byte)(tmpValue >> 48);
                        *(pointer + 7) = (byte)(tmpValue >> 56);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public void Write(Int64 position, SByte value)
        {
            int sizeOfType = sizeof(SByte);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public void Write(Int64 position, UInt16 value)
        {
            int sizeOfType = sizeof(UInt16);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((UInt16*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public void Write(Int64 position, UInt32 value)
        {
            int sizeOfType = sizeof(UInt32);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((UInt32*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                    }
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

        /// <summary>
        /// Writes the value at the specified position.
        /// </summary>
        /// <param name="position">The position of the first byte.</param>
        /// <param name="value">Value to be written to the memory</param>
        [System.Security.SecuritySafeCritical]  // auto-generated
        [CLSCompliant(false)]
        public void Write(Int64 position, UInt64 value)
        {
            int sizeOfType = sizeof(UInt64);
            EnsureSafeToWrite(position, sizeOfType);

            unsafe
            {
                byte* pointer = null;

                try
                {
                    _buffer.AcquirePointer(ref pointer);
                    pointer += (_offset + position);

                    // check if pointer is aligned
                    if (((int)pointer & (sizeOfType - 1)) == 0)
                    {
                        *((UInt64*)pointer) = value;
                    }
                    else
                    {
                        *(pointer) = (byte)value;
                        *(pointer + 1) = (byte)(value >> 8);
                        *(pointer + 2) = (byte)(value >> 16);
                        *(pointer + 3) = (byte)(value >> 24);
                        *(pointer + 4) = (byte)(value >> 32);
                        *(pointer + 5) = (byte)(value >> 40);
                        *(pointer + 6) = (byte)(value >> 48);
                        *(pointer + 7) = (byte)(value >> 56);
                    }
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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private byte InternalReadByte(Int64 position)
        {
            Debug.Assert(CanRead, "UMA not readable");
            Debug.Assert(position >= 0, "position less than 0");
            Debug.Assert(position <= _capacity - sizeof(byte), "position is greater than capacity - sizeof(byte)");

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

        [System.Security.SecuritySafeCritical]  // auto-generated
        private void InternalWrite(Int64 position, byte value)
        {
            Debug.Assert(CanWrite, "UMA not writeable");
            Debug.Assert(position >= 0, "position less than 0");
            Debug.Assert(position <= _capacity - sizeof(byte), "position is greater than capacity - sizeof(byte)");

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
                    throw new ArgumentException(SR.Argument_NotEnoughBytesToWrite, nameof(position));
                }
            }
        }
    }
}
