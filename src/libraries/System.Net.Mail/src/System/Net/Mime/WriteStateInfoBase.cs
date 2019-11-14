// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Net.Mime
{
    internal class WriteStateInfoBase
    {
        protected readonly byte[] _header;
        protected readonly byte[] _footer;
        protected readonly int _maxLineLength;

        protected byte[] _buffer;
        protected int _currentLineLength;
        protected int _currentBufferUsed;

        //1024 was originally set in the encoding streams
        protected const int DefaultBufferSize = 1024;

        internal WriteStateInfoBase()
        {
            _header = Array.Empty<byte>();
            _footer = Array.Empty<byte>();
            _maxLineLength = EncodedStreamFactory.DefaultMaxLineLength;

            _buffer = new byte[DefaultBufferSize];
            _currentLineLength = 0;
            _currentBufferUsed = 0;
        }

        internal WriteStateInfoBase(int bufferSize, byte[] header, byte[] footer, int maxLineLength)
            : this(bufferSize, header, footer, maxLineLength, 0)
        {
        }

        internal WriteStateInfoBase(int bufferSize, byte[] header, byte[] footer, int maxLineLength, int mimeHeaderLength)
        {
            _buffer = new byte[bufferSize];
            _header = header;
            _footer = footer;
            _maxLineLength = maxLineLength;
            // Account for header name, if any.  e.g. "Subject: "
            _currentLineLength = mimeHeaderLength;
            _currentBufferUsed = 0;
        }

        internal int FooterLength => _footer.Length;
        internal byte[] Footer => _footer;
        internal byte[] Header => _header;
        internal byte[] Buffer => _buffer;
        internal int Length => _currentBufferUsed;
        internal int CurrentLineLength => _currentLineLength;

        // Make sure there is enough space in the buffer to write at least this many more bytes.
        // This should be called before ANY direct write to Buffer.
        private void EnsureSpaceInBuffer(int moreBytes)
        {
            int newsize = Buffer.Length;
            while (_currentBufferUsed + moreBytes >= newsize)
            {
                newsize *= 2;
            }

            if (newsize > Buffer.Length)
            {
                //try to resize- if the machine doesn't have the memory to resize just let it throw
                byte[] tempBuffer = new byte[newsize];

                _buffer.CopyTo(tempBuffer, 0);
                _buffer = tempBuffer;
            }
        }

        internal void Append(byte aByte)
        {
            EnsureSpaceInBuffer(1);
            Buffer[_currentBufferUsed++] = aByte;
            _currentLineLength++;
        }

        internal void Append(params byte[] bytes)
        {
            EnsureSpaceInBuffer(bytes.Length);
            bytes.CopyTo(_buffer, Length);
            _currentLineLength += bytes.Length;
            _currentBufferUsed += bytes.Length;
        }

        internal void AppendCRLF(bool includeSpace)
        {
            AppendFooter();

            //add soft line break
            Append((byte)'\r', (byte)'\n');
            _currentLineLength = 0; // New Line
            if (includeSpace)
            {
                //add whitespace to new line (RFC 2045, soft CRLF must be followed by whitespace char)
                //space selected for parity with other MS email clients
                Append((byte)' ');
            }

            AppendHeader();
        }

        internal void AppendHeader()
        {
            if (Header != null && Header.Length != 0)
            {
                Append(Header);
            }
        }

        internal void AppendFooter()
        {
            if (Footer != null && Footer.Length != 0)
            {
                Append(Footer);
            }
        }

        internal int MaxLineLength => _maxLineLength;

        internal void Reset()
        {
            _currentBufferUsed = 0;
            _currentLineLength = 0;
        }

        internal void BufferFlushed()
        {
            _currentBufferUsed = 0;
        }
    }
}
