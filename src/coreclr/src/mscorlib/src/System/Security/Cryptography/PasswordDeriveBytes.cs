// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography {
    using System.IO;
    using System.Runtime.CompilerServices;
    using System.Runtime.InteropServices;
    using System.Runtime.Versioning;
    using System.Security.Cryptography.X509Certificates;
    using System.Text;
    using System.Globalization;
    using System.Diagnostics.Contracts;

    [System.Runtime.InteropServices.ComVisible(true)]
    public class PasswordDeriveBytes : DeriveBytes {
        private int             _extraCount;
        private int             _prefix;
        private int             _iterations;
        private byte[]          _baseValue;
        private byte[]          _extra;
        private byte[]          _salt;
        private string          _hashName;
        private byte[]          _password;
        private HashAlgorithm   _hash;
        private CspParameters   _cspParams;

        [System.Security.SecurityCritical] // auto-generated
        private SafeProvHandle _safeProvHandle = null;
        private SafeProvHandle ProvHandle {
            [System.Security.SecurityCritical]  // auto-generated
            get {
                if (_safeProvHandle == null) {
                    lock (this) {
                        if (_safeProvHandle == null) {
                            SafeProvHandle safeProvHandle = Utils.AcquireProvHandle(_cspParams);
                            System.Threading.Thread.MemoryBarrier();
                            _safeProvHandle = safeProvHandle;
                        }
                    }
                }
                return _safeProvHandle;
            }
        }

        //
        // public constructors
        //

        public PasswordDeriveBytes (String strPassword, byte[] rgbSalt) : this (strPassword, rgbSalt, new CspParameters()) {}

        public PasswordDeriveBytes (byte[] password, byte[] salt) : this (password, salt, new CspParameters()) {}

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, string strHashName, int iterations) : 
            this (strPassword, rgbSalt, strHashName, iterations, new CspParameters()) {}

        public PasswordDeriveBytes (byte[] password, byte[] salt, string hashName, int iterations) : 
            this (password, salt, hashName, iterations, new CspParameters()) {}

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, CspParameters cspParams) :
            this (strPassword, rgbSalt, "SHA1", 100, cspParams) {}

        public PasswordDeriveBytes (byte[] password, byte[] salt, CspParameters cspParams) : 
            this (password, salt, "SHA1", 100, cspParams) {}

        public PasswordDeriveBytes (string strPassword, byte[] rgbSalt, String strHashName, int iterations, CspParameters cspParams) :
            this ((new UTF8Encoding(false)).GetBytes(strPassword), rgbSalt, strHashName, iterations, cspParams) {}

        // This method needs to be safe critical, because in debug builds the C# compiler will include null
        // initialization of the _safeProvHandle field in the method.  Since SafeProvHandle is critical, a
        // transparent reference triggers an error using PasswordDeriveBytes.
        [SecuritySafeCritical]
        public PasswordDeriveBytes (byte[] password, byte[] salt, String hashName, int iterations, CspParameters cspParams) {
            this.IterationCount = iterations;
            this.Salt = salt;
            this.HashName = hashName;
            _password = password;
            _cspParams = cspParams;
        }

        //
        // public properties
        //

        public String HashName {
            get { return _hashName; }
            set { 
                if (_baseValue != null)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", "HashName"));
                _hashName = value;
                _hash = (HashAlgorithm) CryptoConfig.CreateFromName(_hashName);
            }
        }

        public int IterationCount {
            get { return _iterations; }
            set { 
                if (value <= 0)
                    throw new ArgumentOutOfRangeException("value", Environment.GetResourceString("ArgumentOutOfRange_NeedPosNum"));
                Contract.EndContractBlock();
                if (_baseValue != null)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", "IterationCount"));
                _iterations = value;
            }
        }

        public byte[] Salt {
            get {
                if (_salt == null) 
                    return null;
                return (byte[]) _salt.Clone();
            }
            set {
                if (_baseValue != null)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_ValuesFixed", "Salt"));
                if (value == null)
                    _salt = null;
                else
                    _salt = (byte[]) value.Clone();
            }
        }

        //
        // public methods
        //

        [System.Security.SecuritySafeCritical]  // auto-generated
        [Obsolete("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")]
    // disable csharp compiler warning #0809: obsolete member overrides non-obsolete member:
    // Even though the compiler will not generate a warning for the obsolete method, the method still needs
    // to be marked as obsolete so that generated documentation (such as MSDN) correctly shows that the method
    // is obsolete and to use Rfc2898DeriveBytes instead.
