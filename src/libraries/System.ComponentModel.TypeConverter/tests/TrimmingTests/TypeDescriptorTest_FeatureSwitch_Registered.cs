// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections;
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
        s_usedProperty = c1.P3;

        if (!RunTest(targetType: typeof(ClassWithUnusedProperties), expectedPropertyCount: 3))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ClassWithUsedProperties), expectedPropertyCount: 3))
        {
            return -2;
        }

        return 100;
    }

    private static bool RunTest(Type targetType, int expectedPropertyCount)
    {
        TypeDescriptor.RegisterType<ClassWithUnusedProperties>();
        TypeDescriptor.RegisterType<ClassWithUsedProperties>();
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(targetType);
        return (properties.Count == expectedPropertyCount);
    }

    private class ClassWithUnusedProperties : BaseClass
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
    }

    private class ClassWithUsedProperties : BaseClass
    {
        public int P1 { get; set; }
        public int P2 { get; set; }
    }

    private class BaseClass
    {
        public int P3 { get; set; }
    }
}
