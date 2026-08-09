// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Threading.Tasks;

namespace System.Memory.Tests
{
    public class NativeReadOnlyMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            int length = initialData?.Length ?? 0;
            var manager = new System.Buffers.NativeMemoryManager(length);
            initialData?.CopyTo(manager.GetSpan());

            var inner = new ReadOnlyMemoryStream(manager.Memory);
            return Task.FromResult<Stream?>(new NativeMemoryOwningStream(inner, manager));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }
}
