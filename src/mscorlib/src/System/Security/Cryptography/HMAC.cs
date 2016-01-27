// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

//
// For test vectors, see RFC2104, e.g. http://www.faqs.org/rfcs/rfc2104.html
//

using System.Diagnostics.Contracts;

namespace System.Security.Cryptography {
    [System.Runtime.InteropServices.ComVisible(true)]
    public abstract class HMAC : KeyedHashAlgorithm {
        //
        // protected members
        //

        // an HMAC uses a hash function where data is hashed by iterating a basic compression 
        // function on blocks of data. BlockSizeValue is the byte size of such a block
        
        private int blockSizeValue = 64;

        protected int BlockSizeValue {
            get {
                return blockSizeValue;
            }
            set {
                blockSizeValue = value;
            }        
        }       
 
        internal string m_hashName;

        internal HashAlgorithm m_hash1;
        internal HashAlgorithm m_hash2;

        //
        // private members
        //

        // m_inner = PaddedKey ^ {0x36,...,0x36}
        // m_outer = PaddedKey ^ {0x5C,...,0x5C}
        private byte[] m_inner;
        private byte[] m_outer;

        private bool m_hashing = false;

        private void UpdateIOPadBuffers () {
            if (m_inner == null)
                m_inner = new byte[BlockSizeValue];
            if (m_outer == null)
                m_outer = new byte[BlockSizeValue];

            int i;
            for (i=0; i < BlockSizeValue; i++) {
                m_inner[i] = 0x36;
                m_outer[i] = 0x5C;
            }
            for (i=0; i < KeyValue.Length; i++) {
                m_inner[i] ^= KeyValue[i];
                m_outer[i] ^= KeyValue[i];
            }
        }

        internal void InitializeKey (byte[] key) {
            // When we change the key value, we'll need to update the initial values of the inner and outer
            // computation buffers.  In the case of correct HMAC vs Whidbey HMAC, these buffers could get
            // generated to a different size than when we started.
            m_inner = null;
            m_outer = null;

            if (key.Length > BlockSizeValue) {
                KeyValue = m_hash1.ComputeHash(key);
                // No need to call Initialize, ComputeHash will do it for us
            } else {
                KeyValue = (byte[]) key.Clone();
            }
            UpdateIOPadBuffers();
        }

        //
        // public properties
        //

        public override byte[] Key {
            get { return (byte[]) KeyValue.Clone(); }
            set {
                if (m_hashing)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashKeySet"));
                InitializeKey(value);
            }
        }

        public string HashName {
            get { return m_hashName; }
#if FEATURE_CRYPTO
            set { 
                if (m_hashing)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_HashNameSet"));
                m_hashName = value; 
                // create the hash algorithms
                m_hash1 = HashAlgorithm.Create(m_hashName);
                m_hash2 = HashAlgorithm.Create(m_hashName);
            }
#endif // FEATURE_CRYPTO
        }

        //
        // public methods
        //

        new static public HMAC Create () {
            return Create("System.Security.Cryptography.HMAC");
        }

        new static public HMAC Create (string algorithmName) {
            return (HMAC) CryptoConfig.CreateFromName(algorithmName);
        }

        public override void Initialize () {
            m_hash1.Initialize();
            m_hash2.Initialize();
            m_hashing = false;
        }

        protected override void HashCore (byte[] rgb, int ib, int cb) {
            if (m_hashing == false) {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }
            m_hash1.TransformBlock(rgb, ib, cb, rgb, ib);
        }

        protected override byte[] HashFinal () {
            if (m_hashing == false) {
                m_hash1.TransformBlock(m_inner, 0, m_inner.Length, m_inner, 0);
                m_hashing = true;
            }
            // finalize the original hash
            m_hash1.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            byte[] hashValue1 = m_hash1.HashValue;
            // write the outer array
            m_hash2.TransformBlock(m_outer, 0, m_outer.Length, m_outer, 0);
            // write the inner hash and finalize the hash
            m_hash2.TransformBlock(hashValue1, 0, hashValue1.Length, hashValue1, 0);
            m_hashing = false;
            m_hash2.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            return m_hash2.HashValue;
        }

        //
        // IDisposable methods
        //

        protected override void Dispose (bool disposing) {
            if (disposing) {
                if (m_hash1 != null)
                    ((IDisposable)m_hash1).Dispose();
                if (m_hash2 != null)
                    ((IDisposable)m_hash2).Dispose();
                if (m_inner != null)
                    Array.Clear(m_inner, 0, m_inner.Length);
                if (m_outer != null)
                    Array.Clear(m_outer, 0, m_outer.Length);
            }
            // call the base class's Dispose
            base.Dispose(disposing);
        }

#if FEATURE_CRYPTO
        /// <summary>
        ///     Get a hash algorithm instance falling back to a second algorithm in FIPS mode. For instance,
        ///     use SHA256Managed by default but fall back to SHA256CryptoServiceProvider which is FIPS
        ///     certified if FIPS is enabled.
        /// </summary>
        /// <returns></returns>
        internal static HashAlgorithm GetHashAlgorithmWithFipsFallback(Func<HashAlgorithm> createStandardHashAlgorithmCallback, 
                                                                       Func<HashAlgorithm> createFipsHashAlgorithmCallback) {
            Contract.Requires(createStandardHashAlgorithmCallback != null);
            Contract.Requires(createFipsHashAlgorithmCallback != null);

            // Use the standard algorithm implementation by default - in FIPS mode try to fall back to the
            // FIPS implementation.
            if (CryptoConfig.AllowOnlyFipsAlgorithms) {
                try {
                    return createFipsHashAlgorithmCallback();
                }
                catch (PlatformNotSupportedException e) {
                    // We need to wrap the PlatformNotSupportedException into an InvalidOperationException to
                    // remain compatible with the error that would be triggered in previous runtimes.
                    throw new InvalidOperationException(e.Message, e);
                }
            }
            else {
                return createStandardHashAlgorithmCallback();
            }
        }
#endif // FEATURE_CRYPTO
    }
}
