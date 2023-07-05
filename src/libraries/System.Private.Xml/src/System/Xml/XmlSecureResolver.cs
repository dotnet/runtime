// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Security;
using System.Threading.Tasks;

namespace System.Xml
{
    [Obsolete(Obsoletions.XmlSecureResolverMessage, DiagnosticId = Obsoletions.XmlSecureResolverDiagId, UrlFormat = Obsoletions.SharedUrlFormat)]
    public class XmlSecureResolver : XmlResolver
    {
        public XmlSecureResolver(XmlResolver resolver, string? securityUrl)
        {
            // no-op
        }

        public override ICredentials Credentials
        {
            set { /* no-op */ }
        }

        // Forward to ThrowingResolver to get its exception message
        public override object? GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn) => XmlResolver.ThrowingResolver.GetEntity(absoluteUri, role, ofObjectToReturn);

        // Forward to ThrowingResolver to get its exception message
        public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn) => XmlResolver.ThrowingResolver.GetEntityAsync(absoluteUri, role, ofObjectToReturn);

        // An earlier implementation of this type overrode this method, so we'll continue to do so
        // to preserve binary compatibility.
        public override Uri ResolveUri(Uri? baseUri, string? relativeUri) => base.ResolveUri(baseUri, relativeUri);
    }
}
