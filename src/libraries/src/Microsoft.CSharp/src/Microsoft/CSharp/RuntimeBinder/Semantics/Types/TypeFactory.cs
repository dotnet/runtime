// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Microsoft.CSharp.RuntimeBinder.Syntax;

namespace Microsoft.CSharp.RuntimeBinder.Semantics
{
    internal sealed class TypeFactory
    {
        // Aggregate
        public AggregateType CreateAggregateType(
            AggregateSymbol parent,
            TypeArray typeArgsThis,
            AggregateType outerType)
        {
            AggregateType type = new AggregateType();

            type.outerType = outerType;
            type.SetOwningAggregate(parent);
            type.SetTypeArgsThis(typeArgsThis);
            type.SetTypeKind(TypeKind.TK_AggregateType);
            return type;
        }

        // TypeParameter
        public TypeParameterType CreateTypeParameter(TypeParameterSymbol pSymbol)
        {
            TypeParameterType type = new TypeParameterType();
            type.SetTypeParameterSymbol(pSymbol);
            type.SetName(pSymbol.name);
            Debug.Assert(pSymbol.GetTypeParameterType() == null);
            pSymbol.SetTypeParameterType(type);

            type.SetTypeKind(TypeKind.TK_TypeParameterType);
            return type;
        }

        // Primitives
        public VoidType CreateVoid()
        {
            VoidType type = new VoidType();
            type.SetTypeKind(TypeKind.TK_VoidType);
            return type;
        }

        public NullType CreateNull()
        {
            NullType type = new NullType();
            type.SetTypeKind(TypeKind.TK_NullType);
            return type;
        }

        public MethodGroupType CreateMethodGroup()
        {
            MethodGroupType type = new MethodGroupType();
            type.SetTypeKind(TypeKind.TK_MethodGroupType);
            return type;
        }

        public ArgumentListType CreateArgList()
        {
            ArgumentListType type = new ArgumentListType();
            type.SetTypeKind(TypeKind.TK_ArgumentListType);
            return type;
        }

        // Derived types - parent is base type
        public ArrayType CreateArray(Name name, CType pElementType, int rank, bool isSZArray)
        {
            ArrayType type = new ArrayType();

            type.SetName(name);
            type.rank = rank;
            type.IsSZArray = isSZArray;
            type.SetElementType(pElementType);

            type.SetTypeKind(TypeKind.TK_ArrayType);
            return type;
        }

        public PointerType CreatePointer(Name name, CType pReferentType)
        {
            PointerType type = new PointerType();
            type.SetName(name);
            type.SetReferentType(pReferentType);

            type.SetTypeKind(TypeKind.TK_PointerType);
            return type;
        }

        public ParameterModifierType CreateParameterModifier(Name name, CType pParameterType)
        {
            ParameterModifierType type = new ParameterModifierType();
            type.SetName(name);
            type.SetParameterType(pParameterType);

            type.SetTypeKind(TypeKind.TK_ParameterModifierType);
            return type;
        }

        public NullableType CreateNullable(Name name, CType pUnderlyingType, BSYMMGR symmgr, TypeManager typeManager)
        {
            NullableType type = new NullableType();
            type.SetName(name);
            type.SetUnderlyingType(pUnderlyingType);
            type.symmgr = symmgr;
            type.typeManager = typeManager;

            type.SetTypeKind(TypeKind.TK_NullableType);
            return type;
        }
    }
}
