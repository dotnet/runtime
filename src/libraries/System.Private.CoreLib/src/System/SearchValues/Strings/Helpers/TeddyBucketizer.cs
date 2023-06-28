// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics;

namespace System.Buffers
{
    internal static class TeddyBucketizer
    {
        public static (Vector512<byte> Low, Vector512<byte> High) GenerateNonBucketizedFingerprint(ReadOnlySpan<string> values, int offset)
        {
            Debug.Assert(values.Length <= 8);

            Vector128<byte> low = default;
            Vector128<byte> high = default;

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];

                int bit = 1 << i;

                char c = value[offset];
                Debug.Assert(char.IsAscii(c));

                int lowNibble = c & 0xF;
                int highNibble = c >> 4;

                low.SetElementUnsafe(lowNibble, (byte)(low.GetElementUnsafe(lowNibble) | bit));
                high.SetElementUnsafe(highNibble, (byte)(high.GetElementUnsafe(highNibble) | bit));
            }

            return (DuplicateTo512(low), DuplicateTo512(high));
        }

        public static (Vector512<byte> Low, Vector512<byte> High) GenerateBucketizedFingerprint(string[][] valueBuckets, int offset)
        {
            Debug.Assert(valueBuckets.Length <= 8);

            Vector128<byte> low = default;
            Vector128<byte> high = default;

            for (int i = 0; i < valueBuckets.Length; i++)
            {
                int bit = 1 << i;

                foreach (string value in valueBuckets[i])
                {
                    char c = value[offset];
                    Debug.Assert(char.IsAscii(c));

                    int lowNibble = c & 0xF;
                    int highNibble = c >> 4;

                    low.SetElementUnsafe(lowNibble, (byte)(low.GetElementUnsafe(lowNibble) | bit));
                    high.SetElementUnsafe(highNibble, (byte)(high.GetElementUnsafe(highNibble) | bit));
                }
            }

            return (DuplicateTo512(low), DuplicateTo512(high));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Vector512<byte> DuplicateTo512(Vector128<byte> vector)
        {
            Vector256<byte> vector256 = Vector256.Create(vector, vector);
            return Vector512.Create(vector256, vector256);
        }

        public static string[][] Bucketize(ReadOnlySpan<string> values, int bucketCount, int n)
        {
            Debug.Assert(bucketCount == 8, "This may change if we end up supporting the 'fat Teddy' variant.");
            Debug.Assert(values.Length > bucketCount, "Should be using a non-bucketized implementation.");
            Debug.Assert(values.Length <= RabinKarp.MaxValues);

            // Stores the offset of the bucket each value should be assigned to.
            // This lets us avoid allocating temporary lists to build up each bucket.
            Span<int> bucketIndexes = stackalloc int[RabinKarp.MaxValues].Slice(0, values.Length);

            // Group patterns with the same prefix into the same bucket to avoid wasting time during verification steps.
            Dictionary<int, int> prefixToBucket = new(bucketCount);

            int bucketCounter = 0;

            for (int i = 0; i < values.Length; i++)
            {
                string value = values[i];

                int prefix = 0;
                for (int j = 0; j < n; j++)
                {
                    Debug.Assert(char.IsAscii(value[j]));
                    prefix = (prefix << 8) | value[j];
                }

                if (!prefixToBucket.TryGetValue(prefix, out int bucketIndex))
                {
                    // TODO: We currently merge values with different prefixes into buckets randomly (round-robin).
                    // We could employ a more sophisticated strategy here, e.g. by trying to minimize the number of
                    // values in each bucket, or by minimizing the PopCount of final merged fingerprints.
                    // Example of the latter: https://gist.github.com/MihaZupan/831324d1d646b69ae0ba4b54e3446a49

                    bucketIndex = bucketCounter++ % bucketCount;
                    prefixToBucket.Add(prefix, bucketIndex);
                }

                bucketIndexes[i] = bucketIndex;
            }

            string[][] buckets = new string[bucketCount][];

            for (int bucketIndex = 0; bucketIndex < buckets.Length; bucketIndex++)
            {
                string[] strings = buckets[bucketIndex] = new string[bucketIndexes.Count(bucketIndex)];

                int count = 0;
                for (int i = 0; i < bucketIndexes.Length; i++)
                {
                    if (bucketIndexes[i] == bucketIndex)
                    {
                        strings[count++] = values[i];
                    }
                }
                Debug.Assert(count == strings.Length);
            }

            return buckets;
        }
    }
}
