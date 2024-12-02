// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class BinaryTreeNodeWithEvents : IDeserializationCallback, BinaryTreeNodeWithEventsBase
{
    public string Name { get; set; } = string.Empty;
    public BinaryTreeNodeWithEvents? Left { get; set; }
    public BinaryTreeNodeWithEvents? Right { get; set; }
    public ValueTypeBase? Value { get; set; }

    public BinaryTreeNodeWithEvents() { }

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

public class BinaryTreeNodeWithEventsSurrogate : ISerializationSurrogate
{
    public void GetObjectData(object obj, SerializationInfo info, StreamingContext context) => throw new NotImplementedException();
    public object SetObjectData(object obj, SerializationInfo info, StreamingContext context, ISurrogateSelector? selector)
    {
        BinaryTreeNodeWithEvents node = (BinaryTreeNodeWithEvents)obj;
        node.Name = info.GetString("<Name>k__BackingField")!;
        node.Left = (BinaryTreeNodeWithEvents)info.GetValue("<Left>k__BackingField", typeof(BinaryTreeNodeWithEvents))!;
        node.Right = (BinaryTreeNodeWithEvents)info.GetValue("<Right>k__BackingField", typeof(BinaryTreeNodeWithEvents))!;
        node.Value = (ValueTypeBase)info.GetValue("<Value>k__BackingField", typeof(ValueTypeBase))!;
        BinaryTreeNodeWithEventsTracker.DeserializationOrder.Add($"{node.Name}s");

        return node;
    }
}
