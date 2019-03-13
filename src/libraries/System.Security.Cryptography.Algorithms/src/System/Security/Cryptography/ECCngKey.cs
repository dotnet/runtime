// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using Internal.NativeCrypto;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECCngKey
    {
        private SafeNCryptKeyHandle _keyHandle;
        private int _lastKeySize;
        private string _lastAlgorithm;
        private readonly string _algorithmGroup;

        internal ECCngKey(string algorithmGroup)
        {
            Debug.Assert(
                algorithmGroup == BCryptNative.AlgorithmName.ECDH ||
                algorithmGroup == BCryptNative.AlgorithmName.ECDsa);

            _algorithmGroup = algorithmGroup;
        }

        internal int KeySize { get; private set; }

        internal string GetCurveName(int callerKeySizeProperty, out string oidValue)
        {
            // Ensure key\handle is created
            using (SafeNCryptKeyHandle keyHandle = GetDuplicatedKeyHandle(callerKeySizeProperty))
            {
                string algorithm = _lastAlgorithm;

                if (ECCng.IsECNamedCurve(algorithm))
                {
                    oidValue = null;
                    return CngKeyLite.GetCurveName(keyHandle);
                }

                // Use hard-coded values (for use with pre-Win10 APIs)
                return ECCng.SpecialNistAlgorithmToCurveName(algorithm, out oidValue);
            }
        }

        internal SafeNCryptKeyHandle GetDuplicatedKeyHandle(int callerKeySizeProperty)
        {
            if (ECCng.IsECNamedCurve(_lastAlgorithm))
            {
                // Curve was previously created, so use that
                return new DuplicateSafeNCryptKeyHandle(_keyHandle);
            }
            else
            {
                if (_lastKeySize != callerKeySizeProperty)
                {
                    // Map the current key size to a CNG algorithm name
                    string algorithm;

                    bool isEcdsa = _algorithmGroup == BCryptNative.AlgorithmName.ECDsa;

                    switch (callerKeySizeProperty)
                    {
                        case 256:
                            algorithm = isEcdsa
                                ? BCryptNative.AlgorithmName.ECDsaP256
                                : BCryptNative.AlgorithmName.ECDHP256;
                            break;
                        case 384:
                            algorithm = isEcdsa
                                ? BCryptNative.AlgorithmName.ECDsaP384
                                : BCryptNative.AlgorithmName.ECDHP384;
                            break;
                        case 521:
                            algorithm = isEcdsa
                                ? BCryptNative.AlgorithmName.ECDsaP521
                                : BCryptNative.AlgorithmName.ECDHP521;
                            break;
                        default:
                            Debug.Fail("Should not have invalid key size");
                            throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                    }

                    if (_keyHandle != null)
                    {
                        DisposeKey();
                    }

                    _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, callerKeySizeProperty);
                    _lastKeySize = callerKeySizeProperty;
                    _lastAlgorithm = algorithm;
                    KeySize = callerKeySizeProperty;
                }

                return new DuplicateSafeNCryptKeyHandle(_keyHandle);
            }
        }

        internal void GenerateKey(ECCurve curve)
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
                {
                    throw new PlatformNotSupportedException(
                        SR.Format(SR.Cryptography_InvalidCurveOid, curve.Oid.Value));
                }

                // Map curve name to algorithm to support pre-Win10 curves
                if (_algorithmGroup == BCryptNative.AlgorithmName.ECDsa)
                {
                    algorithm = ECCng.EcdsaCurveNameToAlgorithm(curve.Oid.FriendlyName);
                }
                else
                {
                    Debug.Assert(_algorithmGroup == BCryptNative.AlgorithmName.ECDH);
                    algorithm = ECCng.EcdhCurveNameToAlgorithm(curve.Oid.FriendlyName);
                }

                if (ECCng.IsECNamedCurve(algorithm))
                {
                    try
                    {
                        _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, curve.Oid.FriendlyName);
                        keySize = CngKeyLite.GetKeyLength(_keyHandle);
                    }
                    catch (CryptographicException e)
                    {
                        // Map to PlatformNotSupportedException if appropriate
                        Interop.NCrypt.ErrorCode errorCode = (Interop.NCrypt.ErrorCode)e.HResult;

                        if (curve.IsNamed && errorCode == Interop.NCrypt.ErrorCode.NTE_INVALID_PARAMETER ||
                            errorCode == Interop.NCrypt.ErrorCode.NTE_NOT_SUPPORTED)
                        {
                            throw new PlatformNotSupportedException(
                                SR.Format(SR.Cryptography_CurveNotSupported, curve.Oid.FriendlyName), e);
                        }

                        throw;
                    }
                }
                else
                {
                    // Get the proper KeySize from algorithm name
                    switch (algorithm)
                    {
                        case BCryptNative.AlgorithmName.ECDsaP256:
                        case BCryptNative.AlgorithmName.ECDHP256:
                            keySize = 256;
                            break;
                        case BCryptNative.AlgorithmName.ECDsaP384:
                        case BCryptNative.AlgorithmName.ECDHP384:
                            keySize = 384;
                            break;
                        case BCryptNative.AlgorithmName.ECDsaP521:
                        case BCryptNative.AlgorithmName.ECDHP521:
                            keySize = 521;
                            break;
                        default:
                            Debug.Fail($"Unknown algorithm {algorithm}");
                            throw new ArgumentException(SR.Cryptography_InvalidKeySize);
                    }

                    _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, keySize);
                }
            }
            else if (curve.IsExplicit)
            {
                algorithm = _algorithmGroup;
                _keyHandle = CngKeyLite.GenerateNewExportableKey(algorithm, ref curve);
                keySize = CngKeyLite.GetKeyLength(_keyHandle);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }

            _lastAlgorithm = algorithm;
            _lastKeySize = keySize;
            KeySize = keySize;
        }

        internal void DisposeKey()
        {
            if (_keyHandle != null)
            {
                _keyHandle.Dispose();
                _keyHandle = null;
            }

            _lastAlgorithm = null;
            _lastKeySize = 0;
        }

        internal void SetHandle(SafeNCryptKeyHandle keyHandle, string algorithmName)
        {
            _keyHandle?.Dispose();
            _keyHandle = keyHandle;
            _lastAlgorithm = algorithmName;

            KeySize = CngKeyLite.GetKeyLength(keyHandle);
            _lastKeySize = KeySize;
        }
    }
}
