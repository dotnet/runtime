// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
///  Array information structure.
/// </summary>
/// <remarks>
///  <para>
///   <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/8fac763f-e46d-43a1-b360-80eb83d2c5fb">
///    [MS-NRBF] 2.4.2.1
///   </see>
///  </para>
/// </remarks>
[DebuggerDisplay("Length={Length}, {ArrayType}, rank={Rank}")]
internal readonly struct ArrayInfo
{
    internal ArrayInfo(int objectId, uint length, ArrayType arrayType = ArrayType.Single, int rank = 1)
    {
        ObjectId = objectId;
        Length = length;
        ArrayType = arrayType;
        Rank = rank;
    }

    internal int ObjectId { get; }

    internal uint Length { get; }

    internal ArrayType ArrayType { get; }

    internal int Rank { get; }

    internal static ArrayInfo Parse(BinaryReader reader)
        => new(reader.ReadInt32(), (uint)ParseValidArrayLength(reader));

    internal static int ParseValidArrayLength(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length is < 0 or > 2147483591) // Array.MaxLength
        {
            throw new SerializationException($"Invalid array length: {length}");
        }

        return length;
    }
}
