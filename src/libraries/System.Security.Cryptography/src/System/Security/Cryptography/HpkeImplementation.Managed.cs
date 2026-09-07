// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Security.Cryptography
{
    internal sealed class HpkeImplementation : Hpke
    {
        private readonly HpkeManagedKemAdapter _adapter;

        private HpkeImplementation(HpkeManagedKemAdapter adapter) : base(adapter.Suite)
        {
            _adapter = adapter;
        }

        internal static bool IsSupportedImpl(HpkeSuite suite) =>
            suite.KemMetadata.IsSupported &&
            suite.KdfMetadata.IsSupported &&
            suite.AeadMetadata.IsSupported;

        internal static HpkeImplementation DeriveKeyImpl(HpkeSuite suite, ReadOnlySpan<byte> ikm)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.DeriveKeyPair(ikm);
                return new HpkeImplementation(adapter);
            }
            catch
            {
                adapter.Dispose();
                throw;
            }
        }

        internal static HpkeImplementation GenerateKeyImpl(HpkeSuite suite)
        {
            HpkeManagedKemAdapter adapter = HpkeManagedKemAdapter.Create(suite);

            try
            {
                adapter.Generate();
                return new HpkeImplementation(adapter);
            }
            catch
            {
                adapter.Dispose();
                throw;
            }
        }

        protected override void ExportDecapsulationKeyCore(Span<byte> destination) =>
            _adapter.ExportDecapsulationKey(destination);

        protected override void ExportEncapsulationKeyCore(Span<byte> destination) =>
            _adapter.ExportEncapsulationKey(destination);

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _adapter.Dispose();
            }

            base.Dispose(disposing);
        }
    }
}
