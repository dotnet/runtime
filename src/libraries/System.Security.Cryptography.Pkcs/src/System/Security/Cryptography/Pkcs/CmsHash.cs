// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;

namespace System.Security.Cryptography.Pkcs
{
    internal abstract class CmsHash : IDisposable
    {
        internal static CmsHash Create(Oid oid, bool forVerification) =>
            oid.Value switch
            {
#if NET
                Oids.Shake128 => new CmsShake128Hash(),
                Oids.Shake256 => new CmsShake256Hash(),
#endif
                _ => new CmsIncrementalHash(oid, forVerification),
            };

        public abstract void Dispose();
        internal abstract void AppendData(ReadOnlySpan<byte> data);
        internal abstract byte[] GetHashAndReset();

#if NET || NETSTANDARD2_1
        internal abstract bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten);
#endif

#if NET
        private sealed class CmsShake256Hash : CmsHash
        {
            // RFC 8702 specifies SHAKE256 in CMS must use 512 bits of output.
            private const int OutputSizeBytes = 512 / 8;

            private readonly Shake256 _shake256;

            internal CmsShake256Hash()
            {
                _shake256 = new Shake256();
            }

            public override void Dispose() => _shake256.Dispose();
            internal override void AppendData(ReadOnlySpan<byte> data) => _shake256.AppendData(data);
            internal override byte[] GetHashAndReset() => _shake256.GetHashAndReset(OutputSizeBytes);

            internal override bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length < OutputSizeBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                _shake256.GetHashAndReset(destination.Slice(0, OutputSizeBytes));
                bytesWritten = OutputSizeBytes;
                return true;
            }
        }

        private sealed class CmsShake128Hash : CmsHash
        {
            // RFC 8702 specifies SHAKE128 in CMS must use 256 bits of output.
            private const int OutputSizeBytes = 256 / 8;

            private readonly Shake128 _shake128;

            internal CmsShake128Hash()
            {
                _shake128 = new Shake128();
            }

            public override void Dispose() => _shake128.Dispose();
            internal override void AppendData(ReadOnlySpan<byte> data) => _shake128.AppendData(data);
            internal override byte[] GetHashAndReset() => _shake128.GetHashAndReset(OutputSizeBytes);

            internal override bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten)
            {
                if (destination.Length < OutputSizeBytes)
                {
                    bytesWritten = 0;
                    return false;
                }

                _shake128.GetHashAndReset(destination.Slice(0, OutputSizeBytes));
                bytesWritten = OutputSizeBytes;
                return true;
            }
        }
#endif

        private sealed class CmsIncrementalHash : CmsHash
        {
            private readonly IncrementalHash _incrementalHash;

            internal CmsIncrementalHash(Oid oid, bool forVerification)
            {
                _incrementalHash = Helpers.CreateIncrementalHash(PkcsHelpers.GetDigestAlgorithm(oid.Value, forVerification));
            }

            public override void Dispose() => _incrementalHash.Dispose();
            internal override void AppendData(ReadOnlySpan<byte> data) => _incrementalHash.AppendData(data);
            internal override byte[] GetHashAndReset() => _incrementalHash.GetHashAndReset();

#if NET || NETSTANDARD2_1
            internal override bool TryGetHashAndReset(Span<byte> destination, out int bytesWritten) =>
                _incrementalHash.TryGetHashAndReset(destination, out bytesWritten);
#endif
        }
    }
}
