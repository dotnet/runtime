// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Formats.Nrbf.Utils;

namespace System.Formats.Nrbf;

/// <summary>
/// Array information structure.
/// </summary>
/// <remarks>
/// ArrayInfo structures are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/8fac763f-e46d-43a1-b360-80eb83d2c5fb">[MS-NRBF] 2.4.2.1</see>.
/// </remarks>
[DebuggerDisplay("{ArrayType}, rank={Rank}")]
internal readonly struct ArrayInfo
{
#if NET
    internal static int MaxArrayLength => Array.MaxLength; // dynamic lookup in case the value changes in a future runtime
#else
    internal const int MaxArrayLength = 2147483591; // hardcode legacy Array.MaxLength for downlevel runtimes
#endif

    internal ArrayInfo(SerializationRecordId id, long totalElementsCount, BinaryArrayType arrayType = BinaryArrayType.Single, int rank = 1)
    {
        Id = id;
        FlattenedLength = totalElementsCount;
        ArrayType = arrayType;
        Rank = rank;
    }

    internal SerializationRecordId Id { get; }

    internal long FlattenedLength { get; }

    internal BinaryArrayType ArrayType { get; }

    internal int Rank { get; }

    internal int GetSZArrayLength()
    {
        Debug.Assert(FlattenedLength <= MaxArrayLength);
        return (int)FlattenedLength;
    }

    internal static ArrayInfo Decode(BinaryReader reader)
        => new(SerializationRecordId.Decode(reader), ParseValidArrayLength(reader));

    internal static int ParseValidArrayLength(BinaryReader reader)
    {
        int length = reader.ReadInt32();

        if (length < 0 || length > MaxArrayLength)
        {
            ThrowHelper.ThrowInvalidValue(length);
        }

        return length;
    }
}
