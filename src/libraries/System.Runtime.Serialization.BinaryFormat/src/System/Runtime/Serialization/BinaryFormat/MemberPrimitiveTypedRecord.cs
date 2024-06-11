// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

/// <summary>
/// Represents a primitive value other than <see langword="string"/>.
/// </summary>
/// <remarks>
/// MemberPrimitiveTyped records are described in <see href="https://learn.microsoft.com/openspecs/windows_protocols/ms-nrbf/c0a190b2-762c-46b9-89f2-c7dabecfc084">[MS-NRBF] 2.5.1</see>.
/// </remarks>
internal sealed class MemberPrimitiveTypedRecord<T> : PrimitiveTypeRecord<T>
    where T : unmanaged
{
    internal MemberPrimitiveTypedRecord(T value, int objectId = 0) : base(value) => ObjectId = objectId;

    public override RecordType RecordType => RecordType.MemberPrimitiveTyped;

    /// <inheritdoc />
    public override int ObjectId { get; }
}
