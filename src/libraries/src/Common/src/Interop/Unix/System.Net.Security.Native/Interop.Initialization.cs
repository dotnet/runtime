// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Initialization of libssl threading support is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the libssl functions will trigger initialization of the threading support.

    internal static partial class libssl
    {
        static libssl()
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
            SSL_library_init();
            SSL_load_error_strings();
        }

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors
        }

        [DllImport(Libraries.LibSsl, SetLastError = true, EntryPoint = "SSL_load_error_strings")]
        internal static extern void SSL_load_error_strings();

        [DllImport(Libraries.LibSsl, SetLastError = true, EntryPoint = "SSL_library_init")]
        internal static extern int SSL_library_init();
    }
}
