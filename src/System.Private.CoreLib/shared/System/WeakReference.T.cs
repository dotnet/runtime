// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;
using System.Runtime.CompilerServices;
using System.Diagnostics.CodeAnalysis;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")] 
    // This class is sealed to mitigate security issues caused by Object::MemberwiseClone.
    public sealed partial class WeakReference<T> : ISerializable
        where T : class?
    {
        // If you fix bugs here, please fix them in WeakReference at the same time.

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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

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
        [MethodImplAttribute(MethodImplOptions.AggressiveInlining)]
        public bool TryGetTarget([MaybeNullWhen(false), NotNullWhen(true)] out T target)
        {
            // Call the worker method that has more performant but less user friendly signature.
            T o = this.Target;
            target = o;
            return o != null;
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            info.AddValue("TrackedObject", this.Target, typeof(T)); // Do not rename (binary serialization)
            info.AddValue("TrackResurrection", IsTrackResurrection()); // Do not rename (binary serialization)
        }
    }
}
