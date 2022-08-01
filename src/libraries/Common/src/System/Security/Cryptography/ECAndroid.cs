// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using Microsoft.Win32.SafeHandles;

namespace System.Security.Cryptography
{
    internal sealed partial class ECAndroid : IDisposable
    {
        private Lazy<SafeEcKeyHandle> _key = null!; // Always initialized

        public ECAndroid(ECCurve curve)
        {
            GenerateKey(curve);
        }

        public ECAndroid(AsymmetricAlgorithm owner)
        {
            _key = new Lazy<SafeEcKeyHandle>(() => GenerateKeyLazy(owner));
        }

        public ECAndroid(ECParameters ecParameters)
        {
            ImportParameters(ecParameters);
        }

        public ECAndroid(SafeEcKeyHandle key)
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

        internal int KeySize => Interop.AndroidCrypto.EcKeyGetSize(_key.Value);

        internal SafeEcKeyHandle UpRefKeyHandle() => _key.Value.DuplicateHandle();

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
                // cache that may have different casing for FriendlyNames than Android
                string oid = !string.IsNullOrEmpty(curve.Oid.Value) ? curve.Oid.Value : curve.Oid.FriendlyName!;

                SafeEcKeyHandle? key = Interop.AndroidCrypto.EcKeyCreateByOid(oid);

                if (key == null || key.IsInvalid)
                {
                    key?.Dispose();
                    throw new PlatformNotSupportedException(SR.Format(SR.Cryptography_CurveNotSupported, oid));
                }

                SetKey(key);
            }
            else if (curve.IsExplicit)
            {
                SafeEcKeyHandle key = Interop.AndroidCrypto.EcKeyCreateByExplicitCurve(curve);

                if (key == null || key.IsInvalid)
                {
                    key?.Dispose();
                    throw new CryptographicException();
                }

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
