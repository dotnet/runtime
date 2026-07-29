// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Reflection.Metadata;

namespace ILAssembler;

internal static partial class PseudoCustomAttributes
{

    private readonly record struct EncodedArgumentType(
        SerializationTypeCode Type,
        SerializationTypeCode ArrayType = SerializationTypeCode.Invalid,
        SerializationTypeCode EnumUnderlyingType = SerializationTypeCode.Invalid,
        string? EnumName = null);

    private static EncodedArgumentType ReadEncodedArgumentType(ref BlobReader reader)
    {
        // The native CustomAttributeParser::GetTag method consumes one byte rather than a
        // compressed integer, so use BlobReader.ReadByte instead of ReadSerializationTypeCode.
        SerializationTypeCode type = (SerializationTypeCode)reader.ReadByte();
        SerializationTypeCode arrayType = SerializationTypeCode.Invalid;
        if (type == SerializationTypeCode.SZArray)
        {
            arrayType = (SerializationTypeCode)reader.ReadByte();
        }

        SerializationTypeCode effectiveType =
            type == SerializationTypeCode.SZArray ? arrayType : type;
        string? enumName = null;
        if (effectiveType == SerializationTypeCode.Enum)
        {
            enumName = reader.ReadSerializedString();
            if (enumName is null)
            {
                throw new BadImageFormatException();
            }
        }

        return new(type, arrayType, EnumName: enumName);
    }

    private static CustomAttributeTypedArgument<SerializationTypeCode> ReadArgument(
        ref BlobReader reader,
        SerializationTypeCode type,
        SerializationTypeCode enumUnderlyingType = SerializationTypeCode.Invalid)
    {
        SerializationTypeCode effectiveType =
            type == SerializationTypeCode.Enum ? enumUnderlyingType : type;

        object? value = effectiveType switch
        {
            SerializationTypeCode.Boolean => reader.ReadBoolean(),
            SerializationTypeCode.SByte => reader.ReadSByte(),
            SerializationTypeCode.Byte => reader.ReadByte(),
            SerializationTypeCode.Char => reader.ReadChar(),
            SerializationTypeCode.Int16 => reader.ReadInt16(),
            SerializationTypeCode.UInt16 => reader.ReadUInt16(),
            SerializationTypeCode.Int32 => reader.ReadInt32(),
            SerializationTypeCode.UInt32 => reader.ReadUInt32(),
            SerializationTypeCode.Int64 => reader.ReadInt64(),
            SerializationTypeCode.UInt64 => reader.ReadUInt64(),
            SerializationTypeCode.Single => reader.ReadSingle(),
            SerializationTypeCode.Double => reader.ReadDouble(),
            SerializationTypeCode.String or SerializationTypeCode.Type => reader.ReadSerializedString(),
            _ => throw new BadImageFormatException(),
        };

        return new(type, value);
    }

    private static unsafe bool TryParseArguments(
        LoweringContext context,
        KnownAttribute known,
        out CustomAttributeValue<SerializationTypeCode> arguments)
    {
        // The native emitter does not look at the blob at all when the attribute has neither
        // fixed nor named arguments, so a malformed blob is tolerated for those attributes.
        if (known.FixedArguments.Length == 0 && known.NamedArguments.Length == 0)
        {
            arguments = new(
                ImmutableArray<CustomAttributeTypedArgument<SerializationTypeCode>>.Empty,
                ImmutableArray<CustomAttributeNamedArgument<SerializationTypeCode>>.Empty);
            return true;
        }

        byte[] blob = context.Attribute.Value.ToArray();
        fixed (byte* blobPointer = blob)
        {
            var reader = new BlobReader(blobPointer, blob.Length);
            try
            {
                if (reader.ReadUInt16() != 0x0001)
                {
                    arguments = default;
                    return context.InvalidBlob();
                }

                var fixedArguments =
                    ImmutableArray.CreateBuilder<CustomAttributeTypedArgument<SerializationTypeCode>>(
                        known.FixedArguments.Length);
                foreach (SerializationTypeCode type in known.FixedArguments)
                {
                    fixedArguments.Add(ReadArgument(ref reader, type));
                }

                ImmutableArray<CustomAttributeNamedArgument<SerializationTypeCode>> namedArguments;
                if (known.NamedArguments.Length == 0 && reader.RemainingBytes == 0)
                {
                    namedArguments = ImmutableArray<CustomAttributeNamedArgument<SerializationTypeCode>>.Empty;
                }
                else if (!TryParseNamedArguments(context, known, ref reader, out namedArguments))
                {
                    arguments = default;
                    return false;
                }

                arguments = new(fixedArguments.MoveToImmutable(), namedArguments);
                return true;
            }
            catch (BadImageFormatException)
            {
                arguments = default;
                return context.InvalidBlob();
            }
        }
    }

