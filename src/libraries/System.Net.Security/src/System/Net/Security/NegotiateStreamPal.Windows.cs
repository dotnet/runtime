// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Principal;

namespace System.Net.Security
{
    //
    // The class does the real work in authentication and
    // user data encryption with NEGO SSPI package.
    //
    // This is part of the NegotiateStream PAL.
    //
    internal static partial class NegotiateStreamPal
    {
        internal static IIdentity GetIdentity(NTAuthentication context)
        {
            IIdentity? result;
            string? name = context.IsServer ? null : context.Spn;
            string protocol = context.ProtocolName;

            if (context.IsServer)
            {
                SecurityContextTokenHandle? token = null;
                try
                {
                    SafeDeleteContext? securityContext = context.GetContext(out SecurityStatusPal status);
                    if (status.ErrorCode != SecurityStatusPalErrorCode.OK)
                    {
                        throw new Win32Exception((int)SecurityStatusAdapterPal.GetInteropFromSecurityStatusPal(status));
                    }

                    name = QueryContextAssociatedName(securityContext!);
                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(context, $"NTAuthentication: The context is associated with [{name}]");

                    // This will return a client token when conducted authentication on server side.
                    // This token can be used for impersonation. We use it to create a WindowsIdentity and hand it out to the server app.
                    Interop.SECURITY_STATUS winStatus = (Interop.SECURITY_STATUS)SSPIWrapper.QuerySecurityContextToken(
                        GlobalSSPI.SSPIAuth,
                        securityContext!,
                        out token);
                    if (winStatus != Interop.SECURITY_STATUS.OK)
                    {
                        throw new Win32Exception((int)winStatus);
                    }
                    string authtype = context.ProtocolName;

                    // The following call was also specifying WindowsAccountType.Normal, true.
                    // WindowsIdentity.IsAuthenticated is no longer supported in .NET Core
                    result = new WindowsIdentity(token.DangerousGetHandle(), authtype);
                    return result;
                }
                catch (SecurityException)
                {
                    // Ignore and construct generic Identity if failed due to security problem.
                }
                finally
                {
                    token?.Dispose();
                }
            }

            // On the client we don't have access to the remote side identity.
            result = new GenericIdentity(name ?? string.Empty, protocol);
            return result;
        }

        internal static void ValidateImpersonationLevel(TokenImpersonationLevel impersonationLevel)
        {
            if (impersonationLevel != TokenImpersonationLevel.Identification &&
                impersonationLevel != TokenImpersonationLevel.Impersonation &&
                impersonationLevel != TokenImpersonationLevel.Delegation)
            {
                throw new ArgumentOutOfRangeException(nameof(impersonationLevel), impersonationLevel.ToString(), SR.net_auth_supported_impl_levels);
            }
        }
    }
}
