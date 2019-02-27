// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;

namespace System.Collections
{
    // A vector of bits.  Use this to store bits efficiently, without having to do bit 
    // shifting yourself.
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class BitArray : ICollection, ICloneable
    {
        private int[] m_array; // Do not rename (binary serialization)
        private int m_length; // Do not rename (binary serialization)
        private int _version; // Do not rename (binary serialization)

        private const int _ShrinkThreshold = 256;

        /*=========================================================================
        ** Allocates space to hold length bit values. All of the values in the bit
        ** array are set to false.
        **
        ** Exceptions: ArgumentException if length < 0.
        =========================================================================*/
        public BitArray(int length)
            : this(length, false)
        {
        }

        /*=========================================================================
        ** Allocates space to hold length bit values. All of the values in the bit
        ** array are set to defaultValue.
        **
        ** Exceptions: ArgumentOutOfRangeException if length < 0.
        =========================================================================*/
        public BitArray(int length, bool defaultValue)
        {
            if (length < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(length), length, SR.ArgumentOutOfRange_NeedNonNegNum);
            }

            m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
            m_length = length;

            if (defaultValue)
            {
                m_array.AsSpan().Fill(-1);
            }

            _version = 0;
        }

        /*=========================================================================
        ** Allocates space to hold the bit values in bytes. bytes[0] represents
        ** bits 0 - 7, bytes[1] represents bits 8 - 15, etc. The LSB of each byte
        ** represents the lowest index value; bytes[0] & 1 represents bit 0,
        ** bytes[0] & 2 represents bit 1, bytes[0] & 4 represents bit 2, etc.
        **
        ** Exceptions: ArgumentException if bytes == null.
        =========================================================================*/
        public BitArray(byte[] bytes)
        {
            if (bytes == null)
            {
                throw new ArgumentNullException(nameof(bytes));
            }

            // this value is chosen to prevent overflow when computing m_length.
            // m_length is of type int32 and is exposed as a property, so 
            // type of m_length can't be changed to accommodate.
            if (bytes.Length > int.MaxValue / BitsPerByte)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerByte), nameof(bytes));
            }

            m_array = new int[GetInt32ArrayLengthFromByteLength(bytes.Length)];
            m_length = bytes.Length * BitsPerByte;

            uint totalCount = (uint)bytes.Length / 4;

            ReadOnlySpan<byte> byteSpan = bytes;
            for (int i = 0; i < totalCount; i++)
            {
                m_array[i] = BinaryPrimitives.ReadInt32LittleEndian(byteSpan);
                byteSpan = byteSpan.Slice(4);
            }

            Debug.Assert(byteSpan.Length >= 0 && byteSpan.Length < 4);

            int last = 0;
            switch (byteSpan.Length)
            {
                case 3:
                    last = byteSpan[2] << 16;
                    goto case 2;
                // fall through
                case 2:
                    last |= byteSpan[1] << 8;
                    goto case 1;
                // fall through
                case 1:
                    m_array[totalCount] = last | byteSpan[0];
                    break;
            }

            _version = 0;
        }

        public BitArray(bool[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            m_array = new int[GetInt32ArrayLengthFromBitLength(values.Length)];
            m_length = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                {
                    int elementIndex = Div32Rem(i, out int extraBits);
                    m_array[elementIndex] |= 1 << extraBits;
                }
            }

            _version = 0;
        }

        /*=========================================================================
        ** Allocates space to hold the bit values in values. values[0] represents
        ** bits 0 - 31, values[1] represents bits 32 - 63, etc. The LSB of each
        ** integer represents the lowest index value; values[0] & 1 represents bit
        ** 0, values[0] & 2 represents bit 1, values[0] & 4 represents bit 2, etc.
        **
        ** Exceptions: ArgumentException if values == null.
        =========================================================================*/
        public BitArray(int[] values)
        {
            if (values == null)
            {
                throw new ArgumentNullException(nameof(values));
            }

            // this value is chosen to prevent overflow when computing m_length
            if (values.Length > int.MaxValue / BitsPerInt32)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerInt32), nameof(values));
            }

            m_array = new int[values.Length];
            Array.Copy(values, 0, m_array, 0, values.Length);
            m_length = values.Length * BitsPerInt32;

            _version = 0;
        }

        /*=========================================================================
        ** Allocates a new BitArray with the same length and bit values as bits.
        **
        ** Exceptions: ArgumentException if bits == null.
        =========================================================================*/
        public BitArray(BitArray bits)
        {
            if (bits == null)
            {
                throw new ArgumentNullException(nameof(bits));
            }

            int arrayLength = GetInt32ArrayLengthFromBitLength(bits.m_length);

            m_array = new int[arrayLength];

            Debug.Assert(bits.m_array.Length <= arrayLength);

            Array.Copy(bits.m_array, 0, m_array, 0, arrayLength);
            m_length = bits.m_length;

            _version = bits._version;
        }

        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        /*=========================================================================
        ** Returns the bit value at position index.
        **
        ** Exceptions: ArgumentOutOfRangeException if index < 0 or
        **             index >= GetLength().
        =========================================================================*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            if ((uint)index >= (uint)m_length)
                ThrowArgumentOutOfRangeException(index);

            return (m_array[index >> 5] & (1 << index)) != 0;
        }

        /*=========================================================================
        ** Sets the bit value at position index to value.
        **
        ** Exceptions: ArgumentOutOfRangeException if index < 0 or
        **             index >= GetLength().
        =========================================================================*/
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            if ((uint)index >= (uint)m_length)
                ThrowArgumentOutOfRangeException(index);

            int bitMask = 1 << index;
            ref int segment = ref m_array[index >> 5];

            if (value)
            {
                segment |= bitMask;
            }
            else
            {
                segment &= ~bitMask;
            }

            _version++;
        }

        /*=========================================================================
        ** Sets all the bit values to value.
        =========================================================================*/
        public void SetAll(bool value)
        {
            int fillValue = value ? -1 : 0;
            int[] array = m_array;

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = fillValue;
            }

            _version++;
        }

        /*=========================================================================
        ** Returns a reference to the current instance ANDed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public unsafe BitArray And(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int count = m_array.Length;

            switch (count)
            {
                case 3: m_array[2] &= value.m_array[2]; goto case 2;
                case 2: m_array[1] &= value.m_array[1]; goto case 1;
                case 1: m_array[0] &= value.m_array[0]; goto Done;
                case 0: goto Done;
            }

            int i = 0;
            if (Sse2.IsSupported)
            {
                fixed (int* leftPtr = m_array)
                fixed (int* rightPtr = value.m_array)
                {
                    for (; i < count - (Vector128<int>.Count - 1); i += Vector128<int>.Count)
                    {
                        Vector128<int> leftVec = Sse2.LoadVector128(leftPtr + i);
                        Vector128<int> rightVec = Sse2.LoadVector128(rightPtr + i);
                        Sse2.Store(leftPtr + i, Sse2.And(leftVec, rightVec));
                    }
                }
            }

            for (; i < count; i++)
                m_array[i] &= value.m_array[i];

        Done:
            _version++;
            return this;
        }

        /*=========================================================================
        ** Returns a reference to the current instance ORed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public unsafe BitArray Or(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int count = m_array.Length;

            switch (count)
            {
                case 3: m_array[2] |= value.m_array[2]; goto case 2;
                case 2: m_array[1] |= value.m_array[1]; goto case 1;
                case 1: m_array[0] |= value.m_array[0]; goto Done;
                case 0: goto Done;
            }

            int i = 0;
            if (Sse2.IsSupported)
            {
                fixed (int* leftPtr = m_array)
                fixed (int* rightPtr = value.m_array)
                {
                    for (; i < count - (Vector128<int>.Count - 1); i += Vector128<int>.Count)
                    {
                        Vector128<int> leftVec = Sse2.LoadVector128(leftPtr + i);
                        Vector128<int> rightVec = Sse2.LoadVector128(rightPtr + i);
                        Sse2.Store(leftPtr + i, Sse2.Or(leftVec, rightVec));
                    }
                }
            }

            for (; i < count; i++)
                m_array[i] |= value.m_array[i];

        Done:
            _version++;
            return this;
        }

        /*=========================================================================
        ** Returns a reference to the current instance XORed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public unsafe BitArray Xor(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int count = m_array.Length;

            switch (count)
            {
                case 3: m_array[2] ^= value.m_array[2]; goto case 2;
                case 2: m_array[1] ^= value.m_array[1]; goto case 1;
                case 1: m_array[0] ^= value.m_array[0]; goto Done;
                case 0: goto Done;
            }

            int i = 0;
            if (Sse2.IsSupported)
            {
                fixed (int* leftPtr = m_array)
                fixed (int* rightPtr = value.m_array)
                {
                    for (; i < count - (Vector128<int>.Count - 1); i += Vector128<int>.Count)
                    {
                        Vector128<int> leftVec = Sse2.LoadVector128(leftPtr + i);
                        Vector128<int> rightVec = Sse2.LoadVector128(rightPtr + i);
                        Sse2.Store(leftPtr + i, Sse2.Xor(leftVec, rightVec));
                    }
                }
            }

            for (; i < count; i++)
                m_array[i] ^= value.m_array[i];

        Done:
            _version++;
            return this;
        }

        /*=========================================================================
        ** Inverts all the bit values. On/true bit values are converted to
        ** off/false. Off/false bit values are turned on/true. The current instance
        ** is updated and returned.
        =========================================================================*/
        public BitArray Not()
        {
            int[] array = m_array;

            for (int i = 0; i < array.Length; i++)
            {
                array[i] = ~array[i];
            }

            _version++;
            return this;
        }

        /*=========================================================================
        ** Shift all the bit values to right on count bits. The current instance is
        ** updated and returned.
        * 
        ** Exceptions: ArgumentOutOfRangeException if count < 0
        =========================================================================*/
        public BitArray RightShift(int count)
        {
            if (count <= 0)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
                }

                _version++;
                return this;
            }

            int toIndex = 0;
            int ints = GetInt32ArrayLengthFromBitLength(m_length);
            if (count < m_length)
            {
                // We can not use Math.DivRem without taking a dependency on System.Runtime.Extensions
                int fromIndex = Div32Rem(count, out int shiftCount);
                Div32Rem(m_length, out int extraBits);
                if (shiftCount == 0)
                {
                    unchecked
                    {
                        // Cannot use `(1u << extraBits) - 1u` as the mask
                        // because for extraBits == 0, we need the mask to be 111...111, not 0.
                        // In that case, we are shifting a uint by 32, which could be considered undefined.
                        // The result of a shift operation is undefined ... if the right operand
                        // is greater than or equal to the width in bits of the promoted left operand,
                        // https://docs.microsoft.com/en-us/cpp/c-language/bitwise-shift-operators?view=vs-2017
                        // However, the compiler protects us from undefined behaviour by constraining the
                        // right operand to between 0 and width - 1 (inclusive), i.e. right_operand = (right_operand % width).
                        uint mask = uint.MaxValue >> (BitsPerInt32 - extraBits);
                        m_array[ints - 1] &= (int)mask;
                    }
                    Array.Copy(m_array, fromIndex, m_array, 0, ints - fromIndex);
                    toIndex = ints - fromIndex;
                }
                else
                {
                    int lastIndex = ints - 1;
                    unchecked
                    {
                        while (fromIndex < lastIndex)
                        {
                            uint right = (uint)m_array[fromIndex] >> shiftCount;
                            int left = m_array[++fromIndex] << (BitsPerInt32 - shiftCount);
                            m_array[toIndex++] = left | (int)right;
                        }
                        uint mask = uint.MaxValue >> (BitsPerInt32 - extraBits);
                        mask &= (uint)m_array[fromIndex];
                        m_array[toIndex++] = (int)(mask >> shiftCount);
                    }
                }
            }

            m_array.AsSpan(toIndex, ints - toIndex).Clear();
            _version++;
            return this;
        }

        /*=========================================================================
        ** Shift all the bit values to left on count bits. The current instance is
        ** updated and returned.
        * 
        ** Exceptions: ArgumentOutOfRangeException if count < 0
        =========================================================================*/
        public BitArray LeftShift(int count)
        {
            if (count <= 0)
            {
                if (count < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(count), count, SR.ArgumentOutOfRange_NeedNonNegNum);
                }

                _version++;
                return this;
            }

            int lengthToClear;
            if (count < m_length)
            {
                int lastIndex = (m_length - 1) >> BitShiftPerInt32;  // Divide by 32.

                // We can not use Math.DivRem without taking a dependency on System.Runtime.Extensions
                lengthToClear = Div32Rem(count, out int shiftCount);

                if (shiftCount == 0)
                {
                    Array.Copy(m_array, 0, m_array, lengthToClear, lastIndex + 1 - lengthToClear);
                }
                else
                {
                    int fromindex = lastIndex - lengthToClear;
                    unchecked
                    {
                        while (fromindex > 0)
                        {
                            int left = m_array[fromindex] << shiftCount;
                            uint right = (uint)m_array[--fromindex] >> (BitsPerInt32 - shiftCount);
                            m_array[lastIndex] = left | (int)right;
                            lastIndex--;
                        }
                        m_array[lastIndex] = m_array[fromindex] << shiftCount;
                    }
                }
            }
            else
            {
                lengthToClear = GetInt32ArrayLengthFromBitLength(m_length); // Clear all
            }

            m_array.AsSpan(0, lengthToClear).Clear();
            _version++;
            return this;
        }

        public int Length
        {
            get
            {
                return m_length;
            }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(value), value, SR.ArgumentOutOfRange_NeedNonNegNum);
                }

                int newints = GetInt32ArrayLengthFromBitLength(value);
                if (newints > m_array.Length || newints + _ShrinkThreshold < m_array.Length)
                {
                    // grow or shrink (if wasting more than _ShrinkThreshold ints)
                    Array.Resize(ref m_array, newints);
                }

                if (value > m_length)
                {
                    // clear high bit values in the last int
                    int last = (m_length - 1) >> BitShiftPerInt32;
                    Div32Rem(m_length, out int bits);
                    if (bits > 0)
                    {
                        m_array[last] &= (1 << bits) - 1;
                    }

                    // clear remaining int values
                    m_array.AsSpan(last + 1, newints - last - 1).Clear();
                }

                m_length = value;
                _version++;
            }
        }

        public void CopyTo(Array array, int index)
        {
            if (array == null)
                throw new ArgumentNullException(nameof(array));

            if (index < 0)
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_NeedNonNegNum);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));

            if (array is int[] intArray)
            {
                Div32Rem(m_length, out int extraBits);

                if (extraBits == 0)
                {
                    // we have perfect bit alignment, no need to sanitize, just copy
                    Array.Copy(m_array, 0, intArray, index, m_array.Length);
                }
                else
                {
                    int last = (m_length - 1) >> BitShiftPerInt32;
                    // do not copy the last int, as it is not completely used
                    Array.Copy(m_array, 0, intArray, index, last);

                    // the last int needs to be masked
                    intArray[index + last] = m_array[last] & unchecked((1 << extraBits) - 1);
                }
            }
            else if (array is byte[] byteArray)
            {
                int arrayLength = GetByteArrayLengthFromBitLength(m_length);
                if ((array.Length - index) < arrayLength)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                // equivalent to m_length % BitsPerByte, since BitsPerByte is a power of 2
                uint extraBits = (uint)m_length & (BitsPerByte - 1);
                if (extraBits > 0)
                {
                    // last byte is not aligned, we will directly copy one less byte
                    arrayLength -= 1;
                }

                Span<byte> span = byteArray.AsSpan(index);

                int quotient = Div4Rem(arrayLength, out int remainder);
                for (int i = 0; i < quotient; i++)
                {
                    BinaryPrimitives.WriteInt32LittleEndian(span, m_array[i]);
                    span = span.Slice(4);
                }

                if (extraBits > 0)
                {
                    Debug.Assert(span.Length > 0);
                    Debug.Assert(m_array.Length > quotient);
                    // mask the final byte
                    span[span.Length - 1] = (byte)((m_array[quotient] >> (remainder * 8)) & ((1 << (int)extraBits) - 1));
                }

                switch (remainder)
                {
                    case 3:
                        span[2] = (byte)(m_array[quotient] >> 16);
                        goto case 2;
                    // fall through
                    case 2:
                        span[1] = (byte)(m_array[quotient] >> 8);
                        goto case 1;
                    // fall through
                    case 1:
                        span[0] = (byte)m_array[quotient];
                        break;
                }
            }
            else if (array is bool[] boolArray)
            {
                if (array.Length - index < m_length)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                for (int i = 0; i < m_length; i++)
                {
                    int elementIndex = Div32Rem(i, out int extraBits);
                    boolArray[index + i] = ((m_array[elementIndex] >> extraBits) & 0x00000001) != 0;
                }
            }
            else
            {
                throw new ArgumentException(SR.Arg_BitArrayTypeUnsupported, nameof(array));
            }
        }

        public int Count => m_length;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public bool IsReadOnly => false;

        public object Clone() => new BitArray(this);

        public IEnumerator GetEnumerator() => new BitArrayEnumeratorSimple(this);

        // XPerY=n means that n Xs can be stored in 1 Y. 
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;

        private const int BitShiftPerInt32 = 5;
        private const int BitShiftPerByte = 3;
        private const int BitShiftForBytesPerInt32 = 2;

        /// <summary>
        /// Used for conversion between different representations of bit array. 
        /// Returns (n + (32 - 1)) / 32, rearranged to avoid arithmetic overflow. 
        /// For example, in the bit to int case, the straightforward calc would 
        /// be (n + 31) / 32, but that would cause overflow. So instead it's 
        /// rearranged to ((n - 1) / 32) + 1.
        /// Due to sign extension, we don't need to special case for n == 0, if we use
        /// bitwise operations (since ((n - 1) >> 5) + 1 = 0).
        /// This doesn't hold true for ((n - 1) / 32) + 1, which equals 1.
        /// 
        /// Usage:
        /// GetArrayLength(77): returns how many ints must be 
        /// allocated to store 77 bits.
        /// </summary>
        /// <param name="n"></param>
        /// <returns>how many ints are required to store n bytes</returns>
        private static int GetInt32ArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerInt32)) >> BitShiftPerInt32);
        }

        private static int GetInt32ArrayLengthFromByteLength(int n)
        {
            Debug.Assert(n >= 0);
            // Due to sign extension, we don't need to special case for n == 0, since ((n - 1) >> 2) + 1 = 0
            // This doesn't hold true for ((n - 1) / 4) + 1, which equals 1.
            return (int)((uint)(n - 1 + (1 << BitShiftForBytesPerInt32)) >> BitShiftForBytesPerInt32);
        }

        private static int GetByteArrayLengthFromBitLength(int n)
        {
            Debug.Assert(n >= 0);
            // Due to sign extension, we don't need to special case for n == 0, since ((n - 1) >> 3) + 1 = 0
            // This doesn't hold true for ((n - 1) / 8) + 1, which equals 1.
            return (int)((uint)(n - 1 + (1 << BitShiftPerByte)) >> BitShiftPerByte);
        }

        private static int Div32Rem(int number, out int remainder)
        {
            uint quotient = (uint)number / 32;
            remainder = number & (32 - 1);    // equivalent to number % 32, since 32 is a power of 2
            return (int)quotient;
        }

        private static int Div4Rem(int number, out int remainder)
        {
            uint quotient = (uint)number / 4;
            remainder = number & (4 - 1);   // equivalent to number % 4, since 4 is a power of 2
            return (int)quotient;
        }

        private static void ThrowArgumentOutOfRangeException(int index)
        {
            throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
        }

        private class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private BitArray _bitarray;
            private int _index;
            private readonly int _version;
            private bool _currentElement;

            internal BitArrayEnumeratorSimple(BitArray bitarray)
            {
                _bitarray = bitarray;
                _index = -1;
                _version = bitarray._version;
            }

            public object Clone() => MemberwiseClone();

            public virtual bool MoveNext()
            {
                ICollection bitarrayAsICollection = _bitarray;
                if (_version != _bitarray._version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);

                if (_index < (bitarrayAsICollection.Count - 1))
                {
                    _index++;
                    _currentElement = _bitarray.Get(_index);
                    return true;
                }
                else
                {
                    _index = bitarrayAsICollection.Count;
                }

                return false;
            }

            public virtual object Current
            {
                get
                {
                    if ((uint)_index >= (uint)_bitarray.Count)
                        throw GetInvalidOperationException(_index);
                    return _currentElement;
                }
            }

            public void Reset()
            {
                if (_version != _bitarray._version)
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                _index = -1;
            }

            private InvalidOperationException GetInvalidOperationException(int index)
            {
                if (index == -1)
                {
                    return new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                }
                else
                {
                    Debug.Assert(index >= _bitarray.Count);
                    return new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                }
            }
        }
    }
}
