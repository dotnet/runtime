// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Reflection.Metadata
{
    internal static class StringUtils
    {
        internal static int IgnoreCaseMask(bool ignoreCase)
        {
            return ignoreCase ? 0x20 : 0xff;
        }

        internal static bool IsEqualAscii(int a, int b, int ignoreCaseMask)
        {
            // When not ignoring case (most often):
            // - only the first condition is evaluated multiple times during the loop.
            // - the remaining condition is false since ignoreCaseMask is 0xff.
            // When ignoring case 
            // - the most likely case is still a == b
            // - ignoreCaseMask is 0x20
            return a == b || ((a | 0x20) == (b | 0x20) && unchecked((uint)((a | ignoreCaseMask) - 'a')) <= 'z' - 'a');
        }
    }
}
