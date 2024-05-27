// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

[Flags]
internal enum AllowedRecordTypes : uint
{
    None = 0,
    SerializedStreamHeader = 1 << RecordType.SerializedStreamHeader,
    ClassWithId = 1 << RecordType.ClassWithId,
    SystemClassWithMembersAndTypes = 1 << RecordType.SystemClassWithMembersAndTypes,
    ClassWithMembersAndTypes = 1 << RecordType.ClassWithMembersAndTypes,
    BinaryObjectString = 1 << RecordType.BinaryObjectString,
    BinaryArray = 1 << RecordType.BinaryArray,
    MemberPrimitiveTyped = 1 << RecordType.MemberPrimitiveTyped,
    MemberReference = 1 << RecordType.MemberReference,
    ObjectNull = 1 << RecordType.ObjectNull,
    MessageEnd = 1 << RecordType.MessageEnd,
    BinaryLibrary = 1 << RecordType.BinaryLibrary,
    ObjectNullMultiple256 = 1 << RecordType.ObjectNullMultiple256,
    ObjectNullMultiple = 1 << RecordType.ObjectNullMultiple,
    ArraySinglePrimitive = 1 << RecordType.ArraySinglePrimitive,
    ArraySingleObject = 1 << RecordType.ArraySingleObject,
    ArraySingleString = 1 << RecordType.ArraySingleString,

    Nulls = ObjectNull | ObjectNullMultiple256 | ObjectNullMultiple,

    /// <summary>
    /// Any .NET object (a primitive, a reference type, a reference or single null).
    /// </summary>
    AnyObject = MemberPrimitiveTyped
        | ArraySingleObject | ArraySinglePrimitive | ArraySingleString | BinaryArray
        | ClassWithId | ClassWithMembersAndTypes | SystemClassWithMembersAndTypes
        | BinaryObjectString
        | MemberReference
        | ObjectNull,
}
