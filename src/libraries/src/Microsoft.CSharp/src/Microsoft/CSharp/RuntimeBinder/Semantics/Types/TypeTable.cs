// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal readonly struct KeyPair<Key1, Key2> : IEquatable<KeyPair<Key1, Key2>>
    {
        private readonly Key1 _pKey1;
        private readonly Key2 _pKey2;

        public KeyPair(Key1 pKey1, Key2 pKey2)
        {
            _pKey1 = pKey1;
            _pKey2 = pKey2;
        }

        public bool Equals(KeyPair<Key1, Key2> other)
        {
            return EqualityComparer<Key1>.Default.Equals(_pKey1, other._pKey1)
                && EqualityComparer<Key2>.Default.Equals(_pKey2, other._pKey2);
        }

#if  DEBUG 
        [ExcludeFromCodeCoverage] // Typed overload should always be the method called.
#endif
        public override bool Equals(object obj)
        {
            Debug.Fail("Sub-optimal overload called. Check if this can be avoided.");
            if (!(obj is KeyPair<Key1, Key2>)) return false;
            return Equals((KeyPair<Key1, Key2>)obj);
        }

        public override int GetHashCode()
        {
            int hash = _pKey1 == null ? 0 : _pKey1.GetHashCode();
            return (hash << 5) - hash + (_pKey2 == null ? 0 : _pKey2.GetHashCode());
        }
    }

    internal sealed class TypeTable
    {
        // Two way hashes
        private readonly Dictionary<KeyPair<AggregateSymbol, KeyPair<AggregateType, TypeArray>>, AggregateType> _aggregateTable;
        private readonly Dictionary<KeyPair<CType, Name>, ArrayType> _pArrayTable;
        private readonly Dictionary<KeyPair<CType, Name>, ParameterModifierType> _pParameterModifierTable;

        // One way hashes
        private readonly Dictionary<CType, PointerType> _pPointerTable;
        private readonly Dictionary<CType, NullableType> _pNullableTable;

        public TypeTable()
        {
            _aggregateTable = new Dictionary<KeyPair<AggregateSymbol, KeyPair<AggregateType, TypeArray>>, AggregateType>();
            _pArrayTable = new Dictionary<KeyPair<CType, Name>, ArrayType>();
            _pParameterModifierTable = new Dictionary<KeyPair<CType, Name>, ParameterModifierType>();
            _pPointerTable = new Dictionary<CType, PointerType>();
            _pNullableTable = new Dictionary<CType, NullableType>();
        }

        private static KeyPair<TKey1, TKey2> MakeKey<TKey1, TKey2>(TKey1 key1, TKey2 key2) =>
            new KeyPair<TKey1, TKey2>(key1, key2);

        public AggregateType LookupAggregate(AggregateSymbol aggregate, AggregateType outer, TypeArray args)
        {
            _aggregateTable.TryGetValue(MakeKey(aggregate, MakeKey(outer, args)), out AggregateType result);
            return result;
        }

        public void InsertAggregate(
            AggregateSymbol aggregate, AggregateType outer, TypeArray args, AggregateType pAggregate)
        {
            Debug.Assert(LookupAggregate(aggregate, outer, args) == null);
            _aggregateTable.Add(MakeKey(aggregate, MakeKey(outer, args)), pAggregate);
        }

        public ArrayType LookupArray(Name pName, CType pElementType)
        {
            var key = new KeyPair<CType, Name>(pElementType, pName);
            ArrayType result;
            if (_pArrayTable.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertArray(Name pName, CType pElementType, ArrayType pArray)
        {
            Debug.Assert(LookupArray(pName, pElementType) == null);
            _pArrayTable.Add(new KeyPair<CType, Name>(pElementType, pName), pArray);
        }

        public ParameterModifierType LookupParameterModifier(Name pName, CType pElementType)
        {
            var key = new KeyPair<CType, Name>(pElementType, pName);
            ParameterModifierType result;
            if (_pParameterModifierTable.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertParameterModifier(
                Name pName,
                CType pElementType,
                ParameterModifierType pParameterModifier)
        {
            Debug.Assert(LookupParameterModifier(pName, pElementType) == null);
            _pParameterModifierTable.Add(new KeyPair<CType, Name>(pElementType, pName), pParameterModifier);
        }

        public PointerType LookupPointer(CType pElementType)
        {
            PointerType result;
            if (_pPointerTable.TryGetValue(pElementType, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertPointer(CType pElementType, PointerType pPointer)
        {
            _pPointerTable.Add(pElementType, pPointer);
        }

        public NullableType LookupNullable(CType pUnderlyingType)
        {
            NullableType result;
            if (_pNullableTable.TryGetValue(pUnderlyingType, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertNullable(CType pUnderlyingType, NullableType pNullable)
        {
            _pNullableTable.Add(pUnderlyingType, pNullable);
        }
    }
}
