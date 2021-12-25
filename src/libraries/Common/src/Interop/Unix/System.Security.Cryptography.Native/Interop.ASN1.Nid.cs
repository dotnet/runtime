// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        private static readonly ConcurrentDictionary<string, int> s_nidLookup =
            new ConcurrentDictionary<string, int>();

        internal const int NID_undef = 0;

        [GeneratedDllImport(Libraries.CryptoNative, EntryPoint = "CryptoNative_ObjTxt2Nid", CharSet = CharSet.Ansi)]
        private static partial int ObjTxt2Nid(string oid);

        internal static int ResolveRequiredNid(string oid)
        {
            return s_nidLookup.GetOrAdd(oid, s => LookupNid(s));
        }

        private static int LookupNid(string oid)
        {
            int nid = ObjTxt2Nid(oid);

            if (nid == NID_undef)
            {
                Debug.Fail($"NID Lookup for {oid} failed, only well-known types should be queried.");
                throw new CryptographicException();
            }

            return nid;
        }
    }
}
