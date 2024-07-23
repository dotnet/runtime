// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public struct StructWithObject
{
    public object? Value;
}

[Serializable]
public struct StructWithTwoObjects
{
    public object? Value;
    public object? Value2;
}

[Serializable]
public struct StructWithTwoObjectsISerializable : ISerializable
{
    public object? Value;
    public object? Value2;

    public StructWithTwoObjectsISerializable() { }

    private StructWithTwoObjectsISerializable(SerializationInfo info, StreamingContext context)
    {
        Value = info.GetValue(nameof(Value), typeof(object));
        Value2 = info.GetValue(nameof(Value2), typeof(object));
    }

    public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Value), typeof(object));
        info.AddValue(nameof(Value2), typeof(object));
    }
}

[Serializable]
public struct NodeStruct : ISerializable
{
    public NodeStruct() { }

    private NodeStruct(SerializationInfo info, StreamingContext context)
    {
        Node = (NodeWithNodeStruct)info.GetValue(nameof(Node), typeof(NodeWithNodeStruct))!;
    }

    public NodeWithNodeStruct? Node { get; set; }

    public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Node), Node, typeof(NodeWithNodeStruct));
    }
}
