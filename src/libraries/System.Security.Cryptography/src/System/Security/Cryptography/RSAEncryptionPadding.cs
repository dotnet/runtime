// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Specifies the padding mode and parameters to use with RSA encryption or decryption operations.
    /// </summary>
    public sealed class RSAEncryptionPadding : IEquatable<RSAEncryptionPadding>
    {
        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Pkcs1"/> mode.
        /// </summary>
        public static RSAEncryptionPadding Pkcs1 { get; } = new RSAEncryptionPadding(RSAEncryptionPaddingMode.Pkcs1, default);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA1 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA1 { get; } = CreateOaep(HashAlgorithmName.SHA1);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA256 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA256 { get; } = CreateOaep(HashAlgorithmName.SHA256);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA384 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA384 { get; } = CreateOaep(HashAlgorithmName.SHA384);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA512 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA512 { get; } = CreateOaep(HashAlgorithmName.SHA512);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA3-256 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA3_256 { get; } = CreateOaep(HashAlgorithmName.SHA3_256);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA3-384 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA3_384 { get; } = CreateOaep(HashAlgorithmName.SHA3_384);

        /// <summary>
        /// <see cref="RSAEncryptionPaddingMode.Oaep"/> mode with SHA3-512 hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding OaepSHA3_512 { get; } = CreateOaep(HashAlgorithmName.SHA3_512);

        private readonly RSAEncryptionPaddingMode _mode;
        private readonly HashAlgorithmName _oaepHashAlgorithm;

        private RSAEncryptionPadding(RSAEncryptionPaddingMode mode, HashAlgorithmName oaepHashAlgorithm)
        {
            _mode = mode;
            _oaepHashAlgorithm = oaepHashAlgorithm;
        }

        /// <summary>
        /// Creates a new instance representing <see cref="RSAEncryptionPaddingMode.Oaep"/>
        /// with the given hash algorithm.
        /// </summary>
        public static RSAEncryptionPadding CreateOaep(HashAlgorithmName hashAlgorithm)
        {
            ArgumentException.ThrowIfNullOrEmpty(hashAlgorithm.Name, nameof(hashAlgorithm));

            return new RSAEncryptionPadding(RSAEncryptionPaddingMode.Oaep, hashAlgorithm);
        }

        /// <summary>
        /// Gets the padding mode to use.
        /// </summary>
        public RSAEncryptionPaddingMode Mode
        {
            get { return _mode; }
        }

        /// <summary>
        /// Gets the padding mode to use in conjunction with <see cref="RSAEncryptionPaddingMode.Oaep"/>.
        /// </summary>
        /// <remarks>
        /// If <see cref="Mode"/> is not <see cref="RSAEncryptionPaddingMode.Oaep"/>, then <see cref="HashAlgorithmName.Name" /> will be null.
        /// </remarks>
        public HashAlgorithmName OaepHashAlgorithm
        {
            get { return _oaepHashAlgorithm; }
        }

        public override int GetHashCode()
        {
            return CombineHashCodes(_mode.GetHashCode(), _oaepHashAlgorithm.GetHashCode());
        }

        // Same as non-public Tuple.CombineHashCodes
        private static int CombineHashCodes(int h1, int h2)
        {
            return (((h1 << 5) + h1) ^ h2);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as RSAEncryptionPadding);
        }

        public bool Equals([NotNullWhen(true)] RSAEncryptionPadding? other)
        {
            return other is not null
                && _mode == other._mode
                && _oaepHashAlgorithm == other._oaepHashAlgorithm;
        }

        public static bool operator ==(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(RSAEncryptionPadding? left, RSAEncryptionPadding? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return _mode.ToString() + _oaepHashAlgorithm.Name;
        }
    }
}
