// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.InteropServices;

internal static partial class Interop
{
    internal static partial class Crypto
    {
        private static volatile bool s_loadedLegacy;
        private static readonly object s_legacyLoadLock = new object();

        [DllImport(Libraries.CryptoNative)]
        private static extern void CryptoNative_RegisterLegacyAlgorithms();

        internal static void EnsureLegacyAlgorithmsRegistered()
        {
            if (!s_loadedLegacy)
            {
                lock (s_legacyLoadLock)
                {
                    if (!s_loadedLegacy)
                    {
                        CryptoNative_RegisterLegacyAlgorithms();
                        s_loadedLegacy = true;
                    }
                }
            }
        }
    }
}
