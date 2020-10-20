// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.Browser;
using System.Threading;
using System.Threading.Tasks;

namespace Internal.Cryptography
{
    internal static partial class HashProviderDispenser
    {
        public static HashProvider CreateHashProvider(string hashAlgorithmId)
        {
            //System.Diagnostics.Debug.WriteLine($"HashProviderDispenser::CreateHashProvider {hashAlgorithmId}");
            Interop.SubtleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);
            return new BrowserAsyncDigestProvider(algorithm);
        }

        public static unsafe HashProvider CreateMacProvider(string hashAlgorithmId, ReadOnlySpan<byte> key)
        {
            throw new PlatformNotSupportedException(SR.SystemSecurityCryptographyAlgorithms_PlatformNotSupported);
        }
        private static Interop.SubtleCrypto.PAL_HashAlgorithm HashAlgorithmToPal(string hashAlgorithmId) => hashAlgorithmId switch {
            HashAlgorithmNames.MD5 => Interop.SubtleCrypto.PAL_HashAlgorithm.Md5,
            HashAlgorithmNames.SHA1 => Interop.SubtleCrypto.PAL_HashAlgorithm.Sha1,
            HashAlgorithmNames.SHA256 => Interop.SubtleCrypto.PAL_HashAlgorithm.Sha256,
            HashAlgorithmNames.SHA384 => Interop.SubtleCrypto.PAL_HashAlgorithm.Sha384,
            HashAlgorithmNames.SHA512 => Interop.SubtleCrypto.PAL_HashAlgorithm.Sha512,
            _ => throw new CryptographicException(SR.Format(SR.Cryptography_UnknownHashAlgorithm, hashAlgorithmId))
        };

        internal static class OneShotHashProvider
        {
            public static unsafe int HashData(string hashAlgorithmId, ReadOnlySpan<byte> source, Span<byte> destination)
            {
                // System.Diagnostics.Debug.WriteLine($"HashProviderDispenser::OneShotHashProvider {hashAlgorithmId} ");
                // Interop.SubtleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);

                // fixed (byte* pSource = source)
                // fixed (byte* pDestination = destination)
                // {
                //     System.Diagnostics.Debug.WriteLine($"HashProviderDispenser::OneShotHashProvider->Interop.SubtleCrypto.DigestOneShot {hashAlgorithmId} / {source.Length} // {destination.Length}");

                //     int ret = Interop.SubtleCrypto.DigestOneShot(
                //         algorithm,
                //         pSource,
                //         source.Length,
                //         pDestination,
                //         destination.Length,
                //         out int digestSize);

                //     if (ret != 1)
                //     {
                //         Debug.Fail($"HashData return value {ret} was not 1");
                //         throw new CryptographicException();
                //     }

                //     Debug.Assert(digestSize <= destination.Length);

                    return -1;
                //}
            }

            public static unsafe Task<int> HashDataAsync(string hashAlgorithmId, byte[] source, byte[] destination, CancellationToken cancellationToken)
            {
                //System.Diagnostics.Debug.WriteLine($"HashProviderDispenser::OneShotHashProvider {hashAlgorithmId} ");
                Interop.SubtleCrypto.PAL_HashAlgorithm algorithm = HashAlgorithmToPal(hashAlgorithmId);

                fixed (byte* pSource = source)
                fixed (byte* pDestination = destination)
                {
                    //System.Diagnostics.Debug.WriteLine($"HashProviderDispenser::OneShotHashProvider->Interop.SubtleCrypto.DigestOneShot {hashAlgorithmId} / {source.Length} // {destination.Length}");

                    TaskCompletionSource<int> finalizetcs = new TaskCompletionSource<int>();
                    int ret = Interop.SubtleCrypto.DigestOneShot(
                        algorithm,
                        pSource,
                        source.Length,
                        pDestination,
                        destination.Length,
                        out int digestSize,
                        finalizetcs);

                    if (ret != 1)
                    {
                        Debug.Fail($"HashData return value {ret} was not 1");
                        throw new CryptographicException();
                    }

                    Debug.Assert(digestSize <= destination.Length);

                    return finalizetcs.Task;
                }
            }
        }

        private sealed class BrowserAsyncDigestProvider : HashProvider
        {
            private readonly SafeDigestCtxHandle _ctx;

            public override int HashSizeInBytes { get; }

