// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;

namespace System.Collections
{
    /// <summary>
    /// Manages a compact array of bit values, which are represented as <see cref="bool"/>, where
    /// <see langword="true"/> indicates that the bit is on (1) and <see langword="false"/> indicates
    /// the bit is off (0).
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class BitArray : ICollection, ICloneable
    {
        /// <summary>Bit array. Always little endian, even on big endian platforms.</summary>
        internal int[] m_array; // Do not rename (binary serialization)
        private int m_length; // Do not rename (binary serialization)
        private int _version; // Do not rename (binary serialization)

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that can hold the specified
        /// number of bit values, which are initially set to false.
        /// </summary>
        /// <param name="length">The number of bit values in the new <see cref="BitArray"/>.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than zero.</exception>
        public BitArray(int length)
            : this(length, false)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that can hold the specified number of
        /// bit values, which are initially set to the specified value.
        /// </summary>
        /// <param name="length">The number of bit values in the new <see cref="BitArray"/>.</param>
        /// <param name="defaultValue">The Boolean value to assign to each bit.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="length"/> is less than zero.</exception>
        public BitArray(int length, bool defaultValue)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
            m_length = length;

            if (defaultValue)
            {
                Array.Fill(m_array, -1);

                // Clear high bit values in the last int.
                int extraBits = (int)((uint)length % BitsPerInt32);
                if (extraBits != 0)
                {
                    m_array[^1] = ReverseIfBE((1 << extraBits) - 1);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that contains bit values copied
        /// from the specified array of bytes.
        /// </summary>
        /// <param name="bytes">An array of bytes containing the values to copy, where each byte represents eight consecutive bits.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bytes"/> is null.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="bytes"/> in bits is greater than <see cref="int.MaxValue"/>.</exception>
        public BitArray(byte[] bytes)
        {
            ArgumentNullException.ThrowIfNull(bytes);
            if (bytes.Length > int.MaxValue / BitsPerByte)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerByte), nameof(bytes));
            }

            m_array = new int[GetInt32ArrayLengthFromByteLength(bytes.Length)];
            m_length = bytes.Length * BitsPerByte;

            bytes.AsSpan().CopyTo(MemoryMarshal.AsBytes((Span<int>)m_array));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that contains bit values
        /// copied from the specified array of Booleans.
        /// </summary>
        /// <param name="values">An array of Booleans to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        public BitArray(bool[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            m_array = new int[GetInt32ArrayLengthFromBitLength(values.Length)];
            m_length = values.Length;

            uint i = 0;

            if (!BitConverter.IsLittleEndian || values.Length < Vector256<byte>.Count)
            {
                goto LessThan32;
            }

            // Comparing with 1s would get rid of the final negation, however this would not work for some CLR bools
            // (true for any non-zero values, false for 0) - any values between 2-255 will be interpreted as false.
            // Instead, We compare with zeroes (== false) then negate the result to ensure compatibility.

            ref byte value = ref Unsafe.As<bool, byte>(ref MemoryMarshal.GetArrayDataReference<bool>(values));
            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= (uint)values.Length - Vector512<byte>.Count; i += (uint)Vector512<byte>.Count)
                {
                    Vector512<byte> vector = Vector512.LoadUnsafe(ref value, i);
                    Vector512<byte> isFalse = Vector512.Equals(vector, Vector512<byte>.Zero);

                    ulong result = isFalse.ExtractMostSignificantBits();
                    m_array[i / 32u] = (int)(~result & 0x00000000FFFFFFFF);
                    m_array[(i / 32u) + 1] = (int)((~result >> 32) & 0x00000000FFFFFFFF);
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= (uint)values.Length - Vector256<byte>.Count; i += (uint)Vector256<byte>.Count)
                {
                    Vector256<byte> vector = Vector256.LoadUnsafe(ref value, i);
                    Vector256<byte> isFalse = Vector256.Equals(vector, Vector256<byte>.Zero);

                    uint result = isFalse.ExtractMostSignificantBits();
                    m_array[i / 32u] = (int)(~result);
                }
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                for (; i <= (uint)values.Length - Vector128<byte>.Count * 2u; i += (uint)Vector128<byte>.Count * 2u)
                {
                    Vector128<byte> lowerVector = Vector128.LoadUnsafe(ref value, i);
                    Vector128<byte> lowerIsFalse = Vector128.Equals(lowerVector, Vector128<byte>.Zero);
                    uint lowerResult = lowerIsFalse.ExtractMostSignificantBits();

                    Vector128<byte> upperVector = Vector128.LoadUnsafe(ref value, i + (uint)Vector128<byte>.Count);
                    Vector128<byte> upperIsFalse = Vector128.Equals(upperVector, Vector128<byte>.Zero);
                    uint upperResult = upperIsFalse.ExtractMostSignificantBits();

                    m_array[i / 32u] = (int)(~((upperResult << 16) | lowerResult));
                }
            }

        LessThan32:
            for (; i < (uint)values.Length; i++)
            {
                if (values[i])
                {
                    (uint elementIndex, uint extraBits) = Math.DivRem(i, BitsPerInt32);
                    m_array[elementIndex] |= ReverseIfBE(1 << (int)extraBits);
                }
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that contains bit values
        /// copied from the specified array of 32-bit integers.
        /// </summary>
        /// <param name="values">An array of integers containing the values to copy, where each integer represents 32 consecutive bits.</param>
        /// <exception cref="ArgumentNullException"><paramref name="values"/> is null.</exception>
        /// <exception cref="ArgumentException">The length of <paramref name="values"/> in bits is greater than <see cref="int.MaxValue"/>.</exception>
        /// <remarks>
        /// The number in the first <paramref name="values"/> array element represents bits 0 through 31, the second number in the array represents
        /// bits 32 through 63, and so on. The Least Significant Bit of each integer represents the lowest index value:
        /// "<paramref name="values"/>[0] &amp; 1" represents bit 0, "<paramref name="values"/>[0] &amp; 2" represents bit 1,
        /// "<paramref name="values"/>[0] &amp; 4" represents bit 2, and so on.
        /// </remarks>
        public BitArray(int[] values)
        {
            ArgumentNullException.ThrowIfNull(values);
            if (values.Length > int.MaxValue / BitsPerInt32)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerInt32), nameof(values));
            }

            m_array = new int[values.Length];
            if (BitConverter.IsLittleEndian)
            {
                Array.Copy(values, m_array, values.Length);
            }
            else
            {
                BinaryPrimitives.ReverseEndianness(values, m_array);
            }

            m_length = values.Length * BitsPerInt32;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that contains bit values copied from the specified BitArray.
        /// </summary>
        /// <param name="bits">The <see cref="BitArray"/> to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bits"/> is null.</exception>
        public BitArray(BitArray bits)
        {
            ArgumentNullException.ThrowIfNull(bits);

            int arrayLength = GetInt32ArrayLengthFromBitLength(bits.m_length);

            m_array = new int[arrayLength];
            Debug.Assert(bits.m_array.Length <= arrayLength);

            Array.Copy(bits.m_array, m_array, arrayLength);
            m_length = bits.m_length;
        }

        /// <summary>
        /// Gets or sets the value of the bit at a specific position in the <see cref="BitArray"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get or set.</param>
        /// <returns>The value of the bit at position <paramref name="index"/>.</returns>
        /// <returns>The value of the bit at position <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is greater than or equal to <see cref="Count"/>.</exception>
        public bool this[int index]
        {
            get => Get(index);
            set => Set(index, value);
        }

        /// <summary>
        /// Gets the value of the bit at a specific position in the <see cref="BitArray"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <returns>The value of the bit at position <paramref name="index"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is greater than or equal to <see cref="Count"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Get(int index)
        {
            if ((uint)index >= (uint)m_length)
            {
                ThrowArgumentOutOfRangeException(index);
            }

            return (ReverseIfBE(m_array[index >> BitShiftPerInt32]) & (1 << index)) != 0;
        }

        /// <summary>
        /// Sets the value of the bit at a specific position in the <see cref="BitArray"/>.
        /// </summary>
        /// <param name="index">The zero-based index of the value to get.</param>
        /// <param name="value">The Boolean value to assign to the bit.</param>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is less than zero.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is greater than or equal to <see cref="Count"/>.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Set(int index, bool value)
        {
            if ((uint)index >= (uint)m_length)
            {
                ThrowArgumentOutOfRangeException(index);
            }

            int bitMask = ReverseIfBE(1 << index);

            ref int segment = ref m_array[index >> BitShiftPerInt32];

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

        /// <summary>
        /// Sets all bits in the <see cref="BitArray"/> to the specified value.
        /// </summary>
        /// <param name="value">The Boolean value to assign to all bits.</param>
        public void SetAll(bool value)
        {
            int arrayLength = GetInt32ArrayLengthFromBitLength(Length);
            Span<int> span = m_array.AsSpan(0, arrayLength);
            if (value)
            {
                span.Fill(-1);

                // Clear high bit values in the last int.
                int extraBits = (int)((uint)m_length % BitsPerInt32);
                if (extraBits != 0)
                {
                    span[^1] &= ReverseIfBE((1 << extraBits) - 1);
                }
            }
            else
            {
                span.Clear();
            }

            _version++;
        }


        /// <summary>
        /// Performs the bitwise AND operation between the elements of the current <see cref="BitArray"/> object and the
        /// corresponding elements in the specified array. The current <see cref="BitArray"/> object will be modified to
        /// store the result of the bitwise AND operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise AND operation.</param>
        /// <returns>An array containing the result of the bitwise AND operation, which is a reference to the current <see cref="BitArray"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> and the current <see cref="BitArray"/> do not have the same number of elements.</exception>
        public BitArray And(BitArray value) => Invoke<AndBinaryOp>(value);

        /// <summary>
        /// Performs the bitwise OR operation between the elements of the current <see cref="BitArray"/> object and the
        /// corresponding elements in the specified array. The current <see cref="BitArray"/> object will be modified to
        /// store the result of the bitwise OR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise OR operation.</param>
        /// <returns>An array containing the result of the bitwise OR operation, which is a reference to the current <see cref="BitArray"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> and the current <see cref="BitArray"/> do not have the same number of elements.</exception>
        public BitArray Or(BitArray value) => Invoke<OrBinaryOp>(value);

        /// <summary>
        /// Performs the bitwise XOR operation between the elements of the current <see cref="BitArray"/> object and the
        /// corresponding elements in the specified array. The current <see cref="BitArray"/> object will be modified to
        /// store the result of the bitwise XOR operation.
        /// </summary>
        /// <param name="value">The array with which to perform the bitwise XOR operation.</param>
        /// <returns>An array containing the result of the bitwise XOR operation, which is a reference to the current <see cref="BitArray"/> object.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException"><paramref name="value"/> and the current <see cref="BitArray"/> do not have the same number of elements.</exception>
        public BitArray Xor(BitArray value) => Invoke<XorBinaryOp>(value);

        /// <summary>
        /// Inverts all the bit values in the current <see cref="BitArray"/>, so that elements set to true are changed to false,
        /// and elements set to false are changed to true.
        /// </summary>
        /// <returns>The current instance with inverted bit values.</returns>
        public BitArray Not() => Invoke<NotBinaryOp>(this); // argument is ignored

        /// <summary>Provides the implementation for <see cref="And"/>, <see cref="Or"/>, etc.</summary>
        private BitArray Invoke<TBinaryOp>(BitArray value) where TBinaryOp : struct, IBinaryOp
        {
            ArgumentNullException.ThrowIfNull(value);

            // This method uses unsafe code to manipulate data in the BitArrays.  To avoid issues with
            // buggy code concurrently mutating these instances in a way that could cause memory corruption,
            // we snapshot the arrays from both and then operate only on those snapshots, while also validating
            // that the count we iterate to is within the bounds of both arrays.  We don't care about such code
            // corrupting the BitArray data in a way that produces incorrect answers, since BitArray is not meant
            // to be thread-safe; we only care about avoiding buffer overruns.
            int[] thisArray = m_array;
            int[] valueArray = value.m_array;
            int count = GetInt32ArrayLengthFromBitLength(Length);

            if (Length != value.Length ||
                (uint)count > (uint)thisArray.Length ||
                (uint)count > (uint)valueArray.Length)
            {
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);
            }

            // Unroll loop for count less than Vector256 size.
            switch (count)
            {
                case 7: thisArray[6] = TBinaryOp.Invoke(thisArray[6], valueArray[6]); goto case 6;
                case 6: thisArray[5] = TBinaryOp.Invoke(thisArray[5], valueArray[5]); goto case 5;
                case 5: thisArray[4] = TBinaryOp.Invoke(thisArray[4], valueArray[4]); goto case 4;
                case 4: thisArray[3] = TBinaryOp.Invoke(thisArray[3], valueArray[3]); goto case 3;
                case 3: thisArray[2] = TBinaryOp.Invoke(thisArray[2], valueArray[2]); goto case 2;
                case 2: thisArray[1] = TBinaryOp.Invoke(thisArray[1], valueArray[1]); goto case 1;
                case 1: thisArray[0] = TBinaryOp.Invoke(thisArray[0], valueArray[0]); goto Done;
                case 0: goto Done;
            }

            uint i = 0;

            if (Vector512.IsHardwareAccelerated)
            {
                Apply<Vector512<int>>(count, ref i, thisArray, valueArray);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                Apply<Vector256<int>>(count, ref i, thisArray, valueArray);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                Apply<Vector128<int>>(count, ref i, thisArray, valueArray);
            }

            for (; i < (uint)count; i++)
            {
                thisArray[i] = TBinaryOp.Invoke(thisArray[i], valueArray[i]);
            }

            Done:
            _version++;
            return this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static void Apply<TVector>(int count, ref uint i, int[] thisArray, int[] valueArray)
                where TVector : ISimdVector<TVector, int>
            {
                ref int left = ref MemoryMarshal.GetArrayDataReference(thisArray);
                ref int right = ref MemoryMarshal.GetArrayDataReference(valueArray);

                for (; i < (uint)count - (TVector.ElementCount - 1u); i += (uint)TVector.ElementCount)
                {
                    TVector result = TBinaryOp.Invoke(TVector.LoadUnsafe(ref left, i), TVector.LoadUnsafe(ref right, i));
                    result.StoreUnsafe(ref left, i);
                }
            }
        }

        private struct AndBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 & value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, int> => value1 & value2;
        }

        private struct OrBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 | value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, int> => value1 | value2;
        }

        private struct XorBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 ^ value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, int> => value1 ^ value2;
        }

        private struct NotBinaryOp : IBinaryOp // not isn't binary, so second argument is just ignored
        {
            public static int Invoke(int value1, int _) => ~value1;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, int> => ~value1;
        }

        private interface IBinaryOp
        {
            static abstract int Invoke(int value1, int value2);
            static abstract TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, int>;
        }

        /// <summary>
        /// Shifts all the bit values of the current <see cref="BitArray"/> to the right on <paramref name="count"/> bits.
        /// </summary>
        /// <param name="count">The number of shifts to make for each bit.</param>
        /// <returns>The current <see cref="BitArray"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
        public BitArray RightShift(int count)
        {
            if (count <= 0)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(count);

                _version++;
                return this;
            }

            int toIndex = 0;
            int ints = GetInt32ArrayLengthFromBitLength(m_length);
            if (count < m_length)
            {
                (int fromIndex, int shiftCount) = Math.DivRem(count, BitsPerInt32);
                int extraBits = (int)((uint)m_length % BitsPerInt32);
                if (shiftCount == 0)
                {
                    // Cannot use `(1u << extraBits) - 1u` as the mask
                    // because for extraBits == 0, we need the mask to be 111...111, not 0.
                    // In that case, we are shifting a uint by 32, which could be considered undefined.
                    // The result of a shift operation is undefined ... if the right operand
                    // is greater than or equal to the width in bits of the promoted left operand,
                    // https://learn.microsoft.com/cpp/c-language/bitwise-shift-operators?view=vs-2017
                    // However, the compiler protects us from undefined behaviour by constraining the
                    // right operand to between 0 and width - 1 (inclusive), i.e. right_operand = (right_operand % width).
                    m_array[ints - 1] &= (int)ReverseIfBE(uint.MaxValue >> (BitsPerInt32 - extraBits));

                    Array.Copy(m_array, fromIndex, m_array, 0, ints - fromIndex);
                    toIndex = ints - fromIndex;
                }
                else
                {
                    int lastIndex = ints - 1;

                    while (fromIndex < lastIndex)
                    {
                        uint right = ReverseIfBE((uint)m_array[fromIndex]) >> shiftCount;
                        int left = ReverseIfBE(m_array[++fromIndex]) << BitsPerInt32 - shiftCount;
                        m_array[toIndex++] = ReverseIfBE(left | (int)right);
                    }

                    uint mask = uint.MaxValue >> (BitsPerInt32 - extraBits);
                    mask &= (uint)ReverseIfBE(m_array[fromIndex]);
                    m_array[toIndex++] = (int)ReverseIfBE(mask >> shiftCount);
                }
            }

            m_array.AsSpan(toIndex, ints - toIndex).Clear();
            _version++;
            return this;
        }

        /// <summary>
        /// Shifts all the bit values of the current <see cref="BitArray"/> to the left on <paramref name="count"/> bits.
        /// </summary>
        /// <param name="count">The number of shifts to make for each bit.</param>
        /// <returns>The current <see cref="BitArray"/>.</returns>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than zero.</exception>
        public BitArray LeftShift(int count)
        {
            if (count <= 0)
            {
                ArgumentOutOfRangeException.ThrowIfNegative(count);

                _version++;
                return this;
            }

            int lengthToClear;
            if (count < m_length)
            {
                int lastIndex = (m_length - 1) >> BitShiftPerInt32;  // Divide by 32.

                (lengthToClear, int shiftCount) = Math.DivRem(count, BitsPerInt32);

                if (shiftCount == 0)
                {
                    Array.Copy(m_array, 0, m_array, lengthToClear, lastIndex + 1 - lengthToClear);
                }
                else
                {
                    int fromindex = lastIndex - lengthToClear;

                    while (fromindex > 0)
                    {
                        int left = ReverseIfBE(m_array[fromindex]) << shiftCount;
                        uint right = (uint)ReverseIfBE(m_array[--fromindex]) >> (BitsPerInt32 - shiftCount);
                        m_array[lastIndex] = ReverseIfBE(left | (int)right);
                        lastIndex--;
                    }
                    m_array[lastIndex] = ReverseIfBE(ReverseIfBE(m_array[fromindex]) << shiftCount);
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

        /// <summary>
        /// Gets or sets the number of elements in the <see cref="BitArray"/>.
        /// </summary>
        /// <value>The number of elements in the <see cref="BitArray"/>.</value>
        /// <exception cref="ArgumentOutOfRangeException">The property is set to a value that is less than zero.</exception>
        public int Length
        {
            get => m_length;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                const int ShrinkThreshold = 256;

                int newints = GetInt32ArrayLengthFromBitLength(value);
                if (newints > m_array.Length || newints + ShrinkThreshold < m_array.Length)
                {
                    // Grow or shrink (if wasting more than ShrinkThreshold ints).
                    Array.Resize(ref m_array, newints);
                }

                if (value > m_length)
                {
                    // Clear high bit values in the last int.
                    int last = (m_length - 1) >> BitShiftPerInt32;
                    int bits = (int)((uint)m_length % BitsPerInt32);
                    if (bits > 0)
                    {
                        m_array[last] &= ReverseIfBE(1 << bits) - 1;
                    }

                    // Clear remaining int values.
                    m_array.AsSpan(last + 1, newints - last - 1).Clear();
                }

                m_length = value;
                _version++;
            }
        }

        /// <inheritdoc/>
        public unsafe void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);
            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Rank != 1)
            {
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));
            }

            if (array is int[] intArray)
            {
                if (array.Length - index < GetInt32ArrayLengthFromBitLength(m_length))
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                (int quotient, int extraBits) = Math.DivRem(m_length, BitsPerInt32);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Copy(m_array, 0, intArray, index, quotient);
                }
                else
                {
                    BinaryPrimitives.ReverseEndianness(m_array.AsSpan(0, quotient), intArray.AsSpan(index, quotient));
                }

                if (extraBits > 0)
                {
                    // The last int needs to be masked.
                    intArray[index + quotient] = m_array[quotient] & ReverseIfBE((1 << extraBits) - 1);
                }
            }
            else if (array is byte[] byteArray)
            {
                int arrayLength = GetByteArrayLengthFromBitLength(m_length);
                if ((array.Length - index) < arrayLength)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                Span<byte> span = byteArray.AsSpan(index);
                MemoryMarshal.AsBytes(m_array).Slice(0, arrayLength).CopyTo(span);
                if ((uint)m_length % BitsPerByte is uint toMask && toMask != 0)
                {
                    span[^1] &= (byte)((1 << (int)toMask) - 1);
                }
            }
            else if (array is bool[] boolArray)
            {
                if (array.Length - index < m_length)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                uint i = 0;

                if (!BitConverter.IsLittleEndian || m_length < BitsPerInt32)
                {
                    goto LessThan32;
                }

                // The mask used when shuffling a single int into Vector128/256/512.
                // On little endian machines, the lower 8 bits of int belong in the first byte, next lower 8 in the second and so on.
                // We place the bytes that contain the bits to its respective byte so that we can mask out only the relevant bits later.
                Vector128<byte> lowerShuffleMask_CopyToBoolArray = Vector128.Create(0, 0x01010101_01010101).AsByte();
                Vector128<byte> upperShuffleMask_CopyToBoolArray = Vector128.Create(0x02020202_02020202, 0x03030303_03030303).AsByte();

                if (Avx512BW.IsSupported && (uint)m_length >= Vector512<byte>.Count)
                {
                    Vector256<byte> upperShuffleMask_CopyToBoolArray256 = Vector256.Create(0x04040404_04040404, 0x05050505_05050505,
                                                                                             0x06060606_06060606, 0x07070707_07070707).AsByte();
                    Vector256<byte> lowerShuffleMask_CopyToBoolArray256 = Vector256.Create(lowerShuffleMask_CopyToBoolArray, upperShuffleMask_CopyToBoolArray);
                    Vector512<byte> shuffleMask = Vector512.Create(lowerShuffleMask_CopyToBoolArray256, upperShuffleMask_CopyToBoolArray256);
                    Vector512<byte> bitMask = Vector512.Create(0x80402010_08040201).AsByte();
                    Vector512<byte> ones = Vector512.Create((byte)1);

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector512<byte>.Count) <= (uint)m_length; i += (uint)Vector512<byte>.Count)
                        {
                            ulong bits = (ulong)(uint)m_array[i / (uint)BitsPerInt32] + ((ulong)m_array[(i / (uint)BitsPerInt32) + 1] << BitsPerInt32);
                            Vector512<ulong> scalar = Vector512.Create(bits);
                            Vector512<byte> shuffled = Avx512BW.Shuffle(scalar.AsByte(), shuffleMask);
                            Vector512<byte> extracted = Avx512F.And(shuffled, bitMask);

                            // The extracted bits can be anywhere between 0 and 255, so we normalise the value to either 0 or 1
                            // to ensure compatibility with "C# bool" (0 for false, 1 for true, rest undefined)
                            Vector512<byte> normalized = Avx512BW.Min(extracted, ones);
                            Avx512F.Store((byte*)destination + i, normalized);
                        }
                    }
                }
                else if (Avx2.IsSupported && (uint)m_length >= Vector256<byte>.Count)
                {
                    Vector256<byte> shuffleMask = Vector256.Create(lowerShuffleMask_CopyToBoolArray, upperShuffleMask_CopyToBoolArray);
                    Vector256<byte> bitMask = Vector256.Create(0x80402010_08040201).AsByte();
                    Vector256<byte> ones = Vector256.Create((byte)1);

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector256<byte>.Count) <= (uint)m_length; i += (uint)Vector256<byte>.Count)
                        {
                            int bits = m_array[i / (uint)BitsPerInt32];
                            Vector256<int> scalar = Vector256.Create(bits);
                            Vector256<byte> shuffled = Avx2.Shuffle(scalar.AsByte(), shuffleMask);
                            Vector256<byte> extracted = Avx2.And(shuffled, bitMask);

                            // The extracted bits can be anywhere between 0 and 255, so we normalise the value to either 0 or 1
                            // to ensure compatibility with "C# bool" (0 for false, 1 for true, rest undefined)
                            Vector256<byte> normalized = Avx2.Min(extracted, ones);
                            Avx.Store((byte*)destination + i, normalized);
                        }
                    }
                }
                else if (Ssse3.IsSupported && ((uint)m_length >= Vector128<byte>.Count * 2u))
                {
                    Vector128<byte> lowerShuffleMask = lowerShuffleMask_CopyToBoolArray;
                    Vector128<byte> upperShuffleMask = upperShuffleMask_CopyToBoolArray;
                    Vector128<byte> ones = Vector128.Create((byte)1);
                    Vector128<byte> bitMask128 = Vector128.Create(0x80402010_08040201).AsByte();

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector128<byte>.Count * 2u) <= (uint)m_length; i += (uint)Vector128<byte>.Count * 2u)
                        {
                            int bits = m_array[i / (uint)BitsPerInt32];
                            Vector128<int> scalar = Vector128.CreateScalarUnsafe(bits);

                            Vector128<byte> shuffledLower = Ssse3.Shuffle(scalar.AsByte(), lowerShuffleMask);
                            Vector128<byte> extractedLower = Sse2.And(shuffledLower, bitMask128);
                            Vector128<byte> normalizedLower = Sse2.Min(extractedLower, ones);
                            Sse2.Store((byte*)destination + i, normalizedLower);

                            Vector128<byte> shuffledHigher = Ssse3.Shuffle(scalar.AsByte(), upperShuffleMask);
                            Vector128<byte> extractedHigher = Sse2.And(shuffledHigher, bitMask128);
                            Vector128<byte> normalizedHigher = Sse2.Min(extractedHigher, ones);
                            Sse2.Store((byte*)destination + i + Vector128<byte>.Count, normalizedHigher);
                        }
                    }
                }
                else if (AdvSimd.Arm64.IsSupported)
                {
                    Vector128<byte> ones = Vector128.Create((byte)1);
                    Vector128<byte> bitMask128 = Vector128.Create(0x80402010_08040201).AsByte();

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector128<byte>.Count * 2u) <= (uint)m_length; i += (uint)Vector128<byte>.Count * 2u)
                        {
                            int bits = m_array[i / (uint)BitsPerInt32];
                            // Same logic as SSSE3 path, except we do not have Shuffle instruction.
                            // (TableVectorLookup could be an alternative - dotnet/runtime#1277)
                            // Instead we use chained ZIP1/2 instructions:
                            // (A0 is the byte containing LSB, A3 is the byte containing MSB)
                            // bits                                 - A0 A1 A2 A3
                            // v1 = Vector128.Create                - A0 A1 A2 A3 A0 A1 A2 A3 A0 A1 A2 A3 A0 A1 A2 A3
                            // v2 = ZipLow(v1, v1)                  - A0 A0 A1 A1 A2 A2 A3 A3 A0 A0 A1 A1 A2 A2 A3 A3
                            // v3 = ZipLow(v2, v2)                  - A0 A0 A0 A0 A1 A1 A1 A1 A2 A2 A2 A2 A3 A3 A3 A3
                            // shuffledLower = ZipLow(v3, v3)       - A0 A0 A0 A0 A0 A0 A0 A0 A1 A1 A1 A1 A1 A1 A1 A1
                            // shuffledHigher = ZipHigh(v3, v3)     - A2 A2 A2 A2 A2 A2 A2 A2 A3 A3 A3 A3 A3 A3 A3 A3

                            Vector128<byte> vector = Vector128.Create(bits).AsByte();
                            vector = AdvSimd.Arm64.ZipLow(vector, vector);
                            vector = AdvSimd.Arm64.ZipLow(vector, vector);

                            Vector128<byte> shuffledLower = AdvSimd.Arm64.ZipLow(vector, vector);
                            Vector128<byte> extractedLower = AdvSimd.And(shuffledLower, bitMask128);
                            Vector128<byte> normalizedLower = AdvSimd.Min(extractedLower, ones);

                            Vector128<byte> shuffledHigher = AdvSimd.Arm64.ZipHigh(vector, vector);
                            Vector128<byte> extractedHigher = AdvSimd.And(shuffledHigher, bitMask128);
                            Vector128<byte> normalizedHigher = AdvSimd.Min(extractedHigher, ones);

                            AdvSimd.Arm64.StorePair((byte*)destination + i, normalizedLower, normalizedHigher);
                        }
                    }
                }

            LessThan32:
                for (; i < (uint)m_length; i++)
                {
                    (uint elementIndex, uint extraBits) = Math.DivRem(i, BitsPerInt32);
                    boolArray[(uint)index + i] = ((m_array[elementIndex] >> (int)extraBits) & 0x00000001) != 0;
                }
            }
            else
            {
                throw new ArgumentException(SR.Arg_BitArrayTypeUnsupported, nameof(array));
            }
        }

        /// <summary>
        /// Determines whether all bits in the <see cref="BitArray"/> are set to <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if every bit in the <see cref="BitArray"/> is set to <c>true</c>, or if <see cref="BitArray"/> is empty; otherwise, <c>false</c>.</returns>
        public bool HasAllSet()
        {
            int extraBits = (int)((uint)m_length % BitsPerInt32);
            int intCount = GetInt32ArrayLengthFromBitLength(m_length);
            if (extraBits != 0)
            {
                intCount--;
            }

            const int AllSetBits = -1; // 0xFF_FF_FF_FF
            if (m_array.AsSpan(0, intCount).ContainsAnyExcept(AllSetBits))
            {
                return false;
            }

            if (extraBits == 0)
            {
                return true;
            }

            Debug.Assert(GetInt32ArrayLengthFromBitLength(m_length) > 0);
            Debug.Assert(intCount == GetInt32ArrayLengthFromBitLength(m_length) - 1);

            int mask = ReverseIfBE(1 << extraBits) - 1;
            return (m_array[intCount] & mask) == mask;
        }

        /// <summary>
        /// Determines whether any bit in the <see cref="BitArray"/> is set to <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if <see cref="BitArray"/> is not empty and at least one of its bit is set to <c>true</c>; otherwise, <c>false</c>.</returns>
        public bool HasAnySet()
        {
            int extraBits = (int)((uint)m_length % BitsPerInt32);
            int intCount = GetInt32ArrayLengthFromBitLength(m_length);
            if (extraBits != 0)
            {
                intCount--;
            }

            if (m_array.AsSpan(0, intCount).ContainsAnyExcept(0))
            {
                return true;
            }

            if (extraBits == 0)
            {
                return false;
            }

            Debug.Assert(GetInt32ArrayLengthFromBitLength(m_length) > 0);
            Debug.Assert(intCount == GetInt32ArrayLengthFromBitLength(m_length) - 1);

            return (m_array[intCount] & ReverseIfBE(1 << extraBits) - 1) != 0;
        }

        /// <summary>Gets the number of elements contained in the <see cref="BitArray"/>.</summary>
        public int Count => m_length;

        /// <summary>Gets an object that can be used to synchronize access to the <see cref="BitArray"/>.</summary>
        public object SyncRoot => this;

        /// <summary>Gets a value indicating whether access to the <see cref="BitArray"/> is synchronized (thread safe).</summary>
        public bool IsSynchronized => false;

        /// <summary>Gets a value indicating whether the <see cref="BitArray"/> is read-only.</summary>
        public bool IsReadOnly => false;

        /// <summary>Creates a shallow copy of the <see cref="BitArray"/>.</summary>
        public object Clone() => new BitArray(this);

        /// <summary>Returns an enumerator that iterates through the <see cref="BitArray"/>.</summary>
        /// <returns>An IEnumerator for the entire <see cref="BitArray"/>.</returns>
        public IEnumerator GetEnumerator() => new BitArrayEnumeratorSimple(this);

        // XPerY=n means that n Xs can be stored in 1 Y.
        private const int BitsPerInt32 = 32;
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
            // Due to sign extension, we don't need to special case for n == 0, since ((n - 1) >> 2) + 1 = 0
            // This doesn't hold true for ((n - 1) / 4) + 1, which equals 1.
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftForBytesPerInt32)) >> BitShiftForBytesPerInt32);
        }

        internal static int GetByteArrayLengthFromBitLength(int n)
        {
            // Due to sign extension, we don't need to special case for n == 0, since ((n - 1) >> 3) + 1 = 0
            // This doesn't hold true for ((n - 1) / 8) + 1, which equals 1.
            Debug.Assert(n >= 0);
            return (int)((uint)(n - 1 + (1 << BitShiftPerByte)) >> BitShiftPerByte);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReverseIfBE(int value) =>
            BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static uint ReverseIfBE(uint value) =>
            BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);

        private static void ThrowArgumentOutOfRangeException(int index) =>
            throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_IndexMustBeLess);

        private sealed class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private readonly BitArray _bitArray;
            private readonly int _version;
            private int _index;
            private bool _currentElement;

            internal BitArrayEnumeratorSimple(BitArray bitArray)
            {
                _bitArray = bitArray;
                _index = -1;
                _version = bitArray._version;
            }

            public object Clone() => MemberwiseClone();

            public bool MoveNext()
            {
                if (_version != _bitArray._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

                if (_index < (_bitArray.m_length - 1))
                {
                    _index++;
                    _currentElement = _bitArray.Get(_index);
                    return true;
                }

                _index = _bitArray.m_length;
                return false;
            }

            public object Current
            {
                get
                {
                    if ((uint)_index >= (uint)_bitArray.m_length)
                    {
                        throw GetInvalidOperationException(_index);
                    }

                    return _currentElement;
                }
            }

            public void Reset()
            {
                if (_version != _bitArray._version)
                {
                    throw new InvalidOperationException(SR.InvalidOperation_EnumFailedVersion);
                }

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
                    Debug.Assert(index >= _bitArray.m_length);
                    return new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                }
            }
        }
    }
}
