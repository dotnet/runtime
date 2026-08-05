// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Net.Mime;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using System.Buffers.Text;

namespace System.Net
{
    internal sealed class Base64Stream : DelegatedStream, IEncodableStream
    {
        /// <summary>Characters that are ignored when decoding: whitespace used for folding, and padding.</summary>
        private static readonly SearchValues<byte> s_ignoredChars = SearchValues.Create("\r\n= \t"u8);

        private readonly Base64WriteStateInfo _writeState;
        private readonly Base64Encoder _encoder;

        internal Base64Stream(Stream stream, Base64WriteStateInfo writeStateInfo) : base(stream)
        {
            _writeState = new Base64WriteStateInfo();
            _encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
        }

        internal Base64Stream(Base64WriteStateInfo writeStateInfo) : base(new MemoryStream())
        {
            _writeState = writeStateInfo;
            _encoder = new Base64Encoder(_writeState, writeStateInfo.MaxLineLength);
        }

        public override bool CanRead => BaseStream.CanRead;
        public override bool CanWrite => BaseStream.CanWrite;

        private ReadStateInfo ReadState => field ??= new ReadStateInfo();

        internal WriteStateInfoBase WriteState
        {
            get
            {
                Debug.Assert(_writeState != null, "_writeState was null");
                return _writeState;
            }
        }

        public override void Close()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                _encoder.AppendPadding();
                FlushInternal();
            }

