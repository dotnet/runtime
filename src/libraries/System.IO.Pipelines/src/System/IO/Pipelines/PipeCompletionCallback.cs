// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.IO.Pipelines
{
    internal readonly struct PipeCompletionCallback
    {
        public readonly Action<Exception?, object?> Callback;
        public readonly object? State;

        public PipeCompletionCallback(Action<Exception?, object?> callback, object? state)
        {
            Callback = callback;
            State = state;
        }
    }
}
