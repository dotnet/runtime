// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    private readonly Stack<CustomBlobArgumentsFrame> _customBlobArgumentFrames = new();
    private readonly Stack<CustomBlobNamedArgumentsFrame> _customBlobNamedArgumentFrames = new();

    private sealed class CustomBlobArgumentsFrame
    {
        public CustomBlobArgumentsFrame(CILParser.CustomBlobArgsContext owner)
        {
            Owner = owner;
        }

        public CILParser.CustomBlobArgsContext Owner { get; }

        public ImmutableArray<SerializedInitializerValue>.Builder? Values { get; set; }
    }

    private sealed class CustomBlobNamedArgumentsFrame
    {
        public CustomBlobNamedArgumentsFrame(CILParser.CustomBlobNVPairsContext owner)
        {
            Owner = owner;
        }

        public CILParser.CustomBlobNVPairsContext Owner { get; }

        public ImmutableArray<CustomAttributeNamedArgumentValue>.Builder? Values { get; set; }
    }

    internal object CreateCustomAttributeType(object? constructor)
        => GetMethodReferenceValue(constructor);

    internal object CreateDefaultCustomAttribute(object? constructor)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateDefaultCustomAttributeBlob()),
            null);

    internal object CreateStringCustomAttribute(object? constructor, string value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateStringBlob(value)),
            null);

    internal object CreateStructuredCustomAttribute(object? constructor, object? value)
        => CreateCustomAttributeDescriptor(
            constructor,
            value as CustomAttributeBlobValue
                ?? new RawCustomAttributeBlobValue(new BlobBuilder()),
            null);

    internal object CreateRawCustomAttribute(object? constructor, ImmutableArray<byte> value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateRawBlob(value)),
            null);

    internal object CreateDefaultOwnedCustomAttribute(object? owner, object? constructor)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateDefaultCustomAttributeBlob()),
            GetOwnerTypeValue(owner));

    internal object CreateStringOwnedCustomAttribute(object? owner, object? constructor, string value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateStringBlob(value)),
            GetOwnerTypeValue(owner));

    internal object CreateStructuredOwnedCustomAttribute(
        object? owner,
        object? constructor,
        object? value)
        => CreateCustomAttributeDescriptor(
            constructor,
            value as CustomAttributeBlobValue
                ?? new RawCustomAttributeBlobValue(new BlobBuilder()),
            GetOwnerTypeValue(owner));

    internal object CreateRawOwnedCustomAttribute(
        object? owner,
        object? constructor,
        ImmutableArray<byte> value)
        => CreateCustomAttributeDescriptor(
            constructor,
            new RawCustomAttributeBlobValue(CreateRawBlob(value)),
            GetOwnerTypeValue(owner));

    internal object CreateCustomAttributeDeclaration(object? value)
        => GetCustomAttributeDescriptorValue(value);

    internal object CreateCustomAttributeTypedef(string alias)
        => new CustomAttributeTypedefValue(alias);

    private static CustomAttributeDescriptorValue CreateCustomAttributeDescriptor(
        object? constructor,
        CustomAttributeBlobValue value,
        OwnerTypeValue? owner)
        => new(GetMethodReferenceValue(constructor), value, owner);

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

    internal object CreateCustomAttributeBlob(object? arguments, object? namedArguments)
        => new StructuredCustomAttributeBlobValue(
            arguments is ImmutableArray<SerializedInitializerValue> argumentValues
                ? argumentValues
                : [],
            namedArguments is ImmutableArray<CustomAttributeNamedArgumentValue> namedArgumentValues
                ? namedArgumentValues
                : []);

    internal void BeginCustomBlobArguments(CILParser.CustomBlobArgsContext context)
        => _customBlobArgumentFrames.Push(new(context));

    internal void AddCustomBlobArgument(CILParser.CustomBlobArgsContext context, object? value)
    {
        if (TryGetCustomBlobArgumentsFrame(context) is { } frame)
        {
            (frame.Values ??=
                ImmutableArray.CreateBuilder<SerializedInitializerValue>())
                .Add(GetSerializedInitializerValue(value));
        }
    }

    internal object EndCustomBlobArguments(CILParser.CustomBlobArgsContext context)
    {
        if (TryGetCustomBlobArgumentsFrame(context) is not { } frame)
        {
            return ImmutableArray<SerializedInitializerValue>.Empty;
        }

        _customBlobArgumentFrames.Pop();
        return frame.Values?.ToImmutable() ?? ImmutableArray<SerializedInitializerValue>.Empty;
    }

    internal void BeginCustomBlobNamedArguments(CILParser.CustomBlobNVPairsContext context)
        => _customBlobNamedArgumentFrames.Push(new(context));

    internal void AddCustomBlobNamedArgument(
        CILParser.CustomBlobNVPairsContext context,
        byte kind,
        object? type,
        string name,
        object? value)
    {
        if (TryGetCustomBlobNamedArgumentsFrame(context) is not { } frame)
        {
            return;
        }

        (frame.Values ??=
            ImmutableArray.CreateBuilder<CustomAttributeNamedArgumentValue>())
            .Add(new(
                kind,
                GetSerializationTypeValue(type),
                name,
                GetSerializedInitializerValue(value)));
    }

    internal object EndCustomBlobNamedArguments(CILParser.CustomBlobNVPairsContext context)
    {
        if (TryGetCustomBlobNamedArgumentsFrame(context) is not { } frame)
        {
            return ImmutableArray<CustomAttributeNamedArgumentValue>.Empty;
        }

        _customBlobNamedArgumentFrames.Pop();
        return frame.Values?.ToImmutable()
            ?? ImmutableArray<CustomAttributeNamedArgumentValue>.Empty;
    }

    private CustomBlobArgumentsFrame? TryGetCustomBlobArgumentsFrame(
        CILParser.CustomBlobArgsContext context)
    {
        Debug.Assert(_customBlobArgumentFrames.Count > 0);
        CustomBlobArgumentsFrame? frame =
            _customBlobArgumentFrames.Count == 0 ? null : _customBlobArgumentFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private CustomBlobNamedArgumentsFrame? TryGetCustomBlobNamedArgumentsFrame(
        CILParser.CustomBlobNVPairsContext context)
    {
        Debug.Assert(_customBlobNamedArgumentFrames.Count > 0);
        CustomBlobNamedArgumentsFrame? frame =
            _customBlobNamedArgumentFrames.Count == 0 ? null : _customBlobNamedArgumentFrames.Peek();
        Debug.Assert(frame is null || ReferenceEquals(frame.Owner, context));
        return frame is not null && ReferenceEquals(frame.Owner, context) ? frame : null;
    }

    private BlobBuilder MaterializeCustomAttributeBlob(CustomAttributeBlobValue value)
    {
        if (value is RawCustomAttributeBlobValue raw)
        {
            return raw.Value;
        }

        StructuredCustomAttributeBlobValue structured =
            (StructuredCustomAttributeBlobValue)value;
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
        object? value,
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
        => MaterializeCustomAttribute(
            GetCustomAttributeDescriptorValue(context.Value),
            context.Start);

    internal EntityRegistry.CustomAttributeEntity? MaterializeMethodBodyCustomAttributeDeclaration(
        CILParser.CustomDescrInMethodBodyContext context)
        => MaterializeCustomAttributeDeclaration(context.Value, context.Start);

    internal EntityRegistry.EntityBase MaterializeOwnerType(
        CILParser.OwnerTypeContext context)
        => MaterializeOwnerType(GetOwnerTypeValue(context.Value));
}
