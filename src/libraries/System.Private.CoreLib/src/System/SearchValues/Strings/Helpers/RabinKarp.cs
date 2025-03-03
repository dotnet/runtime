// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using static System.Buffers.StringSearchValuesHelper;

namespace System.Buffers
{
    /// <summary>
    /// An implementation of the Rabin-Karp algorithm we use as a fallback for
    /// short inputs that we can't handle with Teddy.
    /// https://en.wikipedia.org/wiki/Rabin%E2%80%93Karp_algorithm
    /// Has an O(i * m) worst-case, but we will only use it for very short inputs.
    /// </summary>
    internal readonly struct RabinKarp
    {
        // The number of values we'll accept before falling back to Aho-Corasick.
        // This also affects when Teddy may be used.
        public const int MaxValues = 80;

        // This is a tradeoff between memory consumption and the number of false positives
        // we have to rule out during the verification step.
        private const nuint BucketCount = 64;

        // 18 = Vector128<byte>.Count + 2 (MatchStartOffset for N=3)
        // The logic in this class is not safe from overflows, but we avoid any issues by
        // only calling into it for inputs that are too short for Teddy to handle.
        private const int MaxInputLength = 18 - 1;

        // We're using nuint as the rolling hash, so we can spread the hash over more bits on 64bit.
        private static int HashShiftPerElement => IntPtr.Size == 8 ? 2 : 1;

        private readonly string[]?[] _buckets;
        private readonly int _hashLength;
        private readonly nuint _hashUpdateMultiplier;

        public RabinKarp(ReadOnlySpan<string> values)
        {
            Debug.Assert(values.Length <= MaxValues);

            int minimumLength = int.MaxValue;
            foreach (string value in values)
            {
                minimumLength = Math.Min(minimumLength, value.Length);
            }

            Debug.Assert(minimumLength > 1);

            _hashLength = minimumLength;
            _hashUpdateMultiplier = (nuint)1 << ((minimumLength - 1) * HashShiftPerElement);

            if (minimumLength > MaxInputLength)
            {
                // All the values are long. They'll either be handled by Teddy or won't match at all.
                // There's no point in allocating the buckets as they will never be accessed.
                _buckets = null!;
                return;
            }

            string[]?[] buckets = _buckets = new string[BucketCount][];

            foreach (string value in values)
            {
                if (value.Length > MaxInputLength)
                {
                    // This value can never match. There's no point in including it in the buckets.
                    continue;
                }

                nuint hash = 0;
                for (int i = 0; i < minimumLength; i++)
                {
                    hash = (hash << HashShiftPerElement) + value[i];
                }

                nuint bucket = hash % BucketCount;
                string[] newBucket;

                // Start with a bucket containing 1 element and reallocate larger ones if needed.
                // As MaxValues is similar to BucketCount, we will have 1 value per bucket on average.
                if (buckets[bucket] is string[] existingBucket)
                {
                    newBucket = new string[existingBucket.Length + 1];
                    existingBucket.AsSpan().CopyTo(newBucket);
                }
                else
                {
                    newBucket = new string[1];
                }

                newBucket[^1] = value;
                buckets[bucket] = newBucket;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly int IndexOfAny<TCaseSensitivity>(ReadOnlySpan<char> span)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            return typeof(TCaseSensitivity) == typeof(CaseInsensitiveUnicode)
                ? IndexOfAnyCaseInsensitiveUnicode(span)
                : IndexOfAnyCore<TCaseSensitivity>(span);
        }

        private readonly int IndexOfAnyCore<TCaseSensitivity>(ReadOnlySpan<char> span)
            where TCaseSensitivity : struct, ICaseSensitivity
        {
            Debug.Assert(typeof(TCaseSensitivity) != typeof(CaseInsensitiveUnicode));
            Debug.Assert(span.Length <= MaxInputLength, "Teddy should have handled short inputs.");

            ref char current = ref MemoryMarshal.GetReference(span);

            int hashLength = _hashLength;

            if (span.Length >= hashLength)
            {
                ref char end = ref Unsafe.Add(ref MemoryMarshal.GetReference(span), (uint)(span.Length - hashLength));

                nuint hash = 0;
                for (uint i = 0; i < hashLength; i++)
                {
                    hash = (hash << HashShiftPerElement) + TCaseSensitivity.TransformInput(Unsafe.Add(ref current, i));
                }

                Debug.Assert(_buckets is not null);
                ref string[]? bucketsRef = ref MemoryMarshal.GetArrayDataReference(_buckets);

                while (true)
                {
                    ValidateReadPosition(span, ref current);

                    if (Unsafe.Add(ref bucketsRef, hash % BucketCount) is string[] bucket)
                    {
                        int startOffset = (int)((nuint)Unsafe.ByteOffset(ref MemoryMarshal.GetReference(span), ref current) / sizeof(char));

                        if (StartsWith<TCaseSensitivity>(ref current, span.Length - startOffset, bucket))
                        {
                            return startOffset;
                        }
                    }

                    if (!Unsafe.IsAddressLessThan(ref current, ref end))
                    {
                        break;
                    }

                    char previous = TCaseSensitivity.TransformInput(current);
                    char next = TCaseSensitivity.TransformInput(Unsafe.Add(ref current, (uint)hashLength));

                    // Update the hash by removing the previous character and adding the next one.
                    hash = ((hash - (previous * _hashUpdateMultiplier)) << HashShiftPerElement) + next;
                    current = ref Unsafe.Add(ref current, 1);
                }
            }

            return -1;
        }

        private readonly int IndexOfAnyCaseInsensitiveUnicode(ReadOnlySpan<char> span)
        {
            Debug.Assert(span.Length <= MaxInputLength, "Teddy should have handled long inputs.");

            if (_hashLength > span.Length)
            {
                // Can't possibly match, all the values are longer than our input span.
                return -1;
            }

            Span<char> upperCase = stackalloc char[MaxInputLength].Slice(0, span.Length);

            int charsWritten = Ordinal.ToUpperOrdinal(span, upperCase);
            Debug.Assert(charsWritten == upperCase.Length);

            // CaseSensitive instead of CaseInsensitiveUnicode as we've already done the case conversion.
            return IndexOfAnyCore<CaseSensitive>(upperCase);
        }
    }
}
