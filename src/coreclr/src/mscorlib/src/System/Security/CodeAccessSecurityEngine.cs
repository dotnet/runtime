// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security {
    using System;
    using System.Threading;
    using System.Security.Util;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Reflection;
    using System.Globalization;
    using System.Security.Policy;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    // Used in DemandInternal, to remember the result of previous demands
    // KEEP IN SYNC WITH DEFINITIONS IN SECURITYPOLICY.H
    [Serializable]
    internal enum PermissionType
    {
        // special flags
        SecurityUnmngdCodeAccess    = 0,
        SecuritySkipVerification    = 1,
        ReflectionTypeInfo          = 2,
        SecurityAssert              = 3,
        ReflectionMemberAccess      = 4,
        SecuritySerialization       = 5,
        ReflectionRestrictedMemberAccess    = 6,
        FullTrust                   = 7,
        SecurityBindingRedirects    = 8,

        // special permissions
        UIPermission                = 9,
        EnvironmentPermission       = 10,
        FileDialogPermission        = 11,
        FileIOPermission            = 12,
        ReflectionPermission        = 13,
        SecurityPermission          = 14,

        // additional special flags
        SecurityControlEvidence     = 16,
        SecurityControlPrincipal    = 17
    }

    internal static class CodeAccessSecurityEngine
    {

        internal static SecurityPermission AssertPermission; 
        internal static PermissionToken AssertPermissionToken; 

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SpecialDemand(PermissionType whatPermission, ref StackCrawlMark stackMark);

        [System.Security.SecurityCritical]  // auto-generated
        [System.Diagnostics.Conditional( "_DEBUG" )]
        private static void DEBUG_OUT( String str )
        {
#if _DEBUG        
            if (debug)
            {
#if !FEATURE_CORECLR
                if (to_file)
                {
                    System.Text.StringBuilder sb = new System.Text.StringBuilder();
                    sb.Append( str );
                    sb.Append ((char)13) ;
                    sb.Append ((char)10) ;
                    PolicyManager.DebugOut( file, sb.ToString() );
                }
                else
#endif                    
                    Console.WriteLine( str );
             }
#endif             
        }
        
#if _DEBUG 
        private static bool debug = false;
#if !FEATURE_CORECLR
        private static readonly bool to_file = false;
#endif
        private const String file = "d:\\foo\\debug.txt";
#endif  

        // static default constructor. This will be called before any of the static members are accessed.
        static CodeAccessSecurityEngine()
        {
#pragma warning disable 618
            AssertPermission = new SecurityPermission(SecurityPermissionFlag.Assertion);
#pragma warning restore 618
            AssertPermissionToken = PermissionToken.GetToken(AssertPermission);
        }

        [System.Security.SecurityCritical]  // auto-generated
#pragma warning disable 618
        private static void ThrowSecurityException(RuntimeAssembly asm, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
#pragma warning restore 618
        {
            AssemblyName asmName = null;
            Evidence asmEvidence = null;
            if (asm != null)
            {
                // Assert here because reflection will check grants and if we fail the check,
                // there will be an infinite recursion that overflows the stack.
                PermissionSet.s_fullTrust.Assert();
                asmName = asm.GetName();
#if FEATURE_CAS_POLICY
                if(asm != Assembly.GetExecutingAssembly()) // this condition is to avoid having to marshal mscorlib's evidence (which is always in teh default domain) to the current domain
                    asmEvidence = asm.Evidence;
#endif // FEATURE_CAS_POLICY
            }
            throw SecurityException.MakeSecurityException(asmName, asmEvidence, granted, refused, rmh, action, demand, permThatFailed);
        }

        [System.Security.SecurityCritical]  // auto-generated
#pragma warning disable 618
        private static void ThrowSecurityException(Object assemblyOrString, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
#pragma warning restore 618
        {
            Contract.Assert((assemblyOrString == null || assemblyOrString is RuntimeAssembly || assemblyOrString is String), "Must pass in an Assembly object or String object here");
            
            if (assemblyOrString == null || assemblyOrString is RuntimeAssembly)
                ThrowSecurityException((RuntimeAssembly)assemblyOrString, granted, refused, rmh, action, demand, permThatFailed);
            else
            {
                AssemblyName asmName = new AssemblyName((String)assemblyOrString);
                throw SecurityException.MakeSecurityException(asmName, null, granted, refused, rmh, action, demand, permThatFailed);
            }
        }

#if FEATURE_COMPRESSEDSTACK
        [System.Security.SecurityCritical]  // auto-generated
        internal static void CheckSetHelper(CompressedStack cs,
                                           PermissionSet grants,
                                           PermissionSet refused,
                                           PermissionSet demands,
                                           RuntimeMethodHandleInternal rmh,
                                           RuntimeAssembly asm,
                                           SecurityAction action)
        {
            if (cs != null)
                cs.CheckSetDemand(demands, rmh);
            else
                CheckSetHelper(grants, refused, demands, rmh, (Object)asm, action, true);
        }
#else // FEATURE_COMPRESSEDSTACK
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
#pragma warning disable 618
        internal static void CheckSetHelper(Object notUsed,
                                           PermissionSet grants,
                                           PermissionSet refused,
                                           PermissionSet demands,
                                           RuntimeMethodHandleInternal rmh,
                                           RuntimeAssembly asm,
                                           SecurityAction action)
#pragma warning restore 618
        {
            // To reduce the amount of ifdef-code-churn, a dummy arg is used for the first parameter - instead of a CompressedStack object,
            // we use a System.Object that should always be null. If we tried to change the signature of the function, there will need to be
            // corresponding changes in VM (metasig.h, mscorlib.h, securitystackwalk.cpp, number of elements in the arg array, etc.)
            Contract.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");

            CheckSetHelper(grants, refused, demands, rmh, (Object)asm, action, true);
        }

#endif // FEATURE_COMPRESSEDSTACK

        [System.Security.SecurityCritical]  // auto-generated
#pragma warning disable 618
        internal static bool CheckSetHelper(PermissionSet grants,
                                           PermissionSet refused,
                                           PermissionSet demands,
                                           RuntimeMethodHandleInternal rmh,
                                           Object assemblyOrString,
                                           SecurityAction action,
                                           bool throwException)
#pragma warning restore 618
        {

            Contract.Assert(demands != null, "Should not reach here with a null demand set");
#if _DEBUG && FEATURE_CAS_POLICY
            if (debug)
            {
                DEBUG_OUT("Granted: ");
                DEBUG_OUT(grants.ToXml().ToString());
                DEBUG_OUT("Refused: ");
                DEBUG_OUT(refused != null ? refused.ToXml().ToString() : "<null>");
                DEBUG_OUT("Demanded: ");
                DEBUG_OUT(demands!=null ? demands.ToXml().ToString() : "<null>");
            }
#endif // _DEBUG && FEATURE_CAS_POLICY

            IPermission permThatFailed = null;
            if (grants != null)
                grants.CheckDecoded(demands);
            if (refused != null)
                refused.CheckDecoded(demands);

            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);

            try
            {

                // Check grant set
                if (!demands.CheckDemand(grants, out permThatFailed))
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                    else
                        return false;
                }

                // Check refused set
                if (!demands.CheckDeny(refused, out permThatFailed))
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                    else
                        return false;
                }
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception)
            {
                // Any exception besides a security exception in this code means that
                // a permission was unable to properly handle what we asked of it.
                // We will define this to mean that the demand failed.
                if (throwException)
                    ThrowSecurityException(assemblyOrString, grants, refused, rmh, action, demands, permThatFailed);
                else
                    return false;
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }
            return true;
        }
