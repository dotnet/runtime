// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO.Pipelines;
using System.Threading.Tasks;

namespace System.Net.Connections.Tests
{
    internal class MockPipe : IDuplexPipe, IAsyncDisposable
    {
        public Func<ValueTask> OnDisposeAsync { get; set; }
        public PipeReader Input { get; set; }

        public PipeWriter Output { get; set; }

        public ValueTask DisposeAsync()
        {
            return OnDisposeAsync?.Invoke() ?? default;
        }
    }
}
