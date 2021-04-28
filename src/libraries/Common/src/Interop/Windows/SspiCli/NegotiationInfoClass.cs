// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

namespace System.Net
{
    // This class is used to determine if NTLM or
    // Kerberos are used in the context of a Negotiate handshake
    internal static partial class NegotiationInfoClass
    {
        internal static string? GetAuthenticationPackageName(SafeHandle safeHandle, int negotiationState)
        {
            if (safeHandle.IsInvalid)
            {
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"Invalid handle:{safeHandle}");
                return null;
            }

            bool gotRef = false;
            try
            {
                safeHandle.DangerousAddRef(ref gotRef);
                IntPtr packageInfo = safeHandle.DangerousGetHandle();
                if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"packageInfo:{packageInfo} negotiationState:{negotiationState:x}");

                if (negotiationState == Interop.SspiCli.SECPKG_NEGOTIATION_COMPLETE ||
                    negotiationState == Interop.SspiCli.SECPKG_NEGOTIATION_OPTIMISTIC)
                {
                    string? name;
                    unsafe
                    {
                        name = Marshal.PtrToStringUni(((SecurityPackageInfo*)packageInfo)->Name);
                    }

                    if (NetEventSource.Log.IsEnabled()) NetEventSource.Info(null, $"packageInfo:{packageInfo} negotiationState:{negotiationState:x} name:{name}");

                    // An optimization for future string comparisons.
                    return
                        string.Equals(name, Kerberos, StringComparison.OrdinalIgnoreCase) ? Kerberos :
                        string.Equals(name, NTLM, StringComparison.OrdinalIgnoreCase) ? NTLM :
                        name;
                }
            }
            finally
            {
                if (gotRef)
                {
                    safeHandle.DangerousRelease();
                }
            }

            return null;
        }
    }
}
