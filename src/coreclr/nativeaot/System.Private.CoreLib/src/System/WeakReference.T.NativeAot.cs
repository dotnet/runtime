// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System
{
    public sealed partial class WeakReference<T>
        where T : class?
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

        // Note: While WeakReference<T> is formally a finalizable type, the finalizer does not actually run.
        //       Instead the instances are treated specially in GC when scanning for no longer strongly-reachable
        //       finalizable objects.
#pragma warning disable CA1821 // Remove empty Finalizers
        ~WeakReference()
        {
            Debug.Assert(false, " WeakReference<T> finalizer should never run");
        }
#pragma warning restore CA1821 // Remove empty Finalizers
    }
}
