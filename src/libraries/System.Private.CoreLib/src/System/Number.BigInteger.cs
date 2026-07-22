// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class Number
    {
        [StructLayout(LayoutKind.Sequential)]
        internal ref struct BigInteger
        {
            // The longest binary mantissa requires: explicit mantissa bits + abs(min exponent)
            // * Half:     10 +    14 =    24
            // * Single:   23 +   126 =   149
            // * Double:   52 +  1022 =  1074
            // * Quad:    112 + 16382 = 16494
            private const int BitsForLongestBinaryMantissa = 1074;

            // The longest digit sequence requires: ceil(log2(pow(10, max significant digits + 1 rounding digit)))
            // * Half:    ceil(log2(pow(10,    21 + 1))) =    74
            // * Single:  ceil(log2(pow(10,   112 + 1))) =   376
            // * Double:  ceil(log2(pow(10,   767 + 1))) =  2552
            // * Quad:    ceil(log2(pow(10, 11563 + 1))) = 38415
            private const int BitsForLongestDigitSequence = 2552;

            // We require BitsPerBlock additional bits for shift space used during the pre-division preparation
            private const int MaxBits = BitsForLongestBinaryMantissa + BitsForLongestDigitSequence + BitsPerBlock;

            // Blocks are the native word width so the algorithms scale with the machine.
#if TARGET_64BIT
            internal const int BitsPerBlock = 64;
            private const int BlockShift = 6;
#else
            internal const int BitsPerBlock = 32;
            private const int BlockShift = 5;
#endif

            // We need one extra block to make our shift left algorithm significantly simpler
            private const int MaxBlockCount = ((MaxBits + (BitsPerBlock - 1)) / BitsPerBlock) + 1;

            // Single-block powers of 10, shared with the primitive types. On 64-bit a block holds
            // 10^0..10^19; on 32-bit, 10^0..10^9. MultiplyPow10 folds any exponent that fits a
            // single block through here, deferring only larger exponents to Pow10BigNumTable.
#if TARGET_64BIT
            private static ReadOnlySpan<ulong> Pow10Table => ulong.PowersOf10;
#else
            private static ReadOnlySpan<uint> Pow10Table => uint.PowersOf10;
#endif

            // Pow10 splits the exponent into a low part applied via a single Pow10Table lookup+multiply
            // and a high part accumulated from Pow10BigNumTable. The low part covers 10^0..10^(2^N - 1),
            // the largest power-of-two range whose values each fit in a single block: N == BlockShift - 2
            // (3 on 32-bit -> Pow10BigNumTable starts at 10^8; 4 on 64-bit -> it starts at 10^16).
            private const int Pow10SmallExpBits = BlockShift - 2;
            private const uint Pow10SmallExpMask = (1u << Pow10SmallExpBits) - 1;

            // Pow10BigNumTable stores each power as a valid BigInteger memory image: a `_length` (in
            // blocks) immediately followed by that many packed blocks. Each bitness uses a blob whose
            // element type matches the native word -- ulong on 64-bit, uint on 32-bit -- so both `_length`
            // (a nint) and each block occupy exactly one element. That keeps the blocks naturally aligned
            // and, since CreateSpan byte-swaps per element, endian-correct when reinterpreted as a
            // BigInteger. The 64-bit variant is generated + verified by src\libraries tooling in history.
#if TARGET_64BIT
            private static ReadOnlySpan<int> Pow10BigNumTableIndices =>
            [
                0,          // 10^16
                2,          // 10^32
                5,          // 10^64
                10,         // 10^128
                18,         // 10^256
                33,         // 10^512
                61,         // 10^1024
            ];

            private static ReadOnlySpan<ulong> Pow10BigNumTable =>
            [
                // 10^16
                1,                      // _length
                0x002386F26FC10000,     // _blocks

                // 10^32
                2,                      // _length
                0x85ACEF8100000000,     // _blocks
                0x000004EE2D6D415B,

                // 10^64
                4,                      // _length
                0x0000000000000000,     // _blocks
                0x6E38ED64BF6A1F01,
                0xE93FF9F4DAA797ED,
                0x0000000000184F03,

                // 10^128
                7,                      // _length
                0x0000000000000000,     // _blocks
                0x0000000000000000,
                0x03DF99092E953E01,
                0x2374E42F0F1538FD,
                0xC404DC08D3CFF5EC,
                0xA6337F19BCCDB0DA,
                0x0000024EE91F2603,

                // 10^256
                14,                     // _length
                0x0000000000000000,     // _blocks
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0xBED3875B982E7C01,
                0x12152F87D8D99F72,
                0xCF4A6E706BDE50C6,
                0x26B2716ED595D80F,
                0x1D153624ADC666B0,
                0x63FF540E3C42D35A,
                0x65F9EF17CC5573C0,
                0x80DCC7F755BC28F2,
                0x5FDCEFCEF46EEDDC,
                0x00000000000553F7,

                // 10^512
                27,                     // _length
                0x0000000000000000,     // _blocks
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x77F27267FC6CF801,
                0x5D96976F8F9546DC,
                0xC31E1AD9B83A8A97,
                0x94E6574746C40513,
                0x4475B579C88976C1,
                0xAA1DA1BF28F8733B,
                0x1E25CFEA703ED321,
                0xBC51FB2EB21A2F22,
                0xBFA3EDAC96E14F5D,
                0xE7FC7153329C57AE,
                0x85A91924C3FC0695,
                0xB2908EE0F95F635E,
                0x1366732A93ABADE4,
                0x69BE5B0E9449775C,
                0xB099BC817343AFAC,
                0xA269974845A71D46,
                0x8A0B1F138CB07303,
                0xC1D238D98CAB8A97,
                0x0000001C633415D4,

                // 10^1024
                54,                     // _length
                0x0000000000000000,     // _blocks
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0xF55B2B722919F001,
                0x1EC29F866E7C215B,
                0x15C51A88991C4E87,
                0x4C7D1E1A140AC535,
                0x0ED1440ECC2CD819,
                0x7DE16CFB896634EE,
                0x9FCE837D1E43F61F,
                0x233E55C7231D2B9C,
                0xF451218B65DC60D7,
                0xC96359861C5CD134,
                0xA7E89431922BBB9F,
                0x62BE695A9F9F2A07,
                0x045B7A748E1042C4,
                0x8AD822A51ABE1DE3,
                0xD814B505BA34C411,
                0x8FC51A16BF3FDEB3,
                0xF56DEEECB1B896BC,
                0xB6F4654B31FB6BFD,
                0x6B7595FB101A3616,
                0x80D98089DC1A47FE,
                0x9A20288280BDA5A5,
                0xFC8F1F9031EB0F66,
                0xE26A7B7E976A3310,
                0x3CE3A0B8DF68368A,
                0x75A351A28E4262CE,
                0x445975836CB0B6C9,
                0xC356E38A31B5653F,
                0x0190FBA035FAABA6,
                0x88BC491B9FC4ED52,
                0x005B80411640114A,
                0x1E8D4649F4F3235E,
                0x73C5534936A8DE06,
                0xC1A6970CA7E6BD2A,
                0xD2DB49EF47187094,
                0xAE6209D4926C3F5B,
                0x34F4A3C62D433949,
                0xD9D61A05D4305D94,
                0x0000000000000325,

                // 5 trailing zero blocks so the last entry can be reinterpreted as a full BigInteger
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
                0x0000000000000000,
            ];
#else
            private static ReadOnlySpan<int> Pow10BigNumTableIndices =>
            [
                0,          // 10^8
                2,          // 10^16
                5,          // 10^32
                10,         // 10^64
                18,         // 10^128
                33,         // 10^256
                61,         // 10^512
                116,        // 10^1024
            ];

            private static ReadOnlySpan<uint> Pow10BigNumTable =>
            [
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

                // 10^512
                54,         // _length
                0x00000000, // _blocks
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0xFC6CF801,
                0x77F27267,
                0x8F9546DC,
                0x5D96976F,
                0xB83A8A97,
                0xC31E1AD9,
                0x46C40513,
                0x94E65747,
                0xC88976C1,
                0x4475B579,
                0x28F8733B,
                0xAA1DA1BF,
                0x703ED321,
                0x1E25CFEA,
                0xB21A2F22,
                0xBC51FB2E,
                0x96E14F5D,
                0xBFA3EDAC,
                0x329C57AE,
                0xE7FC7153,
                0xC3FC0695,
                0x85A91924,
                0xF95F635E,
                0xB2908EE0,
                0x93ABADE4,
                0x1366732A,
                0x9449775C,
                0x69BE5B0E,
                0x7343AFAC,
                0xB099BC81,
                0x45A71D46,
                0xA2699748,
                0x8CB07303,
                0x8A0B1F13,
                0x8CAB8A97,
                0xC1D238D9,
                0x633415D4,
                0x0000001C,

                // 10^1024
                107,        // _length
                0x00000000, // _blocks
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x2919F001,
                0xF55B2B72,
                0x6E7C215B,
                0x1EC29F86,
                0x991C4E87,
                0x15C51A88,
                0x140AC535,
                0x4C7D1E1A,
                0xCC2CD819,
                0x0ED1440E,
                0x896634EE,
                0x7DE16CFB,
                0x1E43F61F,
                0x9FCE837D,
                0x231D2B9C,
                0x233E55C7,
                0x65DC60D7,
                0xF451218B,
                0x1C5CD134,
                0xC9635986,
                0x922BBB9F,
                0xA7E89431,
                0x9F9F2A07,
                0x62BE695A,
                0x8E1042C4,
                0x045B7A74,
                0x1ABE1DE3,
                0x8AD822A5,
                0xBA34C411,
                0xD814B505,
                0xBF3FDEB3,
                0x8FC51A16,
                0xB1B896BC,
                0xF56DEEEC,
                0x31FB6BFD,
                0xB6F4654B,
                0x101A3616,
                0x6B7595FB,
                0xDC1A47FE,
                0x80D98089,
                0x80BDA5A5,
                0x9A202882,
                0x31EB0F66,
                0xFC8F1F90,
                0x976A3310,
                0xE26A7B7E,
                0xDF68368A,
                0x3CE3A0B8,
                0x8E4262CE,
                0x75A351A2,
                0x6CB0B6C9,
                0x44597583,
                0x31B5653F,
                0xC356E38A,
                0x35FAABA6,
                0x0190FBA0,
                0x9FC4ED52,
                0x88BC491B,
                0x1640114A,
                0x005B8041,
                0xF4F3235E,
                0x1E8D4649,
                0x36A8DE06,
                0x73C55349,
                0xA7E6BD2A,
                0xC1A6970C,
                0x47187094,
                0xD2DB49EF,
                0x926C3F5B,
                0xAE6209D4,
                0x2D433949,
                0x34F4A3C6,
                0xD4305D94,
                0xD9D61A05,
                0x00000325,

                // 10 Trailing blocks to ensure MaxBlockCount
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
                0x00000000,
            ];
#endif

            private nint _length;
            private BlocksBuffer _blocks;

            public static void Add(scoped ref BigInteger lhs, scoped ref BigInteger rhs, out BigInteger result)
            {
                Unsafe.SkipInit(out result);

                // Order operands so the longer one drives the loop.
                ref BigInteger large = ref (lhs._length < rhs._length ? ref rhs : ref lhs);
                ref BigInteger small = ref (lhs._length < rhs._length ? ref lhs : ref rhs);

                int largeLength = (int)large._length;
                int smallLength = (int)small._length;

                if (smallLength == 0)
                {
                    SetValue(out result, ref large);
                    return;
                }

                Debug.Assert(unchecked((uint)largeLength) < MaxBlockCount);

                if (unchecked((uint)largeLength) >= MaxBlockCount)
                {
                    // We shouldn't reach here, and the above assert will help flag this
                    // during testing, but we'll ensure that we return a safe value of
                    // zero in the case we end up overflowing in any way.

                    SetZero(out result);
                    return;
                }

                // The shared kernel writes largeLength + 1 blocks; the extra holds the carry-out.
                Span<nuint> resultBlocks = result._blocks;
                BigIntegerCalculator.Add(large._blocks[..largeLength], small._blocks[..smallLength], resultBlocks[..(largeLength + 1)]);

                result._length = (resultBlocks[largeLength] != 0) ? (largeLength + 1) : largeLength;
            }

            public static int Compare(scoped ref BigInteger lhs, scoped ref BigInteger rhs)
            {
                Debug.Assert(unchecked((uint)lhs._length) <= MaxBlockCount);
                Debug.Assert(unchecked((uint)rhs._length) <= MaxBlockCount);

                return BigIntegerCalculator.Compare(lhs._blocks[..(int)lhs._length], rhs._blocks[..(int)rhs._length]);
            }

            public static int CountSignificantBits(ulong value)
            {
                return 64 - BitOperations.LeadingZeroCount(value);
            }

            public static int CountSignificantBits(ref BigInteger value)
            {
                if (value.IsZero())
                {
                    return 0;
                }

                // We don't track any unused blocks, so we only need to do a BSR on the
                // last index and add that to the number of bits we skipped.

                int lastIndex = (int)value._length - 1;
                return (lastIndex * BitsPerBlock) + (BitsPerBlock - BitOperations.LeadingZeroCount(value._blocks[lastIndex]));
            }

            public static void DivRem(scoped ref BigInteger lhs, scoped ref BigInteger rhs, out BigInteger quo, out BigInteger rem)
            {
                Unsafe.SkipInit(out quo);

                // This is modified from the libraries BigIntegerCalculator.DivRem.cs implementation:
                // https://github.com/dotnet/runtime/blob/main/src/libraries/System.Runtime.Numerics/src/System/Numerics/BigIntegerCalculator.DivRem.cs

                Debug.Assert(!rhs.IsZero());

                if (lhs.IsZero())
                {
                    SetZero(out quo);
                    SetZero(out rem);
                    return;
                }

                int lhsLength = (int)lhs._length;
                int rhsLength = (int)rhs._length;

                if ((lhsLength == 1) && (rhsLength == 1))
                {
                    (nuint quotient, nuint remainder) = Math.DivRem(lhs._blocks[0], rhs._blocks[0]);
                    SetBlock(out quo, quotient);
                    SetBlock(out rem, remainder);
                    return;
                }

                if (rhsLength == 1)
                {
                    // We can make the computation much simpler if the rhs is only one block

                    int quoLength = lhsLength;

                    nuint rhsValue = rhs._blocks[0];
                    nuint carry = 0;

                    for (int i = quoLength - 1; i >= 0; i--)
                    {
                        // carry is the running remainder, always < rhsValue, so the quotient fits a block.
                        nuint digit = BigIntegerCalculator.DivRem(carry, lhs._blocks[i], rhsValue, out carry);

                        if ((digit == 0) && (i == (quoLength - 1)))
                        {
                            quoLength--;
                        }
                        else
                        {
                            quo._blocks[i] = digit;
                        }
                    }

                    quo._length = quoLength;
                    SetBlock(out rem, carry);

                    return;
                }
                else if (rhsLength > lhsLength)
                {
                    // Handle the case where we have no quotient
                    SetZero(out quo);
                    SetValue(out rem, ref lhs);
                    return;
                }
                else
                {
                    int quoLength = lhsLength - rhsLength + 1;
                    SetValue(out rem, ref lhs);

                    // The shared kernel divides in place: rem holds the dividend on entry and the
                    // remainder (in its low rhsLength blocks) on exit, with quo receiving the quotient.
                    Span<nuint> quoBlocks = quo._blocks;
                    BigIntegerCalculator.DivideGrammarSchool(rem._blocks[..lhsLength], rhs._blocks[..rhsLength], quoBlocks[..quoLength]);

                    quo._length = BigIntegerCalculator.ActualLength(quoBlocks[..quoLength]);
                    rem._length = BigIntegerCalculator.ActualLength(rem._blocks[..rhsLength]);
                }
            }

            public static uint HeuristicDivide(ref BigInteger dividend, ref BigInteger divisor)
            {
                int divisorLength = (int)divisor._length;

                if (dividend._length < divisorLength)
                {
                    return 0;
                }

                // This is an estimated quotient. Its error should be less than 2.
                // Reference inequality:
                // a/b - floor(floor(a)/(floor(b) + 1)) < 2
                int lastIndex = divisorLength - 1;
                nuint quotient = dividend._blocks[lastIndex] / (divisor._blocks[lastIndex] + 1);

                if (quotient != 0)
                {
                    // dividend -= divisor * quotient. This runs once per output digit in Dragon4's hot
                    // loop, so the widening multiply-subtract is kept fully inline: routing through the
                    // shared span kernels (SubMul1) regressed ToString("G50") ~40%+ (out-of-line call +
                    // Span setup spilling the block pointers), and even an inlinable scalar helper left
                    // ~8% on the table because the UInt128 body did not inline. The `nint.Size` branch
                    // constant-folds so only the native-width loop is emitted. The estimate never
                    // overshoots enough to underflow, so the trailing borrow is discarded.
                    nuint borrow = 0;
                    if (nint.Size == 8)
                    {
                        for (int i = 0; i < divisorLength; i++)
                        {
                            UInt128 product = (UInt128)(ulong)divisor._blocks[i] * quotient + borrow;
                            nuint low = (nuint)product.Lower;
                            nuint orig = dividend._blocks[i];
                            dividend._blocks[i] = orig - low;
                            borrow = (nuint)product.Upper + ((orig < low) ? (nuint)1 : 0);
                        }
                    }
                    else
                    {
                        for (int i = 0; i < divisorLength; i++)
                        {
                            ulong product = (ulong)(uint)divisor._blocks[i] * quotient + borrow;
                            nuint low = (nuint)(uint)product;
                            nuint orig = dividend._blocks[i];
                            dividend._blocks[i] = orig - low;
                            borrow = (nuint)(uint)(product >> 32) + ((orig < low) ? (nuint)1 : 0);
                        }
                    }

                    // Remove all leading zero blocks from dividend
                    divisorLength = BigIntegerCalculator.ActualLength(dividend._blocks[..divisorLength]);
                    dividend._length = divisorLength;
                }

                // If the dividend is still larger than the divisor, we overshot our estimate quotient. To correct,
                // we increment the quotient and subtract one more divisor from the dividend (Because we guaranteed the error range).
                if (Compare(ref dividend, ref divisor) >= 0)
                {
                    quotient++;

                    // dividend -= divisor. This is the cold correction path (only on overshoot), so it
                    // reuses the shared SubWithBorrow leaf, which inlines into this local loop.
                    nuint borrow = 0;
                    for (int i = 0; i < divisorLength; i++)
                    {
                        dividend._blocks[i] = BigIntegerCalculator.SubWithBorrow(dividend._blocks[i], divisor._blocks[i], borrow, out nuint nextBorrow);
                        borrow = nextBorrow;
                    }

                    // Remove all leading zero blocks from dividend
                    divisorLength = BigIntegerCalculator.ActualLength(dividend._blocks[..divisorLength]);
                    dividend._length = divisorLength;
                }

                Debug.Assert(quotient < 10);
                return (uint)quotient;
            }

            public static void Multiply(scoped ref BigInteger lhs, nuint value, out BigInteger result)
            {
                Unsafe.SkipInit(out result);

                if (value <= 1)
                {
                    if (value == 0)
                    {
                        SetZero(out result);
                    }
                    else
                    {
                        SetValue(out result, ref lhs);
                    }
                    return;
                }

                int lhsLength = (int)lhs._length;

                // Mul1 is safe in place, so this also serves the result-aliases-lhs callers.
                Span<nuint> resultBlocks = result._blocks;
                nuint carry = BigIntegerCalculator.Mul1(resultBlocks[..lhsLength], lhs._blocks[..lhsLength], value);

                int resultLength = lhsLength;

                if (carry != 0)
                {
                    Debug.Assert(unchecked((uint)resultLength) < MaxBlockCount);

                    if (unchecked((uint)resultLength) >= MaxBlockCount)
                    {
                        // We shouldn't reach here, and the above assert will help flag this
                        // during testing, but we'll ensure that we return a safe value of
                        // zero in the case we end up overflowing in any way.

                        SetZero(out result);
                        return;
                    }

                    resultBlocks[resultLength] = carry;
                    resultLength += 1;
                }

                result._length = resultLength;
            }

            public static void Multiply(scoped ref BigInteger lhs, scoped ref readonly BigInteger rhs, out BigInteger result)
            {
                Unsafe.SkipInit(out result);

                if (lhs._length <= 1)
                {
                    Multiply(ref Unsafe.AsRef(in rhs), lhs.ToBlock(), out result);
                    return;
                }

                if (rhs._length <= 1)
                {
                    Multiply(ref lhs, rhs.ToBlock(), out result);
                    return;
                }

                ref readonly BigInteger large = ref lhs;
                int largeLength = (int)lhs._length;

                ref readonly BigInteger small = ref rhs;
                int smallLength = (int)rhs._length;

                if (largeLength < smallLength)
                {
                    large = ref rhs;
                    largeLength = (int)rhs._length;

                    small = ref lhs;
                    smallLength = (int)lhs._length;
                }

                int maxResultLength = smallLength + largeLength;
                Debug.Assert(unchecked((uint)maxResultLength) <= MaxBlockCount);

                if (unchecked((uint)maxResultLength) > MaxBlockCount)
                {
                    // We shouldn't reach here, and the above assert will help flag this
                    // during testing, but we'll ensure that we return a safe value of
                    // zero in the case we end up overflowing in any way.

                    SetZero(out result);
                    return;
                }

                // The shared kernel accumulates into a zero-initialized result buffer.
                result._length = maxResultLength;

                ReadOnlySpan<nuint> largeBlocks = ((ReadOnlySpan<nuint>)large._blocks).Slice(0, largeLength);
                ReadOnlySpan<nuint> smallBlocks = ((ReadOnlySpan<nuint>)small._blocks).Slice(0, smallLength);
                Span<nuint> resultBlocks = ((Span<nuint>)result._blocks).Slice(0, maxResultLength);

                resultBlocks.Clear();
                BigIntegerCalculator.MultiplyNaive(largeBlocks, smallBlocks, resultBlocks);

                if ((maxResultLength > 0) && (resultBlocks[maxResultLength - 1] == 0))
                {
                    result._length--;
                }
            }

            public static void Pow2(int exponent, out BigInteger result)
            {
                Unsafe.SkipInit(out result);

                int blocksToShift = DivRemBlock(exponent, out int remainingBitsToShift);
                result._length = blocksToShift + 1;

                Debug.Assert(unchecked((uint)result._length) <= MaxBlockCount);

                if (unchecked((uint)result._length) > MaxBlockCount)
                {
                    // We shouldn't reach here, and the above assert will help flag this
                    // during testing, but we'll ensure that we return a safe value of
                    // zero in the case we end up overflowing in any way.

                    SetZero(out result);
                    return;
                }

                if (blocksToShift > 0)
                {
                    result.Clear(blocksToShift);
                }
                result._blocks[blocksToShift] = (nuint)1 << remainingBitsToShift;
            }

            // Reinterprets the power stored at the given block index of Pow10BigNumTable as a BigInteger.
            // The table packs each power as `nint _length` followed by its blocks, all in native-word
            // sized elements, so a block index maps to a byte offset of index * sizeof(nuint). Viewing the
            // bytes keeps the per-bitness element type (ulong on 64-bit, uint on 32-bit) out of the cast.
            private static ref readonly BigInteger Pow10BigNum(int index)
            {
                ReadOnlySpan<byte> table = MemoryMarshal.AsBytes(Pow10BigNumTable);
                int offset = index * sizeof(nuint);
                Debug.Assert((offset + sizeof(BigInteger)) <= table.Length);
                return ref Unsafe.As<byte, BigInteger>(ref MemoryMarshal.GetReference(table.Slice(offset)));
            }

            public static void Pow10(uint exponent, out BigInteger result)
            {
                // We leverage two arrays - Pow10Table and Pow10BigNumTable to speed up the Pow10 calculation.
                //
                // The low Pow10SmallExpBits of the exponent select 10^smallExp, which fits in a single block
                // and is applied with one lookup+multiply. Each remaining bit selects a power from
                // Pow10BigNumTable, whose entries are 10^(2^Pow10SmallExpBits), 10^(2^(Pow10SmallExpBits+1)), ...
                // So the result is 10^smallExp * product(selected big powers).
                //
                // The split is a clean power-of-two boundary: bits below Pow10SmallExpBits are small, the rest
                // are big. That requires 10^(2^Pow10SmallExpBits - 1) to still fit in a single block, which is
                // why the boundary tracks the block width (see Pow10SmallExpBits).
                //
                // For example on 32-bit (Pow10SmallExpBits == 3) exp = 0b111111 splits into 10^7 (0b111) and the
                // remaining 0b111 selecting 10^8 * 10^16 * 10^32.
                //
                // More details of this implementation can be found at: https://github.com/dotnet/coreclr/pull/12894#discussion_r128890596

                SetBlock(out BigInteger temp1, (nuint)Pow10Table[(int)(exponent & Pow10SmallExpMask)]);
                ref BigInteger lhs = ref temp1;

                SetZero(out BigInteger temp2);
                ref BigInteger product = ref temp2;

                exponent >>= Pow10SmallExpBits;
                int index = 0;

                while (exponent != 0)
                {
                    // If the current bit is set, multiply it with the corresponding power of 10
                    if ((exponent & 1) != 0)
                    {
                        // Multiply into the next temporary.
                        ref readonly BigInteger rhs = ref Pow10BigNum(Pow10BigNumTableIndices[index]);
                        Multiply(ref lhs, in rhs, out product);

                        // Swap to the next temporary
                        ref BigInteger temp = ref product;
                        product = ref lhs;
                        lhs = ref temp;
                    }

                    // Advance to the next bit
                    ++index;
                    exponent >>= 1;
                }

                SetValue(out result, ref lhs);
            }

            public void Add(nuint value)
            {
                int length = (int)_length;
                if (length == 0)
                {
                    SetUInt64(out this, value);
                    return;
                }

                _blocks[0] += value;
                if (_blocks[0] >= value)
                {
                    // No carry
                    return;
                }

                for (int index = 1; index < length; index++)
                {
                    _blocks[index]++;
                    if (_blocks[index] > 0)
                    {
                        // No carry
                        return;
                    }
                }

                Debug.Assert(unchecked((uint)length) < MaxBlockCount);

                if (unchecked((uint)length) >= MaxBlockCount)
                {
                    // We shouldn't reach here, and the above assert will help flag this
                    // during testing, but we'll ensure that we return a safe value of
                    // zero in the case we end up overflowing in any way.

                    SetZero(out this);
                    return;
                }

                _blocks[length] = 1;
                _length = length + 1;
            }

            public readonly nuint GetBlock(int index)
            {
                Debug.Assert((uint)index < _length);
                return _blocks[index];
            }

            public readonly ulong GetBits64(int bitIndex)
            {
                // Extracts the 64-bit window starting at bitIndex, i.e. bits [bitIndex, bitIndex + 64),
                // funnel-shifting across native blocks. Callers guarantee the window is in range.
                int block = bitIndex >> BlockShift;
                int shift = bitIndex & (BitsPerBlock - 1);

#if TARGET_64BIT
                Debug.Assert((uint)block < (uint)_length);
                ulong low = (ulong)_blocks[block];

                if (shift == 0)
                {
                    return low;
                }

                Debug.Assert((uint)(block + 1) < (uint)_length);
                ulong high = (ulong)_blocks[block + 1];
                return (low >> shift) | (high << (BitsPerBlock - shift));
#else
                Debug.Assert((uint)(block + 1) < (uint)_length);
                ulong low = _blocks[block] | ((ulong)_blocks[block + 1] << BitsPerBlock);

                if (shift == 0)
                {
                    return low;
                }

                Debug.Assert((uint)(block + 2) < (uint)_length);
                ulong high = _blocks[block + 2];
                return (low >> shift) | (high << (64 - shift));
#endif
            }

            public readonly bool HasZeroTail(int bitCount)
            {
                // Returns true when the lowest bitCount bits are all zero.
                Debug.Assert((uint)bitCount <= (uint)(_length * BitsPerBlock));

                int fullBlocks = bitCount >> BlockShift;

                if (((ReadOnlySpan<nuint>)_blocks)[..fullBlocks].ContainsAnyExcept((nuint)0))
                {
                    return false;
                }

                int remainingBits = bitCount & (BitsPerBlock - 1);

                if (remainingBits != 0)
                {
                    nuint mask = ((nuint)1 << remainingBits) - 1;
                    return (_blocks[fullBlocks] & mask) == 0;
                }

                return true;
            }

            public readonly int GetLength()
            {
                return (int)_length;
            }

            public readonly bool IsZero()
            {
                return _length == 0;
            }

            public void Multiply(nuint value)
            {
                Multiply(ref this, value, out this);
            }

            public void Multiply(scoped ref BigInteger value)
            {
                if (value._length <= 1)
                {
                    Multiply(ref this, value.ToBlock(), out this);
                }
                else
                {
                    SetValue(out BigInteger temp, ref this);
                    Multiply(ref temp, ref value, out this);
                }
            }

            public void Multiply10()
            {
                if (IsZero())
                {
                    return;
                }

                int length = (int)_length;

                // Multiply-by-10 is called once per output digit in Dragon4's hot loop. Routing it
                // through the shared Mul1 kernel costs a non-inlined call plus span setup per digit
                // (the unrolled UInt128 body is too large to inline), which measurably regresses
                // ToString of long fixed-precision formats. Keeping the single-block multiply inline
                // here avoids that; nint.Size constant-folds to the native-width loop.
                nuint carry = 0;

                if (nint.Size == 8)
                {
                    for (int i = 0; i < length; i++)
                    {
                        UInt128 product = (UInt128)(ulong)_blocks[i] * 10 + carry;
                        _blocks[i] = (nuint)product.Lower;
                        carry = (nuint)product.Upper;
                    }
                }
                else
                {
                    for (int i = 0; i < length; i++)
                    {
                        ulong product = (ulong)(uint)_blocks[i] * 10 + carry;
                        _blocks[i] = (nuint)(uint)product;
                        carry = (nuint)(uint)(product >> 32);
                    }
                }

                if (carry != 0)
                {
                    Debug.Assert(unchecked((uint)length) < MaxBlockCount);

                    if (unchecked((uint)length) >= MaxBlockCount)
                    {
                        // We shouldn't reach here, and the above assert will help flag this
                        // during testing, but we'll ensure that we return a safe value of
                        // zero in the case we end up overflowing in any way.

                        SetZero(out this);
                        return;
                    }

                    _blocks[length] = carry;
                    _length = length + 1;
                }
            }

            public void MultiplyPow10(uint exponent)
            {
                if (exponent < (uint)Pow10Table.Length)
                {
                    Multiply((nuint)Pow10Table[(int)exponent]);
                }
                else if (!IsZero())
                {
                    Pow10(exponent, out BigInteger poweredValue);
                    Multiply(ref poweredValue);
                }
            }

            public static void SetUInt32(out BigInteger result, uint value)
            {
                Unsafe.SkipInit(out result);

                if (value == 0)
                {
                    SetZero(out result);
                }
                else
                {
                    result._blocks[0] = value;
                    result._length = 1;
                }
            }

            public static void SetUInt64(out BigInteger result, ulong value)
            {
                Unsafe.SkipInit(out result);

#if TARGET_64BIT
                // A ulong fits into a single 64-bit block.
                if (value == 0)
                {
                    SetZero(out result);
                }
                else
                {
                    result._blocks[0] = (nuint)value;
                    result._length = 1;
                }
#else
                if (value <= uint.MaxValue)
                {
                    SetUInt32(out result, (uint)value);
                }
                else
                {
                    result._blocks[0] = (uint)value;
                    result._blocks[1] = (uint)(value >> 32);

                    result._length = 2;
                }
#endif
            }

            public static void SetValue(out BigInteger result, scoped ref BigInteger value)
            {
                Unsafe.SkipInit(out result);
                int rhsLength = (int)value._length;

                result._length = rhsLength;
                Buffer.Memmove(ref result._blocks[0], ref value._blocks[0], (nuint)rhsLength);
            }

            public static void SetZero(out BigInteger result)
            {
                Unsafe.SkipInit(out result);
                result._length = 0;
            }

            public void ShiftLeft(int shift)
            {
                Debug.Assert(shift >= 0);

                // Process blocks high to low so that we can safely process in place
                int length = (int)_length;

                if ((length == 0) || (shift == 0))
                {
                    return;
                }

                int blocksToShift = DivRemBlock(shift, out int remainingBitsToShift);

                // Copy blocks from high to low
                int readIndex = length - 1;
                int writeIndex = readIndex + blocksToShift;

                // Check if the shift is block aligned
                if (remainingBitsToShift == 0)
                {
                    Debug.Assert(unchecked((uint)length) < MaxBlockCount);

                    if (unchecked((uint)length) >= MaxBlockCount)
                    {
                        // We shouldn't reach here, and the above assert will help flag this
                        // during testing, but we'll ensure that we return a safe value of
                        // zero in the case we end up overflowing in any way.

                        SetZero(out this);
                        return;
                    }

                    // CopyTo is overlap-safe, so it handles the in-place upward block shift.
                    ((Span<nuint>)_blocks).Slice(0, length).CopyTo(((Span<nuint>)_blocks).Slice(blocksToShift));

                    _length += blocksToShift;

                    // Zero the remaining low blocks
                    Clear(blocksToShift);
                }
                else
                {
                    // We need an extra block for the partial shift

                    writeIndex++;
                    Debug.Assert(unchecked((uint)length) < MaxBlockCount);

                    if (unchecked((uint)length) >= MaxBlockCount)
                    {
                        // We shouldn't reach here, and the above assert will help flag this
                        // during testing, but we'll ensure that we return a safe value of
                        // zero in the case we end up overflowing in any way.

                        SetZero(out this);
                        return;
                    }

                    // Set the length to hold the shifted blocks
                    _length = writeIndex + 1;

                    // Output the initial blocks
                    int lowBitsShift = BitsPerBlock - remainingBitsToShift;

                    nuint highBits = 0;
                    nuint block = _blocks[readIndex];
                    nuint lowBits = block >> lowBitsShift;

                    while (readIndex > 0)
                    {
                        _blocks[writeIndex] = highBits | lowBits;
                        highBits = block << remainingBitsToShift;

                        --readIndex;
                        --writeIndex;

                        block = _blocks[readIndex];
                        lowBits = block >> lowBitsShift;
                    }

                    // Output the final blocks
                    _blocks[writeIndex] = highBits | lowBits;
                    _blocks[writeIndex - 1] = block << remainingBitsToShift;

                    // Zero the remaining low blocks
                    Clear(blocksToShift);

                    // Check if the terminating block has no set bits
                    if (_blocks[(int)_length - 1] == 0)
                    {
                        _length--;
                    }
                }
            }

            public readonly ulong ToUInt64()
            {
#if TARGET_64BIT
                // A single 64-bit block already holds the full value.
                if (_length > 0)
                {
                    return _blocks[0];
                }

                return 0;
#else
                if (_length > 1)
                {
                    return ((ulong)_blocks[1] << 32) + _blocks[0];
                }

                if (_length > 0)
                {
                    return _blocks[0];
                }

                return 0;
#endif
            }

            public UInt128 ToUInt128()
            {
#if TARGET_64BIT
                if (_length > 1)
                {
                    return new UInt128(_blocks[1], _blocks[0]);
                }

                if (_length > 0)
                {
                    return _blocks[0];
                }

                return 0;
#else
                if (_length > 3)
                {
                    return new UInt128(((ulong)_blocks[3] << 32) + _blocks[2], ((ulong)(_blocks[1]) << 32) + _blocks[0]);
                }

                if (_length > 2)
                {
                    return new UInt128((ulong)_blocks[2], ((ulong)_blocks[1] << 32) + _blocks[0]);
                }

                if (_length > 1)
                {
                    return ((ulong)(_blocks[1]) << 32) + _blocks[0];
                }

                if (_length > 0)
                {
                    return _blocks[0];
                }

                return 0;
#endif
            }

            private readonly nuint ToBlock()
            {
                if (_length > 0)
                {
                    return _blocks[0];
                }

                return 0;
            }

            private static void SetBlock(out BigInteger result, nuint value)
            {
                Unsafe.SkipInit(out result);

                if (value == 0)
                {
                    result._length = 0;
                }
                else
                {
                    result._blocks[0] = value;
                    result._length = 1;
                }
            }

            private void Clear(int length) => ((Span<nuint>)_blocks).Slice(0, length).Clear();

            private static int DivRemBlock(int value, out int remainder)
            {
                Debug.Assert(value >= 0);
                remainder = value & (BitsPerBlock - 1);
                return value >>> BlockShift;
            }

            [InlineArray(MaxBlockCount)]
            private struct BlocksBuffer
            {
                public nuint e0;
            }
        }
    }
}
