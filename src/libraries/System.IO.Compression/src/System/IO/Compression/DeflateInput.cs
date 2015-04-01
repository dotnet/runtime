// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal class DeflateInput
    {
        private byte[] buffer;
        private int count;
        private int startIndex;

        internal byte[] Buffer
        {
            get
            {
                return buffer;
            }
            set
            {
                buffer = value;
            }
        }

        internal int Count
        {
            get
            {
                return count;
            }
            set
            {
                count = value;
            }
        }

        internal int StartIndex
        {
            get
            {
                return startIndex;
            }
            set
            {
                startIndex = value;
            }
        }

        internal void ConsumeBytes(int n)
        {
            Debug.Assert(n <= count, "Should use more bytes than what we have in the buffer");
            startIndex += n;
            count -= n;
            Debug.Assert(startIndex + count <= buffer.Length, "Input buffer is in invalid state!");
        }

        internal InputState DumpState()
        {
            InputState savedState;
            savedState.count = count;
            savedState.startIndex = startIndex;
            return savedState;
        }

        internal void RestoreState(InputState state)
        {
            count = state.count;
            startIndex = state.startIndex;
        }

        internal struct InputState
        {
            internal int count;
            internal int startIndex;
        }
    }
}

