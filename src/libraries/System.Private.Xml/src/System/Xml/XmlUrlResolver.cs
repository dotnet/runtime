// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.IO;
using System.Net;
using System.Net.Cache;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;

namespace System.Xml
{
    // Resolves external XML resources named by a Uniform Resource Identifier (URI).
    public partial class XmlUrlResolver : XmlResolver
    {
        private ICredentials? _credentials;
        private IWebProxy? _proxy;

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
            if (ofObjectToReturn is null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
            {
                return XmlDownloadManager.GetStream(absoluteUri, _credentials, _proxy);
            }

            throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
        }

        // Maps a URI to an Object containing the actual resource.
        public override async Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
        {
            if (ofObjectToReturn == null || ofObjectToReturn == typeof(Stream) || ofObjectToReturn == typeof(object))
            {
                return await XmlDownloadManager.GetStreamAsync(absoluteUri, _credentials, _proxy).ConfigureAwait(false);
            }

            throw new XmlException(SR.Xml_UnsupportedClass, string.Empty);
        }

        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) =>
            base.ResolveUri(baseUri, relativeUri);
    }
}
