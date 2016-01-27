// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
**
** Purpose: Container for assemblies.
**
**
=============================================================================*/

namespace System {
[Serializable]
    [AttributeUsage (AttributeTargets.All, Inherited=true, AllowMultiple=false)]
[System.Runtime.InteropServices.ComVisible(true)]
    public sealed class CLSCompliantAttribute : Attribute 
    {
        private bool m_compliant;

        public CLSCompliantAttribute (bool isCompliant)
        {
            m_compliant = isCompliant;
        }
        public bool IsCompliant 
        {
            get 
            {
                return m_compliant;
            }
        }
    }
}
