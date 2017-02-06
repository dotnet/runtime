// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
** 
** 
** 
**
** Purpose: Holds state about A/G/R permissionsets in a callstack or appdomain
**          (Replacement for PermissionListSet)
**
=============================================================================*/

namespace System.Security
{
    using System.Globalization;
    using System.Reflection;
    using System.Runtime.InteropServices;
    using System.Security;
    using System.Security.Permissions;
    using System.Threading;
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;

    [Serializable]
    sealed internal class PermissionListSet
    {
        // Only internal (public) methods are creation methods and demand evaluation methods.
        // Scroll down to the end to see them.
        private PermissionSetTriple m_firstPermSetTriple;
        private ArrayList m_permSetTriples;

        internal PermissionListSet() {}
        private void Update(PermissionSetTriple currentTriple, PermissionSet in_g, PermissionSet in_r)
        {
            currentTriple.UpdateGrant(in_g);
            currentTriple.UpdateRefused(in_r);
        }

        // Called from the VM for HG CS construction        
        private void Update(PermissionSet in_g)
        {
            if (m_firstPermSetTriple == null)
                m_firstPermSetTriple = new PermissionSetTriple();
            Update(m_firstPermSetTriple, in_g, null);
        }

        // Private Demand evaluation functions - only called from the VM
        internal bool CheckDemandNoThrow(CodeAccessPermission demand)
        {
            // AppDomain permissions - no asserts. So there should only be one triple to work with
            Debug.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");
            

            
            PermissionToken permToken = null;
            if (demand != null)
                permToken = PermissionToken.GetToken(demand);

            return m_firstPermSetTriple.CheckDemandNoThrow(demand, permToken);
                

        }
        internal bool CheckSetDemandNoThrow(PermissionSet pSet)
        {
            // AppDomain permissions - no asserts. So there should only be one triple to work with
            Debug.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");

            
            return m_firstPermSetTriple.CheckSetDemandNoThrow(pSet);
        }

    }

}
