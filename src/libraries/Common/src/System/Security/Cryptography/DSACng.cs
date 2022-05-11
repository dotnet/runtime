// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    public sealed partial class DSACng : DSA, IRuntimeAlgorithm
    {
        /// <summary>
        ///     Create a DSACng algorithm with a random 2048 bit key pair.
        /// </summary>
        [SupportedOSPlatform("windows")]
        public DSACng()
            : this(keySize: s_defaultKeySize)
        {
        }

        /// <summary>
        ///     Creates a new DSACng object that will use a randomly generated key of the specified size.
        ///     Valid key sizes range from 512 to 3072 bits, in increments of 64. It's suggested that a
        ///     minimum size of 2048 bits be used for all keys.
        /// </summary>
        /// <param name="keySize">Size of the key to generate, in bits.</param>
        /// <exception cref="CryptographicException">if <paramref name="keySize" /> is not valid</exception>
        [SupportedOSPlatform("windows")]
        public DSACng(int keySize)
        {
            LegalKeySizesValue = s_legalKeySizes;
            KeySize = keySize;
        }

        public override KeySizes[] LegalKeySizes
        {
            get
            {
                return base.LegalKeySizes;
            }
        }

        public override string SignatureAlgorithm => "DSA";
        public override string? KeyExchangeAlgorithm => null;

        private void ForceSetKeySize(int newKeySize)
        {
            // Our LegalKeySizes value stores the values that we encoded as being the correct
            // legal key size limitations for this algorithm, as documented on MSDN.
            //
            // But on a new OS version we might not question if our limit is accurate, or MSDN
            // could have been inaccurate to start with.
            //
            // Since the key is already loaded, we know that Windows thought it to be valid;
            // therefore we should set KeySizeValue directly to bypass the LegalKeySizes conformance
            // check.
            //
            KeySizeValue = newKeySize;
        }

        private static bool Supports2048KeySize()
        {
            Debug.Assert(OperatingSystem.IsWindows());
            Version version = Environment.OSVersion.Version;
            bool isAtLeastWindows8 = version.Major > 6 || (version.Major == 6 && version.Minor >= 2);
            return isAtLeastWindows8;
        }

        private static readonly KeySizes[] s_legalKeySizes = new KeySizes[] { new KeySizes(minSize: 512, maxSize: 3072, skipSize: 64) };
        private static readonly int s_defaultKeySize = Supports2048KeySize() ? 2048 : 1024;
    }
}
