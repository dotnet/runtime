// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

/// <summary>
/// Indicates the kind of an array for an NRBF BinaryArray record.
/// </summary>
/// <remarks>
/// BinaryArrayType enumeration is described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4dbbf3a8-6bc4-4dfc-aa7e-36a35be6ff58">[MS-NRBF] 2.4.1.1</see>.
/// </remarks>
internal enum BinaryArrayType : byte
{
    /// <summary>
    ///  A single-dimensional array.
    /// </summary>
    Single = 0,

    /// <summary>
    ///  An array whose elements are arrays. The elements of a jagged array can be of different dimensions and sizes.
    /// </summary>
    Jagged = 1,

    /// <summary>
    ///  A multi-dimensional rectangular array.
    /// </summary>
    Rectangular = 2
}
