// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines.Tests
{
    public class StreamPipeReaderReadAtLeastAsyncTests : ReadAtLeastAsyncTests
    {
        private PipeReader? _pipeReader;
        protected override PipeReader PipeReader => _pipeReader ?? (_pipeReader = PipeReader.Create(Pipe.Reader.AsStream()));

        protected override void SetPipeReaderOptions(int bufferSize)
        {
            _pipeReader = PipeReader.Create(Pipe.Reader.AsStream(), new StreamPipeReaderOptions(bufferSize: bufferSize));
        }
    }
}