#if FEATURE_COMPRESSEDSTACK
        [System.Security.SecurityCritical]  // auto-generated
        internal static void CheckHelper(CompressedStack cs,
                                        PermissionSet grantedSet,
                                        PermissionSet refusedSet,
                                        CodeAccessPermission demand, 
                                        PermissionToken permToken,
                                        RuntimeMethodHandleInternal rmh,
                                        RuntimeAssembly asm,
                                        SecurityAction action)
        {
            if (cs != null)
                cs.CheckDemand(demand, permToken, rmh);
            else
                CheckHelper(grantedSet, refusedSet, demand, permToken, rmh, (Object)asm, action, true);
        }
#else // FEATURE_COMPRESSEDSTACK
        #if FEATURE_CORECLR
        [System.Security.SecurityCritical] // auto-generated
        #endif
#pragma warning disable 618
        internal static void CheckHelper(Object notUsed,
                                        PermissionSet grantedSet,
                                        PermissionSet refusedSet,
                                        CodeAccessPermission demand, 
                                        PermissionToken permToken,
                                        RuntimeMethodHandleInternal rmh,
                                        RuntimeAssembly asm,
                                        SecurityAction action)
#pragma warning restore 618
        {
            // To reduce the amount of ifdef-code-churn, a dummy arg is used for the first parameter - instead of a CompressedStack object,
            // we use a System.Object that should always be null. If we tried to change the signature of the function, there will need to be
            // corresponding changes in VM (metasig.h, mscorlib.h, securitystackwalk.cpp, number of elements in the arg array, etc.)
            Contract.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");
            CheckHelper(grantedSet, refusedSet, demand, permToken, rmh, (Object)asm, action, true);
        }
