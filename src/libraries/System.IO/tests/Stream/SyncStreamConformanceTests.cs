// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    [ActiveIssue("https://github.com/dotnet/runtime/issues/53679", typeof(PlatformDetection), nameof(PlatformDetection.IsAndroidAOT))]
    public class SyncStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool NopFlushCompletesSynchronously => false;

        protected override Task<Stream> CreateReadOnlyStreamCore(byte[] initialData) =>
            Task.FromResult(Stream.Synchronized(new MemoryStream(initialData ?? Array.Empty<byte>(), writable: false)));

        protected override Task<Stream> CreateReadWriteStreamCore(byte[] initialData) =>
            Task.FromResult(Stream.Synchronized(initialData != null ? new MemoryStream(initialData) : new MemoryStream()));

        protected override Task<Stream> CreateWriteOnlyStreamCore(byte[] initialData) =>
            Task.FromResult<Stream>(null);
    }
}
