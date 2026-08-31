// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public struct ValueTypeISerializable : ISerializable, ValueTypeBase
{
    public string Name { get; set; } = string.Empty;

    public BinaryTreeNodeWithEventsBase? Reference { get; set; }

    private ValueTypeISerializable(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        Name = serializationInfo.GetString(nameof(Name))!;
        Reference = (BinaryTreeNodeWithEventsISerializable?)serializationInfo.GetValue(nameof(Reference), typeof(BinaryTreeNodeWithEventsISerializable));
        BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}s");
    }

    public readonly void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Name), Name);
        info.AddValue(nameof(Reference), Reference);
    }

    [OnDeserialized]
    private readonly void OnDeserialized(StreamingContext context) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}p");
    public readonly void OnDeserialization(object? sender) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}i");
}
