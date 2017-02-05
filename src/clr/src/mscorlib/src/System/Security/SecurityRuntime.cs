// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

// 

namespace System.Security
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Reflection;
    using System.Collections;
    using System.Runtime.CompilerServices;
    using System.Security.Permissions;
    using System.Runtime.Versioning;
    using System.Diagnostics.Contracts;

    internal class SecurityRuntime
    {
        private SecurityRuntime(){}

        // Constants used to return status to native
        internal const bool StackContinue  = true;
        internal const bool StackHalt      = false;

        // this method is a big perf hit, so don't call unnecessarily
        internal static MethodInfo GetMethodInfo(RuntimeMethodHandleInternal rmh)
        {
            if (rmh.IsNullHandle())
                return null;

#if _DEBUG
            try
            {
#endif
                // Assert here because reflection will check grants and if we fail the check,
                // there will be an infinite recursion that overflows the stack.
                PermissionSet.s_fullTrust.Assert();
                return (System.RuntimeType.GetMethodBase(RuntimeMethodHandle.GetDeclaringType(rmh), rmh) as MethodInfo);
#if _DEBUG
            }
            catch(Exception)
            {
                return null;
            }
#endif
        }

        private static bool FrameDescSetHelper(FrameSecurityDescriptor secDesc,
                                               PermissionSet demandSet,
                                               out PermissionSet alteredDemandSet,
                                               RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckSetDemand(demandSet, out alteredDemandSet, rmh);
        }

        private static bool FrameDescHelper(FrameSecurityDescriptor secDesc,
                                               IPermission demandIn, 
                                               PermissionToken permToken,
                                               RuntimeMethodHandleInternal rmh)
        {
            return secDesc.CheckDemand((CodeAccessPermission) demandIn, permToken, rmh);
        }


        //
        // API for PermissionSets
        //

        internal static void Assert(PermissionSet permSet, ref StackCrawlMark stackMark)
        {
        }

        //
        // Revert API
        //

        internal static void RevertAssert(ref StackCrawlMark stackMark)
        {
        }
    }
}


