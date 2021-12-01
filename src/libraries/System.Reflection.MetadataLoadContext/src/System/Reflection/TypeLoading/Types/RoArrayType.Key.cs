// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System.Reflection.TypeLoading
{
    internal sealed partial class RoArrayType : RoHasElementType
    {
        //
        // Multidimensional is implied here (even for rank 1.) SzArrays live in their own unification table.
        //
        public readonly struct Key : IEquatable<Key>
        {
            public Key(RoType elementType, int rank)
            {
                Debug.Assert(elementType != null);

                ElementType = elementType;
                Rank = rank;
            }

            public RoType ElementType { get; }
            public int Rank { get; }

            public bool Equals(Key other)
            {
                if (ElementType != other.ElementType)
                    return false;
                if (Rank != other.Rank)
                    return false;
                return true;
            }

            public override bool Equals([NotNullWhen(true)] object? obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => ElementType.GetHashCode() ^ Rank.GetHashCode();
        }
    }
}
