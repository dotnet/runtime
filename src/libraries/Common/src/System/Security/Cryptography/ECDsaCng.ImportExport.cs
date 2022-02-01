// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDsaImplementation
    {
#endif
        public sealed partial class ECDsaCng : ECDsa
        {
            /// <summary>
            ///         ImportParameters will replace the existing key that ECDsaCng is working with by creating a
            ///         new CngKey. If the parameters contains only Q, then only a public key will be imported.
            ///         If the parameters also contains D, then a full key pair will be imported.
            ///         The parameters Curve value specifies the type of the curve to import.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///     if <paramref name="parameters" /> does not contain valid values.
            /// </exception>
            /// <exception cref="NotSupportedException">
            ///     if <paramref name="parameters" /> references a curve that cannot be imported.
            /// </exception>
            /// <exception cref="PlatformNotSupportedException">
            ///     if <paramref name="parameters" /> references a curve that is not supported by this platform.
            /// </exception>
            public override void ImportParameters(ECParameters parameters)
            {
                parameters.Validate();
                ThrowIfDisposed();

                ECCurve curve = parameters.Curve;
                bool includePrivateParameters = parameters.D != null;
                bool hasPublicParameters = parameters.Q.X != null && parameters.Q.Y != null;

                if (curve.IsPrime)
                {
                    if (!hasPublicParameters && includePrivateParameters)
                    {
                        byte[] zero = new byte[parameters.D!.Length];
                        ECParameters ecParamsCopy = parameters;
                        ecParamsCopy.Q.X = zero;
                        ecParamsCopy.Q.Y = zero;
                        byte[] ecExplicitBlob = ECCng.GetPrimeCurveBlob(ref ecParamsCopy, ecdh: false);
                        ImportFullKeyBlob(ecExplicitBlob, includePrivateParameters: true);
                    }
                    else
                    {
                        byte[] ecExplicitBlob = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: false);
                        ImportFullKeyBlob(ecExplicitBlob, includePrivateParameters);
                    }
                }
                else if (curve.IsNamed)
                {
                    // FriendlyName is required; an attempt was already made to default it in ECCurve
                    if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                        throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value!.ToString()));

                    if (!hasPublicParameters && includePrivateParameters)
                    {
                        byte[] zero = new byte[parameters.D!.Length];
                        ECParameters ecParamsCopy = parameters;
                        ecParamsCopy.Q.X = zero;
                        ecParamsCopy.Q.Y = zero;
                        byte[] ecNamedCurveBlob = ECCng.GetNamedCurveBlob(ref ecParamsCopy, ecdh: false);
                        ImportKeyBlob(ecNamedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
                    }
                    else
                    {
                        byte[] ecNamedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters, ecdh: false);
                        ImportKeyBlob(ecNamedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters);
                    }
                }
                else
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
                }
            }

            /// <summary>
            ///     Exports the key and explicit curve parameters used by the ECC object into an <see cref="ECParameters"/> object.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///     if there was an issue obtaining the curve values.
            /// </exception>
            /// <exception cref="PlatformNotSupportedException">
            ///     if explicit export is not supported by this platform. Windows 10 or higher is required.
            /// </exception>
            /// <returns>The key and explicit curve parameters used by the ECC object.</returns>
            public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
            {
                byte[] blob = ExportFullKeyBlob(includePrivateParameters);
                ECParameters ecparams = default;
                ECCng.ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters);
                return ecparams;
            }

            /// <summary>
            ///     Exports the key used by the ECC object into an <see cref="ECParameters"/> object.
            ///     If the curve has a name, the Curve property will contain named curve parameters otherwise it will contain explicit parameters.
            /// </summary>
            /// <exception cref="CryptographicException">
            ///     if there was an issue obtaining the curve values.
            /// </exception>
            /// <returns>The key and named curve parameters used by the ECC object.</returns>
            public override ECParameters ExportParameters(bool includePrivateParameters)
            {
                ECParameters ecparams = default;

                string? curveName = GetCurveName(out string? oidValue);

                if (string.IsNullOrEmpty(curveName))
                {
                    byte[] fullKeyBlob = ExportFullKeyBlob(includePrivateParameters);
                    ECCng.ExportPrimeCurveParameters(ref ecparams, fullKeyBlob, includePrivateParameters);
                }
                else
                {
                    byte[] keyBlob = ExportKeyBlob(includePrivateParameters);
                    ECCng.ExportNamedCurveParameters(ref ecparams, keyBlob, includePrivateParameters);
                    ecparams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
                }

                return ecparams;
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
                switch (response.GetAlgorithmGroup())
                {
                    // CNG ECDH and ECDSA keys can both do ECDSA.
                    case BCryptNative.AlgorithmName.ECDsa:
                    case BCryptNative.AlgorithmName.ECDH:
                        AcceptImport(response);
                        return;
                }

                response.FreeKey();
                throw new CryptographicException(SR.Cryptography_NotValidPublicOrPrivateKey);
            }

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<byte> passwordBytes,
                PbeParameters pbeParameters)
            {
                if (pbeParameters == null)
                    throw new ArgumentNullException(nameof(pbeParameters));

                return CngPkcs8.ExportEncryptedPkcs8PrivateKey(
                    this,
                    passwordBytes,
                    pbeParameters);
            }

            public override byte[] ExportEncryptedPkcs8PrivateKey(
                ReadOnlySpan<char> password,
                PbeParameters pbeParameters)
            {
                if (pbeParameters == null)
                {
                    throw new ArgumentNullException(nameof(pbeParameters));
                }

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
                if (pbeParameters == null)
                    throw new ArgumentNullException(nameof(pbeParameters));

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
                if (pbeParameters == null)
                    throw new ArgumentNullException(nameof(pbeParameters));

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
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
        }
#endif
    }
}
