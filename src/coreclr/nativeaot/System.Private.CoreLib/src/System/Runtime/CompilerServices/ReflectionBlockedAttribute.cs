// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    // When applied to a type this custom attribute cause the type to be treated as reflection blocked.
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct | AttributeTargets.Delegate | AttributeTargets.Enum | AttributeTargets.Interface, Inherited = false, AllowMultiple = false)]
    public class ReflectionBlockedAttribute : Attribute
    {
    }
}
