// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

using static System.WeakReferenceHandleTags;

namespace System
{
    internal static class WeakReferenceHandleTags
    {
        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        // handles are at least 2-byte aligned, so we can use one bit for tagging
        internal const nint TracksResurrectionBit = 1;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
        // one more bit is used to track whether the handle refers to an instance of ComAwareWeakReference
        // we can use this bit because on COM-supporting platforms a handle is at least 4-byte aligned
        internal const nint ComAwareBit = 2;
        internal const nint HandleTagBits = TracksResurrectionBit | ComAwareBit;
#else
        internal const nint HandleTagBits = TracksResurrectionBit;
#endif
    }

    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class WeakReference : ISerializable
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

        private nint _taggedHandle;

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

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        protected WeakReference(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            object? target = info.GetValue("TrackedObject", typeof(object)); // Do not rename (binary serialization)
            bool trackResurrection = info.GetBoolean("TrackResurrection"); // Do not rename (binary serialization)

            Create(target, trackResurrection);
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
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
            nint h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            _taggedHandle = trackResurrection ?
                h | TracksResurrectionBit :
                h;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
            ComAwareWeakReference.ComInfo? comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
            if (comInfo != null)
            {
                ComAwareWeakReference.SetComInfoInConstructor(ref _taggedHandle, comInfo);
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

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
                if ((th & ComAwareBit) != 0)
                    return ComAwareWeakReference.GetWeakHandle(th);
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
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                nint th = _taggedHandle & ~TracksResurrectionBit;

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
                if (th == 0)
                    return default;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
                if ((th & ComAwareBit) != 0)
                    return ComAwareWeakReference.GetTarget(th);
#endif

                // unsafe cast is ok as the handle cannot be destroyed and recycled while we keep the instance alive
                object? target = GCHandle.InternalGet(th);

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

                return target;
            }

            set
            {
                nint th = _taggedHandle & ~TracksResurrectionBit;

                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // GC can finalize the instance if it becomes F-Reachable.
                // That, however, cannot happen while we use the instance.
                if (th == 0)
                    throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
                var comInfo = ComAwareWeakReference.ComInfo.FromObject(value);
                if ((th & ComAwareBit) != 0 || comInfo != null)
                {
                    ComAwareWeakReference.SetTarget(ref _taggedHandle, value, comInfo);
                    return;
                }
#endif

                GCHandle.InternalSet(th, value);

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
