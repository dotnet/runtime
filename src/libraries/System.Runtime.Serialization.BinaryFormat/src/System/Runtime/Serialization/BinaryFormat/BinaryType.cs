// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Identifies the remoting type of a class member or array item.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/054e5c58-be21-4c86-b1c3-f6d3ce17ec72">
///    [MS-NRBF] 2.1.2.2
///   </see>
///  </para>
/// </remarks>
internal enum BinaryType : byte
{
    /// <summary>
    ///  Type is defined by <see cref="PrimitiveType"/> and it is not a string.
    /// </summary>
    Primitive,

    /// <summary>
    ///  Type is <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/10b218f5-9b2b-4947-b4b7-07725a2c8127">
    ///  length prefixed string</see>.
    /// </summary>
    String,

    /// <summary>
    ///  Type is System.Object.
    /// </summary>
    Object,

    /// <summary>
    ///  Type is a standard .NET object.
    /// </summary>
    SystemClass,

    /// <summary>
    ///  Type is an object.
    /// </summary>
    Class,

    /// <summary>
    ///  Type is a single-dimensional array of objects.
    /// </summary>
    ObjectArray,

    /// <summary>
    ///  Type is a single-dimensional array of strings.
    /// </summary>
    StringArray,

    /// <summary>
    ///  Types is a single-dimensional array of a primitive type.
    /// </summary>
    PrimitiveArray
}
