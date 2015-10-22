// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Initialization of libssl threading support is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the Ssl functions will trigger initialization of the threading support.

    internal static partial class Ssl
    {
        static Ssl()
        {
            SslInitializer.Initialize();
        }
    }

    internal static class SslInitializer
    {
        static SslInitializer()
        {
            CryptoInitializer.Initialize();

            //Call ssl specific initializer
            Ssl.EnsureLibSslInitialized();
        }

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors
        }
    }
}
