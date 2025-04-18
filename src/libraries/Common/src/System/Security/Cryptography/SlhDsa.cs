// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Formats.Asn1;
using System.Runtime.CompilerServices;
using System.Security.Cryptography.Asn1;
using Internal.Cryptography;

// The type being internal is making unused parameter warnings fire for
// not-implemented methods. Suppress those warnings.
#pragma warning disable IDE0060

namespace System.Security.Cryptography
{
    /// <summary>
    ///   Represents an SLH-DSA key.
    /// </summary>
    /// <remarks>
    ///   Developers are encouraged to program against the <c>SlhDsa</c> base class,
    ///   rather than any specific derived class.
    ///   The derived classes are intended for interop with the underlying system
    ///   cryptographic libraries.
    /// </remarks>
    [Experimental(Experimentals.PostQuantumCryptographyDiagId)]
    public abstract partial class SlhDsa : IDisposable
#if DESIGNTIMEINTERFACES
#pragma warning disable SA1001
        , IImportExportShape<SlhDsa>
#pragma warning restore SA1001
#endif
    {
        private static readonly string[] s_knownOids =
        [
            Oids.SlhDsaSha2_128s,
            Oids.SlhDsaShake128s,
            Oids.SlhDsaSha2_128f,
            Oids.SlhDsaShake128f,
            Oids.SlhDsaSha2_192s,
            Oids.SlhDsaShake192s,
            Oids.SlhDsaSha2_192f,
            Oids.SlhDsaShake192f,
            Oids.SlhDsaSha2_256s,
            Oids.SlhDsaShake256s,
            Oids.SlhDsaSha2_256f,
            Oids.SlhDsaShake256f,
        ];

        private const int MaxContextLength = 255;

        private bool _disposed;

        /// <summary>
        ///   Initializes a new instance of the <see cref="SlhDsa" /> class.
        /// </summary>
        /// <param name="algorithm">
        ///   The specific SLH-DSA algorithm for this key.
        /// </param>
        protected SlhDsa(SlhDsaAlgorithm algorithm)
        {
            Algorithm = algorithm;
        }

        /// <summary>
        ///   Throws <see cref="ObjectDisposedException" /> if the current instance is disposed.
        /// </summary>
        protected void ThrowIfDisposed()
        {
#if NET
            ObjectDisposedException.ThrowIf(_disposed, typeof(SlhDsa));
#else
            if (_disposed)
            {
                throw new ObjectDisposedException(typeof(SlhDsa).FullName);
            }
#endif
        }

        /// <summary>
        ///   Gets a value indicating whether the current platform supports SLH-DSA.
        /// </summary>
        /// <value>
        ///   <see langword="true" /> if the current platform supports SLH-DSA; otherwise, <see langword="false" />.
        /// </value>
        public static bool IsSupported { get; } = SlhDsaImplementation.SupportsAny();

        /// <summary>
        ///   Gets the specific SLH-DSA algorithm for this key.
        /// </summary>
        /// <value>
        ///   The specific SLH-DSA algorithm for this key.
        /// </value>
        public SlhDsaAlgorithm Algorithm { get; }

        /// <summary>
        ///   Releases all resources used by the <see cref="SlhDsa"/> class.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Dispose(true);
                GC.SuppressFinalize(this);
            }
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
            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            int signatureSizeInBytes = Algorithm.SignatureSizeInBytes;

