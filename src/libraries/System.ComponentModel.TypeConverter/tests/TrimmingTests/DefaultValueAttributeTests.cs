// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;
using System.Reflection;

/// <summary>
/// Tests that using the [DefaultValue] ctor(Type, String) throws and other ctors with primitive values do not throw.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        object value;
        DefaultValueAttribute attribute;

        // Primitive types should not throw.
        attribute = typeof(TestClass).GetProperty(nameof(TestClass.MyPropertyWithDefaultValue))!.GetCustomAttribute<DefaultValueAttribute>()!;
        value = attribute.Value;
        if (value == null || (int)value != 42)
        {
            return -1;
        }

        attribute = typeof(TestClass).GetProperty(nameof(TestClass.MyPropertyWithDefaultValueUsingTypeConverter))!.GetCustomAttribute<DefaultValueAttribute>()!;
        try
        {
            value = attribute.Value;
        }
        catch (ArgumentException)
        {
            // The System.ComponentModel.DefaultValueAttribute.IsSupported feature switch is off so ArgumentException should be thrown.
            return 100;
        }

        return -2;
    }
}

public class TestClass
{
    [DefaultValue(42)]
    public int MyPropertyWithDefaultValue { get; set; }

    [DefaultValue(typeof(int), "42")]
    public int MyPropertyWithDefaultValueUsingTypeConverter { get; set; }
}
