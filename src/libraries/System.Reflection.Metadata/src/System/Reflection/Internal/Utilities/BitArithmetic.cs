// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Numerics;

namespace System.Reflection.Internal
{
    internal static class BitArithmetic
    {
        internal static int CountBits(int v)
        {
            return CountBits(unchecked((uint)v));
        }

        internal static int CountBits(uint v)
        {
            return BitOperations.PopCount(v);
        }

        internal static int CountBits(ulong v)
        {
            return BitOperations.PopCount(v);
        }

        internal static uint Align(uint position, uint alignment)
        {
            Debug.Assert(CountBits(alignment) == 1);

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
            Debug.Assert(CountBits(alignment) == 1);

            int result = position & ~(alignment - 1);
            if (result == position)
            {
                return result;
            }

            return result + alignment;
        }
    }
}
