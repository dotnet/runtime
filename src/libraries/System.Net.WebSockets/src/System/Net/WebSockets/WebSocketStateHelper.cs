// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net.WebSockets
{
    internal static class WebSocketStateHelper
    {
        internal const int All = (1 << ((int)WebSocketState.Aborted + 1)) - 1;

        internal static void ThrowIfInvalidState(WebSocketState currentState, bool isDisposed, Exception? innerException, int validStates)
        {
            if (!HasFlag(validStates, currentState))
            {
                string invalidStateMessage = SR.Format(
                    SR.net_WebSockets_InvalidState, currentState, string.Join(", ", FromFlags(validStates)));

                throw new WebSocketException(WebSocketError.InvalidState, invalidStateMessage, innerException);
            }

            if (innerException is not null)
            {
                Debug.Assert(currentState == WebSocketState.Aborted);
                throw new OperationCanceledException(nameof(WebSocketState.Aborted), innerException);
            }

            // Ordering is important to maintain .NET 4.5 WebSocket implementation exception behavior.
            ObjectDisposedException.ThrowIf(isDisposed, typeof(WebSocket));
        }

        internal static bool HasFlag(int states, WebSocketState value) => (states & 1 << (int)value) != 0;

        internal static int ToFlags(params WebSocketState[] values)
        {
            int states = 0;
            foreach (WebSocketState value in values)
            {
                states |= 1 << (int)value;
            }
            return states;
        }

        private static IEnumerable<WebSocketState> FromFlags(int states)
        {
            foreach (WebSocketState value in Enum.GetValues<WebSocketState>())
            {
                if (HasFlag(states, value))
                {
                    yield return value;
                }
            }
        }
    }
}
