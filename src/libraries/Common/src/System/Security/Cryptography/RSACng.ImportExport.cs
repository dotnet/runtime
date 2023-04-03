// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
    public sealed partial class RSACng : RSA
    {
        /// <summary>
        ///   <para>
        ///     ImportParameters will replace the existing key that RSACng is working with by creating a
        ///     new CngKey for the parameters structure. If the parameters structure contains only an
        ///     exponent and modulus, then only a public key will be imported. If the parameters also
        ///     contain P and Q values, then a full key pair will be imported.
        ///   </para>
        /// </summary>
        /// <exception cref="ArgumentException">
        ///     if <paramref name="parameters" /> contains neither an exponent nor a modulus.
        /// </exception>
        /// <exception cref="CryptographicException">
        ///     if <paramref name="parameters" /> is not a valid RSA key or if <paramref name="parameters"
        ///     /> is a full key pair and the default KSP is used.
        /// </exception>
        public override unsafe void ImportParameters(RSAParameters parameters)
        {
            ArraySegment<byte> keyBlob = parameters.ToBCryptBlob();

            try
            {
                ImportKeyBlob(keyBlob, parameters.D != null);
            }
            finally
            {
                CryptoPool.Return(keyBlob);
            }
        }

        public override void ImportPkcs8PrivateKey(ReadOnlySpan<byte> source, out int bytesRead)
        {
            ThrowIfDisposed();

            CngPkcs8.Pkcs8Response response = CngPkcs8.ImportPkcs8PrivateKey(source, out int localRead);

            ProcessPkcs8Response(response);
            bytesRead = localRead;
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();

            CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(
                passwordBytes,
                source,
                out int localRead);

            ProcessPkcs8Response(response);
            bytesRead = localRead;
        }

        public override void ImportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            ReadOnlySpan<byte> source,
            out int bytesRead)
        {
            ThrowIfDisposed();

            CngPkcs8.Pkcs8Response response = CngPkcs8.ImportEncryptedPkcs8PrivateKey(
                password,
                source,
                out int localRead);

            ProcessPkcs8Response(response);
            bytesRead = localRead;
        }

        private void ProcessPkcs8Response(CngPkcs8.Pkcs8Response response)
        {
            // Wrong algorithm?
            if (response.GetAlgorithmGroup() != BCryptNative.AlgorithmName.RSA)
            {
                response.FreeKey();
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            AcceptImport(response);
        }

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                this,
                passwordBytes,
                pbeParameters);
        }

        public override byte[] ExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            if (CngPkcs8.IsPlatformScheme(pbeParameters))
            {
                return ExportEncryptedPkcs8(password, pbeParameters.IterationCount);
            }

            return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                this,
                password,
                pbeParameters);
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<byte> passwordBytes,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                ReadOnlySpan<char>.Empty,
                passwordBytes);

            return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                this,
                passwordBytes,
                pbeParameters,
                destination,
                out bytesWritten);
        }

        public override bool TryExportEncryptedPkcs8PrivateKey(
            ReadOnlySpan<char> password,
            PbeParameters pbeParameters,
            Span<byte> destination,
            out int bytesWritten)
        {
            ArgumentNullException.ThrowIfNull(pbeParameters);

            PasswordBasedEncryption.ValidatePbeParameters(
                pbeParameters,
                password,
                ReadOnlySpan<byte>.Empty);

            if (CngPkcs8.IsPlatformScheme(pbeParameters))
            {
                return TryExportEncryptedPkcs8(
                    password,
                    pbeParameters.IterationCount,
                    destination,
                    out bytesWritten);
            }

            return CngPkcs8.TryExportEncryptedPkcs8PrivateKey(
                this,
                password,
                pbeParameters,
                destination,
                out bytesWritten);
        }

        /// <summary>
        ///     Exports the key used by the RSA object into an RSAParameters object.
        /// </summary>
        public override RSAParameters ExportParameters(bool includePrivateParameters)
        {
            byte[] rsaBlob = ExportKeyBlob(includePrivateParameters);
            RSAParameters rsaParams = default;
            rsaParams.FromBCryptBlob(rsaBlob, includePrivateParameters);
            return rsaParams;
        }
    }
}
