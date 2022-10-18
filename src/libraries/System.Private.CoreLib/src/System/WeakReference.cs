// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;

#if CORECLR
namespace System.Runtime.InteropServices
{
    /// <summary>
    /// This class stores the weak references to the native COM Objects to ensure a way to map the weak
    /// reference to the native ComObject target and keep the mapping alive until the native object is alive
    /// allowing the connection to remain alive even though the managed wrapper might die.
    /// </summary>
    internal static class ComWeakReferenceHelpers
    {
        // Holds the mapping from the weak reference to the COMWeakReference which is a thin wrapper for native WeakReference.
        private static readonly ConditionalWeakTable<object, ComWeakReference> s_ComWeakReferenceTable = new ConditionalWeakTable<object, ComWeakReference>();

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern object? ComWeakRefToObject(IntPtr pComWeakRef, long wrapperId);

        [MethodImpl(MethodImplOptions.InternalCall)]
        private static extern IntPtr ObjectToComWeakRef(object target, out long wrapperId);

        /// <summary>
        /// This class is a thin wrapper that holds the native IWeakReference and wrapperId.
        /// </summary>
        internal sealed class ComWeakReference
        {
            internal readonly IntPtr _pComWeakRef;
            internal readonly long   _wrapperId;

            internal object? Resolve()
            {
                return ComWeakRefToObject(_pComWeakRef, _wrapperId);
            }

            internal static ComWeakReference? FromObject(object target)
            {
                IntPtr pComWeakRef = ObjectToComWeakRef(target, out long wrapperId);
                if (pComWeakRef == 0)
                    return null;

                return new ComWeakReference(pComWeakRef, wrapperId);
            }

            internal ComWeakReference(IntPtr pComWeakRef, long wrapperId)
            {
                Debug.Assert(pComWeakRef != IntPtr.Zero);

                _pComWeakRef = pComWeakRef;
                _wrapperId = wrapperId;
            }

            ~ComWeakReference()
            {
                Debug.Assert(_pComWeakRef != 0);
                Marshal.Release(_pComWeakRef);
            }
        }

        // if object is an IWeakReference, associates the managed weak reference with the COM weak reference
        public static bool SetComTarget(object weakReference, object? target)
        {
            Debug.Assert(weakReference != null);

            // Check if this weakReference is already associated with a native target.
            if (s_ComWeakReferenceTable.TryGetValue(weakReference, out _))
            {
                // Remove the previous target.
                // We do not have to release the native ComWeakReference since it will be done as part of the finalizer.
                s_ComWeakReferenceTable.Remove(weakReference);
            }

            if (target == null)
                return false;

            ComWeakReference? comWeakRef = ComWeakReference.FromObject(target);
            if (comWeakRef == null)
                return false;

            // Since we have already checked s_COMWeakReferenceTable for the weak reference, we can simply add the entry w/o checking.
            s_ComWeakReferenceTable.Add(weakReference, comWeakRef);
            return true;
        }

        // if the weak reference is associated with a COM weak reference, retrieve the target from the COM weak reference
        public static object? GetComTarget(object weakReference)
        {
            Debug.Assert(weakReference != null);

            ComWeakReference? comWeakRef;
            if (s_ComWeakReferenceTable.TryGetValue(weakReference, out comWeakRef))
            {
                return comWeakRef.Resolve();
            }

            return null;
        }
    }
}

#endif

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

        private IntPtr _handleAndKind;

        // the lowermost 3 bits are reserved for storing additional info about the handle
        // we can use these bits because handle is at least 32bit aligned
        private const nint HandleTagBits = 7;

        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private const nint TracksResurrectionBit = 1;

#if CORECLR
        // the next bit is used to indicate whether there is an associated com weak reference
        private const nint IsComWeakReferenceBit = 2;

        private void TrySetComTarget(object? target)
        {
            if (target != null)
            {
                if (ComWeakReferenceHelpers.SetComTarget(this, target))
                {
                    _handleAndKind |= IsComWeakReferenceBit;
                }
            }
        }

        private object? TryGetComTarget()
        {
            if ((_handleAndKind & IsComWeakReferenceBit) != 0)
            {
                return ComWeakReferenceHelpers.GetComTarget(this);
            }

            return null;
        }
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

        private void Create(object? target, bool trackResurrection)
        {
            IntPtr h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            _handleAndKind = trackResurrection ?
                h | TracksResurrectionBit :
                h;

#if CORECLR
            TrySetComTarget(target);
#endif
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        private bool IsTrackResurrection() => (_handleAndKind & TracksResurrectionBit) != 0;

        internal IntPtr Handle => _handleAndKind & ~HandleTagBits;

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

#if CORECLR
                target ??= TryGetComTarget();
#endif
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

#if CORECLR
                TrySetComTarget(value);
#endif
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
    }
}
