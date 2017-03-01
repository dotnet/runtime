// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_StoreEnumerateUserRoot(
            out SafeCFArrayHandle pCertsOut,
            out int pOSStatusOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_StoreEnumerateMachineRoot(
            out SafeCFArrayHandle pCertsOut,
            out int pOSStatusOut);

        internal static SafeCFArrayHandle StoreEnumerateRoot(StoreLocation location)
        {
            int result;
            SafeCFArrayHandle matches;
            int osStatus;

            if (location == StoreLocation.CurrentUser)
            {
                result = AppleCryptoNative_StoreEnumerateUserRoot(out matches, out osStatus);
            }
            else if (location == StoreLocation.LocalMachine)
            {
                result = AppleCryptoNative_StoreEnumerateMachineRoot(out matches, out osStatus);
            }
            else
            {
                Debug.Fail($"Unrecognized StoreLocation value: {location}");
                throw new CryptographicException();
            }

            if (result == 1)
            {
                return matches;
            }

            matches.Dispose();

            if (result == 0)
                throw CreateExceptionForOSStatus(osStatus);

            Debug.Fail($"Unexpected result from AppleCryptoNative_StoreEnumerateRoot ({location}): {result}");
            throw new CryptographicException();
        }
    }
}
