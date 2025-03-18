// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;

// The type being internal is making unused parameter warnings fire for
// not-implemented methods. Suppress those warnings.
#pragma warning disable IDE0060

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an SLH-DSA key.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <c>SLHDsa</c> base class,
    ///   rather than any specific derived class.
    ///   The derived classes are intended for interop with the underlying system
    ///   cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    internal abstract partial class SLHDsa : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<SLHDsa>
#pragma warning restore SA1001
#endif
    {
        private const int MaxContextLength = 255;

        private readonly ParameterSetInfo _parameterSetInfo;
        private bool _disposed;

        private SLHDsa(ParameterSetInfo parameterSetInfo)
        {
            Debug.Assert(parameterSetInfo is not null);

            _parameterSetInfo = parameterSetInfo;
        }

        /// <summary>
        ///   Initializes a new instance of the <see cref="SLHDsa" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        protected SLHDsa(SLHDsaAlgorithm algorithm)
            : this(ParameterSetInfo.GetParameterSetInfo(algorithm))
        {
        }

        protected void ThrowIfDisposed()
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
        }

        /// <summary>
        ///   Gets a value indicating whether the current platform supports SLH-DSA.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the current platform supports SLH-DSA; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = SLHDsaImplementation.SupportsAny();

        /// <summary>
        ///   Gets the size of the signature for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm to query.
        /// </param>
        /// <returns>
        ///   The size, in bytes, of the signature for the specified algorithm.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        /// </exception>
        public static int GetSignatureSizeInBytes(SLHDsaAlgorithm algorithm) =>
            ParameterSetInfo.GetParameterSetInfo(algorithm).SignatureSizeInBytes;

        /// <summary>
        ///   Gets the size of the SLH-DSA secret key for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm to query.
        /// </param>
        /// <returns>
        ///   The size, in bytes, of the SLH-DSA secret key for the specified algorithm.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        /// </exception>
        public static int GetSecretKeySizeInBytes(SLHDsaAlgorithm algorithm) =>
            ParameterSetInfo.GetParameterSetInfo(algorithm).SecretKeySizeInBytes;

        /// <summary>
        ///   Gets the size of the SLH-DSA public key for the specified algorithm.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm to query.
        /// </param>
        /// <returns>
        ///   The size, in bytes, of the SLH-DSA public key for the specified algorithm.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        /// </exception>
        public static int GetPublicKeySizeInBytes(SLHDsaAlgorithm algorithm) =>
            ParameterSetInfo.GetParameterSetInfo(algorithm).PublicKeySizeInBytes;

        /// <summary>
        ///   Gets the size, in bytes, of the signature for the current instance.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the signature for the current instance.
        /// </value>
        public int SignatureSizeInBytes => _parameterSetInfo.SignatureSizeInBytes;

        /// <summary>
        ///   Gets the size, in bytes, of the SLH-DSA secret key for the current instance.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the SLH-DSA secret key for the current instance.
        /// </value>
        public int SecretKeySizeInBytes => _parameterSetInfo.SecretKeySizeInBytes;

        /// <summary>
        ///   Gets the size, in bytes, of the SLH-DSA public key for the current instance.
        /// </summary>
        /// <value>
        ///   The size, in bytes, of the SLH-DSA public key for the current instance.
        /// </value>
        public int PublicKeySizeInBytes => _parameterSetInfo.PublicKeySizeInBytes;

        /// <summary>
        ///  Releases all resources used by the <see cref="SLHDsa"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            _disposed = true;
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///   Sign the specified data, writing the signature into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature.
        /// </param>
        /// <param name="context">
        ///   An optional context-specific value to limit the scope of the signature.
        ///   The default value is an empty buffer.
        /// </param>
        /// <returns>
        ///   The number of bytes written to the <paramref name="destination" /> buffer.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   The buffer in <paramref name="destination"/> is too small to hold the signature.
        /// </exception>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the data.</para>
        /// </exception>
        public int SignData(ReadOnlySpan<byte> data, Span<byte> destination, ReadOnlySpan<byte> context = default)
        {
            ThrowIfDisposed();

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            if (destination.Length < SignatureSizeInBytes)
            {
                throw new ArgumentException(nameof(destination), SR.Argument_DestinationTooShort);
            }

            SignDataCore(data, context, destination.Slice(0, SignatureSizeInBytes));
            return SignatureSizeInBytes;
        }

        // TODO: SignPreHash

        /// <summary>
        ///   Verifies that the specified signature is valid for this key and the provided data.
        /// </summary>
        /// <param name="data">
        ///   The data to verify.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <param name="context">
        ///   The context value which was provided during signing.
        ///   The default value is an empty buffer.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="ArgumentOutOfRangeException">
        ///   <paramref name="context"/> has a <see cref="ReadOnlySpan{T}.Length"/> in excess of
        ///   255 bytes.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>The instance represents only a public key.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while signing the data.</para>
        /// </exception>
        public bool VerifyData(ReadOnlySpan<byte> data, ReadOnlySpan<byte> signature, ReadOnlySpan<byte> context = default)
        {
            ThrowIfDisposed();

            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            if (signature.Length != SignatureSizeInBytes)
            {
                return false;
            }

            return VerifyDataCore(data, context, signature);
        }

        // TODO: VerifyPreHash

        /// <summary>
        ///  Exports the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the X.509 SubjectPublicKeyInfo representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportSubjectPublicKeyInfo()
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            return writer.Encode();
        }

        /// <summary>
        ///  Attempts to export the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format
        ///  into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the X.509 SubjectPublicKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportSubjectPublicKeyInfo(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            return writer.TryEncode(destination, out bytesWritten);
        }

        /// <summary>
        ///  Exports the public-key portion of the current key in a PEM-encoded representation of
        ///  the X.509 SubjectPublicKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the X.509 SubjectPublicKeyInfo
        ///   representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportSubjectPublicKeyInfoPem()
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportSubjectPublicKeyInfoCore();
            return writer.Encode(static span => PemEncoding.WriteString(PemLabels.SpkiPublicKey, span));
        }

        /// <summary>
        ///  Exports the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();

            // TODO: When defining this, provide a virtual method whose base implementation is to
            // call ExportPrivateSeed and/or ExportSecretKey, and then assemble the result,
            // but allow the derived class to override it in case they need to implement those
            // others in terms of the PKCS8 export from the underlying provider.

            throw new NotImplementedException("The PKCS#8 format is still under debate");
        }

        /// <summary>
        ///  Attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        ///  into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 PrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public bool TryExportPkcs8PrivateKey(Span<byte> destination, out int bytesWritten)
        {
            ThrowIfDisposed();

            // TODO: Once the minimum size of a PKCS#8 export is known, add an early return false.

            throw new NotImplementedException("The PKCS#8 format is still under debate");
        }

        /// <summary>
        ///  Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A string containing the PEM-encoded representation of the PKCS#8 PrivateKeyInfo
        ///   representation of the public-key portion of this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public string ExportPkcs8PrivateKeyPem()
        {
            ThrowIfDisposed();

            throw new NotImplementedException("The PKCS#8 format is still under debate");
        }

        /// <summary>
        ///  Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public byte[] ExportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return writer.Encode();
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///  using a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The password to use when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 EncryptedPrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///  using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 EncryptedPrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
        ///   This parameter is treated as uninitialized.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ThrowIfDisposed();

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return writer.TryEncode(destination, out bytesWritten);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo representation of this key,
        ///  using a char-based password.
        /// </summary>
        /// <param name="password">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return writer.Encode(static span => PemEncoding.WriteString(PemLabels.EncryptedPkcs8PrivateKey, span));
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo representation of this key,
        ///  using a byte-based password.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when encrypting the key material.
        /// </param>
        /// <param name="pbeParameters">
        ///   The password-based encryption (PBE) parameters to use when encrypting the key material.
        /// </param>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para><paramref name="pbeParameters"/> specifies a KDF that requires a char-based password.</para>
        ///   <para>-or-</para>
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        /// </exception>
        public string ExportEncryptedPkcs8PrivateKeyPem(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return writer.Encode(static span => PemEncoding.WriteString(PemLabels.EncryptedPkcs8PrivateKey, span));
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///  Exports the public-key portion of the current key in the FIPS 205 public key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is too small to hold the public key.
        /// </exception>
        public int ExportSLHDsaPublicKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < PublicKeySizeInBytes)
            {
                throw new ArgumentException(nameof(destination), SR.Argument_DestinationTooShort);
            }

            ExportSLHDsaPublicKeyCore(destination.Slice(0, PublicKeySizeInBytes));
            return PublicKeySizeInBytes;
        }

        /// <summary>
        ///  Exports the current key in the FIPS 205 secret key format.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the secret key.
        /// </param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is too small to hold the secret key.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public int ExportSLHDsaSecretKey(Span<byte> destination)
        {
            ThrowIfDisposed();

            if (destination.Length < SecretKeySizeInBytes)
            {
                throw new ArgumentException(nameof(destination), SR.Argument_DestinationTooShort);
            }

            ExportSLHDsaSecretKeyCore(destination.Slice(0, SecretKeySizeInBytes));
            return SecretKeySizeInBytes;
        }

        /// <summary>
        ///  Exports the private seed of the current key.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed.
        /// </param>
        /// <returns>
        ///   The number of bytes written to <paramref name="destination"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <paramref name="destination"/> is too small to hold the private seed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the private seed.
        /// </exception>
        public int ExportSLHDsaPrivateSeed(Span<byte> destination)
        {
            ThrowIfDisposed();

            int privateSeedSizeInBytes = _parameterSetInfo.PrivateSeedSizeInBytes;
            if (destination.Length < privateSeedSizeInBytes)
            {
                throw new ArgumentException(nameof(destination), SR.Argument_DestinationTooShort);
            }

            ExportSLHDsaPrivateSeedCore(destination.Slice(0, privateSeedSizeInBytes));
            return privateSeedSizeInBytes;
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-128s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_128sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_128s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-128s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_128sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_128s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-128f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_128fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_128f);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-128f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_128fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_128f);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-192s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_192sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_192s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-192s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_192sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_192s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-192f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_192fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_192f);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-192f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_192fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_192f);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-256s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_256sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_256s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-256s key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_256sKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_256s);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHA2-256f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaSha2_256fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaSha2_256f);
        }

        /// <summary>
        ///   Generates a new SLH-DSA-SHAKE-256f key.
        /// </summary>
        /// <returns>
        ///   The generated key.
        /// </returns>
        public static SLHDsa GenerateSLHDsaShake_256fKey()
        {
            ThrowIfNotSupported();

            return SLHDsaImplementation.GenerateKey(SLHDsaAlgorithm.SLHDsaShake_256f);
        }

        /// <summary>
        ///  Imports an SLH-DSA public key from an X.509 SubjectPublicKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///  The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-DER-encoded X.509 SubjectPublicKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The SubjectPublicKeyInfo value does not represent an SLH-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            unsafe
            {
                fixed (byte* pointer = source)
                {
                    using (PointerMemoryManager<byte> manager = new(pointer, source.Length))
                    {
                        AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);
                        SubjectPublicKeyInfoAsn.Decode(ref reader, manager.Memory, out SubjectPublicKeyInfoAsn spki);

                        ParameterSetInfo info = ParameterSetInfo.GetParameterSetInfoFromOid(spki.Algorithm.Algorithm);

                        if (spki.Algorithm.Parameters.HasValue)
                        {
                            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                            spki.Algorithm.Encode(writer);
                            ThrowAlgorithmUnknown(writer);
                            Debug.Fail("Execution should have halted in the throw-helper.");
                        }

                        return SLHDsaImplementation.ImportPublicKey(info, spki.SubjectPublicKey.Span);
                    }
                }
            }
        }

        /// <summary>
        ///  Imports an SLH-DSA private key from a PKCS#8 PrivateKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///  The bytes of a PKCS#8 PrivateKeyInfo structure in the ASN.1-DER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 PrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The PrivateKeyInfo value does not represent an SLH-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            unsafe
            {
                fixed (byte* pointer = source)
                {
                    using (PointerMemoryManager<byte> manager = new(pointer, source.Length))
                    {
                        AsnValueReader reader = new AsnValueReader(source, AsnEncodingRules.DER);
                        PrivateKeyInfoAsn.Decode(ref reader, manager.Memory, out PrivateKeyInfoAsn pki);

                        ParameterSetInfo info = ParameterSetInfo.GetParameterSetInfoFromOid(pki.PrivateKeyAlgorithm.Algorithm);

                        if (pki.PrivateKeyAlgorithm.Parameters.HasValue)
                        {
                            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                            pki.PrivateKeyAlgorithm.Encode(writer);
                            ThrowAlgorithmUnknown(writer);
                            Debug.Fail("Execution should have halted in the throw-helper.");
                        }

                        return SLHDsaImplementation.ImportPkcs8PrivateKeyValue(info, pki.PrivateKey.Span);
                    }
                }
            }
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </summary>
        /// <param name="passwordBytes">
        ///   The bytes to use as a password when decrypting the key material.
        /// </param>
        /// <param name="source">
        ///   The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The EncryptedPrivateKeyInfo indicates the Key Derivation Function (KDF) to apply is the legacy PKCS#12 KDF,
        ///     which requires <see cref="char"/>-based passwords.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The value does not represent an SLH-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                passwordBytes,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 EncryptedPrivateKeyInfo structure.
        /// </summary>
        /// <param name="password">
        ///   The password to use when decrypting the key material.
        /// </param>
        /// <param name="source">
        ///   The bytes of a PKCS#8 EncryptedPrivateKeyInfo structure in the ASN.1-BER encoding.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     The contents of <paramref name="source"/> do not represent an ASN.1-BER-encoded PKCS#8 EncryptedPrivateKeyInfo structure.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The specified password is incorrect.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The value does not represent an SLH-DSA key.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     The algorithm-specific import failed.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///  Imports an SLH-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <returns>
        ///   The imported SLH-DSA key.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded SLH-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded SLH-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        public static SLHDsa ImportFromPem(ReadOnlySpan<char> source)
        {
            ThrowIfNotSupported();

            // TODO: Match the behavior of ECDsa.ImportFromPem.
            // Double-check that the base64-decoded data has no trailing contents.
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <param name="password">
        ///  The password to use when decrypting the key material.
        /// </param>
        /// <returns>
        ///   <see langword="false" /> if the source did not contain a PEM-encoded SLH-DSA key;
        ///   <see langword="true" /> if the source contains an SLH-DSA key and it was successfully imported;
        ///   otherwise, an exception is thrown.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded SLH-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded SLH-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        public static SLHDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
        {
            ThrowIfNotSupported();

            // TODO: Match the behavior of ECDsa.ImportFromEncryptedPem.
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an RFC 7468 PEM-encoded string.
        /// </summary>
        /// <param name="source">
        ///   The text of the PEM key to import.
        /// </param>
        /// <param name="passwordBytes">
        ///  The password to use when decrypting the key material.
        /// </param>
        /// <returns>
        ///   <see langword="false" /> if the source did not contain a PEM-encoded SLH-DSA key;
        ///   <see langword="true" /> if the source contains an SLH-DSA key and it was successfully imported;
        ///   otherwise, an exception is thrown.
        /// </returns>
        /// <exception cref="ArgumentException">
        ///   <para><paramref name="source" /> contains an encrypted PEM-encoded key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains multiple PEM-encoded SLH-DSA keys.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="source" /> contains no PEM-encoded SLH-DSA keys.</para>
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while importing the key.
        /// </exception>
        public static SLHDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
        {
            ThrowIfNotSupported();

            // TODO: Match the behavior of ECDsa.ImportFromEncryptedPem.
            throw new NotImplementedException();
        }

        /// <summary>
        ///   Imports an SLH-DSA public key in the FIPS 205 public key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 205 public key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportSLHDsaPublicKey(SLHDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();
            ArgumentNullException.ThrowIfNull(algorithm);

            ParameterSetInfo info = ParameterSetInfo.GetParameterSetInfo(algorithm);

            if (source.Length != info.PublicKeySizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_KeyWrongSizeForAlgorithm);
            }

            return SLHDsaImplementation.ImportPublicKey(info, source);
        }

        /// <summary>
        ///   Imports an SLH-DSA private key in the FIPS 205 secret key format.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes of a FIPS 205 secret key.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportSLHDsaSecretKey(SLHDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();
            ArgumentNullException.ThrowIfNull(algorithm);

            ParameterSetInfo info = ParameterSetInfo.GetParameterSetInfo(algorithm);

            if (source.Length != info.SecretKeySizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_KeyWrongSizeForAlgorithm);
            }

            return SLHDsaImplementation.ImportSecretKey(info, source);
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from its private seed value.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        /// <param name="source">
        ///   The bytes the key seed.
        /// </param>
        /// <returns>
        ///   The imported key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   <para>
        ///     <paramref name="algorithm"/> is not a valid SLH-DSA algorithm identifier.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     <paramref name="source"/> is not the correct size for the specified algorithm.
        ///   </para>
        ///   <para>-or-</para>
        ///   <para>
        ///     An error occurred while importing the key.
        ///   </para>
        /// </exception>
        public static SLHDsa ImportSLHDsaPrivateSeed(SLHDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            ParameterSetInfo info = ParameterSetInfo.GetParameterSetInfo(algorithm);

            if (source.Length != info.PrivateSeedSizeInBytes)
            {
                throw new CryptographicException(SR.Cryptography_KeyWrongSizeForAlgorithm);
            }

            return SLHDsaImplementation.ImportSeed(info, source);
        }

        /// <summary>
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="SLHDsa"/> class.
        /// </summary>
        /// <param name="disposing">
        ///   <see langword="true" /> to release managed and unmanaged resources;
        ///   <see langword="false" /> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
        }

        /// <summary>
        ///   When overridden in a derived class, computes the signature of the specified data and context,
        ///   writing it into the provided buffer.
        /// </summary>
        /// <param name="data">
        ///   The data to sign.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="destination">
        ///   The buffer to receive the signature, which will always be the exactly correct size for the algorithm.
        /// </param>
        /// <exception cref="CryptographicException">
        ///   An error occurred while signing the data.
        /// </exception>
        protected abstract void SignDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, verifies the signature of the specified data and context.
        /// </summary>
        /// <param name="data">
        ///   The data to verify.
        /// </param>
        /// <param name="context">
        ///   The signature context.
        /// </param>
        /// <param name="signature">
        ///   The signature to verify.
        /// </param>
        /// <returns>
        ///   <see langword="true"/> if the signature validates the data; otherwise, <see langword="false"/>.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while signing the data.
        /// </exception>
        protected abstract bool VerifyDataCore(ReadOnlySpan<byte> data, ReadOnlySpan<byte> context, ReadOnlySpan<byte> signature);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 205 public key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the public key.
        /// </param>
        protected abstract void ExportSLHDsaPublicKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 205 secret key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the secret key.
        /// </param>
        protected abstract void ExportSLHDsaSecretKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the private seed to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the private seed.
        /// </param>
        protected abstract void ExportSLHDsaPrivateSeedCore(Span<byte> destination);

        private AsnWriter ExportSubjectPublicKeyInfoCore()
        {
            ThrowIfDisposed();

            byte[] rented = CryptoPool.Rent(_parameterSetInfo.PublicKeySizeInBytes);

            try
            {
                Span<byte> keySpan = rented.AsSpan(0, _parameterSetInfo.PublicKeySizeInBytes);
                ExportSLHDsaPublicKey(keySpan);

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                using (writer.PushSequence())
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(_parameterSetInfo.Oid);
                    }

                    writer.WriteBitString(keySpan);
                }

                return writer;
            }
            finally
            {
                // Public key doesn't need to be cleared
                CryptoPool.Return(rented, 0);
            }
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore(ReadOnlySpan<byte> passwordBytes, PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Determine a more appropriate maximum size once the format is actually known.
            int size = _parameterSetInfo.SecretKeySizeInBytes * 2;
            // The buffer is only being passed out as a span, so the derived type can't meaningfully
            // hold on to it without being malicious.
            byte[] rented = CryptoPool.Rent(size);
            int written;

            while (!TryExportPkcs8PrivateKey(rented, out written))
            {
                size = rented.Length;
                CryptoPool.Return(rented, 0);
                rented = CryptoPool.Rent(size * 2);
            }

            AsnWriter tmp = new AsnWriter(AsnEncodingRules.BER);

            try
            {
                tmp.WriteEncodedValueForCrypto(rented.AsSpan(0, written));
                return KeyFormatHelper.WriteEncryptedPkcs8(passwordBytes, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
                CryptoPool.Return(rented, written);
            }
        }

        private AsnWriter ExportEncryptedPkcs8PrivateKeyCore(ReadOnlySpan<char> password, PbeParameters pbeParameters)
        {
            ThrowIfDisposed();

            // TODO: Determine a more appropriate maximum size once the format is actually known.
            int initialSize = _parameterSetInfo.SecretKeySizeInBytes * 2;
            // The buffer is only being passed out as a span, so the derived type can't meaningfully
            // hold on to it without being malicious.
            byte[] rented = CryptoPool.Rent(initialSize);
            int written;

            while (!TryExportPkcs8PrivateKey(rented, out written))
            {
                CryptoPool.Return(rented, 0);
                rented = CryptoPool.Rent(rented.Length * 2);
            }

            AsnWriter tmp = new AsnWriter(AsnEncodingRules.BER);

            try
            {
                tmp.WriteEncodedValueForCrypto(rented.AsSpan(0, written));
                return KeyFormatHelper.WriteEncryptedPkcs8(password, tmp, pbeParameters);
            }
            finally
            {
                tmp.Reset();
                CryptoPool.Return(rented, written);
            }
        }

        internal static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SLHDsa)));
            }
        }

        [DoesNotReturn]
        private static void ThrowAlgorithmUnknown(AsnWriter encodedId)
        {
#if NET9_0_OR_GREATER
            throw encodedId.Encode(static encoded =>
                new CryptographicException(
                    SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, Convert.ToHexString(encoded))));
