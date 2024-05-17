// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace System.Runtime.Serialization.BinaryFormat;

#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
static class PayloadReader
{
    private static UTF8Encoding ThrowOnInvalidUtf8Encoding { get; } = new(false, throwOnInvalidBytes: true);

    /// <summary>
    /// Checks if given buffer contains only Binary Formatter payload.
    /// </summary>
    /// <param name="bytes">The buffer to inspect</param>
    /// <returns>True if the first and last bytes indicate Binary Format, otherwise false.</returns>
    public static bool ContainsBinaryFormatterPayload(ReadOnlySpan<byte> bytes)
        => bytes.Length >= SerializedStreamHeaderRecord.Size + 2
            && bytes[0] == (byte)RecordType.SerializedStreamHeader
            && bytes[bytes.Length - 1] == (byte)RecordType.MessageEnd;

    /// <summary>
    /// Checks if given stream contains only Binary Formatter payload.
    /// </summary>
    /// <param name="stream">The readable and seekable stream to inspect.</param>
    /// <returns>True if the first and last bytes indicate Binary Format, otherwise false.</returns>
    /// <exception cref="ArgumentNullException">The stream is null.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading or seeking.</exception>
    /// <exception cref="ObjectDisposedException">The stream was closed.</exception>
    /// <remarks><para>It does not modify the position of the stream.</para></remarks>
    public static bool ContainsBinaryFormatterPayload(Stream stream)
    {
#if NETCOREAPP
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null) throw new ArgumentNullException(nameof(stream));
#endif
        long beginning = stream.Position;
        if (stream.Length - beginning < SerializedStreamHeaderRecord.Size + 2)
        {
            return false;
        }

