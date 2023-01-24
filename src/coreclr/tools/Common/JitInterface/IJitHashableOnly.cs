// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using Internal.TypeSystem;

namespace Internal.JitInterface
{
    public class JitObjectComparer : IEqualityComparer<object>
    {
        public new bool Equals(object x, object y) => x == y;
        public int GetHashCode(object obj)
        {
            if (obj is IJitHashableOnly jitHashable)
                return jitHashable.GetJitVisibleHashCode();
            return obj.GetHashCode();
        }
    }

    public class JitObjectComparer<T> : IEqualityComparer<T> where T:class
    {
        public bool Equals(T x, T y) => x == y;
        public int GetHashCode(T obj)
        {
            if (obj is IJitHashableOnly jitHashable)
                return jitHashable.GetJitVisibleHashCode();
            return obj.GetHashCode();
        }
    }

    // Mark a type system object with this interface to indicate that it
    // can be hashed, but only by using the IJitHashableOnly interface.
    // Implementors of this should throw an exception in their implementation of
    // ComputeHashCode so that the normal GetHashCode function does not work.
    // This is used to prevent putting these objects into long lived storage.
    //
    // The goal here is to make it difficult to accidentally store a type into
    // another hashtable that isn't associated with the JIT itself, but still
    // allow the jit side code use standard collections.
    public interface IJitHashableOnly
    {
        int GetJitVisibleHashCode();
    }
}
