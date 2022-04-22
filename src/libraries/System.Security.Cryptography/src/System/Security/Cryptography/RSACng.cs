// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        private CngAlgorithmCore _core = new CngAlgorithmCore(nameof(RSACng));

        /// <summary>
        ///     Creates a new RSACng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be Rsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the
        ///     passed in key and continue using the RSACng object.
        /// </summary>
        /// <param name="key">Key to use for RSA operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an RSA key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        [SupportedOSPlatform("windows")]
        public RSACng(CngKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
                throw new ArgumentException(SR.Cryptography_ArgRSARequiresRSAKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        protected override void Dispose(bool disposing)
        {
            _core.Dispose();
        }

        private void ThrowIfDisposed()
        {
            _core.ThrowIfDisposed();
        }
    }
}
