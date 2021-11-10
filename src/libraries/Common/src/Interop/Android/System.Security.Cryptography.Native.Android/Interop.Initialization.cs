// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Initialization of libcrypto threading support is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the System.Security.Cryptography.Native functions will trigger
    // initialization of the threading support.

    internal static partial class Crypto
    {
        static Crypto()
        {
            CryptoInitializer.Initialize();
        }
    }

    internal static partial class OpenSsl
    {
        static OpenSsl()
        {
            CryptoInitializer.Initialize();
        }
    }

    internal static class CryptoInitializer
    {
        static CryptoInitializer()
        {
            if (EnsureOpenSslInitialized() != 0)
            {
                // Ideally this would be a CryptographicException, but we use
                // OpenSSL in libraries lower than System.Security.Cryptography.
                // It's not a big deal, though: this will already be wrapped in a
                // TypeLoadException, and this failing means something is very
                // wrong with the system's configuration and any code using
                // these libraries will be unable to operate correctly.
                throw new InvalidOperationException();
            }
        }

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors.
        }

        [DllImport(Libraries.AndroidCryptoNative, EntryPoint = "CryptoNative_EnsureOpenSslInitialized")]
        private static extern int EnsureOpenSslInitialized();
    }
}
