// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class BinaryTreeNodeWithEventsISerializable : ISerializable, IDeserializationCallback, BinaryTreeNodeWithEventsBase
{
    public string Name { get; set; } = string.Empty;
    public BinaryTreeNodeWithEventsISerializable? Left { get; set; }
    public BinaryTreeNodeWithEventsISerializable? Right { get; set; }
    public ValueTypeBase? Value { get; set; }

    public BinaryTreeNodeWithEventsISerializable() { }

    protected BinaryTreeNodeWithEventsISerializable(SerializationInfo serializationInfo, StreamingContext streamingContext)
    {
        Name = serializationInfo.GetString(nameof(Name))!;
        Left = (BinaryTreeNodeWithEventsISerializable?)serializationInfo.GetValue(nameof(Left), typeof(BinaryTreeNodeWithEventsISerializable));
        Right = (BinaryTreeNodeWithEventsISerializable?)serializationInfo.GetValue(nameof(Right), typeof(BinaryTreeNodeWithEventsISerializable));
        Value = (ValueTypeBase?)serializationInfo.GetValue(nameof(Value), typeof(ValueTypeBase));
        BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}s");
    }

    [OnDeserialized]
    private void OnDeserialized(StreamingContext context) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}p");

    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue(nameof(Name), Name);
        info.AddValue(nameof(Left), Left);
        info.AddValue(nameof(Right), Right);
        info.AddValue(nameof(Value), Value);
    }

    public void OnDeserialization(object? sender) => BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{Name}i");
}

public class BinaryTreeNodeWithEventsTracker
{
    public static List<string> DeserializationOrder { get; } = new();
}
