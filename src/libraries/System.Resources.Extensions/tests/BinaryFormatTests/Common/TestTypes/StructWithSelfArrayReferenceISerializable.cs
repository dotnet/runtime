// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public struct StructWithSelfArrayReferenceISerializable : ISerializable
{
    public nint Value { get; set; }
    public StructWithSelfArrayReferenceISerializable[]? Array { get; set; }

    public StructWithSelfArrayReferenceISerializable() { }

    private StructWithSelfArrayReferenceISerializable(SerializationInfo info, StreamingContext context)
    {
        Array = (StructWithSelfArrayReferenceISerializable[]?)info.GetValue(nameof(Array), typeof(StructWithSelfArrayReferenceISerializable[]))!;
        Value = (nint)info.GetValue(nameof(Value), typeof(nint))!;
    }

    public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Array), Array);
        info.AddValue(nameof(Value), Value, typeof(nint));
    }
}
