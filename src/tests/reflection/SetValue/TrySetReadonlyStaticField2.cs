// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class SetValueScenario
{
    public static readonly long MagicNumber = 42;
}

public class SetValueDirectScenario
{
    public static readonly string MagicString = "";
}

// Validate that the readonly static field cannot be set via reflection when the static constructor is triggered
// by the reflection SetValue operation itself.
public class TrySetReadonlyStaticField2
{
    [Fact]
    public static void TestSetValue()
    {
        Assert.Throws<FieldAccessException>(() =>
        {
            typeof(SetValueScenario).GetField(nameof(SetValueScenario.MagicNumber)).SetValue(null, 0x123456789);
        });
    }

    [Fact]
    public static void TestSetValueDirect()
    {
        Assert.Throws<FieldAccessException>(() =>
        {
            int i = 0;
            typeof(SetValueDirectScenario).GetField(nameof(SetValueDirectScenario.MagicString)).SetValueDirect(__makeref(i), "Hello");
        });
    }
}
