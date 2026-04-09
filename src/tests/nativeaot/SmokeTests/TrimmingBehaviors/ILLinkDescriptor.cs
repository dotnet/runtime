// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics.CodeAnalysis;

using BindingFlags = System.Reflection.BindingFlags;

class ILLinkDescriptor
{
    public static int Run()
    {
        ThrowIfMemberNotPresent(typeof(ILLinkDescriptor), nameof(methodKeptViaDescriptor));
        ThrowIfMemberNotPresent(typeof(ILLinkDescriptor), nameof(methodKeptViaStandaloneDescriptor));
        ThrowIfMemberPresent(typeof(ILLinkDescriptor), nameof(methodNotKept));
        ThrowIfMemberNotPresent(typeof(ILLinkDescriptor), nameof(fieldKeptViaDescriptor));
        ThrowIfMemberNotPresent(typeof(ILLinkDescriptor), nameof(PropertyKeptViaDescriptor));
        ThrowIfMemberNotPresent(typeof(ILLinkDescriptor), nameof(EventKeptViaDescriptor));
        ThrowIfTypeNotPresent(typeof(ILLinkDescriptor), nameof(NestedTypeKeptViaDescriptor));
        ThrowIfTypeNotPresent("LibraryClass, ShimLibrary");
        ThrowIfTypePresent(typeof(ILLinkDescriptor), nameof(NestedTypeNonKept));
        return 100;
    }

    public static void methodKeptViaDescriptor()
    {
    }

    public static void methodKeptViaStandaloneDescriptor()
    {
    }

    public static void methodNotKept()
    {
    }

    public int fieldKeptViaDescriptor;

    public int PropertyKeptViaDescriptor { get; set; }

    public event EventHandler EventKeptViaDescriptor { add { } remove { } }

    class NestedTypeKeptViaDescriptor
    {
    }

    class NestedTypeNonKept
    {
    }

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsTypePresent(Type testType, string typeName) => testType.GetNestedType(typeName, BindingFlags.NonPublic | BindingFlags.Public) != null;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2057:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsTypePresent(string typeName) => Type.GetType(typeName) != null;

    [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070:UnrecognizedReflectionPattern",
        Justification = "That's the point")]
    private static bool IsMemberPresent(Type testType, string memberName) {
        foreach (var member in testType.GetMembers())
        {
            if (member.Name == memberName)
                return true;
        }
        return false;
    }

    private static void ThrowIfTypeNotPresent(Type testType, string typeName)
    {
        if (!IsTypePresent(testType, typeName))
        {
            throw new Exception(typeName);
        }
    }

    private static void ThrowIfTypeNotPresent(string typeName)
    {
        if (!IsTypePresent(typeName))
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

    private static void ThrowIfMemberNotPresent(Type testType, string memberName)
    {
        if (!IsMemberPresent(testType, memberName))
        {
            throw new Exception(memberName);
        }
    }

    private static void ThrowIfMemberPresent(Type testType, string memberName)
    {
        if (IsMemberPresent(testType, memberName))
        {
            throw new Exception(memberName);
        }
    }
}
