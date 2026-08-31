// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Reflection.Runtime.TypeInfos;

using Internal.Metadata.NativeFormat;

namespace System.Reflection.Runtime.TypeInfos.NativeFormat
{
    internal sealed partial class NativeFormatRuntimeNamedTypeInfo : RuntimeNamedTypeInfo, IEquatable<NativeFormatRuntimeNamedTypeInfo>
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
            public UnificationKey(MetadataReader reader, TypeDefinitionHandle typeDefinitionHandle, RuntimeTypeHandle typeHandle)
            {
                Reader = reader;
                TypeDefinitionHandle = typeDefinitionHandle;
                TypeHandle = typeHandle;
            }

            public MetadataReader Reader { get; }
            public TypeDefinitionHandle TypeDefinitionHandle { get; }
            public RuntimeTypeHandle TypeHandle { get; }

            public override bool Equals(object obj)
            {
                if (!(obj is UnificationKey other))
                    return false;
                return Equals(other);
            }

            public bool Equals(UnificationKey other)
            {
                if (!TypeDefinitionHandle.Equals(other.TypeDefinitionHandle))
                    return false;

                if (!Reader.Equals(other.Reader))
                    return false;

                // The TypeHandle is not actually part of the key but riding along for convenience (see comment at head of class.)
                // If the other parts of the key matched, this must too.
                Debug.Assert(TypeHandle.Equals(other.TypeHandle));
                return true;
            }

            public override int GetHashCode()
            {
                return TypeDefinitionHandle.GetHashCode();
            }
        }
    }
}
