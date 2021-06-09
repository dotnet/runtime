// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Xml
{
    //
    //  IncrementalReadDecoder abstract class
    //
    internal abstract class IncrementalReadDecoder
    {
        internal abstract int DecodedCount { get; }
        internal abstract bool IsFull { get; }
        internal abstract void SetNextOutputBuffer(Array array, int offset, int len);
        internal abstract int Decode(char[] chars, int startPos, int len);
        internal abstract int Decode(string str, int startPos, int len);
        internal abstract void Reset();
    }

    // Needed only for XmlTextReader
    //
    //  Dummy IncrementalReadDecoder
    //
    internal sealed class IncrementalReadDummyDecoder : IncrementalReadDecoder
    {
        internal override int DecodedCount { get { return -1; } }
        internal override bool IsFull { get { return false; } }
        internal override void SetNextOutputBuffer(Array array, int offset, int len) { }
        internal override int Decode(char[] chars, int startPos, int len) { return len; }
        internal override int Decode(string str, int startPos, int len) { return len; }
        internal override void Reset() { }
    }

    //
    //  IncrementalReadDecoder for ReadChars
    //
    internal sealed class IncrementalReadCharsDecoder : IncrementalReadDecoder
    {
        private char[]? _buffer;
        private int _startIndex;
        private int _curIndex;
        private int _endIndex;

        internal override int DecodedCount
        {
            get
            {
                return _curIndex - _startIndex;
            }
        }

        internal override bool IsFull
        {
            get
            {
                return _curIndex == _endIndex;
            }
        }

        internal override int Decode(char[] chars, int startPos, int len)
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(chars != null);
            Debug.Assert(len >= 0);
            Debug.Assert(startPos >= 0);
            Debug.Assert(chars.Length - startPos >= len);

            Debug.Assert(len > 0);

            int copyCount = _endIndex - _curIndex;
            if (copyCount > len)
            {
                copyCount = len;
            }

            Buffer.BlockCopy(chars, startPos * 2, _buffer, _curIndex * 2, copyCount * 2);
            _curIndex += copyCount;

            return copyCount;
        }

        internal override int Decode(string str, int startPos, int len)
        {
            Debug.Assert(_buffer != null);
            Debug.Assert(str != null);
            Debug.Assert(len >= 0);
            Debug.Assert(startPos >= 0);
            Debug.Assert(str.Length - startPos >= len);

            Debug.Assert(len > 0);

            int copyCount = _endIndex - _curIndex;
            if (copyCount > len)
            {
                copyCount = len;
            }

            str.CopyTo(startPos, _buffer, _curIndex, copyCount);
            _curIndex += copyCount;

            return copyCount;
        }

        internal override void Reset()
        {
        }

        internal override void SetNextOutputBuffer(Array buffer, int index, int count)
        {
            Debug.Assert(buffer != null);
            Debug.Assert(count >= 0);
            Debug.Assert(index >= 0);
            Debug.Assert(buffer.Length - index >= count);

            Debug.Assert((buffer as char[]) != null);
            _buffer = (char[])buffer;
            _startIndex = index;
            _curIndex = index;
            _endIndex = index + count;
        }
    }
}
