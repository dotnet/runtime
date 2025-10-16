// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Net.Test.Common
{
    public readonly struct WebSocketEchoOptions
    {
        public static class EchoQueryKey
        {
            public const string ReplyWithPartialMessages = "replyWithPartialMessages";
            public const string ReplyWithEnhancedCloseMessage = "replyWithEnhancedCloseMessage";
            public const string SubProtocol = "subprotocol";
            public const string Delay10Sec = "delay10sec";
            public const string Delay20Sec = "delay20sec";
        }

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
                ReplyWithPartialMessages = query.Contains(EchoQueryKey.ReplyWithPartialMessages),
                ReplyWithEnhancedCloseMessage = query.Contains(EchoQueryKey.ReplyWithEnhancedCloseMessage),
                SubProtocol = ParseSubProtocol(query),
                Delay = ParseDelay(query)
            };
        }

        private static string ParseSubProtocol(string query)
        {
            const string subProtocolEquals = $"{EchoQueryKey.SubProtocol}=";

            var index = query.IndexOf(subProtocolEquals);
            if (index == -1)
            {
                return null;
            }

            var subProtocol = query.Substring(index + subProtocolEquals.Length);
            return subProtocol.Contains("&")
                ? subProtocol.Substring(0, subProtocol.IndexOf("&"))
                : subProtocol;
        }

        private static TimeSpan? ParseDelay(string query)
            => query.Contains(EchoQueryKey.Delay10Sec)
                ? TimeSpan.FromSeconds(10)
                : query.Contains(EchoQueryKey.Delay20Sec) ? TimeSpan.FromSeconds(20) : null;
    }
}
