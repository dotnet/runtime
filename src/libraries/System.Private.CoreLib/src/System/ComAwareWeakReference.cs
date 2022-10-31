// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS

using static System.WeakReferenceHandleTags;

namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        // _weakHandle is effectively readonly.
        // the only place where we change it after construction is in finalizer.
        private nint _weakHandle;
        private ComInfo? _comInfo;

        internal sealed class ComInfo
        {
            // _pComWeakRef is effectively readonly.
            // the only place where we change it after construction is in finalizer.
            private IntPtr _pComWeakRef;
            private readonly long _wrapperId;

            internal object? ResolveTarget()
            {
                return ComWeakRefToObject(_pComWeakRef, _wrapperId);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            internal static ComInfo? FromObject(object? target)
            {
                if (target == null || !PossiblyComObject(target))
                    return null;

                return FromObjectSlow(target);
            }

            [MethodImpl(MethodImplOptions.NoInlining)]
            private static unsafe ComInfo? FromObjectSlow(object target)
            {
                IntPtr pComWeakRef = ObjectToComWeakRef(target, out long wrapperId);
                if (pComWeakRef == 0)
                    return null;

                try
                {
                    return new ComInfo(pComWeakRef, wrapperId);
                }
                catch (OutOfMemoryException)
                {
                    // we did not create an object, so ComInfo finalizer will not run
                    Marshal.Release(pComWeakRef);
                    throw;
                }
            }

            private ComInfo(IntPtr pComWeakRef, long wrapperId)
            {
                Debug.Assert(pComWeakRef != IntPtr.Zero);
                _pComWeakRef = pComWeakRef;
                _wrapperId = wrapperId;
            }

            ~ComInfo()
            {
                Marshal.Release(_pComWeakRef);
                // our use pattern guarantees that the instance is not reachable after this.
                // clear the pointer just in case that gets somehow broken.
                _pComWeakRef = 0;
            }
        }

        private ComAwareWeakReference(nint weakHandle)
        {
            Debug.Assert(weakHandle != 0);
            _weakHandle = weakHandle;
        }

        ~ComAwareWeakReference()
        {
            GCHandle.InternalFree(_weakHandle);
            // our use pattern guarantees that the instance is not reachable after this.
            // clear the pointer just in case that gets somehow broken.
            _weakHandle = 0;
        }

        private void SetTarget(object? target, ComInfo? comInfo)
        {
            // NOTE: ComAwareWeakReference is an internal implementation detail and
            //       instances are never exposed publicly, thus we can use "this" for locking
            lock (this)
            {
                GCHandle.InternalSet(_weakHandle, target);
                _comInfo = comInfo;
            }
        }

        internal object? Target => GCHandle.InternalGet(_weakHandle) ?? RehydrateTarget();

        private object? RehydrateTarget()
        {
            object? target = null;
            lock (this)
            {
                if (_comInfo != null)
                {
                    // check if the target is still null
                    target = GCHandle.InternalGet(_weakHandle);
                    if (target == null)
                    {
                        // resolve and reset
                        target = _comInfo.ResolveTarget();
                        if (target != null)
                            GCHandle.InternalSet(_weakHandle, target);
                    }
                }
            }

            return target;
        }

        private static ComAwareWeakReference EnsureComAwareReference(ref nint taggedHandle)
        {
            nint current = taggedHandle;
            if ((current & ComAwareBit) == 0)
            {
                ComAwareWeakReference newRef = new ComAwareWeakReference(taggedHandle & ~HandleTagBits);
                nint newHandle = GCHandle.InternalAlloc(newRef, GCHandleType.Normal);
                nint newTaggedHandle = newHandle | ComAwareBit | (taggedHandle & TracksResurrectionBit);
                if (Interlocked.CompareExchange(ref taggedHandle, newTaggedHandle, current) == current)
                {
                    // success.
                    return newRef;
                }

                // someone beat us to it. (this is rare)
                GCHandle.InternalFree(newHandle);
                GC.SuppressFinalize(newRef);
            }

            return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~HandleTagBits));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static object? GetTarget(nint taggedHandle)
        {
            Debug.Assert((taggedHandle & ComAwareBit) != 0);
            return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~HandleTagBits)).Target;
        }

        internal static nint GetWeakHandle(nint taggedHandle)
        {
            Debug.Assert((taggedHandle & ComAwareBit) != 0);
            return Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~HandleTagBits))._weakHandle;
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetTarget(ref nint taggedHandle, object? target, ComInfo? comInfo)
        {
            ComAwareWeakReference comAwareRef = comInfo != null ?
                EnsureComAwareReference(ref taggedHandle) :
                Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~HandleTagBits));

            comAwareRef.SetTarget(target, comInfo);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void SetComInfoInConstructor(ref nint taggedHandle, ComInfo comInfo)
        {
            Debug.Assert((taggedHandle & ComAwareBit) == 0);
            ComAwareWeakReference comAwareRef = new ComAwareWeakReference(taggedHandle & ~HandleTagBits);
            nint newHandle = GCHandle.InternalAlloc(comAwareRef, GCHandleType.Normal);
            taggedHandle = newHandle | ComAwareBit | (taggedHandle & TracksResurrectionBit);
            comAwareRef._comInfo = comInfo;
        }
    }
}
#endif
