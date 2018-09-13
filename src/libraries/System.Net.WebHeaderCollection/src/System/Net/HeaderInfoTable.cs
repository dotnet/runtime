// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

namespace System.Net
{
    internal class HeaderInfoTable
    {
        private static readonly Func<string, string[]> s_singleParser = value => new[] { value };
        private static readonly Func<string, string[]> s_multiParser = value => ParseValueHelper(value, isSetCookie: false);
        private static readonly Func<string, string[]> s_setCookieParser = value => ParseValueHelper(value, isSetCookie: true);
        private static readonly HeaderInfo s_unknownHeaderInfo = new HeaderInfo(string.Empty, false, false, false, s_singleParser);
        private static readonly Hashtable s_headerHashTable;

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
                    string singleValue = value.SubstringTrim(startIndex, length);
                    if (!isSetCookie || !IsDuringExpiresAttributeParsing(singleValue))
                    {
                        tempStringCollection.Add(singleValue);
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
                tempStringCollection.Add(value.SubstringTrim(startIndex, length));
            }

            return tempStringCollection.ToArray();
        }

        // This method is to check if we are in the middle of parsing the Expires attribute
        // for Set-Cookie header. It needs to check two conditions: 1. If current attribute
        // is Expires. 2. Have we finished parsing it yet. Because the Expires attribute
        // will contain exactly one comma, no comma means we are still parsing it.
        private static bool IsDuringExpiresAttributeParsing(string singleValue)
        {
            // Current cookie doesn't contain any attributes.
            if (!singleValue.Contains(';')) return false;

            string lastElement = singleValue.Split(';').Last();
            bool noComma = !lastElement.Contains(',');

            string lastAttribute = lastElement.Split('=')[0].Trim();
            bool isExpires = string.Equals(lastAttribute, "Expires", StringComparison.OrdinalIgnoreCase);

            return (isExpires && noComma);
        }

        static HeaderInfoTable()
        {
            HeaderInfo[] InfoArray = new HeaderInfo[]
            {
                new HeaderInfo(HttpKnownHeaderNames.Age,                false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Allow,              false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Accept,             true,   false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Authorization,      false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.AcceptRanges,       false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.AcceptCharset,      false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.AcceptEncoding,     false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.AcceptLanguage,     false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Cookie,             false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Connection,         true,   false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentMD5,         false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentType,        true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.CacheControl,       false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentRange,       false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentLength,      true,   true,   false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentEncoding,    false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentLanguage,    false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ContentLocation,    false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Date,               true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.ETag,               false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Expect,             true,   false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Expires,            false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.From,               false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Host,               true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.IfMatch,            false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.IfRange,            false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.IfNoneMatch,        false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.IfModifiedSince,    true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.IfUnmodifiedSince,  false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.KeepAlive,          false,  true,   false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Location,           false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.LastModified,       false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.MaxForwards,        false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Pragma,             false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ProxyAuthenticate,  false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ProxyAuthorization, false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.ProxyConnection,    true,   false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Range,              true,   false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Referer,            true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.RetryAfter,         false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Server,             false,  false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.SetCookie,          false,  false,  true,   s_setCookieParser),
                new HeaderInfo(HttpKnownHeaderNames.SetCookie2,         false,  false,  true,   s_setCookieParser),
                new HeaderInfo(HttpKnownHeaderNames.TE,                 false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Trailer,            false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.TransferEncoding,   true,   true,   true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Upgrade,            false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.UserAgent,          true,   false,  false,  s_singleParser),
                new HeaderInfo(HttpKnownHeaderNames.Via,                false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Vary,               false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.Warning,            false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.WWWAuthenticate,    false,  true,   true,   s_singleParser)
            };

            s_headerHashTable = new Hashtable(InfoArray.Length * 2, CaseInsensitiveAscii.StaticInstance);
            for (int i = 0; i < InfoArray.Length; i++)
            {
                s_headerHashTable[InfoArray[i].HeaderName] = InfoArray[i];
            }
        }

        internal HeaderInfo this[string name]
        {
            get
            {
                HeaderInfo tempHeaderInfo = (HeaderInfo)s_headerHashTable[name];
                if (tempHeaderInfo == null)
                {
                    return s_unknownHeaderInfo;
                }
                return tempHeaderInfo;
            }
        }
    }
}
