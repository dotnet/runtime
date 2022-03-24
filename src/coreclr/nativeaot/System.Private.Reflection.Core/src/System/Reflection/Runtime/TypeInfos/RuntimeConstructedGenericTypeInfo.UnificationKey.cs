// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Generic;
using System.Collections.Concurrent;

namespace System.Reflection.Runtime.TypeInfos
{
    internal sealed partial class RuntimeConstructedGenericTypeInfo : RuntimeTypeInfo, IKeyedItem<RuntimeConstructedGenericTypeInfo.UnificationKey>
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
            public UnificationKey(RuntimeTypeInfo genericTypeDefinition, RuntimeTypeInfo[] genericTypeArguments, RuntimeTypeHandle typeHandle)
            {
                GenericTypeDefinition = genericTypeDefinition;
                GenericTypeArguments = genericTypeArguments;
                TypeHandle = typeHandle;
            }

            public RuntimeTypeInfo GenericTypeDefinition { get; }
            public RuntimeTypeInfo[] GenericTypeArguments { get; }
            public RuntimeTypeHandle TypeHandle { get; }

            public override bool Equals(object obj)
            {
                if (!(obj is UnificationKey other))
                    return false;
                return Equals(other);
            }

            public bool Equals(UnificationKey other)
            {
                if (!GenericTypeDefinition.Equals(other.GenericTypeDefinition))
                    return false;
                if (GenericTypeArguments.Length != other.GenericTypeArguments.Length)
                    return false;
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    if (!(GenericTypeArguments[i].Equals(other.GenericTypeArguments[i])))
                        return false;
                }

                // The TypeHandle is not actually part of the key but riding along for convenience (see commment at head of class.)
                // If the other parts of the key matched, this must too.
                Debug.Assert(TypeHandle.Equals(other.TypeHandle));
                return true;
            }

            public override int GetHashCode()
            {
                int hashCode = GenericTypeDefinition.GetHashCode();
                for (int i = 0; i < GenericTypeArguments.Length; i++)
                {
                    hashCode ^= GenericTypeArguments[i].GetHashCode();
                }
                return hashCode;
            }
        }
    }
}
