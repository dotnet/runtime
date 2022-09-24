// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    public sealed partial class WeakReference<T>
        where T : class?
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

        // the handle field is effectively readonly, not formally readonly because we assign it in "Create".
        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private IntPtr m_handleAndKind;
        private const nint TracksResurrectionBit = 1;

        //Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        //or just until they're finalized (false).
        private bool IsTrackResurrection() => (m_handleAndKind & TracksResurrectionBit) != 0;

        //Creates a new WeakReference that keeps track of target.
        private void Create(T target, bool trackResurrection)
        {
            IntPtr h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            m_handleAndKind = trackResurrection ?
                h | TracksResurrectionBit :
                h;
        }

        private IntPtr Handle => m_handleAndKind & ~TracksResurrectionBit;

        public void SetTarget(T target)
        {
            IntPtr h = Handle;
            // Should only happen for corner cases, like using a WeakReference from a finalizer.
            // GC can finalize the instance if it becomes F-Reachable.
            // That, however, cannot happen while we use the instance.
            if (default(IntPtr) == h)
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

            GCHandle.InternalSet(h, target);

            // must keep the instance alive as long as we use the handle.
            GC.KeepAlive(this);
        }

        private T? Target
        {
            get
            {
                IntPtr h = Handle;
                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // GC can finalize the instance if it becomes F-Reachable.
                // That, however, cannot happen while we use the instance.
                if (default(IntPtr) == h)
                    return default;

                // unsafe cast is ok as the handle cannot be destroyed and recycled while we keep the instance alive
                T? target = Unsafe.As<T?>(GCHandle.InternalGet(h));

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return target;
            }
        }

        // Note: While WeakReference<T> is a formally a finalizable type, the finalizer does not actually run.
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
