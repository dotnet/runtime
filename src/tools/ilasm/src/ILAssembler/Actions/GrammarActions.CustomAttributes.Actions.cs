// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Immutable;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal MethodReferenceValue CreateCustomAttributeType(MethodReferenceValue constructor)
        => constructor;

    internal CustomAttributeDescriptorValue CreateDefaultCustomAttribute(
        MethodReferenceValue constructor)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateDefaultCustomAttributeBlob()),
            null);

    internal CustomAttributeDescriptorValue CreateStringCustomAttribute(
        MethodReferenceValue constructor,
        string value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateStringBlob(value)),
            null);

    internal CustomAttributeDescriptorValue CreateStructuredCustomAttribute(
        MethodReferenceValue constructor,
        CustomAttributeBlobValue value)
        => CreateCustomAttributeDescriptor(
            constructor,
            value,
            null);

    internal CustomAttributeDescriptorValue CreateRawCustomAttribute(
        MethodReferenceValue constructor,
        ImmutableArray<byte> value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateRawBlob(value)),
            null);

    internal CustomAttributeDescriptorValue CreateDefaultOwnedCustomAttribute(
        OwnerTypeValue owner,
        MethodReferenceValue constructor)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateDefaultCustomAttributeBlob()),
            owner);

    internal CustomAttributeDescriptorValue CreateStringOwnedCustomAttribute(
        OwnerTypeValue owner,
        MethodReferenceValue constructor,
        string value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateStringBlob(value)),
            owner);

    internal CustomAttributeDescriptorValue CreateStructuredOwnedCustomAttribute(
        OwnerTypeValue owner,
        MethodReferenceValue constructor,
        CustomAttributeBlobValue value)
        => CreateCustomAttributeDescriptor(
            constructor,
            value,
            owner);

    internal CustomAttributeDescriptorValue CreateRawOwnedCustomAttribute(
        OwnerTypeValue owner,
        MethodReferenceValue constructor,
        ImmutableArray<byte> value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateRawBlob(value)),
            owner);

    internal CustomAttributeDeclarationValue CreateCustomAttributeDeclaration(
        CustomAttributeDescriptorValue value)
        => value;

    internal CustomAttributeDeclarationValue CreateCustomAttributeTypedef(string alias)
        => new CustomAttributeTypedefValue(alias);

    private static CustomAttributeDescriptorValue CreateCustomAttributeDescriptor(
        MethodReferenceValue constructor,
        CustomAttributeBlobValue value,
        OwnerTypeValue? owner)
        => new(constructor, value, owner);

    private static BlobBuilder CreateStringBlob(string value)
    {
        BlobBuilder blob = new();
        blob.WriteUTF8(value);
        return blob;
    }

    private static BlobBuilder CreateRawBlob(ImmutableArray<byte> value)
    {
        BlobBuilder blob = new(value.Length);
        blob.WriteBytes(value);
        return blob;
    }

    private static BlobBuilder CreateDefaultCustomAttributeBlob()
    {
        BlobBuilder value = new();
        value.WriteUInt16(CustomAttributeBlobFormatVersion);
        value.WriteUInt16(0);
        return value;
    }

    internal CustomAttributeBlobValue CreateCustomAttributeBlob(
        ImmutableArray<SerializedInitializerValue> arguments,
        ImmutableArray<CustomAttributeNamedArgumentValue> namedArguments)
        => new StructuredCustomAttributeBlobValue(arguments, namedArguments);

    internal CustomAttributeNamedArgumentValue CreateCustomBlobNamedArgument(
        byte kind,
        SerializationTypeValue type,
        string name,
        SerializedInitializerValue value)
        => new(kind, type, name, value);

    private BlobBuilder MaterializeCustomAttributeBlob(CustomAttributeBlobValue value)
    {
        if (value is RawCustomAttributeBlobValue raw)
        {
            return raw.Value;
        }

        if (value is not StructuredCustomAttributeBlobValue structured)
        {
            return new BlobBuilder();
        }

        BlobBuilder result = new();
        result.WriteUInt16(CustomAttributeBlobFormatVersion);
        foreach (SerializedInitializerValue argument in structured.Arguments)
        {
            MaterializeSerializedInitializer(argument).WriteContentTo(result);
        }

        WriteCustomBlobNamedArguments(result, structured.NamedArguments);
        return result;
    }

    private void WriteCustomBlobNamedArguments(
        BlobBuilder result,
        ImmutableArray<CustomAttributeNamedArgumentValue> namedArguments)
    {
        result.WriteInt16((short)namedArguments.Length);
        foreach (CustomAttributeNamedArgumentValue argument in namedArguments)
        {
            result.WriteByte(argument.Kind);
            MaterializeSerializationType(argument.Type).WriteContentTo(result);
            result.WriteSerializedString(argument.Name);
            MaterializeSerializedInitializer(argument.Value).WriteContentTo(result);
        }
    }

    private EntityRegistry.CustomAttributeEntity MaterializeCustomAttribute(
        CustomAttributeDescriptorValue descriptor,
        IToken location)
    {
        EntityRegistry.EntityBase constructor = MaterializeMethodReference(descriptor.Constructor);
        BlobBuilder value = MaterializeCustomAttributeBlob(descriptor.Value);
        EntityRegistry.CustomAttributeEntity attribute =
            _entityRegistry.CreateCustomAttribute(constructor, value);
        attribute.Location = Location.From(location, _documents);
        if (descriptor.Owner is { } owner)
        {
            attribute.Owner = MaterializeOwnerType(owner);
        }

        return attribute;
    }

    private EntityRegistry.CustomAttributeEntity? MaterializeCustomAttributeDeclaration(
        CustomAttributeDeclarationValue? value,
        IToken location)
    {
        if (value is CustomAttributeTypedefValue typedef)
        {
            if (TryResolveTypedefAsCustomAttribute(typedef.Alias) is not { } resolved)
            {
                return null;
            }

            EntityRegistry.CustomAttributeEntity typedefAttribute =
                _entityRegistry.CreateCustomAttribute(resolved.Constructor, resolved.Value);
            typedefAttribute.Location = Location.From(location, _documents);
            return typedefAttribute;
        }

        if (value is not CustomAttributeDescriptorValue descriptor)
        {
            return null;
        }

        EntityRegistry.CustomAttributeEntity attribute = MaterializeCustomAttribute(descriptor, location);
        return descriptor.Owner is null ? attribute : null;
    }

    internal EntityRegistry.CustomAttributeEntity? MaterializeCustomAttributeDeclaration(
        CILParser.CustomAttrDeclContext context)
        => MaterializeCustomAttributeDeclaration(context.Value, context.Start);

    internal EntityRegistry.CustomAttributeEntity MaterializeCustomAttributeDescriptor(
        CILParser.CustomDescrContext context)
        => MaterializeCustomAttribute(context.Value, context.Start);

    internal EntityRegistry.CustomAttributeEntity? MaterializeMethodBodyCustomAttributeDeclaration(
        CILParser.CustomDescrInMethodBodyContext context)
        => MaterializeCustomAttributeDeclaration(context.Value, context.Start);

    internal EntityRegistry.EntityBase MaterializeOwnerType(
        CILParser.OwnerTypeContext context)
        => MaterializeOwnerType(context.Value);
}
