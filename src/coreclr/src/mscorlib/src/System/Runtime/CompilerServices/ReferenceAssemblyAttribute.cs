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
    using System;
    
    [Serializable]
    [AttributeUsage(AttributeTargets.Assembly, AllowMultiple=false)]
    public sealed class ReferenceAssemblyAttribute : Attribute
    {
        private String _description;  // Maybe ".NET FX v4.0 SP1, partial trust"?

        public ReferenceAssemblyAttribute()
        {
        }

        public ReferenceAssemblyAttribute(String description)
        {
            _description = description;
        }

        public String Description
        {
            get { return _description; }
        }
    }
}
