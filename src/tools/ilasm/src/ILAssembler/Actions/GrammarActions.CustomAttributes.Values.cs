// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

internal sealed partial class GrammarActions
{
    private sealed record CustomAttributeDescriptorValue(
        MethodReferenceValue Constructor,
        CustomAttributeBlobValue Value,
        OwnerTypeValue? Owner);

    private sealed record CustomAttributeTypedefValue(string Alias);

    private abstract record CustomAttributeBlobValue;

    private sealed record RawCustomAttributeBlobValue(BlobBuilder Value)
        : CustomAttributeBlobValue;

    private sealed record StructuredCustomAttributeBlobValue(
        ImmutableArray<SerializedInitializerValue> Arguments,
        ImmutableArray<CustomAttributeNamedArgumentValue> NamedArguments)
        : CustomAttributeBlobValue;

    private sealed record CustomAttributeNamedArgumentValue(
        byte Kind,
        SerializationTypeValue Type,
        string Name,
        SerializedInitializerValue Value);

    private abstract record SerializationTypeValue;

    private sealed record RawSerializationTypeValue(BlobBuilder Value)
        : SerializationTypeValue;

    private sealed record SimpleSerializationTypeValue(SerializationTypeCode Type)
        : SerializationTypeValue;

    private sealed record ArraySerializationTypeValue(SerializationTypeValue ElementType)
        : SerializationTypeValue;

    private sealed record StringEnumSerializationTypeValue(string Name)
        : SerializationTypeValue;

    private sealed record ClassEnumSerializationTypeValue(ClassNameValue ClassName)
        : SerializationTypeValue;

    private sealed record TypedefSerializationTypeValue(IToken Token, string Alias)
        : SerializationTypeValue;

    private abstract record SerializedInitializerValue(SerializationTypeValue Type);

    private sealed record RawSerializedInitializerValue(
        SerializationTypeValue Type,
        BlobBuilder Value)
        : SerializedInitializerValue(Type);

    private sealed record ClassNameSerializedInitializerValue(ClassNameValue ClassName)
        : SerializedInitializerValue(new SimpleSerializationTypeValue(SerializationTypeCode.Type));

    private sealed record ObjectSerializedInitializerValue(SerializedInitializerValue Value)
        : SerializedInitializerValue(
            new SimpleSerializationTypeValue(SerializationTypeCode.TaggedObject));

    private sealed record InvalidByteArraySerializedInitializerValue(IToken Token)
        : SerializedInitializerValue(
            new SimpleSerializationTypeValue(SerializationTypeCode.String));

    private sealed record ArraySerializedInitializerValue(
        SerializationTypeValue Type,
        int Length,
        SerializedSequenceValue Values)
        : SerializedInitializerValue(Type);

    private abstract record SerializedSequenceValue;

    private sealed record RawSerializedSequenceValue(BlobBuilder Value)
        : SerializedSequenceValue;

    private sealed record ClassSerializedSequenceValue(
        ImmutableArray<ClassSequenceElementValue> Values)
        : SerializedSequenceValue;

    private sealed record ObjectSerializedSequenceValue(
        ImmutableArray<SerializedInitializerValue> Values)
        : SerializedSequenceValue;

    private abstract record ClassSequenceElementValue;

    private sealed record StringClassSequenceElementValue(string? Value)
        : ClassSequenceElementValue;

    private sealed record TypeClassSequenceElementValue(ClassNameValue ClassName)
        : ClassSequenceElementValue;

    private static CustomAttributeDescriptorValue GetCustomAttributeDescriptorValue(object? value)
        => value as CustomAttributeDescriptorValue
            ?? new(
                MethodReferenceValue.Error,
                new RawCustomAttributeBlobValue(CreateDefaultCustomAttributeBlob()),
                null);

    private static SerializedInitializerValue GetSerializedInitializerValue(object? value)
        => value as SerializedInitializerValue
            ?? new RawSerializedInitializerValue(
                new RawSerializationTypeValue(new BlobBuilder()),
                new BlobBuilder());

    private static SerializationTypeValue GetSerializationTypeValue(object? value)
        => value as SerializationTypeValue
            ?? new RawSerializationTypeValue(new BlobBuilder());

    private static SerializedSequenceValue GetSerializedSequenceValue(object? value)
        => value switch
        {
            SerializedSequenceValue sequence => sequence,
            BlobBuilder blob => new RawSerializedSequenceValue(blob),
            _ => new RawSerializedSequenceValue(new BlobBuilder())
        };

    private static BlobBuilder CreateDefaultCustomAttributeBlob()
    {
        BlobBuilder value = new();
        value.WriteUInt16(CustomAttributeBlobFormatVersion);
        value.WriteUInt16(0);
        return value;
    }
}
