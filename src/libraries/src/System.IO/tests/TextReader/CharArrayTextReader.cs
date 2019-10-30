// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    public class CharArrayTextReader : TextReader
    {
        private readonly char[] _charBuffer;
        private int _charPos = 0;

        public bool EndOfStream => _charPos >= _charBuffer.Length;

        public CharArrayTextReader(char[] data)
        {
            _charBuffer = data;
        }

        public override int Peek()
        {
            if (_charPos == _charBuffer.Length)
            {
                return -1;
            }
            return _charBuffer[_charPos];
        }

        public override int Read()
        {
            if (_charPos == _charBuffer.Length)
            {
                return -1;
            }
            return _charBuffer[_charPos++];
        }
    }
}
