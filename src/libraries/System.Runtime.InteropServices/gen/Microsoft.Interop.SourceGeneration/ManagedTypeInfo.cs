// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Interop
{
    /// <summary>
    /// A discriminated union that contains enough info about a managed type to determine a marshalling generator and generate code.
    /// </summary>
    public abstract record ManagedTypeInfo(string FullTypeName, string DiagnosticFormattedName)
    {
        public TypeSyntax Syntax { get; } = SyntaxFactory.ParseTypeName(FullTypeName);

        public static ManagedTypeInfo CreateTypeInfoForTypeSymbol(ITypeSymbol type)
        {
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string diagonsticFormattedName = type.ToDisplayString();
            if (type.SpecialType != SpecialType.None)
            {
                return new SpecialTypeInfo(typeName, diagonsticFormattedName, type.SpecialType);
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return new EnumTypeInfo(typeName, diagonsticFormattedName, ((INamedTypeSymbol)type).EnumUnderlyingType!.SpecialType);
            }
            if (type.TypeKind == TypeKind.Pointer)
            {
                return new PointerTypeInfo(typeName, diagonsticFormattedName, IsFunctionPointer: false);
            }
            if (type.TypeKind == TypeKind.FunctionPointer)
            {
                return new PointerTypeInfo(typeName, diagonsticFormattedName, IsFunctionPointer: true);
            }
            if (type.TypeKind == TypeKind.Array && type is IArrayTypeSymbol { IsSZArray: true } arraySymbol)
            {
                return new SzArrayType(CreateTypeInfoForTypeSymbol(arraySymbol.ElementType));
            }
            if (type.TypeKind == TypeKind.Delegate)
            {
                return new DelegateTypeInfo(typeName, diagonsticFormattedName);
            }
            return new SimpleManagedTypeInfo(typeName, diagonsticFormattedName);
        }
    }

    public sealed record SpecialTypeInfo(string FullTypeName, string DiagnosticFormattedName, SpecialType SpecialType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName)
    {
        public static readonly SpecialTypeInfo Byte = new("byte", "byte", SpecialType.System_Void);
        public static readonly SpecialTypeInfo Int32 = new("int", "int", SpecialType.System_Int32);
        public static readonly SpecialTypeInfo Void = new("void", "void", SpecialType.System_Void);

        public bool Equals(SpecialTypeInfo? other)
        {
            return other is not null && SpecialType == other.SpecialType;
        }

        public override int GetHashCode()
        {
            return (int)SpecialType;
        }
    }

    public sealed record EnumTypeInfo(string FullTypeName, string DiagnosticFormattedName, SpecialType UnderlyingType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record PointerTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsFunctionPointer) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record SzArrayType(ManagedTypeInfo ElementTypeInfo) : ManagedTypeInfo($"{ElementTypeInfo.FullTypeName}[]", $"{ElementTypeInfo.DiagnosticFormattedName}[]");

    public sealed record DelegateTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record SimpleManagedTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);
}
