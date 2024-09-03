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
        // This method is the same as GenerateBucketizedFingerprint below, but each bucket only contains 1 value.
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

            return (Vector512.Create(low), Vector512.Create(high));
        }

        // We can have up to 8 buckets, and their positions are encoded by 1 bit each.
        // Every bitmap encodes a mapping of each of the possible 16 nibble values into an 8-bit bitmap.
        // For example if bucket 0 contains strings ["foo", "bar"], the bitmaps will have the first bit (0th bucket) set like the following:
        // 'f' is 0x66, 'b' is 0x62, so n0Low has the bit set at index 2 and  6, n0High has it set at index 6.
        // 'o' is 0x6F, 'a' is 0x61, so n1Low has the bit set at index 1 and 15, n1High has it set at index 6.
        // 'o' is 0x6F, 'r' is 0x72, so n2Low has the bit set at index 2 and 15, n2High has it set at index 6 and 7.
        // We repeat this for each bucket and then OR together the bitmaps (fingerprints) of each bucket to generate a single bitmap for each nibble.
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

            return (Vector512.Create(low), Vector512.Create(high));
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
                    // Potential optimization: We currently merge values with different prefixes into buckets randomly (round-robin).
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
