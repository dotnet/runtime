// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;
using Xunit;

namespace System.IO.Tests
{
    public class MemoryStreamMemoryCtorTests : StandaloneStreamConformanceTests
    {
        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(new MemoryStream(initialData.AsMemory(), writable: false));

        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(
                initialData != null ? new MemoryStream(initialData.AsMemory()) :
                new MemoryStream());

        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(null);
    }
}
