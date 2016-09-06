// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal class DeflateInput
    {
        private byte[] _buffer;
        private int _count;
        private int _startIndex;

        internal byte[] Buffer
        {
            get
            {
                return _buffer;
            }
            set
            {
                _buffer = value;
            }
        }

        internal int Count
        {
            get
            {
                return _count;
            }
            set
            {
                _count = value;
            }
        }

        internal int StartIndex
        {
            get
            {
                return _startIndex;
            }
            set
            {
                _startIndex = value;
            }
        }

        internal void ConsumeBytes(int n)
        {
            Debug.Assert(n <= _count, "Should use more bytes than what we have in the buffer");
            _startIndex += n;
            _count -= n;
            Debug.Assert(_startIndex + _count <= _buffer.Length, "Input buffer is in invalid state!");
        }

        internal InputState DumpState()
        {
            InputState savedState;
            savedState.count = _count;
            savedState.startIndex = _startIndex;
            return savedState;
        }

        internal void RestoreState(InputState state)
        {
            _count = state.count;
            _startIndex = state.startIndex;
        }

        internal struct InputState
        {
            internal int count;
            internal int startIndex;
        }
    }
}
