// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

[AttributeUsage(AttributeTargets.Method)]
public class ExpectedILMappings : Attribute
{
    public int[] Debug { get; set; }
    public int[] Opts { get; set; }
}
