// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.Arm;
using System.Runtime.Intrinsics.X86;
using System.Runtime.Serialization;

namespace System.Collections
{
    /// <summary>
    /// Manages a compact array of bit values, which are represented as <see cref="bool"/>, where
    /// <see langword="true"/> indicates that the bit is on (1) and <see langword="false"/> indicates
    /// the bit is off (0).
    /// </summary>
    [Serializable]
    [TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public sealed class BitArray : ICollection, ICloneable,
        ISerializable // introduced in .NET 10 for compat with existing serialized assets, not exposed in the ref assembly
    {
        /// <summary>sizeof(int) * 8</summary>
        private const int BitsPerInt32 = 32;
        /// <summary>sizeof(byte) * 8</summary>
        private const int BitsPerByte = 8;

        /// <summary>The array of bytes used to store bits.</summary>
        /// <remarks>
        /// The array is allocated to hold enough bytes to store the specified number of bits, rounded up to the nearest multiple
        /// of sizeof(int). The last four bytes that might contain valid bits are always kept in a state where the unused bits
        /// are cleared, such that AsBytes and CopyTo operations will not show any set bits that are not actually set.
        /// </remarks>
        internal byte[] _array;

        /// <summary>The number of bits in the array.</summary>
        private int _bitLength;

        /// <summary>Version number incremented on mutation, used to invalidate enumerators.</summary>
        private int _version;

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

            _array = AllocateByteArray(length);
            _bitLength = length;

            if (defaultValue)
            {
                Array.Fill(_array, (byte)0xFF);
                ClearHighExtraBits();
            }
        }

        /// <summary>Deserializes BitArray in a way that's compatible with the original .NET Framework implementation.</summary>
        private BitArray(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            var array = (int[]?)info.GetValue("m_array", typeof(int[]));
            _bitLength = info.GetInt32("m_length");
            _version = info.GetInt32("_version");

            if (array is null || (uint)_bitLength > checked((uint)array.Length * BitsPerInt32))
            {
                throw new SerializationException(SR.Serialization_InvalidData);
            }

            _array = AllocateByteArray(_bitLength);
            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.AsBytes(array).CopyTo(_array);
            }
            else
            {
                BinaryPrimitives.ReverseEndianness(array, MemoryMarshal.Cast<byte, int>((Span<byte>)_array));
            }
        }

        /// <summary>Generates serialization data for the BitArray in a way that's compatible with the original .NET Framework implementation.</summary>
        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            var m_array = new int[GetInt32ArrayLengthFromBitLength(_bitLength)];
            CopyTo(m_array, 0);

