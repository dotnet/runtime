// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

[Flags]
internal enum AllowedRecordTypes : uint
{
    None = 0,
    SerializedStreamHeader = 1 << SerializationRecordType.SerializedStreamHeader,
    ClassWithId = 1 << SerializationRecordType.ClassWithId,
    SystemClassWithMembersAndTypes = 1 << SerializationRecordType.SystemClassWithMembersAndTypes,
    ClassWithMembersAndTypes = 1 << SerializationRecordType.ClassWithMembersAndTypes,
    BinaryObjectString = 1 << SerializationRecordType.BinaryObjectString,
    BinaryArray = 1 << SerializationRecordType.BinaryArray,
    MemberPrimitiveTyped = 1 << SerializationRecordType.MemberPrimitiveTyped,
    MemberReference = 1 << SerializationRecordType.MemberReference,
    ObjectNull = 1 << SerializationRecordType.ObjectNull,
    MessageEnd = 1 << SerializationRecordType.MessageEnd,
    BinaryLibrary = 1 << SerializationRecordType.BinaryLibrary,
    ObjectNullMultiple256 = 1 << SerializationRecordType.ObjectNullMultiple256,
    ObjectNullMultiple = 1 << SerializationRecordType.ObjectNullMultiple,
    ArraySinglePrimitive = 1 << SerializationRecordType.ArraySinglePrimitive,
    ArraySingleObject = 1 << SerializationRecordType.ArraySingleObject,
    ArraySingleString = 1 << SerializationRecordType.ArraySingleString,

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
