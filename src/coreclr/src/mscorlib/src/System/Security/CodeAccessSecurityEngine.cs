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
    using System.Diagnostics;
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

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern void SpecialDemand(PermissionType whatPermission, ref StackCrawlMark stackMark);

        [System.Diagnostics.Conditional( "_DEBUG" )]
        private static void DEBUG_OUT( String str )
        {
#if _DEBUG
            if (debug)
                Console.WriteLine( str );
#endif
        }

#if _DEBUG
        private static bool debug = false;
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
            }
            throw SecurityException.MakeSecurityException(asmName, asmEvidence, granted, refused, rmh, action, demand, permThatFailed);
        }

#pragma warning disable 618
        private static void ThrowSecurityException(Object assemblyOrString, PermissionSet granted, PermissionSet refused, RuntimeMethodHandleInternal rmh, SecurityAction action, Object demand, IPermission permThatFailed)
#pragma warning restore 618
        {
            Debug.Assert((assemblyOrString == null || assemblyOrString is RuntimeAssembly || assemblyOrString is String), "Must pass in an Assembly object or String object here");
            
            if (assemblyOrString == null || assemblyOrString is RuntimeAssembly)
                ThrowSecurityException((RuntimeAssembly)assemblyOrString, granted, refused, rmh, action, demand, permThatFailed);
            else
            {
                AssemblyName asmName = new AssemblyName((String)assemblyOrString);
                throw SecurityException.MakeSecurityException(asmName, null, granted, refused, rmh, action, demand, permThatFailed);
            }
        }

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
            Debug.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");

            CheckSetHelper(grants, refused, demands, rmh, (Object)asm, action, true);
        }


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
            Debug.Assert(demands != null, "Should not reach here with a null demand set");

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
            Debug.Assert(notUsed == null, "Should not reach here with a non-null first arg which is the CompressedStack");
            CheckHelper(grantedSet, refusedSet, demand, permToken, rmh, (Object)asm, action, true);
        }
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
            Debug.Assert(demand != null, "Should not reach here with a null demand");

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

                    Debug.Assert(demand != null,"demand != null");

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

        internal static void Check(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }


        internal static void Check(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        [MethodImplAttribute(MethodImplOptions.InternalCall)]
        internal static extern FrameSecurityDescriptor CheckNReturnSO(PermissionToken permToken, 
                                                                    CodeAccessPermission demand, 
                                                                    ref StackCrawlMark stackMark,
                                                                    int create );

        internal static void Assert(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }

        internal static void Deny(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }
        
        internal static void PermitOnly(CodeAccessPermission cap, ref StackCrawlMark stackMark)
        {
        }

    }
}
