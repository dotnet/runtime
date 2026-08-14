// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reflection.Metadata;
using System.Text;
using Antlr4.Runtime;

namespace ILAssembler;

#pragma warning disable CA1822 // Parser actions are invoked through the per-parser GrammarActions instance.
internal sealed partial class GrammarActions
{
    internal byte GetCustomAttributeNamedArgumentKind(IToken token)
        => (byte)(token.Text == "field"
            ? CustomAttributeNamedArgumentKind.Field
            : CustomAttributeNamedArgumentKind.Property);

    internal SerializationTypeValue CreateSerializationType(
        SerializationTypeValue element,
        IToken? array)
    {
        return array is null ? element : new ArraySerializationTypeValue(element);
    }

    internal SerializationTypeValue CreatePrimitiveSerializationType(byte type)
        => new SimpleSerializationTypeValue((SerializationTypeCode)type);

    internal SerializationTypeValue CreateSerializationTypeTypedef(
        CILParser.SerializTypeElementContext context,
        string alias)
        => new TypedefSerializationTypeValue(context.Start, alias);

    internal SerializationTypeValue CreateSimpleSerializationType(IToken type)
        => new SimpleSerializationTypeValue(GetSerializationTypeCode(type.Type));

    internal SerializationTypeValue CreateEnumSerializationType(IToken name)
        => new StringEnumSerializationTypeValue(StringHelpers.ParseQuotedString(name.Text));

    internal SerializationTypeValue CreateEnumSerializationType(ClassNameValue className)
        => new ClassEnumSerializationTypeValue(className);

    internal BlobBuilder CreateFloat32SerializedInitializer(
        CILParser.Float64Context context,
        double value)
    {
        float serializedValue = IsPlainInteger(context) &&
            ParseIntegerValue(context.Start.Text.AsSpan(), out long rawValue)
                ? BitConverter.Int32BitsToSingle((int)rawValue)
                : (float)value;
        BlobBuilder blob = CreateSerializedInitializer(SerializationTypeCode.Single);
        blob.WriteSingle(serializedValue);
        return blob;
    }

    internal BlobBuilder CreateFloat64SerializedInitializer(
        CILParser.Float64Context context,
        double value)
    {
        double serializedValue = IsPlainInteger(context) &&
            ParseIntegerValue(context.Start.Text.AsSpan(), out long rawValue)
                ? BitConverter.Int64BitsToDouble(rawValue)
                : value;
        BlobBuilder blob = CreateSerializedInitializer(SerializationTypeCode.Double);
        blob.WriteDouble(serializedValue);
        return blob;
    }

    private static bool IsPlainInteger(CILParser.Float64Context context)
        => context.Start.Type == CILParser.INT32 &&
            context.Stop is { Type: CILParser.INT32 };

    internal BlobBuilder CreateFloat32BitsSerializedInitializer(IToken value)
    {
        BlobBuilder blob = CreateSerializedInitializer(SerializationTypeCode.Single);
        blob.WriteSingle(BitConverter.Int32BitsToSingle(ParseInt32(value)));
        return blob;
    }

    internal BlobBuilder CreateFloat64BitsSerializedInitializer(IToken value)
    {
        BlobBuilder blob = CreateSerializedInitializer(SerializationTypeCode.Double);
        blob.WriteDouble(BitConverter.Int64BitsToDouble(ParseInt64(value)));
        return blob;
    }

    internal BlobBuilder CreateIntegerSerializedInitializer(IToken type, IToken value)
    {
        BlobBuilder blob = CreateSerializedInitializer(GetSerializationTypeCode(type.Type));
        switch (type.Type)
        {
            case CILParser.INT8:
            case CILParser.UINT8:
                blob.WriteByte((byte)ParseInt32(value));
                break;
            case CILParser.CHAR:
            case CILParser.INT16:
            case CILParser.UINT16:
                blob.WriteInt16((short)ParseInt32(value));
                break;
            case CILParser.INT32_:
            case CILParser.UINT32:
                blob.WriteInt32(ParseInt32(value));
                break;
            case CILParser.INT64_:
            case CILParser.UINT64:
                blob.WriteInt64(ParseInt64(value));
                break;
            default:
                throw new UnreachableException();
        }

        return blob;
    }

