// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Reflection.Metadata;

namespace System.Formats.Nrbf;

internal abstract class NullsRecord : SerializationRecord
{
    internal abstract int NullCount { get; }

    public override SerializationRecordId Id => SerializationRecordId.NoId;

    public override TypeName TypeName
    {
        get
        {
            Debug.Fail($"TypeName should never be called on {GetType().Name}");
            return TypeName.Parse(GetType().Name.AsSpan());
        }
    }
}
