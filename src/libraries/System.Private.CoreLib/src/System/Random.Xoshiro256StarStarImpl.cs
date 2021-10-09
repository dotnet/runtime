// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Internal.Runtime.CompilerServices;

namespace System
{
    public partial class Random
    {
        /// <summary>
        /// Provides an implementation of the xoshiro256** algorithm. This implementation is used
        /// on 64-bit when no seed is specified and an instance of the base Random class is constructed.
        /// As such, we are free to implement however we see fit, without back compat concerns around
        /// the sequence of numbers generated or what methods call what other methods.
        /// </summary>
        internal sealed class XoshiroImpl : ImplBase
        {
            // NextUInt64 is based on the algorithm from http://prng.di.unimi.it/xoshiro256starstar.c:
            //
            //     Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
            //
            //     To the extent possible under law, the author has dedicated all copyright
            //     and related and neighboring rights to this software to the public domain
            //     worldwide. This software is distributed without any warranty.
            //
            //     See <http://creativecommons.org/publicdomain/zero/1.0/>.

            private ulong _s0, _s1, _s2, _s3;

            public unsafe XoshiroImpl()
            {
                ulong* ptr = stackalloc ulong[4];
                do
                {
                    Interop.GetRandomBytes((byte*)ptr, 4 * sizeof(ulong));
                    _s0 = ptr[0];
                    _s1 = ptr[1];
                    _s2 = ptr[2];
                    _s3 = ptr[3];
                }
                while ((_s0 | _s1 | _s2 | _s3) == 0); // at least one value must be non-zero
            }

            /// <summary>Produces a value in the range [0, uint.MaxValue].</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by very few call sites
            internal uint NextUInt32() => (uint)(NextUInt64() >> 32);

            /// <summary>Produces a value in the range [0, ulong.MaxValue].</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by a handful of "next" methods
            internal ulong NextUInt64()
            {
                ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

                ulong result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
                ulong t = s1 << 17;

                s2 ^= s0;
                s3 ^= s1;
                s1 ^= s2;
                s0 ^= s3;

                s2 ^= t;
                s3 = BitOperations.RotateLeft(s3, 45);

                _s0 = s0;
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;

                return result;
            }

            public override int Next()
            {
                while (true)
                {
                    // Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
                    // if the value is actually int.MaxValue, as the method is defined to return a value
                    // in the range [0, int.MaxValue).
                    ulong result = NextUInt64() >> 33;
                    if (result != int.MaxValue)
                    {
                        return (int)result;
                    }
                }
            }

            public override int Next(int maxValue)
            {
                if (maxValue > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling((uint)maxValue);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(ulong) * 8 - bits);
                        if (result < (uint)maxValue)
                        {
                            return (int)result;
                        }
                    }
                }

                Debug.Assert(maxValue == 0 || maxValue == 1);
                return 0;
            }

            public override int Next(int minValue, int maxValue)
            {
                ulong exclusiveRange = (ulong)((long)maxValue - minValue);

                if (exclusiveRange > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling(exclusiveRange);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(ulong) * 8 - bits);
                        if (result < exclusiveRange)
                        {
                            return (int)result + minValue;
                        }
                    }
                }

                Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
                return minValue;
            }

            public override long NextInt64()
            {
                while (true)
                {
                    // Get top 63 bits to get a value in the range [0, long.MaxValue], but try again
                    // if the value is actually long.MaxValue, as the method is defined to return a value
                    // in the range [0, long.MaxValue).
                    ulong result = NextUInt64() >> 1;
                    if (result != long.MaxValue)
                    {
                        return (long)result;
                    }
                }
            }

            public override long NextInt64(long maxValue)
            {
                if (maxValue > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling((ulong)maxValue);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(ulong) * 8 - bits);
                        if (result < (ulong)maxValue)
                        {
                            return (long)result;
                        }
                    }
                }

                Debug.Assert(maxValue == 0 || maxValue == 1);
                return 0;
            }

            public override long NextInt64(long minValue, long maxValue)
            {
                ulong exclusiveRange = (ulong)(maxValue - minValue);

                if (exclusiveRange > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling(exclusiveRange);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(ulong) * 8 - bits);
                        if (result < exclusiveRange)
                        {
                            return (long)result + minValue;
                        }
                    }
                }

                Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
                return minValue;
            }

            public override void NextBytes(byte[] buffer) => NextBytes((Span<byte>)buffer);

            public override unsafe void NextBytes(Span<byte> buffer)
            {
                ulong s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

                while (buffer.Length >= sizeof(ulong))
                {
                    Unsafe.WriteUnaligned(
                        ref MemoryMarshal.GetReference(buffer),
                        BitOperations.RotateLeft(s1 * 5, 7) * 9);

                    // Update PRNG state.
                    ulong t = s1 << 17;
                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;
                    s2 ^= t;
                    s3 = BitOperations.RotateLeft(s3, 45);

                    buffer = buffer.Slice(sizeof(ulong));
                }

                if (!buffer.IsEmpty)
                {
                    ulong next = BitOperations.RotateLeft(s1 * 5, 7) * 9;
                    byte* remainingBytes = (byte*)&next;
                    Debug.Assert(buffer.Length < sizeof(ulong));
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = remainingBytes[i];
                    }

                    // Update PRNG state.
                    ulong t = s1 << 17;
                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;
                    s2 ^= t;
                    s3 = BitOperations.RotateLeft(s3, 45);
                }

                _s0 = s0;
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;
            }

            public override double NextDouble() =>
                // As described in http://prng.di.unimi.it/:
                // "A standard double (64-bit) floating-point number in IEEE floating point format has 52 bits of significand,
                //  plus an implicit bit at the left of the significand. Thus, the representation can actually store numbers with
                //  53 significant binary digits. Because of this fact, in C99 a 64-bit unsigned integer x should be converted to
                //  a 64-bit double using the expression
                //  (x >> 11) * 0x1.0p-53"
                (NextUInt64() >> 11) * (1.0 / (1ul << 53));

            public override float NextSingle() =>
                // Same as above, but with 24 bits instead of 53.
                (NextUInt64() >> 40) * (1.0f / (1u << 24));

            public override double Sample()
            {
                Debug.Fail("Not used or called for this implementation.");
                throw new NotSupportedException();
            }
        }
    }
}
