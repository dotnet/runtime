// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;

namespace System.ComponentModel
{
    /// <summary>
    /// This is a hashtable that stores object keys as weak references.
    /// It monitors memory usage and will periodically scavenge the
    /// hash table to clean out dead references.
    /// </summary>
    internal sealed class WeakHashtable : Hashtable
    {
        private static readonly IEqualityComparer s_comparer = new WeakKeyComparer();

        private long _lastGlobalMem;
        private int _lastHashCount;

        internal WeakHashtable() : base(s_comparer)
        {
        }

        /// <summary>
        /// Override of Item that wraps a weak reference around the
        /// key and performs a scavenge.
        /// </summary>
        public void SetWeak(object key, object value)
        {
            ScavengeKeys();
            this[new EqualityWeakReference(key)] = value;
        }

        /// <summary>
        /// This method checks to see if it is necessary to
        /// scavenge keys, and if it is it performs a scan
        /// of all keys to see which ones are no longer valid.
        /// To determine if we need to scavenge keys we need to
        /// try to track the current GC memory. Our rule of
        /// thumb is that if GC memory is decreasing and our
        /// key count is constant we need to scavenge. We
        /// will need to see if this is too often for extreme
        /// use cases like the CompactFramework (they add
        /// custom type data for every object at design time).
        /// </summary>
        private void ScavengeKeys()
        {
            int hashCount = Count;

            if (hashCount == 0)
            {
                return;
            }

            if (_lastHashCount == 0)
            {
                _lastHashCount = hashCount;
                return;
            }

            long globalMem = GC.GetTotalMemory(false);

            if (_lastGlobalMem == 0)
            {
                _lastGlobalMem = globalMem;
                return;
            }

            float memDelta = (globalMem - _lastGlobalMem) / (float)_lastGlobalMem;
            float hashDelta = (hashCount - _lastHashCount) / (float)_lastHashCount;

            if (memDelta < 0 && hashDelta >= 0)
            {
                // Perform a scavenge through our keys, looking
                // for dead references.
                List<object>? cleanupList = null;
                foreach (object o in Keys)
                {
                    if (o is WeakReference wr && !wr.IsAlive)
                    {
                        cleanupList ??= new List<object>();
                        cleanupList.Add(wr);
                    }
                }

                if (cleanupList != null)
                {
                    foreach (object o in cleanupList)
                    {
                        Remove(o);
                    }
                }
            }

            _lastGlobalMem = globalMem;
            _lastHashCount = hashCount;
        }

        private sealed class WeakKeyComparer : IEqualityComparer
        {
            bool IEqualityComparer.Equals(object? x, object? y)
            {
                if (x == null)
                {
                    return y == null;
                }
                if (y != null && x.GetHashCode() == y.GetHashCode())
                {
                    if (x is WeakReference wX)
                    {
                        if (!wX.IsAlive)
                        {
                            return false;
                        }
                        x = wX.Target;
                    }

                    if (y is WeakReference wY)
                    {
                        if (!wY.IsAlive)
                        {
                            return false;
                        }
                        y = wY.Target;
                    }

                    return object.ReferenceEquals(x, y);
                }

                return false;
            }

            int IEqualityComparer.GetHashCode(object obj) => obj.GetHashCode();
        }

        /// <summary>
        /// A subclass of WeakReference that overrides GetHashCode and
        /// Equals so that the weak reference returns the same equality
        /// semantics as the object it wraps. This will always return
        /// the object's hash code and will return True for a Equals
        /// comparison of the object it is wrapping. If the object
        /// it is wrapping has finalized, Equals always returns false.
        /// </summary>
        private sealed class EqualityWeakReference : WeakReference
        {
            private readonly int _hashCode;

            internal EqualityWeakReference(object o) : base(o)
            {
                _hashCode = o.GetHashCode();
            }

            public override bool Equals(object? o)
            {
                if (o?.GetHashCode() != _hashCode)
                {
                    return false;
                }

                if (o == this || (IsAlive && ReferenceEquals(o, Target)))
                {
                    return true;
                }

                return false;
            }

            public override int GetHashCode() => _hashCode;
        }
    }
}
