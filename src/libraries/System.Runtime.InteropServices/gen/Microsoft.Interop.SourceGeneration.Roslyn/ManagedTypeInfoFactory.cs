// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;

namespace Microsoft.Interop;

/// <summary>Creates compiler-independent type information from type symbols.</summary>
public static class ManagedTypeInfoFactory
{
    extension(ManagedTypeInfo)
    {
        /// <summary>Extracts the information needed to resolve marshalling and generate source.</summary>
        /// <param name="type">The managed type symbol.</param>
        /// <returns>The compiler-independent type information.</returns>
        public static ManagedTypeInfo CreateTypeInfoForTypeSymbol(ITypeSymbol type)
        {
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string diagnosticFormattedName = type.ToDisplayString();
            SpecialType specialType = type.SpecialType.ToInteropSpecialType();
            if (specialType != SpecialType.None)
            {
                return new SpecialTypeInfo(typeName, diagnosticFormattedName, specialType);
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return new EnumTypeInfo(typeName, diagnosticFormattedName, ((INamedTypeSymbol)type).EnumUnderlyingType!.SpecialType.ToInteropSpecialType());
            }
            if (type.TypeKind == TypeKind.Pointer)
            {
                return new PointerTypeInfo(typeName, diagnosticFormattedName, IsFunctionPointer: false);
            }
            if (type.TypeKind == TypeKind.FunctionPointer)
            {
                return new PointerTypeInfo(typeName, diagnosticFormattedName, IsFunctionPointer: true);
            }
            if (type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol { IsSZArray: true } arraySymbol)
            {
                return new SzArrayType(ManagedTypeInfo.CreateTypeInfoForTypeSymbol(arraySymbol.ElementType));
            }
            if (type.TypeKind == TypeKind.Delegate)
            {
                return new DelegateTypeInfo(typeName, diagnosticFormattedName, type is INamedTypeSymbol { IsGenericType: true });
            }
            if (type.TypeKind == TypeKind.TypeParameter)
            {
                return new TypeParameterTypeInfo(typeName, diagnosticFormattedName);
            }
            if (type.IsValueType)
            {
                return new ValueTypeInfo(typeName, diagnosticFormattedName, type.IsRefLikeType);
            }
            return new ReferenceTypeInfo(typeName, diagnosticFormattedName);
        }
    }
}
