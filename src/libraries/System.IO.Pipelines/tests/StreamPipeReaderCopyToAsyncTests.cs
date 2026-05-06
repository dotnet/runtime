// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeReaderCopyToAsyncTests : CopyToAsyncTests
    {
        private PipeReader? _pipeReader;
        protected override PipeReader PipeReader => _pipeReader ??= PipeReader.Create(Pipe.Reader.AsStream());
    }
}
