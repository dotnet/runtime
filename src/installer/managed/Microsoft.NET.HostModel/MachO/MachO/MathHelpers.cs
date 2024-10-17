// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace Microsoft.NET.HostModel
{
    internal static class Utils
    {
        internal static uint Log2(uint value)
        {
            uint result = 0;
            while (value > 1)
            {
                value >>= 1;
                result++;
            }
            return result;
        }

        internal static int Align(int value, int alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        internal static uint Align(uint value, uint alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }

        internal static ulong Align(ulong value, ulong alignment)
        {
            return (value + (alignment - 1)) & ~(alignment - 1);
        }
    }
}
