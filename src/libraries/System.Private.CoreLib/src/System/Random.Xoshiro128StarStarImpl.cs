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
        /// Provides an implementation of the xoshiro128** algorithm. This implementation is used
        /// on 32-bit when no seed is specified and an instance of the base Random class is constructed.
        /// As such, we are free to implement however we see fit, without back compat concerns around
        /// the sequence of numbers generated or what methods call what other methods.
        /// </summary>
        internal sealed class XoshiroImpl : ImplBase
        {
            // NextUInt32 is based on the algorithm from http://prng.di.unimi.it/xoshiro128starstar.c:
            //
            //     Written in 2018 by David Blackman and Sebastiano Vigna (vigna@acm.org)
            //
            //     To the extent possible under law, the author has dedicated all copyright
            //     and related and neighboring rights to this software to the public domain
            //     worldwide. This software is distributed without any warranty.
            //
            //     See <http://creativecommons.org/publicdomain/zero/1.0/>.

            private uint _s0, _s1, _s2, _s3;

            public unsafe XoshiroImpl()
            {
                uint* ptr = stackalloc uint[4];
                do
                {
                    Interop.GetRandomBytes((byte*)ptr, 4 * sizeof(uint));
                    _s0 = ptr[0];
                    _s1 = ptr[1];
                    _s2 = ptr[2];
                    _s3 = ptr[3];
                }
                while ((_s0 | _s1 | _s2 | _s3) == 0); // at least one value must be non-zero
            }

            /// <summary>Produces a value in the range [0, uint.MaxValue].</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by a handful of "next" methods
            internal uint NextUInt32()
            {
                uint s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

                uint result = BitOperations.RotateLeft(s1 * 5, 7) * 9;
                uint t = s1 << 9;

                s2 ^= s0;
                s3 ^= s1;
                s1 ^= s2;
                s0 ^= s3;

                s2 ^= t;
                s3 = BitOperations.RotateLeft(s3, 11);

                _s0 = s0;
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;

                return result;
            }

            /// <summary>Produces a value in the range [0, ulong.MaxValue].</summary>
            [MethodImpl(MethodImplOptions.AggressiveInlining)] // small-ish hot path used by a handful of "next" methods
            internal ulong NextUInt64() => (((ulong)NextUInt32()) << 32) | NextUInt32();

            public override int Next()
            {
                while (true)
                {
                    // Get top 31 bits to get a value in the range [0, int.MaxValue], but try again
                    // if the value is actually int.MaxValue, as the method is defined to return a value
                    // in the range [0, int.MaxValue).
                    uint result = NextUInt32() >> 1;
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
                        uint result = NextUInt32() >> (sizeof(uint) * 8 - bits);
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
                uint exclusiveRange = (uint)(maxValue - minValue);

                if (exclusiveRange > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue.
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling(exclusiveRange);
                    while (true)
                    {
                        uint result = NextUInt32() >> (sizeof(uint) * 8 - bits);
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
                if (maxValue <= int.MaxValue)
                {
                    return Next((int)maxValue);
                }

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

                if (exclusiveRange <= int.MaxValue)
                {
                    return Next((int)exclusiveRange) + minValue;
                }

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
                uint s0 = _s0, s1 = _s1, s2 = _s2, s3 = _s3;

                while (buffer.Length >= sizeof(uint))
                {
                    Unsafe.WriteUnaligned(
                        ref MemoryMarshal.GetReference(buffer),
                        BitOperations.RotateLeft(s1 * 5, 7) * 9);

                    // Update PRNG state.
                    uint t = s1 << 9;
                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;
                    s2 ^= t;
                    s3 = BitOperations.RotateLeft(s3, 11);

                    buffer = buffer.Slice(sizeof(uint));
                }

                if (!buffer.IsEmpty)
                {
                    uint next = BitOperations.RotateLeft(s1 * 5, 7) * 9;
                    byte* remainingBytes = (byte*)&next;
                    Debug.Assert(buffer.Length < sizeof(uint));
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        buffer[i] = remainingBytes[i];
                    }

                    // Update PRNG state.
                    uint t = s1 << 9;
                    s2 ^= s0;
                    s3 ^= s1;
                    s1 ^= s2;
                    s0 ^= s3;
                    s2 ^= t;
                    s3 = BitOperations.RotateLeft(s3, 11);
                }

                _s0 = s0;
                _s1 = s1;
                _s2 = s2;
                _s3 = s3;
            }

            public override double NextDouble() =>
                // See comment in Xoshiro256StarStarImpl.
                (NextUInt64() >> 11) * (1.0 / (1ul << 53));

            public override float NextSingle() =>
                // See comment in Xoshiro256StarStarImpl.
                (NextUInt32() >> 8) * (1.0f / (1u << 24));

            public override double Sample()
            {
                Debug.Fail("Not used or called for this implementation.");
                throw new NotSupportedException();
            }
        }
    }
}
