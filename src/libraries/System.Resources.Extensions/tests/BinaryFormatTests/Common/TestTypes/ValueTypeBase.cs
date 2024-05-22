// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Serialization;

namespace FormatTests.Common.TestTypes;

public interface ValueTypeBase : IDeserializationCallback
{
    public string Name { get; set; }

    public BinaryTreeNodeWithEventsBase? Reference { get; set; }
}