            if (destination.Length < signatureSizeInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            ThrowIfDisposed();

            SignDataCore(data, context, destination.Slice(0, signatureSizeInBytes));
            return signatureSizeInBytes;
        }

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
            if (context.Length > MaxContextLength)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(context),
                    context.Length,
                    SR.Argument_SignatureContextTooLong255);
            }

            ThrowIfDisposed();

            if (signature.Length != Algorithm.SignatureSizeInBytes)
            {
                return false;
            }

            return VerifyDataCore(data, context, signature);
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format.
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
        ///   Attempts to export the public-key portion of the current key in the X.509 SubjectPublicKeyInfo format
        ///   into the provided buffer.
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
        ///   Exports the public-key portion of the current key in a PEM-encoded representation of
        ///   the X.509 SubjectPublicKeyInfo format.
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
            return GetEncodedPemString(writer, PemLabels.SpkiPublicKey);
        }

        /// <summary>
        ///   Export the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        public byte[] ExportPkcs8PrivateKey()
        {
            ThrowIfDisposed();

            return ExportPkcs8PrivateKeyCore();
        }

        /// <summary>
        ///   Export the current key in the PKCS#8 PrivateKeyInfo format.
        /// </summary>
        /// <returns>
        ///   A byte array containing the PKCS#8 PrivateKeyInfo representation of the this key.
        /// </returns>
        /// <exception cref="CryptographicException">
        ///   An error occurred while exporting the key.
        /// </exception>
        protected virtual byte[] ExportPkcs8PrivateKeyCore()
        {
            return ExportPkcs8PrivateKeyCallback(static pkcs8 => pkcs8.ToArray());
        }

        private TResult ExportPkcs8PrivateKeyCallback<TResult>(ExportPkcs8PrivateKeyFunc<TResult> func)
        {
            // A PKCS#8 SLH-DSA-*-256s/f private key has an ASN.1 overhead of 22 bytes, assuming no attributes.
            // Make it an even 32 and that should give a good starting point for a buffer size.
            int size = Algorithm.SecretKeySizeInBytes + 32;
            byte[] buffer = ArrayPool<byte>.Shared.Rent(size); // Released to callers, do not use CryptoPool.
            int written;

            while (!TryExportPkcs8PrivateKeyCore(buffer, out written))
            {
                ClearAndReturnToPool(buffer, written);
                size = checked(size * 2);
                buffer = ArrayPool<byte>.Shared.Rent(size);
            }

            if (written > buffer.Length)
            {
                // We got a nonsense value written back. Clear the buffer, but don't put it back in the pool.
                CryptographicOperations.ZeroMemory(buffer);
                throw new CryptographicException();
            }

            TResult result = func(buffer.AsSpan(0, written));
            ClearAndReturnToPool(buffer, written);
            return result;

            static void ClearAndReturnToPool(byte[] buffer, int clearSize)
            {
                CryptographicOperations.ZeroMemory(buffer.AsSpan(0, clearSize));
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        /// <summary>
        ///   Attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        ///   into the provided buffer.
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

            // An SLH-DSA-SHA2-128s private key export with no attributes is 84 bytes. A buffer smaller than that cannot hold a
            // PKCS#8 encoded key. If we happen to get a buffer smaller than that, it won't export.
            const int MinimumPossiblePkcs8SlhDsaKey = 84;

            if (destination.Length < MinimumPossiblePkcs8SlhDsaKey)
            {
                bytesWritten = 0;
                return false;
            }

            return TryExportPkcs8PrivateKeyCore(destination, out bytesWritten);
        }

        /// <summary>
        ///   When overridden in a derived class, attempts to export the current key in the PKCS#8 PrivateKeyInfo format
        ///   into the provided buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the PKCS#8 PrivateKeyInfo value.
        /// </param>
        /// <param name="bytesWritten">
        ///   When this method returns, contains the number of bytes written to the <paramref name="destination"/> buffer.
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
        protected virtual bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            // TODO: Design decision: The PKCS#8 can be generated by getting the key and encoding it in the PKCS#8 format.
            // Alternatively, if the underlying library can export the PKCS#8 format directly, they can implement
            // that by overriding this method instead.

            AlgorithmIdentifierAsn algorithmIdentifier = new()
            {
                Algorithm = Algorithm.Oid,
                Parameters = default(ReadOnlyMemory<byte>?),
            };

            // Secret key size for SLH-DSA is at most 128 bytes so we can stack allocate it.
            Debug.Assert(Algorithm.SecretKeySizeInBytes is <= 128 and >= 0);
            Span<byte> buffer = stackalloc byte[Algorithm.SecretKeySizeInBytes];

            try
            {
                int secretKeyBytesWritten = ExportSlhDsaSecretKey(buffer);
                Debug.Assert(secretKeyBytesWritten == Algorithm.SecretKeySizeInBytes);

                AsnWriter algorithmWriter = new(AsnEncodingRules.DER);
                algorithmIdentifier.Encode(algorithmWriter);
                AsnWriter privateKeyWriter = new(AsnEncodingRules.DER);
                privateKeyWriter.WriteOctetString(buffer);

                AsnWriter pkcs8Writer = KeyFormatHelper.WritePkcs8(algorithmWriter, privateKeyWriter, wrapPrivateKeyInOctetString: false);

                return pkcs8Writer.TryEncode(destination, out bytesWritten);
            }
            catch (ArgumentException ae)
            {
                // TODO is this the right exception message?
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, ae);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(buffer);
            }
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 PrivateKeyInfo format.
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
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a char-based password.
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
            ArgumentNullException.ThrowIfNull(pbeParameters);
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
        ///   Exports the current key in the PKCS#8 EncryptedPrivateKeyInfo format with a byte-based password.
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
            ArgumentNullException.ThrowIfNull(pbeParameters);
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
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a char-based password.
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
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, password, ReadOnlySpan<byte>.Empty);
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
        ///   Attempts to export the current key in the PKCS#8 EncryptedPrivateKeyInfo format into a provided buffer,
        ///   using a byte-based password.
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
        ///   <see langword="true" /> if <paramref name="destination"/> was large enough to hold the result;
        ///   otherwise, <see langword="false" />.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        ///    <paramref name="pbeParameters"/> is <see langword="null"/>.
        /// </exception>
        /// <exception cref="ObjectDisposedException">
        ///   This instance has been disposed.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///   <para>This instance only represents a public key.</para>
        ///   <para>-or-</para>
        ///   <para>The private key is not exportable.</para>
        ///   <para>-or-</para>
        ///   <para>An error occurred while exporting the key.</para>
        ///   <para>-or-</para>
        ///   <para><paramref name="pbeParameters"/> does not represent a valid password-based encryption algorithm.</para>
        /// </exception>
        public bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);
            PasswordBasedEncryption.ValidatePbeParameters(pbeParameters, ReadOnlySpan<char>.Empty, passwordBytes);
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
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo representation of this key,
        ///   using a char-based password.
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
            ArgumentNullException.ThrowIfNull(pbeParameters);
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(password, pbeParameters);

            try
            {
                return GetEncodedPemString(writer, PemLabels.EncryptedPkcs8PrivateKey);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Exports the current key in a PEM-encoded representation of the PKCS#8 EncryptedPrivateKeyInfo representation of this key,
        ///   using a byte-based password.
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
            ArgumentNullException.ThrowIfNull(pbeParameters);
            ThrowIfDisposed();

            // TODO: Validation on pbeParameters.

            AsnWriter writer = ExportEncryptedPkcs8PrivateKeyCore(passwordBytes, pbeParameters);

            try
            {
                return GetEncodedPemString(writer, PemLabels.EncryptedPkcs8PrivateKey);
            }
            finally
            {
                writer.Reset();
            }
        }

        /// <summary>
        ///   Exports the public-key portion of the current key in the FIPS 205 public key format.
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
        public int ExportSlhDsaPublicKey(Span<byte> destination)
        {
            int publicKeySizeInBytes = Algorithm.PublicKeySizeInBytes;

            if (destination.Length < publicKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            ThrowIfDisposed();

            ExportSlhDsaPublicKeyCore(destination.Slice(0, publicKeySizeInBytes));
            return publicKeySizeInBytes;
        }

        /// <summary>
        ///   Exports the current key in the FIPS 205 secret key format.
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
        public int ExportSlhDsaSecretKey(Span<byte> destination)
        {
            int secretKeySizeInBytes = Algorithm.SecretKeySizeInBytes;

            if (destination.Length < secretKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            }

            ThrowIfDisposed();

            ExportSlhDsaSecretKeyCore(destination.Slice(0, secretKeySizeInBytes));
            return secretKeySizeInBytes;
        }

        /// <summary>
        ///   Generates a new SLH-DSA key for the specified algorithm.
        /// </summary>
        /// <returns>
        ///   The generated object.
        /// </returns>
        public static SlhDsa GenerateKey(SlhDsaAlgorithm algorithm)
        {
            ArgumentNullException.ThrowIfNull(algorithm);
            ThrowIfNotSupported();

            return SlhDsaImplementation.GenerateKeyCore(algorithm);
        }

        /// <summary>
        ///   Imports an SLH-DSA public key from an X.509 SubjectPublicKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of an X.509 SubjectPublicKeyInfo structure in the ASN.1-DER encoding.
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
        public static SlhDsa ImportSubjectPublicKeyInfo(ReadOnlySpan<byte> source)
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

                        SlhDsaAlgorithm algorithm = SlhDsaAlgorithm.GetAlgorithmFromOid(spki.Algorithm.Algorithm);

                        if (spki.Algorithm.Parameters.HasValue)
                        {
                            AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                            spki.Algorithm.Encode(writer);
                            throw Helpers.CreateAlgorithmUnknownException(writer);
                        }

                        return SlhDsaImplementation.ImportPublicKey(algorithm, spki.SubjectPublicKey.Span);
                    }
                }
            }
        }

        /// <summary>
        ///   Imports an SLH-DSA private key from a PKCS#8 PrivateKeyInfo structure.
        /// </summary>
        /// <param name="source">
        ///   The bytes of a PKCS#8 PrivateKeyInfo structure in the ASN.1-DER encoding.
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
        public static SlhDsa ImportPkcs8PrivateKey(ReadOnlySpan<byte> source)
        {
            ThrowIfTrailingData(source);
            ThrowIfNotSupported();

            // TODO Should this be moved into SlhDsaImplementation? If OpenSSL or Windows can
            // import a key directly from PKCS#8, then we can let them do so instead of us unwrapping
            // the key and passing it to them.
            KeyFormatHelper.ReadPkcs8(
                s_knownOids,
                source,
                (ReadOnlyMemory<byte> key, in AlgorithmIdentifierAsn algId, out SlhDsa ret) =>
                {
                    SlhDsaAlgorithm info = SlhDsaAlgorithm.GetAlgorithmFromOid(algId.Algorithm);
                    try
                    {
                        ret = ImportSlhDsaSecretKey(info, key.Span);
                    }
                    catch (ArgumentException ae)
                    {
                        throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding, ae);
                    }
                },
                out int read,
                out SlhDsa kem);
            Debug.Assert(read == source.Length);
            return kem;
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
        public static SlhDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<byte> passwordBytes, ReadOnlySpan<byte> source)
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
        public static SlhDsa ImportEncryptedPkcs8PrivateKey(ReadOnlySpan<char> password, ReadOnlySpan<byte> source)
        {
            ThrowIfNotSupported();

            return KeyFormatHelper.DecryptPkcs8(
                password,
                source,
                ImportPkcs8PrivateKey,
                out _);
        }

        /// <summary>
        ///   Imports an SLH-DSA key from an RFC 7468 PEM-encoded string.
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
        public static SlhDsa ImportFromPem(ReadOnlySpan<char> source)
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
        ///   The password to use when decrypting the key material.
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
        public static SlhDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<char> password)
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
        ///   The password to use when decrypting the key material.
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
        public static SlhDsa ImportFromEncryptedPem(ReadOnlySpan<char> source, ReadOnlySpan<byte> passwordBytes)
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
        public static SlhDsa ImportSlhDsaPublicKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.PublicKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_PublicKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();

            return SlhDsaImplementation.ImportPublicKey(algorithm, source);
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
        public static SlhDsa ImportSlhDsaSecretKey(SlhDsaAlgorithm algorithm, ReadOnlySpan<byte> source)
        {
            ArgumentNullException.ThrowIfNull(algorithm);

            if (source.Length != algorithm.SecretKeySizeInBytes)
            {
                throw new ArgumentException(SR.Argument_SecretKeyWrongSizeForAlgorithm, nameof(source));
            }

            ThrowIfNotSupported();

            return SlhDsaImplementation.ImportSecretKey(algorithm, source);
        }

        /// <summary>
        ///   Called by the <c>Dispose()</c> and <c>Finalize()</c> methods to release the managed and unmanaged
        ///   resources used by the current instance of the <see cref="SlhDsa"/> class.
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
        protected abstract void ExportSlhDsaPublicKeyCore(Span<byte> destination);

        /// <summary>
        ///   When overridden in a derived class, exports the FIPS 205 secret key to the specified buffer.
        /// </summary>
        /// <param name="destination">
        ///   The buffer to receive the secret key.
        /// </param>
        protected abstract void ExportSlhDsaSecretKeyCore(Span<byte> destination);

        private AsnWriter ExportSubjectPublicKeyInfoCore()
        {
            byte[] rented = CryptoPool.Rent(Algorithm.PublicKeySizeInBytes);

            try
            {
                Span<byte> keySpan = rented.AsSpan(0, Algorithm.PublicKeySizeInBytes);
                ExportSlhDsaPublicKey(keySpan);

                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);

                using (writer.PushSequence())
                {
                    using (writer.PushSequence())
                    {
                        writer.WriteObjectIdentifier(Algorithm.Oid);
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
            // TODO: Determine a more appropriate maximum size once the format is actually known.
            int size = Algorithm.SecretKeySizeInBytes * 2;
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
            // TODO: Determine a more appropriate maximum size once the format is actually known.
            int initialSize = Algorithm.SecretKeySizeInBytes * 2;
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

        private static SlhDsaAlgorithm GetAlgorithmIdentifier(ref readonly AlgorithmIdentifierAsn identifier)
        {
            SlhDsaAlgorithm algorithm = SlhDsaAlgorithm.GetAlgorithmFromOid(identifier.Algorithm) ??
                throw new CryptographicException(
                    SR.Format(SR.Cryptography_UnknownAlgorithmIdentifier, identifier.Algorithm));

            if (identifier.Parameters.HasValue)
            {
                AsnWriter writer = new AsnWriter(AsnEncodingRules.DER);
                identifier.Encode(writer);
                throw Helpers.CreateAlgorithmUnknownException(writer);
            }

            return algorithm;
        }

        private static void ThrowIfNotSupported()
        {
            if (!IsSupported)
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_AlgorithmNotSupported, nameof(SlhDsa)));
            }
        }


        private static void ThrowIfTrailingData(ReadOnlySpan<byte> data)
        {
            AsnDecoder.ReadEncodedValue(data, AsnEncodingRules.BER, out _, out _, out int bytesRead);

            if (bytesRead != data.Length)
            {
                throw new CryptographicException(SR.Cryptography_Der_Invalid_Encoding);
            }
        }

        internal static string GetEncodedPemString(AsnWriter writer, string label)
        {
#if NET10_0_OR_GREATER
            return writer.Encode(label, static (l, span) => PemEncoding.WriteString(l, span));
#else
            throw new NotImplementedException();
#endif
        }

        private delegate TResult ExportPkcs8PrivateKeyFunc<TResult>(ReadOnlySpan<byte> pkcs8);
    }
}
