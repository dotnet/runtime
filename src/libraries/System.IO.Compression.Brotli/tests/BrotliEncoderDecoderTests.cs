// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Linq;
using System.Reflection;
using Xunit;

namespace System.IO.Compression
{
    public class BrotliEncoderDecoderTests : EncoderDecoderTestBase
    {
        protected override bool SupportsDictionaries => false;
        protected override bool SupportsReset => false;

        protected override string WindowLogParamName => "window";
        protected override string InputLengthParamName => "inputSize";

        protected override int ValidQuality => 3;
        protected override int ValidWindowLog => 10;

        protected override int InvalidQualityTooLow => -1;
        protected override int InvalidQualityTooHigh => 12;
        protected override int InvalidWindowLogTooLow => 9;
        protected override int InvalidWindowLogTooHigh => 25;

        public class BrotliEncoderAdapter : EncoderAdapter
        {
            private BrotliEncoder _encoder;

            public BrotliEncoderAdapter(BrotliEncoder encoder)
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

        public class BrotliDecoderAdapter : DecoderAdapter
        {
            private BrotliDecoder _decoder;

            public BrotliDecoderAdapter(BrotliDecoder decoder)
            {
                _decoder = decoder;
            }

            public override OperationStatus Decompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesConsumed, out int bytesWritten) =>
                _decoder.Decompress(source, destination, out bytesConsumed, out bytesWritten);

            public override void Dispose() => _decoder.Dispose();
            public override void Reset() => throw new NotSupportedException();
        }

        protected override EncoderAdapter CreateEncoder() =>
            new BrotliEncoderAdapter(new BrotliEncoder());

        protected override EncoderAdapter CreateEncoder(int quality, int windowLog) =>
            new BrotliEncoderAdapter(new BrotliEncoder(quality, windowLog));

        protected override EncoderAdapter CreateEncoder(DictionaryAdapter dictionary, int windowLog) =>
            throw new NotSupportedException();

        protected override DecoderAdapter CreateDecoder() =>
            new BrotliDecoderAdapter(new BrotliDecoder());

        protected override DecoderAdapter CreateDecoder(DictionaryAdapter dictionary) =>
            throw new NotSupportedException();

        protected override DictionaryAdapter CreateDictionary(ReadOnlySpan<byte> dictionaryData, int quality) => throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            BrotliEncoder.TryCompress(source, destination, out bytesWritten);

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary, int windowLog) => throw new NotSupportedException();

        protected override bool TryCompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, int quality, int windowLog) =>
            BrotliEncoder.TryCompress(source, destination, out bytesWritten, quality, windowLog);

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, DictionaryAdapter dictionary) => throw new NotSupportedException();

        protected override bool TryDecompress(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten) =>
            BrotliDecoder.TryDecompress(source, destination, out bytesWritten);

        protected override long GetMaxCompressedLength(long inputSize) =>
            BrotliEncoder.GetMaxCompressedLength((int)inputSize);
    }
}
