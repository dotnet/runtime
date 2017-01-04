// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;

namespace System.IO.Compression
{
    internal sealed class DeflateInput
    {
        internal byte[] Buffer;
        internal int Count;
        internal int StartIndex;

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
            Count = state.Count;
            StartIndex = state.StartIndex;
        }

        internal struct InputState
        {
            internal readonly int Count;
            internal readonly int StartIndex;

            public InputState(int count, int startIndex)
            {
                Count = count;
                StartIndex = startIndex;
            }
        }
    }
}
