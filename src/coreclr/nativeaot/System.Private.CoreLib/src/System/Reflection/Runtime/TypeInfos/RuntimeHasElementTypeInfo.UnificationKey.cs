// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace System.Reflection.Runtime.TypeInfos
{
    internal abstract partial class RuntimeHasElementTypeInfo : RuntimeTypeInfo
    {
        //
        // Key for unification.
        //
        internal struct UnificationKey : IEquatable<UnificationKey>
        {
            //
            // Q: Why is the type handle part of the unification key when it doesn't participate in the Equals/HashCode computations?
            // A: It's a passenger.
            //
            //    The typeHandle argument is "redundant" in that it can be computed from the rest of the key. However, we have callers (Type.GetTypeFromHandle()) that
            //    already have the typeHandle so to avoid an unnecessary round-trip computation, we require the caller to pass it in separately.
            //    We allow it to ride along in the key object because the ConcurrentUnifier classes we use don't support passing "extra" parameters to
            //    their Factory methods.
            //
            public UnificationKey(RuntimeTypeInfo elementType, RuntimeTypeHandle typeHandle)
            {
                ElementType = elementType;
                TypeHandle = typeHandle;
            }

            public RuntimeTypeInfo ElementType { get; }
            public RuntimeTypeHandle TypeHandle { get; }

            public override bool Equals(object obj)
            {
                if (!(obj is UnificationKey other))
                    return false;
                return Equals(other);
            }

            public bool Equals(UnificationKey other)
            {
                if (!ElementType.Equals(other.ElementType))
                    return false;

                // The TypeHandle is not actually part of the key but riding along for convenience (see comment at head of class.)
                // If the other parts of the key matched, this must too.
                Debug.Assert(TypeHandle.Equals(other.TypeHandle));
                return true;
            }

            public override int GetHashCode()
            {
                return ElementType.GetHashCode();
            }
        }
    }
}
