// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Numerics;
using System.Text;

namespace System.Net.Test.Common
{
    public static class QPackDecoder
    {
        public static (int bytesConsumed, int requiredInsertCount, int deltaBase) DecodePrefix(ReadOnlySpan<byte> buffer)
        {
            if (buffer[0] != 0x00 && buffer[1] != 0x00)
            {
                throw new Exception("QPack dynamic table is not yet supported.");
            }

            return (2, 0, 0);
        }
        public static (int bytesConsumed, HttpHeaderData) DecodeHeader(ReadOnlySpan<byte> buffer)
        {
            int firstByte = buffer[0];

            // Indexed Header Field, dynamic.
            if ((firstByte & 0b1100_0000) == 0b1000_0000)
            {
                throw new Exception("QPack dynamic table is not yet supported.");
            }

            // Indexed Header Field, static.
            if ((firstByte & 0b1100_0000) == 0b1100_0000)
            {
                (int bytesConsumed, int staticIndex) = DecodeInteger(buffer, 0b1100_0000);

                var staticHeader = s_staticTable[staticIndex];
                var header = new HttpHeaderData(staticHeader.Name, staticHeader.Value, raw: buffer.Slice(0, bytesConsumed).ToArray());

                return (bytesConsumed, header);
            }

            // Indexed Header Field With Post-Base Index
            if ((firstByte & 0b1111_0000) == 0b0001_0000)
            {
                throw new Exception("QPack dynamic table is not yet supported.");
            }

            // Literal Header Field With Name Reference
            if ((firstByte & 0b1100_0000) == 0b0100_0000)
            {
                if ((firstByte & 0b0001_0000) != 0)
                {
                    throw new Exception("QPack dynamic table is not yet supported.");
                }

                (int nameLength, int staticIndex) = DecodeInteger(buffer, 0b1111_0000);
                (int valueLength, string value) = DecodeString(buffer.Slice(nameLength), 0b1000_0000);

                int headerLength = nameLength + valueLength;
                var header = new HttpHeaderData(s_staticTable[staticIndex].Name, value, raw: buffer.Slice(0, headerLength).ToArray());

                return (headerLength, header);
            }

            // Literal Header Field With Post-Base Name Reference.
            if ((firstByte & 0b1111_0000) == 0b0000_0000)
            {
                throw new Exception("QPack dynamic table is not yet supported.");
            }

            // Literal Header Field Without Name Reference.
            if ((firstByte & 0b1110_0000) == 0b0010_0000)
            {
                (int nameLength, string name) = DecodeString(buffer, 0b1111_1000);
                (int valueLength, string value) = DecodeString(buffer.Slice(nameLength), 0b1000_0000);

                int headerLength = nameLength + valueLength;
                var header = new HttpHeaderData(name, value, raw: buffer.Slice(0, headerLength).ToArray());

                return (headerLength, header);
            }

            throw new Exception("Invalid QPack.");
        }

        private static (int bytesConsumed, string value) DecodeString(ReadOnlySpan<byte> buffer, byte prefixMask)
        {
            bool huffman = (buffer[0] & (1 << BitOperations.TrailingZeroCount(prefixMask))) != 0;

            if (huffman)
            {
                throw new Exception("Huffman coding not yet supported.");
            }

            (int nameLength, int stringLength) = DecodeInteger(buffer, prefixMask);
            string value = Encoding.ASCII.GetString(buffer.Slice(nameLength));

            return (nameLength + stringLength, value);
        }

        public static (int bytesConsumed, int value) DecodeInteger(ReadOnlySpan<byte> headerBlock, byte prefixMask)
        {
            int value = headerBlock[0] & prefixMask;
            if (value != prefixMask)
            {
                return (1, value);
            }

            byte b = headerBlock[1];
            if ((b & 0b10000000) != 0)
            {
                throw new Exception("long integers currently not supported");
            }

            return (2, prefixMask + b);
        }

