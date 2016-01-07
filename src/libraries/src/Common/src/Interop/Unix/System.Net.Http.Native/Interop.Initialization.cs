// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Runtime.InteropServices;

internal static partial class Interop
{
    // Initialization of libcurl is done in a static constructor.
    // This enables a project simply to include this file, and any usage of any of
    // the Http functions will trigger initialization.
    
    internal static partial class Http
    {
        static Http()
        {
            HttpInitializer.Initialize();
        }
    }

    internal static class HttpInitializer
    {
        static HttpInitializer()
        {
            // CURL uses OpenSSL which me must initialize first to guarantee thread-safety
            CryptoInitializer.Initialize();
            
            if (EnsureCurlIsInitialized() != 0)
            {
                throw new InvalidOperationException();
            }
        }

        internal static void Initialize()
        {
            // No-op that exists to provide a hook for other static constructors
        }

        [DllImport(Libraries.HttpNative, EntryPoint = "HttpNative_EnsureCurlIsInitialized")]
        private static extern int EnsureCurlIsInitialized();
    }
}
