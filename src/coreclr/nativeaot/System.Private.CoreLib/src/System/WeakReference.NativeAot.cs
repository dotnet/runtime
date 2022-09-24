// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;
using System.Threading;
using System.Diagnostics;

namespace System
{
    public partial class WeakReference
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

        // Free all system resources associated with this reference.
        ~WeakReference()
        {
            // Note: While WeakReference is formally a finalizable type, the finalizer does not actually run.
            //       Instead the instances are treated specially in GC when scanning for no longer strongly-reachable
            //       finalizable objects.
            //
            // Unlike WeakReference<T> case, the instance could be of a derived type and
            //       in such case it is finalized via a finalizer.

            Debug.Assert(this.GetType() != typeof(WeakReference));

            IntPtr handle = Handle;
            if (handle != default(IntPtr))
            {
                GCHandle.InternalFree(handle);

                // keep the bit that indicates whether this reference was tracking resurrection
                m_handleAndKind &= TracksResurrectionBit;
            }
        }
    }
}
