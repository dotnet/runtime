// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading.Tasks;

namespace System.IO.Compression.Tests
{
    public sealed class BeginEndDeflateStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => true;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task<int>.Factory.FromAsync<byte[], int, int>(((DeflateStream)unzip).BeginRead, ((DeflateStream)unzip).EndRead, buffer, offset, count, null);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task.Factory.FromAsync<byte[], int, int>(((DeflateStream)unzip).BeginWrite, ((DeflateStream)unzip).EndWrite, buffer, offset, count, null);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new DeflateStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new DeflateStream(stream, mode, leaveOpen);
    }

    public sealed class BeginEndGZipStreamTests : CompressionStreamAsyncTestBase
    {
        public override bool StripHeaders => false;
        public override Task<int> ReadAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task<int>.Factory.FromAsync<byte[], int, int>(((GZipStream)unzip).BeginRead, ((GZipStream)unzip).EndRead, buffer, offset, count, null);
        public override Task WriteAsync(Stream unzip, byte[] buffer, int offset, int count) =>
            Task.Factory.FromAsync<byte[], int, int>(((GZipStream)unzip).BeginWrite, ((GZipStream)unzip).EndWrite, buffer, offset, count, null);
        public override Stream CreateStream(Stream stream, CompressionMode mode) => new GZipStream(stream, mode);
        public override Stream CreateStream(Stream stream, CompressionMode mode, bool leaveOpen) => new GZipStream(stream, mode, leaveOpen);
    }
}