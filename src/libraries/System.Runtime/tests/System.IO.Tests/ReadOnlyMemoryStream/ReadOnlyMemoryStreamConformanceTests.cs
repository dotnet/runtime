// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    public class ReadOnlyMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(new ReadOnlyMemoryStream(ReadOnlyMemory<byte>.Empty));
            }

            return Task.FromResult<Stream?>(new ReadOnlyMemoryStream(new ReadOnlyMemory<byte>(initialData)));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }
}
