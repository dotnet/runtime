// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection;
using Xunit;

public class TestSetValue
{
    public static readonly long MagicNumber = 42;
}

public class TestSetValueDirect
{
    public static readonly string MagicString = "";
}

public class Test_TrySetReadonlyStaticField2
{
    [Fact]
    public static int TestEntryPoint()
    {
        // Validate that the readonly static field cannot be set via reflection when the static constructor is triggered 
        // by the reflection SetValue operation itself.

        try
        {
            typeof(TestSetValue).GetField(nameof(TestSetValue.MagicNumber)).SetValue(null, 0x123456789);
            Console.WriteLine("FAILED: TestSetValue - Exception expected");
            return -1;
        }
        catch (FieldAccessException)
        {
            Console.WriteLine("TestSetValue - Caught expected exception");
        }

        try 
        {
            int i = 0;
            typeof(TestSetValueDirect).GetField(nameof(TestSetValueDirect.MagicString)).SetValueDirect(__makeref(i), "Hello");
            Console.WriteLine("FAILED: TestSetValueDirect - Exception expected");
            return -1;
        }
        catch (FieldAccessException)
        {
            Console.WriteLine("TestSetValueDirect - Caught expected exception");
        }
        return 100;
    }
}


