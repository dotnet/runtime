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

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

        // the handle field is effectively readonly until the object is finalized.
        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private IntPtr m_handleAndKind;

        private void Create(object? target, bool trackResurrection)
        {
            IntPtr h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            m_handleAndKind = trackResurrection ? h | 1: h;

            if (target != null)
            {
                // Set the conditional weak table if the target is a __ComObject.
                TrySetComTarget(target);
            }
        }

        internal IntPtr Handle => m_handleAndKind & ~1;

        //Determines whether or not this instance of WeakReference still refers to an object
        //that has not been collected.
        public virtual bool IsAlive
        {
            get
            {
                IntPtr h = Handle;

                // In determining whether it is valid to use this object, we need to at least expose this
                // without throwing an exception.
                if (default(IntPtr) == h)
                    return false;

                bool result = GCHandle.InternalGet(h) != null || TryGetComTarget() != null;

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return result;
            }
        }

        //Gets the Object stored in the handle if it's accessible.
        // Or sets it.
        public virtual object? Target
        {
            get
            {
                IntPtr h = Handle;
                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // GC can finalize the instance if it becomes F-Reachable.
                // That, however, cannot happen while we use the instance.
                //
                // A derived class will be finalized with an actual Finalize, but the finalizer queue is single threaded,
                // thus the default implementation will never access Target concurrently with finalizing.
                //
                // There is a possibility that a derived type overrides the default finalizer and arranges concurrent access.
                // There is nothing that we can do about that and a few other exotic ways to break this.
                //
                if (default(IntPtr) == h)
                    return default;

                object? target = GCHandle.InternalGet(h) ?? TryGetComTarget();

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return target;
            }

            set
            {
                IntPtr h = Handle;
                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // See the comment in the getter.
                if (default(IntPtr) == h)
                    throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

                // Update the conditionalweakTable in case the target is __ComObject.
                TrySetComTarget(value);

                GCHandle.InternalSet(h, value);

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);
            }
        }

        /// <summary>
        /// This method checks whether the target to the weakreference is a native COMObject in which case the native object might still be alive although the RuntimeHandle could be null.
        /// Hence we check in the conditionalweaktable maintained by the System.private.Interop.dll that maps weakreferenceInstance->nativeComObject to check whether the native COMObject is alive or not.
        /// and gets\create a new RCW in case it is alive.
        /// </summary>
        private static object? TryGetComTarget()
        {
#if ENABLE_WINRT
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks != null)
            {
                return callbacks.GetCOMWeakReferenceTarget(this);
            }
            else
            {
                Debug.Fail("WinRTInteropCallback is null");
            }
#endif // ENABLE_WINRT
            return null;
        }

        /// <summary>
        /// This method notifies the System.private.Interop.dll to update the conditionalweaktable for weakreferenceInstance->target in case the target is __ComObject. This ensures that we have a means to
        /// go from the managed weak reference to the actual native object even though the managed counterpart might have been collected.
        /// </summary>
        private static void TrySetComTarget(object? target)
        {
#if ENABLE_WINRT
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks != null)
            {
                callbacks.SetCOMWeakReferenceTarget(this, target);
            }
            else
            {
                Debug.Fail("WinRTInteropCallback is null");
            }
#endif // ENABLE_WINRT
        }

        // Free all system resources associated with this reference.
        ~WeakReference()
        {
            // Note: While WeakReference is a formally a finalizable type, the finalizer does not actually run.
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

                // keep the lowermost bit that indicates whether this reference was tracking resurrection
                m_handleAndKind &= 1;
            }
        }

        //Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        //or just until they're finalized (false).
        private bool IsTrackResurrection() => (m_handleAndKind & 1) != 0;
    }
}
