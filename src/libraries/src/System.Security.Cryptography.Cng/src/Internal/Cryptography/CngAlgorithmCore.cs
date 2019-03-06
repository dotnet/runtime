// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Cryptography;
using Microsoft.Win32.SafeHandles;
using static Interop.NCrypt;

namespace Internal.Cryptography
{
    //
    // Common infrastructure for crypto algorithms that accept CngKeys.
    //
    internal struct CngAlgorithmCore
    {
        public CngAlgorithm DefaultKeyType;
        private CngKey _lazyKey;

        public static CngKey Duplicate(CngKey key)
        {
            using (SafeNCryptKeyHandle keyHandle = key.Handle)
            {
                return CngKey.Open(keyHandle, key.IsEphemeral ? CngKeyHandleOpenOptions.EphemeralKey : CngKeyHandleOpenOptions.None);
            }
        }

        public bool IsKeyGeneratedNamedCurve()
        {
            return (_lazyKey != null && _lazyKey.IsECNamedCurve());
        }

        public void DisposeKey()
        {
            if (_lazyKey != null)
            {
                _lazyKey.Dispose();
                _lazyKey = null;
            }
        }

        public CngKey GetOrGenerateKey(int keySize, CngAlgorithm algorithm)
        {
            // If our key size was changed, we need to generate a new key.
            if (_lazyKey != null)
            {
                if (_lazyKey.KeySize != keySize)
                    DisposeKey();
            }

            // If we don't have a key yet, we need to generate one now.
            if (_lazyKey == null)
            {
                CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
                {
                    ExportPolicy = CngExportPolicies.AllowPlaintextExport,
                };

                CngProperty keySizeProperty = new CngProperty(KeyPropertyName.Length, BitConverter.GetBytes(keySize), CngPropertyOptions.None);
                creationParameters.Parameters.Add(keySizeProperty);

                _lazyKey = CngKey.Create(algorithm, null, creationParameters);
            }

            return _lazyKey;
        }

        public CngKey GetOrGenerateKey(ECCurve? curve)
        {
            if (_lazyKey != null)
            {
                return _lazyKey;
            }

            // We don't have a key yet so generate
            Debug.Assert(curve.HasValue);

            CngKeyCreationParameters creationParameters = new CngKeyCreationParameters()
            {
                ExportPolicy = CngExportPolicies.AllowPlaintextExport,
            };

            if (curve.Value.IsNamed)
            {
                creationParameters.Parameters.Add(CngKey.GetPropertyFromNamedCurve(curve.Value));
            }
            else if (curve.Value.IsPrime)
            {
                ECCurve eccurve = curve.Value;
                byte[] parametersBlob = ECCng.GetPrimeCurveParameterBlob(ref eccurve);
                CngProperty prop = new CngProperty(
                    Interop.BCrypt.BCryptPropertyStrings.BCRYPT_ECC_PARAMETERS,
                    parametersBlob,
                    CngPropertyOptions.None);
                creationParameters.Parameters.Add(prop);
            }
            else
            {
                throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curve.Value.CurveType.ToString()));
            }

            try
            {
                _lazyKey = CngKey.Create(DefaultKeyType ?? CngAlgorithm.ECDsa, null, creationParameters);
            }
            catch (CryptographicException e)
            {
                // Map to PlatformNotSupportedException if appropriate
                ErrorCode errorCode = (ErrorCode)e.HResult;

                if (curve.Value.IsNamed &&
                    errorCode == ErrorCode.NTE_INVALID_PARAMETER || errorCode == ErrorCode.NTE_NOT_SUPPORTED)
                {
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, curve.Value.Oid.FriendlyName), e);
                }
                throw;
            }

            return _lazyKey;
        }

        public void SetKey(CngKey key)
        {
            Debug.Assert(key != null);

            // If we already have a key, clear it out.
            DisposeKey();

            _lazyKey = key;
        }

        public void Dispose()
        {
            DisposeKey();
        }
    }
}
