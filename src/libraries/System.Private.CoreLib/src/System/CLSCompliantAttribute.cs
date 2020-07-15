// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

/*=============================================================================
**
**
**
** Purpose: Container for assemblies.
**
**
=============================================================================*/

namespace System
{
    [AttributeUsage(AttributeTargets.All, Inherited = true, AllowMultiple = false)]
    public sealed class CLSCompliantAttribute : Attribute
    {
        private readonly bool _compliant;

        public CLSCompliantAttribute(bool isCompliant)
        {
            _compliant = isCompliant;
        }
        public bool IsCompliant => _compliant;
    }
}
