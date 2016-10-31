// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace System.IO.Compression.Tests
{
    public sealed class BeginEndDeflateStreamTests : DeflateStreamAsyncTestsBase
    {
        public override Task<int> ReadAsync(DeflateStream unzip, byte[] buffer, int offset, int count) =>
            Task<int>.Factory.FromAsync<byte[], int, int>(unzip.BeginRead, unzip.EndRead, buffer, offset, count, null);

        public override Task WriteAsync(DeflateStream unzip, byte[] buffer, int offset, int count) =>
            Task.Factory.FromAsync<byte[], int, int>(unzip.BeginWrite, unzip.EndWrite, buffer, offset, count, null);
    }
}