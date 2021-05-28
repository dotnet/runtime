// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;

#pragma warning disable CA5373 // Call to obsolete key derivation function PasswordDeriveBytes.*

namespace System.Security.Cryptography
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public partial class PasswordDeriveBytes : DeriveBytes
    {
        private const string HashAlgorithmUnreferencedCodeMessage = "The hash implementation might be removed. Ensure the referenced hash algorithm is not trimmed.";

        private int _extraCount;
        private int _prefix;
        private int _iterations;
        private byte[]? _baseValue;
        private byte[]? _extra;
        private byte[]? _salt;
        private readonly byte[] _password;
        private string? _hashName;
        private HashAlgorithm? _hash;
        private readonly CspParameters? _cspParams;

#pragma warning disable CA1416 // Validate platform compatibility, CspParametersis is windows only type, we might want to annotate this constructors windows only, suppressing for now
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt) : this(strPassword, rgbSalt, new CspParameters()) { }

        public PasswordDeriveBytes(byte[] password, byte[]? salt) : this(password, salt, new CspParameters()) { }

        [RequiresUnreferencedCode(HashAlgorithmUnreferencedCodeMessage)]
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations) :
            this(strPassword, rgbSalt, strHashName, iterations, new CspParameters()) { }

        [RequiresUnreferencedCode(HashAlgorithmUnreferencedCodeMessage)]
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations) :
            this(password, salt, hashName, iterations, new CspParameters()) { }
#pragma warning restore CA1416

#pragma warning disable SYSLIB0021 // Obsolete: derived cryptographic types
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct hash algorithm is being preserved by the DynamicDependency.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(SHA1CryptoServiceProvider))]
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, CspParameters? cspParams) :
            this(strPassword, rgbSalt, "SHA1", 100, cspParams) { }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode", Justification = "The correct hash algorithm is being preserved by the DynamicDependency.")]
        [DynamicDependency(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor, typeof(SHA1CryptoServiceProvider))]
        public PasswordDeriveBytes(byte[] password, byte[]? salt, CspParameters? cspParams) :
            this(password, salt, "SHA1", 100, cspParams) { }
#pragma warning restore SYSLIB0021

        [RequiresUnreferencedCode(HashAlgorithmUnreferencedCodeMessage)]
        public PasswordDeriveBytes(string strPassword, byte[]? rgbSalt, string strHashName, int iterations, CspParameters? cspParams) :
            this((new UTF8Encoding(false)).GetBytes(strPassword), rgbSalt, strHashName, iterations, cspParams) { }

        [RequiresUnreferencedCode(HashAlgorithmUnreferencedCodeMessage)]
        public PasswordDeriveBytes(byte[] password, byte[]? salt, string hashName, int iterations, CspParameters? cspParams)
        {
            IterationCount = iterations;
            Salt = salt;
            HashName = hashName;
            _password = password;
            _cspParams = cspParams;
        }

        public string HashName
        {
            get { return _hashName!; }
            [RequiresUnreferencedCode(HashAlgorithmUnreferencedCodeMessage)]
            set
            {
                if (_baseValue != null)
                    throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_ValuesFixed, nameof(HashName));

                _hashName = value;
                _hash = (HashAlgorithm?)CryptoConfig.CreateFromName(_hashName);
            }
        }

        public int IterationCount
        {
            get { return _iterations; }
            set
            {
                if (value <= 0)
                    throw new ArgumentOutOfRangeException(nameof(value), SR.ArgumentOutOfRange_NeedPosNum);
                if (_baseValue != null)
                    throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_ValuesFixed, nameof(IterationCount));

                _iterations = value;
            }
        }

        public byte[]? Salt
        {
            get
            {
                return (byte[]?)_salt?.Clone();
            }
            set
            {
                if (_baseValue != null)
                    throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_ValuesFixed, nameof(Salt));

                _salt = (byte[]?)value?.Clone();
            }
        }

        [Obsolete("Rfc2898DeriveBytes replaces PasswordDeriveBytes for deriving key material from a password and is preferred in new applications.")]
