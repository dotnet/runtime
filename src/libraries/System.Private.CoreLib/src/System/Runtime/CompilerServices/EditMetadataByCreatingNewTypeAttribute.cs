// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Reserved to be used by the compiler to emit new types
    /// rather than updating them when applying metadata updates
    /// </summary>

    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct, AllowMultiple = false)]
    public class EditMetadataByCreatingNewTypeAttribute : Attribute
    {
    }
}
