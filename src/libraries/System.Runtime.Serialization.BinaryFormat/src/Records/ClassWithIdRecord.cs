// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class information that references another class record's metadata.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/2d168388-37f4-408a-b5e0-e48dbce73e26">
///    [MS-NRBF] 2.3.2.5
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassWithIdRecord : ClassRecord
{
    private ClassWithIdRecord(int objectId, ClassRecord metadataClass) : base(metadataClass.ClassInfo)
    {
        ObjectId = objectId;
        MetadataClass = metadataClass;
    }

    public override RecordType RecordType => RecordType.ClassWithId;

    internal override AssemblyNameInfo LibraryName => MetadataClass.LibraryName;

    public override int ObjectId { get; }

    internal ClassRecord MetadataClass { get; }

    internal override int ExpectedValuesCount => MetadataClass.ExpectedValuesCount;

    internal static ClassWithIdRecord Parse(
        BinaryReader reader,
        RecordMap recordMap)
    {
        int objectId = reader.ReadInt32();
        int metadataId = reader.ReadInt32();

        if (recordMap[metadataId] is not ClassRecord referencedRecord)
        {
            throw new SerializationException();
        }

        return new(objectId, referencedRecord);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MetadataClass switch
        {
            ClassWithMembersAndTypesRecord classWithMembersAndTypes
                => classWithMembersAndTypes.MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count),
            SystemClassWithMembersAndTypesRecord systemClassWithMembersAndTypes
                => systemClassWithMembersAndTypes.MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count),
            // ClassWithMembersRecord and SystemClassWithMembersRecord allow for AnyData
            _ => (AllowedRecordTypes.AnyObject, PrimitiveType.None)
        };
}