#endif // FEATURE_COMPRESSEDSTACK
        [System.Security.SecurityCritical]  // auto-generated
#pragma warning disable 618
        internal static bool CheckHelper(PermissionSet grantedSet,
                                        PermissionSet refusedSet,
                                        CodeAccessPermission demand, 
                                        PermissionToken permToken,
                                        RuntimeMethodHandleInternal rmh,
                                        Object assemblyOrString,
                                        SecurityAction action,
                                        bool throwException)
#pragma warning restore 618
        {
            // We should never get here with a null demand
            Contract.Assert(demand != null, "Should not reach here with a null demand");
            
#if _DEBUG && FEATURE_CAS_POLICY
            if (debug)
            {
                DEBUG_OUT("Granted: ");
                DEBUG_OUT(grantedSet.ToXml().ToString());
                DEBUG_OUT("Refused: ");
                DEBUG_OUT(refusedSet != null ? refusedSet.ToXml().ToString() : "<null>");
                DEBUG_OUT("Demanded: ");
                DEBUG_OUT(demand.ToString());
            }
#endif // _DEBUG && FEATURE_CAS_POLICY

            if (permToken == null)
                permToken = PermissionToken.GetToken(demand);

            if (grantedSet != null)
                grantedSet.CheckDecoded(permToken.m_index);
            if (refusedSet != null)
                refusedSet.CheckDecoded(permToken.m_index);

            // If PermissionSet is null, then module does not have Permissions... Fail check.

            bool bThreadSecurity = SecurityManager._SetThreadSecurity(false);

            try
            {
                if (grantedSet == null)
                {
                    if (throwException)
                        ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                    else
                        return false;
                }
                
                else if (!grantedSet.IsUnrestricted())
                {
                    // If we aren't unrestricted, there is a refused set, or our permission is not of the unrestricted
                    // variety, we need to do the proper callback.

                    Contract.Assert(demand != null,"demand != null");

                    // Find the permission of matching type in the permission set.

                    CodeAccessPermission grantedPerm = 
                                (CodeAccessPermission)grantedSet.GetPermission(permToken);

                    // Make sure the demand has been granted
                    if (!demand.CheckDemand( grantedPerm ))
                    {
                        if (throwException)
                            ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                        else
                            return false;
                    }
                }

                // Make the sure the permission is not refused.

                if (refusedSet != null)
                {
                    CodeAccessPermission refusedPerm = 
                        (CodeAccessPermission)refusedSet.GetPermission(permToken);
                    if (refusedPerm != null)
                    {
                        if (!refusedPerm.CheckDeny(demand))
                        {
        #if _DEBUG
                            if (debug)
                                DEBUG_OUT( "Permission found in refused set" );
        #endif
                                if (throwException)
                                    ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                                else
                                    return false;

                        }
                    }

                    if (refusedSet.IsUnrestricted())
                    {
                        if (throwException)
                            ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                        else
                            return false;
                    }
                }
            }
            catch (SecurityException)
            {
                throw;
            }
            catch (Exception)
            {
                // Any exception besides a security exception in this code means that
                // a permission was unable to properly handle what we asked of it.
                // We will define this to mean that the demand failed.
                if (throwException)
                    ThrowSecurityException(assemblyOrString, grantedSet, refusedSet, rmh, action, demand, demand);
                else
                    return false;
            }
            finally
            {
                if (bThreadSecurity)
                    SecurityManager._SetThreadSecurity(true);
            }

            DEBUG_OUT( "Check passed" );
            return true;
        }

