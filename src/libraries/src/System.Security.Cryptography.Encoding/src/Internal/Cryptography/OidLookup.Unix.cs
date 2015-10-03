// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
                    throw Interop.libcrypto.CreateOpenSslCryptographicException();
                default:
                    Debug.Assert(result == 0, "LookupFriendlyNameByOid returned unexpected result " + result);
                    return null;
            }
        }

        private static string NativeFriendlyNameToOid(string friendlyName, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            int nid = Interop.libcrypto.OBJ_ln2nid(friendlyName);

            if (nid == Interop.libcrypto.NID_undef)
            {
                nid = Interop.libcrypto.OBJ_sn2nid(friendlyName);
            }

            if (nid == Interop.libcrypto.NID_undef)
            {
                return null;
            }

            IntPtr sharedObject = Interop.libcrypto.OBJ_nid2obj(nid);

            if (sharedObject == IntPtr.Zero)
            {
                return null;
            }

            return Interop.libcrypto.OBJ_obj2txt_helper(sharedObject);
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}