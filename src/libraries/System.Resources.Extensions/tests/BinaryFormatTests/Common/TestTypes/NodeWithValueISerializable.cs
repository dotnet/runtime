// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class NodeWithValueISerializable : ISerializable
{
    public NodeWithValueISerializable() { }

    protected NodeWithValueISerializable(SerializationInfo info, StreamingContext context)
    {
        Node = (NodeWithValueISerializable?)info.GetValue(nameof(Node), typeof(NodeWithValueISerializable));
        Value = info.GetInt32(nameof(Value));
    }

    public NodeWithValueISerializable? Node { get; set; }
    public int Value { get; set; }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Node), Node);
        info.AddValue(nameof(Value), Value);
    }
}
