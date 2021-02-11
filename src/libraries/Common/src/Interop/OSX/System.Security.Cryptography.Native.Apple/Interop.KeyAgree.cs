// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Apple;
using Microsoft.Win32.SafeHandles;

internal static partial class Interop
{
    internal static partial class AppleCrypto
    {
        [DllImport(Libraries.AppleCryptoNative)]
        private static extern int AppleCryptoNative_EcdhKeyAgree(
            SafeSecKeyRefHandle privateKey,
            SafeSecKeyRefHandle publicKey,
            out SafeCFDataHandle cfDataOut,
            out SafeCFErrorHandle cfErrorOut);

        internal static byte[]? EcdhKeyAgree(
            SafeSecKeyRefHandle privateKey,
            SafeSecKeyRefHandle publicKey,
            Span<byte> opportunisticDestination,
            out int bytesWritten)
        {
            const int Success = 1;
            const int kErrorSeeError = -2;

            SafeCFDataHandle data;
            SafeCFErrorHandle error;

            int status = AppleCryptoNative_EcdhKeyAgree(privateKey, publicKey, out data, out error);

            using (data)
            using (error)
            {
                if (status == kErrorSeeError)
                {
                    throw CreateExceptionForCFError(error);
                }

                if (status == Success && !data.IsInvalid)
                {
                    if (CoreFoundation.TryCFWriteData(data, opportunisticDestination, out bytesWritten))
                    {
                        return null;
                    }

                    bytesWritten = 0;
                    return CoreFoundation.CFGetData(data);
                }

                Debug.Fail($"Unexpected status ({status})");
                throw new CryptographicException();
            }
        }
    }
}
