// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace System.Net.WebSockets
{
    internal sealed partial class ManagedWebSocket : WebSocket
    {
        private Task ValidateAndReceiveAsync(Task receiveTask, byte[] buffer, CancellationToken cancellationToken)
        {
            if (receiveTask == null ||
                (receiveTask.Status == TaskStatus.RanToCompletion &&
                !(receiveTask is Task<WebSocketReceiveResult> wsrr && wsrr.Result.MessageType == WebSocketMessageType.Close)))
            {
                receiveTask = ReceiveAsyncPrivate<WebSocketReceiveResultGetter, WebSocketReceiveResult>(new ArraySegment<byte>(buffer), cancellationToken).AsTask();
            }

            return receiveTask;
        }
    }
}
