// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Resources.Extensions.Tests.Common.TestTypes;

[Serializable]
public class NodeWithNodeStruct
{
    public string? Value { get; set; }
    public NodeStruct NodeStruct { get; set; }
}
