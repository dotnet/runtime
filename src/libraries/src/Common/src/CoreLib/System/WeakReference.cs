// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.Serialization;

namespace System
{
    [Serializable]
    [System.Runtime.CompilerServices.TypeForwardedFrom("mscorlib, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    public partial class WeakReference : ISerializable
    {
        // If you fix bugs here, please fix them in WeakReference<T> at the same time.

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
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }

            object? target = info.GetValue("TrackedObject", typeof(object)); // Do not rename (binary serialization)
            bool trackResurrection = info.GetBoolean("TrackResurrection"); // Do not rename (binary serialization)

            Create(target, trackResurrection);
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            if (info == null)
            {
                throw new ArgumentNullException(nameof(info));
            }
            info.AddValue("TrackedObject", Target, typeof(object)); // Do not rename (binary serialization)
            info.AddValue("TrackResurrection", IsTrackResurrection()); // Do not rename (binary serialization)
        }
    }
}
