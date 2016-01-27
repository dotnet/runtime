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
    using System.Diagnostics.Contracts;

    [Serializable]
    sealed internal class PermissionListSet
    {
        // Only internal (public) methods are creation methods and demand evaluation methods.
        // Scroll down to the end to see them.
        private PermissionSetTriple m_firstPermSetTriple;
        private ArrayList m_permSetTriples;
#if FEATURE_COMPRESSEDSTACK
        private ArrayList m_zoneList;
        private ArrayList m_originList;
#endif // FEATURE_COMPRESSEDSTACK

        internal PermissionListSet() {}

        private void EnsureTriplesListCreated()
        {
            if (m_permSetTriples == null)
            {
                m_permSetTriples = new ArrayList();
                if (m_firstPermSetTriple != null)
                {
                    m_permSetTriples.Add(m_firstPermSetTriple);
                    m_firstPermSetTriple = null;
                }
            }
        }

#if FEATURE_PLS
        [System.Security.SecurityCritical]  // auto-generated
        internal void UpdateDomainPLS (PermissionListSet adPLS) {
            if (adPLS != null && adPLS.m_firstPermSetTriple != null)
                UpdateDomainPLS(adPLS.m_firstPermSetTriple.GrantSet, adPLS.m_firstPermSetTriple.RefusedSet);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void UpdateDomainPLS (PermissionSet grantSet, PermissionSet deniedSet) {
            Contract.Assert(m_permSetTriples == null, "m_permSetTriples != null");
            if (m_firstPermSetTriple == null)
                m_firstPermSetTriple = new PermissionSetTriple();

            // update the grant and denied sets
            m_firstPermSetTriple.UpdateGrant(grantSet);
            m_firstPermSetTriple.UpdateRefused(deniedSet);
        }
#endif // FEATURE_PLS

        private void Terminate(PermissionSetTriple currentTriple)
        {
            UpdateTripleListAndCreateNewTriple(currentTriple, null);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private void Terminate(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
#if FEATURE_COMPRESSEDSTACK
            this.UpdateZoneAndOrigin(pls);
#endif // FEATURE_COMPRESSEDSTACK
            this.UpdatePermissions(currentTriple, pls);
            this.UpdateTripleListAndCreateNewTriple(currentTriple, null);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private bool Update(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
#if FEATURE_COMPRESSEDSTACK
            this.UpdateZoneAndOrigin(pls);
#endif // FEATURE_COMPRESSEDSTACK
            return this.UpdatePermissions(currentTriple, pls);
        }

        [System.Security.SecurityCritical]  // auto-generated
        private bool Update(PermissionSetTriple currentTriple, FrameSecurityDescriptor fsd)
        {
#if FEATURE_COMPRESSEDSTACK
           FrameSecurityDescriptorWithResolver fsdWithResolver = fsd as FrameSecurityDescriptorWithResolver;
           if (fsdWithResolver != null)
           {
               return Update2(currentTriple, fsdWithResolver);
           }
#endif // FEATURE_COMPRESSEDSTACK

           // check imperative
           bool fHalt = Update2(currentTriple, fsd, false);
           if (!fHalt)            
           {
                // then declarative
                fHalt = Update2(currentTriple, fsd, true);
           }
           return fHalt;
        }

#if FEATURE_COMPRESSEDSTACK
        [System.Security.SecurityCritical]
        private bool Update2(PermissionSetTriple currentTriple, FrameSecurityDescriptorWithResolver fsdWithResolver)
        {
            System.Reflection.Emit.DynamicResolver resolver = fsdWithResolver.Resolver;
            CompressedStack dynamicCompressedStack = resolver.GetSecurityContext();
            dynamicCompressedStack.CompleteConstruction(null);
            return this.Update(currentTriple, dynamicCompressedStack.PLS);
        }
#endif // FEATURE_COMPRESSEDSTACK

        [System.Security.SecurityCritical]  // auto-generated
        private bool Update2(PermissionSetTriple currentTriple, FrameSecurityDescriptor fsd, bool fDeclarative)
        {
            // Deny
            PermissionSet deniedPset = fsd.GetDenials(fDeclarative);
            if (deniedPset != null)
            {
                currentTriple.UpdateRefused(deniedPset);
            }

            // permit only
            PermissionSet permitOnlyPset = fsd.GetPermitOnly(fDeclarative);
            if (permitOnlyPset != null)
            {
                currentTriple.UpdateGrant(permitOnlyPset);
            }

            // Assert all possible
            if (fsd.GetAssertAllPossible())
            {
                // If we have no grant set, it means that the only assembly we've seen on the stack so
                // far is mscorlib. Since mscorlib will always be fully trusted, the grant set of the
                // compressed stack is also FullTrust.
                if (currentTriple.GrantSet == null)
                    currentTriple.GrantSet = PermissionSet.s_fullTrust;

                UpdateTripleListAndCreateNewTriple(currentTriple, m_permSetTriples);
                currentTriple.GrantSet = PermissionSet.s_fullTrust;
                currentTriple.UpdateAssert(fsd.GetAssertions(fDeclarative));
                return true;
            }
                
            // Assert
            PermissionSet assertPset = fsd.GetAssertions(fDeclarative);
            if (assertPset != null)
            {
                if (assertPset.IsUnrestricted())
                {
                    // If we have no grant set, it means that the only assembly we've seen on the stack so
                    // far is mscorlib. Since mscorlib will always be fully trusted, the grant set of the
                    // compressed stack is also FullTrust.
                    if (currentTriple.GrantSet == null)
                        currentTriple.GrantSet = PermissionSet.s_fullTrust;

                    UpdateTripleListAndCreateNewTriple(currentTriple, m_permSetTriples);
                    currentTriple.GrantSet = PermissionSet.s_fullTrust;
                    currentTriple.UpdateAssert(assertPset);
                    return true;
                }

                PermissionSetTriple retTriple = currentTriple.UpdateAssert(assertPset);
                if (retTriple != null)
                {
                    EnsureTriplesListCreated();
                    m_permSetTriples.Add(retTriple);
                }
            }

            return false;
        }
        [System.Security.SecurityCritical]  // auto-generated
        private void Update(PermissionSetTriple currentTriple, PermissionSet in_g, PermissionSet in_r)
        {
#if FEATURE_COMPRESSEDSTACK
            ZoneIdentityPermission z;
            UrlIdentityPermission u;
            currentTriple.UpdateGrant(in_g, out z, out u);
            currentTriple.UpdateRefused(in_r);
            AppendZoneOrigin(z, u);
#else // !FEATURE_COMPRESEDSTACK
            currentTriple.UpdateGrant(in_g);
            currentTriple.UpdateRefused(in_r);
#endif // FEATURE_COMPRESSEDSTACK
        }

        // Called from the VM for HG CS construction        
        [System.Security.SecurityCritical]  // auto-generated
        private void Update(PermissionSet in_g)
        {
            if (m_firstPermSetTriple == null)
                m_firstPermSetTriple = new PermissionSetTriple();
            Update(m_firstPermSetTriple, in_g, null);
        }
        
#if FEATURE_COMPRESSEDSTACK
        private void UpdateZoneAndOrigin(PermissionListSet pls)
        {
            if (pls != null)
            {
                if (this.m_zoneList == null && pls.m_zoneList != null && pls.m_zoneList.Count > 0)
                    this.m_zoneList = new ArrayList();
                UpdateArrayList(this.m_zoneList, pls.m_zoneList);
                if (this.m_originList == null && pls.m_originList != null && pls.m_originList.Count > 0)
                    this.m_originList = new ArrayList();                                
                UpdateArrayList(this.m_originList, pls.m_originList);
            }
        }
#endif // FEATURE_COMPRESSEDSTACK

        [System.Security.SecurityCritical]  // auto-generated
        private bool UpdatePermissions(PermissionSetTriple currentTriple, PermissionListSet pls)
        {
            if (pls != null)
            {
                if (pls.m_permSetTriples != null)
                {
                    // DCS has an AGR List. So we need to add the AGR List
                    UpdateTripleListAndCreateNewTriple(currentTriple,pls.m_permSetTriples);
                }
                else
                {
                    // Common case: One AGR set
                    
                    PermissionSetTriple tmp_psTriple = pls.m_firstPermSetTriple;
                    PermissionSetTriple retTriple;
                    // First try and update currentTriple. Return value indicates if we can stop construction
                    if (currentTriple.Update(tmp_psTriple, out retTriple))
                        return true;
                    // If we got a non-null retTriple, what it means is that compression failed,
                    // and we now have 2 triples to deal with: retTriple and currentTriple.
                    // retTriple has to be appended first. then currentTriple.
                    if (retTriple != null)
                    {
                        EnsureTriplesListCreated();
                        // we just created a new triple...add the previous one (returned) to the list
                        m_permSetTriples.Add(retTriple);
                    }
                }
            }
            else
            {
                // pls can be null only outside the loop in CreateCompressedState
                UpdateTripleListAndCreateNewTriple(currentTriple, null);
            }
            

            return false;
            
        }


        private void UpdateTripleListAndCreateNewTriple(PermissionSetTriple currentTriple, ArrayList tripleList)
        {
            if (!currentTriple.IsEmpty())
            {
                if (m_firstPermSetTriple == null && m_permSetTriples == null)
                {
                    m_firstPermSetTriple = new PermissionSetTriple(currentTriple);
                }
                else
                {
                    EnsureTriplesListCreated();
                    m_permSetTriples.Add(new PermissionSetTriple(currentTriple));
                }
                currentTriple.Reset();
            }
            if (tripleList != null)
            {
                EnsureTriplesListCreated();
                m_permSetTriples.AddRange(tripleList);
            }
        }

        private static void UpdateArrayList(ArrayList current, ArrayList newList)
        {
            if (newList == null)
                return;

            for(int i=0;i < newList.Count; i++)
            {
                if (!current.Contains(newList[i]))
                    current.Add(newList[i]);
            }

        }

#if FEATURE_COMPRESSEDSTACK
        private void AppendZoneOrigin(ZoneIdentityPermission z, UrlIdentityPermission u)
        {

            if (z != null)
            {
                if (m_zoneList == null)
                    m_zoneList = new ArrayList();
                z.AppendZones(m_zoneList);
            }

            if (u != null)
            {
                if (m_originList == null)
                    m_originList = new ArrayList();
                u.AppendOrigin(m_originList);
            }
        }

[System.Security.SecurityCritical]  // auto-generated
[System.Runtime.InteropServices.ComVisible(true)]
        // public(internal) interface begins...
        // Creation functions
        static internal PermissionListSet CreateCompressedState(CompressedStack cs, CompressedStack innerCS)
        {
            // function that completes the construction of the compressed stack if not done so already (bottom half for demand evaluation)
            
            bool bHaltConstruction = false;
            if (cs.CompressedStackHandle == null)
                return null; //  FT case or Security off
   
            PermissionListSet pls = new PermissionListSet();
            PermissionSetTriple currentTriple = new PermissionSetTriple();
            int numDomains = CompressedStack.GetDCSCount(cs.CompressedStackHandle);
            for (int i=numDomains-1; (i >= 0 && !bHaltConstruction) ; i--)
            {
                DomainCompressedStack dcs = CompressedStack.GetDomainCompressedStack(cs.CompressedStackHandle, i);
                if (dcs == null)
                    continue; // we hit a FT Domain
                if (dcs.PLS == null)
                {
                    // We failed on some DCS
                    throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic")));
                }
                pls.UpdateZoneAndOrigin(dcs.PLS);
                pls.Update(currentTriple, dcs.PLS); 
                bHaltConstruction = dcs.ConstructionHalted;
            }
            if (!bHaltConstruction)
            {
                PermissionListSet tmp_pls = null;
                // Construction did not halt. 
                if (innerCS != null)
                {
                    innerCS.CompleteConstruction(null);
                    tmp_pls = innerCS.PLS;
                }
                pls.Terminate(currentTriple, tmp_pls);
            }
            else
            {
                pls.Terminate(currentTriple);
            }

            return pls;
        }

        [System.Security.SecurityCritical]  // auto-generated
        static internal PermissionListSet CreateCompressedState(IntPtr unmanagedDCS, out bool bHaltConstruction)
        {
            PermissionListSet pls = new PermissionListSet();
            PermissionSetTriple currentTriple = new PermissionSetTriple();

            PermissionSet tmp_g, tmp_r;
            // Construct the descriptor list
            int descCount = DomainCompressedStack.GetDescCount(unmanagedDCS);
            bHaltConstruction = false;
            for(int i=0; (i < descCount && !bHaltConstruction); i++)
            {
                FrameSecurityDescriptor fsd;
                Assembly assembly;
                if (DomainCompressedStack.GetDescriptorInfo(unmanagedDCS, i, out tmp_g, out tmp_r, out assembly, out fsd))
                {
                    // Got an FSD
                    bHaltConstruction = pls.Update(currentTriple, fsd);
                }
                else
                {
                    pls.Update(currentTriple, tmp_g, tmp_r);
                }
                
            }
            if (!bHaltConstruction)
            {
                // domain
                if (!DomainCompressedStack.IgnoreDomain(unmanagedDCS))
                {
                    DomainCompressedStack.GetDomainPermissionSets(unmanagedDCS, out tmp_g, out tmp_r);
                    pls.Update(currentTriple, tmp_g, tmp_r);
                }
            }
            pls.Terminate(currentTriple);


            // return the created object
            return pls;
            
        }
        [System.Security.SecurityCritical]  // auto-generated
        static internal PermissionListSet CreateCompressedState_HG()
        {
            PermissionListSet pls = new PermissionListSet();
            CompressedStack.GetHomogeneousPLS(pls);
            return pls;
        }
#endif // #if FEATURE_COMPRESSEDSTACK
        // Private Demand evaluation functions - only called from the VM
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemandNoThrow(CodeAccessPermission demand)
        {
            // AppDomain permissions - no asserts. So there should only be one triple to work with
            Contract.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");
            

            
            PermissionToken permToken = null;
            if (demand != null)
                permToken = PermissionToken.GetToken(demand);

            return m_firstPermSetTriple.CheckDemandNoThrow(demand, permToken);
                

        }
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemandNoThrow(PermissionSet pSet)
        {
            // AppDomain permissions - no asserts. So there should only be one triple to work with
            Contract.Assert(m_permSetTriples == null && m_firstPermSetTriple != null, "More than one PermissionSetTriple encountered in AD PermissionListSet");

            
            return m_firstPermSetTriple.CheckSetDemandNoThrow(pSet);
        }

        // Demand evauation functions
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            bool bRet = SecurityRuntime.StackContinue;
            if (m_permSetTriples != null)
            {
                for (int i=0; (i < m_permSetTriples.Count && bRet != SecurityRuntime.StackHalt) ; i++)
                {
                    PermissionSetTriple psTriple = (PermissionSetTriple)m_permSetTriples[i];
                    bRet = psTriple.CheckDemand(demand, permToken, rmh);
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                bRet = m_firstPermSetTriple.CheckDemand(demand, permToken, rmh);
            }

            return bRet;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemand(PermissionSet pset , RuntimeMethodHandleInternal rmh)
        {
            PermissionSet unused;
            CheckSetDemandWithModification(pset, out unused, rmh);
            return SecurityRuntime.StackHalt; //  CS demand check always terminates the stackwalk    
        }

        [System.Security.SecurityCritical]
        internal bool CheckSetDemandWithModification(PermissionSet pset, out PermissionSet alteredDemandSet, RuntimeMethodHandleInternal rmh)
        {
            bool bRet = SecurityRuntime.StackContinue;
            PermissionSet demandSet = pset;
            alteredDemandSet = null;
            if (m_permSetTriples != null)
            {
                for (int i=0; (i < m_permSetTriples.Count && bRet != SecurityRuntime.StackHalt) ; i++)
                {
                    PermissionSetTriple psTriple = (PermissionSetTriple)m_permSetTriples[i];
                    bRet = psTriple.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
                    if (alteredDemandSet != null)
                        demandSet = alteredDemandSet;
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                bRet = m_firstPermSetTriple.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
            }

            return bRet;
        }

        /// <summary>
        ///     Check to see if the PLS satisfies a demand for the special permissions encoded in flags
        /// </summary>
        /// <param name="flags">set of flags to check (See PermissionType)</param>
        [System.Security.SecurityCritical]  // auto-generated
        private bool CheckFlags(int flags)
        {
            Contract.Assert(flags != 0, "Invalid permission flag demand");

            bool check = true;

            if (m_permSetTriples != null)
            {
                for (int i = 0; i < m_permSetTriples.Count && check && flags != 0; i++)
                {
                    check &= ((PermissionSetTriple)m_permSetTriples[i]).CheckFlags(ref flags);
                }
            }
            else if (m_firstPermSetTriple != null)
            {
                check = m_firstPermSetTriple.CheckFlags(ref flags);
            }
            
            return check;
        }

        /// <summary>
        ///     Demand which succeeds if either a set of special permissions or a permission set is granted
        ///     to the call stack
        /// </summary>
        /// <param name="flags">set of flags to check (See PermissionType)</param>
        /// <param name="grantSet">alternate permission set to check</param>
        [System.Security.SecurityCritical]  // auto-generated
        internal void DemandFlagsOrGrantSet(int flags, PermissionSet grantSet)
        {
            if (CheckFlags(flags))
                return;

            CheckSetDemand(grantSet, RuntimeMethodHandleInternal.EmptyHandle);
        }

#if FEATURE_COMPRESSEDSTACK
        internal void GetZoneAndOrigin(ArrayList zoneList, ArrayList originList, PermissionToken zoneToken, PermissionToken originToken)
        {
            if (m_zoneList != null)
                zoneList.AddRange(m_zoneList);
            if (m_originList != null)
                originList.AddRange(m_originList);
        }
#endif
    }

}
