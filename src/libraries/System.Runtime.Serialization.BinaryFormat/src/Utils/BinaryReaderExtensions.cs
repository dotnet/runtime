// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace System.Runtime.Serialization.BinaryFormat;

internal static class BinaryReaderExtensions
{
    /// <summary>
    ///  Reads a primitive of <paramref name="primitiveType"/> from the given <paramref name="reader"/>.
    /// </summary>
    internal static object ReadPrimitiveType(this BinaryReader reader, PrimitiveType primitiveType)
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
            PrimitiveType.TimeSpan => new TimeSpan(reader.ReadInt64()),
            // String is handled with a record, never on it's own
            _ => throw new SerializationException($"Failure trying to read primitive '{primitiveType}'"),
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

    internal static TypeName ReadTypeName(this BinaryReader binaryReader, PayloadOptions options)
    {
        string name = binaryReader.ReadString();
        if (!TypeName.TryParse(name.AsSpan(), out TypeName? typeName, options.TypeNameParseOptions))
        {
            throw new SerializationException($"Invalid type name: '{name}'");
        }
        else if (typeName.AssemblyName is not null)
        {
            throw new SerializationException("Type names must not contain assembly names");
        }

        return typeName;
    }

    internal static AssemblyNameInfo ReadLibraryName(this BinaryReader binaryReader)
    {
        string name = binaryReader.ReadString();
        if (!AssemblyNameInfo.TryParse(name.AsSpan(), out AssemblyNameInfo? libraryName))
        {
            throw new SerializationException($"Invalid library name: '{name}'");
        }

        return libraryName;
    }
}
