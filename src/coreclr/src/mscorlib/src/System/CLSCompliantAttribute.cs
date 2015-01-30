// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