    internal BlobBuilder CreateBooleanSerializedInitializer(IToken type, bool value)
    {
        Debug.Assert(type.Type == CILParser.BOOL);
        BlobBuilder blob = CreateSerializedInitializer(SerializationTypeCode.Boolean);
        blob.WriteBoolean(value);
        return blob;
    }

    internal BlobBuilder CreateByteArraySerializedInitializer(ImmutableArray<byte> value)
    {
        BlobBuilder blob = CreateSerializedInitializer(
            SerializationTypeCode.String,
            value.Length + 1);
        blob.WriteBytes(value);
        return blob;
    }

    private static BlobBuilder CreateSerializedInitializer(
        SerializationTypeCode type,
        int capacity = 9)
    {
        BlobBuilder blob = new(capacity);
        blob.WriteByte((byte)type);
        return blob;
    }

    internal FieldInitializerValue CreateFieldInitializer(BlobBuilder value)
        => new(true, ExtractConstantFromSerInit(value));

    internal FieldInitializerValue CreateFieldInitializer(string value)
        => new(true, value);

    internal FieldInitializerValue CreateNullFieldInitializer()
        => new(true, null);

    internal SerializedInitializerValue CreateScalarSerializedValue(
        CILParser.SerInitContext context,
        CILParser.FieldSerInitContext initializer,
        BlobBuilder value)
    {
        if (initializer.Start.Text == "bytearray")
        {
            return new InvalidByteArraySerializedInitializerValue(context.Start);
        }

        ImmutableArray<byte> encodedValue = value.ToImmutableArray();
        BlobBuilder serializedValue = new(Math.Max(0, encodedValue.Length - 1));
        if (encodedValue.Length > 1)
        {
            serializedValue.WriteBytes(encodedValue.AsSpan().Slice(1).ToArray());
        }

        SerializationTypeValue type = encodedValue.Length == 0
            ? new RawSerializationTypeValue(new BlobBuilder())
            : new SimpleSerializationTypeValue((SerializationTypeCode)encodedValue[0]);
        return new RawSerializedInitializerValue(type, serializedValue);
    }

    internal SerializedInitializerValue CreateStringSerializedValue()
        => CreateSerializedStringValue(SerializationTypeCode.String, null);

    internal SerializedInitializerValue CreateStringSerializedValue(IToken value)
        => CreateSerializedStringValue(
            SerializationTypeCode.String,
            StringHelpers.ParseQuotedString(value.Text));

    internal SerializedInitializerValue CreateTypeSerializedValue(IToken value)
        => CreateSerializedStringValue(
            SerializationTypeCode.Type,
            StringHelpers.ParseQuotedString(value.Text));

    internal SerializedInitializerValue CreateTypeSerializedValue(ClassNameValue className)
        => new ClassNameSerializedInitializerValue(className);

    internal SerializedInitializerValue CreateNullTypeSerializedValue()
        => CreateSerializedStringValue(SerializationTypeCode.Type, null);

    private static RawSerializedInitializerValue CreateSerializedStringValue(
        SerializationTypeCode type,
        string? value)
    {
        BlobBuilder serializedValue = new();
        serializedValue.WriteSerializedString(value);
        return new RawSerializedInitializerValue(
            new SimpleSerializationTypeValue(type),
            serializedValue);
    }

    internal SerializedInitializerValue CreateObjectSerializedValue(
        SerializedInitializerValue value)
        => new ObjectSerializedInitializerValue(value);

