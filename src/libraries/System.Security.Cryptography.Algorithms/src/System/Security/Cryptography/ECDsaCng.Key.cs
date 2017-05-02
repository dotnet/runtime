// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Security.Cryptography
{
    using Microsoft.Win32.SafeHandles;
    using System.Diagnostics;
    using static Internal.NativeCrypto.BCryptNative;
    using static Interop.NCrypt;
    internal static partial class ECDsaImplementation
    {
        public sealed partial class ECDsaCng : ECDsa
        {
            private SafeNCryptKeyHandle _keyHandle;
            private int _lastKeySize;
            private string _lastAlgorithm;

            internal string GetCurveName()
            {
                using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle()) // Ensure key\handle is created
                {
                    string algorithm = _lastAlgorithm;
                    if (IsECNamedCurve(algorithm))
                    {
                        return CngKeyLite.GetCurveName(keyHandle);
                    }

                    // Use hard-coded values (for use with pre-Win10 APIs)
                    return SpecialNistAlgorithmToCurveName(algorithm);
                }
            }

            private SafeNCryptKeyHandle GetDuplicatedKeyHandle()
            {
                if (IsECNamedCurve(_lastAlgorithm))
                {
                    // Curve was previously created, so use that
                    return new DuplicateSafeNCryptKeyHandle(_keyHandle);
                }
                else
                {
                    string algorithm = null;

                    int keySize = KeySize;
                    if (_lastKeySize != keySize)
                    {
                        // Map the current key size to a CNG algorithm name
                        switch (keySize)
                        {
                            case 256: algorithm = AlgorithmName.ECDsaP256; break;
                            case 384: algorithm = AlgorithmName.ECDsaP384; break;
                            case 521: algorithm = AlgorithmName.ECDsaP521; break;
                            default:
                                Debug.Fail("Should not have invalid key size");
                                throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                        }
                        if (_keyHandle != null)
                        {
                            DisposeKey();
                        }
                        _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, keySize);
                        _lastKeySize = keySize;
                        _lastAlgorithm = algorithm;
                        ForceSetKeySize(keySize);
                    }
                    return new DuplicateSafeNCryptKeyHandle(_keyHandle);
                }
            }

            public override void GenerateKey(ECCurve curve)
            {
                curve.Validate();

                if (_keyHandle != null)
                {
                    DisposeKey();
                }

                string algorithm = null;
                int keySize = 0;

                if (curve.IsNamed)
                {
                    if (string.IsNullOrEmpty(curve.Oid.FriendlyName))
                        throw new PlatformNotSupportedException(string.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value));

                    // Map curve name to algorithm to support pre-Win10 curves
                    algorithm = ECCng.EcdsaCurveNameToAlgorithm(curve.Oid.FriendlyName);
                    if (IsECNamedCurve(algorithm))
                    {
                        try
                        {
                            _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, curve.Oid.FriendlyName);
                            keySize = CngKeyLite.GetKeyLength(_keyHandle);
                        }
                        catch (CryptographicException e)
                        {
                            // Map to PlatformNotSupportedException if appropriate
                            ErrorCode errorCode = (ErrorCode)e.HResult;

                            if (curve.IsNamed &&
                                errorCode == ErrorCode.NTE_INVALID_PARAMETER || errorCode == ErrorCode.NTE_NOT_SUPPORTED)
                            {
                                throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curve.Oid.FriendlyName), e);
                            }
                            throw;
                        }
                    }
                    else
                    {
                        // Get the proper KeySize from algorithm name
                        if (algorithm == AlgorithmName.ECDsaP256)
                            keySize = 256;
                        else if (algorithm == AlgorithmName.ECDsaP384)
                            keySize = 384;
                        else if (algorithm == AlgorithmName.ECDsaP521)
                            keySize = 521;
                        else
                        {
                            Debug.Fail(string.Format("Unknown algorithm {0}", algorithm.ToString()));
                            throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                        }
                        _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, keySize);
                    }
                }
                else if (curve.IsExplicit)
                {
                    algorithm = AlgorithmName.ECDsa;
                    _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, ref curve);
                    keySize = CngKeyLite.GetKeyLength(_keyHandle);
                }
                else
                {
                    throw new PlatformNotSupportedException(string.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
                }

                _lastAlgorithm = algorithm;
                _lastKeySize = keySize;
                ForceSetKeySize(keySize);
            }

            private void DisposeKey()
            {
                if (_keyHandle != null)
                {
                    _keyHandle.Dispose();
                    _keyHandle = null;
                }
                _lastAlgorithm = null;
                _lastKeySize = 0;
            }
        }
    }
}
