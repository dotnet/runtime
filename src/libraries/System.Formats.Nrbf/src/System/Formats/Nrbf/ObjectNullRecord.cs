// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

/// <summary>
/// Represents a <see langword="null" />.
/// </summary>
/// <remarks>
/// ObjectNull records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/fe51522c-23d1-48dd-9913-c84894abc127">[MS-NRBF] 2.5.4</see>.
/// </remarks>
internal sealed class ObjectNullRecord : NullsRecord
{
    internal static ObjectNullRecord Instance { get; } = new();

    public override SerializationRecordType RecordType => SerializationRecordType.ObjectNull;

    internal override int NullCount => 1;

    internal override object? GetValue() => null;
}
