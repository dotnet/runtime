// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class BinaryTreeNodeISerializable : BinaryTreeNode, ISerializable
{
    public BinaryTreeNodeISerializable() { }

    protected BinaryTreeNodeISerializable(SerializationInfo info, StreamingContext context)
    {
        Value = info.GetString(nameof(Value));
        Left = (BinaryTreeNode)info.GetValue(nameof(Left), typeof(BinaryTreeNode))!;
        Right = (BinaryTreeNode)info.GetValue(nameof(Right), typeof(BinaryTreeNode))!;
    }

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("Value", Value);
        info.AddValue("Left", Left, typeof(BinaryTreeNode));
        info.AddValue("Right", Right, typeof(BinaryTreeNode));
    }
}
