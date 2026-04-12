// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Reflection.Internal
{
    internal static class BitArithmetic
    {
        internal static uint Align(uint position, uint alignment)
        {
            Debug.Assert(BitOperations.PopCount(alignment) == 1);

            uint result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }

        internal static int Align(int position, int alignment)
        {
            Debug.Assert(position >= 0 && alignment > 0);
            Debug.Assert(BitOperations.PopCount((uint)alignment) == 1);

            int result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }
    }
}
