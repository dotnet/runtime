// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public sealed record TypeName(TypeName? ContainingTypeName, string DottedName);

    public sealed record ArrayBoundValue(int? Lower, int? Upper);

    public abstract record ElementTypeValue
    {
        public static ElementTypeValue Error { get; } = new ErrorElementTypeValue();
    }

    public sealed record ErrorElementTypeValue : ElementTypeValue;

    public sealed record PrimitiveElementTypeValue(byte TypeCode) : ElementTypeValue;

    public sealed record ClassElementTypeValue(ClassNameValue ClassName, bool IsValueType) : ElementTypeValue;

    public sealed record FunctionPointerElementTypeValue(
        byte CallingConvention,
        TypeValue ReturnType,
        ImmutableArray<SignatureArgumentValue> Arguments) : ElementTypeValue;

    public abstract record GenericParameterElementTypeValue(bool IsMethodParameter) : ElementTypeValue;

    public sealed record IndexedGenericParameterElementTypeValue(bool IsMethodParameter, int Index)
        : GenericParameterElementTypeValue(IsMethodParameter);

    public sealed record NamedGenericParameterElementTypeValue(
        IToken Token,
        bool IsMethodParameter,
        string Name) : GenericParameterElementTypeValue(IsMethodParameter);

    public sealed record TypedefElementTypeValue(IToken Token, string Alias) : ElementTypeValue;

    public sealed record SentinelElementTypeValue(TypeValue Type) : ElementTypeValue;

    public sealed record TypeValue(
        ElementTypeValue ElementType,
        ImmutableArray<TypeModifierValue> Modifiers)
    {
        public static TypeValue Error { get; } = new(ElementTypeValue.Error, []);
    }

    public abstract record TypeModifierValue
    {
        public static TypeModifierValue Error { get; } = new ErrorTypeModifierValue();
    }

    public sealed record ErrorTypeModifierValue : TypeModifierValue;

    public enum SimpleTypeModifierKind
    {
        SzArray,
        ByReference,
        Pointer,
        Pinned
    }

    public sealed record SimpleTypeModifierValue(SimpleTypeModifierKind Kind) : TypeModifierValue;

    public sealed record ArrayTypeModifierValue(ImmutableArray<ArrayBoundValue> Bounds) : TypeModifierValue;

    public sealed record CustomTypeModifierValue(
        TypeSpecificationValue Type,
        bool IsRequired) : TypeModifierValue;

    public sealed record GenericArgumentsTypeModifierValue(
        ImmutableArray<TypeValue> Arguments) : TypeModifierValue;

    public sealed record SignatureArgumentValue(
        bool IsSentinel,
        int Attributes,
        TypeValue? Type,
        MarshallingDescriptorValue? Marshalling,
        string? Name)
    {
        public static SignatureArgumentValue Error { get; } =
            new(false, 0, TypeValue.Error, null, null);
    }

    public sealed record CalliSignatureValue(
        byte CallingConvention,
        TypeValue ReturnType,
        ImmutableArray<SignatureArgumentValue> Arguments)
    {
        public static CalliSignatureValue Error { get; } = new(0, TypeValue.Error, []);
    }

    public abstract record ClassNameValue
    {
        public static ClassNameValue Error { get; } = new ErrorClassNameValue();
    }

    public sealed record ErrorClassNameValue : ClassNameValue;

    public sealed record UnqualifiedClassNameValue(TypeName Name) : ClassNameValue;

    public sealed record AssemblyQualifiedClassNameValue(
        string AssemblyName,
        TypeName Name) : ClassNameValue;

    public sealed record ModuleQualifiedClassNameValue(
        IToken Token,
        string ModuleName,
        TypeName Name) : ClassNameValue;

    public sealed record TokenQualifiedClassNameValue(
        int Token,
        TypeName Name) : ClassNameValue;

    public sealed record PointerQualifiedClassNameValue(TypeName Name) : ClassNameValue;

    public sealed record TokenClassNameValue(int Token) : ClassNameValue;

    public enum SpecialClassNameKind
    {
        This,
        Base,
        Nester
    }

    public sealed record SpecialClassNameValue(
        IToken Token,
        SpecialClassNameKind Kind) : ClassNameValue;

    public abstract record TypeSpecificationValue
    {
        public static TypeSpecificationValue Error { get; } =
            new ErrorTypeSpecificationValue();
    }

    public sealed record ErrorTypeSpecificationValue : TypeSpecificationValue;

    public sealed record ClassTypeSpecificationValue(
        ClassNameValue ClassName) : TypeSpecificationValue;

    public sealed record AssemblyTypeSpecificationValue(
        string AssemblyName) : TypeSpecificationValue;

    public sealed record ModuleTypeSpecificationValue(
        string ModuleName) : TypeSpecificationValue;

    public sealed record SignatureTypeSpecificationValue(
        TypeValue Type) : TypeSpecificationValue;

    public abstract record MethodReferenceValue
    {
        public static MethodReferenceValue Error { get; } = new ErrorMethodReferenceValue();
    }

    public sealed record ErrorMethodReferenceValue : MethodReferenceValue;

    public sealed record TokenMethodReferenceValue(int Token) : MethodReferenceValue;

    public sealed record TypedefMethodReferenceValue(
        IToken Token,
        string Alias) : MethodReferenceValue;

    public sealed record ParsedMethodReferenceValue(
        IToken Token,
        byte CallingConvention,
        TypeValue ReturnType,
        TypeSpecificationValue? Owner,
        string Name,
        ImmutableArray<TypeValue>? GenericArguments,
        int GenericArity,
        ImmutableArray<SignatureArgumentValue> Arguments) : MethodReferenceValue;

    public abstract record FieldReferenceValue
    {
        public static FieldReferenceValue Error { get; } = new ErrorFieldReferenceValue();
    }

    public sealed record ErrorFieldReferenceValue : FieldReferenceValue;

    public sealed record TypedefFieldReferenceValue(
        IToken Token,
        string Alias) : FieldReferenceValue;

    public sealed record ParsedFieldReferenceValue(
        TypeValue FieldType,
        TypeSpecificationValue? Owner,
        string Name) : FieldReferenceValue;

    public abstract record MemberReferenceValue
    {
        public static MemberReferenceValue Error { get; } = new ErrorMemberReferenceValue();
    }

    public sealed record ErrorMemberReferenceValue : MemberReferenceValue;

    public sealed record MethodMemberReferenceValue(
        MethodReferenceValue Method) : MemberReferenceValue;

    public sealed record FieldMemberReferenceValue(
        FieldReferenceValue Field) : MemberReferenceValue;

    public sealed record TokenMemberReferenceValue(int Token) : MemberReferenceValue;

    public abstract record OwnerTypeValue
    {
        public static OwnerTypeValue Error { get; } = new ErrorOwnerTypeValue();
    }

    public sealed record ErrorOwnerTypeValue : OwnerTypeValue;

    public sealed record TypeOwnerValue(TypeSpecificationValue Type) : OwnerTypeValue;

    public sealed record MemberOwnerValue(MemberReferenceValue Member) : OwnerTypeValue;
}
