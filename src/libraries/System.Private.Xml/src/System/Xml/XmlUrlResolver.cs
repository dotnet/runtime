// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Threading;
using System.Net;
using System.Net.Cache;
using System.Runtime.Versioning;

namespace System.Xml
{
    // Resolves external XML resources named by a Uniform Resource Identifier (URI).
    public partial class XmlUrlResolver : XmlResolver
    {
        private static XmlDownloadManager? s_downloadManager;
        private ICredentials? _credentials;
        private IWebProxy? _proxy;

        private static XmlDownloadManager DownloadManager =>
            s_downloadManager ??
            Interlocked.CompareExchange(ref s_downloadManager, new XmlDownloadManager(), null) ??
            s_downloadManager;

        public XmlUrlResolver() { }

        [UnsupportedOSPlatform("browser")]
        public override ICredentials? Credentials
        {
            set { _credentials = value; }
        }

        [UnsupportedOSPlatform("browser")]
        public IWebProxy? Proxy
        {
            set { _proxy = value; }
        }

        public RequestCachePolicy CachePolicy
        {
            set { } // nop, as caching isn't implemented
        }

        // Maps a URI to an Object containing the actual resource.
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (ofObjectToReturn is null || ofObjectToReturn == typeof(System.IO.Stream) || ofObjectToReturn == typeof(object))
            {
                return XmlDownloadManager.GetStream(absoluteUri, _credentials, _proxy);
            }

            throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            base.ResolveUri(baseUri, relativeUri);
    }
}
