// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    // ANTLR context classes are public, but these implementation values must remain internal.
    // Grammar return slots therefore use object and are unwrapped only by these strongly typed helpers.
    private static T GetSemanticValue<T>(object? value, T fallback)
        where T : class
        => value as T ?? fallback;

    private static TypeValue GetTypeValue(object? value)
        => GetSemanticValue(value, TypeValue.Error);

    private static ElementTypeValue GetElementTypeValue(object? value)
        => GetSemanticValue(value, ElementTypeValue.Error);

    private static TypeModifierValue GetTypeModifierValue(object? value)
        => GetSemanticValue(value, TypeModifierValue.Error);

    private static ImmutableArray<TypeValue> GetTypeArgumentsValue(object? value)
        => value is ImmutableArray<TypeValue> arguments ? arguments : [];

    private static ImmutableArray<ArrayBoundValue> GetBoundsValue(object? value)
        => value is ImmutableArray<ArrayBoundValue> bounds ? bounds : [];

    private static SignatureArgumentValue GetSignatureArgumentValue(object? value)
        => GetSemanticValue(value, SignatureArgumentValue.Error);

    private static ImmutableArray<SignatureArgumentValue> GetSignatureArgumentsValue(object? value)
        => value is ImmutableArray<SignatureArgumentValue> arguments ? arguments : [];

    private static TypeName GetTypeNameValue(object? value)
        => value as TypeName ?? new TypeName(null, string.Empty);

    private static TypeName GetSlashedNameValue(CILParser.SlashedNameContext context)
        => GetTypeNameValue(context.Value);

    private static ClassNameValue GetClassNameValue(object? value)
        => GetSemanticValue(value, ClassNameValue.Error);

    private static TypeSpecificationValue GetTypeSpecificationValue(object? value)
        => GetSemanticValue(value, TypeSpecificationValue.Error);

    private static MethodReferenceValue GetMethodReferenceValue(object? value)
        => GetSemanticValue(value, MethodReferenceValue.Error);

    private static FieldReferenceValue GetFieldReferenceValue(object? value)
        => GetSemanticValue(value, FieldReferenceValue.Error);

    private static MemberReferenceValue GetMemberReferenceValue(object? value)
        => GetSemanticValue(value, MemberReferenceValue.Error);

    private static OwnerTypeValue GetOwnerTypeValue(object? value)
        => GetSemanticValue(value, OwnerTypeValue.Error);

    private static CalliSignatureValue GetCalliSignatureValue(object? value)
        => GetSemanticValue(value, CalliSignatureValue.Error);

    private sealed record ArrayBoundValue(int? Lower, int? Upper);

    private abstract record ElementTypeValue
    {
        public static ElementTypeValue Error { get; } = new ErrorElementTypeValue();
    }

    private sealed record ErrorElementTypeValue : ElementTypeValue;

    private sealed record PrimitiveElementTypeValue(byte TypeCode) : ElementTypeValue;

    private sealed record ClassElementTypeValue(ClassNameValue ClassName, bool IsValueType) : ElementTypeValue;

    private sealed record FunctionPointerElementTypeValue(
        byte CallingConvention,
        TypeValue ReturnType,
        ImmutableArray<SignatureArgumentValue> Arguments) : ElementTypeValue;

    private abstract record GenericParameterElementTypeValue(bool IsMethodParameter) : ElementTypeValue;

    private sealed record IndexedGenericParameterElementTypeValue(bool IsMethodParameter, int Index)
        : GenericParameterElementTypeValue(IsMethodParameter);

    private sealed record NamedGenericParameterElementTypeValue(IToken Token, bool IsMethodParameter, string Name)
        : GenericParameterElementTypeValue(IsMethodParameter);

    private sealed record TypedefElementTypeValue(IToken Token, string Alias) : ElementTypeValue;

    private sealed record SentinelElementTypeValue(TypeValue Type) : ElementTypeValue;

    private sealed record TypeValue(ElementTypeValue ElementType, ImmutableArray<TypeModifierValue> Modifiers)
    {
        public static TypeValue Error { get; } = new(ElementTypeValue.Error, []);
    }

    private abstract record TypeModifierValue
    {
        public static TypeModifierValue Error { get; } = new ErrorTypeModifierValue();
    }

    private sealed record ErrorTypeModifierValue : TypeModifierValue;

    private enum SimpleTypeModifierKind
    {
        SzArray,
        ByReference,
        Pointer,
        Pinned
    }

    private sealed record SimpleTypeModifierValue(SimpleTypeModifierKind Kind) : TypeModifierValue;

    private sealed record ArrayTypeModifierValue(ImmutableArray<ArrayBoundValue> Bounds) : TypeModifierValue;

    private sealed record CustomTypeModifierValue(TypeSpecificationValue Type, bool IsRequired) : TypeModifierValue;

    private sealed record GenericArgumentsTypeModifierValue(ImmutableArray<TypeValue> Arguments) : TypeModifierValue;

    private sealed record SignatureArgumentValue(
        bool IsSentinel,
        int Attributes,
        TypeValue? Type,
        MarshallingDescriptorValue? Marshalling,
        string? Name)
    {
        public static SignatureArgumentValue Error { get; } = new(false, 0, TypeValue.Error, null, null);
    }

    private sealed record CalliSignatureValue(
        byte CallingConvention,
        TypeValue ReturnType,
        ImmutableArray<SignatureArgumentValue> Arguments)
    {
        public static CalliSignatureValue Error { get; } = new(0, TypeValue.Error, []);
    }

    private abstract record ClassNameValue
    {
        public static ClassNameValue Error { get; } = new ErrorClassNameValue();
    }

    private sealed record ErrorClassNameValue : ClassNameValue;

    private sealed record UnqualifiedClassNameValue(TypeName Name) : ClassNameValue;

    private sealed record AssemblyQualifiedClassNameValue(string AssemblyName, TypeName Name) : ClassNameValue;

    private sealed record ModuleQualifiedClassNameValue(IToken Token, string ModuleName, TypeName Name) : ClassNameValue;

    private sealed record TokenQualifiedClassNameValue(int Token, TypeName Name) : ClassNameValue;

    private sealed record PointerQualifiedClassNameValue(TypeName Name) : ClassNameValue;

    private sealed record TokenClassNameValue(int Token) : ClassNameValue;

    private enum SpecialClassNameKind
    {
        This,
        Base,
        Nester
    }

    private sealed record SpecialClassNameValue(IToken Token, SpecialClassNameKind Kind) : ClassNameValue;

    private abstract record TypeSpecificationValue
    {
        public static TypeSpecificationValue Error { get; } = new ErrorTypeSpecificationValue();
    }

    private sealed record ErrorTypeSpecificationValue : TypeSpecificationValue;

    private sealed record ClassTypeSpecificationValue(ClassNameValue ClassName) : TypeSpecificationValue;

    private sealed record AssemblyTypeSpecificationValue(string AssemblyName) : TypeSpecificationValue;

    private sealed record ModuleTypeSpecificationValue(string ModuleName) : TypeSpecificationValue;

    private sealed record SignatureTypeSpecificationValue(TypeValue Type) : TypeSpecificationValue;

    private abstract record MethodReferenceValue
    {
        public static MethodReferenceValue Error { get; } = new ErrorMethodReferenceValue();
    }

    private sealed record ErrorMethodReferenceValue : MethodReferenceValue;

    private sealed record TokenMethodReferenceValue(int Token) : MethodReferenceValue;

    private sealed record TypedefMethodReferenceValue(IToken Token, string Alias) : MethodReferenceValue;

    private sealed record ParsedMethodReferenceValue(
        IToken Token,
        byte CallingConvention,
        TypeValue ReturnType,
        TypeSpecificationValue? Owner,
        string Name,
        ImmutableArray<TypeValue>? GenericArguments,
        int GenericArity,
        ImmutableArray<SignatureArgumentValue> Arguments) : MethodReferenceValue;

    private abstract record FieldReferenceValue
    {
        public static FieldReferenceValue Error { get; } = new ErrorFieldReferenceValue();
    }

    private sealed record ErrorFieldReferenceValue : FieldReferenceValue;

    private sealed record TypedefFieldReferenceValue(IToken Token, string Alias) : FieldReferenceValue;

    private sealed record ParsedFieldReferenceValue(
        TypeValue FieldType,
        TypeSpecificationValue? Owner,
        string Name) : FieldReferenceValue;

    private abstract record MemberReferenceValue
    {
        public static MemberReferenceValue Error { get; } = new ErrorMemberReferenceValue();
    }

    private sealed record ErrorMemberReferenceValue : MemberReferenceValue;

    private sealed record MethodMemberReferenceValue(MethodReferenceValue Method) : MemberReferenceValue;

    private sealed record FieldMemberReferenceValue(FieldReferenceValue Field) : MemberReferenceValue;

    private sealed record TokenMemberReferenceValue(int Token) : MemberReferenceValue;

    private abstract record OwnerTypeValue
    {
        public static OwnerTypeValue Error { get; } = new ErrorOwnerTypeValue();
    }

    private sealed record ErrorOwnerTypeValue : OwnerTypeValue;

    private sealed record TypeOwnerValue(TypeSpecificationValue Type) : OwnerTypeValue;

    private sealed record MemberOwnerValue(MemberReferenceValue Member) : OwnerTypeValue;
}
