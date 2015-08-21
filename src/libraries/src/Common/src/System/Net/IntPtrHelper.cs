// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class IntPtrHelper
    {
        internal static IntPtr Add(IntPtr a, int b)
        {
            return (IntPtr)((long)a + (long)b);
        }

        internal static long Subtract(IntPtr a, IntPtr b)
        {
            return ((long)a - (long)b);
        }
    }
}
