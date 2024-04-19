// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
using System;
using Xunit;

[AttributeUsage(AttributeTargets.Method)]
public class MyAttribute : Attribute
{
    public Type[] Types;
}

public class Test
{
    [MyAttribute(Types = new Type[]{typeof(string), typeof(void)})]
    [Fact]
    public static void TestEntryPoint() { }
}
