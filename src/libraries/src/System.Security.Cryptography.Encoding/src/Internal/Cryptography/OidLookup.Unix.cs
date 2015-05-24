// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Security.Cryptography;
using System.Text;

namespace Internal.Cryptography
{
    internal static partial class OidLookup
    {
        private static string NativeOidToFriendlyName(string oid, OidGroup oidGroup, bool fallBackToAllGroups)
        {
            // We're going to call OBJ_txt2nid, then OBJ_nid2obj. Anyone proficient in factor/label reduction
            // would see that this should be equivalent to OBJ_txt2obj, but it isn't.
            //
            // OBJ_txt2obj, when given an OID value dotted string will decode the dotted string and return
            // a blank(ish) object (which would need to be freed).  We could then pass that to OBJ_obj2nid to
            // look up the internal identifier.  Then, if we got a NID which wasn't NID_undef we would know
            // there was a match, and could call OBJ_nid2obj to get the shared pointer to the definition.
            //
            // In this case, the composition of functions that we want is OBJ_obj2nid(OBJ_txt2obj) => OBJ_txt2nid.

            int nid = Interop.libcrypto.OBJ_txt2nid(oid);

            if (nid == Interop.libcrypto.NID_undef)
            {
                return null;
            }

            return Interop.libcrypto.OBJ_nid2ln(nid);
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

        private static string FindFriendlyNameAlias(string userValue)
        {
            foreach (FriendlyNameAlias alias in s_friendlyNameAliases)
            {
                if (userValue.Equals(alias.Windows, StringComparison.OrdinalIgnoreCase))
                {
                    return alias.OpenSsl;
                }
            }

            return null;
        }

        // -----------------------------
        // ---- PAL layer ends here ----
        // -----------------------------
    }
}