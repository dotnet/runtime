// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace FormatTests.Common.TestTypes;

[Serializable]
public struct ValueType : ValueTypeBase
{
    public ValueType()
    {
    }

    public string Name { get; set; } = string.Empty;

    public BinaryTreeNodeWithEventsBase? Reference { get; set; }

    public readonly void OnDeserialization(object? sender) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}i");

    [OnDeserialized]
    private readonly void OnDeserialized(StreamingContext context) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}p");
}
