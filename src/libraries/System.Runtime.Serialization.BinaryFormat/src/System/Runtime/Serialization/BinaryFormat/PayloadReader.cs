﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers.Binary;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization.BinaryFormat.Utils;
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
    /// Checks if given buffer starts with <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">NRBF payload header</see>.
    /// </summary>
    /// <param name="bytes">The buffer to inspect.</param>
    /// <returns><see langword="true" /> if it starts with NRBF payload header; otherwise, <see langword="false" />.</returns>
    public static bool StartsWithPayloadHeader(byte[] bytes)
    {
#if NET
        ArgumentNullException.ThrowIfNull(bytes);
#else
        if (bytes is null)
        {
            throw new ArgumentNullException(nameof(bytes));
        }
#endif

        return bytes.Length >= SerializedStreamHeaderRecord.Size
            && bytes[0] == (byte)RecordType.SerializedStreamHeader
#if NET
            && BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(9)) == SerializedStreamHeaderRecord.MajorVersion
            && BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(13)) == SerializedStreamHeaderRecord.MinorVersion;
#else
            && BitConverter.ToInt32(bytes, 9) == SerializedStreamHeaderRecord.MajorVersion
            && BitConverter.ToInt32(bytes, 13) == SerializedStreamHeaderRecord.MinorVersion;
