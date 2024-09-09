// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources.Extensions.Tests.Common.TestTypes;

namespace System.Resources.Extensions.Tests.Common;

public abstract class CycleTests<T> : SerializationTest<T> where T : ISerializer
{
    [Fact]
    public void SelfReferencingISerializableObject()
    {
        NodeWithValueISerializable node = new()
        {
            Value = 42
        };

        node.Node = node;

        Stream stream = Serialize(node);

        var deserialized = (NodeWithValueISerializable)Deserialize(stream);

        Assert.Equal(42, deserialized.Value);
        Assert.Same(deserialized, deserialized.Node);
    }

    [Fact]
    public void SimpleLoopingISerializableObjects()
    {
        NodeWithValueISerializable node1 = new()
        {
            Value = 42
        };

        NodeWithValueISerializable node2 = new()
        {
            Value = 43
        };

        node1.Node = node2;
        node2.Node = node1;

        Stream stream = Serialize(node1);

        var deserialized = (NodeWithValueISerializable)Deserialize(stream);
        Assert.Equal(42, deserialized.Value);
        Assert.Equal(43, deserialized.Node!.Value);
        Assert.Same(deserialized, deserialized.Node.Node);
    }

    [Fact]
    public virtual void BackPointerToISerializableClass()
    {
        ClassWithValueISerializable<StructWithReferenceISerializable<object>> @object = new();
        StructWithReferenceISerializable<object> structValue = new() { Value = 42, Reference = @object };
        @object.Value = structValue;

        Stream stream = Serialize(@object);

        // BinaryFormatter doesn't handle this round trip.
        var deserialized = (ClassWithValueISerializable<StructWithReferenceISerializable<object>>)Deserialize(stream);
        Assert.Equal(42, deserialized.Value.Value);
        Assert.Same(deserialized, deserialized.Value.Reference);
    }

    [Fact]
    public void BackPointerToArray()
    {
        var nints = new StructWithSelfArrayReferenceISerializable[3];
        nints[0] = new() { Value = 42, Array = nints };
        nints[1] = new() { Value = 43, Array = nints };
        nints[2] = new() { Value = 44, Array = nints };

        Stream stream = Serialize(nints);
        var deserialized = (StructWithSelfArrayReferenceISerializable[])Deserialize(stream);

        Assert.Equal(42, deserialized[0].Value);
        Assert.Equal(43, deserialized[1].Value);
        Assert.Equal(44, deserialized[2].Value);
        Assert.Same(deserialized, deserialized[0].Array);
    }

    [Fact]
    public void BackPointerFromNestedStruct()
    {
        NodeWithNodeStruct node = new() { Value = "Root" };
        node.NodeStruct = new NodeStruct { Node = node };

        NodeWithNodeStruct deserialized = (NodeWithNodeStruct)Deserialize(Serialize(node));

        Assert.Same(deserialized, deserialized.NodeStruct.Node);
        Assert.Equal("Root", deserialized.Value);
    }

    [Fact]
    public void IndirectBackPointerFromNestedStruct()
    {
        NodeWithNodeStruct node = new() { Value = "Root" };
        NodeWithNodeStruct node2 = new() { Value = "Node2" };
        node.NodeStruct = new() { Node = node2 };
        node2.NodeStruct = new() { Node = node };

        NodeWithNodeStruct deserialized = (NodeWithNodeStruct)Deserialize(Serialize(node));

        Assert.Equal("Root", deserialized.Value);
        Assert.Same(deserialized, deserialized.NodeStruct.Node!.NodeStruct.Node);
        Assert.Equal("Node2", deserialized.NodeStruct.Node!.Value);
    }

    [Fact]
    public void BinaryTreeCycles()
    {
        BinaryTreeNode root = new();
        root.Left = root;

        BinaryTreeNode deserialized = (BinaryTreeNode)Deserialize(Serialize(root));
        Assert.Same(deserialized, deserialized.Left);
        Assert.Null(deserialized.Right);

        root.Right = root.Left;
        deserialized = (BinaryTreeNode)Deserialize(Serialize(root));
        Assert.Same(deserialized, deserialized.Left);
        Assert.Same(deserialized, deserialized.Right);
    }

    [Fact]
    public void BinaryTreeCycles_ISerializable()
    {
        BinaryTreeNodeISerializable root = new();
        root.Left = root;

        var deserialized = (BinaryTreeNodeISerializable)Deserialize(Serialize(root));
        Assert.Same(deserialized, deserialized.Left);
        Assert.Null(deserialized.Right);

        root.Right = root.Left;
        deserialized = (BinaryTreeNodeISerializable)Deserialize(Serialize(root));
        Assert.Same(deserialized, deserialized.Left);
        Assert.Same(deserialized, deserialized.Right);
    }
}
