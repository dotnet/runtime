// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Runtime.CompilerServices
{
    /// <summary>Indicates the version of the memory safety rules used when the module was compiled.</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    [AttributeUsage(AttributeTargets.Module, Inherited = false, AllowMultiple = false)]
    public sealed class MemorySafetyRulesAttribute : Attribute
    {
        /// <summary>Initializes a new instance of the <see cref="MemorySafetyRulesAttribute"/> class.</summary>
        /// <param name="version">The version of the memory safety rules used when the module was compiled.</param>
        public MemorySafetyRulesAttribute(int version) => Version = version;

        /// <summary>Gets the version of the memory safety rules used when the module was compiled.</summary>
        public int Version { get; }
    }
}
