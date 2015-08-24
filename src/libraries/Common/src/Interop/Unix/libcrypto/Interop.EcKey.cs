// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;

using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        [DllImport(Libraries.LibCrypto)]
        internal static extern void EC_KEY_free(IntPtr r);

        [DllImport(Libraries.LibCrypto)]
        internal static extern SafeEcKeyHandle EC_KEY_new_by_curve_name(int nid);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EC_KEY_generate_key(SafeEcKeyHandle eckey);

        [DllImport(Libraries.LibCrypto)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool EC_KEY_up_ref(IntPtr r);

        internal static int EcKeyGetCurveName(SafeEcKeyHandle ecKey)
        {
            IntPtr ecGroup = EC_KEY_get0_group(ecKey.DangerousGetHandle());
            int nid = EC_GROUP_get_curve_name(ecGroup);
            GC.KeepAlive(ecKey);
            return nid;
        }

        // This P/Invoke is a classic GC trap, so we're only exposing it via a safe helper (EcKeyGetCurveName)
        [DllImport(Libraries.LibCrypto)]
        private static extern IntPtr EC_KEY_get0_group(IntPtr key);

        // This P/Invoke is a classic GC trap, so we're only exposing it via a safe helper (EcKeyGetCurveName)
        [DllImport(Libraries.LibCrypto)]
        private static extern int EC_GROUP_get_curve_name(IntPtr ecGroup);
    }
}
