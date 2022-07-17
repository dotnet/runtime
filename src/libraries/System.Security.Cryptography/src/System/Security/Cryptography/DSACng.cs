// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Runtime.Versioning;
using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class DSACng : DSA
    {
        private CngAlgorithmCore _core = new CngAlgorithmCore(nameof(DSACng));

        /// <summary>
        ///     Creates a new DSACng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be Dsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the
        ///     passed in key and continue using the DSACng object.
        /// </summary>
        /// <param name="key">Key to use for DSA operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an DSA key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        [SupportedOSPlatform("windows")]
        public DSACng(CngKey key)
        {
            ArgumentNullException.ThrowIfNull(key);

            if (key.AlgorithmGroup != CngAlgorithmGroup.Dsa)
                throw new ArgumentException(SR.Cryptography_ArgDSARequiresDSAKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        /// <summary>
        ///     Creates a new ECDsaCng object that will use the specified key. Unlike the public
        ///     constructor, this does not copy the key and ownership is transferred. The
        ///     <paramref name="transferOwnership"/> parameter must be true.
        /// </summary>
        /// <param name="key">Key to use for DSA operations</param>
        /// <param name="transferOwnership">
        /// Must be true. Signals that ownership of <paramref name="key"/> will be transferred to the new instance.
        /// </param>
        [SupportedOSPlatform("windows")]
        internal DSACng(CngKey key, bool transferOwnership)
        {
            Debug.Assert(key is not null);
            Debug.Assert(key.AlgorithmGroup == CngAlgorithmGroup.Dsa);
            Debug.Assert(transferOwnership);

            Key = key;
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
