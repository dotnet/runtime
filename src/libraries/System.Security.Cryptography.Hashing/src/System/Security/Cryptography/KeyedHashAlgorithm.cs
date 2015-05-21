// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System;
using System.Diagnostics;

namespace System.Security.Cryptography
{
    public abstract class KeyedHashAlgorithm : HashAlgorithm
    {
        protected KeyedHashAlgorithm()
        {
        }

        public virtual byte[] Key
        {
            get
            {
                return _key.CloneByteArray();
            }

            set
            {
                _key = value.CloneByteArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // For keyed hash algorithms, we always want to zero out the key value
            if (disposing)
            {
                if (_key != null)
                {
                    Array.Clear(_key, 0, _key.Length);
                }
                _key = null;
            }
            base.Dispose(disposing);
        }

        private byte[] _key;
    }
}

