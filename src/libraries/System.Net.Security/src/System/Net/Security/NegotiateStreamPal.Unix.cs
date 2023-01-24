// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Security.Principal;

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
            string name = context.Spn!;
            string protocol = context.ProtocolName;

            if (context.IsServer)
            {
                SafeDeleteContext safeContext = context.GetContext(out var status)!;
                if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                {
                    throw new Win32Exception((int)status.ErrorCode);
                }
                name = GetUser(ref safeContext);
            }

            return new GenericIdentity(name, protocol);
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
