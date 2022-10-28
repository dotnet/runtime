// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace System.Collections
{
    internal static class ThrowHelper
    {
        public static void IfBufferTooSmall(int actual, int required)
        {
            if (actual < required)
            {
                ThrowDestinationArrayTooSmall();
            }
        }

        [DoesNotReturn]
        public static void ThrowDestinationArrayTooSmall() =>
            throw new ArgumentException(SR.CapacityMustBeGreaterThanOrEqualToCount);
    }
}
