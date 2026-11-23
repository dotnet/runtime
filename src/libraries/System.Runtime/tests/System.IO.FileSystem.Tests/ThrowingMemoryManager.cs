// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;

namespace System.IO.Tests
{
    internal sealed class ThrowingMemoryManager : MemoryManager<byte>
    {
        public Memory<byte> Buffer => CreateMemory(1);
        public override Span<byte> GetSpan() => throw new InvalidOperationException();
        public override MemoryHandle Pin(int elementIndex = 0) => throw new InvalidOperationException();
        public override void Unpin() { }
        protected override void Dispose(bool disposing) { }
    }
}