#if FEATURE_CAS_POLICY
        /// <summary>
        ///     Demand for the grant set of an assembly
        /// </summary>
        /// <remarks>
        ///     Managed half of SecurityStackWalk::DemandGrantSet.
        /// </remarks>
        [System.Security.SecurityCritical]  // auto-generated
        private static void CheckGrantSetHelper(PermissionSet grantSet)
        {
            Contract.Assert(grantSet != null, "Missing grant set");
            grantSet.CopyWithNoIdentityPermissions().Demand();
        }

        /// <summary>
        ///     Perform a security demand which succeeds if either a compatibilty permission is granted to the
        ///     call stack, or restricted member access and the grant set of the target of the reflection
        ///     operation is granted.
        /// </summary>
        /// <param name="permission">compatibility permission to check</param>
        /// <param name="targetGrant">grant set of the reflection target</param>
        [System.Security.SecurityCritical]  // auto-generated
        internal static void ReflectionTargetDemandHelper(PermissionType permission, PermissionSet targetGrant)
        {
            ReflectionTargetDemandHelper((int)permission, targetGrant);
        }

        /// <summary>
        ///     Perform a security demand which succeeds if either a compatibilty permission is granted to the
        ///     call stack, or restricted member access and the grant set of the target of the reflection
        ///     operation is granted.
        /// </summary>
        /// <remarks>
        ///     Managed half of SecurityStackWalk::ReflectionTargetDemand.
        /// </remarks>
        /// <param name="permission">compatibility permission to check (See PermissionType)</param>
        /// <param name="targetGrant">grant set of the reflection target</param>
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.NoInlining)] // Methods containing StackCrawlMark local var has to be marked non-inlineable
        private static void ReflectionTargetDemandHelper(int permission, PermissionSet targetGrant)
        {
            // Capture a compressed stack so that we can make both permission checks without walking the stack
            // multiple times.
            StackCrawlMark stackMark = StackCrawlMark.LookForMyCaller;
            CompressedStack cs = CompressedStack.GetCompressedStack(ref stackMark);

            ReflectionTargetDemandHelper(permission, targetGrant, cs);
        }

        /// <summary>
        ///     Perform a reflection target demand against a given access context
        /// </summary>
        /// <remarks>
        ///     Managed half of SecurityStackWalk::ReflectionTargetDemand
        /// </remarks>
        /// <param name="permission">compatibility permission to check (See PermissionType)</param>
        /// <param name="targetGrant">grant set of the reflection target</param>
        /// <param name="accessContext">access context to do the demand against</param>
        [System.Security.SecurityCritical]  // auto-generated
        private static void ReflectionTargetDemandHelper(int permission,
                                                         PermissionSet targetGrant,
                                                         Resolver accessContext)
        {
            ReflectionTargetDemandHelper(permission, targetGrant, accessContext.GetSecurityContext());
        }

        /// <summary>
        ///     Perform a reflection target demand against a given compressed stack
        /// </summary>
        /// <remarks>
        ///     Managed half of SecurityStackWalk::ReflectionTargetDemand
        /// </remarks>
        /// <param name="permission">compatibility permission to check (See PermissionType)</param>
        /// <param name="targetGrant">grant set of the reflection target</param>
        /// <param name="securityContext">compressed stack to do the demand against</param>
        [System.Security.SecurityCritical]  // auto-generated
        private static void ReflectionTargetDemandHelper(int permission,
                                                         PermissionSet targetGrant,
                                                         CompressedStack securityContext)
        {
            Contract.Assert(securityContext != null, "securityContext != null");

            // We need to remove all identity permissions from the grant set of the target, otherwise the
            // disjunctive demand will fail unless we're reflecting on the same assembly.
            PermissionSet demandSet = null;
            if (targetGrant == null)
            {
                demandSet = new PermissionSet(PermissionState.Unrestricted);
            }
            else
            {
                demandSet = targetGrant.CopyWithNoIdentityPermissions();
                demandSet.AddPermission(new ReflectionPermission(ReflectionPermissionFlag.RestrictedMemberAccess));
            }

            securityContext.DemandFlagsOrGrantSet((1 << (int)permission), demandSet);
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static void GetZoneAndOriginHelper( CompressedStack cs, PermissionSet grantSet, PermissionSet refusedSet, ArrayList zoneList, ArrayList originList )
        {
            if (cs != null)
                cs.GetZoneAndOrigin(zoneList, originList, PermissionToken.GetToken(typeof(ZoneIdentityPermission)), PermissionToken.GetToken(typeof(UrlIdentityPermission)));
            else
        {
            ZoneIdentityPermission zone = (ZoneIdentityPermission)grantSet.GetPermission( typeof( ZoneIdentityPermission ) );
            UrlIdentityPermission url = (UrlIdentityPermission)grantSet.GetPermission( typeof( UrlIdentityPermission ) );

            if (zone != null)
                zoneList.Add( zone.SecurityZone );

            if (url != null)
                originList.Add( url.Url );
            }
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static void GetZoneAndOrigin( ref StackCrawlMark mark, out ArrayList zone, out ArrayList origin )
        {
            zone = new ArrayList();
            origin = new ArrayList();

            GetZoneAndOriginInternal( zone, origin, ref mark);
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void GetZoneAndOriginInternal(ArrayList zoneList, 
                                  ArrayList originList, 
                                  ref StackCrawlMark stackMark);

        [System.Security.SecurityCritical]  // auto-generated
        internal static void CheckAssembly(RuntimeAssembly asm, CodeAccessPermission demand )
        {
            Contract.Assert( asm != null, "Must pass in a good assembly" );
            Contract.Assert( demand != null, "Must pass in a good demand" );

            PermissionSet granted, refused;
            asm.GetGrantSet( out granted, out refused );
#pragma warning disable 618
                CheckHelper( granted, refused, demand, PermissionToken.GetToken(demand), RuntimeMethodHandleInternal.EmptyHandle, asm, SecurityAction.Demand, true );
#pragma warning restore 618
        }

        // Check - Used to initiate a code-access security check.
        // This method invokes a stack walk after skipping to the frame
        // referenced by stackMark.
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        private static extern void Check (Object demand,
                                  ref StackCrawlMark stackMark, 
                                  bool isPermSet);

  
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool QuickCheckForAllDemands();
        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern bool AllDomainsHomogeneousWithNoStackModifiers();
#endif // FEATURE_CAS_POLICY

        [System.Security.SecurityCritical]  // auto-generated
        internal static void Check(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            Check(cap,
                  ref stackMark,
                  false);
#endif // FEATURE_CAS_POLICY
        }


        [System.Security.SecurityCritical]  // auto-generated
        internal static void Check(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            Check(permSet,
                 ref stackMark,
                 true);
#endif // FEATURE_CAS_POLICY
        }

        [System.Security.SecurityCritical]  // auto-generated
        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern FrameSecurityDescriptor CheckNReturnSO(PermissionToken permToken, 
                                                                    CodeAccessPermission demand, 
                                                                    ref StackCrawlMark stackMark,
                                                                    int create );

        [System.Security.SecurityCritical]  // auto-generated
        internal static void Assert(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            // Make sure the caller of assert has the permission to assert
            //WARNING: The placement of the call here is just right to check
            //         the appropriate frame.
            
            // Note: if the "AssertPermission" is not a permission that implements IUnrestrictedPermission
            // you need to change the last parameter to a zero.
            Contract.Assert(AssertPermissionToken != null && AssertPermission != null, "Assert Permission not setup correctly");
            FrameSecurityDescriptor secObj = CheckNReturnSO(AssertPermissionToken,
                                                            AssertPermission,
                                                            ref stackMark,
                                                            1 );
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                // This can happen when you create delegates over functions that need the REQ_SQ 
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeAsserts())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetAssert(cap);
            }
#endif // FEATURE_CAS_POLICY
        }

        [System.Security.SecurityCritical]  // auto-generated
        internal static void Deny(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            // Deny is only valid in legacy mode
            if (!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                throw new NotSupportedException(Environment.GetResourceString("NotSupported_CasDeny"));
            }

            FrameSecurityDescriptor secObj =
                SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                // This can happen when you create delegates over functions that need the REQ_SQ 
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeDenials())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetDeny(cap);
            }
