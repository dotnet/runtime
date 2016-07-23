// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
**
** Purpose: A wrapper for establishing a WeakReference to a generic type.
**
===========================================================*/
namespace System
{
    using System;
    using System.Runtime.Serialization;
    using System.Security;
    using System.Runtime;
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    [Serializable]
    // This class is sealed to mitigate security issues caused by Object::MemberwiseClone.
    public sealed class WeakReference<T> : ISerializable 
        where T : class
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

        // This field is not a regular GC handle. It can have a special values that are used to prevent a race condition between setting the target and finalization.
        internal IntPtr m_handle;

        // Creates a new WeakReference that keeps track of target.
        // Assumes a Short Weak Reference (ie TrackResurrection is false.)
        //
        public WeakReference(T target)
            : this(target, false)
        {
        }

        //Creates a new WeakReference that keeps track of target.
        //
        public WeakReference(T target, bool trackResurrection)
        {
            Create(target, trackResurrection);
        }

        internal WeakReference(SerializationInfo info, StreamingContext context)
        {
            if (info == null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            T target = (T)info.GetValue("TrackedObject", typeof(T));
            bool trackResurrection = info.GetBoolean("TrackResurrection");

            Create(target, trackResurrection);
        }

        //
        // We are exposing TryGetTarget instead of a simple getter to avoid a common problem where people write incorrect code like:
        //
        //      WeakReference ref = ...;
        //      if (ref.Target != null)
        //          DoSomething(ref.Target)
        //
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTarget(out T target)
        {
            // Call the worker method that has more performant but less user friendly signature.
            T o = this.Target;
            target = o;
            return o != null;
        }

        public void SetTarget(T target)
        {
            this.Target = target;
        }

        // This is property for better debugging experience (VS debugger shows values of properties when you hover over the variables)
        private extern T Target
        {
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            get;
            [MethodImplAttribute(MethodImplOptions.InternalCall)]
            [SecuritySafeCritical]
            set;
        }

        // Free all system resources associated with this reference.
        //
        // Note: The WeakReference<T> finalizer is not usually run, but
        // treated specially in gc.cpp's ScanForFinalization
        // This is needed for subclasses deriving from WeakReference<T>, however.
        // Additionally, there may be some cases during shutdown when we run this finalizer.
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecuritySafeCritical]
        extern ~WeakReference();

        [SecurityCritical]
        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null) {
                throw new ArgumentNullException("info");
            }
            Contract.EndContractBlock();

            info.AddValue("TrackedObject", this.Target, typeof(T));
            info.AddValue("TrackResurrection", IsTrackResurrection());
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecuritySafeCritical]
        private extern void Create(T target, bool trackResurrection);

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        [SecuritySafeCritical]
        private extern bool IsTrackResurrection();
    }
}
