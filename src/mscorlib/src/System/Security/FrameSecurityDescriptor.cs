// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security {
    using System.Text;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System;
    using System.Collections;
    using System.Security.Permissions;
    using System.Globalization;
    using System.Runtime.ConstrainedExecution;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;
#if !FEATURE_PAL
    using Microsoft.Win32.SafeHandles;
#endif    
     //FrameSecurityDescriptor.cs
    //
    // Internal use only.
    // DO NOT DOCUMENT
    //

    [Serializable]
    internal class FrameSecurityDescriptor
    {
    
        /*    EE has native FrameSecurityDescriptorObject definition in object.h
                Make sure to update that structure as well, if you make any changes here.
        */
        private PermissionSet       m_assertions;    // imperative asserts
        private PermissionSet       m_denials;      // imperative denials
        private PermissionSet       m_restriction;      // imperative permitonlys
        private PermissionSet       m_DeclarativeAssertions;
        private PermissionSet       m_DeclarativeDenials;
        private PermissionSet       m_DeclarativeRestrictions;

#if !FEATURE_PAL
        // if this frame contains a call to any WindowsIdentity.Impersonate(), 
        // we save the previous SafeTokenHandles here (in the next two fields)
        // Used during exceptionstackwalks to revert impersonation before calling filters        
        [System.Security.SecurityCritical] // auto-generated
        [NonSerialized]
        private SafeAccessTokenHandle     m_callerToken; 
        [System.Security.SecurityCritical] // auto-generated
        [NonSerialized]
        private SafeAccessTokenHandle     m_impToken;                               
#endif

        private bool                m_AssertFT;
        private bool                m_assertAllPossible;
#pragma warning disable 169 
        private bool                m_declSecComputed; // set from the VM to indicate that the declarative A/PO/D on this frame has been populated
#pragma warning restore 169

 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void IncrementOverridesCount();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void DecrementOverridesCount();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void IncrementAssertCount();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void DecrementAssertCount();


        // Default constructor.
        internal FrameSecurityDescriptor()
        {
            //m_flags = 0;
        }
        //-----------------------------------------------------------+
        // H E L P E R
        //-----------------------------------------------------------+
        
        private PermissionSet CreateSingletonSet(IPermission perm)
        {
            PermissionSet permSet = new PermissionSet(false);
            permSet.AddPermission(perm.Copy());
            return permSet;
        }
    
        //-----------------------------------------------------------+
        // A S S E R T
        //-----------------------------------------------------------+

        internal bool HasImperativeAsserts() 
        {
            // we store declarative actions in both fields, so check if they are different
            return (m_assertions != null);
        }
        internal bool HasImperativeDenials() 
        {
            // we store declarative actions in both fields, so check if they are different
            return (m_denials != null);
        }
        internal bool HasImperativeRestrictions() 
        {
            // we store declarative actions in both fields, so check if they are different
            return (m_restriction != null);
        }
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetAssert(IPermission perm)
        {            
            m_assertions = CreateSingletonSet(perm);
            IncrementAssertCount();
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetAssert(PermissionSet permSet)
        {            
            m_assertions = permSet.Copy();
            m_AssertFT = m_AssertFT || m_assertions.IsUnrestricted();
            IncrementAssertCount();
        }
        
        internal PermissionSet GetAssertions(bool fDeclarative)
        {
            return (fDeclarative) ? m_DeclarativeAssertions : m_assertions;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void SetAssertAllPossible()
        {
            m_assertAllPossible = true;
            IncrementAssertCount();
        }

        internal bool GetAssertAllPossible()
        {
            return m_assertAllPossible;
        }
        
        //-----------------------------------------------------------+
        // D E N Y
        //-----------------------------------------------------------+
    
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetDeny(IPermission perm)
        {
#if FEATURE_CAS_POLICY
            BCLDebug.Assert(AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled, "Deny is only valid in legacy CAS mode");
#endif // FEATURE_CAS_POLICY

            m_denials = CreateSingletonSet(perm);
            IncrementOverridesCount();

        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetDeny(PermissionSet permSet)
        {
            m_denials = permSet.Copy();
            IncrementOverridesCount();
        }
    
        internal PermissionSet GetDenials(bool fDeclarative)
        {
            return (fDeclarative) ? m_DeclarativeDenials: m_denials;
        }

        //-----------------------------------------------------------+
        // R E S T R I C T
        //-----------------------------------------------------------+
    
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetPermitOnly(IPermission perm)
        {
            m_restriction  = CreateSingletonSet(perm);
            IncrementOverridesCount();
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetPermitOnly(PermissionSet permSet)
        {
            // permSet must not be null
            m_restriction  = permSet.Copy();
            IncrementOverridesCount();
        }
        
        internal PermissionSet GetPermitOnly(bool fDeclarative)
        {
            
            return (fDeclarative) ? m_DeclarativeRestrictions : m_restriction;
        }
#if !FEATURE_PAL        
        //-----------------------------------------------------------+
        // SafeAccessTokenHandle (Impersonation + EH purposes)
        //-----------------------------------------------------------+
        [System.Security.SecurityCritical]  // auto-generated
        internal void SetTokenHandles (SafeAccessTokenHandle callerToken, SafeAccessTokenHandle impToken)
        {
            m_callerToken = callerToken;
            m_impToken = impToken;
        }
#endif
        //-----------------------------------------------------------+
        // R E V E R T
        //-----------------------------------------------------------+
    
        [System.Security.SecurityCritical]  // auto-generated
        internal void RevertAssert()
        {
            if (m_assertions != null)
            {
                m_assertions = null;
                DecrementAssertCount();
            }


            if (m_DeclarativeAssertions != null)
            {
                m_AssertFT = m_DeclarativeAssertions.IsUnrestricted();
            }
            else
            {
                m_AssertFT = false;
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal void RevertAssertAllPossible()
        {
            if (m_assertAllPossible)
            {
            m_assertAllPossible = false;
                DecrementAssertCount();
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void RevertDeny()
        {
            if (HasImperativeDenials())
            {
                DecrementOverridesCount();
                m_denials = null;
            }
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal void RevertPermitOnly()
        {
            if (HasImperativeRestrictions())
            {
                DecrementOverridesCount();
                m_restriction= null;;
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal void RevertAll()
        {
            RevertAssert();
            RevertAssertAllPossible();
            RevertDeny();
            RevertPermitOnly();
        }


        //-----------------------------------------------------------+
        // Demand Evaluation
        //-----------------------------------------------------------+

 
        // This will get called when we hit a FSD while evaluating a demand on the call stack or compressedstack
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemand(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh)
        {
            // imperative security
            bool fContinue = CheckDemand2(demand, permToken, rmh, false); 
            if (fContinue == SecurityRuntime.StackContinue)
            {
                // declarative security
                fContinue = CheckDemand2(demand, permToken, rmh, true);
            }
            return fContinue;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckDemand2(CodeAccessPermission demand, PermissionToken permToken, RuntimeMethodHandleInternal rmh, bool fDeclarative)
        {
            PermissionSet permSet;
            
            // If the demand is null, there is no need to continue
            Contract.Assert(demand != null && !demand.CheckDemand(null), "Empty demands should have been filtered out by this point");

            // decode imperative
            if (GetPermitOnly(fDeclarative) != null)
                GetPermitOnly(fDeclarative).CheckDecoded(demand, permToken);
    
            if (GetDenials(fDeclarative) != null)
                GetDenials(fDeclarative).CheckDecoded(demand, permToken);
    
            if (GetAssertions(fDeclarative) != null)
                GetAssertions(fDeclarative).CheckDecoded(demand, permToken);
            
            // NOTE: See notes about exceptions and exception handling in FrameDescSetHelper 
    
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
    
            // Check Reduction
            
            try
            {
                permSet = GetPermitOnly(fDeclarative);
                if (permSet != null)
                {
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
            
                    // If the permit only set does not contain the demanded permission, throw a security exception
                    if (perm == null)
                    {
                        if (!permSet.IsUnrestricted())
                            throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                    }
                    else
                    {
                        bool bNeedToThrow = true;
    
                        try
                        {
                            bNeedToThrow = !demand.CheckPermitOnly(perm);
                        }
                        catch (ArgumentException)
                        {
                        }
    
                        if (bNeedToThrow)
                            throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                    }
                }
            
                // Check Denials
            
                permSet = GetDenials(fDeclarative);
                if (permSet != null)
                {
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
                    
                    // If an unrestricted set was denied and the demand implements IUnrestricted
                    if (permSet.IsUnrestricted())
                        throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
    
                    // If the deny set does contain the demanded permission, throw a security exception
                    bool bNeedToThrow = true;
                    try
                    {
                        bNeedToThrow = !demand.CheckDeny(perm);
                    }
                    catch (ArgumentException)
                    {
                    }
                    if (bNeedToThrow)
                        throw new SecurityException(String.Format(CultureInfo.InvariantCulture, Environment.GetResourceString("Security_Generic"), demand.GetType().AssemblyQualifiedName), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demand, demand);
                }
    
                if (GetAssertAllPossible())
                {
                    return SecurityRuntime.StackHalt;
                }        
    
                permSet = GetAssertions(fDeclarative);
                // Check Assertions
                if (permSet != null)
                {
            
                    CodeAccessPermission perm = (CodeAccessPermission)permSet.GetPermission(demand);
                
                    // If the assert set does contain the demanded permission, halt the stackwalk
            
                    try
                    {
                        if (permSet.IsUnrestricted() || demand.CheckAssert(perm))
                        {
                            return SecurityRuntime.StackHalt;
                        }
                    }
                    catch (ArgumentException)
                    {
                    }
                }
                
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }
            
            return SecurityRuntime.StackContinue;
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemand(PermissionSet demandSet,
                                                                   out PermissionSet alteredDemandSet,
                                                                   RuntimeMethodHandleInternal rmh)
        {
            // imperative security
            PermissionSet altPset1 = null, altPset2 = null;
            bool fContinue = CheckSetDemand2(demandSet, out altPset1, rmh, false); 
            if (altPset1 != null)
            {
                demandSet = altPset1;
            }
                
            if (fContinue == SecurityRuntime.StackContinue)
            {
                // declarative security
                fContinue = CheckSetDemand2(demandSet, out altPset2, rmh, true);
            }
            // Return the most recent altered set
            // If both declarative and imperative asserts modified the demand set: return altPset2
            // Else if imperative asserts modified the demand set: return altPset1
            // else no alteration: return null
            if (altPset2 != null)
                alteredDemandSet = altPset2;
            else if (altPset1 != null)
                alteredDemandSet = altPset1;
            else
                alteredDemandSet = null;                
            
            return fContinue;
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal bool CheckSetDemand2(PermissionSet demandSet,
                                                                   out PermissionSet alteredDemandSet,
                                                                   RuntimeMethodHandleInternal rmh, bool fDeclarative)
        {
            PermissionSet permSet;
    
            // In the common case we are not going to alter the demand set, so just to
            // be safe we'll set it to null up front.
            alteredDemandSet = null;
    
            // There's some oddness in here to deal with exceptions.  The general idea behind
            // this is that we need some way of dealing with custom permissions that may not
            // handle all possible scenarios of Union(), Intersect(), and IsSubsetOf() properly
            // (they don't support it, throw null reference exceptions, etc.).
            
            // An empty demand always succeeds.
            if (demandSet == null || demandSet.IsEmpty())
                return SecurityRuntime.StackHalt;

            if (GetPermitOnly(fDeclarative) != null)
                GetPermitOnly(fDeclarative).CheckDecoded( demandSet );
            if (GetDenials(fDeclarative) != null)
                GetDenials(fDeclarative).CheckDecoded( demandSet );
            if (GetAssertions(fDeclarative) != null)
                GetAssertions(fDeclarative).CheckDecoded( demandSet );
            
         
            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);
    
            try
            {
                // In the case of permit only, we define an exception to be failure of the check
                // and therefore we throw a security exception.
                
                permSet = GetPermitOnly(fDeclarative);
                if (permSet != null)
                {
                    IPermission permFailed = null;
                    bool bNeedToThrow = true;
    
                    try
                    {
                        bNeedToThrow = !demandSet.CheckPermitOnly(permSet, out permFailed);
                    }
                    catch (ArgumentException)
                    {
                    }
                    if (bNeedToThrow)
                        throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), null, permSet, SecurityRuntime.GetMethodInfo(rmh), demandSet, permFailed);
                }
                
                // In the case of denial, we define an exception to be failure of the check
                // and therefore we throw a security exception.
                
                permSet = GetDenials(fDeclarative);
    
    
                if (permSet != null)
                {
                    IPermission permFailed = null;
    
                    bool bNeedToThrow = true;
    
                    try
                    {
                        bNeedToThrow = !demandSet.CheckDeny(permSet, out permFailed);
                    }
                    catch (ArgumentException)
                    {
                    }
    
                    if (bNeedToThrow)
                        throw new SecurityException(Environment.GetResourceString("Security_GenericNoType"), permSet, null, SecurityRuntime.GetMethodInfo(rmh), demandSet, permFailed);
                }
            
                // The assert case is more complex.  Since asserts have the ability to "bleed through"
                // (where part of a demand is handled by an assertion, but the rest is passed on to
                // continue the stackwalk), we need to be more careful in handling the "failure" case.
                // Therefore, if an exception is thrown in performing any operation, we make sure to keep
                // that permission in the demand set thereby continuing the demand for that permission
                // walking down the stack.
                
                if (GetAssertAllPossible())
                {
                    return SecurityRuntime.StackHalt;
                }        
            
                permSet = GetAssertions(fDeclarative);
                if (permSet != null)
                {
                    // If this frame asserts a superset of the demand set we're done
                    
                    if (demandSet.CheckAssertion( permSet ))
                        return SecurityRuntime.StackHalt;
                
                    // Determine whether any of the demand set asserted.  We do this by
                    // copying the demand set and removing anything in it that is asserted.
                        
                    if (!permSet.IsUnrestricted())
                    {
                        PermissionSet.RemoveAssertedPermissionSet(demandSet, permSet, out alteredDemandSet);
                    }
                }
    
                        }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            return SecurityRuntime.StackContinue;
        }
    }

#if FEATURE_COMPRESSEDSTACK
    // Used by the stack compressor to communicate a DynamicResolver to managed code during a stackwalk.
    // The JIT will not actually place these on frames.
    internal class FrameSecurityDescriptorWithResolver : FrameSecurityDescriptor
    {
        private System.Reflection.Emit.DynamicResolver m_resolver;

        public System.Reflection.Emit.DynamicResolver Resolver
        {
            get
            {
                return m_resolver;
            }
        }
    }
#endif // FEATURE_COMPRESSEDSTACK
}
