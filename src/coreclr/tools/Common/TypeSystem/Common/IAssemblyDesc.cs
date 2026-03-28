// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Reflection.Metadata;

namespace Internal.TypeSystem
{
    /// <summary>
    /// Optional interface a <see cref="ModuleDesc"/> should implement if it represents an assembly.
    /// </summary>
    public interface IAssemblyDesc
    {
        /// <summary>
        /// Gets the assembly name.
        /// </summary>
        AssemblyNameInfo GetName();

        /// <summary>
        /// Gets the simple assembly name
        /// </summary>
        ReadOnlySpan<byte> Name { get; }
    }
}
