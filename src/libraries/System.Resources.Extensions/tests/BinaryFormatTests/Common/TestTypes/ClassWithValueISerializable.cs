// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class ClassWithValueISerializable<T> : ISerializable
{
    public T? Value { get; set; }

    public ClassWithValueISerializable() { }

    protected ClassWithValueISerializable(SerializationInfo info, StreamingContext context)
    {
        Value = (T)info.GetValue(nameof(Value), typeof(T))!;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Value), Value);
    }
}
