// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Runtime.InteropServices
{
    public abstract partial class SafeHandle
    {
        // The handle cannot be closed until we are sure that no other objects might
        // be using it.  In the case of finalization, there may be other objects in
        // the finalization queue that still hold a reference to this SafeHandle.
        // So we can't assume that just because our finalizer is running, no other
        // object will need to access this handle.
        //
        // The CLR solves this by having SafeHandle derive from CriticalFinalizerObject.
        // This ensures that SafeHandle's finalizer will run only after all "normal"
        // finalizers in the queue.  But MRT doesn't support CriticalFinalizerObject, or
        // any other explicit control of finalization order.
        //
        // For now, we'll hack this by not releasing the handle when our finalizer
        // is called.  Instead, we create a new DelayedFinalizer instance, whose
        // finalizer will release the handle.  Thus the handle won't be released in this
        // finalization cycle, but should be released in the next.
        //
        // This has the effect of delaying cleanup for much longer than would have
        // happened on the CLR.  This also means that we may not close some handles
        // at shutdown, since there may not be another finalization cycle to run
        // the delayed finalizer.  If either of these end up being a problem, we should
        // consider adding more control over finalization order to MRT (or, better,
        // turning over control of finalization ordering to System.Private.CoreLib).

        private sealed class DelayedFinalizer
        {
            private readonly SafeHandle _safeHandle;

            public DelayedFinalizer(SafeHandle safeHandle)
            {
                _safeHandle = safeHandle;
            }

            ~DelayedFinalizer()
            {
                _safeHandle.Dispose(disposing: false);
            }
        }

        ~SafeHandle()
        {
            if (_fullyInitialized)
            {
                new DelayedFinalizer(this);
            }
        }
    }
}