    internal SerializedInitializerValue CreateArraySerializedValue(
        IToken elementType,
        IToken length,
        SerializedSequenceValue values)
        => new ArraySerializedInitializerValue(
            new ArraySerializationTypeValue(
                new SimpleSerializationTypeValue(GetSerializationTypeCode(elementType.Type))),
            ParseInt32(length),
            values);

    internal SerializedInitializerValue CreateArraySerializedValue(
        IToken elementType,
        IToken length,
        BlobBuilder values)
        => CreateArraySerializedValue(
            elementType,
            length,
            new RawSerializedSequenceValue(values));

    private BlobBuilder MaterializeSerializationType(SerializationTypeValue value)
    {
        if (value is RawSerializationTypeValue raw)
        {
            return raw.Value;
        }

        BlobBuilder blob = new();
        switch (value)
        {
            case SimpleSerializationTypeValue simple:
                blob.WriteByte((byte)simple.Type);
                break;
            case ArraySerializationTypeValue array:
                blob.WriteByte((byte)SerializationTypeCode.SZArray);
                MaterializeSerializationType(array.ElementType).WriteContentTo(blob);
                break;
            case StringEnumSerializationTypeValue stringEnum:
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                blob.WriteSerializedString(stringEnum.Name);
                break;
            case ClassEnumSerializationTypeValue classEnum:
                blob.WriteByte((byte)SerializationTypeCode.Enum);
                blob.WriteSerializedString(GetReflectionNotation(classEnum.ClassName));
                break;
            case TypedefSerializationTypeValue typedef:
                ReportError(
                    DiagnosticIds.TypedefNotFound,
                    string.Format(DiagnosticMessageTemplates.TypedefNotFound, typedef.Alias),
                    typedef.Token);
                break;
        }

        return blob;
    }

    private BlobBuilder MaterializeSerializedInitializer(SerializedInitializerValue value)
    {
        if (value is RawSerializedInitializerValue raw)
        {
            return raw.Value;
        }

        BlobBuilder blob = new();
        switch (value)
        {
            case ClassNameSerializedInitializerValue className:
                blob.WriteSerializedString(GetReflectionNotation(className.ClassName));
                break;
            case ObjectSerializedInitializerValue boxed:
                MaterializeSerializationType(boxed.Value.Type).WriteContentTo(blob);
                MaterializeSerializedInitializer(boxed.Value).WriteContentTo(blob);
                break;
            case InvalidByteArraySerializedInitializerValue invalid:
                ReportError(
                    DiagnosticIds.InvalidMetadataToken,
                    "bytearray is not a valid structured custom attribute value",
                    invalid.Token);
                blob.WriteSerializedString(null);
                break;
            case ArraySerializedInitializerValue array:
                blob.WriteInt32(array.Length);
                MaterializeSerializedSequence(array.Values).WriteContentTo(blob);
                break;
        }

        return blob;
    }

    private string GetReflectionNotation(ClassNameValue className)
    {
        EntityRegistry.TypeEntity type = ResolveClassName(className);
        return (type as EntityRegistry.IHasReflectionNotation)?.ReflectionNotation ?? string.Empty;
    }

    private static SerializationTypeCode GetSerializationTypeCode(int tokenType)
        => tokenType switch
        {
            CILParser.INT8 => SerializationTypeCode.SByte,
            CILParser.UINT8 => SerializationTypeCode.Byte,
            CILParser.INT16 => SerializationTypeCode.Int16,
            CILParser.UINT16 => SerializationTypeCode.UInt16,
            CILParser.INT32_ => SerializationTypeCode.Int32,
            CILParser.UINT32 => SerializationTypeCode.UInt32,
            CILParser.INT64_ => SerializationTypeCode.Int64,
            CILParser.UINT64 => SerializationTypeCode.UInt64,
            CILParser.FLOAT32 => SerializationTypeCode.Single,
            CILParser.FLOAT64_ => SerializationTypeCode.Double,
            CILParser.CHAR => SerializationTypeCode.Char,
            CILParser.BOOL => SerializationTypeCode.Boolean,
            CILParser.STRING => SerializationTypeCode.String,
            CILParser.TYPE => SerializationTypeCode.Type,
            CILParser.OBJECT => SerializationTypeCode.TaggedObject,
            _ => throw new UnreachableException()
        };

