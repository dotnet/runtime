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
    [DirectoryRdnPrefix("CN")]
    internal class UnknownPrincipal : Principal
    {
        //
        // Public constructors
        //
        private UnknownPrincipal(PrincipalContext context)
        {
            if (context == null)
                throw new ArgumentException(SR.NullArguments);

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
