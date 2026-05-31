// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text;
using System.Threading.Tasks;

namespace System.IO.Tests
{
    /// <summary>
    /// Conformance tests for StringStream using the ReadOnlyMemory{char} overload.
    /// </summary>
    public class StringStreamConformanceTests_Memory : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => false;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(new StringStream(ReadOnlyMemory<char>.Empty, Encoding.UTF8));
            }

            string sourceString = Encoding.UTF8.GetString(initialData);

            byte[] reencoded = Encoding.UTF8.GetBytes(sourceString);
            if (reencoded.Length != initialData.Length || !reencoded.AsSpan().SequenceEqual(initialData))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new StringStream(sourceString.AsMemory(), Encoding.UTF8));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }

    /// <summary>
    /// Conformance tests for StringStream using the string overload.
    /// </summary>
    public class StringStreamConformanceTests_String : StandaloneStreamConformanceTests
    {
        protected override bool CanSeek => false;
        protected override bool CanSetLength => false;
        protected override bool NopFlushCompletesSynchronously => true;

        protected override Task<Stream?> CreateReadOnlyStreamCore(byte[]? initialData)
        {
            if (initialData is null || initialData.Length == 0)
            {
                return Task.FromResult<Stream?>(new StringStream("", Encoding.UTF8));
            }

            string sourceString = Encoding.UTF8.GetString(initialData);

            byte[] reencoded = Encoding.UTF8.GetBytes(sourceString);
            if (reencoded.Length != initialData.Length || !reencoded.AsSpan().SequenceEqual(initialData))
            {
                return Task.FromResult<Stream?>(null);
            }

            return Task.FromResult<Stream?>(new StringStream(sourceString, Encoding.UTF8));
        }

        protected override Task<Stream?> CreateWriteOnlyStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);

        protected override Task<Stream?> CreateReadWriteStreamCore(byte[]? initialData) => Task.FromResult<Stream?>(null);
    }
}
