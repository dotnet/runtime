// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace System.Net.Test.Common
{
    public struct WebSocketEchoOptions
    {
        public bool ReplyWithPartialMessages { get; set; }
        public bool ReplyWithEnhancedCloseMessage { get; set; }
        public string SubProtocol { get; set; }
        public TimeSpan? Delay { get; set; }

        public static WebSocketEchoOptions Parse(string query)
        {
            if (query is null or "" or "?")
            {
                return default;
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