            base.Close();
        }

        public int DecodeBytes(Span<byte> buffer)
        {
            // Strip out any characters that are ignored when decoding, compacting the remaining
            // base64 characters to the beginning of the buffer.
            int length = 0;
            ReadOnlySpan<byte> remaining = buffer;
            while (true)
            {
                int index = remaining.IndexOfAny(s_ignoredChars);
                if (index < 0)
                {
                    remaining.CopyTo(buffer.Slice(length));
                    length += remaining.Length;
                    break;
                }

                remaining.Slice(0, index).CopyTo(buffer.Slice(length));
                length += index;
                remaining = remaining.Slice(index + 1);
            }

            ReadStateInfo readState = ReadState;
            int leftoverCount = readState.LeftoverCount;

            // As with the base stream, bytes are produced as soon as enough base64 characters have been
            // seen to produce them, even if that means decoding a partial four character block. Characters
            // belonging to such a partial block are remembered, along with how many bytes they've already
            // produced, so that the remainder of the block can be decoded once more data arrives.
            int total = leftoverCount + length;
            int leftoverBytes = readState.LeftoverBytes;
            int newLeftoverCount = total % 4;

            // A trailing partial block of two or three characters still yields one or two bytes, so it's
            // padded out to a full block with characters that contribute zero bits and decoded; a single
            // trailing character yields nothing.
            int sourceLength = total - newLeftoverCount + (newLeftoverCount >= 2 ? 4 : 0);
            int totalBytes = sourceLength / 4 * 3 - (newLeftoverCount >= 2 ? 4 - newLeftoverCount : 0);

            if (sourceLength == 0)
            {
                // Not enough data to produce anything yet.
                buffer.Slice(0, length).CopyTo(readState.Leftover.Slice(leftoverCount));
                readState.LeftoverCount = total;
                return 0;
            }

            OperationStatus status;
            if (leftoverCount == 0 && newLeftoverCount == 0)
            {
                // The buffer contains a whole number of four character blocks and nothing needs to be
                // prepended, so it can be decoded in place.
                Debug.Assert(leftoverBytes == 0);
                status = Base64.DecodeFromUtf8InPlace(buffer.Slice(0, length), out _);
            }
            else
            {
                // Concatenate the characters left over from the previous chunk with the new ones, padding
                // any trailing partial block so that it decodes to as many bytes as it can produce.
                int destinationLength = sourceLength / 4 * 3;
                int scratchLength = Math.Max(sourceLength, total);
                byte[] rented = ArrayPool<byte>.Shared.Rent(scratchLength + destinationLength);
                Span<byte> scratch = rented.AsSpan(0, scratchLength);
                Span<byte> destination = rented.AsSpan(scratchLength, destinationLength);

                readState.Leftover.Slice(0, leftoverCount).CopyTo(scratch);
                buffer.Slice(0, length).CopyTo(scratch.Slice(leftoverCount));

                scratch.Slice(total - newLeftoverCount, newLeftoverCount).CopyTo(readState.Leftover);
                readState.LeftoverCount = newLeftoverCount;
                readState.LeftoverBytes = totalBytes - (total - newLeftoverCount) / 4 * 3;

                // 'A' contributes no bits, so padding with it produces the same leading bytes as the
                // partial block on its own, and unlike '=' it doesn't require the trailing bits to be zero.
                scratch.Slice(total).Fill((byte)'A');

                status = Base64.DecodeFromUtf8(scratch.Slice(0, sourceLength), destination, out _, out _);

                if (status == OperationStatus.Done)
                {
                    destination.Slice(leftoverBytes, totalBytes - leftoverBytes).CopyTo(buffer);
                }

                ArrayPool<byte>.Shared.Return(rented);
            }

            if (status != OperationStatus.Done)
            {
                throw new FormatException(SR.MailBase64InvalidCharacter);
            }

            // Exclude the bytes that were already produced from the leftover characters.
            return totalBytes - leftoverBytes;
        }

        public int EncodeBytes(ReadOnlySpan<byte> buffer) =>
            _encoder.EncodeBytes(buffer, true, true);

        internal int EncodeBytes(ReadOnlySpan<byte> buffer, bool dontDeferFinalBytes, bool shouldAppendSpaceToCRLF)
        {
            return _encoder.EncodeBytes(buffer, dontDeferFinalBytes, shouldAppendSpaceToCRLF);
        }

        public int EncodeString(string value, Encoding encoding) => _encoder.EncodeString(value, encoding);

        public string GetEncodedString() => _encoder.GetEncodedString();

        public override void Flush()
        {
            if (_writeState != null && WriteState.Length > 0)
            {
                FlushInternal();
            }

            base.Flush();
        }

        public override async Task FlushAsync(CancellationToken cancellationToken)
        {
            await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
            await base.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        private void FlushInternal()
        {
            BaseStream.Write(WriteState.Buffer.AsSpan(0, WriteState.Length));
            WriteState.Reset();
        }

        private async ValueTask FlushInternalAsync(CancellationToken cancellationToken)
        {
            await BaseStream.WriteAsync(WriteState.Buffer.AsMemory(0, WriteState.Length), cancellationToken).ConfigureAwait(false);
            WriteState.Reset();
        }

        protected override int ReadInternal(Span<byte> buffer)
        {
            while (true)
            {
                // read data from the underlying stream
                int read = BaseStream.Read(buffer);

                // if the underlying stream returns 0 then there
                // is no more data - just return 0.
                if (read == 0)
                {
                    return 0;
                }

                // Decode the read bytes and update the input buffer with decoded bytes
                read = DecodeBytes(buffer.Slice(0, read));
                if (read > 0)
                {
                    return read;
                }
            }
        }

        protected override async ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            while (true)
            {
                // read data from the underlying stream
                int read = await BaseStream.ReadAsync(buffer, cancellationToken).ConfigureAwait(false);

                // if the underlying stream returns 0 then there
                // is no more data - just return 0.
                if (read == 0)
                {
                    return 0;
                }

                // Decode the read bytes and update the input buffer with decoded bytes
                read = DecodeBytes(buffer.Span.Slice(0, read));
                if (read > 0)
                {
                    return read;
                }
            }
        }

        protected override void WriteInternal(ReadOnlySpan<byte> buffer)
        {
            int written = 0;

            // do not append a space when writing from a stream since this means
            // it's writing the email body
            while (true)
            {
                written += EncodeBytes(buffer.Slice(written), false, false);
                if (written < buffer.Length)
                {
                    FlushInternal();
                }
                else
                {
                    break;
                }
            }
        }

        protected override async ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            int written = 0;

            // do not append a space when writing from a stream since this means
            // it's writing the email body
            while (true)
            {
                written += EncodeBytes(buffer.Span.Slice(written), false, false);
                if (written < buffer.Length)
                {
                    await FlushInternalAsync(cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    break;
                }
            }
        }

        private sealed class ReadStateInfo
        {
            private readonly byte[] _leftover = new byte[3];

            /// <summary>Base64 characters left over from a previous chunk that didn't form a complete four character block.</summary>
            internal Span<byte> Leftover => _leftover;

            internal int LeftoverCount { get; set; }

            /// <summary>Number of bytes already produced from the characters in <see cref="Leftover"/>.</summary>
            internal int LeftoverBytes { get; set; }
        }
    }
}
