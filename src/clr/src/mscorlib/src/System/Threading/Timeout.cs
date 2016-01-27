// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Threading {
    using System.Threading;
    using System;
    // A constant used by methods that take a timeout (Object.Wait, Thread.Sleep
    // etc) to indicate that no timeout should occur.
    //
    [System.Runtime.InteropServices.ComVisible(true)]
    public static class Timeout
    {
        [System.Runtime.InteropServices.ComVisible(false)]
        public static readonly TimeSpan InfiniteTimeSpan = new TimeSpan(0, 0, 0, 0, Timeout.Infinite);

        public const int Infinite = -1;
        internal const uint UnsignedInfinite = unchecked((uint)-1);
    }

}
