// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;
using System.Threading;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    // This class is sealed to mitigate security issues caused by Object::MemberwiseClone.
    public sealed partial class WeakReference<T> : ISerializable
        where T : class?
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

        // Most methods using the handle should use GC.KeepAlive(this) to avoid potential handle recycling
        // attacks (i.e. if the WeakReference instance is finalized away underneath you when you're still
        // handling a cached value of the handle then the handle could be freed and reused).

        private nint _handleAndKind;

        // the lowermost 3 bits are reserved for storing additional info about the handle
        // we can use these bits because handle is at least 32bit aligned
        private const nint HandleTagBits = 7;

        // the lowermost bit is used to indicate whether the handle is tracking resurrection
        private const nint TracksResurrectionBit = 1;

#if CORECLR
        // the next bit is used to indicate whether there is an associated COM weak reference
        private const nint HasComWeakReferenceBit = 2;
        private bool HasComWeakReference() => (_handleAndKind & HasComWeakReferenceBit) != 0;

        // one more bit to coordinate exclusive Set operations
        // because of COM interop a Set may change two values, so we must ensure two Sets are not running concurrently
        private const nint ExclusiveSetAccessBit = 4;

        private void TrySetComTarget(object? target)
        {
            if (target != null || HasComWeakReference())
            {
                if (ComWeakReferenceHelpers.SetComTarget(this, target, HasComWeakReference()))
                {
                    _handleAndKind |= HasComWeakReferenceBit;
                }
                else
                {
                    _handleAndKind &= ~HasComWeakReferenceBit;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private object? TryGetComTarget()
        {
            if (HasComWeakReference())
            {
                return ComWeakReferenceHelpers.GetComTarget(this);
            }

            return null;
        }
#endif

        // Creates a new WeakReference that keeps track of target.
        // Assumes a Short Weak Reference (ie TrackResurrection is false.)
        public WeakReference(T target)
            : this(target, false)
        {
        }

        // Creates a new WeakReference that keeps track of target.
        //
        public WeakReference(T target, bool trackResurrection)
        {
            Create(target, trackResurrection);
        }

        private WeakReference(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            T target = (T)info.GetValue("TrackedObject", typeof(T))!; // Do not rename (binary serialization)
            bool trackResurrection = info.GetBoolean("TrackResurrection"); // Do not rename (binary serialization)

            Create(target, trackResurrection);
        }

        //
        // We are exposing TryGetTarget instead of a simple getter to avoid a common problem where people write incorrect code like:
        //
        //      WeakReference ref = ...;
        //      if (ref.Target != null)
        //          DoSomething(ref.Target)
        //
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTarget([MaybeNullWhen(false), NotNullWhen(true)] out T target)
        {
            // Call the worker method that has more performant but less user friendly signature.
            T? o = this.Target;

#if CORECLR
            o ??= Unsafe.As<T?>(TryGetComTarget());
#endif

            target = o!;
            return o != null;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            info.AddValue("TrackedObject", this.Target, typeof(T)); // Do not rename (binary serialization)
            info.AddValue("TrackResurrection", IsTrackResurrection()); // Do not rename (binary serialization)
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        private bool IsTrackResurrection() => (_handleAndKind & TracksResurrectionBit) != 0;

        // Creates a new WeakReference that keeps track of target.
        private void Create(T target, bool trackResurrection)
        {
            nint h = GCHandle.InternalAlloc(target, trackResurrection ? GCHandleType.WeakTrackResurrection : GCHandleType.Weak);
            _handleAndKind = trackResurrection ?
                h | TracksResurrectionBit :
                h;

#if CORECLR
            TrySetComTarget(target);
#endif
        }

        private nint Handle => _handleAndKind & ~HandleTagBits;

        public void SetTarget(T target)
        {
#if CORECLR
            // simple spinlock to ensure that two Sets are not runing concurrently
            SpinWait sw = default;
            nint hk = _handleAndKind;
            while ((hk & ExclusiveSetAccessBit) != 0 ||
                    Interlocked.CompareExchange(ref _handleAndKind, hk | ExclusiveSetAccessBit, hk) != hk)
            {
                sw.SpinOnce();
                hk = _handleAndKind;
            }
#endif

            nint h = Handle;
            // Should only happen for corner cases, like using a WeakReference from a finalizer.
            // GC can finalize the instance if it becomes F-Reachable.
            // That, however, cannot happen while we use the instance.
            if (h == 0)
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

            GCHandle.InternalSet(h, target);

            // must keep the instance alive as long as we use the handle.
            GC.KeepAlive(this);

#if CORECLR
            TrySetComTarget(target);
            // release the lock
            Volatile.Write(ref _handleAndKind, _handleAndKind & ~ExclusiveSetAccessBit);
#endif
        }

        private T? Target
        {
            get
            {
                nint h = Handle;
                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // GC can finalize the instance if it becomes F-Reachable.
                // That, however, cannot happen while we use the instance.
                if (h == 0)
                    return default;

                // unsafe cast is ok as the handle cannot be destroyed and recycled while we keep the instance alive
                T? target = Unsafe.As<T?>(GCHandle.InternalGet(h));

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

#if CORECLR
                target ??= Unsafe.As<T?>(TryGetComTarget());
#endif

                return target;
            }
        }

        // Note: While WeakReference<T> is formally a finalizable type, the finalizer does not actually run.
        //       Instead the instances are treated specially in GC when scanning for no longer strongly-reachable
        //       finalizable objects.
#pragma warning disable CA1821 // Remove empty Finalizers
        ~WeakReference()
        {
            Debug.Assert(false, " WeakReference<T> finalizer should never run");
        }
#pragma warning restore CA1821 // Remove empty Finalizers

    }
}
