// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Diagnostics.CodeAnalysis;
using System.Diagnostics;

using static System.WeakReferenceHandleTags;

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
        private nint _taggedHandle;

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
            T? o = this.Target;
            target = o!;
            return o != null;
        }

        [Obsolete(Obsoletions.LegacyFormatterImplMessage, DiagnosticId = Obsoletions.LegacyFormatterImplDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            ArgumentNullException.ThrowIfNull(info);

            info.AddValue("TrackedObject", this.Target, typeof(T)); // Do not rename (binary serialization)
            info.AddValue("TrackResurrection", IsTrackResurrection()); // Do not rename (binary serialization)
        }

        // Returns a boolean indicating whether or not we're tracking objects until they're collected (true)
        // or just until they're finalized (false).
        private bool IsTrackResurrection() => (_taggedHandle & TracksResurrectionBit) != 0;

        // Creates a new WeakReference that keeps track of target.
        private void Create(T target, bool trackResurrection)
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

        public void SetTarget(T target)
        {
            nint th = _taggedHandle & ~TracksResurrectionBit;

            // Should only happen for corner cases, like using a WeakReference from a finalizer.
            // GC can finalize the instance if it becomes F-Reachable.
            // That, however, cannot happen while we use the instance.
            if (th == 0)
                throw new InvalidOperationException(SR.InvalidOperation_HandleIsNotInitialized);

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
            var comInfo = ComAwareWeakReference.ComInfo.FromObject(target);
            if ((th & ComAwareBit) != 0 || comInfo != null)
            {
                ComAwareWeakReference.SetTarget(ref _taggedHandle, target, comInfo);
                return;
            }
#endif

            GCHandle.InternalSet(th, target);

            // must keep the instance alive as long as we use the handle.
            GC.KeepAlive(this);
        }

        private T? Target
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get
            {
                nint th = _taggedHandle & ~TracksResurrectionBit;

                // Should only happen for corner cases, like using a WeakReference from a finalizer.
                // GC can finalize the instance if it becomes F-Reachable.
                // That, however, cannot happen while we use the instance.
                if (th == 0)
                    return default;

#if FEATURE_COMINTEROP || FEATURE_COMWRAPPERS
                if ((th & ComAwareBit) != 0)
                    return Unsafe.As<T?>(ComAwareWeakReference.GetTarget(th));
#endif

                // unsafe cast is ok as the handle cannot be destroyed and recycled while we keep the instance alive
                T? target = Unsafe.As<T?>(GCHandle.InternalGet(th));

                // must keep the instance alive as long as we use the handle.
                GC.KeepAlive(this);

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
