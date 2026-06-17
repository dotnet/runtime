// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class WritableMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;
        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData)
        {
            if (initialData is null)
            {
                return Task.FromResult<Stream?>(null);
            }

            if (initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(
                    new WritableMemoryStream(new Memory<byte>(new byte[1024])));
            }

            var memory = new Memory<byte>(new byte[initialData.Length]);
            var stream = new WritableMemoryStream(memory);
            stream.Write(initialData, 0, initialData.Length);
            stream.Position = 0;
            return Task.FromResult<Stream?>(stream);
        }
    }
}
