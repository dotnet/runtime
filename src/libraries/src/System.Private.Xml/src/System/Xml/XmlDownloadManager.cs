// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System;
    using System.IO;
    using System.Security;
    using System.Collections;
    using System.Net;
    using System.Net.Cache;
    using System.Runtime.Versioning;
    using System.Net.Http;

    //
    // XmlDownloadManager
    //
    internal partial class XmlDownloadManager
    {
        private Hashtable _connections;

        internal Stream GetStream(Uri uri, ICredentials credentials, IWebProxy proxy,
            RequestCachePolicy cachePolicy)
        {
            if (uri.Scheme == "file")
            {
                return new FileStream(uri.LocalPath, FileMode.Open, FileAccess.Read, FileShare.Read, 1);
            }
            else
            {
                return GetNonFileStream(uri, credentials, proxy, cachePolicy);
            }
        }

        private Stream GetNonFileStream(Uri uri, ICredentials credentials, IWebProxy proxy,
            RequestCachePolicy cachePolicy)
        {
            HttpClient client = new HttpClient();
            HttpRequestMessage req = new HttpRequestMessage(HttpMethod.Get, uri);

            lock (this)
            {
                if (_connections == null)
                {
                    _connections = new Hashtable();
                }
            }

            HttpResponseMessage resp = client.SendAsync(req).GetAwaiter().GetResult();

            Stream respStream = new MemoryStream();
            resp.Content.CopyToAsync(respStream);
            return respStream;
        }
    }
}
