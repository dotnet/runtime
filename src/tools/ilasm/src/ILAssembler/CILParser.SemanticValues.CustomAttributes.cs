// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

public partial class CILParser
{
    public abstract record CustomAttributeDeclarationValue
    {
        public static CustomAttributeDeclarationValue Error { get; } =
            new ErrorCustomAttributeDeclarationValue();
    }

    public sealed record ErrorCustomAttributeDeclarationValue : CustomAttributeDeclarationValue;

    public sealed record CustomAttributeDescriptorValue(
        MethodReferenceValue Constructor,
        CustomAttributeBlobValue Value,
        OwnerTypeValue? Owner) : CustomAttributeDeclarationValue
    {
        public static new CustomAttributeDescriptorValue Error { get; } =
            new(MethodReferenceValue.Error, CustomAttributeBlobValue.Error, null);
    }

    public sealed record CustomAttributeTypedefValue(
        string Alias) : CustomAttributeDeclarationValue;

    public sealed record CustomAttributeApplicationValue(
        CustomAttributeDeclarationValue? Value,
        IToken Location,
        bool HasSyntaxError);

    public abstract record CustomAttributeBlobValue
    {
        public static CustomAttributeBlobValue Error { get; } =
            new ErrorCustomAttributeBlobValue();
    }

    public sealed record ErrorCustomAttributeBlobValue : CustomAttributeBlobValue;

    public sealed record RawCustomAttributeBlobValue(
        BlobBuilder Value) : CustomAttributeBlobValue;

    public sealed record StructuredCustomAttributeBlobValue(
        ImmutableArray<SerializedInitializerValue> Arguments,
        ImmutableArray<CustomAttributeNamedArgumentValue> NamedArguments)
        : CustomAttributeBlobValue;

    public sealed record CustomAttributeNamedArgumentValue(
        byte Kind,
        SerializationTypeValue Type,
        string Name,
        SerializedInitializerValue Value);

    public abstract record SerializationTypeValue
    {
        public static SerializationTypeValue Error { get; } =
            new ErrorSerializationTypeValue();
    }

    public sealed record ErrorSerializationTypeValue : SerializationTypeValue;

    public sealed record RawSerializationTypeValue(
        BlobBuilder Value) : SerializationTypeValue;

    public sealed record SimpleSerializationTypeValue(
        SerializationTypeCode Type) : SerializationTypeValue;

    public sealed record ArraySerializationTypeValue(
        SerializationTypeValue ElementType) : SerializationTypeValue;

    public sealed record StringEnumSerializationTypeValue(
        string Name) : SerializationTypeValue;

    public sealed record ClassEnumSerializationTypeValue(
        ClassNameValue ClassName) : SerializationTypeValue;

    public sealed record TypedefSerializationTypeValue(
        IToken Token,
        string Alias) : SerializationTypeValue;

    public abstract record SerializedInitializerValue(SerializationTypeValue Type)
    {
        public static SerializedInitializerValue Error { get; } =
            new ErrorSerializedInitializerValue();
    }

    public sealed record ErrorSerializedInitializerValue()
        : SerializedInitializerValue(SerializationTypeValue.Error);

    public sealed record RawSerializedInitializerValue(
        SerializationTypeValue Type,
        BlobBuilder Value) : SerializedInitializerValue(Type);

    public sealed record ClassNameSerializedInitializerValue(
        ClassNameValue ClassName)
        : SerializedInitializerValue(
            new SimpleSerializationTypeValue(SerializationTypeCode.Type));

    public sealed record ObjectSerializedInitializerValue(
        SerializedInitializerValue Value)
        : SerializedInitializerValue(
            new SimpleSerializationTypeValue(SerializationTypeCode.TaggedObject));

    public sealed record InvalidByteArraySerializedInitializerValue(
        IToken Token)
        : SerializedInitializerValue(
            new SimpleSerializationTypeValue(SerializationTypeCode.String));

    public sealed record ArraySerializedInitializerValue(
        SerializationTypeValue Type,
        int Length,
        SerializedSequenceValue Values) : SerializedInitializerValue(Type);

    public abstract record SerializedSequenceValue;

    public sealed record RawSerializedSequenceValue(
        BlobBuilder Value) : SerializedSequenceValue;

    public sealed record ClassSerializedSequenceValue(
        ImmutableArray<ClassSequenceElementValue> Values) : SerializedSequenceValue;

    public sealed record ObjectSerializedSequenceValue(
        ImmutableArray<SerializedInitializerValue> Values) : SerializedSequenceValue;

    public abstract record ClassSequenceElementValue
    {
        public static ClassSequenceElementValue Error { get; } =
            new ErrorClassSequenceElementValue();
    }

    public sealed record ErrorClassSequenceElementValue : ClassSequenceElementValue;

    public sealed record StringClassSequenceElementValue(
        string? Value) : ClassSequenceElementValue;

    public sealed record TypeClassSequenceElementValue(
        ClassNameValue ClassName) : ClassSequenceElementValue;

    public sealed record FieldInitializerValue(bool HasValue, object? ConstantValue)
    {
        public static FieldInitializerValue Empty { get; } = new(false, null);
    }
}
