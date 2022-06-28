// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Diagnostics;

using Internal.Runtime.Augments;

namespace System
{
    public partial class WeakReference
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

        // Most methods using m_handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).
        internal volatile IntPtr m_handle;
        internal bool m_IsLongReference;

        private void Create(object? target, bool trackResurrection)
        {
            m_IsLongReference = trackResurrection;
            m_handle = GCHandle.ToIntPtr(GCHandle.Alloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak));

            if (target != null)
            {
                // Set the conditional weak table if the target is a __ComObject.
                TrySetComTarget(target);
            }
        }

        //Determines whether or not this instance of WeakReference still refers to an object
        //that has not been collected.
        //
        public virtual bool IsAlive
        {
            get
            {
                IntPtr h = m_handle;

                // In determining whether it is valid to use this object, we need to at least expose this
                // without throwing an exception.
                if (default(IntPtr) == h)
                    return false;

                bool result = (RuntimeImports.RhHandleGet(h) != null || TryGetComTarget() != null);

                // We want to ensure that if the target is live, then we will
                // return it to the user. We need to keep this WeakReference object
                // live so m_handle doesn't get set to 0 or reused.
                // Since m_handle is volatile, the following statement will
                // guarantee the weakref object is live till the following
                // statement.
                return (m_handle == default(IntPtr)) ? false : result;
            }
        }

        //Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        //or just until they're finalized (false).
        //
        public virtual bool TrackResurrection
        {
            get { return m_IsLongReference; }
        }

        //Gets the Object stored in the handle if it's accessible.
        // Or sets it.
        //
        public virtual object? Target
        {
            get
            {
                IntPtr h = m_handle;
                // Should only happen when used illegally, like using a
                // WeakReference from a finalizer.
                if (default(IntPtr) == h)
                    return null;

                object? o = RuntimeImports.RhHandleGet(h);

                if (o == null)
                {
                    o = TryGetComTarget();
                }

                // We want to ensure that if the target is live, then we will
                // return it to the user. We need to keep this WeakReference object
                // live so m_handle doesn't get set to 0 or reused.
                // Since m_handle is volatile, the following statement will
                // guarantee the weakref object is live till the following
                // statement.
                return (m_handle == default(IntPtr)) ? null : o;
            }

            set
            {
                IntPtr h = m_handle;
                if (h == default(IntPtr))
                    throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

#if false
                // There is a race w/ finalization where m_handle gets set to
                // NULL and the WeakReference becomes invalid.  Here we have to
                // do the following in order:
                //
                // 1.  Get the old object value
                // 2.  Get m_handle
                // 3.  HndInterlockedCompareExchange(m_handle, newValue, oldValue);
                //
                // If the interlocked-cmp-exchange fails, then either we lost a race
                // with another updater, or we lost a race w/ the finalizer.  In
                // either case, we can just let the other guy win.
                Object oldValue = RuntimeImports.RhHandleGet(h);
                h = m_handle;
                if (h == default(IntPtr))
                    throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);
                GCHandle.InternalCompareExchange(h, value, oldValue, false /* isPinned */);
#else
                // The above logic seems somewhat paranoid and even wrong.
                //
                // 1.  It's the GC rather than any finalizer that clears weak handles (indeed there's no guarantee any finalizer is involved
                //     at all).
                // 2.  Retrieving the object from the handle atomically creates a strong reference to it, so
                //     as soon as we get the handle contents above (before it's even assigned into oldValue)
                //     the only race we can be in is with another setter.
                // 3.  We don't really care who wins in a race between two setters: last update wins is just
                //     as good as first update wins. If there was a race with the "finalizer" though, we'd
                //     probably want the setter to win (otherwise we could nullify a set just because it raced
                //     with the old object becoming unreferenced).
                //
                // The upshot of all of this is that we can just go ahead and set the handle. I suspect that
                // with further review I could prove that this class doesn't need to mess around with raw
                // IntPtrs at all and can simply use GCHandle directly, avoiding all these internal calls.

                // Check whether the new value is __COMObject. If so, add the new entry to conditional weak table.
                TrySetComTarget(value);
                RuntimeImports.RhHandleSet(h, value);
#endif

                // Ensure we don't have any handle recycling attacks in this
                // method where the finalizer frees the handle.
                GC.KeepAlive(this);
            }
        }

        /// <summary>
        /// This method checks whether the target to the weakreference is a native COMObject in which case the native object might still be alive although the RuntimeHandle could be null.
        /// Hence we check in the conditionalweaktable maintained by the System.private.Interop.dll that maps weakreferenceInstance->nativeComObject to check whether the native COMObject is alive or not.
        /// and gets\create a new RCW in case it is alive.
        /// </summary>
        /// <returns></returns>
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
        /// <param name="target"></param>
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
#pragma warning disable 420  // FYI - ref m_handle causes this.  I asked the C# team to add in "ref volatile T" as a parameter type in a future version.
            IntPtr handle = Interlocked.Exchange(ref m_handle, default(IntPtr));
#pragma warning restore 420
            if (handle != default(IntPtr))
                ((GCHandle)handle).Free();
        }

        private bool IsTrackResurrection() => m_IsLongReference;
    }
}
