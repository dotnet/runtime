// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net.Sockets;
using System.Threading.Tasks;
using System.Threading.Tasks.Sources;

namespace System.Net.Connections
{
    internal sealed class TaskSocketAsyncEventArgs : SocketAsyncEventArgs, IValueTaskSource
    {
        private ManualResetValueTaskSourceCore<int> _valueTaskSource;

        public void ResetTask() => _valueTaskSource.Reset();
        public ValueTask Task => new ValueTask(this, _valueTaskSource.Version);

        public void GetResult(short token) => _valueTaskSource.GetResult(token);
        public ValueTaskSourceStatus GetStatus(short token) => _valueTaskSource.GetStatus(token);
        public void OnCompleted(Action<object?> continuation, object? state, short token, ValueTaskSourceOnCompletedFlags flags) => _valueTaskSource.OnCompleted(continuation, state, token, flags);
        public void Complete() => _valueTaskSource.SetResult(0);

        public TaskSocketAsyncEventArgs()
            : base(unsafeSuppressExecutionContextFlow: true)
        {
        }

        protected override void OnCompleted(SocketAsyncEventArgs e)
        {
            _valueTaskSource.SetResult(0);
        }
    }
}
