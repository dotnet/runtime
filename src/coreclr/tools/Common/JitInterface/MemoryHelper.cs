// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Internal.JitInterface
{
    internal static unsafe class MemoryHelper
    {
        public static void FillMemory(byte* dest, byte fill, int count)
        {
            for (; count > 0; count--)
            {
                *dest = fill;
                dest++;
            }
        }
    }
}
