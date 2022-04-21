// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Text;
using System.Collections.Specialized;

namespace System.Net.Mime
{
    /// <summary>
    /// Provides an abstraction for writing a MIME multi-part
    /// message.
    /// </summary>
    internal sealed class MimeWriter : BaseWriter
    {
        private static readonly byte[] s_DASHDASH = new byte[] { (byte)'-', (byte)'-' };

        private readonly byte[] _boundaryBytes;
        private bool _writeBoundary = true;

        internal MimeWriter(Stream stream, string boundary)
            : base(stream, false) // Unnecessary, the underlying MailWriter stream already encodes dots
        {
            ArgumentNullException.ThrowIfNull(boundary);

            _boundaryBytes = Encoding.ASCII.GetBytes(boundary);
        }

        internal override void WriteHeaders(NameValueCollection headers, bool allowUnicode)
        {
            ArgumentNullException.ThrowIfNull(headers);

            foreach (string key in headers)
                WriteHeader(key, headers[key]!, allowUnicode);
        }

        #region Cleanup

        internal IAsyncResult BeginClose(AsyncCallback? callback, object? state)
        {
            MultiAsyncResult multiResult = new MultiAsyncResult(this, callback, state);

            Close(multiResult);

            multiResult.CompleteSequence();

            return multiResult;
        }

        internal void EndClose(IAsyncResult result)
        {
            MultiAsyncResult.End(result);

            _stream.Close();
        }

        internal override void Close()
        {
            Close(null);

            _stream.Close();
        }

        private void Close(MultiAsyncResult? multiResult)
        {
            _bufferBuilder.Append(s_crlf);
            _bufferBuilder.Append(s_DASHDASH);
            _bufferBuilder.Append(_boundaryBytes);
            _bufferBuilder.Append(s_DASHDASH);
            _bufferBuilder.Append(s_crlf);
            Flush(multiResult);
        }

        /// <summary>
        /// Called when the current stream is closed.  Allows us to
        /// prepare for the next message part.
        /// </summary>
        /// <param name="sender">Sender of the close event</param>
        /// <param name="args">Event args (not used)</param>
        protected override void OnClose(object? sender, EventArgs args)
        {
            if (_contentStream != sender)
                return; // may have called WriteHeader

            _contentStream.Flush();
            _contentStream = null!;
            _writeBoundary = true;

            _isInContent = false;
        }

        #endregion Cleanup

        /// <summary>
        /// Writes the boundary sequence if required.
        /// </summary>
        protected override void CheckBoundary()
        {
            if (_writeBoundary)
            {
                _bufferBuilder.Append(s_crlf);
                _bufferBuilder.Append(s_DASHDASH);
                _bufferBuilder.Append(_boundaryBytes);
                _bufferBuilder.Append(s_crlf);
                _writeBoundary = false;
            }
        }
    }
}
