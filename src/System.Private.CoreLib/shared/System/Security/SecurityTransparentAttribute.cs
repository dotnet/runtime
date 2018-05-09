// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security
{
    // SecurityTransparentAttribute:
    // Indicates the assembly contains only transparent code.
    // Security critical actions will be restricted or converted into less critical actions. For example,
    // Assert will be restricted, SuppressUnmanagedCode, LinkDemand, unsafe, and unverifiable code will be converted
    // into Full-Demands.

    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false, Inherited = false)]
    public sealed class SecurityTransparentAttribute : Attribute
    {
        public SecurityTransparentAttribute() { }
    }
}

