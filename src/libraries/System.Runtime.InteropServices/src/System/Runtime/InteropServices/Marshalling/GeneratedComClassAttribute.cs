// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Specifies that code should be generated to enable exposing the attributed class to COM.
    /// </summary>
    /// <remarks>
    /// This attribute is only valid on types that implement at least one <see cref="GeneratedComInterfaceAttribute"/>-attributed interface.
    /// </remarks>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedComClassAttribute : Attribute
    {
    }
}
