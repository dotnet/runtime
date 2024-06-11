﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.BinaryFormat.Utils;

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Array information structure.
/// </summary>
/// <remarks>
/// ArrayInfo structures are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/8fac763f-e46d-43a1-b360-80eb83d2c5fb">[MS-NRBF] 2.4.2.1</see>.
/// </remarks>
[DebuggerDisplay("Length={Length}, {ArrayType}, rank={Rank}")]
internal readonly struct ArrayInfo
{
    internal const int MaxArrayLength = 2147483591; // Array.MaxLength

    internal ArrayInfo(int objectId, long totalElementsCount, BinaryArrayType arrayType = BinaryArrayType.Single, int rank = 1)
    {
        ObjectId = objectId;
        TotalElementsCount = totalElementsCount;
        ArrayType = arrayType;
        Rank = rank;
    }

    internal int ObjectId { get; }

    internal long TotalElementsCount { get; }

    internal BinaryArrayType ArrayType { get; }

    internal int Rank { get; }

    internal int GetSZArrayLength()
    {
        Debug.Assert(TotalElementsCount <= MaxArrayLength);
        return (int)TotalElementsCount;
    }

    internal static ArrayInfo Decode(BinaryReader reader)
        => new(reader.ReadInt32(), ParseValidArrayLength(reader));

    internal static int ParseValidArrayLength(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length is < 0 or > MaxArrayLength)
        {
            ThrowHelper.ThrowInvalidValue(length);
        }

        return length;
    }
}
