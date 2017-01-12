// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed class DeflateInput
    {
        internal byte[] Buffer { get; set; }
        internal int Count { get; set; }
        internal int StartIndex { get; set; }

        internal void ConsumeBytes(int n)
        {
            Debug.Assert(n <= Count, "Should use more bytes than what we have in the buffer");
            StartIndex += n;
            Count -= n;
            Debug.Assert(StartIndex + Count <= Buffer.Length, "Input buffer is in invalid state!");
        }

        internal InputState DumpState() => new InputState(Count, StartIndex);

        internal void RestoreState(InputState state)
        {
            Count = state._count;
            StartIndex = state._startIndex;
        }

        internal struct InputState
        {
            internal readonly int _count;
            internal readonly int _startIndex;

            internal InputState(int count, int startIndex)
            {
                _count = count;
                _startIndex = startIndex;
            }
        }
    }
}
