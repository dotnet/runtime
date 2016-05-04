// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System
{
    /// <summary>Provides an object wrapper that can transition between strong and weak references to the object.</summary>
    internal sealed class StrongToWeakReference<T> : WeakReference where T : class
    {
        private T _strongRef;

        /// <summary>Initializes the instance with a strong reference to the specified object.</summary>
        /// <param name="obj">The object to wrap.</param>
        public StrongToWeakReference(T obj) : base(obj)
        {
            Debug.Assert(obj != null, "Expected non-null obj");
            _strongRef = obj;
        }

        /// <summary>Drops the strong reference to the object, keeping only a weak reference.</summary>
        public void MakeWeak() => _strongRef = null;

        /// <summary>Restores the strong reference, assuming the object hasn't yet been collected.</summary>
        public void MakeStrong()
        {
            _strongRef = WeakTarget;
            Debug.Assert(_strongRef != null, $"Expected non-null {nameof(_strongRef)} after setting");
        }

        /// <summary>Gets the wrapped object.</summary>
        public new T Target => _strongRef ?? WeakTarget;

        /// <summary>Gets the wrapped object via its weak reference.</summary>
        private T WeakTarget => base.Target as T;
    }
}
