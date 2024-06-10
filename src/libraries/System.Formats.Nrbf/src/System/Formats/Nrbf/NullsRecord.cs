// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Formats.Nrbf;

internal abstract class NullsRecord : SerializationRecord
{
    internal abstract int NullCount { get; }

    public override SerializationRecordId Id => SerializationRecordId.NoId;
}