#endif // FEATURE_CAS_POLICY
        }
        
        [System.Security.SecurityCritical]  // auto-generated
        internal static void PermitOnly(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
#if FEATURE_CAS_POLICY
            FrameSecurityDescriptor secObj =
                SecurityRuntime.GetSecurityObjectForFrame(ref stackMark, true);
            if (secObj == null)
            {
                // Security: REQ_SQ flag is missing. Bad compiler ?
                // This can happen when you create delegates over functions that need the REQ_SQ 
                System.Environment.FailFast(Environment.GetResourceString("ExecutionEngine_MissingSecurityDescriptor"));
            }
            else
            {
                if (secObj.HasImperativeRestrictions())
                    throw new SecurityException( Environment.GetResourceString( "Security_MustRevertOverride" ) );

                secObj.SetPermitOnly(cap);
            }
#endif // FEATURE_CAS_POLICY
        }
        
#if FEATURE_CAS_POLICY
        // Called from the VM to do a pre-domain initialization check of the security state of the
        // AppDomain.  This method looks at the state of the security of an AppDomain before it is
        // completely initialized - so the output of this method does not always match what will be true
        // when the domain is completely initialized.  Instead, it is used to read what the input parameters
        // to the domain setup say about the domain.
        private static void PreResolve(out bool isFullyTrusted, out bool isHomogeneous)
        {
            //
            // There are three main cases:
            //   1. The AppDomain has an explict ApplicationTrust - we can use this to read the input state
            //      of the AppDomain.
            //   2. The AppDomain is using legacy CAS policy - this means we can't tell much about the
            //      domain itself without a full policy resolution.
            //   3. The domain is a standard v4+ AppDomain - these are always full trust and homogenous by
            //      default.
            //

            // If the AppDomain is setup with an ApplicationTrust then it is always homogenous and we can
            // tell its grant set right from the ApplicaitonTrust
            ApplicationTrust domainTrust = AppDomain.CurrentDomain.SetupInformation.ApplicationTrust;
            if (domainTrust != null)
            {
                isFullyTrusted = domainTrust.DefaultGrantSet.PermissionSet.IsUnrestricted();
                isHomogeneous = true;
                return;
            }

            // Otherwise, see if the domain is being configured on input to use legacy CAS policy
            if (CompatibilitySwitches.IsNetFx40LegacySecurityPolicy || AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled)
            {
                isFullyTrusted = false;
                isHomogeneous = false;
                return;
            }

            // If none of the above is true, then we must be a standard AppDomain
            isFullyTrusted = true;
            isHomogeneous = true;
        }

        // Called from the VM when either a HostSecurityManager or simple sandbox domain can determine the
        // grant set of an assembly
        private static PermissionSet ResolveGrantSet(Evidence evidence, out int specialFlags, bool checkExecutionPermission)
        {
            Contract.Assert(evidence != null);
            Contract.Assert(!AppDomain.CurrentDomain.IsLegacyCasPolicyEnabled); // This API does not do CAS policy resolution

            PermissionSet grantSet = null;
            if (!TryResolveGrantSet(evidence, out grantSet))
            {
                // If we couldn't figure out a grant set from the domain or the host, then we treat the
                // assembly as fully trusted.
                grantSet = new PermissionSet(PermissionState.Unrestricted);
            }

            // Make sure the grant set includes the ability to execute code if that has been requested.
            if (checkExecutionPermission)
            {
                SecurityPermission executionPermission = new SecurityPermission(SecurityPermissionFlag.Execution);
                if (!grantSet.Contains(executionPermission))
                {
                    throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"),
                                              System.__HResults.CORSEC_E_NO_EXEC_PERM);
                }
            }

            specialFlags = SecurityManager.GetSpecialFlags(grantSet, null);
            return grantSet;
        }

        // Consult the host and the current AppDomain if it is homogenous to determine what the grant set
        // of an assembly is.  This API returns true if it was able to determine a grant set for the evidence,
        // false if it cannot and other policy needs to be applied.
        [SecuritySafeCritical]
        internal static bool TryResolveGrantSet(Evidence evidence, out PermissionSet grantSet)
        {
            Contract.Assert(evidence != null);

            HostSecurityManager securityManager = AppDomain.CurrentDomain.HostSecurityManager;

            // GAC assemblies always are fully trusted
            if (evidence.GetHostEvidence<GacInstalled>() != null)
            {
                grantSet = new PermissionSet(PermissionState.Unrestricted);
                return true;
            }
            // If the host wants to participate in policy resolution, then our next option is to ask it for
            // a grant set
            else if ((securityManager.Flags & HostSecurityManagerOptions.HostResolvePolicy) == HostSecurityManagerOptions.HostResolvePolicy)
            {
                PermissionSet hostGrantSet = securityManager.ResolvePolicy(evidence);

                if (hostGrantSet == null)
                {
                    throw new PolicyException(Environment.GetResourceString("Policy_NullHostGrantSet", securityManager.GetType().FullName));
                }

                // If we're in a homogenous domain, we don't want to allow the host to create multiple
                // levels of permissions within the domain.  So, if we see the host return something other
                // than full trust or the homogenous grant set, we reject the grant set.
                if (AppDomain.CurrentDomain.IsHomogenous)
                {
                    // Some hosts, such as ASP.NET, return Nothing as a way of saying that the assembly should
                    // not be allowed to run in the AppDomain.  Reject that with a specific
                    // no-execution-allowed-here exception message, rather than the return value validation
                    // exception message we'd hit below.
                    if (hostGrantSet.IsEmpty())
                    {
                        throw new PolicyException(Environment.GetResourceString("Policy_NoExecutionPermission"));
                    }

                    PermissionSet homogenousGrantSet = AppDomain.CurrentDomain.ApplicationTrust.DefaultGrantSet.PermissionSet;
                    bool isValidGrantSet = hostGrantSet.IsUnrestricted() ||
                                           (hostGrantSet.IsSubsetOf(homogenousGrantSet) && homogenousGrantSet.IsSubsetOf(hostGrantSet));

                    if (!isValidGrantSet)
                    {
                        throw new PolicyException(Environment.GetResourceString("Policy_GrantSetDoesNotMatchDomain", securityManager.GetType().FullName));
                    }
                }

                grantSet = hostGrantSet;
                return true;
            }
            // If we're in a homogenous domain, we can get the grant set directly from the application trust
            else if (AppDomain.CurrentDomain.IsHomogenous)
            {
                grantSet = AppDomain.CurrentDomain.GetHomogenousGrantSet(evidence);
                return true;
            }
            // Otherwise we have no way to figure out what the grant set is
            else
            {
                grantSet = null;
                return false;
            }
        }
#endif // FEATURE_CAS_POLICY

#if FEATURE_PLS
        // Update the PLS used for optimization in the AppDomain: called from the VM
        [System.Security.SecurityCritical]  // auto-generated
        private static PermissionListSet UpdateAppDomainPLS(PermissionListSet adPLS, PermissionSet grantedPerms, PermissionSet refusedPerms) {
            if (adPLS == null) {
                adPLS = new PermissionListSet();
                adPLS.UpdateDomainPLS(grantedPerms, refusedPerms);
                return adPLS;
            } else {
                PermissionListSet newPLS = new PermissionListSet();
                newPLS.UpdateDomainPLS(adPLS);
                newPLS.UpdateDomainPLS(grantedPerms, refusedPerms);
                return newPLS;
            }
        }
#endif //FEATURE_PLS
    }
}