    private static object? ExtractConstantFromSerInit(BlobBuilder blob)
    {
        ImmutableArray<byte> bytes = blob.ToImmutableArray();
        if (bytes.Length == 0)
        {
            return null;
        }

        SerializationTypeCode typeCode = (SerializationTypeCode)bytes[0];
        ReadOnlySpan<byte> valueBytes = bytes.AsSpan().Slice(1);
        return typeCode switch
        {
            SerializationTypeCode.Boolean => valueBytes.Length >= 1 && valueBytes[0] != 0,
            SerializationTypeCode.Char => valueBytes.Length >= 2 ? BitConverter.ToChar(valueBytes) : '\0',
            SerializationTypeCode.SByte => valueBytes.Length >= 1 ? (sbyte)valueBytes[0] : (sbyte)0,
            SerializationTypeCode.Byte => valueBytes.Length >= 1 ? valueBytes[0] : (byte)0,
            SerializationTypeCode.Int16 => valueBytes.Length >= 2 ? BitConverter.ToInt16(valueBytes) : (short)0,
            SerializationTypeCode.UInt16 => valueBytes.Length >= 2 ? BitConverter.ToUInt16(valueBytes) : (ushort)0,
            SerializationTypeCode.Int32 => valueBytes.Length >= 4 ? BitConverter.ToInt32(valueBytes) : 0,
            SerializationTypeCode.UInt32 => valueBytes.Length >= 4 ? BitConverter.ToUInt32(valueBytes) : 0u,
            SerializationTypeCode.Int64 => valueBytes.Length >= 8 ? BitConverter.ToInt64(valueBytes) : 0L,
            SerializationTypeCode.UInt64 => valueBytes.Length >= 8 ? BitConverter.ToUInt64(valueBytes) : 0uL,
            SerializationTypeCode.Single => valueBytes.Length >= 4 ? BitConverter.ToSingle(valueBytes) : 0f,
            SerializationTypeCode.Double => valueBytes.Length >= 8 ? BitConverter.ToDouble(valueBytes) : 0d,
            SerializationTypeCode.String => Encoding.Unicode.GetString(valueBytes),
            SerializationTypeCode.Type => ExtractSerString(valueBytes),
            SerializationTypeCode.SZArray => valueBytes.ToArray(),
            SerializationTypeCode.TaggedObject => valueBytes.ToArray(),
            SerializationTypeCode.Enum => valueBytes.ToArray(),
            _ => bytes.AsSpan().ToArray()
        };
    }

    private static string? ExtractSerString(ReadOnlySpan<byte> bytes)
    {
        if (bytes.Length == 0 || bytes[0] == 0xFF)
        {
            return null;
        }

        int length;
        int bytesRead;
        if ((bytes[0] & 0x80) == 0)
        {
            length = bytes[0];
            bytesRead = 1;
        }
        else if ((bytes[0] & 0xC0) == 0x80)
        {
            if (bytes.Length < 2)
            {
                return null;
            }

            length = ((bytes[0] & 0x3F) << 8) | bytes[1];
            bytesRead = 2;
        }
        else
        {
            if (bytes.Length < 4)
            {
                return null;
            }

            length = ((bytes[0] & 0x1F) << 24) |
                (bytes[1] << 16) |
                (bytes[2] << 8) |
                bytes[3];
            bytesRead = 4;
        }

        return bytes.Length < bytesRead + length
            ? null
            : Encoding.UTF8.GetString(bytes.Slice(bytesRead, length));
    }

    internal static FieldInitializerValue GetInitializerValue(CILParser.InitOptContext context)
        => context.Value;
}