            info.AddValue("m_array", m_array);
            info.AddValue("m_length", _bitLength);
            info.AddValue("_version", _version);
        }

        private void ClearHighExtraBits()
        {
            (uint index, uint extraBits) = Math.DivRem((uint)_bitLength, BitsPerInt32);
            if (extraBits != 0)
            {
                MemoryMarshal.Cast<byte, int>((Span<byte>)_array)[(int)index] &= ReverseIfBE((1 << (int)extraBits) - 1);
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

            _bitLength = bytes.Length * BitsPerByte;
            _array = AllocateByteArray(_bitLength);

            Array.Copy(bytes, _array, bytes.Length);
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

            _array = AllocateByteArray(values.Length);
            _bitLength = values.Length;

            uint i = 0;

            if (!BitConverter.IsLittleEndian || values.Length < Vector256<byte>.Count)
            {
                goto Remainder;
            }

            // Comparing with 1s would get rid of the final negation, however this would not work for some CLR bools
            // (true for any non-zero values, false for 0) - any values between 2-255 will be interpreted as false.
            // Instead, We compare with zeroes (== false) then negate the result to ensure compatibility.

            ref byte arrayRef = ref MemoryMarshal.GetArrayDataReference(_array);
            ref byte value = ref Unsafe.As<bool, byte>(ref MemoryMarshal.GetArrayDataReference<bool>(values));
            if (Vector512.IsHardwareAccelerated)
            {
                for (; i <= (uint)values.Length - Vector512<byte>.Count; i += (uint)Vector512<byte>.Count)
                {
                    Vector512<byte> vector = Vector512.LoadUnsafe(ref value, i);
                    Vector512<byte> isFalse = Vector512.Equals(vector, Vector512<byte>.Zero);

                    ulong result = isFalse.ExtractMostSignificantBits();
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref arrayRef, sizeof(ulong) * (i / 64u)), ~result);
                }
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                for (; i <= (uint)values.Length - Vector256<byte>.Count; i += (uint)Vector256<byte>.Count)
                {
                    Vector256<byte> vector = Vector256.LoadUnsafe(ref value, i);
                    Vector256<byte> isFalse = Vector256.Equals(vector, Vector256<byte>.Zero);

                    uint result = isFalse.ExtractMostSignificantBits();
                    Unsafe.WriteUnaligned(ref Unsafe.Add(ref arrayRef, sizeof(uint) * (i / 32u)), ~result);
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

                    Unsafe.WriteUnaligned(
                        ref Unsafe.Add(ref arrayRef, sizeof(uint) * (i / 32u)),
                        ~((upperResult << 16) | lowerResult));
                }
            }

        Remainder:
            for (; i < (uint)values.Length; i++)
            {
                if (values[i])
                {
                    (uint byteIndex, uint bitOffset) = Math.DivRem(i, BitsPerByte);
                    _array[byteIndex] |= (byte)(1 << (int)bitOffset);
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

            _bitLength = values.Length * BitsPerInt32;
            _array = AllocateByteArray(_bitLength);

            if (BitConverter.IsLittleEndian)
            {
                MemoryMarshal.AsBytes(values).CopyTo(_array);
            }
            else
            {
                BinaryPrimitives.ReverseEndianness(values, MemoryMarshal.Cast<byte, int>((Span<byte>)_array));
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BitArray"/> class that contains bit values copied from the specified BitArray.
        /// </summary>
        /// <param name="bits">The <see cref="BitArray"/> to copy.</param>
        /// <exception cref="ArgumentNullException"><paramref name="bits"/> is null.</exception>
        public BitArray(BitArray bits)
        {
            ArgumentNullException.ThrowIfNull(bits);

            _bitLength = bits._bitLength;
            _array = AllocateByteArray(_bitLength);

            Array.Copy(bits._array, _array, _array.Length);
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
            if ((uint)index >= (uint)_bitLength)
            {
                ThrowArgumentOutOfRangeException(index);
            }

            (uint byteIndex, uint bitOffset) = Math.DivRem((uint)index, BitsPerByte);
            return ((_array[byteIndex]) & (1 << (int)bitOffset)) != 0;
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
            if ((uint)index >= (uint)_bitLength)
            {
                ThrowArgumentOutOfRangeException(index);
            }

            (uint byteIndex, uint bitOffset) = Math.DivRem((uint)index, BitsPerByte);

            ref byte segment = ref _array[byteIndex];
            byte bitMask = (byte)(1 << (int)bitOffset);
            if (value)
            {
                segment |= bitMask;
            }
            else
            {
                segment &= (byte)~bitMask;
            }

            _version++;
        }

        /// <summary>
        /// Sets all bits in the <see cref="BitArray"/> to the specified value.
        /// </summary>
        /// <param name="value">The Boolean value to assign to all bits.</param>
        public void SetAll(bool value)
        {
            if (value)
            {
                _array.AsSpan(0, GetByteArrayLengthFromBitLength(_bitLength)).Fill(0xFF);
                ClearHighExtraBits();
            }
            else
            {
                _array.AsSpan(0, GetByteArrayLengthFromBitLength(_bitLength)).Clear();
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
        public BitArray Not()
        {
            Invoke<NotBinaryOp>(this); // argument is ignored
            ClearHighExtraBits(); // applying ~ to last Int32 may set extra bits we're trying to keep clear
            return this;
        }

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
            byte[] thisArray = _array;
            byte[] valueArray = value._array;
            int count = GetByteArrayLengthFromBitLength(Length);

            if (Length != value.Length ||
                (uint)count > (uint)thisArray.Length ||
                (uint)count > (uint)valueArray.Length)
            {
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);
            }

            int i = 0;

            if (Vector512.IsHardwareAccelerated)
            {
                i = Apply<Vector512<byte>>(count, thisArray, valueArray);
            }
            else if (Vector256.IsHardwareAccelerated)
            {
                i = Apply<Vector256<byte>>(count, thisArray, valueArray);
            }
            else if (Vector128.IsHardwareAccelerated)
            {
                i = Apply<Vector128<byte>>(count, thisArray, valueArray);
            }

            // Process remaining.
            if (i != count)
            {
                int roundedBytesRemaining = RoundUpToMultipleSizeOfInt32(count - i);
                Span<int> thisRemaining = MemoryMarshal.Cast<byte, int>(((Span<byte>)thisArray).Slice(i, roundedBytesRemaining));
                Span<int> valueRemaining = MemoryMarshal.Cast<byte, int>(((Span<byte>)valueArray).Slice(i, roundedBytesRemaining));
                for (i = 0; i < thisRemaining.Length; i++)
                {
                    thisRemaining[i] = TBinaryOp.Invoke(thisRemaining[i], valueRemaining[i]);
                }
            }

            _version++;
            return this;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            static int Apply<TVector>(int count, byte[] thisArray, byte[] valueArray)
                where TVector : ISimdVector<TVector, byte>
            {
                ref byte left = ref MemoryMarshal.GetArrayDataReference(thisArray);
                ref byte right = ref MemoryMarshal.GetArrayDataReference(valueArray);

                int i;

                for (i = 0; i <= count - TVector.ElementCount; i += TVector.ElementCount)
                {
                    TVector result = TBinaryOp.Invoke(TVector.LoadUnsafe(ref left, (uint)i), TVector.LoadUnsafe(ref right, (uint)i));
                    result.StoreUnsafe(ref left, (uint)i);
                }

                return i;
            }
        }

        private struct AndBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 & value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, byte> => value1 & value2;
        }

        private struct OrBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 | value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, byte> => value1 | value2;
        }

        private struct XorBinaryOp : IBinaryOp
        {
            public static int Invoke(int value1, int value2) => value1 ^ value2;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, byte> => value1 ^ value2;
        }

        private struct NotBinaryOp : IBinaryOp // not isn't binary, so second argument is just ignored
        {
            public static int Invoke(int value1, int _) => ~value1;
            public static TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, byte> => ~value1;
        }

        private interface IBinaryOp
        {
            static abstract int Invoke(int value1, int value2);
            static abstract TVector Invoke<TVector>(TVector value1, TVector value2) where TVector : ISimdVector<TVector, byte>;
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

            Span<int> intSpan = MemoryMarshal.Cast<byte, int>((Span<byte>)_array);

            int toIndex = 0;
            int ints = GetInt32ArrayLengthFromBitLength(_bitLength);
            if (count < _bitLength)
            {
                // We can not use Math.DivRem without taking a dependency on System.Runtime.Extensions
                (int fromIndex, int shiftCount) = Math.DivRem(count, 32);
                int extraBits = (int)((uint)_bitLength % 32);
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
                    uint mask = uint.MaxValue >> (BitsPerInt32 - extraBits);
                    intSpan[ints - 1] &= ReverseIfBE((int)mask);

                    intSpan.Slice((int)fromIndex, ints - fromIndex).CopyTo(intSpan);
                    toIndex = ints - fromIndex;
                }
                else
                {
                    int lastIndex = ints - 1;

                    while (fromIndex < lastIndex)
                    {
                        uint right = (uint)ReverseIfBE(intSpan[fromIndex]) >> shiftCount;
                        int left = ReverseIfBE(intSpan[++fromIndex]) << (BitsPerInt32 - shiftCount);
                        intSpan[toIndex++] = ReverseIfBE(left | (int)right);
                    }

                    uint mask = uint.MaxValue >> (BitsPerInt32 - extraBits);
                    mask &= (uint)ReverseIfBE(intSpan[fromIndex]);
                    intSpan[toIndex++] = ReverseIfBE((int)(mask >> shiftCount));
                }
            }

            intSpan.Slice(toIndex, ints - toIndex).Clear();
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

            Span<int> intSpan = MemoryMarshal.Cast<byte, int>((Span<byte>)_array);

            int lengthToClear;
            if (count < _bitLength)
            {
                int lastIndex = (int)((uint)(_bitLength - 1) / BitsPerInt32);

                (lengthToClear, int shiftCount) = Math.DivRem(count, BitsPerInt32);

                if (shiftCount == 0)
                {
                    intSpan.Slice(0, lastIndex + 1 - lengthToClear).CopyTo(intSpan.Slice(lengthToClear));
                }
                else
                {
                    int fromindex = lastIndex - lengthToClear;

                    while (fromindex > 0)
                    {
                        int left = ReverseIfBE(intSpan[fromindex]) << shiftCount;
                        uint right = (uint)ReverseIfBE(intSpan[--fromindex]) >> (BitsPerInt32 - shiftCount);
                        intSpan[lastIndex] = ReverseIfBE(left | (int)right);
                        lastIndex--;
                    }
                    intSpan[lastIndex] = ReverseIfBE(ReverseIfBE(intSpan[fromindex]) << shiftCount);
                }
            }
            else
            {
                lengthToClear = GetInt32ArrayLengthFromBitLength(_bitLength); // Clear all
            }

            intSpan.Slice(0, lengthToClear).Clear();
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
            get => _bitLength;
            set
            {
                ArgumentOutOfRangeException.ThrowIfNegative(value);

                int newByteLength = GetAlignedByteArrayLength(value);
                if (newByteLength > _array.Length)
                {
                    Array.Resize(ref _array, newByteLength);
                }
                else
                {
                    int currentByteLength = GetByteArrayLengthFromBitLength(_bitLength);
                    if (newByteLength > currentByteLength)
                    {
                        _array.AsSpan(currentByteLength).Clear();
                    }
                    else
                    {
                        // If we'll be shrinking by a significant amount, re-allocate to avoid wasting too much space.
                        const int ShrinkThreshold = 1024;
                        if (newByteLength < _array.Length - ShrinkThreshold)
                        {
                            Array.Resize(ref _array, newByteLength);
                        }
                    }
                }

                _bitLength = value;
                ClearHighExtraBits();

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
                int intLength = GetInt32ArrayLengthFromBitLength(_bitLength);

                if (array.Length - index < intLength)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                if (intLength > 0)
                {
                    Span<int> source = MemoryMarshal.Cast<byte, int>((Span<byte>)_array).Slice(0, intLength);
                    if (BitConverter.IsLittleEndian)
                    {
                        source.CopyTo(intArray.AsSpan(index));
                    }
                    else
                    {
                        BinaryPrimitives.ReverseEndianness(source, intArray.AsSpan(index));
                    }

                    uint extraBits = (uint)_bitLength % BitsPerInt32;
                    if (extraBits != 0)
                    {
                        intArray[index + intLength - 1] = ReverseIfBE(source[^1]) & ((1 << (int)extraBits) - 1);
                    }
                }
            }
            else if (array is byte[] byteArray)
            {
                int byteLength = GetByteArrayLengthFromBitLength(_bitLength);

                if ((array.Length - index) < byteLength)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                if (byteLength > 0)
                {
                    ReadOnlySpan<byte> source = _array.AsSpan(0, byteLength);
                    source.CopyTo(byteArray.AsSpan(index));

                    uint extraBits = (uint)_bitLength % BitsPerByte;
                    if (extraBits != 0)
                    {
                        byteArray[index + byteLength - 1] = (byte)(source[^1] & ((1 << (int)extraBits) - 1));
                    }
                }
            }
            else if (array is bool[] boolArray)
            {
                if (boolArray.Length - index < _bitLength)
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                uint i = 0;

                if (!BitConverter.IsLittleEndian || _bitLength < BitsPerInt32)
                {
                    goto Remainder;
                }

                Span<int> in32Span = MemoryMarshal.Cast<byte, int>((Span<byte>)_array);

                // The mask used when shuffling a single int into Vector128/256/512.
                // On little endian machines, the lower 8 bits of int belong in the first byte, next lower 8 in the second and so on.
                // We place the bytes that contain the bits to its respective byte so that we can mask out only the relevant bits later.
                Vector128<byte> lowerShuffleMask_CopyToBoolArray = Vector128.Create(0, 0x01010101_01010101).AsByte();
                Vector128<byte> upperShuffleMask_CopyToBoolArray = Vector128.Create(0x02020202_02020202, 0x03030303_03030303).AsByte();

                if (Avx512BW.IsSupported && (uint)_bitLength >= Vector512<byte>.Count)
                {
                    Vector256<byte> upperShuffleMask_CopyToBoolArray256 = Vector256.Create(0x04040404_04040404, 0x05050505_05050505,
                                                                                             0x06060606_06060606, 0x07070707_07070707).AsByte();
                    Vector256<byte> lowerShuffleMask_CopyToBoolArray256 = Vector256.Create(lowerShuffleMask_CopyToBoolArray, upperShuffleMask_CopyToBoolArray);
                    Vector512<byte> shuffleMask = Vector512.Create(lowerShuffleMask_CopyToBoolArray256, upperShuffleMask_CopyToBoolArray256);
                    Vector512<byte> bitMask = Vector512.Create(0x80402010_08040201).AsByte();
                    Vector512<byte> ones = Vector512.Create((byte)1);

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector512<byte>.Count) <= (uint)_bitLength; i += (uint)Vector512<byte>.Count)
                        {
                            ulong bits = (ulong)(uint)in32Span[(int)(i / (uint)BitsPerInt32)] + ((ulong)in32Span[(int)(i / (uint)BitsPerInt32) + 1] << BitsPerInt32);
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
                else if (Avx2.IsSupported && (uint)_bitLength >= Vector256<byte>.Count)
                {
                    Vector256<byte> shuffleMask = Vector256.Create(lowerShuffleMask_CopyToBoolArray, upperShuffleMask_CopyToBoolArray);
                    Vector256<byte> bitMask = Vector256.Create(0x80402010_08040201).AsByte();
                    Vector256<byte> ones = Vector256.Create((byte)1);

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector256<byte>.Count) <= (uint)_bitLength; i += (uint)Vector256<byte>.Count)
                        {
                            int bits = in32Span[(int)(i / (uint)BitsPerInt32)];
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
                else if (Ssse3.IsSupported && ((uint)_bitLength >= Vector128<byte>.Count * 2u))
                {
                    Vector128<byte> lowerShuffleMask = lowerShuffleMask_CopyToBoolArray;
                    Vector128<byte> upperShuffleMask = upperShuffleMask_CopyToBoolArray;
                    Vector128<byte> ones = Vector128.Create((byte)1);
                    Vector128<byte> bitMask128 = Vector128.Create(0x80402010_08040201).AsByte();

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector128<byte>.Count * 2u) <= (uint)_bitLength; i += (uint)Vector128<byte>.Count * 2u)
                        {
                            int bits = in32Span[(int)(i / (uint)BitsPerInt32)];
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
                        for (; (i + Vector128<byte>.Count * 2u) <= (uint)_bitLength; i += (uint)Vector128<byte>.Count * 2u)
                        {
                            int bits = in32Span[(int)(i / (uint)BitsPerInt32)];

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

            Remainder:
                for (; i < (uint)_bitLength; i++)
                {
                    (uint byteIndex, uint extraBits) = Math.DivRem(i, BitsPerByte);
                    boolArray[(uint)index + i] = (_array[byteIndex] & (1 << (int)extraBits)) != 0;
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
            uint extraBits = (uint)_bitLength % BitsPerByte;
            int byteCount = GetByteArrayLengthFromBitLength(_bitLength);
            if (extraBits != 0)
            {
                byteCount--;
            }

            if (_array.AsSpan(0, byteCount).ContainsAnyExcept((byte)0xFF))
            {
                return false;
            }

            if (extraBits == 0)
            {
                return true;
            }

            byte mask = (byte)((1 << (int)extraBits) - 1);
            return (_array[byteCount] & mask) == mask;
        }

        /// <summary>
        /// Determines whether any bit in the <see cref="BitArray"/> is set to <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if <see cref="BitArray"/> is not empty and at least one of its bit is set to <c>true</c>; otherwise, <c>false</c>.</returns>
        public bool HasAnySet()
        {
            uint extraBits = (uint)_bitLength % BitsPerByte;
            int byteCount = GetByteArrayLengthFromBitLength(_bitLength);
            if (extraBits != 0)
            {
                byteCount--;
            }

            if (_array.AsSpan(0, byteCount).ContainsAnyExcept((byte)0))
            {
                return true;
            }

            if (extraBits == 0)
            {
                return false;
            }

            byte mask = (byte)((1 << (int)extraBits) - 1);
            return (_array[byteCount] & mask) != 0;
        }

        /// <summary>Gets the number of elements contained in the <see cref="BitArray"/>.</summary>
        public int Count => _bitLength;

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

        /// <summary>Determines the number of <see cref="int"/>s required to store <paramref name="bitLength"/> bits.</summary>
        private static int GetInt32ArrayLengthFromBitLength(int bitLength)
        {
            Debug.Assert(bitLength >= 0);
            return (int)(((uint)bitLength + 31u) >> 5);
        }

        /// <summary>Determines the number of <see cref="byte"/>s required to store <paramref name="bitLength"/> bits.</summary>
        internal static int GetByteArrayLengthFromBitLength(int bitLength)
        {
            Debug.Assert(bitLength >= 0);
            return (int)(((uint)bitLength + 7u) >> 3);
        }

        /// <summary>Rounds <paramref name="value"/> up to a multiple of sizeof(int).</summary>
        private static int RoundUpToMultipleSizeOfInt32(int value) =>
            (value + (sizeof(int) - 1)) & ~(sizeof(int) - 1);

        private static int GetAlignedByteArrayLength(int bitLength) =>
            // Always allocate in groups of sizeof(int) bytes so that we can use MemoryMarshal.Cast<byte, int>
            // to manipulate as ints when desired.
            RoundUpToMultipleSizeOfInt32(GetByteArrayLengthFromBitLength(bitLength));

        /// <summary>Allocates a new byte array of the specified bit length, rounded up to the nearest multiple of sizeof(int).</summary>
        private static byte[] AllocateByteArray(int bitLength)
        {
            int byteLength = GetAlignedByteArrayLength(bitLength);
            Debug.Assert(byteLength >= 0, "byteLength should be non-negative.");
            Debug.Assert(byteLength % sizeof(int) == 0, "byteLength should be a multiple of sizeof(int).");
            return bitLength != 0 ? new byte[byteLength] : [];
        }

        /// <summary>Nop on little endian, reverses the endianness of <paramref name="value"/> on big endian.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static int ReverseIfBE(int value) =>
            BitConverter.IsLittleEndian ? value : BinaryPrimitives.ReverseEndianness(value);

        private static void ThrowArgumentOutOfRangeException(int index) =>
            throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_IndexMustBeLess);

        private sealed class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private static readonly object s_boxedTrue = true;
            private static readonly object s_boxedFalse = false;

            private readonly BitArray _bitArray;
            private readonly int _version;
            private int _index;
            private object _currentElement = s_boxedFalse;

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

                if (_index < (_bitArray._bitLength - 1))
                {
                    _index++;
                    _currentElement = _bitArray.Get(_index) ? s_boxedTrue : s_boxedFalse;
                    return true;
                }

                _index = _bitArray._bitLength;
                return false;
            }

            public object Current
            {
                get
                {
                    if ((uint)_index >= (uint)_bitArray._bitLength)
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
                    Debug.Assert(index >= _bitArray._bitLength);
                    return new InvalidOperationException(SR.InvalidOperation_EnumEnded);
                }
            }
        }
    }
}
