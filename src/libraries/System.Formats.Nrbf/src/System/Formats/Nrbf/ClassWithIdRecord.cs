// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Formats.Nrbf.Utils;
using System.IO;
using System.Runtime.Serialization;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a class information that references another class record's metadata.
/// </summary>
/// <remarks>
/// ClassWithId records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/2d168388-37f4-408a-b5e0-e48dbce73e26">[MS-NRBF] 2.3.2.5</see>.
/// </remarks>
internal sealed class ClassWithIdRecord : ClassRecord
{
    private ClassWithIdRecord(SerializationRecordId id, ClassRecord metadataClass) : base(metadataClass.ClassInfo, metadataClass.MemberTypeInfo)
    {
        Id = id;
        MetadataClass = metadataClass;
    }

    public override SerializationRecordType RecordType => SerializationRecordType.ClassWithId;

    /// <inheritdoc />
    public override SerializationRecordId Id { get; }

    internal ClassRecord MetadataClass { get; }

    internal static SerializationRecord Decode(
        BinaryReader reader,
        RecordMap recordMap)
    {
        SerializationRecordId id = SerializationRecordId.Decode(reader);
        SerializationRecordId metadataId = SerializationRecordId.Decode(reader);

        SerializationRecord metadataRecord = recordMap.GetRecord(metadataId);
        if (metadataRecord is ClassRecord referencedClassRecord)
        {
            return new ClassWithIdRecord(id, referencedClassRecord);
        }
        else if (metadataRecord is PrimitiveTypeRecord primitiveTypeRecord
            && !primitiveTypeRecord.Id.Equals(default) // such records always have Id provided
            && metadataRecord is not BinaryObjectStringRecord) // it does not apply to BinaryObjectStringRecord
        {
            // BinaryFormatter represents primitive types as MemberPrimitiveTypedRecord
            // only for arrays of objects. For other arrays, like arrays of some abstraction
            // (example: new IComparable[] { int.MaxValue }), it uses SystemClassWithMembersAndTypes.
            // SystemClassWithMembersAndTypes.Decode handles that by returning MemberPrimitiveTypedRecord.
            // But arrays of such types typically have only one SystemClassWithMembersAndTypes record with
            // all the member information and multiple ClassWithIdRecord records that just reuse that information.
            return primitiveTypeRecord switch
            {
                MemberPrimitiveTypedRecord<bool> => Create(reader.ReadBoolean()),
                MemberPrimitiveTypedRecord<byte> => Create(reader.ReadByte()),
                MemberPrimitiveTypedRecord<sbyte> => Create(reader.ReadSByte()),
                MemberPrimitiveTypedRecord<char> => Create(reader.ParseChar()),
                MemberPrimitiveTypedRecord<short> => Create(reader.ReadInt16()),
                MemberPrimitiveTypedRecord<ushort> => Create(reader.ReadUInt16()),
                MemberPrimitiveTypedRecord<int> => Create(reader.ReadInt32()),
                MemberPrimitiveTypedRecord<uint> => Create(reader.ReadUInt32()),
                MemberPrimitiveTypedRecord<long> => Create(reader.ReadInt64()),
                MemberPrimitiveTypedRecord<ulong> => Create(reader.ReadUInt64()),
                MemberPrimitiveTypedRecord<float> => Create(reader.ReadSingle()),
                MemberPrimitiveTypedRecord<double> => Create(reader.ReadDouble()),
                MemberPrimitiveTypedRecord<IntPtr> => Create(new IntPtr(reader.ReadInt64())),
                MemberPrimitiveTypedRecord<UIntPtr> => Create(new UIntPtr(reader.ReadUInt64())),
                MemberPrimitiveTypedRecord<TimeSpan> => Create(new TimeSpan(reader.ReadInt64())),
                MemberPrimitiveTypedRecord<DateTime> => SystemClassWithMembersAndTypesRecord.DecodeDateTime(reader, id),
                MemberPrimitiveTypedRecord<decimal> => SystemClassWithMembersAndTypesRecord.DecodeDecimal(reader, id),
                _ => throw new InvalidOperationException()
            };
        }
        else
        {
            throw new SerializationException(SR.Serialization_InvalidReference);
        }

        SerializationRecord Create<T>(T value) where T : unmanaged
            => new MemberPrimitiveTypedRecord<T>(value, id);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MetadataClass.MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
