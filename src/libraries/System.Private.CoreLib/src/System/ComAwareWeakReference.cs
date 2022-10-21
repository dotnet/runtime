// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
namespace System
{
    internal sealed partial class ComAwareWeakReference
    {
        private readonly nint _weakHandle;
        private ComInfo? _comInfo;

        private const nint TracksResurrectionBit = 1;
        private const nint ComAwareBit = 2;
        private const nint HandleTagBits = 3;

        internal sealed class ComInfo
        {
            internal readonly IntPtr _pComWeakRef;
            internal readonly long _wrapperId;

            internal object? ResolveTarget()
            {
                return ComWeakRefToObject(_pComWeakRef, _wrapperId);
            }

            internal static ComInfo? FromObject(object? target)
            {
                if (target == null)
                    return null;

                IntPtr pComWeakRef = ObjectToComWeakRef(target, out long wrapperId);
                if (pComWeakRef == 0)
                    return null;

                return new ComInfo(pComWeakRef, wrapperId);
            }

            private ComInfo(IntPtr pComWeakRef, long wrapperId)
            {
                Debug.Assert(pComWeakRef != IntPtr.Zero);

                _pComWeakRef = pComWeakRef;
                _wrapperId = wrapperId;
            }

            ~ComInfo()
            {
                Debug.Assert(_pComWeakRef != IntPtr.Zero);
                Marshal.Release(_pComWeakRef);
            }
        }

        internal nint WeakHandle => _weakHandle;

        internal ComAwareWeakReference(nint weakHandle)
        {
            _weakHandle = weakHandle;
        }

        internal void SetTarget(object? target, ComInfo? comInfo)
        {
            // NOTE: ComAwareWeakReference is an internal implementation detail and
            //       instances are never exposed publicly, thus we can use "this" for locking
            lock (this)
            {
                GCHandle.InternalSet(_weakHandle, target);
                _comInfo = comInfo;
            }
        }

        internal void UpdateComInfo(object? target, ComInfo? comInfo)
        {
            lock (this)
            {
                if (_comInfo != comInfo && GCHandle.InternalGet(_weakHandle) == target)
                {
                    _comInfo = comInfo;
                }
            }
        }

        public object? Target => GCHandle.InternalGet(_weakHandle) ?? _comInfo?.ResolveTarget();

        internal static ComAwareWeakReference EnsureComAwareReference(ref nint taggedHandle)
        {
            nint current = taggedHandle;
            if ((current & ComAwareBit) == 0)
            {
                ComAwareWeakReference newRef = new ComAwareWeakReference(taggedHandle & ~HandleTagBits);
                nint newHandle = (nint)GCHandle.InternalAlloc(newRef, GCHandleType.Normal);
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
        internal static ComAwareWeakReference? GetComAwareReference(nint taggedHandle)
        {
            return (taggedHandle & ComAwareBit) != 0 ?
                Unsafe.As<ComAwareWeakReference>(GCHandle.InternalGet(taggedHandle & ~HandleTagBits)) :
                null;
        }

        ~ComAwareWeakReference()
        {
            GCHandle.InternalFree(_weakHandle);
        }
    }
}
#endif
