// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*============================================================
**
** Attribute: ReferenceAssemblyAttribute
**
** Purpose: Identifies an assembly as being a "reference 
**    assembly", meaning it contains public surface area but
**    no usable implementation.  Reference assemblies 
**    should be loadable for introspection, but not execution.
**
============================================================*/

namespace System.Runtime.CompilerServices
{
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
