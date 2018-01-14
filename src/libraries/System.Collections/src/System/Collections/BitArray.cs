// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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
        [NonSerialized]
        private object _syncRoot;

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

            m_array = new int[GetArrayLength(length, BitsPerInt32)];
            m_length = length;

            int fillValue = defaultValue ? unchecked(((int)0xffffffff)) : 0;
            for (int i = 0; i < m_array.Length; i++)
            {
                m_array[i] = fillValue;
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

            m_array = new int[GetArrayLength(bytes.Length, BytesPerInt32)];
            m_length = bytes.Length * BitsPerByte;

            int i = 0;
            int j = 0;
            while (bytes.Length - j >= 4)
            {
                m_array[i++] = (bytes[j] & 0xff) |
                              ((bytes[j + 1] & 0xff) << 8) |
                              ((bytes[j + 2] & 0xff) << 16) |
                              ((bytes[j + 3] & 0xff) << 24);
                j += 4;
            }

            Debug.Assert(bytes.Length - j >= 0, "BitArray byteLength problem");
            Debug.Assert(bytes.Length - j < 4, "BitArray byteLength problem #2");

            switch (bytes.Length - j)
            {
                case 3:
                    m_array[i] = ((bytes[j + 2] & 0xff) << 16);
                    goto case 2;
                // fall through
                case 2:
                    m_array[i] |= ((bytes[j + 1] & 0xff) << 8);
                    goto case 1;
                // fall through
                case 1:
                    m_array[i] |= (bytes[j] & 0xff);
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

            m_array = new int[GetArrayLength(values.Length, BitsPerInt32)];
            m_length = values.Length;

            for (int i = 0; i < values.Length; i++)
            {
                if (values[i])
                    m_array[i / 32] |= (1 << (i % 32));
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

            int arrayLength = GetArrayLength(bits.m_length, BitsPerInt32);

            m_array = new int[arrayLength];
            Array.Copy(bits.m_array, 0, m_array, 0, arrayLength);
            m_length = bits.m_length;

            _version = bits._version;
        }

        public bool this[int index]
        {
            get
            {
                return Get(index);
            }
            set
            {
                Set(index, value);
            }
        }

        /*=========================================================================
        ** Returns the bit value at position index.
        **
        ** Exceptions: ArgumentOutOfRangeException if index < 0 or
        **             index >= GetLength().
        =========================================================================*/
        public bool Get(int index)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            return (m_array[index / 32] & (1 << (index % 32))) != 0;
        }

        /*=========================================================================
        ** Sets the bit value at position index to value.
        **
        ** Exceptions: ArgumentOutOfRangeException if index < 0 or
        **             index >= GetLength().
        =========================================================================*/
        public void Set(int index, bool value)
        {
            if (index < 0 || index >= Length)
            {
                throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_Index);
            }

            if (value)
            {
                m_array[index / 32] |= (1 << (index % 32));
            }
            else
            {
                m_array[index / 32] &= ~(1 << (index % 32));
            }

            _version++;
        }

        /*=========================================================================
        ** Sets all the bit values to value.
        =========================================================================*/
        public void SetAll(bool value)
        {
            int fillValue = value ? unchecked(((int)0xffffffff)) : 0;
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = fillValue;
            }

            _version++;
        }

        /*=========================================================================
        ** Returns a reference to the current instance ANDed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public BitArray And(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] &= value.m_array[i];
            }

            _version++;
            return this;
        }

        /*=========================================================================
        ** Returns a reference to the current instance ORed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public BitArray Or(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] |= value.m_array[i];
            }

            _version++;
            return this;
        }

        /*=========================================================================
        ** Returns a reference to the current instance XORed with value.
        **
        ** Exceptions: ArgumentException if value == null or
        **             value.Length != this.Length.
        =========================================================================*/
        public BitArray Xor(BitArray value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (Length != value.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] ^= value.m_array[i];
            }

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
            int ints = GetArrayLength(m_length, BitsPerInt32);
            for (int i = 0; i < ints; i++)
            {
                m_array[i] = ~m_array[i];
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
            int ints = GetArrayLength(m_length, BitsPerInt32);
            if (count < m_length)
            {
                int shiftCount;
                // We can not use Math.DivRem without taking a dependency on System.Runtime.Extensions
                int fromIndex = count / BitsPerInt32;
                shiftCount = count - fromIndex * BitsPerInt32; // Optimized Rem

                if (shiftCount == 0)
                {
                    unchecked
                    {
                        uint mask = uint.MaxValue >> (BitsPerInt32 - m_length % BitsPerInt32);
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
                        uint mask = uint.MaxValue >> (BitsPerInt32 - m_length % BitsPerInt32);
                        mask &= (uint)m_array[fromIndex];
                        m_array[toIndex++] = (int)(mask >> shiftCount);
                    }
                }
            }

            Array.Clear(m_array, toIndex, ints - toIndex);
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
                int lastIndex = (m_length - 1) / BitsPerInt32;

                int shiftCount;
                // We can not use Math.DivRem without taking a dependency on System.Runtime.Extensions
                lengthToClear = count / BitsPerInt32;
                shiftCount = count - lengthToClear * BitsPerInt32; // Optimized Rem

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
                lengthToClear = GetArrayLength(m_length, BitsPerInt32); // Clear all
            }

            Array.Clear(m_array, 0, lengthToClear);
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

                int newints = GetArrayLength(value, BitsPerInt32);
                if (newints > m_array.Length || newints + _ShrinkThreshold < m_array.Length)
                {
                    // grow or shrink (if wasting more than _ShrinkThreshold ints)
                    Array.Resize(ref m_array, newints);
                }

                if (value > m_length)
                {
                    // clear high bit values in the last int
                    int last = GetArrayLength(m_length, BitsPerInt32) - 1;
                    int bits = m_length % 32;
                    if (bits > 0)
                    {
                        m_array[last] &= (1 << bits) - 1;
                    }

                    // clear remaining int values
                    Array.Clear(m_array, last + 1, newints - last - 1);
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

            int[] intArray = array as int[];
            if (intArray != null)
            {
                int last = GetArrayLength(m_length, BitsPerInt32) - 1;
                int extraBits = m_length % BitsPerInt32;

                if (extraBits == 0)
                {
                    // we have perfect bit alignment, no need to sanitize, just copy
                    Array.Copy(m_array, 0, intArray, index, GetArrayLength(m_length, BitsPerInt32));
                }
                else
                {
                    // do not copy the last int, as it is not completely used
                    Array.Copy(m_array, 0, intArray, index, GetArrayLength(m_length, BitsPerInt32) - 1);

                    // the last int needs to be masked
                    intArray[index + last] = m_array[last] & unchecked((1 << extraBits) - 1);
                }
            }
            else if (array is byte[])
            {
                int extraBits = m_length % BitsPerByte;

                int arrayLength = GetArrayLength(m_length, BitsPerByte);
                if ((array.Length - index) < arrayLength)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                if (extraBits > 0)
                {
                    // last byte is not aligned, we will directly copy one less byte
                    arrayLength -= 1;
                }

                byte[] b = (byte[])array;

                // copy all the perfectly-aligned bytes
                for (int i = 0; i < arrayLength; i++)
                    b[index + i] = (byte)((m_array[i / 4] >> ((i % 4) * 8)) & 0x000000FF); // Shift to bring the required byte to LSB, then mask

                if (extraBits > 0)
                {
                    // mask the final byte
                    int i = arrayLength;
                    b[index + i] = (byte)((m_array[i / 4] >> ((i % 4) * 8)) & ((1 << extraBits) - 1));
                }
            }
            else if (array is bool[])
            {
                if (array.Length - index < m_length)
                    throw new ArgumentException(SR.Argument_InvalidOffLen);

                bool[] b = (bool[])array;
                for (int i = 0; i < m_length; i++)
                    b[index + i] = ((m_array[i / 32] >> (i % 32)) & 0x00000001) != 0;
            }
            else
                throw new ArgumentException(SR.Arg_BitArrayTypeUnsupported, nameof(array));
        }

        public int Count
        {
            get
            {
                return m_length;
            }
        }

        public Object SyncRoot
        {
            get
            {
                if (_syncRoot == null)
                {
                    System.Threading.Interlocked.CompareExchange<Object>(ref _syncRoot, new object(), null);
                }
                return _syncRoot;
            }
        }

        public bool IsSynchronized
        {
            get
            {
                return false;
            }
        }

        public bool IsReadOnly
        {
            get
            {
                return false;
            }
        }

        public object Clone()
        {
            return new BitArray(this);
        }

        public IEnumerator GetEnumerator()
        {
            return new BitArrayEnumeratorSimple(this);
        }

        // XPerY=n means that n Xs can be stored in 1 Y. 
        private const int BitsPerInt32 = 32;
        private const int BytesPerInt32 = 4;
        private const int BitsPerByte = 8;

        /// <summary>
        /// Used for conversion between different representations of bit array. 
        /// Returns (n+(div-1))/div, rearranged to avoid arithmetic overflow. 
        /// For example, in the bit to int case, the straightforward calc would 
        /// be (n+31)/32, but that would cause overflow. So instead it's 
        /// rearranged to ((n-1)/32) + 1, with special casing for 0.
        /// 
        /// Usage:
        /// GetArrayLength(77, BitsPerInt32): returns how many ints must be 
        /// allocated to store 77 bits.
        /// </summary>
        /// <param name="n"></param>
        /// <param name="div">use a conversion constant, e.g. BytesPerInt32 to get
        /// how many ints are required to store n bytes</param>
        /// <returns></returns>
        private static int GetArrayLength(int n, int div)
        {
            Debug.Assert(div > 0, "GetArrayLength: div arg must be greater than 0");
            return n > 0 ? (((n - 1) / div) + 1) : 0;
        }

        private class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private BitArray bitarray;
            private int index;
            private int version;
            private bool currentElement;

            internal BitArrayEnumeratorSimple(BitArray bitarray)
            {
                this.bitarray = bitarray;
                this.index = -1;
                version = bitarray._version;
            }

            public object Clone()
            {
                return MemberwiseClone();
            }

            public virtual bool MoveNext()
            {
                ICollection bitarrayAsICollection = bitarray;
                if (version != bitarray._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                if (index < (bitarrayAsICollection.Count - 1))
                {
                    index++;
                    currentElement = bitarray.Get(index);
                    return true;
                }
                else
                    index = bitarrayAsICollection.Count;

                return false;
            }

            public virtual object Current
            {
                get
                {
                    if (index == -1)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumNotStarted);
                    if (index >= ((ICollection)bitarray).Count)
                        throw new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                    return currentElement;
                }
            }

            public void Reset()
            {
                if (version != bitarray._version) throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                index = -1;
            }
        }
    }
}