            internal BrowserAsyncDigestProvider(Interop.SubtleCrypto.PAL_HashAlgorithm algorithm)
            {
                int hashSizeInBytes;
                _ctx = Interop.SubtleCrypto.DigestCreate(algorithm, out hashSizeInBytes);
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestCreate {algorithm} / {hashSizeInBytes}");
                if (hashSizeInBytes < 0)
                {
                    _ctx.Dispose();
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.Cryptography_UnknownHashAlgorithm,
                            Enum.GetName(typeof(Interop.SubtleCrypto.PAL_HashAlgorithm), algorithm)));
                }

                if (_ctx.IsInvalid)
                {
                    _ctx.Dispose();
                    throw new CryptographicException();
                }

                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestUpdate synchronous data length: {data.Length}");
                int ret = Interop.SubtleCrypto.DigestUpdate(_ctx, data);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestUpdate return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }
            }

            public override Task AppendHashDataAsync(byte[] array, int ibStart, int cbSize, CancellationToken cancellationToken)
            {
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider:AppendHashDataAsync::Interop.SubtleCrypto.DigestUpdate data length: {array.Length}");
                // At this time the DigestUpdate does not do anything async
                int ret = Interop.SubtleCrypto.DigestUpdate(_ctx, array, ibStart, cbSize);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestUpdate return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }
                return Task.CompletedTask;
            }
            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestFinal destination length: {destination.Length}");

                TaskCompletionSource<int> finalizetcs = new TaskCompletionSource<int>();
                int ret = Interop.SubtleCrypto.DigestFinal(_ctx, destination, finalizetcs);
                //return await mytcs.Task;
                //int ret = Interop.SubtleCrypto.DigestFinal(_ctx, destination);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestFinal return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override Task<int> FinalizeHashAndResetAsync(byte[] destination, CancellationToken cancellationToken)
            {
                TaskCompletionSource<int> finalizetcs = new TaskCompletionSource<int>();
                int ret = Interop.SubtleCrypto.DigestFinal(_ctx, destination, finalizetcs);
                return finalizetcs.Task;
            }

            public override int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);
                System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.GetCurrentHash destination length: {destination.Length}");
                int ret = Interop.SubtleCrypto.DigestCurrent(_ctx, destination);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestCurrent return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ctx?.Dispose();
                }
            }
        }

        private sealed class BrowserDigestProvider : HashProvider
        {
            private readonly SafeDigestCtxHandle _ctx;

            public override int HashSizeInBytes { get; }

            internal BrowserDigestProvider(Interop.SubtleCrypto.PAL_HashAlgorithm algorithm)
            {
                int hashSizeInBytes;
                _ctx = Interop.SubtleCrypto.DigestCreate(algorithm, out hashSizeInBytes);
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestCreate {algorithm} / {hashSizeInBytes}");
                if (hashSizeInBytes < 0)
                {
                    _ctx.Dispose();
                    throw new PlatformNotSupportedException(
                        SR.Format(
                            SR.Cryptography_UnknownHashAlgorithm,
                            Enum.GetName(typeof(Interop.SubtleCrypto.PAL_HashAlgorithm), algorithm)));
                }

                if (_ctx.IsInvalid)
                {
                    _ctx.Dispose();
                    throw new CryptographicException();
                }

                HashSizeInBytes = hashSizeInBytes;
            }

            public override void AppendHashData(ReadOnlySpan<byte> data)
            {
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestUpdate data length: {data.Length}");
                int ret = Interop.SubtleCrypto.DigestUpdate(_ctx, data);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestUpdate return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }
            }

            public override Task AppendHashDataAsync(byte[] array, int ibStart, int cbSize, CancellationToken cancellationToken) => throw new NotImplementedException();

            public override int FinalizeHashAndReset(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);
                //System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.DigestFinal destination length: {destination.Length}");

                int ret = Interop.SubtleCrypto.DigestFinal(_ctx, destination);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestFinal return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override Task<int> FinalizeHashAndResetAsync(byte[] destination, CancellationToken cancellationToken) => throw new NotImplementedException();

            public override int GetCurrentHash(Span<byte> destination)
            {
                Debug.Assert(destination.Length >= HashSizeInBytes);
                System.Diagnostics.Debug.WriteLine($"BrowserDigestProvider::Interop.SubtleCrypto.GetCurrentHash destination length: {destination.Length}");
                int ret = Interop.SubtleCrypto.DigestCurrent(_ctx, destination);

                if (ret != 1)
                {
                    Debug.Assert(ret == 0, $"DigestCurrent return value {ret} was not 0 or 1");
                    throw new CryptographicException();
                }

                return HashSizeInBytes;
            }

            public override void Dispose(bool disposing)
            {
                if (disposing)
                {
                    _ctx?.Dispose();
                }
            }
        }
    }

}