#pragma warning disable 0809
        public override byte[] GetBytes(int cb) {
            int         ib = 0;
            byte[]      rgb;
            byte[]      rgbOut = new byte[cb];

            if (_baseValue == null) {
                ComputeBaseValue();
            }
            else if (_extra != null) {
                ib = _extra.Length - _extraCount;
                if (ib >= cb) {
                    Buffer.InternalBlockCopy(_extra, _extraCount, rgbOut, 0, cb);
                    if (ib > cb)
                        _extraCount += cb;
                    else
                        _extra = null;

                    return rgbOut;
                }
                else {
                    //
                    // Note: The second parameter should really be _extraCount instead
                    // However, changing this would constitute a breaking change compared
                    // to what has shipped in V1.x.
                    //

                    Buffer.InternalBlockCopy(_extra, ib, rgbOut, 0, ib);
                    _extra = null;
                }
            }

            rgb = ComputeBytes(cb-ib);
            Buffer.InternalBlockCopy(rgb, 0, rgbOut, ib, cb-ib);
            if (rgb.Length + ib > cb) {
                _extra = rgb;
                _extraCount = cb-ib;
            }
            return rgbOut;
        }
#pragma warning restore 0809

        public override void Reset() {
            _prefix = 0;
            _extra = null;
            _baseValue = null;
        }

        protected override void Dispose(bool disposing) {
            base.Dispose(disposing);

            if (disposing) {
                if (_hash != null) {
                    _hash.Dispose();
                }

                if (_baseValue != null) {
                    Array.Clear(_baseValue, 0, _baseValue.Length);
                }
                if (_extra != null) {
                    Array.Clear(_extra, 0, _extra.Length);
                }
                if (_password != null) {
                    Array.Clear(_password, 0, _password.Length);
                }
                if (_salt != null) {
                    Array.Clear(_salt, 0, _salt.Length);
                }
            }
        }

        [System.Security.SecuritySafeCritical]  // auto-generated
        public byte[] CryptDeriveKey(string algname, string alghashname, int keySize, byte[] rgbIV)
        {
            if (keySize < 0)
                throw new CryptographicException(Environment.GetResourceString("Cryptography_InvalidKeySize"));

            int algidhash = X509Utils.NameOrOidToAlgId(alghashname, OidGroup.HashAlgorithm);
            if (algidhash == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));
            int algid = X509Utils.NameOrOidToAlgId(algname, OidGroup.AllGroups);
            if (algid == 0) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidAlgorithm"));

            // Validate the rgbIV array
            if (rgbIV == null) 
                throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_InvalidIV"));

            byte[] key = null;
            DeriveKey(ProvHandle, algid, algidhash, 
                      _password, _password.Length, keySize << 16, rgbIV, rgbIV.Length, 
                      JitHelpers.GetObjectHandleOnStack(ref key));
            return key;
        }

        //
        // private methods
        //

        [System.Security.SecurityCritical]  // auto-generated
        [DllImport(JitHelpers.QCall, CharSet = CharSet.Unicode), SuppressUnmanagedCodeSecurity]
        private static extern void DeriveKey(SafeProvHandle hProv, int algid, int algidHash, 
                                             byte[] password, int cbPassword, int dwFlags, byte[] IV, int cbIV, 
                                             ObjectHandleOnStack retKey);

        private byte[] ComputeBaseValue() {
            _hash.Initialize();
            _hash.TransformBlock(_password, 0, _password.Length, _password, 0);
            if (_salt != null)
                _hash.TransformBlock(_salt, 0, _salt.Length, _salt, 0);
            _hash.TransformFinalBlock(EmptyArray<Byte>.Value, 0, 0);
            _baseValue = _hash.Hash;
            _hash.Initialize();

            for (int i=1; i<(_iterations-1); i++) {
                _hash.ComputeHash(_baseValue);
                _baseValue = _hash.Hash;
            }
            return _baseValue;
        }

        [System.Security.SecurityCritical]  // auto-generated
        private byte[] ComputeBytes(int cb) {
            int                 cbHash;
            int                 ib = 0;
            byte[]              rgb;

            _hash.Initialize();
            cbHash = _hash.HashSize / 8;
            rgb = new byte[((cb+cbHash-1)/cbHash)*cbHash];

            using (CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write)) {
                HashPrefix(cs);
                cs.Write(_baseValue, 0, _baseValue.Length);
                cs.Close();
            }

            Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash);
            ib += cbHash;

            while (cb > ib) {
                _hash.Initialize();
                using (CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write)) {
                    HashPrefix(cs);
                    cs.Write(_baseValue, 0, _baseValue.Length);
                    cs.Close();
                }

                Buffer.InternalBlockCopy(_hash.Hash, 0, rgb, ib, cbHash);
                ib += cbHash;
            }

            return rgb;
        }

        void HashPrefix(CryptoStream cs) {
            int    cb = 0;
            byte[] rgb = {(byte)'0', (byte)'0', (byte)'0'};

            if (_prefix > 999)
                    throw new CryptographicException(Environment.GetResourceString("Cryptography_PasswordDerivedBytes_TooManyBytes"));

            if (_prefix >= 100) {
                rgb[0] += (byte) (_prefix /100);
                cb += 1;
            }
            if (_prefix >= 10) {
                rgb[cb] += (byte) ((_prefix % 100) / 10);
                cb += 1;
            }
            if (_prefix > 0) {
                rgb[cb] += (byte) (_prefix % 10);
                cb += 1;
                cs.Write(rgb, 0, cb);
            }
            _prefix += 1;
        }
    }
}
