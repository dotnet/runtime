// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

#pragma warning disable SA1121 // explicitly using type aliases instead of built-in types

#if FEATURE_COMINTEROP
namespace System.Runtime.InteropServices
{
    // UNDONE: will move the type definition to appropriate location before merging

    /// <summary>
    /// This class stores the weak references to the native COM Objects to ensure a way to map the weak
    /// reference to the native ComObject target and keep the mapping alive until the native object is alive
    /// allowing the connection to remain alive even though the managed wrapper might die.
    /// </summary>
    internal static class ComWeakReferenceHelpers
    {
        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId);

        [MethodImpl(MethodImplOptions.InternalCall)]
        internal static extern IntPtr ObjectToComWeakRef(object target, out long wrapperId);
    }
}

namespace System
{
    // UNDONE: will move the type definition to a separate file before merging

    internal sealed class ComAwareWeakReference
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
                return ComWeakReferenceHelpers.ComWeakRefToObject(_pComWeakRef, _wrapperId);
            }

            internal static ComInfo? FromObject(object? target)
            {
                if (target == null)
                    return null;

                IntPtr pComWeakRef = ComWeakReferenceHelpers.ObjectToComWeakRef(target, out long wrapperId);
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
#endif

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class WeakReference : ISerializable
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

        private nint _taggedHandle;

#if FEATURE_COMINTEROP
        // the lowermost 2 bits are reserved for storing additional info about the handle
        // we can use these bits because handle is at least 32bit aligned
        private const nint HandleTagBits = 3;
#else
        // the lowermost 1 bit is reserved for storing additional info about the handle
        private const nint HandleTagBits = 1;
#endif

        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private const nint TracksResurrectionBit = 1;

        // Creates a new WeakReference that keeps track of target.
        // Assumes a Short Weak Reference (ie TrackResurrection is false.)
        //
        public WeakReference(object? target)
            : this(target, false)
        {
        }

        public WeakReference(object? target, bool trackResurrection)
        {
            Create(target, trackResurrection);
        }

        protected WeakReference(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            object? target = info.GetValue("TrackedObject", typeof(object)); // Do not rename (binary serialization)
            bool trackResurrection = info.GetBoolean("TrackResurrection"); // Do not rename (binary serialization)

            Create(target, trackResurrection);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            info.AddValue("TrackedObject", Target, typeof(object)); // Do not rename (binary serialization)
            info.AddValue("TrackResurrection", IsTrackResurrection()); // Do not rename (binary serialization)
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        public virtual bool TrackResurrection => IsTrackResurrection();

        private void Create(object? target, bool trackResurrection)
        {
            nint h = (nint)GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            _taggedHandle = trackResurrection ?
                h | TracksResurrectionBit :
                h;

#if FEATURE_COMINTEROP
            ComAwareWeakReference.ComInfo? comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
            if (comInfo != null)
            {
                ComAwareWeakReference.EnsureComAwareReference(ref _taggedHandle).SetTarget(target, comInfo);
            }
#endif
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        private bool IsTrackResurrection() => (_taggedHandle & TracksResurrectionBit) != 0;

        internal nint WeakHandle
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                nint th = _taggedHandle;

#if FEATURE_COMINTEROP
                ComAwareWeakReference? cr = ComAwareWeakReference.GetComAwareReference(th);
                if (cr != null)
                {
                    return cr.WeakHandle;
                }
#endif
                return th & ~HandleTagBits;
            }
        }

        // Determines whether or not this instance of WeakReference still refers to an object
        // that has not been collected.
        public virtual bool IsAlive
        {
            get
            {
                nint wh = WeakHandle;

                // In determining whether it is valid to use this object, we need to at least expose this
                // without throwing an exception.
                if (wh == 0)
                    return false;

                bool result = GCHandle.InternalGet(wh) != null;

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
                nint wh = WeakHandle;
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
                if (wh == 0)
                    return default;

                object? target = GCHandle.InternalGet(wh);

#if FEATURE_COMINTEROP
                target ??= ComAwareWeakReference.GetComAwareReference(_taggedHandle)?.Target;
#endif
                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return target;
            }

            set
            {
                nint wh = WeakHandle;
                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // See the comment in the getter.
                if (wh == 0)
                    throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

                GCHandle.InternalSet(wh, value);

#if FEATURE_COMINTEROP
                ComAwareWeakReference.ComInfo? comInfo = ComAwareWeakReference.ComInfo.FromObject(value);
                ComAwareWeakReference? fr = comInfo == null ?
                    ComAwareWeakReference.GetComAwareReference(_taggedHandle) :
                    ComAwareWeakReference.EnsureComAwareReference(ref _taggedHandle);

                // Update the COM info to match the new target.
                // The target is alive while we update the COM info, since we have a strong reference here.
                fr?.UpdateComInfo(value, comInfo);
#endif

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);
            }
        }

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

            nint handle = _taggedHandle & ~HandleTagBits;
            if (handle != 0)
            {
                GCHandle.InternalFree(handle);

                // keep the bit that indicates whether this reference was tracking resurrection, clear the rest.
                _taggedHandle &= TracksResurrectionBit;
            }
        }
    }
}
