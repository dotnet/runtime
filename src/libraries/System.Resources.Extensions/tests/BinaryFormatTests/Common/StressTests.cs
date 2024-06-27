// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Resources.Extensions.Tests.Common.TestTypes;

namespace System.Resources.Extensions.Tests.Common;

public abstract class StressTests<T> : SerializationTest<T> where T : ISerializer
{
    [Theory]
    [InlineData(1000)]
    [InlineData(10000)]
    // This takes a few seconds
    // [InlineData(100000)]
    public void GraphDepth(int depth)
    {
        SimpleNode root = new();
        SimpleNode current = root;
        for (int i = 1; i < depth; i++)
        {
            current.Next = new();
            current = current.Next;
        }

        SimpleNode deserialized = (SimpleNode)Deserialize(Serialize(root));
        deserialized.Next.Should().NotBeNull();
        deserialized.Next.Should().NotBeSameAs(deserialized);
    }
}
