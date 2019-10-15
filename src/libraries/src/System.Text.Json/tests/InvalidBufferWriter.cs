// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Buffers;
using System.Runtime.CompilerServices;

namespace System.Text.Json.Tests
{
    internal class InvalidBufferWriter : IBufferWriter<byte>
    {
        public InvalidBufferWriter()
        {
        }

        public Memory<byte> GetMemory(int minimumLength = 0) => new byte[10];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> GetSpan(int minimumLength = 0) => new byte[10];

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Advance(int bytes)
        {
        }
    }
}
