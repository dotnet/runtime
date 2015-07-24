// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.IO;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class HMAC : KeyedHashAlgorithm
    {
        protected HMAC()
        {
        }

        public String HashName
        {
            get
            {
                return _hashName;
            }
            set
            {
                // On the desktop, setting the HashName selects (or switches over to) a new hashing algorithm via CryptoConfig.
                // Our intended refactoring turns HMAC back into an abstract class with no algorithm-specific implementation.
                // Changing the HashName would not have the intended effect so throw a proper exception so the developer knows what's up.
                //
                // We still have to allow setting it the first time as the contract provides no other way to do so.
                // Since the set is public, ensure that hmac.HashName = hmac.HashName works without throwing.

                if (_hashName != null && value != _hashName)
                    throw new PlatformNotSupportedException(SR.HashNameMultipleSetNotSupported);
                _hashName = value;
            }
        }

        public override byte[] Key
        {
            get
            {
                return base.Key;
            }

            set
            {
                base.Key = value;
            }
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
        }

        protected override void HashCore(byte[] rgb, int ib, int cb)
        {
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);
        }

        protected override byte[] HashFinal()
        {
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);
        }

        public override void Initialize()
        {
            throw new PlatformNotSupportedException(SR.CryptoConfigNotSupported);
        }

        private String _hashName;
    }
}

