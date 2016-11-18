// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    public abstract class KeyedHashAlgorithm : HashAlgorithm
    {
        protected KeyedHashAlgorithm() { }

        public static new KeyedHashAlgorithm Create()
        {
            return Create("System.Security.Cryptography.KeyedHashAlgorithm");
        }

        public static new KeyedHashAlgorithm Create(string algName)
        {
            throw new PlatformNotSupportedException();
        }

        public virtual byte[] Key
        {
            get
            {
                return KeyValue.CloneByteArray();
            }

            set
            {
                KeyValue = value.CloneByteArray();
            }
        }

        protected override void Dispose(bool disposing)
        {
            // For keyed hash algorithms, we always want to zero out the key value
            if (disposing)
            {
                if (KeyValue != null)
                {
                    Array.Clear(KeyValue, 0, KeyValue.Length);
                }
                KeyValue = null;
            }
            base.Dispose(disposing);
        }

        protected byte[] KeyValue;
    }
}
