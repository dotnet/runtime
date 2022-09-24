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

        //Creates a new WeakReference that keeps track of target.
        private void Create(T target, bool trackResurrection)
        {
            IntPtr h = RuntimeImports.RhHandleAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            m_handleAndKind = trackResurrection ? h | 1 : h;

            if (target != null)
            {
                // Set the conditional weak table if the target is a __ComObject.
                TrySetComTarget(target);
            }
        }

        private IntPtr Handle => m_handleAndKind & ~1;

        public void SetTarget(T target)
        {
            IntPtr h = Handle;
            // Should only happen for corner cases, like using a WeakReference from a finalizer.
            // GC can finalize the instance if it becomes F-Reachable.
            // That, however, cannot happen while we use the instance.
            if (default(IntPtr) == h)
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

            // Update the conditionalweakTable in case the target is __ComObject.
            TrySetComTarget(target);

            RuntimeImports.RhHandleSet(h, target);

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
                T? target = Unsafe.As<T?>(RuntimeImports.RhHandleGet(h)) ?? TryGetComTarget() as T;

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return target;
            }
        }

        /// <summary>
        /// This method checks whether the target to the weakreference is a native COMObject in which case the native object might still be alive although the RuntimeHandle could be null.
        /// Hence we check in the conditionalweaktable maintained by the System.Private.Interop.dll that maps weakreferenceInstance->nativeComObject to check whether the native COMObject is alive or not.
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
        /// This method notifies the System.Private.Interop.dll to update the conditionalweaktable for weakreferenceInstance->target in case the target is __ComObject. This ensures that we have a means to
        /// go from the managed weak reference to the actual native object even though the managed counterpart might have been collected.
        /// </summary>
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

        // Note: While WeakReference<T> is a formally a finalizable type, the finalizer does not actually run.
        //       Instead the instances are treated specially in GC when scanning for no longer strongly-reachable
        //       finalizable objects.
#pragma warning disable CA1821 // Remove empty Finalizers
        ~WeakReference()
        {
            Debug.Assert(false, " WeakReference<T> finalizer should never run");
        }
#pragma warning restore CA1821 // Remove empty Finalizers

        private bool IsTrackResurrection() => (m_handleAndKind & 1) != 0;
    }
}
