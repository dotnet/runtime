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

        if (!RunTest(targetType: typeof(ClassWithUnusedProperties), expectedPropertyCount: 2, expectedAttributeCount: 0, expectedEventCount: 0))
        {
            return -1;
        }

        if (!RunTest(targetType: typeof(ClassWithUsedProperties), expectedPropertyCount: 2, expectedAttributeCount: 0, expectedEventCount: 0))
        {
            return -2;
        }

        if (!RunTest(targetType: typeof(ClassWithAttribute), expectedPropertyCount: 0, expectedAttributeCount: 1, expectedEventCount: 0))
        {
            return -3;
        }

        if (!RunTest(targetType: typeof(ClassWithEvent), expectedPropertyCount: 0, expectedAttributeCount: 0, expectedEventCount: 1))
        {
            return -4;
        }

        return 100;
    }

    private static bool RunTest(Type targetType, int expectedPropertyCount, int expectedAttributeCount, int expectedEventCount)
    {
        TypeDescriptor.RegisterType<ClassWithUnusedProperties>();
        TypeDescriptor.RegisterType<ClassWithUsedProperties>();
        TypeDescriptor.RegisterType<ClassWithEvent>();
        // ClassWithAttribute is not registered; trimmer does not remove attributes.

        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(targetType);
        if (properties.Count != expectedPropertyCount)
        {
            return false;
        }

        AttributeCollection attributes = TypeDescriptor.GetAttributes(targetType);
        if (attributes.Count != expectedAttributeCount)
        {
            return false;
        }

        EventDescriptorCollection events = TypeDescriptor.GetEvents(targetType);
        return (events.Count == expectedEventCount);
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

    [MyCustom]
    private class ClassWithAttribute { }

    [AttributeUsage(AttributeTargets.All)]
    private class MyCustomAttribute : Attribute { }

    private class ClassWithEvent
    {
#pragma warning disable CS0067 // The event is never used
        public event EventHandler MyEvent;
#pragma warning restore CS0067
    }
}
