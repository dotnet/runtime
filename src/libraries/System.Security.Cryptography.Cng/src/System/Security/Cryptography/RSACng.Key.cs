// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///     Gets the key that will be used by the RSA object for any cryptographic operation that it uses.
        ///     This key object will be disposed if the key is reset, for instance by changing the KeySize
        ///     property, using ImportParamers to create a new key, or by Disposing of the parent RSA object.
        ///     Therefore, you should make sure that the key object is no longer used in these scenarios. This
        ///     object will not be the same object as the CngKey passed to the RSACng constructor if that
        ///     constructor was used, however it will point at the same CNG key.
        /// </summary>
        public CngKey Key
        {
            get
            {
                CngKey key = _core.GetOrGenerateKey(KeySize, CngAlgorithm.Rsa);
                return key;
            }

            private set
            {
                CngKey key = value;
                Debug.Assert(key != null, "key != null");
                if (key.AlgorithmGroup != CngAlgorithmGroup.Rsa)
                    throw new ArgumentException(SR.Cryptography_ArgRSARequiresRSAKey, nameof(value));
                _core.SetKey(key);

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
                // For RSA there are known cases where this change matters. RSACryptoServiceProvider can
                // create a 384-bit RSA key, which we consider too small to be legal. It can also create
                // a 1032-bit RSA key, which we consider illegal because it doesn't match our 64-bit
                // alignment requirement. (In both cases Windows loads it just fine)
                ForceSetKeySize(key.KeySize);
            }
        }

        private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
        {
            return Key.Handle;
        }
    }
}

