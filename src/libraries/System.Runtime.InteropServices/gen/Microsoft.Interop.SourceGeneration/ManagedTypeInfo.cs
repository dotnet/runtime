// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// A discriminated union that contains enough info about a managed type to determine a marshalling generator and generate code.
    /// </summary>
    public abstract record ManagedTypeInfo(string FullTypeName, string DiagnosticFormattedName)
    {
        private TypeSyntax? _syntax;
        public TypeSyntax Syntax => _syntax ??= SyntaxFactory.ParseTypeName(FullTypeName);

        public virtual bool Equals(ManagedTypeInfo? other)
        {
            return other is not null
                && Syntax.IsEquivalentTo(other.Syntax)
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FullTypeName, DiagnosticFormattedName);
        }

        protected ManagedTypeInfo(ManagedTypeInfo original)
        {
            FullTypeName = original.FullTypeName;
            DiagnosticFormattedName = original.DiagnosticFormattedName;
            // Explicitly don't initialize _syntax here. We want Syntax to be recalculated
            // from the results of a with-expression, which assigns the new property values
            // to the result of this constructor.
        }

        public static ManagedTypeInfo CreateTypeInfoForTypeSymbol(ITypeSymbol type)
        {
            string typeName = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            string diagnosticFormattedName = type.ToDisplayString();
            if (type.SpecialType != SpecialType.None)
            {
                return new SpecialTypeInfo(typeName, diagnosticFormattedName, type.SpecialType);
            }
            if (type.TypeKind == TypeKind.Enum)
            {
                return new EnumTypeInfo(typeName, diagnosticFormattedName, ((INamedTypeSymbol)type).EnumUnderlyingType!.SpecialType);
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
                return new SzArrayType(CreateTypeInfoForTypeSymbol(arraySymbol.ElementType));
            }
            if (type.TypeKind == TypeKind.Delegate)
            {
                return new DelegateTypeInfo(typeName, diagnosticFormattedName);
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

    public sealed record SpecialTypeInfo(string FullTypeName, string DiagnosticFormattedName, SpecialType SpecialType) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName)
    {
        public static readonly SpecialTypeInfo Byte = new("byte", "byte", SpecialType.System_Byte);
        public static readonly SpecialTypeInfo SByte = new("sbyte", "sbyte", SpecialType.System_SByte);
        public static readonly SpecialTypeInfo Int16 = new("short", "short", SpecialType.System_Int16);
        public static readonly SpecialTypeInfo UInt16 = new("ushort", "ushort", SpecialType.System_UInt16);
        public static readonly SpecialTypeInfo Int32 = new("int", "int", SpecialType.System_Int32);
        public static readonly SpecialTypeInfo UInt32 = new("uint", "uint", SpecialType.System_UInt32);
        public static readonly SpecialTypeInfo Void = new("void", "void", SpecialType.System_Void);
        public static readonly SpecialTypeInfo String = new("string", "string", SpecialType.System_String);
        public static readonly SpecialTypeInfo Boolean = new("bool", "bool", SpecialType.System_Boolean);
        public static readonly SpecialTypeInfo IntPtr = new("nint", "nint", SpecialType.System_IntPtr);

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

    public sealed record TypeParameterTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record ValueTypeInfo(string FullTypeName, string DiagnosticFormattedName, bool IsByRefLike) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);

    public sealed record ReferenceTypeInfo(string FullTypeName, string DiagnosticFormattedName) : ManagedTypeInfo(FullTypeName, DiagnosticFormattedName);
}
