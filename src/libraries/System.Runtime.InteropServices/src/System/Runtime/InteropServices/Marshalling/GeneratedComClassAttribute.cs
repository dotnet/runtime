// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.InteropServices.Marshalling
{
    /// <summary>
    /// Specifies that the attributed type will be exposed to COM through source-generated COM and that the source generator should generate code for it.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class GeneratedComClassAttribute : Attribute
    {
    }
}
