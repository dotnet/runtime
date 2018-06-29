// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Threading;
using System.Net;
using System.Net.Cache;
using System.Runtime.Versioning;

namespace System.Xml
{
    // Resolves external XML resources named by a Uniform Resource Identifier (URI).
    public partial class XmlUrlResolver : XmlResolver
    {
        private static object s_DownloadManager;
        private ICredentials _credentials;
        private IWebProxy _proxy;
        private RequestCachePolicy _cachePolicy;

        private static XmlDownloadManager DownloadManager
        {
            get
            {
                if (s_DownloadManager == null)
                {
                    object dm = new XmlDownloadManager();
                    Interlocked.CompareExchange<object>(ref s_DownloadManager, dm, null);
                }
                return (XmlDownloadManager)s_DownloadManager;
            }
        }

        // Construction

        // Creates a new instance of the XmlUrlResolver class.
        public XmlUrlResolver()
        {
        }

        public override ICredentials Credentials
        {
            set { _credentials = value; }
        }

        public IWebProxy Proxy
        {
            set { _proxy = value; }
        }

        public RequestCachePolicy CachePolicy
        {
            set { _cachePolicy = value; }
        }

        // Resource resolution

        // Maps a URI to an Object containing the actual resource.
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            if (ofObjectToReturn == null || ofObjectToReturn == typeof(System.IO.Stream) || ofObjectToReturn == typeof(object))
            {
                return DownloadManager.GetStream(absoluteUri, _credentials, _proxy, _cachePolicy);
            }
            else
            {
                throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
            }
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            return base.ResolveUri(baseUri, relativeUri);
        }
    }
}