#endif
    }


    /// <summary>
    /// Checks if given stream starts with <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/a7e578d3-400a-4249-9424-7529d10d1b3c">NRBF payload header</see>.
    /// </summary>
    /// <param name="stream">The stream to inspect. The stream must be both readable and seekable.</param>
    /// <returns><see langword="true" /> if it starts with NRBF payload header; otherwise, <see langword="false" />.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="stream" /> is <see langword="null" />.</exception>
    /// <exception cref="NotSupportedException">The stream does not support reading or seeking.</exception>
    /// <exception cref="ObjectDisposedException">The stream was closed.</exception>
    /// <remarks><para>When this method returns, <paramref name="stream" /> will be restored to its original position.</para></remarks>
    public static bool StartsWithPayloadHeader(Stream stream)
    {
#if NET
        ArgumentNullException.ThrowIfNull(stream);
#else
        if (stream is null)
        {
            throw new ArgumentNullException(nameof(stream));
        }
#endif
        if (!stream.CanSeek)
        {
            throw new ArgumentException(SR.Argument_NonSeekableStream, nameof(stream));
        }

        long beginning = stream.Position;
        if (stream.Length - beginning <= SerializedStreamHeaderRecord.Size)
        {
            return false;
        }

        byte[] buffer = new byte[SerializedStreamHeaderRecord.Size];

        try
        {
#if NET
            stream.ReadExactly(buffer, 0, buffer.Length);
#else
            int offset = 0;
            while (offset < buffer.Length)
            {
                int read = stream.Read(buffer, offset, buffer.Length - offset);
                if (read == 0)
                    throw new EndOfStreamException();
                offset += read;
            }
#endif
            return StartsWithPayloadHeader(buffer);
        }
        finally
        {
            stream.Position = beginning;
        }
    }

    /// <summary>
    /// Reads the provided NRBF payload.
    /// </summary>
    /// <param name="payload">The NRBF payload.</param>
    /// <param name="options">Options to control behavior during parsing.</param>
    /// <param name="leaveOpen">
    ///   <see langword="true" /> to leave <paramref name="payload"/> payload open
    ///   after the reading is finished; otherwise, <see langword="false" />.
    /// </param>
    /// <returns>A <see cref="SerializationRecord"/> that represents the root object.
    /// It can be either <see cref="PrimitiveTypeRecord{T}"/>,
    /// a <see cref="ClassRecord"/> or an <see cref="ArrayRecord"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="payload"/> is <see langword="null" />.</exception>
    /// <exception cref="ArgumentException"><paramref name="payload"/> does not support reading or is already closed.</exception>
    /// <exception cref="SerializationException">Reading from <paramref name="payload"/> encounters invalid NRBF data.</exception>
    /// <exception cref="DecoderFallbackException">Reading from <paramref name="payload"/>
    /// encounters an invalid UTF8 sequence.</exception>
    public static SerializationRecord Read(Stream payload, PayloadOptions? options = default, bool leaveOpen = false)
        => Read(payload, out _, options, leaveOpen);

    /// <param name="payload">The NRBF payload.</param>
    /// <param name="recordMap">
    ///   When this method returns, contains a mapping of <see cref="SerializationRecord.ObjectId" /> to the associated serialization record.
    ///   This parameter is treated as uninitialized.
    /// </param>
    /// <param name="options">An object that describes optional <see cref="PayloadOptions"/> parameters to use.</param>
    /// <param name="leaveOpen">
    ///   <see langword="true" /> to leave <paramref name="payload"/> payload open
    ///   after the reading is finished; otherwise, <see langword="false" />.
    /// </param>
    /// <inheritdoc cref="Read(Stream, PayloadOptions?, bool)"/>
    public static SerializationRecord Read(Stream payload, out IReadOnlyDictionary<int, SerializationRecord> recordMap, PayloadOptions? options = default, bool leaveOpen = false)
    {
#if NET
        ArgumentNullException.ThrowIfNull(payload);
#else
        if (payload is null)
        {
            throw new ArgumentNullException(nameof(payload));
        }
#endif

        using BinaryReader reader = new(payload, ThrowOnInvalidUtf8Encoding, leaveOpen: leaveOpen);
        return Read(reader, options ?? new(), out recordMap);
    }

    /// <summary>
    /// Reads the provided NRBF payload that is expected to contain an instance of any class (or struct) that is not an <see cref="Array"/> or a primitive type.
    /// </summary>
    /// <returns>A <see cref="ClassRecord"/> that represents the root object.</returns>
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
        const AllowedRecordTypes Allowed = AllowedRecordTypes.AnyObject
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
                    object value = reader.ReadPrimitiveValue(nextInfo.PrimitiveType);

                    nextInfo.Parent.HandleNextValue(value, nextInfo);
                }
            }

            nextRecord = ReadNext(reader, recordMap, Allowed, options, out recordType);
            PushFirstNestedRecordInfo(nextRecord, readStack);
        }
        while (recordType != RecordType.MessageEnd);

        readOnlyRecordMap = recordMap;
        return recordMap.GetRootRecord(header);
    }

    private static SerializationRecord ReadNext(BinaryReader reader, RecordMap recordMap,
        AllowedRecordTypes allowed, PayloadOptions options, out RecordType recordType)
    {
        byte nextByte = reader.ReadByte();
        if (((uint)allowed & (1u << nextByte)) == 0)
        {
            ThrowHelper.ThrowForUnexpectedRecordType(nextByte);
        }
        recordType = (RecordType)nextByte;

        SerializationRecord record = recordType switch
        {
            RecordType.ArraySingleObject => ArraySingleObjectRecord.Decode(reader),
            RecordType.ArraySinglePrimitive => DecodeArraySinglePrimitiveRecord(reader),
            RecordType.ArraySingleString => ArraySingleStringRecord.Decode(reader),
            RecordType.BinaryArray => BinaryArrayRecord.Decode(reader, recordMap, options),
            RecordType.BinaryLibrary => BinaryLibraryRecord.Decode(reader),
            RecordType.BinaryObjectString => BinaryObjectStringRecord.Decode(reader),
            RecordType.ClassWithId => ClassWithIdRecord.Decode(reader, recordMap),
            RecordType.ClassWithMembersAndTypes => ClassWithMembersAndTypesRecord.Decode(reader, recordMap, options),
            RecordType.MemberPrimitiveTyped => DecodeMemberPrimitiveTypedRecord(reader),
            RecordType.MemberReference => MemberReferenceRecord.Decode(reader, recordMap),
            RecordType.MessageEnd => MessageEndRecord.Singleton,
            RecordType.ObjectNull => ObjectNullRecord.Instance,
            RecordType.ObjectNullMultiple => ObjectNullMultipleRecord.Decode(reader),
            RecordType.ObjectNullMultiple256 => ObjectNullMultiple256Record.Decode(reader),
            RecordType.SerializedStreamHeader => SerializedStreamHeaderRecord.Decode(reader),
            _ => SystemClassWithMembersAndTypesRecord.Decode(reader, recordMap, options),
        };

        recordMap.Add(record);

        return record;
    }

    private static SerializationRecord DecodeMemberPrimitiveTypedRecord(BinaryReader reader)
    {
        PrimitiveType primitiveType = reader.ReadPrimitiveType();

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
            PrimitiveType.DateTime => new MemberPrimitiveTypedRecord<DateTime>(Utils.BinaryReaderExtensions.CreateDateTimeFromData(reader.ReadInt64())),
            // String is handled with a record, never on it's own
            _ => new MemberPrimitiveTypedRecord<TimeSpan>(new TimeSpan(reader.ReadInt64())),
        };
    }

    private static SerializationRecord DecodeArraySinglePrimitiveRecord(BinaryReader reader)
    {
        ArrayInfo info = ArrayInfo.Decode(reader);
        PrimitiveType primitiveType = reader.ReadPrimitiveType();

        return primitiveType switch
        {
            PrimitiveType.Boolean => Decode<bool>(info, reader),
            PrimitiveType.Byte => Decode<byte>(info, reader),
            PrimitiveType.SByte => Decode<sbyte>(info, reader),
            PrimitiveType.Char => Decode<char>(info, reader),
            PrimitiveType.Int16 => Decode<short>(info, reader),
            PrimitiveType.UInt16 => Decode<ushort>(info, reader),
            PrimitiveType.Int32 => Decode<int>(info, reader),
            PrimitiveType.UInt32 => Decode<uint>(info, reader),
            PrimitiveType.Int64 => Decode<long>(info, reader),
            PrimitiveType.UInt64 => Decode<ulong>(info, reader),
            PrimitiveType.Single => Decode<float>(info, reader),
            PrimitiveType.Double => Decode<double>(info, reader),
            PrimitiveType.Decimal => Decode<decimal>(info, reader),
            PrimitiveType.DateTime => Decode<DateTime>(info, reader),
            _ => Decode<TimeSpan>(info, reader),
        };

        static SerializationRecord Decode<T>(ArrayInfo info, BinaryReader reader) where T : unmanaged
            => new ArraySinglePrimitiveRecord<T>(info, ArraySinglePrimitiveRecord<T>.DecodePrimitiveTypes(reader, info.GetSZArrayLength()));
    }

    /// <summary>
    /// This method is responsible for pushing only the FIRST read info
    /// of the NESTED record into the <paramref name="readStack"/>.
    /// It's not pushing all of them, because it could be used as a vector of attack.
    /// Example: BinaryArrayRecord with Array.MaxLength length,
    /// where first item turns out to be <see cref="ObjectNullMultipleRecord"/>
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
