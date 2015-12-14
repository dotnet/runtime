// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
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
    }
}
