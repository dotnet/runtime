// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Net.Http;

namespace System.Net.WebSockets
{
    internal static class WebSocketMessageTypeAdapter
    {
        internal static Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE GetWinHttpMessageType(WebSocketMessageType messageType, bool endOfMessage)
        {
            switch (messageType)
            {
                case WebSocketMessageType.Binary:
                    if (endOfMessage)
                    {
                        return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE;
                    }
                    else
                    {
                        return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE;
                    }

                case WebSocketMessageType.Text:
                    if (endOfMessage)
                    {
                        return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE;
                    }
                    else
                    {
                        return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE;
                    }

                case WebSocketMessageType.Close:
                    return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE;

                default:
                    Debug.Fail("Unknown WebSocketMessageType.");
                    return Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE;
            }
        }

        internal static WebSocketMessageType GetWebSocketMessageType(Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE winHttpMessageType, out bool endOfMessage)
        {
            switch (winHttpMessageType)
            {
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_MESSAGE_BUFFER_TYPE:
                    endOfMessage = true;
                    return WebSocketMessageType.Binary;
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_BINARY_FRAGMENT_BUFFER_TYPE:
                    endOfMessage = false;
                    return WebSocketMessageType.Binary;
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_UTF8_MESSAGE_BUFFER_TYPE:
                    endOfMessage = true;
                    return WebSocketMessageType.Text;
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_UTF8_FRAGMENT_BUFFER_TYPE:
                    endOfMessage = false;
                    return WebSocketMessageType.Text;
                case Interop.WinHttp.WINHTTP_WEB_SOCKET_BUFFER_TYPE.WINHTTP_WEB_SOCKET_CLOSE_BUFFER_TYPE:
                    endOfMessage = true;
                    return WebSocketMessageType.Close;
                default:
                    throw new ArgumentOutOfRangeException(nameof(winHttpMessageType), "Unknown WINHTTP_WEB_SOCKET_BUFFER_TYPE.");
            }
        }
    }
}
