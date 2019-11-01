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

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_StoreEnumerateUserDisallowed(
            out SafeCFArrayHandle pCertsOut,
            out int pOSStatusOut);

        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_StoreEnumerateMachineDisallowed(
            out SafeCFArrayHandle pCertsOut,
            out int pOSStatusOut);

        private delegate int StoreEnumerator(out SafeCFArrayHandle pCertsOut, out int pOSStatusOut);

        internal static SafeCFArrayHandle StoreEnumerateDisallowed(StoreLocation location)
        {
            return EnumerateStore(
                location,
                AppleCryptoNative_StoreEnumerateUserDisallowed,
                AppleCryptoNative_StoreEnumerateMachineDisallowed);
        }

        internal static SafeCFArrayHandle StoreEnumerateRoot(StoreLocation location)
        {
            return EnumerateStore(
                location,
                AppleCryptoNative_StoreEnumerateUserRoot,
                AppleCryptoNative_StoreEnumerateMachineRoot);
        }

        private static SafeCFArrayHandle EnumerateStore(
            StoreLocation location,
            StoreEnumerator userEnumerator,
            StoreEnumerator machineEnumerator)
        {
            const int RetryLimit = 3;
            int osStatus = 0;

            // Occasionally calls to enumerate the trust list get errSecInvalidRecord.
            // So, if we fail with result 0 ("see osStatus") just retry and see if the
            // intermediate state has flushed itself.
            for (int i = 0; i < RetryLimit; i++)
            {
                int result;
                SafeCFArrayHandle matches;

                if (location == StoreLocation.CurrentUser)
                {
                    result = userEnumerator(out matches, out osStatus);
                }
                else if (location == StoreLocation.LocalMachine)
                {
                    result = machineEnumerator(out matches, out osStatus);
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
                {
                    // Instead of limiting it to particular error codes, just try it again.
                    // A permanent error will be stable, a temporary one will hopefully go away.
                    continue;
                }

                Debug.Fail($"Unexpected result from {location} trust store enumeration: {result}");
                throw new CryptographicException();
            }

            throw CreateExceptionForOSStatus(osStatus);
        }
    }
}
