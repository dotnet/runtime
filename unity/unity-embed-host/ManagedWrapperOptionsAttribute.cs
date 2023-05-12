// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Unity.CoreCLRHelpers;

/// <summary>
/// Used to control how the C# wrappers are generated for a method, parameter, or return value
/// </summary>
[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue | AttributeTargets.Method)]
public class ManagedWrapperOptionsAttribute : Attribute
{
    public ManagedWrapperOptionsAttribute(ManagedWrapperOptions value)
    {
    }

    public ManagedWrapperOptionsAttribute(ManagedWrapperOptions value, string parameterTypeName)
    {
    }
}
