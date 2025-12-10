// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Threading
{
    internal static class ManagedThreadId
    {
        // This will be initialized by the runtime.
        [ThreadStatic]
        private static int t_currentManagedThreadId;
        internal static int CurrentManagedThreadIdUnchecked => t_currentManagedThreadId;

        public static int Current => t_currentManagedThreadId;
    }
}
