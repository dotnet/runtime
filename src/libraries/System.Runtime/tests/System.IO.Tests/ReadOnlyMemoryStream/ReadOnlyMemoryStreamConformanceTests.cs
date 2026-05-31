// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading.Tasks;

namespace System.IO.Tests
{
    /// <summary>
    /// Conformance tests for ReadOnlyMemoryStream - a read-only, seekable stream
    /// over a ReadOnlyMemory&lt;byte&gt;.
    /// </summary>
    public class ReadOnlyMemoryStreamConformanceTests : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => true;
        protected override bool CanSetLength => false; // Immutable stream
        protected override bool NopFlushCompletesSynchronously => true;

        /// <summary>
        /// Creates a read-only ReadOnlyMemoryStream with provided initial data.
        /// </summary>
        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData == null || initialData.Length == 0)
            {
                // Empty data
                return Task.FromResult<Stream?>(new ReadOnlyMemoryStream(ReadOnlyMemory<byte>.Empty));
            }

            var data = new ReadOnlyMemory<byte>(initialData);
            return Task.FromResult<Stream?>(new ReadOnlyMemoryStream(data));
        }

        // Read-only stream does not support write-only mode
        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        // Read-only stream does not support read-write mode
        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }
}
