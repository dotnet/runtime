// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class BasicISerializableObject : ISerializable
{
    private readonly NonSerializablePair<int, string> _data;

    public BasicISerializableObject(int value1, string value2)
    {
        _data = new NonSerializablePair<int, string> { Value1 = value1, Value2 = value2 };
    }

    protected BasicISerializableObject(SerializationInfo info, StreamingContext context)
    {
        _data = new NonSerializablePair<int, string> { Value1 = info.GetInt32("Value1"), Value2 = info.GetString("Value2")! };
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Value1", _data.Value1);
        info.AddValue("Value2", _data.Value2);
    }

    public override bool Equals(object? obj)
    {
        if (obj is not BasicISerializableObject o)
            return false;
        if (_data is null || o._data is null)
            return _data == o._data;
        return _data.Value1 == o._data.Value1 && _data.Value2 == o._data.Value2;
    }

    public override int GetHashCode() => 1;
}
