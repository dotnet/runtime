// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization.BinaryFormat.Utils;

internal static class BinaryReaderExtensions
{
    internal static ArrayType ReadArrayType(this BinaryReader reader)
    {
        byte arrayType = reader.ReadByte();
        if (arrayType > 5)
        {
            ThrowHelper.ThrowInvalidValue(arrayType);
        }

        return (ArrayType)arrayType;
    }

    internal static BinaryType ReadBinaryType(this BinaryReader reader)
    {
        byte binaryType = reader.ReadByte();
        if (binaryType > 7)
        {
            ThrowHelper.ThrowInvalidValue(binaryType);
        }
        return (BinaryType)binaryType;
    }

    internal static PrimitiveType ReadPrimitiveType(this BinaryReader reader)
    {
        byte primitiveType = reader.ReadByte();
        if (primitiveType is > 18 or 4)
        {
            ThrowHelper.ThrowInvalidValue(primitiveType);
        }
        return (PrimitiveType)primitiveType;
    }

    /// <summary>
    ///  Reads a primitive of <paramref name="primitiveType"/> from the given <paramref name="reader"/>.
    /// </summary>
    internal static object ReadPrimitiveValue(this BinaryReader reader, PrimitiveType primitiveType)
        => primitiveType switch
        {
            PrimitiveType.Boolean => reader.ReadBoolean(),
            PrimitiveType.Byte => reader.ReadByte(),
            PrimitiveType.SByte => reader.ReadSByte(),
            PrimitiveType.Char => reader.ReadChar(),
            PrimitiveType.Int16 => reader.ReadInt16(),
            PrimitiveType.UInt16 => reader.ReadUInt16(),
            PrimitiveType.Int32 => reader.ReadInt32(),
            PrimitiveType.UInt32 => reader.ReadUInt32(),
            PrimitiveType.Int64 => reader.ReadInt64(),
            PrimitiveType.UInt64 => reader.ReadUInt64(),
            PrimitiveType.Single => reader.ReadSingle(),
            PrimitiveType.Double => reader.ReadDouble(),
            PrimitiveType.Decimal => decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture),
            PrimitiveType.DateTime => CreateDateTimeFromData(reader.ReadInt64()),
            _ => new TimeSpan(reader.ReadInt64()),
        };

    // TODO: fix https://github.com/adamsitnik/SafePayloadReader/issues/2
    /// <summary>
    ///  Creates a <see cref="DateTime"/> object from raw data with validation.
    /// </summary>
    /// <exception cref="SerializationException"><paramref name="data"/> was invalid.</exception>
    internal static DateTime CreateDateTimeFromData(long data)
    {
        // Copied from System.Runtime.Serialization.Formatters.Binary.BinaryParser

        // Use DateTime's public constructor to validate the input, but we
        // can't return that result as it strips off the kind. To address
        // that, store the value directly into a DateTime via an unsafe cast.
        // See BinaryFormatterWriter.WriteDateTime for details.

        try
        {
            const long TicksMask = 0x3FFFFFFFFFFFFFFF;
            _ = new DateTime(data & TicksMask);
        }
        catch (ArgumentException ex)
        {
            // Bad data
            throw new SerializationException(ex.Message, ex);
        }

        return Unsafe.As<long, DateTime>(ref data);
    }
}
