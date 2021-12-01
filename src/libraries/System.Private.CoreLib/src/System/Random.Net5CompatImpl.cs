// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System
{
    public partial class Random
    {
        /// <summary>
        /// Provides an implementation used for compatibility with cases where a seed is specified
        /// and thus the sequence produced historically could have been relied upon.
        /// </summary>
        private sealed class Net5CompatSeedImpl : ImplBase
        {
            private CompatPrng _prng; // mutable struct; do not make this readonly

            public Net5CompatSeedImpl(int seed) =>
                _prng = new CompatPrng(seed);

            public override double Sample() => _prng.Sample();

            public override int Next() => _prng.InternalSample();

            public override int Next(int maxValue) => (int)(_prng.Sample() * maxValue);

            public override int Next(int minValue, int maxValue)
            {
                long range = (long)maxValue - minValue;
                return range <= int.MaxValue ?
                    (int)(_prng.Sample() * range) + minValue :
                    (int)((long)(_prng.GetSampleForLargeRange() * range) + minValue);
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

            public override long NextInt64(long maxValue) => NextInt64(0, maxValue);

            public override long NextInt64(long minValue, long maxValue)
            {
                ulong exclusiveRange = (ulong)(maxValue - minValue);

                if (exclusiveRange > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue - minValue
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling(exclusiveRange);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(long) * 8 - bits);
                        if (result < exclusiveRange)
                        {
                            return (long)result + minValue;
                        }
                    }
                }

                Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
                return minValue;
            }

            /// <summary>Produces a value in the range [0, ulong.MaxValue].</summary>
            private ulong NextUInt64() =>
                 ((ulong)(uint)Next(1 << 22)) |
                (((ulong)(uint)Next(1 << 22)) << 22) |
                (((ulong)(uint)Next(1 << 20)) << 44);

            public override double NextDouble() => _prng.Sample();

            public override float NextSingle() => (float)_prng.Sample();

            public override void NextBytes(byte[] buffer) => _prng.NextBytes(buffer);

            public override void NextBytes(Span<byte> buffer) => _prng.NextBytes(buffer);
        }

        /// <summary>
        /// Provides an implementation used for compatibility with cases where a derived type may
        /// reasonably expect its overrides to be called.
        /// </summary>
        private sealed class Net5CompatDerivedImpl : ImplBase
        {
            /// <summary>Reference to the <see cref="Random"/> containing this implementation instance.</summary>
            /// <remarks>Used to ensure that any calls to other virtual members are performed using the Random-derived instance, if one exists.</remarks>
            private readonly Random _parent;
            /// <summary>Potentially lazily-initialized algorithm backing this instance.</summary>
            private CompatPrng _prng; // mutable struct; do not make this readonly

            public Net5CompatDerivedImpl(Random parent) : this(parent, Shared.Next()) { }

            public Net5CompatDerivedImpl(Random parent, int seed)
            {
                _parent = parent;
                _prng = new CompatPrng(seed);
            }

            public override double Sample() => _prng.Sample();

            public override int Next() => _prng.InternalSample();

            public override int Next(int maxValue) => (int)(_parent.Sample() * maxValue);

            public override int Next(int minValue, int maxValue)
            {
                long range = (long)maxValue - minValue;
                return range <= int.MaxValue ?
                    (int)(_parent.Sample() * range) + minValue :
                    (int)((long)(_prng.GetSampleForLargeRange() * range) + minValue);
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

            public override long NextInt64(long maxValue) => NextInt64(0, maxValue);

            public override long NextInt64(long minValue, long maxValue)
            {
                ulong exclusiveRange = (ulong)(maxValue - minValue);

                if (exclusiveRange > 1)
                {
                    // Narrow down to the smallest range [0, 2^bits] that contains maxValue - minValue
                    // Then repeatedly generate a value in that outer range until we get one within the inner range.
                    int bits = BitOperations.Log2Ceiling(exclusiveRange);
                    while (true)
                    {
                        ulong result = NextUInt64() >> (sizeof(long) * 8 - bits);
                        if (result < exclusiveRange)
                        {
                            return (long)result + minValue;
                        }
                    }
                }

                Debug.Assert(minValue == maxValue || minValue + 1 == maxValue);
                return minValue;
            }

            /// <summary>Produces a value in the range [0, ulong.MaxValue].</summary>
            private unsafe ulong NextUInt64() =>
                 ((ulong)(uint)_parent.Next(1 << 22)) |
                (((ulong)(uint)_parent.Next(1 << 22)) << 22) |
                (((ulong)(uint)_parent.Next(1 << 20)) << 44);

            public override double NextDouble() => _parent.Sample();

            public override float NextSingle() => (float)_parent.Sample();

            public override void NextBytes(byte[] buffer) => _prng.NextBytes(buffer);

            public override void NextBytes(Span<byte> buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)_parent.Next();
                }
            }
        }

        /// <summary>
        /// Implementation used for compatibility with previous releases. The algorithm is based on a modified version
        /// of Knuth's subtractive random number generator algorithm.  See https://github.com/dotnet/runtime/issues/23198
        /// for a discussion of some of the modifications / discrepancies.
        /// </summary>
        private struct CompatPrng
        {
            private int[] _seedArray;
            private int _inext;
            private int _inextp;

            public CompatPrng(int seed)
            {
                // Initialize seed array.
                int[] seedArray = new int[56];

                int subtraction = (seed == int.MinValue) ? int.MaxValue : Math.Abs(seed);
                int mj = 161803398 - subtraction; // magic number based on Phi (golden ratio)
                seedArray[55] = mj;
                int mk = 1;

                int ii = 0;
                for (int i = 1; i < 55; i++)
                {
                    // The range [1..55] is special (Knuth) and so we're wasting the 0'th position.
                    if ((ii += 21) >= 55)
                    {
                        ii -= 55;
                    }

                    seedArray[ii] = mk;
                    mk = mj - mk;
                    if (mk < 0)
                    {
                        mk += int.MaxValue;
                    }

                    mj = seedArray[ii];
                }

                for (int k = 1; k < 5; k++)
                {
                    for (int i = 1; i < 56; i++)
                    {
                        int n = i + 30;
                        if (n >= 55)
                        {
                            n -= 55;
                        }

                        seedArray[i] -= seedArray[1 + n];
                        if (seedArray[i] < 0)
                        {
                            seedArray[i] += int.MaxValue;
                        }
                    }
                }

                _seedArray = seedArray;
                _inext = 0;
                _inextp = 21;
            }

            internal double Sample() =>
                // Including the division at the end gives us significantly improved random number distribution.
                InternalSample() * (1.0 / int.MaxValue);

            internal void NextBytes(Span<byte> buffer)
            {
                for (int i = 0; i < buffer.Length; i++)
                {
                    buffer[i] = (byte)InternalSample();
                }
            }

            internal int InternalSample()
            {
                int locINext = _inext;
                if (++locINext >= 56)
                {
                    locINext = 1;
                }

                int locINextp = _inextp;
                if (++locINextp >= 56)
                {
                    locINextp = 1;
                }

                int[] seedArray = _seedArray;
                int retVal = seedArray[locINext] - seedArray[locINextp];

                if (retVal == int.MaxValue)
                {
                    retVal--;
                }
                if (retVal < 0)
                {
                    retVal += int.MaxValue;
                }

                seedArray[locINext] = retVal;
                _inext = locINext;
                _inextp = locINextp;

                return retVal;
            }

            internal double GetSampleForLargeRange()
            {
                // The distribution of the double returned by Sample is not good enough for a large range.
                // If we use Sample for a range [int.MinValue..int.MaxValue), we will end up getting even numbers only.
                int result = InternalSample();

                // We can't use addition here: the distribution will be bad if we do that.
                if (InternalSample() % 2 == 0) // decide the sign based on second sample
                {
                    result = -result;
                }

                double d = result;
                d += int.MaxValue - 1; // get a number in range [0..2*int.MaxValue-1)
                d /= 2u * int.MaxValue - 1;
                return d;
            }
        }
    }
}
