// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

/*=============================================================================
**
**
** 
**
** Purpose: Container class for holding an AppDomain's Grantset and Refused sets.
**          Also used for CompressedStacks which brings in the third PermissionSet.
**          Hence, the name PermissionSetTriple. 
**
=============================================================================*/

namespace System.Security
{
    using IEnumerator = System.Collections.IEnumerator;
    using System.Security;
    using System.Security.Permissions;
    using System.Runtime.InteropServices;
    using System.Diagnostics;
    using System.Diagnostics.Contracts;


    [Serializable]
    sealed internal class PermissionSetTriple
    {
        internal PermissionSet AssertSet;
        internal PermissionSet GrantSet;
        internal PermissionSet RefusedSet;
        internal PermissionSetTriple()
        {
            Reset();
        }
        internal void Reset()
        {
            AssertSet = null;
            GrantSet = null;
            RefusedSet = null;
        }

        internal void UpdateGrant(PermissionSet in_g)
        {
            if (in_g != null)
            {
                if (GrantSet == null)
                    GrantSet = in_g.Copy();
                else
                    GrantSet.InplaceIntersect(in_g);
            }
        }
        internal void UpdateRefused(PermissionSet in_r)
        {
            if (in_r != null)
            {
                if (RefusedSet == null)
                    RefusedSet = in_r.Copy();
                else
                    RefusedSet.InplaceUnion(in_r);
            }
        } 
        
        internal bool CheckDemandNoThrow(CodeAccessPermission demand, PermissionToken permToken)
        {
            Debug.Assert(AssertSet == null, "AssertSet not null");
#pragma warning disable 618
            return CodeAccessSecurityEngine.CheckHelper(GrantSet, RefusedSet, demand, permToken, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
#pragma warning restore 618
        }
        internal bool CheckSetDemandNoThrow(PermissionSet demandSet)
        {
            Debug.Assert(AssertSet == null, "AssertSet not null");

#pragma warning disable 618
            return CodeAccessSecurityEngine.CheckSetHelper(GrantSet, RefusedSet, demandSet, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
#pragma warning restore 618
        }        
    }
}


