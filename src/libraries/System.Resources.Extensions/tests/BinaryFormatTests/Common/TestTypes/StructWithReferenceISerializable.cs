// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public struct StructWithReferenceISerializable<T> : ISerializable
    where T : class
{
    public nint Value { get; set; }
    public T? Reference { get; set; }

    private StructWithReferenceISerializable(SerializationInfo info, StreamingContext context)
    {
        Reference = (T?)info.GetValue(nameof(Reference), typeof(T));
        Value = (nint)info.GetValue(nameof(Value), typeof(nint))!;
    }

    public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Reference), Reference);
        info.AddValue(nameof(Value), Value, typeof(nint));
    }
}
