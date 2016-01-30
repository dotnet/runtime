// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static bool ShouldUseCache(OidGroup oidGroup)
        {
            return true;
        }

        private static string NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            IntPtr friendlyNamePtr = IntPtr.Zero;
            int result = Interop.Crypto.LookupFriendlyNameByOid(oid, ref friendlyNamePtr);

            switch (result)
            {
                case 1: /* Success */
                    Debug.Assert(friendlyNamePtr != IntPtr.Zero, "friendlyNamePtr != IntPtr.Zero");

                    // The pointer is to a shared string, so marshalling it out is all that's required.
                    return Marshal.PtrToStringAnsi(friendlyNamePtr);
                case -1: /* OpenSSL internal error */
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                default:
                    Debug.Assert(result == 0, "LookupFriendlyNameByOid returned unexpected result " + result);
                    return null;
            }
        }

        private static string NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            IntPtr sharedObject = Interop.Crypto.GetObjectDefinitionByName(friendlyName);

            if (sharedObject == IntPtr.Zero)
            {
                return null;
            }

            return Interop.Crypto.GetOidValue(sharedObject);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}
