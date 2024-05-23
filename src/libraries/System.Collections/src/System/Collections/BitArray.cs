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
            ArgumentOutOfRangeException.ThrowIfNegative(length);

            m_array = new int[GetInt32ArrayLengthFromBitLength(length)];
            m_length = length;

            if (defaultValue)
            {
                Array.Fill(m_array, -1);

                // clear high bit values in the last int
                Div32Rem(length, out int extraBits);
                if (extraBits > 0)
                {
                    m_array[^1] = (1 << extraBits) - 1;
                }
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
            ArgumentNullException.ThrowIfNull(bytes);

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

        public unsafe BitArray(bool[] values)
        {
            ArgumentNullException.ThrowIfNull(values);

            m_array = new int[GetInt32ArrayLengthFromBitLength(values.Length)];
            m_length = values.Length;

            uint i = 0;

            if (values.Length < Vector256<byte>.Count)
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
                    int elementIndex = Div32Rem((int)i, out int extraBits);
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
            ArgumentNullException.ThrowIfNull(values);

            // this value is chosen to prevent overflow when computing m_length
            if (values.Length > int.MaxValue / BitsPerInt32)
            {
                throw new ArgumentException(SR.Format(SR.Argument_ArrayTooLarge, BitsPerInt32), nameof(values));
            }

            m_array = new int[values.Length];
            Array.Copy(values, m_array, values.Length);
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
            ArgumentNullException.ThrowIfNull(bits);

            int arrayLength = GetInt32ArrayLengthFromBitLength(bits.m_length);

            m_array = new int[arrayLength];

            Debug.Assert(bits.m_array.Length <= arrayLength);

            Array.Copy(bits.m_array, m_array, arrayLength);
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
            int arrayLength = GetInt32ArrayLengthFromBitLength(Length);
            Span<int> span = m_array.AsSpan(0, arrayLength);
            if (value)
            {
                span.Fill(-1);

                // clear high bit values in the last int
                Div32Rem(m_length, out int extraBits);
                if (extraBits > 0)
                {
                    span[^1] &= (1 << extraBits) - 1;
                }
            }
            else
            {
                span.Clear();
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
            if (Length != value.Length || (uint)count > (uint)thisArray.Length || (uint)count > (uint)valueArray.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            // Unroll loop for count less than Vector256 size.
            switch (count)
            {
                case 7: thisArray[6] &= valueArray[6]; goto case 6;
                case 6: thisArray[5] &= valueArray[5]; goto case 5;
                case 5: thisArray[4] &= valueArray[4]; goto case 4;
                case 4: thisArray[3] &= valueArray[3]; goto case 3;
                case 3: thisArray[2] &= valueArray[2]; goto case 2;
                case 2: thisArray[1] &= valueArray[1]; goto case 1;
                case 1: thisArray[0] &= valueArray[0]; goto Done;
                case 0: goto Done;
            }

            uint i = 0;

            ref int left = ref MemoryMarshal.GetArrayDataReference<int>(thisArray);
            ref int right = ref MemoryMarshal.GetArrayDataReference<int>(valueArray);
            if (Vector512.IsHardwareAccelerated && (uint)count >= Vector512<int>.Count)
            {
                for (; i < (uint)count - (Vector512<int>.Count - 1u); i += (uint)Vector512<int>.Count)
                {
                    Vector512<int> result = Vector512.LoadUnsafe(ref left, i) & Vector512.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)count >= Vector256<int>.Count)
            {
                for (; i < (uint)count - (Vector256<int>.Count - 1u); i += (uint)Vector256<int>.Count)
                {
                    Vector256<int> result = Vector256.LoadUnsafe(ref left, i) & Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)count >= Vector128<int>.Count)
            {
                for (; i < (uint)count - (Vector128<int>.Count - 1u); i += (uint)Vector128<int>.Count)
                {
                    Vector128<int> result = Vector128.LoadUnsafe(ref left, i) & Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < (uint)count; i++)
                thisArray[i] &= valueArray[i];

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
            if (Length != value.Length || (uint)count > (uint)thisArray.Length || (uint)count > (uint)valueArray.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            // Unroll loop for count less than Vector256 size.
            switch (count)
            {
                case 7: thisArray[6] |= valueArray[6]; goto case 6;
                case 6: thisArray[5] |= valueArray[5]; goto case 5;
                case 5: thisArray[4] |= valueArray[4]; goto case 4;
                case 4: thisArray[3] |= valueArray[3]; goto case 3;
                case 3: thisArray[2] |= valueArray[2]; goto case 2;
                case 2: thisArray[1] |= valueArray[1]; goto case 1;
                case 1: thisArray[0] |= valueArray[0]; goto Done;
                case 0: goto Done;
            }

            uint i = 0;

            ref int left = ref MemoryMarshal.GetArrayDataReference<int>(thisArray);
            ref int right = ref MemoryMarshal.GetArrayDataReference<int>(valueArray);
            if (Vector512.IsHardwareAccelerated && (uint)count >= Vector512<int>.Count)
            {
                for (; i < (uint)count - (Vector512<int>.Count - 1u); i += (uint)Vector512<int>.Count)
                {
                    Vector512<int> result = Vector512.LoadUnsafe(ref left, i) | Vector512.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)count >= Vector256<int>.Count)
            {
                for (; i < (uint)count - (Vector256<int>.Count - 1u); i += (uint)Vector256<int>.Count)
                {
                    Vector256<int> result = Vector256.LoadUnsafe(ref left, i) | Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)count >= Vector128<int>.Count)
            {
                for (; i < (uint)count - (Vector128<int>.Count - 1u); i += (uint)Vector128<int>.Count)
                {
                    Vector128<int> result = Vector128.LoadUnsafe(ref left, i) | Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < (uint)count; i++)
                thisArray[i] |= valueArray[i];

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
            if (Length != value.Length || (uint)count > (uint)thisArray.Length || (uint)count > (uint)valueArray.Length)
                throw new ArgumentException(SR.Arg_ArrayLengthsDiffer);

            // Unroll loop for count less than Vector256 size.
            switch (count)
            {
                case 7: thisArray[6] ^= valueArray[6]; goto case 6;
                case 6: thisArray[5] ^= valueArray[5]; goto case 5;
                case 5: thisArray[4] ^= valueArray[4]; goto case 4;
                case 4: thisArray[3] ^= valueArray[3]; goto case 3;
                case 3: thisArray[2] ^= valueArray[2]; goto case 2;
                case 2: thisArray[1] ^= valueArray[1]; goto case 1;
                case 1: thisArray[0] ^= valueArray[0]; goto Done;
                case 0: goto Done;
            }

            uint i = 0;

            ref int left = ref MemoryMarshal.GetArrayDataReference<int>(thisArray);
            ref int right = ref MemoryMarshal.GetArrayDataReference<int>(valueArray);

            if (Vector512.IsHardwareAccelerated && (uint)count >= Vector512<int>.Count)
            {
                for (; i < (uint)count - (Vector512<int>.Count - 1u); i += (uint)Vector512<int>.Count)
                {
                    Vector512<int> result = Vector512.LoadUnsafe(ref left, i) ^ Vector512.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)count >= Vector256<int>.Count)
            {
                for (; i < (uint)count - (Vector256<int>.Count - 1u); i += (uint)Vector256<int>.Count)
                {
                    Vector256<int> result = Vector256.LoadUnsafe(ref left, i) ^ Vector256.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)count >= Vector128<int>.Count)
            {
                for (; i < (uint)count - (Vector128<int>.Count - 1u); i += (uint)Vector128<int>.Count)
                {
                    Vector128<int> result = Vector128.LoadUnsafe(ref left, i) ^ Vector128.LoadUnsafe(ref right, i);
                    result.StoreUnsafe(ref left, i);
                }
            }

            for (; i < (uint)count; i++)
                thisArray[i] ^= valueArray[i];

        Done:
            _version++;
            return this;
        }

        /*=========================================================================
        ** Inverts all the bit values. On/true bit values are converted to
        ** off/false. Off/false bit values are turned on/true. The current instance
        ** is updated and returned.
        =========================================================================*/
        public unsafe BitArray Not()
        {
            // This method uses unsafe code to manipulate data in the BitArray.  To avoid issues with
            // buggy code concurrently mutating this instance in a way that could cause memory corruption,
            // we snapshot the array then operate only on this snapshot.  We don't care about such code
            // corrupting the BitArray data in a way that produces incorrect answers, since BitArray is not meant
            // to be thread-safe; we only care about avoiding buffer overruns.
            int[] thisArray = m_array;

            int count = GetInt32ArrayLengthFromBitLength(Length);

            // Unroll loop for count less than Vector256 size.
            switch (count)
            {
                case 7: thisArray[6] = ~thisArray[6]; goto case 6;
                case 6: thisArray[5] = ~thisArray[5]; goto case 5;
                case 5: thisArray[4] = ~thisArray[4]; goto case 4;
                case 4: thisArray[3] = ~thisArray[3]; goto case 3;
                case 3: thisArray[2] = ~thisArray[2]; goto case 2;
                case 2: thisArray[1] = ~thisArray[1]; goto case 1;
                case 1: thisArray[0] = ~thisArray[0]; goto Done;
                case 0: goto Done;
            }

            uint i = 0;

            ref int value = ref MemoryMarshal.GetArrayDataReference<int>(thisArray);
            if (Vector512.IsHardwareAccelerated && (uint)count >= Vector512<int>.Count)
            {
                for (; i < (uint)count - (Vector512<int>.Count - 1u); i += (uint)Vector512<int>.Count)
                {
                    Vector512<int> result = ~Vector512.LoadUnsafe(ref value, i);
                    result.StoreUnsafe(ref value, i);
                }
            }
            else if (Vector256.IsHardwareAccelerated && (uint)count >= Vector256<int>.Count)
            {
                for (; i < (uint)count - (Vector256<int>.Count - 1u); i += (uint)Vector256<int>.Count)
                {
                    Vector256<int> result = ~Vector256.LoadUnsafe(ref value, i);
                    result.StoreUnsafe(ref value, i);
                }
            }
            else if (Vector128.IsHardwareAccelerated && (uint)count >= Vector128<int>.Count)
            {
                for (; i < (uint)count - (Vector128<int>.Count - 1u); i += (uint)Vector128<int>.Count)
                {
                    Vector128<int> result = ~Vector128.LoadUnsafe(ref value, i);
                    result.StoreUnsafe(ref value, i);
                }
            }

            for (; i < (uint)count; i++)
                thisArray[i] = ~thisArray[i];

        Done:
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
                ArgumentOutOfRangeException.ThrowIfNegative(count);

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
                ArgumentOutOfRangeException.ThrowIfNegative(count);

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
                ArgumentOutOfRangeException.ThrowIfNegative(value);

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

        public unsafe void CopyTo(Array array, int index)
        {
            ArgumentNullException.ThrowIfNull(array);

            ArgumentOutOfRangeException.ThrowIfNegative(index);

            if (array.Rank != 1)
                throw new ArgumentException(SR.Arg_RankMultiDimNotSupported, nameof(array));

            if (array is int[] intArray)
            {
                if (array.Length - index < GetInt32ArrayLengthFromBitLength(m_length))
                {
                    throw new ArgumentException(SR.Argument_InvalidOffLen);
                }

                int quotient = Div32Rem(m_length, out int extraBits);

                Array.Copy(m_array, 0, intArray, index, quotient);

                if (extraBits > 0)
                {
                    // the last int needs to be masked
                    intArray[index + quotient] = m_array[quotient] & unchecked((1 << extraBits) - 1);
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
                    span[remainder] = (byte)((m_array[quotient] >> (remainder * 8)) & ((1 << (int)extraBits) - 1));
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

                uint i = 0;

                if (m_length < BitsPerInt32)
                    goto LessThan32;

                // The mask used when shuffling a single int into Vector128/256/512.
                // On little endian machines, the lower 8 bits of int belong in the first byte, next lower 8 in the second and so on.
                // We place the bytes that contain the bits to its respective byte so that we can mask out only the relevant bits later.
                Vector128<byte> lowerShuffleMask_CopyToBoolArray = Vector128.Create(0, 0x01010101_01010101).AsByte();
                Vector128<byte> upperShuffleMask_CopyToBoolArray = Vector128.Create(0x02020202_02020202, 0x03030303_03030303).AsByte();

                if (Avx512F.IsSupported && (uint)m_length >= Vector512<byte>.Count)
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
                    //Internal.Console.WriteLine(bitMask);
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
                else if (Ssse3.IsSupported && ((uint)m_length >= Vector512<byte>.Count * 2u))
                {
                    Vector128<byte> lowerShuffleMask = lowerShuffleMask_CopyToBoolArray;
                    Vector128<byte> upperShuffleMask = upperShuffleMask_CopyToBoolArray;
                    Vector128<byte> ones = Vector128.Create((byte)1);
                    Vector128<byte> bitMask128 = BitConverter.IsLittleEndian ?
                                                 Vector128.Create(0x80402010_08040201).AsByte() :
                                                 Vector128.Create(0x01020408_10204080).AsByte();

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
                    Vector128<byte> bitMask128 = BitConverter.IsLittleEndian ?
                                                 Vector128.Create(0x80402010_08040201).AsByte() :
                                                 Vector128.Create(0x01020408_10204080).AsByte();

                    fixed (bool* destination = &boolArray[index])
                    {
                        for (; (i + Vector128<byte>.Count * 2u) <= (uint)m_length; i += (uint)Vector128<byte>.Count * 2u)
                        {
                            int bits = m_array[i / (uint)BitsPerInt32];
                            // Same logic as SSSE3 path, except we do not have Shuffle instruction.
                            // (TableVectorLookup could be an alternative - dotnet/runtime#1277)
                            // Instead we use chained ZIP1/2 instructions:
                            // (A0 is the byte containing LSB, A3 is the byte containing MSB)
                            // bits (on Big endian)                 - A3 A2 A1 A0
                            // bits (Little endian) / Byte reversal - A0 A1 A2 A3
                            // v1 = Vector128.Create                - A0 A1 A2 A3 A0 A1 A2 A3 A0 A1 A2 A3 A0 A1 A2 A3
                            // v2 = ZipLow(v1, v1)                  - A0 A0 A1 A1 A2 A2 A3 A3 A0 A0 A1 A1 A2 A2 A3 A3
                            // v3 = ZipLow(v2, v2)                  - A0 A0 A0 A0 A1 A1 A1 A1 A2 A2 A2 A2 A3 A3 A3 A3
                            // shuffledLower = ZipLow(v3, v3)       - A0 A0 A0 A0 A0 A0 A0 A0 A1 A1 A1 A1 A1 A1 A1 A1
                            // shuffledHigher = ZipHigh(v3, v3)     - A2 A2 A2 A2 A2 A2 A2 A2 A3 A3 A3 A3 A3 A3 A3 A3
                            if (!BitConverter.IsLittleEndian)
                            {
                                bits = BinaryPrimitives.ReverseEndianness(bits);
                            }
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
                    int elementIndex = Div32Rem((int)i, out int extraBits);
                    boolArray[(uint)index + i] = ((m_array[elementIndex] >> extraBits) & 0x00000001) != 0;
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
            Div32Rem(m_length, out int extraBits);
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

            int mask = (1 << extraBits) - 1;
            return (m_array[intCount] & mask) == mask;
        }

        /// <summary>
        /// Determines whether any bit in the <see cref="BitArray"/> is set to <c>true</c>.
        /// </summary>
        /// <returns><c>true</c> if <see cref="BitArray"/> is not empty and at least one of its bit is set to <c>true</c>; otherwise, <c>false</c>.</returns>
        public bool HasAnySet()
        {
            Div32Rem(m_length, out int extraBits);
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

            return (m_array[intCount] & (1 << extraBits) - 1) != 0;
        }

        public int Count => m_length;

        public object SyncRoot => this;

        public bool IsSynchronized => false;

        public bool IsReadOnly => false;

        public object Clone() => new BitArray(this);

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
            throw new ArgumentOutOfRangeException(nameof(index), index, SR.ArgumentOutOfRange_IndexMustBeLess);
        }

        private sealed class BitArrayEnumeratorSimple : IEnumerator, ICloneable
        {
            private readonly BitArray _bitArray;
            private int _index;
            private readonly int _version;
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
                else
                {
                    _index = _bitArray.m_length;
                }

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
