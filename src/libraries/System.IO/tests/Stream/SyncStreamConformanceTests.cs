// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Tests
{
    public class SyncStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool NopFlushCompletesSynchronously => false;

        protected override Stream CreateReadOnlyStreamCore(byte[] initialData) =>
            Stream.Synchronized(new MemoryStream(initialData ?? Array.Empty<byte>(), writable: false));

        protected override Stream CreateReadWriteStreamCore(byte[] initialData) =>
            Stream.Synchronized(initialData != null ? new MemoryStream(initialData) : new MemoryStream());

        protected override Stream CreateWriteOnlyStreamCore(byte[] initialData) =>
            null;
    }
}
