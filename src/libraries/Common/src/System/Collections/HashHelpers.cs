// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Collections
{
    internal static partial class HashHelpers
    {
        public const uint HashCollisionThreshold = 100;

        // This is the maximum prime smaller than Array.MaxLength.
        public const int MaxPrimeArrayLength = 0x7FFFFFC3;

        public const int HashPrime = 101;
        private const int MinPrime = 3;

        // Table of prime numbers to use as hash table sizes.
        // A typical resize algorithm would pick the smallest prime number in this array
        // that is larger than twice the previous capacity.
        // Suppose our Hashtable currently has capacity x and enough elements are added
        // such that a resize needs to occur. Resizing first computes 2x then finds the
        // first prime in the table greater than 2x, i.e. if primes are ordered
        // p_1, p_2, ..., p_i, ..., it finds p_n such that p_n-1 < 2x < p_n.
        // Doubling is important for preserving the asymptotic complexity of the
        // hashtable operations such as add.  Having a prime guarantees that double
        // hashing does not lead to infinite loops.  IE, your hash function will be
        // h1(key) + i*h2(key), 0 <= i < size.  h2 and the size must be relatively prime.
        // We prefer the low computation costs of higher prime numbers over the increased
        // memory allocation of a fixed prime number i.e. when right sizing a HashSet.
        internal static ReadOnlySpan<int> Primes =>
        [
            3, 7, 11, 17, 23, 29, 37, 47, 59, 71, 89, 107, 131, 163, 197, 239, 293, 353, 431, 521, 631, 761, 919,
            1103, 1327, 1597, 1931, 2333, 2801, 3371, 4049, 4861, 5839, 7013, 8419, 10103, 12143, 14591,
            17519, 21023, 25229, 30293, 36353, 43627, 52361, 62851, 75431, 90523, 108631, 130363, 156437,
            187751, 225307, 270371, 324449, 389357, 467237, 560689, 672827, 807403, 968897, 1162687, 1395263,
            1674319, 2009191, 2411033, 2893249, 3471899, 4166287, 4999559, 5999471, 7199369
        ];

        public static bool IsPrime(int candidate)
        {
            // This only tests hash table capacities, whose minimum is MinPrime, so 2 is intentionally excluded.
            Debug.Assert(candidate >= MinPrime);

            if ((candidate & 1) == 0 || (uint)candidate % MinPrime == 0)
            {
                return candidate == MinPrime;
            }

            return HasNoPrimeDivisors(candidate, (int)Math.Sqrt(candidate));
        }

        private static bool HasNoPrimeDivisors(int candidate, int limit)
        {
            // Every prime greater than 3 is 6k - 1 or 6k + 1, so test both candidates in each group.
            for (int divisor = 5; divisor <= limit; divisor += 6)
            {
                if (candidate % divisor == 0 || candidate % (divisor + 2) == 0)
                {
                    return false;
                }
            }

            return true;
        }

        public static int GetPrime(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException(SR.Arg_HTCapacityOverflow);
            }

            if (min <= MinPrime)
            {
                return MinPrime;
            }

            // A short linear scan is faster for the common small capacities.
            const int LinearSearchCount = 16;

            ReadOnlySpan<int> primes = Primes;
            if (min <= primes[LinearSearchCount - 1])
            {
                for (int i = 1; i < LinearSearchCount; i++)
                {
                    if (primes[i] >= min)
                    {
                        return primes[i];
                    }
                }
            }
            else
            {
                int index = primes.Slice(LinearSearchCount).BinarySearch(min);
                index = index < 0 ? ~index : index;
                index += LinearSearchCount;
                if ((uint)index < (uint)primes.Length)
                {
                    return primes[index];
                }
            }

            return GetPrimeAtLeastCore(min);
        }

        public static int GetPrimeAtLeast(int min)
        {
            if (min < 0)
            {
                throw new ArgumentException(SR.Arg_HTCapacityOverflow);
            }

            return min <= MinPrime ? MinPrime : GetPrimeAtLeastCore(min);
        }

        private static int GetPrimeAtLeastCore(int min)
        {
            Debug.Assert(min > MinPrime);

            int candidate = min | 1;
            uint remainder = (uint)candidate % 6;
            if (remainder == 3)
            {
                candidate += 2;
            }

            int increment = remainder == 1 ? 4 : 2;
            int limit = (int)Math.Sqrt(candidate);
            long nextLimitSquared = (long)(limit + 1) * (limit + 1);
            while (true)
            {
                while (nextLimitSquared <= candidate)
                {
                    limit++;
                    nextLimitSquared = (long)(limit + 1) * (limit + 1);
                }

                if ((uint)(candidate - 1) % HashPrime != 0 && HasNoPrimeDivisors(candidate, limit))
                {
                    return candidate;
                }

                candidate += increment;
                increment = 6 - increment;
            }
        }

        // Returns the size of the hashtable to grow to.
        public static int ExpandPrime(int oldSize)
        {
            int newSize = 2 * oldSize;

            // Allow the hashtables to grow to maximum possible size (~2G elements) before encountering capacity overflow.
            // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
            if ((uint)newSize > MaxPrimeArrayLength && MaxPrimeArrayLength > oldSize)
            {
                Debug.Assert(MaxPrimeArrayLength == GetPrime(MaxPrimeArrayLength), "Invalid MaxPrimeArrayLength");
                return MaxPrimeArrayLength;
            }

            return GetPrime(newSize);
        }

        /// <summary>Returns approximate reciprocal of the divisor: ceil(2**64 / divisor).</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        public static ulong GetFastModMultiplier(uint divisor) =>
            ulong.MaxValue / divisor + 1;

        /// <summary>Performs a mod operation using the multiplier pre-computed with <see cref="GetFastModMultiplier"/>.</summary>
        /// <remarks>This should only be used on 64-bit.</remarks>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint FastMod(uint value, uint divisor, ulong multiplier)
        {
            // We use modified Daniel Lemire's fastmod algorithm (https://github.com/dotnet/runtime/pull/406),
            // which allows to avoid the long multiplication if the divisor is less than 2**31.
            Debug.Assert(divisor <= int.MaxValue);

            // This is equivalent of (uint)Math.BigMul(multiplier * value, divisor, out _). This version
            // is faster than BigMul currently because we only need the high bits.
            uint highbits = (uint)(((((multiplier * value) >> 32) + 1) * divisor) >> 32);

            Debug.Assert(highbits == value % divisor);
            return highbits;
        }
    }
}
