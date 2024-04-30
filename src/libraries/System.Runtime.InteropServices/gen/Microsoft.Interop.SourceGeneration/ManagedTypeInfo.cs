// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Microsoft.Interop
{
    /// <summary>
    /// A discriminated union that contains enough info about a managed type to determine a marshalling generator and generate code.
    /// </summary>
    public abstract class ManagedTypeInfo : IEquatable<ManagedTypeInfo>
    {
        public string FullTypeName { get; }

        public string DiagnosticFormattedName { get; }

        private TypeSyntax? _syntax;
        public TypeSyntax Syntax => _syntax ??= SyntaxFactory.ParseTypeName(FullTypeName);

        protected ManagedTypeInfo(string fullTypeName, string diagnosticFormattedName)
        {
            FullTypeName = fullTypeName;
            DiagnosticFormattedName = diagnosticFormattedName;
        }

        public static bool operator ==(ManagedTypeInfo? left, ManagedTypeInfo? right)
        {
            if (left is null)
            {
                return right is null;
            }
            return left.Equals(right);
        }
        public static bool operator !=(ManagedTypeInfo? left, ManagedTypeInfo? right) => !(left == right);

        public override bool Equals(object obj) => obj is ManagedTypeInfo mti && Equals(mti);

        public abstract bool Equals(ManagedTypeInfo? other);

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

        public abstract ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname);

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
                return new PointerTypeInfo(typeName, diagnosticFormattedName, isFunctionPointer: false);
            }
            if (type.TypeKind == TypeKind.FunctionPointer)
            {
                return new PointerTypeInfo(typeName, diagnosticFormattedName, isFunctionPointer: true);
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

    public sealed class SpecialTypeInfo : ManagedTypeInfo
    {
        public SpecialType SpecialType { get; }

        public SpecialTypeInfo(string fullTypeName, string diagnosticFormattedName, SpecialType specialType) : base(fullTypeName, diagnosticFormattedName)
        {
            SpecialType = specialType;
        }

        public static readonly SpecialTypeInfo Byte = new("byte", "byte", SpecialType.System_Byte);
        public static readonly SpecialTypeInfo SByte = new("sbyte", "sbyte", SpecialType.System_SByte);
        public static readonly SpecialTypeInfo Int16 = new("short", "short", SpecialType.System_Int16);
        public static readonly SpecialTypeInfo UInt16 = new("ushort", "ushort", SpecialType.System_UInt16);
        public static readonly SpecialTypeInfo Int32 = new("int", "int", SpecialType.System_Int32);
        public static readonly SpecialTypeInfo UInt32 = new("uint", "uint", SpecialType.System_UInt32);
        public static readonly SpecialTypeInfo Void = new("void", "void", SpecialType.System_Void);
        public static readonly SpecialTypeInfo String = new("string", "string", SpecialType.System_String);
        public static readonly SpecialTypeInfo Boolean = new("bool", "bool", SpecialType.System_Boolean);
        public static readonly SpecialTypeInfo IntPtr = new("System.IntPtr", "System.IntPtr", SpecialType.System_IntPtr);

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname)
        {
            return new SpecialTypeInfo(fullTypeName, diagnosticFormattedname, SpecialType);
        }

        public override bool Equals(ManagedTypeInfo? other)
            => other is SpecialTypeInfo specialType
                && SpecialType == specialType.SpecialType
                && FullTypeName == specialType.FullTypeName
                && DiagnosticFormattedName == specialType.DiagnosticFormattedName;

        public override int GetHashCode()
        {
            return HashCode.Combine(_typeHashCode, SpecialType, base.GetHashCode());
        }

        private static readonly int _typeHashCode = typeof(SpecialTypeInfo).GetHashCode();
    }

    public sealed class EnumTypeInfo : ManagedTypeInfo
    {
        public SpecialType UnderlyingType { get; }

        public EnumTypeInfo(string fullTypeName, string diagnosticFormattedName, SpecialType underlyingType) : base(fullTypeName, diagnosticFormattedName)
        {
            UnderlyingType = underlyingType;
        }

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new EnumTypeInfo(fullTypeName, diagnosticFormattedname, UnderlyingType);

        public override bool Equals(ManagedTypeInfo? other)
            => other is EnumTypeInfo enumTypeInfo
                && this.UnderlyingType == enumTypeInfo.UnderlyingType
                && FullTypeName == enumTypeInfo.FullTypeName
                && DiagnosticFormattedName == enumTypeInfo.DiagnosticFormattedName;

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, UnderlyingType, base.GetHashCode());

        private static readonly int _typeHashCode = typeof(EnumTypeInfo).GetHashCode();
    }

    public sealed class PointerTypeInfo : ManagedTypeInfo
    {
        public bool IsFunctionPointer { get; }

        public PointerTypeInfo(string fullTypeName, string diagnosticFormattedName, bool isFunctionPointer) : base(fullTypeName, diagnosticFormattedName)
        {
            IsFunctionPointer = isFunctionPointer;
        }

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new PointerTypeInfo(fullTypeName, diagnosticFormattedname, IsFunctionPointer);

        public override bool Equals(ManagedTypeInfo? obj)
            => obj is PointerTypeInfo ptrType
                && IsFunctionPointer == ptrType.IsFunctionPointer
                && FullTypeName == ptrType.FullTypeName
                && DiagnosticFormattedName == ptrType.DiagnosticFormattedName;

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, IsFunctionPointer, base.GetHashCode());

        private static readonly int _typeHashCode = typeof(PointerTypeInfo).GetHashCode();
    }

    public sealed class SzArrayType : ManagedTypeInfo
    {
        public ManagedTypeInfo ElementTypeInfo { get; }

        public SzArrayType(ManagedTypeInfo elementTypeInfo) : base($"{elementTypeInfo.FullTypeName}[]", $"{elementTypeInfo.DiagnosticFormattedName}[]")
        {
            ElementTypeInfo = elementTypeInfo;
        }

        private SzArrayType(string fullTypeName, string DiagnosticFormattedName, ManagedTypeInfo elementTypeInfo) : base(fullTypeName, DiagnosticFormattedName)
        {
            ElementTypeInfo = elementTypeInfo;
        }

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new SzArrayType(fullTypeName, diagnosticFormattedname, ElementTypeInfo);

        public override bool Equals(ManagedTypeInfo? other)
        {
            return other is SzArrayType szArrType
                && ElementTypeInfo.Equals(szArrType.ElementTypeInfo)
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;
        }

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, ElementTypeInfo, FullTypeName, DiagnosticFormattedName);

        private static readonly int _typeHashCode = typeof(SzArrayType).GetHashCode();
    }

    public sealed class DelegateTypeInfo : ManagedTypeInfo
    {
        public DelegateTypeInfo(string fullTypeName, string diagnosticFormattedName) : base(fullTypeName, diagnosticFormattedName) { }

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new DelegateTypeInfo(fullTypeName, diagnosticFormattedname);

        public override bool Equals(ManagedTypeInfo? other)
            => other is DelegateTypeInfo delegateType
                && FullTypeName == delegateType.FullTypeName
                && DiagnosticFormattedName == delegateType.DiagnosticFormattedName;

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, base.GetHashCode());

        private static readonly int _typeHashCode = typeof(DelegateTypeInfo).GetHashCode();
    }

    public sealed class TypeParameterTypeInfo : ManagedTypeInfo
    {
        public TypeParameterTypeInfo(string fullTypeName, string diagnosticFormattedName) : base(fullTypeName, diagnosticFormattedName) { }

        public override bool Equals(ManagedTypeInfo? other)
            => other is TypeParameterTypeInfo
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, base.GetHashCode());

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new TypeParameterTypeInfo(fullTypeName, diagnosticFormattedname);

        private static readonly int _typeHashCode = typeof(TypeParameterTypeInfo).GetHashCode();
    }

    public sealed class ValueTypeInfo : ManagedTypeInfo
    {
        public bool IsByRefLike { get; }

        public ValueTypeInfo(string fullTypeName, string diagnosticFormattedName, bool isByRefLike) : base(fullTypeName, diagnosticFormattedName)
        {
            IsByRefLike = isByRefLike;
        }

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, IsByRefLike, base.GetHashCode());

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new ValueTypeInfo(fullTypeName, diagnosticFormattedname, IsByRefLike);
        public override bool Equals(ManagedTypeInfo? other)
            => other is ValueTypeInfo
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;


        private static readonly int _typeHashCode = typeof(ReferenceTypeInfo).GetHashCode();
    }

    public sealed class ReferenceTypeInfo : ManagedTypeInfo
    {
        public ReferenceTypeInfo(string fullTypeName, string diagnosticFormattedName) : base(fullTypeName, diagnosticFormattedName) { }

        public override ManagedTypeInfo WithName(string fullTypeName, string diagnosticFormattedname) => new ReferenceTypeInfo(fullTypeName, diagnosticFormattedname);

        public override bool Equals(ManagedTypeInfo? other)
            => other is ReferenceTypeInfo
                && FullTypeName == other.FullTypeName
                && DiagnosticFormattedName == other.DiagnosticFormattedName;

        public override int GetHashCode() => HashCode.Combine(_typeHashCode, base.GetHashCode());

        private static readonly int _typeHashCode = typeof(ReferenceTypeInfo).GetHashCode();
    }
}
