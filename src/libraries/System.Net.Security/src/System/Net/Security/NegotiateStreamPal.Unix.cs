// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
using System.Security.Authentication.ExtendedProtection;
using System.Security.Claims;
using System.Security.Principal;
using System.Threading;
using Microsoft.Win32.SafeHandles;

namespace System.Net.Security
{
    //
    // The class maintains the state of the authentication process and the security context.
    // It encapsulates security context and does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    internal static partial class NegotiateStreamPal
    {
        internal static IIdentity GetIdentity(NTAuthentication context)
        {
            string name = context.Spn;
            string protocol = context.ProtocolName;

            if (context.IsServer)
            {
                var safeContext = context.GetContext(out var status);
                if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    throw new Win32Exception((int)status.ErrorCode);
                }
                name = GetUser(ref safeContext);
            }

            return new GenericIdentity(name, protocol);

        }
        
        internal static string QueryContextAssociatedName(SafeDeleteContext securityContext)
        {
            throw new PlatformNotSupportedException(SR.net_nego_server_not_supported);
        }
        
        internal static void ValidateImpersonationLevel(TokenImpersonationLevel impersonationLevel)
        {
            if (impersonationLevel != TokenImpersonationLevel.Identification)
            {
                throw new ArgumentOutOfRangeException(nameof(impersonationLevel), impersonationLevel.ToString(),
                    SR.net_auth_supported_impl_levels);
            }
        }
    }
}
