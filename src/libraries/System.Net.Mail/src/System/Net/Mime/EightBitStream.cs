// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;

namespace System.Net.Mime
{
    /// <summary>
    /// This stream does not encode content, but merely allows the user to declare
    /// that the content does not need encoding.
    ///
    /// For SMTP transport, this stream implements RFC 5321 Sections 2.3.8 and
    /// 4.5.2 (normalize line endings and pad leading dots) on the entire message
    /// so we don't have to implement it on all of the individual components.
    ///
    /// History: This class used to be called SevenBitStream and was supposed to
    /// validate that outgoing bytes were within the acceptable range of 0 - 127
    /// and throw if a value > 127 is found.
    /// However, the enforcement was not properly implemented and rarely executed.
    /// For legacy (app-compat) reasons we have chosen to remove the enforcement
    /// and rename the class from SevenBitStream to EightBitStream.
    /// </summary>
    internal sealed class EightBitStream : DelegatedStream, IEncodableStream
    {
        // Apply SMTP line normalization and dot-stuffing only at the outermost
        // stream, since this stream may also be used recursively.
        private readonly bool _shouldEncodeLeadingDots;
        private bool _lastWriteEndedWithCr;

        private WriteStateInfoBase WriteState => field ??= new WriteStateInfoBase();

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal EightBitStream(Stream stream) : base(stream) { }

        internal EightBitStream(Stream stream, bool shouldEncodeLeadingDots) : this(stream)
        {
            _shouldEncodeLeadingDots = shouldEncodeLeadingDots;
        }

        public override bool CanRead => false;
        public override bool CanWrite => BaseStream.CanWrite;

        protected override int ReadInternal(Span<byte> buffer)
        {
            throw new NotImplementedException();
        }

        protected override ValueTask<int> ReadAsyncInternal(Memory<byte> buffer, CancellationToken cancellationToken = default)
        {
            throw new NotImplementedException();
        }

        // Implement abstract Write methods
        protected override void WriteInternal(ReadOnlySpan<byte> buffer)
        {
            if (_shouldEncodeLeadingDots)
            {
                EncodeLines(buffer);
                BaseStream.Write(WriteState.Buffer.AsSpan(0, WriteState.Length));
                WriteState.BufferFlushed();
            }
            else
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.
                BaseStream.Write(buffer);
            }
        }

        protected override ValueTask WriteAsyncInternal(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
        {
            if (_shouldEncodeLeadingDots)
            {
                EncodeLines(buffer.Span);
                ValueTask task = BaseStream.WriteAsync(WriteState.Buffer.AsMemory(0, WriteState.Length), cancellationToken);
                WriteState.BufferFlushed(); // Reset state after initiating async write
                return task;
            }
            else
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.
                return BaseStream.WriteAsync(buffer, cancellationToken);
            }
        }

        // helper methods

        // Normalize line endings to CRLF and escape leading dots (RFC 5321
        // Sections 2.3.8 and 4.5.2).
        private void EncodeLines(ReadOnlySpan<byte> buffer)
        {
            WriteStateInfoBase writeState = WriteState;

            while (!buffer.IsEmpty)
            {
                if (_lastWriteEndedWithCr)
                {
                    _lastWriteEndedWithCr = false;

                    // The CR already emitted CRLF, so consume a following LF without
                    // adding another newline, even when CRLF is split across writes.
                    if (buffer[0] == '\n')
                    {
                        buffer = buffer.Slice(1);
                        continue;
                    }
                }

                if (writeState.CurrentLineLength == 0 && buffer[0] == '.')
                {
                    // Escape leading dots so a line containing only "." cannot end the SMTP data.
                    writeState.Append((byte)'.');
                }

                int lineEndingIndex = buffer.IndexOfAny((byte)'\r', (byte)'\n');
                if (lineEndingIndex < 0)
                {
                    writeState.Append(buffer);
                    return;
                }

                writeState.Append(buffer.Slice(0, lineEndingIndex));
                // Some SMTP servers accept bare CR or LF as line endings. Normalize
                // both and reset the line state so subsequent leading dots are escaped.
                writeState.AppendCRLF(false);
                _lastWriteEndedWithCr = buffer[lineEndingIndex] == '\r';
                buffer = buffer.Slice(lineEndingIndex + 1);
            }
        }

        public int DecodeBytes(Span<byte> buffer) { throw new NotImplementedException(); }

        public int EncodeBytes(ReadOnlySpan<byte> buffer) { throw new NotImplementedException(); }

        public int EncodeString(string value, Encoding encoding) { throw new NotImplementedException(); }

        public string GetEncodedString() { throw new NotImplementedException(); }
    }
}
