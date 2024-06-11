// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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

    internal static ClassWithIdRecord Decode(
        BinaryReader reader,
        RecordMap recordMap)
    {
        SerializationRecordId id = SerializationRecordId.Decode(reader);
        SerializationRecordId metadataId = SerializationRecordId.Decode(reader);

        if (recordMap[metadataId] is not ClassRecord referencedRecord)
        {
            throw new SerializationException(SR.Serialization_InvalidReference);
        }

        return new ClassWithIdRecord(id, referencedRecord);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MetadataClass.MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
