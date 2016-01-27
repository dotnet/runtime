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
    using System.Diagnostics.Contracts;


    [Serializable]
    sealed internal class PermissionSetTriple
    {
        static private volatile PermissionToken s_zoneToken;
        static private volatile PermissionToken s_urlToken;
        internal PermissionSet AssertSet;
        internal PermissionSet GrantSet;
        internal PermissionSet RefusedSet;
        internal PermissionSetTriple()
        {
            Reset();
        }
        internal PermissionSetTriple(PermissionSetTriple triple)
        {
            this.AssertSet = triple.AssertSet;
            this.GrantSet = triple.GrantSet;
            this.RefusedSet = triple.RefusedSet;
        }
        internal void Reset()
        {
            AssertSet = null;
            GrantSet = null;
            RefusedSet = null;
        }
        internal bool IsEmpty()
        {
            return (AssertSet == null && GrantSet == null && RefusedSet == null);
        }

        private PermissionToken ZoneToken
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (s_zoneToken == null)
                    s_zoneToken =  PermissionToken.GetToken(typeof(ZoneIdentityPermission));
                return s_zoneToken;
            }
        }            
        private PermissionToken UrlToken
        {
            [System.Security.SecurityCritical]  // auto-generated
            get
            {
                if (s_urlToken == null)
                    s_urlToken =  PermissionToken.GetToken(typeof(UrlIdentityPermission));
                return s_urlToken;
            }
        }            
        [System.Security.SecurityCritical]  // auto-generated
        internal bool Update(PermissionSetTriple psTriple, out PermissionSetTriple retTriple)
        {
            retTriple = null;
            retTriple = UpdateAssert(psTriple.AssertSet);
            // Special case: unrestricted assert. Note: dcs.Assert.IsUnrestricted => dcs.Grant.IsUnrestricted
            if (psTriple.AssertSet != null && psTriple.AssertSet.IsUnrestricted())
            {
                return true; // stop construction
            }
            UpdateGrant(psTriple.GrantSet);
            UpdateRefused(psTriple.RefusedSet);
            return false;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal PermissionSetTriple UpdateAssert(PermissionSet in_a)
        {
            PermissionSetTriple retTriple = null;
            if (in_a != null)
            {
                Contract.Assert((!in_a.IsUnrestricted() || RefusedSet == null), "Cannot be unrestricted or refused must be null");
                // if we're already asserting in_a, nothing to do
                if (in_a.IsSubsetOf(AssertSet))
                    return null;

                PermissionSet retPs;
                if (GrantSet != null)
                    retPs = in_a.Intersect(GrantSet); // Restrict the assert to what we've already been granted
                else
                {
                    GrantSet = new PermissionSet(true);
                    retPs = in_a.Copy(); // Currently unrestricted Grant: assert the whole assert set
                }
                bool bFailedToCompress = false;
                // removes anything that is already in the refused set from the assert set
                if (RefusedSet != null)
                {
                    retPs = PermissionSet.RemoveRefusedPermissionSet(retPs, RefusedSet, out bFailedToCompress);
                }
                if (!bFailedToCompress)
                    bFailedToCompress = PermissionSet.IsIntersectingAssertedPermissions(retPs, AssertSet);
                if (bFailedToCompress)
                {
                    retTriple = new PermissionSetTriple(this);
                    this.Reset();
                    this.GrantSet = retTriple.GrantSet.Copy();
                }

                if (AssertSet == null)
                    AssertSet = retPs;
                else
                    AssertSet.InplaceUnion(retPs);

            }
            return retTriple;
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal void UpdateGrant(PermissionSet in_g, out ZoneIdentityPermission z,out UrlIdentityPermission u)
        {
            z = null;
            u = null;
            if (in_g != null)
            {
                if (GrantSet == null)
                    GrantSet = in_g.Copy();
                else
                    GrantSet.InplaceIntersect(in_g);
                
                z = (ZoneIdentityPermission)in_g.GetPermission(ZoneToken);
                u = (UrlIdentityPermission)in_g.GetPermission(UrlToken);
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
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

        
        [System.Security.SecurityCritical]  // auto-generated
        static bool CheckAssert(PermissionSet pSet, CodeAccessPermission demand, PermissionToken permToken)
        {
            if (pSet != null)
            {
                pSet.CheckDecoded(demand, permToken);

                CodeAccessPermission perm = (CodeAccessPermission)pSet.GetPermission(demand);
            
                // If the assert set does contain the demanded permission, halt the stackwalk

                try
                {
                    if (pSet.IsUnrestricted() || demand.CheckAssert(perm))
                    {
                        return SecurityRuntime.StackHalt;
                    }
                }
                catch (ArgumentException)
                {
                }
            }
            return SecurityRuntime.StackContinue;
        }

        [System.Security.SecurityCritical]  // auto-generated
        static bool CheckAssert(PermissionSet assertPset, PermissionSet demandSet, out PermissionSet newDemandSet)
        {
            newDemandSet = null;
            if (assertPset!= null)
            {
                assertPset.CheckDecoded(demandSet);
                // If this frame asserts a superset of the demand set we're done

                if (demandSet.CheckAssertion(assertPset))
                    return SecurityRuntime.StackHalt;
                PermissionSet.RemoveAssertedPermissionSet(demandSet, assertPset, out newDemandSet);
            }
            return SecurityRuntime.StackContinue;
        }

        
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            if (CheckAssert(AssertSet, demand, permToken) == SecurityRuntime.StackHalt)
                return SecurityRuntime.StackHalt;

#pragma warning disable 618
            CodeAccessSecurityEngine.CheckHelper(GrantSet, RefusedSet, demand, permToken, rmh, null, SecurityAction.Demand, true);
#pragma warning restore 618

            return SecurityRuntime.StackContinue;
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemand(PermissionSet demandSet , out PermissionSet alteredDemandset, RuntimeMethodHandleInternal rmh)
        {
            alteredDemandset = null;
            
            if (CheckAssert(AssertSet, demandSet, out alteredDemandset) == SecurityRuntime.StackHalt)
                return SecurityRuntime.StackHalt;
            if (alteredDemandset != null)
                demandSet = alteredDemandset; // note that this does not modify demandSet external to this function.
#pragma warning disable 618
            CodeAccessSecurityEngine.CheckSetHelper(GrantSet, RefusedSet, demandSet, rmh, null, SecurityAction.Demand, true);
#pragma warning restore 618

            return SecurityRuntime.StackContinue;

        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemandNoThrow(CodeAccessPermission demand, PermissionToken permToken)
        {
            Contract.Assert(AssertSet == null, "AssertSet not null");
#pragma warning disable 618
            return CodeAccessSecurityEngine.CheckHelper(GrantSet, RefusedSet, demand, permToken, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
#pragma warning restore 618
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemandNoThrow(PermissionSet demandSet)
        {
            Contract.Assert(AssertSet == null, "AssertSet not null");

#pragma warning disable 618
            return CodeAccessSecurityEngine.CheckSetHelper(GrantSet, RefusedSet, demandSet, RuntimeMethodHandleInternal.EmptyHandle, null, SecurityAction.Demand, false);
#pragma warning restore 618
        }        
        /// <summary>
        ///     Check to see if the triple satisfies a demand for the permission represented by the flag.
        /// </summary>
        /// <remarks>
        ///     If the triple asserts for one of the bits in the flags, it is zeroed out.
        /// </remarks>
        /// <param name="flags">set of flags to check (See PermissionType)</param>
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckFlags(ref int flags)
        {
            if (AssertSet != null)
            {
                // remove any permissions which were asserted for
                int assertFlags = SecurityManager.GetSpecialFlags(AssertSet, null);
                if ((flags & assertFlags) != 0)
                    flags = flags & ~assertFlags;
            }

            return (SecurityManager.GetSpecialFlags(GrantSet, RefusedSet) & flags) == flags;
        }        
    }
}


