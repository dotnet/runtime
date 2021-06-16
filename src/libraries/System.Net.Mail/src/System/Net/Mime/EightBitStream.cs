// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Text;

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
        private WriteStateInfoBase? _writeState;

        // Should we do RFC 2821 Section 4.5.2 encoding of leading dots on a line?
        // We make this optional because this stream may be used recursively and
        // the encoding should only be done once.
        private readonly bool _shouldEncodeLeadingDots;

        private WriteStateInfoBase WriteState => _writeState ?? (_writeState = new WriteStateInfoBase());

        /// <summary>
        /// ctor.
        /// </summary>
        /// <param name="stream">Underlying stream</param>
        internal EightBitStream(Stream stream) : base(stream) { }

        internal EightBitStream(Stream stream, bool shouldEncodeLeadingDots) : this(stream)
        {
            _shouldEncodeLeadingDots = shouldEncodeLeadingDots;
        }

        /// <summary>
        /// Writes the specified content to the underlying stream
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param>
        /// <param name="count">Count of bytes to write</param>
        /// <param name="callback">Callback to call when write completes</param>
        /// <param name="state">State to pass to callback</param>
        public override IAsyncResult BeginWrite(byte[] buffer, int offset, int count, AsyncCallback? callback, object? state)
        {
            ValidateBufferArguments(buffer, offset, count);

            IAsyncResult result;
            if (_shouldEncodeLeadingDots)
            {
                EncodeLines(buffer, offset, count);
                result = base.BeginWrite(WriteState.Buffer, 0, WriteState.Length, callback, state);
            }
            else
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.
                result = base.BeginWrite(buffer, offset, count, callback, state);
            }

            return result;
        }

        public override void EndWrite(IAsyncResult asyncResult)
        {
            base.EndWrite(asyncResult);
            WriteState.BufferFlushed();
        }

        /// <summary>
        /// Writes the specified content to the underlying stream
        /// </summary>
        /// <param name="buffer">Buffer to write</param>
        /// <param name="offset">Offset within buffer to start writing</param>
        /// <param name="count">Count of bytes to write</param>
        public override void Write(byte[] buffer, int offset, int count)
        {
            ValidateBufferArguments(buffer, offset, count);

            if (_shouldEncodeLeadingDots)
            {
                EncodeLines(buffer, offset, count);
                base.Write(WriteState.Buffer, 0, WriteState.Length);
                WriteState.BufferFlushed();
            }
            else
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.
                base.Write(buffer, offset, count);
            }
        }

        // helper methods

        // Despite not having to encode content, we still have to implement
        // RFC 2821 Section 4.5.2 about leading dots on a line
        private void EncodeLines(byte[] buffer, int offset, int count)
        {
            for (int i = offset; (i < offset + count) && (i < buffer.Length); i++)
            {
                // Note: for legacy reasons we are not enforcing buffer[i] <= 127.

                // Detect CRLF line endings
                if ((buffer[i] == '\r') && ((i + 1) < (offset + count)) && (buffer[i + 1] == '\n'))
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

        public int DecodeBytes(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

        public int EncodeBytes(byte[] buffer, int offset, int count) { throw new NotImplementedException(); }

        public int EncodeString(string value, Encoding encoding) { throw new NotImplementedException(); }

        public string GetEncodedString() { throw new NotImplementedException(); }
    }
}