        private static readonly HttpHeaderData[] s_staticTable = new HttpHeaderData[]
        {
            new HttpHeaderData(":authority", ""), // 0
            new HttpHeaderData(":path", "/"), // 1
            new HttpHeaderData("age", "0"), // 2
            new HttpHeaderData("content-disposition", ""),
            new HttpHeaderData("content-length", "0"),
            new HttpHeaderData("cookie", ""),
            new HttpHeaderData("date", ""),
            new HttpHeaderData("etag", ""),
            new HttpHeaderData("if-modified-since", ""),
            new HttpHeaderData("if-none-match", ""),
            new HttpHeaderData("last-modified", ""), // 10
            new HttpHeaderData("link", ""),
            new HttpHeaderData("location", ""),
            new HttpHeaderData("referer", ""),
            new HttpHeaderData("set-cookie", ""),
            new HttpHeaderData(":method", "CONNECT"),
            new HttpHeaderData(":method", "DELETE"),
            new HttpHeaderData(":method", "GET"),
            new HttpHeaderData(":method", "HEAD"),
            new HttpHeaderData(":method", "OPTIONS"),
            new HttpHeaderData(":method", "POST"), // 20
            new HttpHeaderData(":method", "PUT"),
            new HttpHeaderData(":scheme", "http"),
            new HttpHeaderData(":scheme", "https"),
            new HttpHeaderData(":status", "103"),
            new HttpHeaderData(":status", "200"),
            new HttpHeaderData(":status", "304"),
            new HttpHeaderData(":status", "404"),
            new HttpHeaderData(":status", "503"),
            new HttpHeaderData("accept", "*/*"),
            new HttpHeaderData("accept", "application/dns-message"), // 30
            new HttpHeaderData("accept-encoding", "gzip, deflate, br"),
            new HttpHeaderData("accept-ranges", "bytes"),
            new HttpHeaderData("access-control-allow-headers", "cache-control"),
            new HttpHeaderData("access-control-allow-origin", "content-type"),
            new HttpHeaderData("access-control-allow-origin", "*"),
            new HttpHeaderData("cache-control", "max-age=0"),
            new HttpHeaderData("cache-control", "max-age=2592000"),
            new HttpHeaderData("cache-control", "max-age=604800"),
            new HttpHeaderData("cache-control", "no-cache"),
            new HttpHeaderData("cache-control", "no-store"), // 40
            new HttpHeaderData("cache-control", "public, max-age=31536000"),
            new HttpHeaderData("content-encoding", "br"),
            new HttpHeaderData("content-encoding", "gzip"),
            new HttpHeaderData("content-type", "application/dns-message"),
            new HttpHeaderData("content-type", "application/javascript"),
            new HttpHeaderData("content-type", "application/json"),
            new HttpHeaderData("content-type", "application/x-www-form-urlencoded"),
            new HttpHeaderData("content-type", "image/gif"),
            new HttpHeaderData("content-type", "image/jpeg"),
            new HttpHeaderData("content-type", "image/png"), // 50
            new HttpHeaderData("content-type", "text/css"),
            new HttpHeaderData("content-type", "text/html; charset=utf-8"),
            new HttpHeaderData("content-type", "text/plain"),
            new HttpHeaderData("content-type", "text/plain;charset=utf-8"),
            new HttpHeaderData("range", "bytes=0-"),
            new HttpHeaderData("strict-transport-security", "max-age=31536000"),
            new HttpHeaderData("strict-transport-security", "max-age=31536000;includesubdomains"), // TODO confirm spaces here don't matter?
            new HttpHeaderData("strict-transport-security", "max-age=31536000;includesubdomains; preload"),
            new HttpHeaderData("vary", "accept-encoding"),
            new HttpHeaderData("vary", "origin"), // 60
            new HttpHeaderData("x-content-type-options", "nosniff"),
            new HttpHeaderData("x-xss-protection", "1; mode=block"),
            new HttpHeaderData(":status", "100"),
            new HttpHeaderData(":status", "204"),
            new HttpHeaderData(":status", "206"),
            new HttpHeaderData(":status", "302"),
            new HttpHeaderData(":status", "400"),
            new HttpHeaderData(":status", "403"),
            new HttpHeaderData(":status", "421"),
            new HttpHeaderData(":status", "425"), // 70
            new HttpHeaderData(":status", "500"),
            new HttpHeaderData("accept-language", ""),
            new HttpHeaderData("access-control-allow-credentials", "FALSE"),
            new HttpHeaderData("access-control-allow-credentials", "TRUE"),
            new HttpHeaderData("access-control-allow-headers", "*"),
            new HttpHeaderData("access-control-allow-methods", "get"),
            new HttpHeaderData("access-control-allow-methods", "get, post, options"),
            new HttpHeaderData("access-control-allow-methods", "options"),
            new HttpHeaderData("access-control-expose-headers", "content-length"),
            new HttpHeaderData("access-control-request-headers", "content-type"), // 80
            new HttpHeaderData("access-control-request-method", "get"),
            new HttpHeaderData("access-control-request-method", "post"),
            new HttpHeaderData("alt-svc", "clear"),
            new HttpHeaderData("authorization", ""),
            new HttpHeaderData("content-security-policy", "script-src 'none'; object-src 'none'; base-uri 'none'"),
            new HttpHeaderData("early-data", "1"),
            new HttpHeaderData("expect-ct", ""),
            new HttpHeaderData("forwarded", ""),
            new HttpHeaderData("if-range", ""),
            new HttpHeaderData("origin", ""), // 90
            new HttpHeaderData("purpose", "prefetch"),
            new HttpHeaderData("server", ""),
            new HttpHeaderData("timing-allow-origin", "*"),
            new HttpHeaderData("upgrading-insecure-requests", "1"),
            new HttpHeaderData("user-agent", ""),
            new HttpHeaderData("x-forwarded-for", ""),
            new HttpHeaderData("x-frame-options", "deny"),
            new HttpHeaderData("x-frame-options", "sameorigin"),
        };
    }

}
