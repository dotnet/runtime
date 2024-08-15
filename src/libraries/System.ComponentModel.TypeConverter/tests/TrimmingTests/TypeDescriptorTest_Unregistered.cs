// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

/// <summary>
/// Tests that the properties are preserved in a trimmed application.
/// </summary>
class Program
{
    static int Main(string[] args)
    {
        if (!RunTest(targetType: typeof(ClassWithUnusedProperties), expectedPropertyCount: 0, expectedAttributeCount: 0, expectedEventCount: 0))
        {
            return -1;
        }

         if (!RunTest(targetType: typeof(ClassWithAttribute), expectedPropertyCount: 0, expectedAttributeCount: 1, expectedEventCount: 0))
        {
            return -2;
        }

        if (!RunTest(targetType: typeof(ClassWithEvent), expectedPropertyCount: 0, expectedAttributeCount: 0, expectedEventCount: 0))
        {
            return -3;
        }

        return 100;
    }

    private static bool RunTest(Type targetType, int expectedPropertyCount, int expectedAttributeCount, int expectedEventCount)
    {
        // Some properties may be missing, but since the feature switch is off there is no InvalidOperationException.
        PropertyDescriptorCollection properties = TypeDescriptor.GetProperties(targetType);
        if (properties.Count != expectedPropertyCount)
        {
            return false;
        }

        // Attributes are preserved.
        AttributeCollection attributes = TypeDescriptor.GetAttributes(targetType);
        if (attributes.Count != expectedAttributeCount)
        {
            return false;
        }

        // Events are not preserved.
        EventDescriptorCollection events = TypeDescriptor.GetEvents(targetType);
        return (events.Count == expectedEventCount);
    }

    private class ClassWithUnusedProperties
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
