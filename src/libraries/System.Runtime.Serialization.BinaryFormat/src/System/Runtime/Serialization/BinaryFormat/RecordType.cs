// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Record type.
/// </summary>
/// <remarks>
///  <para>
///   The enumeration does not contain all values supported by the <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32">
///   [MS-NRBF] 2.1.2.1</see>, but only those supported by the <see cref="PayloadReader"/>.
///  </para>
/// </remarks>
#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
enum RecordType : byte
{
    /// <summary>
    /// The NRBF header (the first record in NRBF payload).
    /// </summary>
    SerializedStreamHeader = 0,
    /// <summary>
    /// Class information that references another class record's metadata.
    /// </summary>
    ClassWithId = 1,

    // SystemClassWithMembers and ClassWithMembers are not supported by design (require type loading)

    /// <summary>
    /// A system class information with type info.
    /// </summary>
    SystemClassWithMembersAndTypes = 4,
    /// <summary>
    /// A class information with type info and the source library.
    /// </summary>
    ClassWithMembersAndTypes = 5,
    /// <summary>
    /// A <see langword="string" />.
    /// </summary>
    BinaryObjectString = 6,
    /// <summary>
    /// An array of any rank or element type.
    /// </summary>
    BinaryArray = 7,
    /// <summary>
    /// A primitive value other than <see langword="string"/>.
    /// </summary>
    MemberPrimitiveTyped = 8,
    /// <summary>
    /// A record that contains a reference to another record that contains the actual value.
    /// </summary>
    MemberReference = 9,
    /// <summary>
    /// A single <see langword="null" /> value.
    /// </summary>
    ObjectNull = 10,
    /// <summary>
    /// The record that marks the end of the binary format stream.
    /// </summary>
    MessageEnd = 11,
    /// <summary>
    /// A record that associates a numeric identifier with a named library.
    /// </summary>
    BinaryLibrary = 12,
    /// <summary>
    /// Multiple (less than 256) <see langword="null" /> values.
    /// </summary>
    ObjectNullMultiple256 = 13,
    /// <summary>
    /// Multiple <see langword="null" />.
    /// </summary>
    ObjectNullMultiple = 14,
    /// <summary>
    /// A single-dimensional array of a primitive type.
    /// </summary>
    ArraySinglePrimitive = 15,
    /// <summary>
    /// A single-dimensional array of <see cref="object" /> values.
    /// </summary>
    ArraySingleObject = 16,
    /// <summary>
    /// A single-dimensional array of <see langword="string" /> values.
    /// </summary>
    ArraySingleString = 17
}
