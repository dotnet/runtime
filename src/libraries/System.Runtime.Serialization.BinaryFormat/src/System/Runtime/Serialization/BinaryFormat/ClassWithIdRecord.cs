// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Represents a class information that references another class record's metadata.
/// </summary>
/// <remarks>
/// ClassWithId records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/2d168388-37f4-408a-b5e0-e48dbce73e26">[MS-NRBF] 2.3.2.5</see>.
/// </remarks>
internal sealed class ClassWithIdRecord : ClassRecord
{
    private ClassWithIdRecord(int objectId, ClassRecord metadataClass) : base(metadataClass.ClassInfo, metadataClass.MemberTypeInfo)
    {
        ObjectId = objectId;
        MetadataClass = metadataClass;
    }

    public override RecordType RecordType => RecordType.ClassWithId;

    /// <inheritdoc />
    public override int ObjectId { get; }

    internal ClassRecord MetadataClass { get; }

    internal static ClassWithIdRecord Decode(
        BinaryReader reader,
        RecordMap recordMap)
    {
        int objectId = reader.ReadInt32();
        int metadataId = reader.ReadInt32();

        if (recordMap[metadataId] is not ClassRecord referencedRecord)
        {
            throw new SerializationException(SR.Serialization_InvalidReference);
        }

        return new ClassWithIdRecord(objectId, referencedRecord);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MetadataClass.MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
