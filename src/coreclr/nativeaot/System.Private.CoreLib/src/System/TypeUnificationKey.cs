// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

//
// Implements System.Type
//

using System;
using System.Diagnostics;

namespace System
{
    //
    // Type doesn't implement IEquatable<Type> which makes it impossible to use a key in our unification tables.
    // This wrapper is here to compensate for that.
    //
    internal struct TypeUnificationKey : IEquatable<TypeUnificationKey>
    {
        public TypeUnificationKey(Type type)
        {
            Debug.Assert(type != null);
            Type = type;
        }

        public override bool Equals(object? obj)
        {
            if (!(obj is TypeUnificationKey))
                return false;
            return Equals((TypeUnificationKey)obj);
        }

        public bool Equals(TypeUnificationKey other)
        {
            return Type.Equals(other.Type);
        }

        public override int GetHashCode()
        {
            return Type.GetHashCode();
        }

        public Type Type { get; }
    }
}
