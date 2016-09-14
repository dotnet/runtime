// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Internal.Cryptography;

namespace System.Security.Cryptography
{
    public sealed partial class DSACng : DSA
    {
        /// <summary>
        ///     Creates a new DSACng object that will use the specified key. The key's
        ///     <see cref="CngKey.AlgorithmGroup" /> must be Dsa. This constructor
        ///     creates a copy of the key. Hence, the caller can safely dispose of the 
        ///     passed in key and continue using the DSACng object. 
        /// </summary>
        /// <param name="key">Key to use for DSA operations</param>
        /// <exception cref="ArgumentException">if <paramref name="key" /> is not an DSA key</exception>
        /// <exception cref="ArgumentNullException">if <paramref name="key" /> is null.</exception>
        public DSACng(CngKey key)
        {
            if (key == null)
                throw new ArgumentNullException(nameof(key));

            if (key.AlgorithmGroup != CngAlgorithmGroup.Dsa)
                throw new ArgumentException(SR.Cryptography_ArgDSARequiresDSAKey, nameof(key));

            Key = CngAlgorithmCore.Duplicate(key);
        }

        protected override void Dispose(bool disposing)
        {
            _core.Dispose();
        }

        private CngAlgorithmCore _core;
    }
}
