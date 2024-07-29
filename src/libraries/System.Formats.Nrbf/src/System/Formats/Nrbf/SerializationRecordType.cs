// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

/// <summary>
/// Record type.
/// </summary>
/// <remarks>
///  <para>
///   The enumeration does not contain all values supported by the <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/954a0657-b901-4813-9398-4ec732fe8b32">
///   [MS-NRBF] 2.1.2.1</see>, but only those supported by the <see cref="NrbfDecoder"/>.
///  </para>
/// </remarks>
public enum SerializationRecordType
{
    /// <summary>
    /// The NRBF header (the first record in NRBF payload).
    /// </summary>
    SerializedStreamHeader,
    /// <summary>
    /// Class information that references another class record's metadata.
    /// </summary>
    ClassWithId,
    /// <summary>
    /// A system class information without type info.
    /// </summary>
    /// <remarks>Not supported by design.</remarks>
    SystemClassWithMembers,
    /// <summary>
    /// A class information with source library, but without type info.
    /// </summary>
    /// <remarks>Not supported by design.</remarks>
    ClassWithMembers,
    /// <summary>
    /// A system class information with type info.
    /// </summary>
    SystemClassWithMembersAndTypes,
    /// <summary>
    /// A class information with type info and the source library.
    /// </summary>
    ClassWithMembersAndTypes,
    /// <summary>
    /// A <see langword="string" />.
    /// </summary>
    BinaryObjectString,
    /// <summary>
    /// An array of any rank or element type.
    /// </summary>
    BinaryArray,
    /// <summary>
    /// A primitive value other than <see langword="string"/>.
    /// </summary>
    MemberPrimitiveTyped,
    /// <summary>
    /// A record that contains a reference to another record that contains the actual value.
    /// </summary>
    MemberReference,
    /// <summary>
    /// A single <see langword="null" /> value.
    /// </summary>
    ObjectNull,
    /// <summary>
    /// The record that marks the end of the binary format stream.
    /// </summary>
    MessageEnd,
    /// <summary>
    /// A record that associates a numeric identifier with a named library.
    /// </summary>
    BinaryLibrary,
    /// <summary>
    /// Multiple (less than 256) <see langword="null" /> values.
    /// </summary>
    ObjectNullMultiple256,
    /// <summary>
    /// Multiple <see langword="null" />.
    /// </summary>
    ObjectNullMultiple,
    /// <summary>
    /// A single-dimensional array of a primitive type.
    /// </summary>
    ArraySinglePrimitive,
    /// <summary>
    /// A single-dimensional array of <see cref="object" /> values.
    /// </summary>
    ArraySingleObject,
    /// <summary>
    /// A single-dimensional array of <see langword="string" /> values.
    /// </summary>
    ArraySingleString,
    /// <summary>
    /// A remote method call.
    /// </summary>
    /// <remarks>Not supported by design.</remarks>
    MethodCall = 21,
    /// <summary>
    /// An information returned by a remote method.
    /// </summary>
    /// <remarks>Not supported by design.</remarks>
    MethodReturn
}
