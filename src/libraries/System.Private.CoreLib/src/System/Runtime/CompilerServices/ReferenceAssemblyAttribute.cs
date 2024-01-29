// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.CompilerServices
{
    /// <summary>
    /// Identifies an assembly as a reference assembly, which contains metadata but no executable code.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple = false)]
    public sealed class ReferenceAssemblyAttribute : Attribute
    {
        public ReferenceAssemblyAttribute()
        {
        }

        public ReferenceAssemblyAttribute(string? description)
        {
            Description = description;
        }

        public string? Description { get; }
    }
}
