// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.IO;

namespace System.Text
{
    /// <summary>Provides a reusable reader for reading all of the text from streams.</summary>
    internal sealed class ReusableTextReader
    {
        /// <summary>StringBuilder used to store intermediate text results.</summary>
        private readonly StringBuilder _builder = new StringBuilder();
        /// <summary>Decoder used to decode data read from the stream.</summary>
        private readonly Decoder _decoder;
        /// <summary>Bytes read from the stream.</summary>
        private readonly byte[] _bytes;
        /// <summary>Temporary storage from characters converted from the bytes then written to the builder.</summary>
        private readonly char[] _chars;

        /// <summary>Initializes a new reusable reader.</summary>
        /// <param name="encoding">The Encoding to use.  Defaults to UTF8.</param>
        /// <param name="bufferSize">The size of the buffer to use when reading from the stream.</param>
        public ReusableTextReader(Encoding encoding = null, int bufferSize = 1024)
        {
            if (encoding == null)
            {
                encoding = Encoding.UTF8;
            }

            _decoder = encoding.GetDecoder();
            _bytes = new byte[bufferSize];
            _chars = new char[encoding.GetMaxCharCount(_bytes.Length)];
        }

        /// <summary>Read all of the text from the current position of the stream.</summary>
        public unsafe string ReadAllText(Stream source)
        {
            int bytesRead;
            while ((bytesRead = source.Read(_bytes, 0, _bytes.Length)) != 0)
            {
                int charCount = _decoder.GetChars(_bytes, 0, bytesRead, _chars, 0);
                _builder.Append(_chars, 0, charCount);
            }

            string s = _builder.ToString();

            _builder.Clear();
            _decoder.Reset();

            return s;
        }
    }
}
