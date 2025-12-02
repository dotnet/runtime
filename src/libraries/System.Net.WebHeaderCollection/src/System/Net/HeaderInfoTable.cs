// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace System.Net
{
    internal sealed class HeaderInfoTable
    {
        private static readonly Func<string, string[]> s_singleParser = value => new[] { value };
        private static readonly Func<string, string[]> s_multiParser = value => ParseValueHelper(value, isSetCookie: false);
        private static readonly Func<string, string[]> s_setCookieParser = value => ParseValueHelper(value, isSetCookie: true);
        private static readonly HeaderInfo s_unknownHeaderInfo = new HeaderInfo(string.Empty, false, false, false, s_singleParser);
        private static readonly Hashtable s_headerHashTable = CreateHeaderHashtable();

        private static string[] ParseValueHelper(string value, bool isSetCookie)
        {
            // RFC 6265: (for Set-Cookie header)
            // If the name-value-pair string lacks a %x3D ("=") character, ignore the set-cookie-string entirely.
            if (isSetCookie && (!value.Contains('='))) return Array.Empty<string>();

            var tempStringCollection = new List<string>();

            bool inquote = false;
            int startIndex = 0;
            int length = 0;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\"')
                {
                    inquote = !inquote;
                }
                else if ((value[i] == ',') && !inquote)
                {
                    ReadOnlySpan<char> singleValue = value.AsSpan(startIndex, length).Trim();
                    if (!isSetCookie || !IsDuringExpiresAttributeParsing(singleValue))
                    {
                        tempStringCollection.Add(singleValue.ToString());
                        startIndex = i + 1;
                        length = 0;
                        continue;
                    }
                }
                length++;
            }

            // Now add the last of the header values to the string table.
            if (startIndex < value.Length && length > 0)
            {
                tempStringCollection.Add(value.AsSpan(startIndex, length).Trim().ToString());
            }

            return tempStringCollection.ToArray();
        }

        // This method is to check if we are in the middle of parsing the Expires attribute
        // for Set-Cookie header. It needs to check two conditions: 1. If current attribute
        // is Expires. 2. Have we finished parsing it yet. Because the Expires attribute
        // will contain exactly one comma, no comma means we are still parsing it.
        private static bool IsDuringExpiresAttributeParsing(ReadOnlySpan<char> singleValue)
        {
            int semiPos = singleValue.LastIndexOf(';');
            if (semiPos >= 0)
            {
                ReadOnlySpan<char> lastElement = singleValue.Slice(semiPos + 1);
                if (!lastElement.Contains(','))
                {
                    int equalsPos = lastElement.IndexOf('=');
                    if (equalsPos < 0)
                    {
                        equalsPos = lastElement.Length;
                    }

                    return lastElement.Slice(0, equalsPos).Trim().Equals("Expires", StringComparison.OrdinalIgnoreCase);
                }
            }

            return false;
        }

        private static Hashtable CreateHeaderHashtable()
        {
            const int Items = 52;
            var headers = new Hashtable(Items * 2, StringComparer.OrdinalIgnoreCase)
            {
                { HttpKnownHeaderNames.Age,                new HeaderInfo(HttpKnownHeaderNames.Age,                false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Allow,              new HeaderInfo(HttpKnownHeaderNames.Allow,              false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Accept,             new HeaderInfo(HttpKnownHeaderNames.Accept,             true,   false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Authorization,      new HeaderInfo(HttpKnownHeaderNames.Authorization,      false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.AcceptRanges,       new HeaderInfo(HttpKnownHeaderNames.AcceptRanges,       false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.AcceptCharset,      new HeaderInfo(HttpKnownHeaderNames.AcceptCharset,      false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.AcceptEncoding,     new HeaderInfo(HttpKnownHeaderNames.AcceptEncoding,     false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.AcceptLanguage,     new HeaderInfo(HttpKnownHeaderNames.AcceptLanguage,     false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Cookie,             new HeaderInfo(HttpKnownHeaderNames.Cookie,             false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Connection,         new HeaderInfo(HttpKnownHeaderNames.Connection,         true,   false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ContentMD5,         new HeaderInfo(HttpKnownHeaderNames.ContentMD5,         false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.ContentType,        new HeaderInfo(HttpKnownHeaderNames.ContentType,        true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.CacheControl,       new HeaderInfo(HttpKnownHeaderNames.CacheControl,       false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ContentRange,       new HeaderInfo(HttpKnownHeaderNames.ContentRange,       false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.ContentLength,      new HeaderInfo(HttpKnownHeaderNames.ContentLength,      true,   true,   false,  s_singleParser) },
                { HttpKnownHeaderNames.ContentEncoding,    new HeaderInfo(HttpKnownHeaderNames.ContentEncoding,    false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ContentLanguage,    new HeaderInfo(HttpKnownHeaderNames.ContentLanguage,    false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ContentLocation,    new HeaderInfo(HttpKnownHeaderNames.ContentLocation,    false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Date,               new HeaderInfo(HttpKnownHeaderNames.Date,               true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.ETag,               new HeaderInfo(HttpKnownHeaderNames.ETag,               false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Expect,             new HeaderInfo(HttpKnownHeaderNames.Expect,             true,   false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Expires,            new HeaderInfo(HttpKnownHeaderNames.Expires,            false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.From,               new HeaderInfo(HttpKnownHeaderNames.From,               false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Host,               new HeaderInfo(HttpKnownHeaderNames.Host,               true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.IfMatch,            new HeaderInfo(HttpKnownHeaderNames.IfMatch,            false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.IfRange,            new HeaderInfo(HttpKnownHeaderNames.IfRange,            false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.IfNoneMatch,        new HeaderInfo(HttpKnownHeaderNames.IfNoneMatch,        false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.IfModifiedSince,    new HeaderInfo(HttpKnownHeaderNames.IfModifiedSince,    true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.IfUnmodifiedSince,  new HeaderInfo(HttpKnownHeaderNames.IfUnmodifiedSince,  false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.KeepAlive,          new HeaderInfo(HttpKnownHeaderNames.KeepAlive,          false,  true,   false,  s_singleParser) },
                { HttpKnownHeaderNames.Location,           new HeaderInfo(HttpKnownHeaderNames.Location,           false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.LastModified,       new HeaderInfo(HttpKnownHeaderNames.LastModified,       false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.MaxForwards,        new HeaderInfo(HttpKnownHeaderNames.MaxForwards,        false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Pragma,             new HeaderInfo(HttpKnownHeaderNames.Pragma,             false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ProxyAuthenticate,  new HeaderInfo(HttpKnownHeaderNames.ProxyAuthenticate,  false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ProxyAuthorization, new HeaderInfo(HttpKnownHeaderNames.ProxyAuthorization, false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.ProxyConnection,    new HeaderInfo(HttpKnownHeaderNames.ProxyConnection,    true,   false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Range,              new HeaderInfo(HttpKnownHeaderNames.Range,              true,   false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Referer,            new HeaderInfo(HttpKnownHeaderNames.Referer,            true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.RetryAfter,         new HeaderInfo(HttpKnownHeaderNames.RetryAfter,         false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Server,             new HeaderInfo(HttpKnownHeaderNames.Server,             false,  false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.SetCookie,          new HeaderInfo(HttpKnownHeaderNames.SetCookie,          false,  false,  true,   s_setCookieParser) },
                { HttpKnownHeaderNames.SetCookie2,         new HeaderInfo(HttpKnownHeaderNames.SetCookie2,         false,  false,  true,   s_setCookieParser) },
                { HttpKnownHeaderNames.TE,                 new HeaderInfo(HttpKnownHeaderNames.TE,                 false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Trailer,            new HeaderInfo(HttpKnownHeaderNames.Trailer,            false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.TransferEncoding,   new HeaderInfo(HttpKnownHeaderNames.TransferEncoding,   true,   true,   true,   s_multiParser) },
                { HttpKnownHeaderNames.Upgrade,            new HeaderInfo(HttpKnownHeaderNames.Upgrade,            false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.UserAgent,          new HeaderInfo(HttpKnownHeaderNames.UserAgent,          true,   false,  false,  s_singleParser) },
                { HttpKnownHeaderNames.Via,                new HeaderInfo(HttpKnownHeaderNames.Via,                false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Vary,               new HeaderInfo(HttpKnownHeaderNames.Vary,               false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.Warning,            new HeaderInfo(HttpKnownHeaderNames.Warning,            false,  false,  true,   s_multiParser) },
                { HttpKnownHeaderNames.WWWAuthenticate,    new HeaderInfo(HttpKnownHeaderNames.WWWAuthenticate,    false,  true,   true,   s_singleParser) }
            };
            Debug.Assert(headers.Count == Items);
            return headers;
        }

        internal HeaderInfo this[string name]
        {
            get
            {
                HeaderInfo? tempHeaderInfo = (HeaderInfo?)s_headerHashTable[name];
                if (tempHeaderInfo == null)
                {
                    return s_unknownHeaderInfo;
                }
                return tempHeaderInfo;
            }
        }
    }
}
