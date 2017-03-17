// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

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
#if !SYSNETSECURITY_NO_OPENSSL
        static SslInitializer()
        {
            CryptoInitializer.Initialize();

            //Call ssl specific initializer
            Ssl.EnsureLibSslInitialized();
        }
#endif

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors
        }
    }
}
