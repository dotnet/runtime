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
        protected override bool NopFlushCompletesSynchronously => true;
        protected override bool CanSetLengthGreaterThanCapacity => false;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData)
        {
            // See WritableMemoryStreamConformanceTests: WritableMemoryStream is fixed-capacity, so the
            // empty case returns null and the grow-from-empty conformance tests are skipped.
            if (initialData is null or { Length: 0 })
            {
                return Task.FromResult<Stream?>(null);
            }

            var manager = new System.Buffers.NativeMemoryManager(initialData.Length);
            manager.GetSpan().Clear();

            var inner = new WritableMemoryStream(manager.Memory);
            inner.Write(initialData, 0, initialData.Length);
            inner.Position = 0;

            return Task.FromResult<Stream?>(new NativeMemoryOwningStream(inner, manager));
        }
    }
}
