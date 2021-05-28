// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines.Tests
{
    public class BasePipeReaderReadAtLeastAsyncTests : ReadAtLeastAsyncTests
    {
        private PipeReader? _pipeReader;
        protected override PipeReader PipeReader => _pipeReader ?? (_pipeReader = new BasePipeReader(Pipe.Reader));
    }
}
