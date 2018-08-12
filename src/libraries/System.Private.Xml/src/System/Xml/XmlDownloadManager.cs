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
    using System.Runtime.CompilerServices;
    using System.Runtime.Versioning;
    using System.Net.Http;

    //
    // XmlDownloadManager
    //
    internal partial class XmlDownloadManager
    {
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
            WebRequest req = CreateWebRequestOrThrowIfRemoved(uri, credentials, proxy, cachePolicy);

            using (WebResponse resp = req.GetResponse())
            using (Stream respStream = resp.GetResponseStream())
            {
                var result = new MemoryStream();
                respStream.CopyTo(result);
                result.Position = 0;
                return result;
            }
        }

        // This method is marked Removable because WebRequest has a lot of dependencies that will bloat
        // self-contained distributions of .NET Apps.
        // This code is statically reachable from any place that uses XmlReaderSettings (i.e. every app that
        // does something XML related is going to have this in their transitive call graph). People rarely need
        // this functionality though.
        [RemovableFeature("System.Xml.XmlUrlResolver.NonFileUrlSupport")] 
        private static WebRequest CreateWebRequestOrThrowIfRemoved(Uri uri, ICredentials credentials, IWebProxy proxy,
            RequestCachePolicy cachePolicy)
        {
            WebRequest req = WebRequest.Create(uri);
            if (credentials != null)
            {
                req.Credentials = credentials;
            }
            if (proxy != null)
            {
                req.Proxy = proxy;
            }
            if (cachePolicy != null)
            {
                req.CachePolicy = cachePolicy;
            }

            return req;
        }
    }
}
