// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.NativeCrypto;

namespace System.Security.Cryptography
{
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    internal static partial class ECDiffieHellmanImplementation
    {
#endif
        public sealed partial class ECDiffieHellmanCng : ECDiffieHellman
        {
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
                        byte[] ecExplicitBlob = ECCng.GetPrimeCurveBlob(ref ecParamsCopy, ecdh: true);
                        ImportFullKeyBlob(ecExplicitBlob, includePrivateParameters: true);
                    }
                    else
                    {
                        byte[] ecExplicitBlob = ECCng.GetPrimeCurveBlob(ref parameters, ecdh: true);
                        ImportFullKeyBlob(ecExplicitBlob, includePrivateParameters);
                    }
                }
                else if (curve.IsNamed)
                {
                    // FriendlyName is required; an attempt was already made to default it in ECCurve
                    if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                    {
                        throw new PlatformNotSupportedException(
                            SR.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
                    }

                    if (!hasPublicParameters && includePrivateParameters)
                    {
                        byte[] zero = new byte[parameters.D!.Length];
                        ECParameters ecParamsCopy = parameters;
                        ecParamsCopy.Q.X = zero;
                        ecParamsCopy.Q.Y = zero;
                        byte[] ecNamedCurveBlob = ECCng.GetNamedCurveBlob(ref ecParamsCopy, ecdh: true);
                        ImportKeyBlob(ecNamedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters: true);
                    }
                    else
                    {
                        byte[] ecNamedCurveBlob = ECCng.GetNamedCurveBlob(ref parameters, ecdh: true);
                        ImportKeyBlob(ecNamedCurveBlob, curve.Oid.FriendlyName, includePrivateParameters);
                    }
                }
                else
                {
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
                }
            }

            public override ECParameters ExportExplicitParameters(bool includePrivateParameters)
            {
                byte[] blob = ExportFullKeyBlob(includePrivateParameters);

                try
                {
                    ECParameters ecparams = default;
                    ECCng.ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters);
                    return ecparams;
                }
                finally
                {
                    Array.Clear(blob, 0, blob.Length);
                }
            }

            public override ECParameters ExportParameters(bool includePrivateParameters)
            {
                ECParameters ecparams = default;

                string? curveName = GetCurveName(out string? oidValue);
                byte[]? blob = null;

                try
                {
                    if (string.IsNullOrEmpty(curveName))
                    {
                        blob = ExportFullKeyBlob(includePrivateParameters);
                        ECCng.ExportPrimeCurveParameters(ref ecparams, blob, includePrivateParameters);
                    }
                    else
                    {
                        blob = ExportKeyBlob(includePrivateParameters);
                        ECCng.ExportNamedCurveParameters(ref ecparams, blob, includePrivateParameters);
                        ecparams.Curve = ECCurve.CreateFromOid(new Oid(oidValue, curveName));
                    }

                    return ecparams;
                }
                finally
                {
                    if (blob != null)
                    {
                        Array.Clear(blob, 0, blob.Length);
                    }
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
                if (response.GetAlgorithmGroup() != BCryptNative.AlgorithmName.ECDH)
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
        }
#if INTERNAL_ASYMMETRIC_IMPLEMENTATIONS
    }
#endif
}
