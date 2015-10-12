// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace System.Net
{
    internal static class RangeValidationHelpers
    {
        public static bool ValidateRange(int actual, int fromAllowed, int toAllowed)
        {
            // On false, API should throw new ArgumentOutOfRangeException("argument").
            return actual >= fromAllowed && actual <= toAllowed;
        }

        // There are threading tricks a malicious app can use to create an ArraySegment with mismatched 
        // array/offset/count.  Copy locally and make sure they're valid before using them.
        public static void ValidateSegment(ArraySegment<byte> segment)
        {
            // ArraySegment<byte> is not nullable.
            if (segment.Array == null)
            {
                throw new ArgumentNullException("segment");
            }

            // Length zero is explicitly allowed
            if (segment.Offset < 0 || segment.Count < 0 || segment.Count > (segment.Array.Length - segment.Offset))
            {
                throw new ArgumentOutOfRangeException("segment");
            }
        }
    }
}
