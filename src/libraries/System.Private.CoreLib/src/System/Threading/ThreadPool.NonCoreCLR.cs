// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace System.Threading
{
    //
    // Portable implementation of ThreadPool
    //

    public static partial class ThreadPool
    {
        private static bool EnsureConfigInitializedCore() => true;
    }
}
