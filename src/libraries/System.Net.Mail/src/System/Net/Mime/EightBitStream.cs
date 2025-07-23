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
    /// This stream is also used to implement RFC 2821 Section 4.5.2 (pad leading
    /// dots on a line) on the entire message so we don't have to implement it
    /// on all of the individual components.
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
        // Should we do RFC 2821 Section 4.5.2 encoding of leading dots on a line?
        // We make this optional because this stream may be used recursively and
        // the encoding should only be done once.
        private readonly bool _shouldEncodeLeadingDots;

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

        // Despite not having to encode content, we still have to implement
        // RFC 2821 Section 4.5.2 about leading dots on a line
        private void EncodeLines(ReadOnlySpan<byte> buffer)
        {
            for (int i = 0; i < buffer.Length; i++)
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.

                // Detect CRLF line endings
                if ((buffer[i] == '\r') && ((i + 1) < buffer.Length) && (buffer[i + 1] == '\n'))
                {
                    WriteState.AppendCRLF(false); // Resets CurrentLineLength to 0
                    i++; // Skip past the recorded CRLF
                }
                else if ((WriteState.CurrentLineLength == 0) && (buffer[i] == '.'))
                {
                    // RFC 2821 Section 4.5.2: We must pad leading dots on a line with an extra dot
                    // This is the only 'encoding' change we make to the data in this method
                    WriteState.Append((byte)'.');
                    WriteState.Append(buffer[i]);
                }
                else
                {
                    // Just regular seven bit data
                    WriteState.Append(buffer[i]);
                }
            }
        }

        public int DecodeBytes(Span<byte> buffer) { throw new NotImplementedException(); }

        public int EncodeBytes(ReadOnlySpan<byte> buffer) { throw new NotImplementedException(); }

        public int EncodeString(string value, Encoding encoding) { throw new NotImplementedException(); }

        public string GetEncodedString() { throw new NotImplementedException(); }
    }
}
