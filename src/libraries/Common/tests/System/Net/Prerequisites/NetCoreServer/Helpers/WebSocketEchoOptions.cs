// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public readonly struct WebSocketEchoOptions
    {
        public static readonly WebSocketEchoOptions Default = new();

        public bool ReplyWithPartialMessages { get; init; }
        public bool ReplyWithEnhancedCloseMessage { get; init; }
        public string SubProtocol { get; init; }
        public TimeSpan? Delay { get; init; }

        public static WebSocketEchoOptions Parse(string query)
        {
            if (query is null or "" or "?")
            {
                return Default;
            }

            return new WebSocketEchoOptions
            {
                ReplyWithPartialMessages = query.Contains("replyWithPartialMessages"),
                ReplyWithEnhancedCloseMessage = query.Contains("replyWithEnhancedCloseMessage"),
                SubProtocol = ParseSubProtocol(query),
                Delay = ParseDelay(query)
            };
        }

        private static string ParseSubProtocol(string query)
        {
            const string subProtocolKey = "subprotocol=";

            var index = query.IndexOf(subProtocolKey);
            if (index == -1)
            {
                return null;
            }

            var subProtocol = query.Substring(index + subProtocolKey.Length);
            return subProtocol.Contains("&")
                ? subProtocol.Substring(0, subProtocol.IndexOf("&"))
                : subProtocol;
        }

        private static TimeSpan? ParseDelay(string query)
            => query.Contains("delay10sec")
                ? TimeSpan.FromSeconds(10)
                : query.Contains("delay20sec") ? TimeSpan.FromSeconds(20) : null;
    }
}
