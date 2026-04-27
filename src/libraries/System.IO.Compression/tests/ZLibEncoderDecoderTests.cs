// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class ZLibEncoderDecoderTests : ZLibEncoderDecoderTestBase
    {
        public class ZLibEncoderAdapter : EncoderAdapter
        {
            private readonly ZLibEncoder _encoder;

            public ZLibEncoderAdapter(ZLibEncoder encoder)
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

        public class ZLibDecoderAdapter : DecoderAdapter
        {
            private readonly ZLibDecoder _decoder;

            public ZLibDecoderAdapter(ZLibDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new ZLibEncoderAdapter(new ZLibEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new ZLibEncoderAdapter(new ZLibEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(ZLibCompressionOptions options) =>
            new ZLibEncoderAdapter(new ZLibEncoder(options));

        protected override DecoderAdapter CreateDecoder() =>
            new ZLibDecoderAdapter(new ZLibDecoder());

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZLibEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            ZLibEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            ZLibDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputSize) =>
            ZLibEncoder.GetMaxCompressedLength(inputSize);
    }
}