#else
            throw new CryptographicException(
                SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, Convert.ToHexString(encodedId.Encode())));
#endif
        }

        [DoesNotReturn]
        private static ParameterSetInfo ThrowAlgorithmUnknown(string algorithmId)
        {
            throw new CryptographicException(
                SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, algorithmId));
        }

        internal sealed class ParameterSetInfo
        {
            // TODO: If SLHDsaAlgorithm is a class, this class can be merged into it.
            // TODO: Some of the information maybe then becomes public on SLHDsaAlgorithm, rather than SLHDsa?

            internal int PrivateSeedSizeInBytes { get; }
            internal int SecretKeySizeInBytes { get; }
            internal int PublicKeySizeInBytes { get; }
            internal int SignatureSizeInBytes { get; }
            internal SLHDsaAlgorithm Algorithm { get; }
            internal string Oid { get; }

            private ParameterSetInfo(
                int n, // Also referred to as the "security parameter" in FIPS 205
                int signatureSizeInBytes,
                SLHDsaAlgorithm algorithm,
                string oid)
            {

                // The seed, secret key and public key sizes are shown to be n, 4n and 2n respectively in
                // section 9.1 "Key Generation", particularly figure 15 and 16.
                PrivateSeedSizeInBytes = n;
                SecretKeySizeInBytes = 4 * n;
                PublicKeySizeInBytes = 2 * n;
                SignatureSizeInBytes = signatureSizeInBytes;
                Algorithm = algorithm;
                Oid = oid;
            }

            // SLH-DSA parameter sets, and the sizes associated with them,
            // are defined in FIPS 205, section 11 "Parameter Sets",
            // particularly Table 2 "SLH-DSA parameter sets".

            internal static readonly ParameterSetInfo SLHDsaSha2_128s =
                new ParameterSetInfo(16, 7856, SLHDsaAlgorithm.SLHDsaSha2_128s, Oids.SLHDsaSha2_128s);

            internal static readonly ParameterSetInfo SLHDsaShake_128s =
                new ParameterSetInfo(16, 7856, SLHDsaAlgorithm.SLHDsaShake_128s, Oids.SLHDsaShake_128s);

            internal static readonly ParameterSetInfo SLHDsaSha2_128f =
                new ParameterSetInfo(16, 17088, SLHDsaAlgorithm.SLHDsaSha2_128f, Oids.SLHDsaSha2_128f);

            internal static readonly ParameterSetInfo SLHDsaShake_128f =
                new ParameterSetInfo(16, 17088, SLHDsaAlgorithm.SLHDsaShake_128f, Oids.SLHDsaShake_128f);

            internal static readonly ParameterSetInfo SLHDsaSha2_192s =
                new ParameterSetInfo(24, 15616, SLHDsaAlgorithm.SLHDsaSha2_192s, Oids.SLHDsaSha2_192s);

            internal static readonly ParameterSetInfo SLHDsaShake_192s =
                new ParameterSetInfo(24, 15616, SLHDsaAlgorithm.SLHDsaShake_192s, Oids.SLHDsaShake_192s);

            internal static readonly ParameterSetInfo SLHDsaSha2_192f =
                new ParameterSetInfo(24, 35664, SLHDsaAlgorithm.SLHDsaSha2_192f, Oids.SLHDsaSha2_192f);

            internal static readonly ParameterSetInfo SLHDsaShake_192f =
                new ParameterSetInfo(24, 35664, SLHDsaAlgorithm.SLHDsaShake_192f, Oids.SLHDsaShake_192f);

            internal static readonly ParameterSetInfo SLHDsaSha2_256s =
                new ParameterSetInfo(32, 29792, SLHDsaAlgorithm.SLHDsaSha2_256s, Oids.SLHDsaSha2_256s);

            internal static readonly ParameterSetInfo SLHDsaShake_256s =
                new ParameterSetInfo(32, 29792, SLHDsaAlgorithm.SLHDsaShake_256s, Oids.SLHDsaShake_256s);

            internal static readonly ParameterSetInfo SLHDsaSha2_256f =
                new ParameterSetInfo(32, 49856, SLHDsaAlgorithm.SLHDsaSha2_256f, Oids.SLHDsaSha2_256f);

            internal static readonly ParameterSetInfo SLHDsaShake_256f =
                new ParameterSetInfo(32, 49856, SLHDsaAlgorithm.SLHDsaShake_256f, Oids.SLHDsaShake_256f);

            internal static ParameterSetInfo GetParameterSetInfo(SLHDsaAlgorithm algorithm)
            {
                ArgumentNullException.ThrowIfNull(algorithm);

                return algorithm.Name switch
                {
                    "SLH-DSA-SHA2-128s" => SLHDsaShake_128s,
                    "SLH-DSA-SHAKE-128s" => SLHDsaShake_128s,
                    "SLH-DSA-SHA2-128f" => SLHDsaShake_128f,
                    "SLH-DSA-SHAKE-128f" => SLHDsaShake_128f,
                    "SLH-DSA-SHA2-192s" => SLHDsaShake_192s,
                    "SLH-DSA-SHAKE-192s" => SLHDsaShake_192s,
                    "SLH-DSA-SHA2-192f" => SLHDsaShake_192f,
                    "SLH-DSA-SHAKE-192f" => SLHDsaShake_192f,
                    "SLH-DSA-SHA2-256s" => SLHDsaShake_256s,
                    "SLH-DSA-SHAKE-256s" => SLHDsaShake_256s,
                    "SLH-DSA-SHA2-256f" => SLHDsaShake_256f,
                    "SLH-DSA-SHAKE-256f" => SLHDsaShake_256f,

                    _ => ThrowAlgorithmUnknown(algorithm.Name),
                };
            }

            internal static ParameterSetInfo GetParameterSetInfoFromOid(string oid)
            {
                return oid switch
                {
                    Oids.SLHDsaSha2_128s => SLHDsaSha2_128s,
                    Oids.SLHDsaShake_128s => SLHDsaShake_128s,
                    Oids.SLHDsaSha2_128f => SLHDsaSha2_128f,
                    Oids.SLHDsaShake_128f => SLHDsaShake_128f,
                    Oids.SLHDsaSha2_192s => SLHDsaSha2_192s,
                    Oids.SLHDsaShake_192s => SLHDsaShake_192s,
                    Oids.SLHDsaSha2_192f => SLHDsaSha2_192f,
                    Oids.SLHDsaShake_192f => SLHDsaShake_192f,
                    Oids.SLHDsaSha2_256s => SLHDsaSha2_256s,
                    Oids.SLHDsaShake_256s => SLHDsaShake_256s,
                    Oids.SLHDsaSha2_256f => SLHDsaSha2_256f,
                    Oids.SLHDsaShake_256f => SLHDsaShake_256f,

                    _ => ThrowAlgorithmUnknown(oid),
                };
            }
        }
    }
}
