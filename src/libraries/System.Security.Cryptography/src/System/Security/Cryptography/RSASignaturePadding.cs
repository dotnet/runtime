// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Versioning;

namespace System.Security.Cryptography
{
    /// <summary>
    /// Specifies the padding mode  and parameters to use with RSA signature creation or verification operations.
    /// </summary>
    public sealed class RSASignaturePadding : IEquatable<RSASignaturePadding>
    {
        /// <summary>
        /// Represents a constant value indicating that the salt length should match the hash length.
        /// </summary>
        /// <remarks>This value is typically used in cryptographic operations where the salt length is required to
        /// be the same as the hash length.</remarks>
        public const int PssSaltLengthIsHashLength = RsaPaddingProcessor.PssSaltLengthIsHashLength;

        /// <summary>
        /// Represents the maximum allowable length, in bytes, for a PSS (Probabilistic Signature Scheme) salt.
        /// </summary>
        /// <remarks>This constant is used to define the upper limit for the salt length in PSS-based
        /// cryptographic operations. The maximum length is determined by the hash algorithm's output size.</remarks>
        public const int PssSaltLengthMax = RsaPaddingProcessor.PssSaltLengthMax;

        /// <summary>
        /// Specifies the salt length to use for PSS padding. This property is only relevant when the <see cref="Mode"/> is <see cref="RSASignaturePaddingMode.Pss"/>.
        /// </summary>
        /// <remarks>
        /// This value must either be a positive number or one of the special constants <see cref="PssSaltLengthIsHashLength"/> or <see cref="PssSaltLengthMax"/>.
        /// </remarks>
        public int PssSaltLength { get; }

        public static RSASignaturePadding CreatePss(int saltLength)
        {
            switch (saltLength)
            {
                case PssSaltLengthIsHashLength or PssSaltLengthMax:
                case > 0:
                    return new RSASignaturePadding(saltLength);
                default:
                    throw new ArgumentOutOfRangeException(nameof(saltLength), SR.Cryptography_InvalidSaltLengthForPss);
            }
        }

        private static readonly RSASignaturePadding s_pkcs1 = new RSASignaturePadding(RSASignaturePaddingMode.Pkcs1);
        private static readonly RSASignaturePadding s_pss = CreatePss(PssSaltLengthIsHashLength);

        private readonly RSASignaturePaddingMode _mode;

        private RSASignaturePadding(RSASignaturePaddingMode mode)
        {
            _mode = mode;
        }

        private RSASignaturePadding(int pssSaltLength)
        {
            _mode = RSASignaturePaddingMode.Pss;
            PssSaltLength = pssSaltLength;
        }

        /// <summary>
        /// <see cref="RSASignaturePaddingMode.Pkcs1"/> mode.
        /// </summary>
        public static RSASignaturePadding Pkcs1
        {
            get { return s_pkcs1; }
        }

        /// <summary>
        /// <see cref="RSASignaturePaddingMode.Pss"/> mode with the number of salt bytes equal to the size of the hash.
        /// </summary>
        public static RSASignaturePadding Pss
        {
            get { return s_pss; }
        }

        /// <summary>
        /// Gets the padding mode to use.
        /// </summary>
        public RSASignaturePaddingMode Mode
        {
            get { return _mode; }
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(_mode, PssSaltLength);
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            return Equals(obj as RSASignaturePadding);
        }

        public bool Equals([NotNullWhen(true)] RSASignaturePadding? other)
        {
            return other is not null && _mode == other._mode && PssSaltLength == other.PssSaltLength;
        }

        public static bool operator ==(RSASignaturePadding? left, RSASignaturePadding? right)
        {
            if (left is null)
            {
                return right is null;
            }

            return left.Equals(right);
        }

        public static bool operator !=(RSASignaturePadding? left, RSASignaturePadding? right)
        {
            return !(left == right);
        }

        public override string ToString()
        {
            return _mode.ToString();
        }
    }
}
