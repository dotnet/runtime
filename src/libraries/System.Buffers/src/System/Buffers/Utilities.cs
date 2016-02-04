// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SelectBucketIndex(int bufferSize)
        {
            uint bitsRemaining = ((uint)bufferSize - 1) >> 4;
            int poolIndex = 0;

            while (bitsRemaining > 0)
            {
                bitsRemaining >>= 1;
                poolIndex++;
            }

            return poolIndex;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaxSizeForBucket(int binIndex)
        {
            checked
            {
                int result = 2;
                int shifts = binIndex + 3;
                result <<= shifts;
                return result;
            }
        }

        internal static int GetPoolId<T>(ArrayPool<T> pool)
        {
            return pool.GetHashCode();
        }

        internal static int GetBufferId<T>(T[] buffer)
        {
            return buffer.GetHashCode();
        }

        internal static int GetBucketId<T>(DefaultArrayPoolBucket<T> bucket)
        {
            return bucket.GetHashCode();
        }
    }
}
