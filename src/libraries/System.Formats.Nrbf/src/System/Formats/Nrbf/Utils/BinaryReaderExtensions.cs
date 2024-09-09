// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Globalization;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Threading;

namespace System.Formats.Nrbf.Utils;

internal static class BinaryReaderExtensions
{
    private static object? s_baseAmbiguousDstDateTime;

    internal static SerializationRecordType ReadSerializationRecordType(this BinaryReader reader, AllowedRecordTypes allowed)
    {
        byte nextByte = reader.ReadByte();
        if (nextByte > (byte)SerializationRecordType.MethodReturn // MethodReturn is the last defined value.
            || (nextByte > (byte)SerializationRecordType.ArraySingleString && nextByte < (byte)SerializationRecordType.MethodCall) // not part of the spec
            || ((uint)allowed & (1u << nextByte)) == 0) // valid, but not allowed
        {
            ThrowHelper.ThrowForUnexpectedRecordType(nextByte);
        }

        return (SerializationRecordType)nextByte;
    }

    internal static BinaryArrayType ReadArrayType(this BinaryReader reader)
    {
        byte arrayType = reader.ReadByte();
        // Rectangular is the last defined value.
        if (arrayType > (byte)BinaryArrayType.Rectangular)
        {
            // Custom offset arrays
            if (arrayType >= 3 && arrayType <= 5)
            {
                throw new NotSupportedException(SR.NotSupported_NonZeroOffsets);
            }

            ThrowHelper.ThrowInvalidValue(arrayType);
        }

        return (BinaryArrayType)arrayType;
    }

    internal static BinaryType ReadBinaryType(this BinaryReader reader)
    {
        byte binaryType = reader.ReadByte();
        // PrimitiveArray is the last defined value.
        if (binaryType > (byte)BinaryType.PrimitiveArray)
        {
            ThrowHelper.ThrowInvalidValue(binaryType);
        }
        return (BinaryType)binaryType;
    }

    internal static PrimitiveType ReadPrimitiveType(this BinaryReader reader)
    {
        byte primitiveType = reader.ReadByte();
        // String is the last defined value, 4 is not used at all.
        if (primitiveType is 0 or 4 or (byte)PrimitiveType.Null or > (byte)PrimitiveType.String)
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
            PrimitiveType.Char => reader.ParseChar(),
            PrimitiveType.Int16 => reader.ReadInt16(),
            PrimitiveType.UInt16 => reader.ReadUInt16(),
            PrimitiveType.Int32 => reader.ReadInt32(),
            PrimitiveType.UInt32 => reader.ReadUInt32(),
            PrimitiveType.Int64 => reader.ReadInt64(),
            PrimitiveType.UInt64 => reader.ReadUInt64(),
            PrimitiveType.Single => reader.ReadSingle(),
            PrimitiveType.Double => reader.ReadDouble(),
            PrimitiveType.Decimal => reader.ParseDecimal(),
            PrimitiveType.DateTime => CreateDateTimeFromData(reader.ReadUInt64()),
            _ => new TimeSpan(reader.ReadInt64()),
        };

    // BinaryFormatter serializes decimals as strings and we can't BinaryReader.ReadDecimal.
    internal static decimal ParseDecimal(this BinaryReader reader)
    {
        string text = reader.ReadString();
        if (!decimal.TryParse(text, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal result))
        {
            ThrowHelper.ThrowInvalidFormat();
        }

        return result;
    }

    internal static char ParseChar(this BinaryReader reader)
    {
        try
        {
            return reader.ReadChar();
        }
        catch (ArgumentException) // A surrogate character was read.
        {
            throw new SerializationException(SR.Serialization_SurrogateCharacter);
        }
    }

    /// <summary>
    ///  Creates a <see cref="DateTime"/> object from raw data with validation.
    /// </summary>
    /// <exception cref="SerializationException"><paramref name="dateData"/> was invalid.</exception>
    internal static DateTime CreateDateTimeFromData(ulong dateData)
    {
        ulong ticks = dateData & 0x3FFFFFFF_FFFFFFFFUL;
        DateTimeKind kind = (DateTimeKind)(dateData >> 62);

        try
        {
            return ((uint)kind <= (uint)DateTimeKind.Local) ? new DateTime((long)ticks, kind) : CreateFromAmbiguousDst(ticks);
        }
        catch (ArgumentException ex)
        {
            throw new SerializationException(ex.Message, ex);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        static DateTime CreateFromAmbiguousDst(ulong ticks)
        {
            // There's no public API to create a DateTime from an ambiguous DST, and we
            // can't use private reflection to access undocumented .NET Framework APIs.
            // However, the ISerializable pattern *is* a documented protocol, so we can
            // use DateTime's serialization ctor to create a zero-tick "ambiguous" instance,
            // then keep reusing it as the base to which we can add our tick offsets.

            if (s_baseAmbiguousDstDateTime is not DateTime baseDateTime)
            {
#pragma warning disable SYSLIB0050 // Type or member is obsolete
                SerializationInfo si = new(typeof(DateTime), new FormatterConverter());
                // We don't know the value of "ticks", so we don't specify it.
                // If the code somehow runs on a very old runtime that does not know the concept of "dateData"
                // (it should not be possible as the library targets .NET Standard 2.0)
                // the ctor is going to throw rather than silently return an invalid value.
                si.AddValue("dateData", 0xC0000000_00000000UL); // new value (serialized as ulong)

#if NET
                baseDateTime = CallPrivateSerializationConstructor(si, new StreamingContext(StreamingContextStates.All));
#else
                ConstructorInfo ci = typeof(DateTime).GetConstructor(
                    BindingFlags.Instance | BindingFlags.NonPublic,
                    binder: null,
                    new Type[] { typeof(SerializationInfo), typeof(StreamingContext) },
                    modifiers: null);

                baseDateTime = (DateTime)ci.Invoke(new object[] { si, new StreamingContext(StreamingContextStates.All) });
#endif

#pragma warning restore SYSLIB0050 // Type or member is obsolete
                Volatile.Write(ref s_baseAmbiguousDstDateTime, baseDateTime); // it's ok if two threads race here
            }

            return baseDateTime.AddTicks((long)ticks);
        }

#if NET
        [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
        extern static DateTime CallPrivateSerializationConstructor(SerializationInfo si, StreamingContext ct);
#endif
    }

    internal static bool? IsDataAvailable(this BinaryReader reader, long requiredBytes)
    {
        if (!reader.BaseStream.CanSeek)
        {
            return null;
        }

        try
        {
            // If the values are equal, it's still not enough, as every NRBF payload
            // needs to end with EndMessageByte and requiredBytes does not take it into account.
            return (reader.BaseStream.Length - reader.BaseStream.Position) > requiredBytes;
        }
        catch
        {
            // seekable Stream can still throw when accessing Length and Position
            return null;
        }
    }
}
