// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using Xunit;

namespace System.IO.Compression
{
    public class GZipEncoderDecoderTests : ZLibEncoderDecoderTestBase
    {
        public class GZipEncoderAdapter : EncoderAdapter
        {
            private readonly GZipEncoder _encoder;

            public GZipEncoderAdapter(GZipEncoder encoder)
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

        public class GZipDecoderAdapter : DecoderAdapter
        {
            private readonly GZipDecoder _decoder;

            public GZipDecoderAdapter(GZipDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new GZipEncoderAdapter(new GZipEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new GZipEncoderAdapter(new GZipEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(ZLibCompressionOptions options) =>
            new GZipEncoderAdapter(new GZipEncoder(options));

        protected override DecoderAdapter CreateDecoder() =>
            new GZipDecoderAdapter(new GZipDecoder());

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            GZipEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            GZipEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            GZipDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputSize) =>
            GZipEncoder.GetMaxCompressedLength(inputSize);
    }
}
