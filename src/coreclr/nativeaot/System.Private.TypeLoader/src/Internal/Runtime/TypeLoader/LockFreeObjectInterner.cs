// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

namespace Internal.TypeSystem
{
    public class LockFreeObjectInterner : LockFreeReaderHashtableOfPointers<object, GCHandle>
    {
        private static LockFreeObjectInterner s_interner = new LockFreeObjectInterner();
        public static GCHandle GetInternedObjectHandle(object obj)
        {
            return s_interner.GetOrCreateValue(obj);
        }

        /// <summary>
        /// Given a key, compute a hash code. This function must be thread safe.
        /// </summary>
        protected override int GetKeyHashCode(object key)
        {
            return key.GetHashCode();
        }

        /// <summary>
        /// Given a value, compute a hash code which would be identical to the hash code
        /// for a key which should look up this value. This function must be thread safe.
        /// </summary>
        protected override int GetValueHashCode(GCHandle value)
        {
            return value.Target.GetHashCode();
        }

        /// <summary>
        /// Compare a key and value. If the key refers to this value, return true.
        /// This function must be thread safe.
        /// </summary>
        protected override bool CompareKeyToValue(object key, GCHandle value)
        {
            return key == value.Target;
        }

        /// <summary>
        /// Compare a value with another value. Return true if values are equal.
        /// This function must be thread safe.
        /// </summary>
        protected override bool CompareValueToValue(GCHandle value1, GCHandle value2)
        {
            return value1.Target == value2.Target;
        }

        /// <summary>
        /// Create a new value from a key. Must be threadsafe. Value may or may not be added
        /// to collection. Return value must not be null.
        /// </summary>
        protected override GCHandle CreateValueFromKey(object key)
        {
            return GCHandle.Alloc(key);
        }

        /// <summary>
        /// Convert a value to an IntPtr for storage into the hashtable
        /// </summary>
        protected override IntPtr ConvertValueToIntPtr(GCHandle value)
        {
            return GCHandle.ToIntPtr(value);
        }

        /// <summary>
        /// Convert an IntPtr into a value for comparisons, or for returning.
        /// </summary>
        protected override GCHandle ConvertIntPtrToValue(IntPtr pointer)
        {
            return GCHandle.FromIntPtr(pointer);
        }
    }
}
