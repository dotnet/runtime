// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.IO.Tests;
using System.Threading.Tasks;

namespace System.Memory.Tests
{
    public class NativeWritableMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;
        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData)
        {
            int dataLength = initialData?.Length ?? 0;
            int length = dataLength == 0 ? 64 * 1024 : dataLength;
            var manager = new System.Buffers.NativeMemoryManager(length);
            manager.GetSpan().Clear();

            var inner = new WritableMemoryStream(manager.Memory);
            if (dataLength != 0)
            {
                inner.Write(initialData!, 0, dataLength);
                inner.Position = 0;
            }

            return Task.FromResult<Stream?>(new NativeMemoryOwningStream(inner, manager));
        }
    }
}
