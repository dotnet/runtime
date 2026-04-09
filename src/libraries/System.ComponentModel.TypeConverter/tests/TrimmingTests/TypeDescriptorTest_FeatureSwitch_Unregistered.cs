// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

/// <summary>
/// Tests that the properties are preserved in a trimmed application.
/// </summary>
class Program
{
    public static int s_usedProperty;

    static int Main(string[] args)
    {
        var c1 = new ClassWithUsedProperties();
        s_usedProperty = c1.P1;
        s_usedProperty = c1.P2;

        if (!RunTest(targetType: typeof(ClassWithUnusedProperties)))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ClassWithUsedProperties)))
        {
            return -2;
        }

        return 100;
    }

    private static bool RunTest(Type targetType)
    {
        try
        {
            // The feature switch is on so InvalidOperationException should be thrown.
            PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(targetType);
        }
        catch (InvalidOperationException)
        {
            return true;
        }

        return false;
    }

    private class ClassWithUnusedProperties
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
    }

    private class ClassWithUsedProperties
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
    }
}
