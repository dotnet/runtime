// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a class information with type info and the source library.
/// </summary>
/// <remarks>
/// ClassWithMembersAndTypes records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/847b0b6a-86af-4203-8ed0-f84345f845b9">[MS-NRBF] 2.3.2.1</see>.
/// </remarks>
internal sealed class ClassWithMembersAndTypesRecord : ClassRecord
{
    private ClassWithMembersAndTypesRecord(ClassInfo classInfo, MemberTypeInfo memberTypeInfo)
        : base(classInfo, memberTypeInfo)
    {
    }

    public override SerializationRecordType RecordType => SerializationRecordType.ClassWithMembersAndTypes;

    internal static ClassWithMembersAndTypesRecord Decode(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        ClassInfo classInfo = ClassInfo.Decode(reader);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Decode(reader, classInfo.MemberNames.Count, options, recordMap);
        SerializationRecordId libraryId = SerializationRecordId.Decode(reader);

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];
        classInfo.LoadTypeName(library, options);

        return new ClassWithMembersAndTypesRecord(classInfo, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
