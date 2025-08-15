// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;

namespace System.Net.WebSockets
{
    internal static class WebSocketStateHelper
    {
        /// <summary>Valid states to be in when calling SendAsync.</summary>
        internal const ManagedWebSocketStates ValidSendStates = ManagedWebSocketStates.Open | ManagedWebSocketStates.CloseReceived;
        /// <summary>Valid states to be in when calling ReceiveAsync.</summary>
        internal const ManagedWebSocketStates ValidReceiveStates = ManagedWebSocketStates.Open | ManagedWebSocketStates.CloseSent;
        /// <summary>Valid states to be in when calling CloseOutputAsync.</summary>
        internal const ManagedWebSocketStates ValidCloseOutputStates = ManagedWebSocketStates.Open | ManagedWebSocketStates.CloseReceived;
        /// <summary>Valid states to be in when calling CloseAsync.</summary>
        internal const ManagedWebSocketStates ValidCloseStates = ManagedWebSocketStates.Open | ManagedWebSocketStates.CloseReceived | ManagedWebSocketStates.CloseSent;

        internal static bool IsValidSendState(WebSocketState state) => ValidSendStates.HasFlag(ToFlag(state));

        internal static void ThrowIfInvalidState(WebSocketState currentState, bool isDisposed, Exception? innerException, ManagedWebSocketStates validStates)
        {
            ManagedWebSocketStates state = ToFlag(currentState);

            if ((state & validStates) == 0)
            {
                string invalidStateMessage = SR.Format(
                    SR.net_WebSockets_InvalidState, currentState, validStates);

                throw new WebSocketException(WebSocketError.InvalidState, invalidStateMessage, innerException);
            }

            if (innerException is not null)
            {
                Debug.Assert(state == ManagedWebSocketStates.Aborted);
                throw new OperationCanceledException(nameof(WebSocketState.Aborted), innerException);
            }

            // Ordering is important to maintain .NET 4.5 WebSocket implementation exception behavior.
            ObjectDisposedException.ThrowIf(isDisposed, typeof(WebSocket));
        }

        private static ManagedWebSocketStates ToFlag(WebSocketState value)
        {
            ManagedWebSocketStates flag = (ManagedWebSocketStates)(1 << (int)value);
            Debug.Assert(Enum.IsDefined(flag));
            return flag;
        }
    }
}
