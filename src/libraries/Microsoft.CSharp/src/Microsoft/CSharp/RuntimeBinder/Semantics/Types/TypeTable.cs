// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal struct KeyPair<Key1, Key2> : IEquatable<KeyPair<Key1, Key2>>
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
            return Equals(_pKey1, other._pKey1)
                && Equals(_pKey2, other._pKey2);
        }

        public override bool Equals(object obj)
        {
            if (!(obj is KeyPair<Key1, Key2>)) return false;
            return Equals((KeyPair<Key1, Key2>)obj);
        }

        public override int GetHashCode()
        {
            return (_pKey1 == null ? 0 : _pKey1.GetHashCode())
                + (_pKey2 == null ? 0 : _pKey2.GetHashCode());
        }
    }

    internal class TypeTable
    {
        // Two way hashes
        private readonly Dictionary<KeyPair<AggregateSymbol, Name>, AggregateType> _pAggregateTable;
        private readonly Dictionary<KeyPair<CType, Name>, ErrorType> _pErrorWithTypeParentTable;
        private readonly Dictionary<KeyPair<AssemblyQualifiedNamespaceSymbol, Name>, ErrorType> _pErrorWithNamespaceParentTable;
        private readonly Dictionary<KeyPair<CType, Name>, ArrayType> _pArrayTable;
        private readonly Dictionary<KeyPair<CType, Name>, ParameterModifierType> _pParameterModifierTable;

        // One way hashes
        private readonly Dictionary<CType, PointerType> _pPointerTable;
        private readonly Dictionary<CType, NullableType> _pNullableTable;
        private readonly Dictionary<TypeParameterSymbol, TypeParameterType> _pTypeParameterTable;

        public TypeTable()
        {
            _pAggregateTable = new Dictionary<KeyPair<AggregateSymbol, Name>, AggregateType>();
            _pErrorWithNamespaceParentTable = new Dictionary<KeyPair<AssemblyQualifiedNamespaceSymbol, Name>, ErrorType>();
            _pErrorWithTypeParentTable = new Dictionary<KeyPair<CType, Name>, ErrorType>();
            _pArrayTable = new Dictionary<KeyPair<CType, Name>, ArrayType>();
            _pParameterModifierTable = new Dictionary<KeyPair<CType, Name>, ParameterModifierType>();
            _pPointerTable = new Dictionary<CType, PointerType>();
            _pNullableTable = new Dictionary<CType, NullableType>();
            _pTypeParameterTable = new Dictionary<TypeParameterSymbol, TypeParameterType>();
        }

        public AggregateType LookupAggregate(Name pName, AggregateSymbol pAggregate)
        {
            var key = new KeyPair<AggregateSymbol, Name>(pAggregate, pName);
            AggregateType result;
            if (_pAggregateTable.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertAggregate(
                Name pName,
                AggregateSymbol pAggregateSymbol,
                AggregateType pAggregate)
        {
            Debug.Assert(LookupAggregate(pName, pAggregateSymbol) == null);
            _pAggregateTable.Add(new KeyPair<AggregateSymbol, Name>(pAggregateSymbol, pName), pAggregate);
        }

        public ErrorType LookupError(Name pName, CType pParentType)
        {
            var key = new KeyPair<CType, Name>(pParentType, pName);
            ErrorType result;
            if (_pErrorWithTypeParentTable.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public ErrorType LookupError(Name pName, AssemblyQualifiedNamespaceSymbol pParentNS)
        {
            var key = new KeyPair<AssemblyQualifiedNamespaceSymbol, Name>(pParentNS, pName);
            ErrorType result;
            if (_pErrorWithNamespaceParentTable.TryGetValue(key, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertError(Name pName, CType pParentType, ErrorType pError)
        {
            Debug.Assert(LookupError(pName, pParentType) == null);
            _pErrorWithTypeParentTable.Add(new KeyPair<CType, Name>(pParentType, pName), pError);
        }

        public void InsertError(Name pName, AssemblyQualifiedNamespaceSymbol pParentNS, ErrorType pError)
        {
            Debug.Assert(LookupError(pName, pParentNS) == null);
            _pErrorWithNamespaceParentTable.Add(new KeyPair<AssemblyQualifiedNamespaceSymbol, Name>(pParentNS, pName), pError);
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

        public TypeParameterType LookupTypeParameter(TypeParameterSymbol pTypeParameterSymbol)
        {
            TypeParameterType result;
            if (_pTypeParameterTable.TryGetValue(pTypeParameterSymbol, out result))
            {
                return result;
            }
            return null;
        }

        public void InsertTypeParameter(
                TypeParameterSymbol pTypeParameterSymbol,
                TypeParameterType pTypeParameter)
        {
            _pTypeParameterTable.Add(pTypeParameterSymbol, pTypeParameter);
        }
    }
}
