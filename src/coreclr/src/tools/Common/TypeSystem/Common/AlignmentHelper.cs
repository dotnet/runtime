// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;

namespace Internal.TypeSystem
{
    public static class AlignmentHelper
    {
        public static int AlignUp(this int val, int alignment)
        {
            Debug.Assert(val >= 0 && alignment >= 0);

            // alignment must be a power of 2 for this implementation to work (need modulo otherwise)
            Debug.Assert(0 == (alignment & (alignment - 1)));
            int result = (val + (alignment - 1)) & ~(alignment - 1);
            Debug.Assert(result >= val);      // check for overflow

            return result;
        }
    }
}