        try
        {
            int firstByte = stream.ReadByte();
            if (firstByte != (byte)RecordType.SerializedStreamHeader)
            {
                return false;
            }

            stream.Seek(-1, SeekOrigin.End);
            int lastByte = stream.ReadByte();
            return lastByte == (byte)RecordType.MessageEnd;
        }
        finally
        {
            stream.Position = beginning;
        }
    }

    /// <summary>
    /// Reads the provided Binary Format payload.
    /// </summary>
    /// <param name="payload">The Binary Format payload.</param>
    /// <param name="options">An object that describes optional <seealso cref="PayloadOptions"/> parameters to use.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="payload"/> payload open
    /// after the reading is finished, otherwise, false.</param>
    /// <returns>A <seealso cref="SerializationRecord"/> that represents the root object.
    /// It can be either <seealso cref="PrimitiveTypeRecord{T}"/>,
    /// a <seealso cref="ClassRecord"/> or an <seealso cref="ArrayRecord"/>.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="payload"/> is null.</exception>
    /// <exception cref="ArgumentException">The <paramref name="payload"/> payload does not support reading or is already closed.</exception>
    /// <exception cref="SerializationException">When reading input from <paramref name="payload"/> encounters invalid Binary Format data.</exception>
    /// <exception cref="DecoderFallbackException">When reading input from <paramref name="payload"/>
    /// encounters invalid sequence of UTF8 characters.</exception>
    public static SerializationRecord Read(Stream payload, PayloadOptions? options = default, bool leaveOpen = false)
        => Read(payload, out _, options, leaveOpen);

    /// <param name="payload">The Binary Format payload.</param>
    /// <param name="recordMap">Record map</param>
    /// <param name="options">An object that describes optional <seealso cref="PayloadOptions"/> parameters to use.</param>
    /// <param name="leaveOpen">True to leave the <paramref name="payload"/> payload open
    /// after the reading is finished, otherwise, false.</param>
    /// <inheritdoc cref="Read(Stream, PayloadOptions?, bool)"/>
    public static SerializationRecord Read(Stream payload, out IReadOnlyDictionary<int, SerializationRecord> recordMap, PayloadOptions? options = default, bool leaveOpen = false)
    {
#if NETCOREAPP
        ArgumentNullException.ThrowIfNull(payload);
#else
        if (payload is null) throw new ArgumentNullException(nameof(payload));
#endif

        using BinaryReader reader = new(payload, ThrowOnInvalidUtf8Encoding, leaveOpen: leaveOpen);
        return Read(reader, options ?? new(), out recordMap);
    }

    /// <summary>
    /// Reads the provided Binary Format payload that is expected to contain an instance of any class (or struct) that is not an <seealso cref="Array"/> or a primitive type.
    /// </summary>
    /// <returns>A <seealso cref="ClassRecord"/> that represents the root object.</returns>
    /// <inheritdoc cref="Read(Stream, PayloadOptions?, bool)"/>
    public static ClassRecord ReadClassRecord(Stream payload, PayloadOptions? options = default, bool leaveOpen = false)
        => (ClassRecord)Read(payload, options, leaveOpen);

    private static SerializationRecord Read(BinaryReader reader, PayloadOptions options, out IReadOnlyDictionary<int, SerializationRecord> readOnlyRecordMap)
    {
        Stack<NextInfo> readStack = new();
        var recordMap = new RecordMap();

        // Everything has to start with a header
        var header = (SerializedStreamHeaderRecord)ReadNext(reader, recordMap, AllowedRecordTypes.SerializedStreamHeader, options, out _);
        // and can be followed by any Object, BinaryLibrary and a MessageEnd.
        const AllowedRecordTypes allowed = AllowedRecordTypes.AnyObject
            | AllowedRecordTypes.BinaryLibrary | AllowedRecordTypes.MessageEnd;

        RecordType recordType;
        SerializationRecord nextRecord;
        do
        {
            while (readStack.Count > 0)
            {
                NextInfo nextInfo = readStack.Pop();

                if (nextInfo.Allowed != AllowedRecordTypes.None)
                {
                    // Read the next Record
                    do
                    {
                        nextRecord = ReadNext(reader, recordMap, nextInfo.Allowed, options, out _);
                        // BinaryLibrary often precedes class records.
                        // It has been already added to the RecordMap and it must not be added
                        // to the array record, so simply read next record.
                        // It's possible to read multiple BinaryLibraryRecord in a row, hence the loop.
                    }
                    while (nextRecord is BinaryLibraryRecord);

                    // Handle it:
                    // - add to the parent records list,
                    // - push next info if there are remaining nested records to read.
                    nextInfo.Parent.HandleNextRecord(nextRecord, nextInfo);
                    // Push on the top of the stack the first nested record of last read record,
                    // so it gets read as next record.
                    PushFirstNestedRecordInfo(nextRecord, readStack);
                }
                else
                {
                    object value = reader.ReadPrimitiveType(nextInfo.PrimitiveType);

                    nextInfo.Parent.HandleNextValue(value, nextInfo);
                }
            }

            nextRecord = ReadNext(reader, recordMap, allowed, options, out recordType);
            PushFirstNestedRecordInfo(nextRecord, readStack);
        }
        while (recordType != RecordType.MessageEnd);

        readOnlyRecordMap = recordMap;
        return recordMap.GetRootRecord(header);
    }

    private static SerializationRecord ReadNext(BinaryReader reader, RecordMap recordMap,
        AllowedRecordTypes allowed, PayloadOptions options, out RecordType recordType)
    {
        recordType = (RecordType)reader.ReadByte();

        if (((uint)allowed & (1u << (int)recordType)) == 0)
        {
            ThrowHelper.ThrowForUnexpectedRecordType(recordType);
        }

        SerializationRecord record = recordType switch
        {
            RecordType.ArraySingleObject => ArraySingleObjectRecord.Parse(reader),
            RecordType.ArraySinglePrimitive => ParseArraySinglePrimitiveRecord(reader),
            RecordType.ArraySingleString => ArraySingleStringRecord.Parse(reader),
            RecordType.BinaryArray => BinaryArrayRecord.Parse(reader, recordMap, options),
            RecordType.BinaryLibrary => BinaryLibraryRecord.Parse(reader),
            RecordType.BinaryObjectString => BinaryObjectStringRecord.Parse(reader),
            RecordType.ClassWithId => ClassWithIdRecord.Parse(reader, recordMap),
            RecordType.ClassWithMembersAndTypes => ClassWithMembersAndTypesRecord.Parse(reader, recordMap, options),
            RecordType.MemberPrimitiveTyped => ParseMemberPrimitiveTypedRecord(reader),
            RecordType.MemberReference => MemberReferenceRecord.Parse(reader, recordMap),
            RecordType.MessageEnd => MessageEndRecord.Singleton,
            RecordType.ObjectNull => ObjectNullRecord.Instance,
            RecordType.ObjectNullMultiple => ObjectNullMultipleRecord.Parse(reader),
            RecordType.ObjectNullMultiple256 => ObjectNullMultiple256Record.Parse(reader),
            RecordType.SerializedStreamHeader => SerializedStreamHeaderRecord.Parse(reader),
            RecordType.SystemClassWithMembersAndTypes => SystemClassWithMembersAndTypesRecord.Parse(reader, options),
            _ => throw new SerializationException($"Invalid RecordType value: {recordType}")
        };

        recordMap.Add(record);

        return record;
    }

    private static SerializationRecord ParseMemberPrimitiveTypedRecord(BinaryReader reader)
    {
        PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();

        return primitiveType switch
        {
            PrimitiveType.Boolean => new MemberPrimitiveTypedRecord<bool>(reader.ReadBoolean()),
            PrimitiveType.Byte => new MemberPrimitiveTypedRecord<byte>(reader.ReadByte()),
            PrimitiveType.SByte => new MemberPrimitiveTypedRecord<sbyte>(reader.ReadSByte()),
            PrimitiveType.Char => new MemberPrimitiveTypedRecord<char>(reader.ReadChar()),
            PrimitiveType.Int16 => new MemberPrimitiveTypedRecord<short>(reader.ReadInt16()),
            PrimitiveType.UInt16 => new MemberPrimitiveTypedRecord<ushort>(reader.ReadUInt16()),
            PrimitiveType.Int32 => new MemberPrimitiveTypedRecord<int>(reader.ReadInt32()),
            PrimitiveType.UInt32 => new MemberPrimitiveTypedRecord<uint>(reader.ReadUInt32()),
            PrimitiveType.Int64 => new MemberPrimitiveTypedRecord<long>(reader.ReadInt64()),
            PrimitiveType.UInt64 => new MemberPrimitiveTypedRecord<ulong>(reader.ReadUInt64()),
            PrimitiveType.Single => new MemberPrimitiveTypedRecord<float>(reader.ReadSingle()),
            PrimitiveType.Double => new MemberPrimitiveTypedRecord<double>(reader.ReadDouble()),
            PrimitiveType.Decimal => new MemberPrimitiveTypedRecord<decimal>(decimal.Parse(reader.ReadString(), CultureInfo.InvariantCulture)),
            PrimitiveType.DateTime => new MemberPrimitiveTypedRecord<DateTime>(BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadInt64())),
            PrimitiveType.TimeSpan => new MemberPrimitiveTypedRecord<TimeSpan>(new TimeSpan(reader.ReadInt64())),
            // String is handled with a record, never on it's own
            _ => throw new SerializationException($"Failure trying to read primitive '{primitiveType}'"),
        };
    }

    private static SerializationRecord ParseArraySinglePrimitiveRecord(BinaryReader reader)
    {
        ArrayInfo info = ArrayInfo.Parse(reader);
        PrimitiveType primitiveType = (PrimitiveType)reader.ReadByte();

        return primitiveType switch
        {
            PrimitiveType.Boolean => Read<bool>(info, reader),
            PrimitiveType.Byte => Read<byte>(info, reader),
            PrimitiveType.SByte => Read<sbyte>(info, reader),
            PrimitiveType.Char => Read<char>(info, reader),
            PrimitiveType.Int16 => Read<short>(info, reader),
            PrimitiveType.UInt16 => Read<ushort>(info, reader),
            PrimitiveType.Int32 => Read<int>(info, reader),
            PrimitiveType.UInt32 => Read<uint>(info, reader),
            PrimitiveType.Int64 => Read<long>(info, reader),
            PrimitiveType.UInt64 => Read<ulong>(info, reader),
            PrimitiveType.Single => Read<float>(info, reader),
            PrimitiveType.Double => Read<double>(info, reader),
            PrimitiveType.Decimal => Read<decimal>(info, reader),
            PrimitiveType.DateTime => Read<DateTime>(info, reader),
            PrimitiveType.TimeSpan => Read<TimeSpan>(info, reader),
            _ => throw new SerializationException($"Failure trying to read primitive '{primitiveType}'"),
        };

        static SerializationRecord Read<T>(ArrayInfo info, BinaryReader reader) where T : unmanaged
            => new ArraySinglePrimitiveRecord<T>(info, ArraySinglePrimitiveRecord<T>.ReadPrimitiveTypes(reader, (int)info.Length));
    }

    /// <summary>
    /// This method is responsible for pushing only the FIRST read info
    /// of the NESTED record into the <paramref name="readStack"/>.
    /// It's not pushing all of them, because it could be used as a vector of attack.
    /// Example: BinaryArrayRecord with Array.MaxLength length,
    /// where first item turns out to be <seealso cref="ObjectNullMultipleRecord"/>
    /// that provides Array.MaxLength nulls.
    /// </summary>
    private static void PushFirstNestedRecordInfo(SerializationRecord record, Stack<NextInfo> readStack)
    {
        if (record is ClassRecord classRecord)
        {
            if (classRecord.ExpectedValuesCount > 0)
            {
                (AllowedRecordTypes allowed, PrimitiveType primitiveType) = classRecord.GetNextAllowedRecordType();

                readStack.Push(new(allowed, classRecord, readStack, primitiveType));
            }
        }
        else if (record is ArrayRecord arrayRecord && arrayRecord.ValuesToRead > 0)
        {
            (AllowedRecordTypes allowed, PrimitiveType primitiveType) = arrayRecord.GetAllowedRecordType();

            readStack.Push(new(allowed, arrayRecord, readStack, primitiveType));
        }
    }
}
