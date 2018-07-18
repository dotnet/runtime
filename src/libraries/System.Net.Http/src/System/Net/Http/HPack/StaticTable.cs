// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Text;

namespace System.Net.Http.HPack
{
    internal static class StaticTable
    {
        public static int Count => s_staticTable.Length;

        public static HeaderField Get(int index) => s_staticTable[index];

        private static readonly HeaderField[] s_staticTable = new HeaderField[]
        {
            CreateHeaderField(":authority", ""),
            CreateHeaderField(":method", "GET"),
            CreateHeaderField(":method", "POST"),
            CreateHeaderField(":path", "/"),
            CreateHeaderField(":path", "/index.html"),
            CreateHeaderField(":scheme", "http"),
            CreateHeaderField(":scheme", "https"),
            CreateHeaderField(":status", "200"),
            CreateHeaderField(":status", "204"),
            CreateHeaderField(":status", "206"),
            CreateHeaderField(":status", "304"),
            CreateHeaderField(":status", "400"),
            CreateHeaderField(":status", "404"),
            CreateHeaderField(":status", "500"),
            CreateHeaderField("accept-charset", ""),
            CreateHeaderField("accept-encoding", "gzip, deflate"),
            CreateHeaderField("accept-language", ""),
            CreateHeaderField("accept-ranges", ""),
            CreateHeaderField("accept", ""),
            CreateHeaderField("access-control-allow-origin", ""),
            CreateHeaderField("age", ""),
            CreateHeaderField("allow", ""),
            CreateHeaderField("authorization", ""),
            CreateHeaderField("cache-control", ""),
            CreateHeaderField("content-disposition", ""),
            CreateHeaderField("content-encoding", ""),
            CreateHeaderField("content-language", ""),
            CreateHeaderField("content-length", ""),
            CreateHeaderField("content-location", ""),
            CreateHeaderField("content-range", ""),
            CreateHeaderField("content-type", ""),
            CreateHeaderField("cookie", ""),
            CreateHeaderField("date", ""),
            CreateHeaderField("etag", ""),
            CreateHeaderField("expect", ""),
            CreateHeaderField("expires", ""),
            CreateHeaderField("from", ""),
            CreateHeaderField("host", ""),
            CreateHeaderField("if-match", ""),
            CreateHeaderField("if-modified-since", ""),
            CreateHeaderField("if-none-match", ""),
            CreateHeaderField("if-range", ""),
            CreateHeaderField("if-unmodifiedsince", ""),
            CreateHeaderField("last-modified", ""),
            CreateHeaderField("link", ""),
            CreateHeaderField("location", ""),
            CreateHeaderField("max-forwards", ""),
            CreateHeaderField("proxy-authenticate", ""),
            CreateHeaderField("proxy-authorization", ""),
            CreateHeaderField("range", ""),
            CreateHeaderField("referer", ""),
            CreateHeaderField("refresh", ""),
            CreateHeaderField("retry-after", ""),
            CreateHeaderField("server", ""),
            CreateHeaderField("set-cookie", ""),
            CreateHeaderField("strict-transport-security", ""),
            CreateHeaderField("transfer-encoding", ""),
            CreateHeaderField("user-agent", ""),
            CreateHeaderField("vary", ""),
            CreateHeaderField("via", ""),
            CreateHeaderField("www-authenticate", "")
        };

        // TODO: The HeaderField constructor will allocate and copy again. We should avoid this.
        // Tackle as part of header table allocation strategy in general (see note in HeaderField constructor).

        private static HeaderField CreateHeaderField(string name, string value)
            => new HeaderField(Encoding.ASCII.GetBytes(name), Encoding.ASCII.GetBytes(value));
    }
}
