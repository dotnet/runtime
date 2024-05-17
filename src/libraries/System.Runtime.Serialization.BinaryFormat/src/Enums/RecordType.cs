// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Record type.
/// </summary>
/// <remarks>
///  <para>
///   The enumeration does not contain all values supported by the <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32">
///   [MS-NRBF] 2.1.2.1</see>, but only those supported by the <seealso cref="PayloadReader"/>.
///  </para>
/// </remarks>
public enum RecordType : byte
{
    SerializedStreamHeader = 0,
    ClassWithId = 1,
    // SystemClassWithMembers and ClassWithMembers are not supported by design (require type loading)
    SystemClassWithMembersAndTypes = 4,
    ClassWithMembersAndTypes = 5,
    BinaryObjectString = 6,
    BinaryArray = 7,
    MemberPrimitiveTyped = 8,
    MemberReference = 9,
    ObjectNull = 10,
    MessageEnd = 11,
    BinaryLibrary = 12,
    ObjectNullMultiple256 = 13,
    ObjectNullMultiple = 14,
    ArraySinglePrimitive = 15,
    ArraySingleObject = 16,
    ArraySingleString = 17
}
