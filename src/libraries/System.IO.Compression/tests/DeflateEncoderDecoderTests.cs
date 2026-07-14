// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public partial class DeflateEncoderDecoderTests : ZLibEncoderDecoderTestBase
    {
        public class DeflateEncoderAdapter : EncoderAdapter
        {
            private readonly DeflateEncoder _encoder;

            public DeflateEncoderAdapter(DeflateEncoder encoder)
            {
                _encoder = encoder;
            }

            public override OperationStatus Compress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten, bool isFinalBlock) =>
                _encoder.Compress(source, destination, out bytesConsumed, out bytesWritten, isFinalBlock);

            public override OperationStatus Flush(Span<byte> destination, out int bytesWritten) =>
                _encoder.Flush(destination, out bytesWritten);

            public override void Dispose() => _encoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        public class DeflateDecoderAdapter : DecoderAdapter
        {
            private readonly DeflateDecoder _decoder;

            public DeflateDecoderAdapter(DeflateDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new DeflateEncoderAdapter(new DeflateEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new DeflateEncoderAdapter(new DeflateEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(ZLibCompressionOptions options) =>
            new DeflateEncoderAdapter(new DeflateEncoder(options));

        protected override DecoderAdapter CreateDecoder() =>
            new DeflateDecoderAdapter(new DeflateDecoder());

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            DeflateEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            DeflateEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            DeflateDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputSize) =>
            DeflateEncoder.GetMaxCompressedLength(inputSize);
    }
}
