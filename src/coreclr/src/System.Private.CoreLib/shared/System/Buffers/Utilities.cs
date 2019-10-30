// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace System.Buffers
{
    internal static class Utilities
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int SelectBucketIndex(int bufferSize)
        {
            Debug.Assert(bufferSize >= 0);
            uint bits = ((uint)bufferSize - 1) >> 4;
            return 32 - BitOperations.LeadingZeroCount(bits);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static int GetMaxSizeForBucket(int binIndex)
        {
            int maxSize = 16 << binIndex;
            Debug.Assert(maxSize >= 0);
            return maxSize;
        }
    }
}
