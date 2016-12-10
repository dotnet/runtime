// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.Security;
using System.Security.Permissions;

namespace System.Security
{
    public abstract class SecurityState
    {
        protected SecurityState(){}
        
        public bool IsStateAvailable()
        {
            AppDomainManager domainManager = AppDomainManager.CurrentAppDomainManager;

            // CheckSecuritySettings only when appdomainManager is present. So if there is no 
            // appDomain Manager return true as by default coreclr runs in fulltrust. 
            return domainManager != null ? domainManager.CheckSecuritySettings(this) : true;
        }
        // override this function and throw the appropriate 
        public abstract void EnsureState();
    }

}
