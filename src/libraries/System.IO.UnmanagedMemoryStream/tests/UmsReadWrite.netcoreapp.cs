// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.IO.Tests
{
    public sealed class SpanUmsReadWriteTests : UmsReadWriteTests
    {
        public override int Read(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.Read(new Span<byte>(array, offset, count));
        public override void Write(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.Write(new Span<byte>(array, offset, count));
    }

    public sealed class MemoryUmsReadWriteTests : UmsReadWriteTests
    {
        public override int Read(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.ReadAsync(new Memory<byte>(array, offset, count)).GetAwaiter().GetResult();
        public override void Write(UnmanagedMemoryStream stream, byte[] array, int offset, int count) =>
            stream.WriteAsync(new Memory<byte>(array, offset, count)).GetAwaiter().GetResult();
    }
}
