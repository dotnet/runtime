// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class WeakReference : ISerializable
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

#if !CORECLR
        // the handle field is effectively readonly until the object is finalized.
        private IntPtr _handleAndKind;

        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private const nint TracksResurrectionBit = 1;
#endif

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

#if !CORECLR
        private void Create(object? target, bool trackResurrection)
        {
            IntPtr h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            _handleAndKind = trackResurrection ?
                h | TracksResurrectionBit :
                h;
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        private bool IsTrackResurrection() => (_handleAndKind & TracksResurrectionBit) != 0;

        internal IntPtr Handle => _handleAndKind & ~TracksResurrectionBit;

        // Determines whether or not this instance of WeakReference still refers to an object
        // that has not been collected.
        public virtual bool IsAlive
        {
            get
            {
                IntPtr h = Handle;

                // In determining whether it is valid to use this object, we need to at least expose this
                // without throwing an exception.
                if (default(IntPtr) == h)
                    return false;

                bool result = GCHandle.InternalGet(h) != null;

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

                object? target = GCHandle.InternalGet(h);

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

                GCHandle.InternalSet(h, value);

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

            IntPtr handle = Handle;
            if (handle != default(IntPtr))
            {
                GCHandle.InternalFree(handle);

                // keep the bit that indicates whether this reference was tracking resurrection
                _handleAndKind &= TracksResurrectionBit;
            }
        }
#endif
    }
}
