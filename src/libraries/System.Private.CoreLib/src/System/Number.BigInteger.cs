// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    internal static partial class Number
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal unsafe ref struct BigInteger
        {
            private const int MaxBlockCount = 35;

            private static readonly uint[] s_Pow10UInt32Table = new uint[]
            {
                1,          // 10^0
                10,         // 10^1
                100,        // 10^2
                1000,       // 10^3
                10000,      // 10^4
                100000,     // 10^5
                1000000,    // 10^6
                10000000,   // 10^7
            };

            private static readonly int[] s_s_Pow10BigNumTableIndices = new int[]
            {
                0,          // 10^8
                2,          // 10^16
                5,          // 10^32
                10,         // 10^64
                18,         // 10^128
                33,         // 10^256
            };

            private static readonly uint[] s_Pow10BigNumTable = new uint[]
            {
                // 10^8
                1,          // _length
                100000000,  // _blocks

                // 10^16
                2,          // _length
                0x6FC10000, // _blocks
                0x002386F2,

                // 10^32
                4,          // _length
                0x00000000, // _blocks
                0x85ACEF81,
                0x2D6D415B,
                0x000004EE,

                // 10^64
                7,          // _length
                0x00000000, // _blocks
                0x00000000,
                0xBF6A1F01,
                0x6E38ED64,
                0xDAA797ED,
                0xE93FF9F4,
                0x00184F03,

                // 10^128
                14,         // _length
                0x00000000, // _blocks
                0x00000000,
                0x00000000,
                0x00000000,
                0x2E953E01,
                0x03DF9909,
                0x0F1538FD,
                0x2374E42F,
                0xD3CFF5EC,
                0xC404DC08,
                0xBCCDB0DA,
                0xA6337F19,
                0xE91F2603,
                0x0000024E,

                // 10^256
                27,         // _length
                0x00000000, // _blocks
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x982E7C01,
                0xBED3875B,
                0xD8D99F72,
                0x12152F87,
                0x6BDE50C6,
                0xCF4A6E70,
                0xD595D80F,
                0x26B2716E,
                0xADC666B0,
                0x1D153624,
                0x3C42D35A,
                0x63FF540E,
                0xCC5573C0,
                0x65F9EF17,
                0x55BC28F2,
                0x80DCC7F7,
                0xF46EEDDC,
                0x5FDCEFCE,
                0x000553F7,

                // Trailing blocks to ensure MaxBlockCount
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
            };

            private static readonly uint[] s_MultiplyDeBruijnBitPosition = new uint[]
            {
                0, 9, 1, 10, 13, 21, 2, 29, 11, 14, 16, 18, 22, 25, 3, 30,
                8, 12, 20, 28, 15, 17, 24, 7, 19, 27, 23, 6, 26, 5, 4, 31
            };

            private int _length;
            private BlocksBuffer _blocks;

            public BigInteger(uint value)
            {
                _blocks[0] = value;
                _length = (value == 0) ? 0 : 1;
            }

            public BigInteger(ulong value)
            {
                var lower = (uint)(value);
                var upper = (uint)(value >> 32);

                _blocks[0] = lower;
                _blocks[1] = upper;

                _length = (upper == 0) ? 1 : 2;
            }

            public static uint BitScanReverse(uint mask)
            {
                // This comes from the Stanford Bit Widdling Hacks by Sean Eron Anderson:
                // http://graphics.stanford.edu/~seander/bithacks.html#IntegerLogDeBruijn

                mask |= (mask >> 1); // first round down to one less than a power of 2 
                mask |= (mask >> 2);
                mask |= (mask >> 4);
                mask |= (mask >> 8);
                mask |= (mask >> 16);

                uint index = (mask * 0x07C4ACDD) >> 27;
                return s_MultiplyDeBruijnBitPosition[(int)(index)];
            }

            public static int Compare(ref BigInteger lhs, ref BigInteger rhs)
            {
                Debug.Assert(unchecked((uint)(lhs._length)) <= MaxBlockCount);
                Debug.Assert(unchecked((uint)(rhs._length)) <= MaxBlockCount);

                int lhsLength = lhs._length;
                int rhsLength = rhs._length;

                int lengthDelta = (lhsLength - rhsLength);

                if (lengthDelta != 0)
                {
                    return lengthDelta;
                }

                if (lhsLength == 0)
                {
                    Debug.Assert(rhsLength == 0);
                    return 0;
                }

                for (int index = (lhsLength - 1); index >= 0; index--)
                {
                    long delta = (long)(lhs._blocks[index]) - rhs._blocks[index];

                    if (delta != 0)
                    {
                        return delta > 0 ? 1 : -1;
                    }
                }

                return 0;
            }

            public static uint HeuristicDivide(ref BigInteger dividend, ref BigInteger divisor)
            {
                int divisorLength = divisor._length;

                if (dividend._length < divisorLength)
                {
                    return 0;
                }

                // This is an estimated quotient. Its error should be less than 2.
                // Reference inequality:
                // a/b - floor(floor(a)/(floor(b) + 1)) < 2
                int lastIndex = (divisorLength - 1);
                uint quotient = dividend._blocks[lastIndex] / (divisor._blocks[lastIndex] + 1);

                if (quotient != 0)
                {
                    // Now we use our estimated quotient to update each block of dividend.
                    // dividend = dividend - divisor * quotient
                    int index = 0;

                    ulong borrow = 0;
                    ulong carry = 0;

                    do
                    {
                        ulong product = ((ulong)(divisor._blocks[index]) * quotient) + carry;
                        carry = product >> 32;

                        ulong difference = (ulong)(dividend._blocks[index]) - (uint)(product) - borrow;
                        borrow = (difference >> 32) & 1;

                        dividend._blocks[index] = (uint)(difference);

                        index++;
                    }
                    while (index < divisorLength);

                    // Remove all leading zero blocks from dividend
                    while ((divisorLength > 0) && (dividend._blocks[divisorLength - 1] == 0))
                    {
                        divisorLength--;
                    }

                    dividend._length = divisorLength;
                }

                // If the dividend is still larger than the divisor, we overshot our estimate quotient. To correct,
                // we increment the quotient and subtract one more divisor from the dividend (Because we guaranteed the error range).
                if (Compare(ref dividend, ref divisor) >= 0)
                {
                    quotient++;

                    // dividend = dividend - divisor
                    int index = 0;
                    ulong borrow = 0;

                    do
                    {
                        ulong difference = (ulong)(dividend._blocks[index]) - divisor._blocks[index] - borrow;
                        borrow = (difference >> 32) & 1;

                        dividend._blocks[index] = (uint)(difference);

                        index++;
                    }
                    while (index < divisorLength);

                    // Remove all leading zero blocks from dividend
                    while ((divisorLength > 0) && (dividend._blocks[divisorLength - 1] == 0))
                    {
                        divisorLength--;
                    }

                    dividend._length = divisorLength;
                }

                return quotient;
            }

            public static uint LogBase2(uint value)
            {
                Debug.Assert(value != 0);
                return BitScanReverse(value);
            }

            public static uint LogBase2(ulong value)
            {
                Debug.Assert(value != 0);

                uint upper = (uint)(value >> 32);

                if (upper != 0)
                {
                    return 32 + LogBase2(upper);
                }

                return LogBase2((uint)(value));
            }

            public static void Multiply(ref BigInteger lhs, uint value, ref BigInteger result)
            {
                if (lhs.IsZero() || (value == 1))
                {
                    result.SetValue(ref lhs);
                    return;
                }

                if (value == 0)
                {
                    result.SetZero();
                    return;
                }

                int lhsLength = lhs._length;
                int index = 0;

                ulong carry = 0;

                while (index < lhsLength)
                {
                    ulong product = ((ulong)(lhs._blocks[index]) * value) + carry;
                    carry = product >> 32;
                    result._blocks[index] = (uint)(product);

                    index++;
                }

                if (carry != 0)
                {
                    Debug.Assert(unchecked((uint)(lhsLength)) + 1 <= MaxBlockCount);
                    result._blocks[index] = (uint)(carry);
                    result._length += (lhsLength + 1);
                }
            }

            public static void Multiply(ref BigInteger lhs, ref BigInteger rhs, ref BigInteger result)
            {
                if (lhs.IsZero() || rhs.IsOne())
                {
                    result.SetValue(ref lhs);
                    return;
                }

                if (rhs.IsZero())
                {
                    result.SetZero();
                    return;
                }

                ref readonly BigInteger large = ref lhs;
                int largeLength = lhs._length;

                ref readonly BigInteger small = ref rhs;
                int smallLength = rhs._length;

                if (largeLength < smallLength)
                {
                    large = ref rhs;
                    largeLength = rhs._length;

                    small = ref lhs;
                    smallLength = lhs._length;
                }

                int maxResultLength = smallLength + largeLength;
                Debug.Assert(unchecked((uint)(maxResultLength)) <= MaxBlockCount);

                // Zero out result internal blocks.
                Buffer.ZeroMemory((byte*)(result._blocks.GetPointer()), (maxResultLength * sizeof(uint)));

                int smallIndex = 0;
                int resultStartIndex = 0;

                while (smallIndex < smallLength)
                {
                    // Multiply each block of large BigNum.
                    if (small._blocks[smallIndex] != 0)
                    {
                        int largeIndex = 0;
                        int resultIndex = resultStartIndex;

                        ulong carry = 0;

                        do
                        {
                            ulong product = result._blocks[resultIndex] + ((ulong)(small._blocks[smallIndex]) * large._blocks[largeIndex]) + carry;
                            carry = product >> 32;
                            result._blocks[resultIndex] = (uint)(product);

                            resultIndex++;
                            largeIndex++;
                        }
                        while (largeIndex < largeLength);

                        result._blocks[resultIndex] = (uint)(carry);
                    }

                    smallIndex++;
                    resultStartIndex++;
                }

                if ((maxResultLength > 0) && (result._blocks[maxResultLength - 1] == 0))
                {
                    result._length = (maxResultLength - 1);
                }
                else
                {
                    result._length = maxResultLength;
                }
            }

            public static void Pow10(uint exponent, ref BigInteger result)
            {
                // We leverage two arrays - s_Pow10UInt32Table and s_Pow10BigNumTable to speed up the Pow10 calculation.
                //
                // s_Pow10UInt32Table stores the results of 10^0 to 10^7.
                // s_Pow10BigNumTable stores the results of 10^8, 10^16, 10^32, 10^64, 10^128 and 10^256.
                //
                // For example, let's say exp = 0b111111. We can split the exp to two parts, one is small exp, 
                // which 10^smallExp can be represented as uint, another part is 10^bigExp, which must be represented as BigNum. 
                // So the result should be 10^smallExp * 10^bigExp.
                //
                // Calculating 10^smallExp is simple, we just lookup the 10^smallExp from s_Pow10UInt32Table. 
                // But here's a bad news: although uint can represent 10^9, exp 9's binary representation is 1001. 
                // That means 10^(1011), 10^(1101), 10^(1111) all cannot be stored as uint, we cannot easily say something like: 
                // "Any bits <= 3 is small exp, any bits > 3 is big exp". So instead of involving 10^8, 10^9 to s_Pow10UInt32Table, 
                // consider 10^8 and 10^9 as a bigNum, so they fall into s_Pow10BigNumTable. Now we can have a simple rule: 
                // "Any bits <= 3 is small exp, any bits > 3 is big exp".
                //
                // For 0b111111, we first calculate 10^(smallExp), which is 10^(7), now we can shift right 3 bits, prepare to calculate the bigExp part, 
                // the exp now becomes 0b000111.
                //
                // Apparently the lowest bit of bigExp should represent 10^8 because we have already shifted 3 bits for smallExp, so s_Pow10BigNumTable[0] = 10^8.
                // Now let's shift exp right 1 bit, the lowest bit should represent 10^(8 * 2) = 10^16, and so on...
                //
                // That's why we just need the values of s_Pow10BigNumTable be power of 2.
                //
                // More details of this implementation can be found at: https://github.com/dotnet/coreclr/pull/12894#discussion_r128890596

                BigInteger temp1 = new BigInteger(s_Pow10UInt32Table[exponent & 0x7]);
                ref BigInteger lhs = ref temp1;

                BigInteger temp2 = new BigInteger(0);
                ref BigInteger product = ref temp2;

                exponent >>= 3;
                uint index = 0;

                while (exponent != 0)
                {
                    // If the current bit is set, multiply it with the corresponding power of 10
                    if ((exponent & 1) != 0)
                    {
                        // Multiply into the next temporary
                        ref BigInteger rhs = ref *(BigInteger*)(Unsafe.AsPointer(ref s_Pow10BigNumTable[s_s_Pow10BigNumTableIndices[index]]));
                        Multiply(ref lhs, ref rhs, ref product);

                        // Swap to the next temporary
                        ref BigInteger temp = ref product;
                        product = ref lhs;
                        lhs = ref temp;
                    }

                    // Advance to the next bit
                    ++index;
                    exponent >>= 1;
                }

                result.SetValue(ref lhs);
            }

            public static void PrepareHeuristicDivide(ref BigInteger dividend, ref BigInteger divisor)
            {
                uint hiBlock = divisor._blocks[divisor._length - 1];

                if ((hiBlock < 8) || (hiBlock > 429496729))
                {
                    // Inspired by http://www.ryanjuckett.com/programming/printing-floating-point-numbers/
                    // Perform a bit shift on all values to get the highest block of the divisor into
                    // the range [8,429496729]. We are more likely to make accurate quotient estimations
                    // in heuristicDivide() with higher divisor values so
                    // we shift the divisor to place the highest bit at index 27 of the highest block.
                    // This is safe because (2^28 - 1) = 268435455 which is less than 429496729. This means
                    // that all values with a highest bit at index 27 are within range.
                    uint hiBlockLog2 = LogBase2(hiBlock);
                    uint shift = (59 - hiBlockLog2) % 32;

                    divisor.ShiftLeft(shift);
                    dividend.ShiftLeft(shift);
                }
            }

            public static void ShiftLeft(ulong input, uint shift, ref BigInteger output)
            {
                if (shift == 0)
                {
                    return;
                }

                uint blocksToShift = Math.DivRem(shift, 32, out uint remainingBitsToShift);

                if (blocksToShift > 0)
                {
                    // If blocks shifted, we should fill the corresponding blocks with zero.
                    output.ExtendBlocks(0, blocksToShift);
                }

                if (remainingBitsToShift == 0)
                {
                    // We shift 32 * n (n >= 1) bits. No remaining bits.
                    output.ExtendBlock((uint)(input));

                    uint highBits = (uint)(input >> 32);

                    if (highBits != 0)
                    {
                        output.ExtendBlock(highBits);
                    }
                }
                else
                {
                    // Extract the high position bits which would be shifted out of range.
                    uint highPositionBits = (uint)(input) >> (int)(64 - remainingBitsToShift);

                    // Shift the input. The result should be stored to current block.
                    ulong shiftedInput = input << (int)(remainingBitsToShift);
                    output.ExtendBlock((uint)(shiftedInput));

                    uint highBits = (uint)(input >> 32);

                    if (highBits != 0)
                    {
                        output.ExtendBlock(highBits);
                    }

                    if (highPositionBits != 0)
                    {
                        // If the high position bits is not 0, we should store them to next block.
                        output.ExtendBlock(highPositionBits);
                    }
                }
            }

            public void ExtendBlock(uint blockValue)
            {
                _blocks[_length] = blockValue;
                _length++;
            }

            public void ExtendBlocks(uint blockValue, uint blockCount)
            {
                Debug.Assert(blockCount > 0);

                if (blockCount == 1)
                {
                    ExtendBlock(blockValue);

                    return;
                }

                Buffer.ZeroMemory((byte*)(_blocks.GetPointer() + _length), ((blockCount - 1) * sizeof(uint)));
                _length += (int)(blockCount);
                _blocks[_length - 1] = blockValue;
            }

            public bool IsOne()
            {
                return (_length == 1)
                    && (_blocks[0] == 1);
            }

            public bool IsZero()
            {
                return _length == 0;
            }

            public void Multiply(uint value)
            {
                Multiply(ref this, value, ref this);
            }

            public void Multiply(ref BigInteger value)
            {
                var result = new BigInteger(0);
                Multiply(ref this, ref value, ref result);

                Buffer.Memcpy((byte*)(_blocks.GetPointer()), (byte*)(result._blocks.GetPointer()), (result._length) * sizeof(uint));
                _length = result._length;
            }

            public void Multiply10()
            {
                if (IsZero())
                {
                    return;
                }

                int index = 0;
                int length = _length;
                ulong carry = 0;

                while (index < length)
                {
                    var block = (ulong)(_blocks[index]);
                    ulong product = (block << 3) + (block << 1) + carry;
                    carry = product >> 32;
                    _blocks[index] = (uint)(product);

                    index++;
                }

                if (carry != 0)
                {
                    Debug.Assert(unchecked((uint)(_length)) + 1 <= MaxBlockCount);
                    _blocks[index] = (uint)(carry);
                    _length += 1;
                }
            }

            public void SetUInt32(uint value)
            {
                _blocks[0] = value;
                _length = 1;
            }

            public void SetUInt64(ulong value)
            {
                var lower = (uint)(value);
                var upper = (uint)(value >> 32);

                _blocks[0] = lower;
                _blocks[1] = upper;

                _length = (upper == 0) ? 1 : 2;
            }

            public void SetValue(ref BigInteger rhs)
            {
                int rhsLength = rhs._length;
                Buffer.Memcpy((byte*)(_blocks.GetPointer()), (byte*)(rhs._blocks.GetPointer()), (rhsLength * sizeof(uint)));
                _length = rhsLength;
            }

            public void SetZero()
            {
                _length = 0;
            }

            public void ShiftLeft(uint shift)
            {
                // Process blocks high to low so that we can safely process in place
                var length = _length;

                if ((length == 0) || (shift == 0))
                {
                    return;
                }

                uint blocksToShift = Math.DivRem(shift, 32, out uint remainingBitsToShift);

                // Copy blocks from high to low
                int readIndex = (length - 1);
                int writeIndex = readIndex + (int)(blocksToShift);

                // Check if the shift is block aligned
                if (remainingBitsToShift == 0)
                {
                    Debug.Assert(writeIndex < MaxBlockCount);

                    while (readIndex >= 0)
                    {
                        _blocks[writeIndex] = _blocks[readIndex];
                        readIndex--;
                        writeIndex--;
                    }

                    _length += (int)(blocksToShift);

                    // Zero the remaining low blocks
                    Buffer.ZeroMemory((byte*)(_blocks.GetPointer()), (blocksToShift * sizeof(uint)));
                }
                else
                {
                    // We need an extra block for the partial shift
                    writeIndex++;
                    Debug.Assert(writeIndex < MaxBlockCount);

                    // Set the length to hold the shifted blocks
                    _length = writeIndex + 1;

                    // Output the initial blocks
                    uint lowBitsShift = (32 - remainingBitsToShift);
                    uint highBits = 0;
                    uint block = _blocks[readIndex];
                    uint lowBits = block >> (int)(lowBitsShift);
                    while (readIndex > 0)
                    {
                        _blocks[writeIndex] = highBits | lowBits;
                        highBits = block << (int)(remainingBitsToShift);

                        --readIndex;
                        --writeIndex;

                        block = _blocks[readIndex];
                        lowBits = block >> (int)lowBitsShift;
                    }

                    // Output the final blocks
                    _blocks[writeIndex] = highBits | lowBits;
                    _blocks[writeIndex - 1] = block << (int)(remainingBitsToShift);

                    // Zero the remaining low blocks
                    Buffer.ZeroMemory((byte*)(_blocks.GetPointer()), (blocksToShift * sizeof(uint)));

                    // Check if the terminating block has no set bits
                    if (_blocks[_length - 1] == 0)
                    {
                        _length--;
                    }
                }
            }

            [StructLayout(LayoutKind.Sequential, Size = (MaxBlockCount * sizeof(uint)))]
            private struct BlocksBuffer
            {
                public ref uint this[int index]
                {
                    get
                    {
                        Debug.Assert(unchecked((uint)(index)) <= MaxBlockCount);
                        return ref Unsafe.Add(ref GetPinnableReference(), index);
                    }
                }

                public ref uint GetPinnableReference()
                {
                    var pThis = Unsafe.AsPointer(ref this);
                    return ref Unsafe.AsRef<uint>(pThis);
                }

                public uint* GetPointer()
                {
                    return (uint*)(Unsafe.AsPointer(ref this));
                }
            }
        }
    }
}
