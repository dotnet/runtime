// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections;
using System.Collections.Specialized;

namespace System.Net
{
    internal class HeaderInfoTable
    {
        private static Hashtable HeaderHashTable;
        private static HeaderInfo UnknownHeaderInfo = new HeaderInfo(string.Empty, false, false, false);

        static HeaderInfoTable()
        {
            HeaderInfo[] InfoArray = new HeaderInfo[]
            {
                new HeaderInfo(HttpKnownHeaderNames.Age,                false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Allow,              false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.Accept,             true,   false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.Authorization,      false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.AcceptRanges,       false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.AcceptCharset,      false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.AcceptEncoding,     false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.AcceptLanguage,     false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.Cookie,             false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.Connection,         true,   false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentMD5,         false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentType,        true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.CacheControl,       false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentRange,       false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentLength,      true,   true,   false  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentEncoding,    false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentLanguage,    false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ContentLocation,    false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Date,               true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.ETag,               false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Expect,             true,   false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.Expires,            false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.From,               false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Host,               true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.IfMatch,            false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.IfRange,            false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.IfNoneMatch,        false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.IfModifiedSince,    true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.IfUnmodifiedSince,  false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.KeepAlive,          false,  true,   false  ),
                new HeaderInfo(HttpKnownHeaderNames.Location,           false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.LastModified,       false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.MaxForwards,        false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Pragma,             false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ProxyAuthenticate,  false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ProxyAuthorization, false,  false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.ProxyConnection,    true,   false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.Range,              true,   false,  true  ),
                new HeaderInfo(HttpKnownHeaderNames.Referer,            true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.RetryAfter,         false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Server,             false,  false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.SetCookie,          false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.SetCookie2,         false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.TE,                 false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.Trailer,            false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.TransferEncoding,   true,   true,   true   ),
                new HeaderInfo(HttpKnownHeaderNames.Upgrade,            false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.UserAgent,          true,   false,  false  ),
                new HeaderInfo(HttpKnownHeaderNames.Via,                false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.Vary,               false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.Warning,            false,  false,  true   ),
                new HeaderInfo(HttpKnownHeaderNames.WWWAuthenticate, false, true, true ),
            };

            HeaderHashTable = new Hashtable(InfoArray.Length * 2, CaseInsensitiveAscii.StaticInstance);
            for (int i = 0; i < InfoArray.Length; i++)
            {
                HeaderHashTable[InfoArray[i].HeaderName] = InfoArray[i];
            }
        }

        internal HeaderInfo this[string name]
        {
            get
            {
                HeaderInfo tempHeaderInfo = (HeaderInfo)HeaderHashTable[name];
                if (tempHeaderInfo == null)
                {
                    return UnknownHeaderInfo;
                }
                return tempHeaderInfo;
            }
        }
    }
}
