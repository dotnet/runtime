// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression
{
    // Regression tests for https://github.com/dotnet/runtime/issues/134700: DeflateStream, GZipStream, and
    // ZLibStream all share a pool of native deflate states, keyed by compression level/strategy/window
    // bits/mem level. These tests live here (rather than in the shared CompressionStreamUnitTestBase used by
    // Brotli/Zstandard fixtures too) because only these three formats are backed by the pooled Deflater.
    public class DeflaterStatePoolTests
    {
        public enum StreamFormat
        {
            Deflate,
            GZip,
            ZLib
        }

        private const int BufferSize = 8192;

        [Theory]
        [InlineData(StreamFormat.Deflate)]
        [InlineData(StreamFormat.GZip)]
        [InlineData(StreamFormat.ZLib)]
        public void PooledState_ReusedAcrossSequentialStreams_RoundTrips(StreamFormat format)
        {
            // The native deflate state is pooled and reused (via deflateReset()) across Deflaters that
            // request the same configuration, so repeatedly disposing and creating new streams with the same
            // options must keep round-tripping data correctly, not just the first time a slot is used.
            var options = new ZLibCompressionOptions { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Default };
            byte[] data = Encoding.UTF8.GetBytes(new string('a', BufferSize * 4) + Guid.NewGuid());

            for (int i = 0; i < 4; i++)
            {
                Assert.Equal(data, RoundTrip(format, options, data));
            }
        }

        [Theory]
        [InlineData(StreamFormat.Deflate)]
        [InlineData(StreamFormat.GZip)]
        [InlineData(StreamFormat.ZLib)]
        public void PooledState_ReusedAfterFailedDispose_RoundTrips(StreamFormat format)
        {
            // If the destination stream fails partway through the Finish() loop that Dispose() runs, the
            // Deflater is still returned to the pool with its native state possibly not fully flushed. The
            // next Deflater to rent that slot resets it via deflateReset(), and must still produce correct,
            // independent output.
            var options = new ZLibCompressionOptions { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Default };
            byte[] incompressible = new byte[BufferSize * 5];
            new Random(42).NextBytes(incompressible);

            var faultyDestination = new ThrowsAfterNWritesStream(writesAllowedBeforeThrow: 1);
            Stream compressor = CreateCompressor(format, faultyDestination, options);
            compressor.Write(incompressible, 0, incompressible.Length);

            Assert.Throws<IOException>(compressor.Dispose);
            Assert.True(faultyDestination.DidThrow, "Test setup issue: the destination stream never threw, so the regression path wasn't exercised.");

            byte[] data = Encoding.UTF8.GetBytes(new string('a', BufferSize * 4) + Guid.NewGuid());
            Assert.Equal(data, RoundTrip(format, options, data));
        }

        [Theory]
        [InlineData(StreamFormat.Deflate)]
        [InlineData(StreamFormat.GZip)]
        [InlineData(StreamFormat.ZLib)]
        public void PooledState_SurvivesPoolWraparound_RoundTrips(StreamFormat format)
        {
            // The pool holds a bounded number of native states and evicts old entries once full. Requesting
            // more distinct configurations than the pool can hold, then reusing an earlier configuration
            // again, must still round-trip correctly whether it hits a pooled state or allocates a fresh one.
            byte[] data = Encoding.UTF8.GetBytes(new string('a', BufferSize * 4) + Guid.NewGuid());
            var firstOptions = new ZLibCompressionOptions { CompressionLevel = 1, CompressionStrategy = ZLibCompressionStrategy.Default };

            Assert.Equal(data, RoundTrip(format, firstOptions, data));

            for (int level = 1; level <= 9; level++)
            {
                foreach (ZLibCompressionStrategy strategy in new[]
                {
                    ZLibCompressionStrategy.Default, ZLibCompressionStrategy.Filtered, ZLibCompressionStrategy.HuffmanOnly
                })
                {
                    var options = new ZLibCompressionOptions { CompressionLevel = level, CompressionStrategy = strategy };
                    Assert.Equal(data, RoundTrip(format, options, data));
                }
            }

            // By now at least 27 distinct configurations have cycled through the pool, well past its capacity.
            // Revisiting the very first configuration must still round-trip correctly.
            Assert.Equal(data, RoundTrip(format, firstOptions, data));
        }

        [Fact]
        public void InterleavedFormats_ReusingPool_DoNotCrossContaminate()
        {
            // GZipStream and ZLibStream each use a distinct window-bits value from plain Deflate, so
            // interleaving them must never let one format's pooled state leak into another's stream.
            var options = new ZLibCompressionOptions { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Default };
            byte[] data = Encoding.UTF8.GetBytes(new string('a', BufferSize * 4) + Guid.NewGuid());

            for (int i = 0; i < 3; i++)
            {
                Assert.Equal(data, RoundTrip(StreamFormat.Deflate, options, data));
                Assert.Equal(data, RoundTrip(StreamFormat.GZip, options, data));
                Assert.Equal(data, RoundTrip(StreamFormat.ZLib, options, data));
            }
        }

        [Theory]
        [InlineData(StreamFormat.Deflate)]
        [InlineData(StreamFormat.GZip)]
        [InlineData(StreamFormat.ZLib)]
        public void SequentialDoubleDispose_IsNoOp(StreamFormat format)
        {
            // Dispose() must be idempotent: calling it a second time after the compressor's native state has
            // already been returned to the pool must be a safe no-op rather than throw.
            Stream compressor = CreateCompressor(format, new MemoryStream(), new ZLibCompressionOptions());
            compressor.Dispose();
            compressor.Dispose();
        }

        [Theory]
        [InlineData(StreamFormat.Deflate)]
        [InlineData(StreamFormat.GZip)]
        [InlineData(StreamFormat.ZLib)]
        public void ConcurrentDispose_ReturnsStateToPoolExactlyOnce(StreamFormat format)
        {
            // Two threads racing to Dispose() the same underlying Deflater must not both win the race to
            // detach and return its native state to the pool - only one may, and the other must observe
            // _isDisposed under the lock and no-op. Calling the public stream's Dispose() concurrently would
            // also race on its own unsynchronized fields, so this drives the Deflater directly (via
            // reflection, since it's internal) to isolate the exact recheck-under-lock this test targets.
            var options = new ZLibCompressionOptions { CompressionLevel = 5, CompressionStrategy = ZLibCompressionStrategy.Default };

            for (int i = 0; i < 25; i++)
            {
                Stream compressor = CreateCompressor(format, new MemoryStream(), options);
                compressor.Write(new byte[] { 1, 2, 3 });

                object deflater = GetDeflater(compressor);
                MethodInfo disposeMethod = deflater.GetType().GetMethod("Dispose", BindingFlags.Public | BindingFlags.Instance)!;

                using var barrier = new Barrier(2);
                void DisposeOnce()
                {
                    barrier.SignalAndWait();
                    disposeMethod.Invoke(deflater, null);
                }

                Task t1 = Task.Run(DisposeOnce);
                Task t2 = Task.Run(DisposeOnce);
                Task.WaitAll(t1, t2);
            }

            // The pool must not have been corrupted by a duplicate return: a subsequent compressor with the
            // same configuration must still round-trip correctly and independently.
            byte[] data = Encoding.UTF8.GetBytes(new string('a', BufferSize * 4) + Guid.NewGuid());
            Assert.Equal(data, RoundTrip(format, options, data));
        }

        private static Stream CreateCompressor(StreamFormat format, Stream output, ZLibCompressionOptions options) => format switch
        {
            StreamFormat.Deflate => new DeflateStream(output, options, leaveOpen: true),
            StreamFormat.GZip => new GZipStream(output, options, leaveOpen: true),
            StreamFormat.ZLib => new ZLibStream(output, options, leaveOpen: true),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        private static Stream CreateDecompressor(StreamFormat format, Stream input) => format switch
        {
            StreamFormat.Deflate => new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true),
            StreamFormat.GZip => new GZipStream(input, CompressionMode.Decompress, leaveOpen: true),
            StreamFormat.ZLib => new ZLibStream(input, CompressionMode.Decompress, leaveOpen: true),
            _ => throw new ArgumentOutOfRangeException(nameof(format))
        };

        private static byte[] RoundTrip(StreamFormat format, ZLibCompressionOptions options, byte[] data)
        {
            var compressed = new MemoryStream();
            using (Stream compressor = CreateCompressor(format, compressed, options))
            {
                compressor.Write(data, 0, data.Length);
            }
            compressed.Position = 0;

            var decompressed = new MemoryStream();
            using (Stream decompressor = CreateDecompressor(format, compressed))
            {
                decompressor.CopyTo(decompressed);
            }
            return decompressed.ToArray();
        }

        // GZipStream/ZLibStream wrap a DeflateStream; unwrap it if necessary to get to the field holding the
        // Deflater. Deflater is internal, so this is the only way to reach it from a test assembly.
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075",
            Justification = "Test-only reflection over internal implementation types that are always present at test time.")]
        private static object GetDeflater(Stream compressionStream)
        {
            object target = compressionStream;

            FieldInfo? wrapperField = target.GetType().GetField("_deflateStream", BindingFlags.NonPublic | BindingFlags.Instance);
            if (wrapperField is not null)
            {
                target = wrapperField.GetValue(target)!;
            }

            FieldInfo deflaterField = target.GetType().GetField("_deflater", BindingFlags.NonPublic | BindingFlags.Instance)!;
            return deflaterField.GetValue(target)!;
        }
    }
}
