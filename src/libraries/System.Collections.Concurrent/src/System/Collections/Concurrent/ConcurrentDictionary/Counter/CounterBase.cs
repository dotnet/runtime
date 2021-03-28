// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
    /// <summary>
    /// Scalable counter base.
    /// </summary>
    internal class CounterBase
    {
        private protected const int CACHE_LINE = 64;
        private protected const int OBJ_HEADER_SIZE = 8;

        private protected static readonly int s_MaxCellCount = HashHelpers.AlignToPowerOfTwo(Environment.ProcessorCount) + 1;

        // how many cells we have
        private protected int cellCount;

        // delayed count time
        private protected uint lastCountTicks;

        private protected CounterBase()
        {
            // touch a static
            _ = s_MaxCellCount;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private protected static unsafe int GetIndex(uint cellCount)
        {
            if (IntPtr.Size == 4)
            {
                uint addr = (uint)&cellCount;
                return (int)(addr % cellCount);
            }
            else
            {
                ulong addr = (ulong)&cellCount;
                return (int)(addr % cellCount);
            }
        }
    }
}
