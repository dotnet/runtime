// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
