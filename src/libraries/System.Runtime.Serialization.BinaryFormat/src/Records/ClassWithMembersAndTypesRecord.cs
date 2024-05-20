// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Reflection.Metadata;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Class information with type info and the source library.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/847b0b6a-86af-4203-8ed0-f84345f845b9">
///    [MS-NRBF] 2.3.2.1
///   </see>
///  </para>
/// </remarks>
internal sealed class ClassWithMembersAndTypesRecord : ClassRecord
{
    private ClassWithMembersAndTypesRecord(ClassInfo classInfo, BinaryLibraryRecord library, MemberTypeInfo memberTypeInfo)
        : base(classInfo)
    {
        Library = library;
        MemberTypeInfo = memberTypeInfo;
    }

    public override RecordType RecordType => RecordType.ClassWithMembersAndTypes;

    internal override AssemblyNameInfo LibraryName => Library.LibraryName;

    internal BinaryLibraryRecord Library { get; }

    internal MemberTypeInfo MemberTypeInfo { get; }

    internal override int ExpectedValuesCount => MemberTypeInfo.Infos.Count;

    public override bool IsTypeNameMatching(Type type)
        => FormatterServices.GetTypeFullNameIncludingTypeForwards(type) == ClassInfo.Name.FullName
        && FormatterServices.GetAssemblyNameIncludingTypeForwards(type) == Library.LibraryName.FullName;

    internal static ClassWithMembersAndTypesRecord Parse(BinaryReader reader, RecordMap recordMap, PayloadOptions options)
    {
        ClassInfo classInfo = ClassInfo.Parse(reader, options);
        MemberTypeInfo memberTypeInfo = MemberTypeInfo.Parse(reader, classInfo.MemberNames.Count, options);
        int libraryId = reader.ReadInt32();

        BinaryLibraryRecord library = (BinaryLibraryRecord)recordMap[libraryId];

        return new(classInfo, library, memberTypeInfo);
    }

    internal override (AllowedRecordTypes allowed, PrimitiveType primitiveType) GetNextAllowedRecordType()
        => MemberTypeInfo.GetNextAllowedRecordType(MemberValues.Count);
}