    private static bool TryParseNamedArguments(
        LoweringContext context,
        KnownAttribute known,
        ref BlobReader reader,
        out ImmutableArray<CustomAttributeNamedArgument<SerializationTypeCode>> namedArguments)
    {
        // A missing count is treated as "no named arguments" rather than an error, matching the
        // native emitter's documented Everett-compatible behavior.
        if (reader.RemainingBytes < sizeof(ushort))
        {
            namedArguments = ImmutableArray<CustomAttributeNamedArgument<SerializationTypeCode>>.Empty;
            return true;
        }

        ushort actualCount = reader.ReadUInt16();
        var arguments =
            ImmutableArray.CreateBuilder<CustomAttributeNamedArgument<SerializationTypeCode>>(
                Math.Min(actualCount, (ushort)known.NamedArguments.Length));
        var seenArguments = new bool[known.NamedArguments.Length];

        // The count is deliberately read as a signed 16-bit value: the native emitter stores it in
        // an INT16 and compares against a wider signed loop counter, so a count with the high bit
        // set yields no named arguments rather than an error.
        for (int i = 0; i < (short)actualCount; i++)
        {
            var kind = (CustomAttributeNamedArgumentKind)reader.ReadByte();
            if (kind is not (CustomAttributeNamedArgumentKind.Field or CustomAttributeNamedArgumentKind.Property))
            {
                namedArguments = default;
                return context.InvalidBlob();
            }

            EncodedArgumentType actual = ReadEncodedArgumentType(ref reader);
            string? argumentName = reader.ReadSerializedString();
            if (string.IsNullOrEmpty(argumentName))
            {
                namedArguments = default;
                return context.InvalidBlob();
            }

            int match = -1;
            for (int candidate = 0; candidate < known.NamedArguments.Length; candidate++)
            {
                NamedArgument descriptor = known.NamedArguments[candidate];

                if (descriptor.Type != SerializationTypeCode.TaggedObject)
                {
                    if (actual.Type != descriptor.Type)
                    {
                        continue;
                    }

                    if (actual.Type == SerializationTypeCode.SZArray
                        && descriptor.ArrayType != SerializationTypeCode.TaggedObject
                        && actual.ArrayType != descriptor.ArrayType)
                    {
                        continue;
                    }
                }

                if (descriptor.Name != argumentName)
                {
                    continue;
                }

                if (descriptor.Type == SerializationTypeCode.Enum
                    || (descriptor.Type == SerializationTypeCode.SZArray && descriptor.ArrayType == SerializationTypeCode.Enum))
                {
                    if (!EnumNameMatches(descriptor.EnumName, actual.EnumName))
                    {
                        continue;
                    }

                    actual = actual with { EnumUnderlyingType = descriptor.EnumType };
                }

                match = candidate;
                break;
            }

            if (match < 0)
            {
                namedArguments = default;
                return context.UnknownArgument(argumentName);
            }

            if (seenArguments[match])
            {
                namedArguments = default;
                return context.RepeatedArgument(argumentName);
            }

            seenArguments[match] = true;
            CustomAttributeTypedArgument<SerializationTypeCode> value =
                ReadArgument(ref reader, actual.Type, actual.EnumUnderlyingType);
            arguments.Add(new(argumentName, kind, value.Type, value.Value));
        }

        namedArguments = arguments.ToImmutable();
        return true;
    }

    private static CustomAttributeNamedArgument<SerializationTypeCode>? FindNamedArgument(
        CustomAttributeValue<SerializationTypeCode> arguments,
        string name)
    {
        foreach (CustomAttributeNamedArgument<SerializationTypeCode> argument in arguments.NamedArguments)
        {
            if (argument.Name == name)
            {
                return argument;
            }
        }

        return null;
    }

    private static short GetInt16(object? value) => value switch
    {
        short signed => signed,
        ushort unsigned => unchecked((short)unsigned),
        _ => throw new BadImageFormatException(),
    };

    private static ushort GetUInt16(object? value) => value switch
    {
        short signed => unchecked((ushort)signed),
        ushort unsigned => unsigned,
        int signed => unchecked((ushort)signed),
        uint unsigned => unchecked((ushort)unsigned),
        _ => throw new BadImageFormatException(),
    };

    private static int GetInt32(object? value) => value switch
    {
        short signed => unchecked((ushort)signed),
        ushort unsigned => unsigned,
        int signed => signed,
        uint unsigned => unchecked((int)unsigned),
        _ => throw new BadImageFormatException(),
    };

    private static uint GetUInt32(object? value) => value switch
    {
        short signed => unchecked((ushort)signed),
        ushort unsigned => unsigned,
        int signed => unchecked((uint)signed),
        uint unsigned => unsigned,
        _ => throw new BadImageFormatException(),
    };

    private static bool GetBoolean(object? value) =>
        value is bool boolean ? boolean : throw new BadImageFormatException();

    private static string GetString(object? value) => value as string ?? "";

    /// <summary>
    /// Matches an enum type name against a descriptor name, allowing the blob to carry an
    /// assembly-qualified name whose namespace-qualified prefix matches.
    /// </summary>
    private static bool EnumNameMatches(string descriptorName, string? actualName)
    {
        if (actualName is null || descriptorName.Length > actualName.Length)
        {
            return false;
        }

        if (!actualName.AsSpan(0, descriptorName.Length).SequenceEqual(descriptorName))
        {
            return false;
        }

        return descriptorName.Length == actualName.Length || actualName[descriptorName.Length] == ',';
    }
}
