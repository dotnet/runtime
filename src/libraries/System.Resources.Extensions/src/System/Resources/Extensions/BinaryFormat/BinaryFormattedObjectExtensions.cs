// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Serialization.BinaryFormat;

namespace System.Resources.Extensions.BinaryFormat;

internal static class BinaryFormattedObjectExtensions
{
    internal static object GetMemberPrimitiveTypedValue(this SerializationRecord record)
    {
        Debug.Assert(record.RecordType is RecordType.MemberPrimitiveTyped or RecordType.BinaryObjectString);

        return record switch
        {
            PrimitiveTypeRecord<string> primitive => primitive.Value,
            PrimitiveTypeRecord<bool> primitive => primitive.Value,
            PrimitiveTypeRecord<byte> primitive => primitive.Value,
            PrimitiveTypeRecord<sbyte> primitive => primitive.Value,
            PrimitiveTypeRecord<char> primitive => primitive.Value,
            PrimitiveTypeRecord<short> primitive => primitive.Value,
            PrimitiveTypeRecord<ushort> primitive => primitive.Value,
            PrimitiveTypeRecord<int> primitive => primitive.Value,
            PrimitiveTypeRecord<uint> primitive => primitive.Value,
            PrimitiveTypeRecord<long> primitive => primitive.Value,
            PrimitiveTypeRecord<ulong> primitive => primitive.Value,
            PrimitiveTypeRecord<float> primitive => primitive.Value,
            PrimitiveTypeRecord<double> primitive => primitive.Value,
            PrimitiveTypeRecord<decimal> primitive => primitive.Value,
            PrimitiveTypeRecord<TimeSpan> primitive => primitive.Value,
            PrimitiveTypeRecord<DateTime> primitive => primitive.Value,
            PrimitiveTypeRecord<IntPtr> primitive => primitive.Value,
            _ => ((PrimitiveTypeRecord<UIntPtr>)record).Value
        };
    }
}
