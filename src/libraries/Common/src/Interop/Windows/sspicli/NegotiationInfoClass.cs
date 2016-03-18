// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.InteropServices;

namespace System.Net
{
    // This class is used to determine if NTLM or
    // Kerberos are used in the context of a Negotiate handshake
    internal partial class NegotiationInfoClass
    {
        internal string AuthenticationPackage;

        internal NegotiationInfoClass(SafeHandle safeHandle, int negotiationState)
        {
            if (safeHandle.IsInvalid)
            {
                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NegotiationInfoClass::.ctor() the handle is invalid:" + (safeHandle.DangerousGetHandle()).ToString("x"));
                }
                return;
            }

            IntPtr packageInfo = safeHandle.DangerousGetHandle();
            if (GlobalLog.IsEnabled)
            {
                GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8"));
            }

            if (negotiationState == Interop.SspiCli.SECPKG_NEGOTIATION_COMPLETE
                || negotiationState == Interop.SspiCli.SECPKG_NEGOTIATION_OPTIMISTIC)
            {
                IntPtr unmanagedString = Marshal.ReadIntPtr(packageInfo, SecurityPackageInfo.NameOffest);
                string name = null;
                if (unmanagedString != IntPtr.Zero)
                {
                    name = Marshal.PtrToStringUni(unmanagedString);
                }

                if (GlobalLog.IsEnabled)
                {
                    GlobalLog.Print("NegotiationInfoClass::.ctor() packageInfo:" + packageInfo.ToString("x8") + " negotiationState:" + negotiationState.ToString("x8") + " name:" + LoggingHash.ObjectToString(name));
                }

                // An optimization for future string comparisons.
                if (string.Compare(name, Kerberos, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = Kerberos;
                }
                else if (string.Compare(name, NTLM, StringComparison.OrdinalIgnoreCase) == 0)
                {
                    AuthenticationPackage = NTLM;
                }
                else
                {
                    AuthenticationPackage = name;
                }
            }
        }
    }
}
