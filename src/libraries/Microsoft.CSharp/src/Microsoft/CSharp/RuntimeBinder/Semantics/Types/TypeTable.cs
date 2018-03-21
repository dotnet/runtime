// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal static class TypeTable
    {
        private readonly struct KeyPair<TKey1, TKey2> : IEquatable<KeyPair<TKey1, TKey2>>
        {
            private readonly TKey1 _pKey1;
            private readonly TKey2 _pKey2;

            public KeyPair(TKey1 pKey1, TKey2 pKey2)
            {
                _pKey1 = pKey1;
                _pKey2 = pKey2;
            }

            public bool Equals(KeyPair<TKey1, TKey2> other) =>
                EqualityComparer<TKey1>.Default.Equals(_pKey1, other._pKey1)
                && EqualityComparer<TKey2>.Default.Equals(_pKey2, other._pKey2);

#if DEBUG
            [ExcludeFromCodeCoverage] // Typed overload should always be the method called.
#endif
            public override bool Equals(object obj)
            {
                Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
                if (!(obj is KeyPair<TKey1, TKey2>))
                {
                    return false;
                }

                return Equals((KeyPair<TKey1, TKey2>)obj);
            }

            public override int GetHashCode()
            {
                int hash = _pKey1 == null ? 0 : _pKey1.GetHashCode();
                return (hash << 5) - hash + (_pKey2 == null ? 0 : _pKey2.GetHashCode());
            }
        }
        
        // The RuntimeBinder uses a global lock when Binding that keeps these dictionary safe.
        // Two way hashes
        private static readonly Dictionary<KeyPair<AggregateSymbol, KeyPair<AggregateType, TypeArray>>, AggregateType> s_aggregateTable =
                new Dictionary<KeyPair<AggregateSymbol, KeyPair<AggregateType, TypeArray>>, AggregateType>();

        private static readonly Dictionary<KeyPair<CType, int>, ArrayType> s_arrayTable =
            new Dictionary<KeyPair<CType, int>, ArrayType>();

        private static readonly Dictionary<KeyPair<CType, bool>, ParameterModifierType> s_parameterModifierTable =
            new Dictionary<KeyPair<CType, bool>, ParameterModifierType>();

        // One way hashes
        private static readonly Dictionary<CType, PointerType> s_pointerTable = new Dictionary<CType, PointerType>();
        private static readonly Dictionary<CType, NullableType> s_nullableTable = new Dictionary<CType, NullableType>();

        private static KeyPair<TKey1, TKey2> MakeKey<TKey1, TKey2>(TKey1 key1, TKey2 key2) =>
            new KeyPair<TKey1, TKey2>(key1, key2);

        public static AggregateType LookupAggregate(AggregateSymbol aggregate, AggregateType outer, TypeArray args)
        {
            RuntimeBinder.EnsureLockIsTaken();
            s_aggregateTable.TryGetValue(MakeKey(aggregate, MakeKey(outer, args)), out AggregateType result);
            return result;
        }

        public static void InsertAggregate(AggregateSymbol aggregate, AggregateType outer, TypeArray args, AggregateType ats)
        {
            RuntimeBinder.EnsureLockIsTaken();
            Debug.Assert(LookupAggregate(aggregate, outer, args) == null);
            s_aggregateTable.Add(MakeKey(aggregate, MakeKey(outer, args)), ats);
        }

        // rankNum is 0 for SZ arrays, equal to rank otherwise.
        public static ArrayType LookupArray(CType elementType, int rankNum)
        {
            RuntimeBinder.EnsureLockIsTaken();
            s_arrayTable.TryGetValue(new KeyPair<CType, int>(elementType, rankNum), out ArrayType result);
            return result;
        }

        public static void InsertArray(CType elementType, int rankNum, ArrayType pArray)
        {
            RuntimeBinder.EnsureLockIsTaken();
            Debug.Assert(LookupArray(elementType, rankNum) == null);
            s_arrayTable.Add(new KeyPair<CType, int>(elementType, rankNum), pArray);
        }

        public static ParameterModifierType LookupParameterModifier(CType elementType, bool isOut)
        {
            RuntimeBinder.EnsureLockIsTaken();
            s_parameterModifierTable.TryGetValue(new KeyPair<CType, bool>(elementType, isOut), out ParameterModifierType result);
            return result;
        }

        public static void InsertParameterModifier(CType elementType, bool isOut, ParameterModifierType parameterModifier)
        {
            RuntimeBinder.EnsureLockIsTaken();
            Debug.Assert(LookupParameterModifier(elementType, isOut) == null);
            s_parameterModifierTable.Add(new KeyPair<CType, bool>(elementType, isOut), parameterModifier);
        }

        public static PointerType LookupPointer(CType elementType)
        {
            s_pointerTable.TryGetValue(elementType, out PointerType result);
            return result;
        }

        public static void InsertPointer(CType elementType, PointerType pointer)
        {
            Debug.Assert(LookupPointer(elementType) == null);
            s_pointerTable.Add(elementType, pointer);
        }

        public static NullableType LookupNullable(CType underlyingType)
        {
            s_nullableTable.TryGetValue(underlyingType, out NullableType result);
            return result;
        }

        public static void InsertNullable(CType underlyingType, NullableType nullable)
        {
            Debug.Assert(LookupNullable(underlyingType) == null);
            s_nullableTable.Add(underlyingType, nullable);
        }
    }
}
