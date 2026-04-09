// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    /////////////////////////////////////////////////////////////////////////////////
    // Encapsulates a type list, including its size.

    internal sealed class TypeArray
    {
        ////////////////////////////////////////////////////////////////////////////////
        // Allocate a type array; used to represent a parameter list.
        // We use a hash table to make sure that allocating the same type array twice
        // returns the same value. This does two things:
        //
        // 1) Save a lot of memory.
        // 2) Make it so parameter lists can be compared by a simple pointer comparison

        private readonly struct TypeArrayKey : IEquatable<TypeArrayKey>
        {
            private readonly CType[] _types;
            private readonly int _hashCode;

            public TypeArrayKey(CType[] types)
            {
                _types = types;
                int hashCode = 0x162A16FE;
                foreach (CType type in types)
                {
                    hashCode = (hashCode << 5) - hashCode;
                    if (type != null)
                    {
                        hashCode ^= type.GetHashCode();
                    }
                }

                _hashCode = hashCode;
            }

            public bool Equals(TypeArrayKey other)
            {
                CType[] types = _types;
                CType[] otherTypes = other._types;
                if (otherTypes == types)
                {
                    return true;
                }

                if (other._hashCode != _hashCode || otherTypes.Length != types.Length)
                {
                    return false;
                }

                for (int i = 0; i < types.Length; i++)
                {
                    if (types[i] != otherTypes[i])
                    {
                        return false;
                    }
                }

                return true;
            }

            [ExcludeFromCodeCoverage(Justification = "Typed overload should always be the method called")]
            public override bool Equals(object obj)
            {
                Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
                return obj is TypeArrayKey key && Equals(key);
            }

            public override int GetHashCode() => _hashCode;
        }

        // The RuntimeBinder uses a global lock when Binding that keeps this dictionary safe.
        private static readonly Dictionary<TypeArrayKey, TypeArray> s_tableTypeArrays =
            new Dictionary<TypeArrayKey, TypeArray>();

        public static readonly TypeArray Empty = new TypeArray(Array.Empty<CType>());

        private TypeArray(CType[] types)
        {
            Debug.Assert(types != null);
            Items = types;
        }

        public int Count => Items.Length;

        public CType[] Items { get; }

        public CType this[int i] => Items[i];

        [Conditional("DEBUG")]
        public void AssertValid()
        {
            Debug.Assert(Count >= 0);
            Debug.Assert(Array.TrueForAll(Items, item => item != null));
        }

        public void CopyItems(int i, int c, CType[] dest) => Array.Copy(Items, i, dest, 0, c);

        public static TypeArray Allocate(int ctype, TypeArray array, int offset)
        {
            if (ctype == 0)
            {
                return Empty;
            }

            if (ctype == array.Count)
            {
                return array;
            }

            CType[] types = array.Items;
            CType[] newTypes = new CType[ctype];
            Array.ConstrainedCopy(types, offset, newTypes, 0, ctype);
            return Allocate(newTypes);
        }

        public static TypeArray Allocate(params CType[] types)
        {
            if (types?.Length > 0)
            {
                RuntimeBinder.EnsureLockIsTaken();
                TypeArrayKey key = new TypeArrayKey(types);
                if (!s_tableTypeArrays.TryGetValue(key, out TypeArray result))
                {
                    result = new TypeArray(types);
                    s_tableTypeArrays.Add(key, result);
                }

                return result;
            }

            return Empty;
        }

        public static TypeArray Concat(TypeArray pta1, TypeArray pta2)
        {
            CType[] prgtype1 = pta1.Items;
            if (prgtype1.Length == 0)
            {
                return pta2;
            }

            CType[] prgtype2 = pta2.Items;
            if (prgtype2.Length == 0)
            {
                return pta1;
            }

            CType[] combined = new CType[prgtype1.Length + prgtype2.Length];
            Array.Copy(prgtype1, combined, prgtype1.Length);
            Array.Copy(prgtype2, 0, combined, prgtype1.Length, prgtype2.Length);
            return Allocate(combined);
        }
    }
}
