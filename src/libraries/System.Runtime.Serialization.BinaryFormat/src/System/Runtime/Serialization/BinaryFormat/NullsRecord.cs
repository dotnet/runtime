// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization.BinaryFormat;

internal abstract class NullsRecord : SerializationRecord
{
    internal abstract int NullCount { get; }

    public override int ObjectId => NoId;
}
