// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Debug = System.Diagnostics.Debug;
using System;

namespace Internal.TypeSystem
{
    public class TypeIdentifierData : IEquatable<TypeIdentifierData>
    {
        public TypeIdentifierData(string scope, string name)
        {
            Debug.Assert(scope != null);
            Debug.Assert(name != null);
            Scope = scope;
            Name = name;
        }

        public string Scope { get; }
        public string Name { get; }

        public bool Equals(TypeIdentifierData other)
        {
            if (Scope != other.Scope)
                return false;
            return Name == other.Name;
        }

        public override int GetHashCode()
        {
            return Scope.GetHashCode() ^ Name.GetHashCode();
        }

        public override bool Equals(object o)
        {
            if (o is TypeIdentifierData other)
                return Equals(other);
            return false;
        }
    }

    public partial class TypeDesc
    {
        public virtual TypeIdentifierData TypeIdentifierData => null;

        public bool IsTypeDefEquivalent => TypeIdentifierData != null;
    }
}
