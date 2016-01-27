// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Security.Permissions;

namespace System.Security
{
    [System.Security.SecurityCritical]  // auto-generated_required
#pragma warning disable 618
    [PermissionSet(SecurityAction.InheritanceDemand, Unrestricted = true)]
#pragma warning restore 618
    public abstract class SecurityState
    {
        protected SecurityState(){}
        
        [System.Security.SecurityCritical]  // auto-generated
        public bool IsStateAvailable()
        {
            AppDomainManager domainManager = AppDomainManager.CurrentAppDomainManager;
#if FEATURE_CORECLR
            // CheckSecuritySettings only when appdomainManager is present. So if there is no 
            // appDomain Manager return true as by default coreclr runs in fulltrust. 
            return domainManager != null ? domainManager.CheckSecuritySettings(this) : true;
#else
            return domainManager != null ? domainManager.CheckSecuritySettings(this) : false;
#endif
        }
        // override this function and throw the appropriate 
        public abstract void EnsureState();
    }

}
