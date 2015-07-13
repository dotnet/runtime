// Copyright (c) Microsoft. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections;
using System.Collections.Specialized;
using System.Globalization;

namespace System.Net
{
    internal class HeaderInfoTable
    {
        private static Hashtable s_headerHashTable;
        private static HeaderInfo s_unknownHeaderInfo = new HeaderInfo(string.Empty, false, false, false, s_singleParser);
        private static HeaderParser s_singleParser = new HeaderParser(ParseSingleValue);
        private static HeaderParser s_multiParser = new HeaderParser(ParseMultiValue);

        private static string[] ParseSingleValue(string value)
        {
            return new string[1] { value };
        }

        private static string[] ParseMultiValue(string value)
        {
            StringCollection tempStringCollection = new StringCollection();

            bool inquote = false;
            int chIndex = 0;
            char[] vp = new char[value.Length];
            string singleValue;

            for (int i = 0; i < value.Length; i++)
            {
                if (value[i] == '\"')
                {
                    inquote = !inquote;
                }
                else if ((value[i] == ',') && !inquote)
                {
                    singleValue = new string(vp, 0, chIndex);
                    tempStringCollection.Add(singleValue.Trim());
                    chIndex = 0;
                    continue;
                }
                vp[chIndex++] = value[i];
            }

            // Now add the last of the header values to the stringtable.

            if (chIndex != 0)
            {
                singleValue = new string(vp, 0, chIndex);
                tempStringCollection.Add(singleValue.Trim());
            }

            string[] stringArray = new string[tempStringCollection.Count];
            tempStringCollection.CopyTo(stringArray, 0);
            return stringArray;
        }

        static HeaderInfoTable()
        {
            HeaderInfo[] InfoArray = new HeaderInfo[] {
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
                new HeaderInfo(HttpKnownHeaderNames.SetCookie,          false,  false,  true,   s_multiParser),
                new HeaderInfo(HttpKnownHeaderNames.SetCookie2,         false,  false,  true,   s_multiParser),
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
