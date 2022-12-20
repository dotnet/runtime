// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECOpenSsl : IDisposable
    {
        private Lazy<SafeEcKeyHandle> _key = null!; // Always initialized

        public ECOpenSsl(ECCurve curve)
        {
            GenerateKey(curve);
        }

        public ECOpenSsl(AsymmetricAlgorithm owner)
        {
            _key = new Lazy<SafeEcKeyHandle>(() => GenerateKeyLazy(owner));
        }

        public ECOpenSsl(ECParameters ecParameters)
        {
            ImportParameters(ecParameters);
        }

        public ECOpenSsl(SafeEcKeyHandle key)
        {
            _key = new Lazy<SafeEcKeyHandle>(key);
        }

        internal SafeEcKeyHandle Value => _key.Value;

        private static SafeEcKeyHandle GenerateKeyLazy(AsymmetricAlgorithm owner) =>
            GenerateKeyByKeySize(owner.KeySize);

        public void Dispose()
        {
            FreeKey();
        }

        internal int KeySize => Interop.Crypto.EcKeyGetSize(_key.Value);

        internal SafeEvpPKeyHandle UpRefKeyHandle()
        {
            SafeEcKeyHandle currentKey = _key.Value;
            Debug.Assert(currentKey != null, "null TODO");

            SafeEvpPKeyHandle pkeyHandle = Interop.Crypto.EvpPkeyCreate();

            try
            {
                // Wrapping our key in an EVP_PKEY will up_ref our key.
                // When the EVP_PKEY is Disposed it will down_ref the key.
                // So everything should be copacetic.
                if (!Interop.Crypto.EvpPkeySetEcKey(pkeyHandle, currentKey))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                return pkeyHandle;
            }
            catch
            {
                pkeyHandle.Dispose();
                throw;
            }
        }

        internal void SetKey(SafeEcKeyHandle key)
        {
            Debug.Assert(key != null, "key != null");
            Debug.Assert(!key.IsInvalid, "!key.IsInvalid");
            Debug.Assert(!key.IsClosed, "!key.IsClosed");

            FreeKey();
            _key = new Lazy<SafeEcKeyHandle>(key);
        }

        internal int GenerateKey(ECCurve curve)
        {
            curve.Validate();
            FreeKey();

            if (curve.IsNamed)
            {
                // Use oid Value first if present, otherwise FriendlyName because Oid maintains a hard-coded
                // cache that may have different casing for FriendlyNames than OpenSsl
                string oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName!;

                SafeEcKeyHandle? key = Interop.Crypto.EcKeyCreateByOid(oid);

                if (key == null || key.IsInvalid)
                {
                    key?.Dispose();
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
                }

                if (!Interop.Crypto.EcKeyGenerateKey(key))
                {
                    throw Interop.Crypto.CreateOpenSslCryptographicException();
                }

                SetKey(key);
            }
            else if (curve.IsExplicit)
            {
                SafeEcKeyHandle key = Interop.Crypto.EcKeyCreateByExplicitCurve(curve);

                if (!Interop.Crypto.EcKeyGenerateKey(key))
                    throw Interop.Crypto.CreateOpenSslCryptographicException();

                SetKey(key);
            }
            else
            {
                throw new PlatformNotSupportedException(
                    SR.Format(SR.Cryptography_CurveNotSupported, curve.CurveType.ToString()));
            }

            return KeySize;
        }

        private void FreeKey()
        {
            if (_key != null)
            {
                if (_key.IsValueCreated)
                {
                    _key.Value?.Dispose();
                }

                _key = null!;
            }
        }
    }
}
