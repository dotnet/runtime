// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

using BindingFlags = System.Reflection.BindingFlags;

class ILLinkLinkAttributes
{
    public static int Run()
    {
        ThrowIfTypeNotPresent(typeof(ILLinkLinkAttributes), nameof(TestDontRemoveAttribute));
        ThrowIfTypePresent(typeof(ILLinkLinkAttributes), nameof(TestRemoveAttribute));
        ThrowIfTypePresent(typeof(ILLinkLinkAttributes), nameof(TestMarkAllRemoveAttribute));
        ThrowIfAttributePresent(typeof(ILLinkLinkAttributes), nameof(_fieldWithCustomAttribute), nameof(TestRemoveAttribute));
        ThrowIfAttributeNotPresent(typeof(ILLinkLinkAttributes), nameof(_fieldWithCustomAttribute), nameof(TestDontRemoveAttribute));
        ThrowIfAttributePresent(typeof(ILLinkLinkAttributes), nameof(_fieldWithCustomAttribute), nameof(AllowNullAttribute));
        return 100;
    }

    [TestDontRemoveAttribute]
    [TestRemoveAttribute]
    [AllowNullAttribute]
    private string _fieldWithCustomAttribute = "Hello world";

    class TestDontRemoveAttribute : Attribute
    {
        public TestDontRemoveAttribute()
        {
        }
    }

    class TestRemoveAttribute : Attribute
    {
        public TestRemoveAttribute()
        {
        }
    }

    class TestMarkAllRemoveAttribute : Attribute
    {
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsTypePresent(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public) != null;

    private static void ThrowIfTypeNotPresent(Type testType, string typeName)
    {
        if (!IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }

    private static void ThrowIfTypePresent(Type testType, string typeName)
    {
        if (IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }

    private static bool IsAttributePresent(Type testType, string memberName, string attributeName)
    {
        foreach (var member in testType.GetMembers())
        {
            if (member.Name == memberName)
            {
                foreach (var attribute in member.GetCustomAttributes(false))
                {
                    if (attribute.GetType().Name == attributeName)
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    private static void ThrowIfAttributeNotPresent(Type testType, string memberName, string attributeName)
    {
        if (!IsAttributePresent(testType, memberName, attributeName))
        {
            throw new Exception(attributeName);
        }
    }

    private static void ThrowIfAttributePresent(Type testType, string memberName, string attributeName)
    {
        if (IsAttributePresent(testType, memberName, attributeName))
        {
            throw new Exception(attributeName);
        }
    }
}