#pragma warning disable 0809 // obsolete member overrides non-obsolete member
        public override byte[] GetBytes(int cb)
        {
            int ib = 0;
            byte[] rgb;
            byte[] rgbOut = new byte[cb];

            if (_baseValue == null)
            {
                ComputeBaseValue();
            }
            else if (_extra != null)
            {
                ib = _extra.Length - _extraCount;
                if (ib >= cb)
                {
                    Buffer.BlockCopy(_extra, _extraCount, rgbOut, 0, cb);

                    if (ib > cb)
                    {
                        _extraCount += cb;
                    }
                    else
                    {
                        _extra = null;
                    }

                    return rgbOut;
                }
                else
                {
                    // Note: The second parameter should really be _extraCount instead.
                    // However, changing this would constitute a breaking change.
                    Buffer.BlockCopy(_extra, ib, rgbOut, 0, ib);
                    _extra = null;
                }
            }

            rgb = ComputeBytes(cb - ib);
            Buffer.BlockCopy(rgb, 0, rgbOut, ib, cb - ib);
            if (rgb.Length + ib > cb)
            {
                _extra = rgb;
                _extraCount = cb - ib;
            }
            return rgbOut;
        }
#pragma warning restore 0809

        public override void Reset()
        {
            _prefix = 0;
            _extra = null;
            _baseValue = null;
        }

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
            {
                _hash?.Dispose();

                if (_baseValue != null)
                {
                    Array.Clear(_baseValue, 0, _baseValue.Length);
                }

                if (_extra != null)
                {
                    Array.Clear(_extra, 0, _extra.Length);
                }

                if (_password != null)
                {
                    Array.Clear(_password, 0, _password.Length);
                }

                if (_salt != null)
                {
                    Array.Clear(_salt, 0, _salt.Length);
                }
            }
        }

        private byte[] ComputeBaseValue()
        {
            Debug.Assert(_hash != null);
            _hash.Initialize();
            _hash.TransformBlock(_password, 0, _password.Length, _password, 0);

            if (_salt != null)
            {
                _hash.TransformBlock(_salt, 0, _salt.Length, _salt, 0);
            }

            _hash.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            _baseValue = _hash.Hash;
            _hash.Initialize();

            for (int i = 1; i < (_iterations - 1); i++)
            {
                _hash.ComputeHash(_baseValue!);
                _baseValue = _hash.Hash;
            }

            return _baseValue!;
        }

        private byte[] ComputeBytes(int cb)
        {
            int cbHash;
            int ib = 0;
            byte[] rgb;

            _hash!.Initialize();
            cbHash = _hash.HashSize / 8;
            rgb = new byte[((cb + cbHash - 1) / cbHash) * cbHash];

            using (CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write))
            {
                HashPrefix(cs);
                cs.Write(_baseValue!, 0, _baseValue!.Length);
                cs.Close();
            }

            Buffer.BlockCopy(_hash.Hash!, 0, rgb, ib, cbHash);
            ib += cbHash;

            while (cb > ib)
            {
                _hash.Initialize();
                using (CryptoStream cs = new CryptoStream(Stream.Null, _hash, CryptoStreamMode.Write))
                {
                    HashPrefix(cs);
                    cs.Write(_baseValue, 0, _baseValue.Length);
                    cs.Close();
                }

                Buffer.BlockCopy(_hash.Hash!, 0, rgb, ib, cbHash);
                ib += cbHash;
            }

            return rgb;
        }

        private void HashPrefix(CryptoStream cs)
        {
            if (_prefix > 999)
                throw new CryptographicException(SR.Cryptography_PasswordDerivedBytes_TooManyBytes);

            int cb = 0;
            byte[] rgb = { (byte)'0', (byte)'0', (byte)'0' };

            if (_prefix >= 100)
            {
                rgb[0] += (byte)(_prefix / 100);
                cb += 1;
            }

            if (_prefix >= 10)
            {
                rgb[cb] += (byte)((_prefix % 100) / 10);
                cb += 1;
            }

            if (_prefix > 0)
            {
                rgb[cb] += (byte)(_prefix % 10);
                cb += 1;
                cs.Write(rgb, 0, cb);
            }

            _prefix += 1;
        }
    }
}
