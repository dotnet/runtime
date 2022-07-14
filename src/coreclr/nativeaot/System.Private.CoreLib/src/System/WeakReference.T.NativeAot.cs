// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

namespace System
{
    public sealed partial class WeakReference<T>
        where T : class?
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

        internal volatile IntPtr m_handle;
        private bool m_trackResurrection;


        //Creates a new WeakReference that keeps track of target.
        //
        private void Create(T target, bool trackResurrection)
        {
            m_handle = (IntPtr)GCHandle.Alloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            m_trackResurrection = trackResurrection;

            if (target != null)
            {
                // Set the conditional weak table if the target is a __ComObject.
                TrySetComTarget(target);
            }
        }

        public void SetTarget(T target)
        {
            if (m_handle == default(IntPtr))
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

            // Update the conditionalweakTable in case the target is __ComObject.
            TrySetComTarget(target);

            RuntimeImports.RhHandleSet(m_handle, target);
            GC.KeepAlive(this);
        }

        private T? Target
        {
            get
            {
                IntPtr h = m_handle;

                // Should only happen for corner cases, like using a
                // WeakReference from a finalizer.
                if (default(IntPtr) == h)
                    return default;

                T? target = Unsafe.As<T?>(RuntimeImports.RhHandleGet(h)) ?? TryGetComTarget() as T;

                // We want to ensure that the handle was still alive when we fetched the target,
                // so we double check m_handle here. Note that the reading of the handle
                // value has to be volatile for this to work, but reading of m_handle does not.

                if (default(IntPtr) == m_handle)
                    return default;

                return target;
            }
        }

        /// <summary>
        /// This method checks whether the target to the weakreference is a native COMObject in which case the native object might still be alive although the RuntimeHandle could be null.
        /// Hence we check in the conditionalweaktable maintained by the System.Private.Interop.dll that maps weakreferenceInstance->nativeComObject to check whether the native COMObject is alive or not.
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
        /// This method notifies the System.Private.Interop.dll to update the conditionalweaktable for weakreferenceInstance->target in case the target is __ComObject. This ensures that we have a means to
        /// go from the managed weak reference to the actual native object even though the managed counterpart might have been collected.
        /// </summary>
        /// <param name="target"></param>
        private static void TrySetComTarget(object? target)
        {
#if ENABLE_WINRT
            WinRTInteropCallbacks callbacks = WinRTInterop.UnsafeCallbacks;
            if (callbacks != null)
                callbacks.SetCOMWeakReferenceTarget(this, target);
            else
            {
                Debug.Fail("WinRTInteropCallback is null");
            }
#endif // ENABLE_WINRT
        }

        // Free all system resources associated with this reference.
        //
        // Note: The WeakReference<T> finalizer is not usually run, but
        // treated specially in gc.cpp's ScanForFinalization
        // This is needed for subclasses deriving from WeakReference<T>, however.
        // Additionally, there may be some cases during shutdown when we run this finalizer.
        ~WeakReference()
        {
            IntPtr old_handle = m_handle;
            if (old_handle != default(IntPtr))
            {
                if (old_handle == Interlocked.CompareExchange(ref m_handle, default(IntPtr), old_handle))
                    ((GCHandle)old_handle).Free();
            }
        }

        private bool IsTrackResurrection() => m_trackResurrection;
    }
}
