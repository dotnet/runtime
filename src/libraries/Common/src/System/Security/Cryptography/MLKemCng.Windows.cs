// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Formats.Asn1;
using System.Security.Cryptography.Asn1;
using System.Runtime.InteropServices;
using System.Runtime.Versioning;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    public sealed partial class MLKemCng : MLKem
    {
        private const string NCRYPT_MLKEM_PARAMETER_SET_512 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_512;
        private const string NCRYPT_MLKEM_PARAMETER_SET_768 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_768;
        private const string NCRYPT_MLKEM_PARAMETER_SET_1024 = PqcBlobHelpers.BCRYPT_MLKEM_PARAMETER_SET_1024;

        internal MLKemCng(CngKey key, bool transferOwnership)
            : base(AlgorithmFromHandleNoDuplicate(key))
        {
            Debug.Assert(key is not null);
            Debug.Assert(key.AlgorithmGroup == CngAlgorithmGroup.MLKem);
            Debug.Assert(transferOwnership);

            _key = key;
        }

        [SupportedOSPlatform("windows")]
        private static partial MLKemAlgorithm AlgorithmFromHandle(CngKey key, out CngKey duplicateKey)
        {
            ArgumentNullException.ThrowIfNull(key);
            ThrowIfNotSupported();

            if (key.AlgorithmGroup != CngAlgorithmGroup.MLDsa)
            {
                throw new ArgumentException(SR.Cryptography_ArgMLKemRequiresMLKemKey, nameof(key));
            }

            MLKemAlgorithm algorithm = AlgorithmFromHandleImpl(key);

#if SYSTEM_SECURITY_CRYPTOGRAPHY
            duplicateKey = CngHelpers.Duplicate(key.HandleNoDuplicate, key.IsEphemeral);
#else
            duplicateKey = key.Duplicate();
#endif

            return algorithm;
        }

        private static MLKemAlgorithm AlgorithmFromHandleNoDuplicate(CngKey key)
        {
            if (key.AlgorithmGroup != CngAlgorithmGroup.MLKem)
            {
                throw new CryptographicException(SR.Cryptography_ArgMLDsaRequiresMLDsaKey);
            }

            Debug.Assert(key is not null);

            return AlgorithmFromHandleImpl(key);
        }

        private static MLKemAlgorithm AlgorithmFromHandleImpl(CngKey key)
        {
            string? parameterSet =
#if SYSTEM_SECURITY_CRYPTOGRAPHY
                key.HandleNoDuplicate.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#else
                key.GetPropertyAsString(KeyPropertyName.ParameterSetName, CngPropertyOptions.None);
#endif

            return parameterSet switch
            {
                NCRYPT_MLKEM_PARAMETER_SET_512 => MLKemAlgorithm.MLKem512,
                NCRYPT_MLKEM_PARAMETER_SET_768 => MLKemAlgorithm.MLKem768,
                NCRYPT_MLKEM_PARAMETER_SET_1024 => MLKemAlgorithm.MLKem1024,
                _ => throw DebugFailAndGetException(parameterSet),
            };

            static Exception DebugFailAndGetException(string? parameterSet)
            {
                Debug.Fail($"Unexpected parameter set '{parameterSet}'.");
                return new CryptographicException();
            }
        }

        public partial CngKey Key
        {
            get
            {
                ThrowIfDisposed();

                return _key;
            }
        }

        protected override void DecapsulateCore(ReadOnlySpan<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void EncapsulateCore(Span<byte> ciphertext, Span<byte> sharedSecret)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportPrivateSeedCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override void ExportEncapsulationKeyCore(Span<byte> destination)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }

        protected override bool TryExportPkcs8PrivateKeyCore(Span<byte> destination, out int bytesWritten)
        {
            Debug.Fail("Caller should have checked platform availability.");
            throw new PlatformNotSupportedException();
        }
    }
}
