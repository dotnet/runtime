// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Indicates the kind of an array for an NRBF BinaryArray record.
/// </summary>
/// <remarks>
/// BinaryArrayType enumeration is described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/4dbbf3a8-6bc4-4dfc-aa7e-36a35be6ff58">[MS-NRBF] 2.4.1.1</see>.
/// </remarks>
#if SYSTEM_RUNTIME_SERIALIZATION_BINARYFORMAT
public
#else
internal
#endif
enum BinaryArrayType : byte
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
    Rectangular = 2,

    /// <summary>
    ///  A single-dimensional array where the lower bound index is greater than 0.
    /// </summary>
    SingleOffset = 3,

    /// <summary>
    ///  A jagged array where the lower bound index is greater than 0.
    /// </summary>
    JaggedOffset = 4,

    /// <summary>
    ///  Multi-dimensional arrays where the lower bound index of at least one of the dimensions is greater than 0.
    /// </summary>
    RectangularOffset = 5,
}
