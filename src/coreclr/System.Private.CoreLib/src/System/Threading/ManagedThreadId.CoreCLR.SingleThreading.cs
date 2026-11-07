// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Threading
{
    internal static class ManagedThreadId
    {
        // The runtime always assigns ID 1 to the only thread.
        internal const int SingleThreadId = 1;

        internal static int CurrentManagedThreadIdUnchecked => SingleThreadId;

        public static int Current => SingleThreadId;
    }
}
