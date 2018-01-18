// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        public override Task SendAsync(ReadOnlyMemory<byte> buffer, WebSocketMessageType messageType, bool endOfMessage, CancellationToken cancellationToken)
        {
            return SendPrivateAsync(buffer, messageType, endOfMessage, cancellationToken);
        }

        public override ValueTask<ValueWebSocketReceiveResult> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            try
            {
                WebSocketValidate.ThrowIfInvalidState(_state, _disposed, s_validReceiveStates);

                Debug.Assert(!Monitor.IsEntered(StateUpdateLock), $"{nameof(StateUpdateLock)} must never be held when acquiring {nameof(ReceiveAsyncLock)}");
                lock (ReceiveAsyncLock) // synchronize with receives in CloseAsync
                {
                    ThrowIfOperationInProgress(_lastReceiveAsync);
                    ValueTask<ValueWebSocketReceiveResult> t = ReceiveAsyncPrivate<ValueWebSocketReceiveResultGetter, ValueWebSocketReceiveResult>(buffer, cancellationToken);
                    _lastReceiveAsync =
                        t.IsCompletedSuccessfully ? (t.Result.MessageType == WebSocketMessageType.Close ? s_cachedCloseTask : Task.CompletedTask) :
                        t.AsTask();
                    return t;
                }
            }
            catch (Exception exc)
            {
                return new ValueTask<ValueWebSocketReceiveResult>(Task.FromException<ValueWebSocketReceiveResult>(exc));
            }
        }

        private Task ValidateAndReceiveAsync(Task receiveTask, byte[] buffer, CancellationToken cancellationToken)
        {
            if (receiveTask == null ||
                        (receiveTask.IsCompletedSuccessfully &&
                         !(receiveTask is Task<WebSocketReceiveResult> wsrr && wsrr.Result.MessageType == WebSocketMessageType.Close) &&
                         !(receiveTask is Task<ValueWebSocketReceiveResult> vwsrr && vwsrr.Result.MessageType == WebSocketMessageType.Close)))
            {
                ValueTask<ValueWebSocketReceiveResult> vt = ReceiveAsyncPrivate<ValueWebSocketReceiveResultGetter, ValueWebSocketReceiveResult>(buffer, cancellationToken);
                receiveTask =
                    vt.IsCompletedSuccessfully ? (vt.Result.MessageType == WebSocketMessageType.Close ? s_cachedCloseTask : Task.CompletedTask) :
                    vt.AsTask();
            }

            return receiveTask;
        }

        /// <summary><see cref="IWebSocketReceiveResultGetter{TResult}"/> implementation for <see cref="ValueWebSocketReceiveResult"/>.</summary>
        private readonly struct ValueWebSocketReceiveResultGetter : IWebSocketReceiveResultGetter<ValueWebSocketReceiveResult>
        {
            public ValueWebSocketReceiveResult GetResult(int count, WebSocketMessageType messageType, bool endOfMessage, WebSocketCloseStatus? closeStatus, string closeDescription) =>
                new ValueWebSocketReceiveResult(count, messageType, endOfMessage); // closeStatus/closeDescription are ignored
        }
    }
}
