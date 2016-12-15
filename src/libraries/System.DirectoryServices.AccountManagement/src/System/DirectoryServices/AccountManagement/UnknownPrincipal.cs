// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Security.Permissions;
using System.Security.Principal;

namespace System.DirectoryServices.AccountManagement
{
    // <SecurityKernel Critical="True" Ring="0">
    // <SatisfiesLinkDemand Name="Principal" />
    // </SecurityKernel>
#pragma warning disable 618    // Have not migrated to v4 transparency yet
    [System.Security.SecurityCritical(System.Security.SecurityCriticalScope.Everything)]
#pragma warning restore 618
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
    [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
    [DirectoryRdnPrefix("CN")]
    internal class UnknownPrincipal : Principal
    {
        //
        // Public constructors
        //
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.LinkDemand, Unrestricted = true)]
        [DirectoryServicesPermission(System.Security.Permissions.SecurityAction.InheritanceDemand, Unrestricted = true)]
        private UnknownPrincipal(PrincipalContext context)
        {
            if (context == null)
                throw new ArgumentException(StringResources.NullArguments);

            this.ContextRaw = context;
            this.unpersisted = true;
        }

        //
        // Internal "constructor": Used for constructing UnknownPrincipal
        //
        static internal UnknownPrincipal CreateUnknownPrincipal(PrincipalContext ctx, byte[] sid, string name)
        {
            UnknownPrincipal up = new UnknownPrincipal(ctx);
            up.unpersisted = false;
            up.fakePrincipal = true;

            // Set the display name on the object
            up.LoadValueIntoProperty(PropertyNames.PrincipalDisplayName, name);

            // Set the display name on the object
            up.LoadValueIntoProperty(PropertyNames.PrincipalName, name);

            // SID IdentityClaim
            SecurityIdentifier sidObj = new SecurityIdentifier(Utils.ConvertSidToSDDL(sid));

            // Set the display name on the object
            up.LoadValueIntoProperty(PropertyNames.PrincipalSid, sidObj);
            return up;
        }
    }
}
