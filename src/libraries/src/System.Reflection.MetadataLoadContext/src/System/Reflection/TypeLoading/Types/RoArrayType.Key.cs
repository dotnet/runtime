// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

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

            public override bool Equals(object obj) => obj is Key other && Equals(other);
            public override int GetHashCode() => ElementType.GetHashCode() ^ Rank.GetHashCode();
        }
    }
}
