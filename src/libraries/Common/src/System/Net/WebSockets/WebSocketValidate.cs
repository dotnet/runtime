// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Buffers;
using System.Diagnostics;
using System.Text;

namespace System.Net.WebSockets
{
    internal static partial class WebSocketValidate
    {
        /// <summary>
        /// The minimum value for window bits that the websocket per-message-deflate extension can support.<para />
        /// For the current implementation of deflate(), a windowBits value of 8 (a window size of 256 bytes) is not supported.
        /// We cannot use silently 9 instead of 8, because the websocket produces raw deflate stream
        /// and thus it needs to know the window bits in advance.
        /// </summary>
        internal const int MinDeflateWindowBits = 9;

        /// <summary>
        /// The maximum value for window bits that the websocket per-message-deflate extension can support.
        /// </summary>
        internal const int MaxDeflateWindowBits = 15;

        internal const int MaxControlFramePayloadLength = 123;
#if TARGET_BROWSER
        private const int ValidCloseStatusCodesFrom = 3000;
        private const int ValidCloseStatusCodesTo = 4999;
#else
        private const int CloseStatusCodeAbort = 1006;
        private const int CloseStatusCodeFailedTLSHandshake = 1015;
        private const int InvalidCloseStatusCodesFrom = 0;
        private const int InvalidCloseStatusCodesTo = 999;
#endif

        // [0x21, 0x7E] except separators "()<>@,;:\\\"/[]?={} ".
        private static readonly SearchValues<char> s_validSubprotocolChars =
            SearchValues.Create("!#$%&'*+-.0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ^_`abcdefghijklmnopqrstuvwxyz|~");

        internal static void ThrowIfInvalidState(WebSocketState currentState, bool isDisposed, WebSocketState[] validStates)
        {
            string validStatesText = string.Empty;

            if (validStates != null && validStates.Length > 0)
            {
                foreach (WebSocketState validState in validStates)
                {
                    if (currentState == validState)
                    {
                        // Ordering is important to maintain .NET 4.5 WebSocket implementation exception behavior.
                        ObjectDisposedException.ThrowIf(isDisposed, typeof(WebSocket));
                        return;
                    }
                }

                validStatesText = string.Join(", ", validStates);
            }

            throw new WebSocketException(
                WebSocketError.InvalidState,
                SR.Format(SR.net_WebSockets_InvalidState, currentState, validStatesText));
        }

        internal static void ValidateSubprotocol(string subProtocol)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(subProtocol);

            int indexOfInvalidChar = subProtocol.AsSpan().IndexOfAnyExcept(s_validSubprotocolChars);
            if (indexOfInvalidChar >= 0)
            {
                char invalidChar = subProtocol[indexOfInvalidChar];

                string invalidCharDescription = char.IsBetween(invalidChar, (char)0x21, (char)0x7E)
                    ? invalidChar.ToString() // ASCII separator
                    : $"[{(int)invalidChar}]";

                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCharInProtocolString, subProtocol, invalidCharDescription), nameof(subProtocol));
            }
        }

        internal static void ValidateCloseStatus(WebSocketCloseStatus closeStatus, string? statusDescription)
        {
            if (closeStatus == WebSocketCloseStatus.Empty && !string.IsNullOrEmpty(statusDescription))
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_ReasonNotNull,
                    statusDescription,
                    WebSocketCloseStatus.Empty),
                    nameof(statusDescription));
            }

            int closeStatusCode = (int)closeStatus;
#if TARGET_BROWSER
            // as defined in https://developer.mozilla.org/en-US/docs/Web/API/WebSocket/close#code
            if (closeStatus != WebSocketCloseStatus.NormalClosure && (closeStatusCode < ValidCloseStatusCodesFrom || closeStatusCode > ValidCloseStatusCodesTo))
#else
            if ((closeStatusCode >= InvalidCloseStatusCodesFrom &&
                closeStatusCode <= InvalidCloseStatusCodesTo) ||
                closeStatusCode == CloseStatusCodeAbort ||
                closeStatusCode == CloseStatusCodeFailedTLSHandshake)
#endif
            {
                // CloseStatus 1006 means Aborted - this will never appear on the wire and is reflected by calling WebSocket.Abort
                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCloseStatusCode,
                    closeStatusCode),
                    nameof(closeStatus));
            }

            if (!string.IsNullOrEmpty(statusDescription) &&
                Encoding.UTF8.GetMaxByteCount(statusDescription.Length) > MaxControlFramePayloadLength &&
                Encoding.UTF8.GetByteCount(statusDescription) > MaxControlFramePayloadLength)
            {
                throw new ArgumentException(SR.Format(SR.net_WebSockets_InvalidCloseStatusDescription,
                    statusDescription,
                    MaxControlFramePayloadLength),
                    nameof(statusDescription));
            }
        }

        internal static void ValidateArraySegment(ArraySegment<byte> arraySegment, string parameterName)
        {
            Debug.Assert(!string.IsNullOrEmpty(parameterName), "'parameterName' MUST NOT be NULL or string.Empty");

            if (arraySegment.Array == null)
            {
                throw new ArgumentNullException(parameterName + "." + nameof(arraySegment.Array));
            }
            if (arraySegment.Offset < 0 || arraySegment.Offset > arraySegment.Array.Length)
            {
                throw new ArgumentOutOfRangeException(parameterName + "." + nameof(arraySegment.Offset));
            }
            if (arraySegment.Count < 0 || arraySegment.Count > (arraySegment.Array.Length - arraySegment.Offset))
            {
                throw new ArgumentOutOfRangeException(parameterName + "." + nameof(arraySegment.Count));
            }
        }

        internal static void ValidateBuffer(byte[] buffer, int offset, int count)
        {
            ArgumentNullException.ThrowIfNull(buffer);

            ArgumentOutOfRangeException.ThrowIfNegative(offset);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(offset, buffer.Length);

            ArgumentOutOfRangeException.ThrowIfNegative(count);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(count, buffer.Length - offset);
        }
    }
}
