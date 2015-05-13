// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;

internal static partial class Interop
{
    internal static partial class libcrypto
    {
        // Initialization of libcrypto threading support is done in a static constructor.
        // This enables a project simply to include this file, and any usage of any of
        // the libcrypto functions will trigger initialization of the threading support.
        static libcrypto()
        {
            if (Interop.libcoreclr.EnsureOpenSslInitialized() != 0)
            {
                throw new CryptographicException();
            }

            // Ensure that the error message table is loaded.
            Interop.libcrypto.ERR_load_crypto_strings();
        }
    }
}
