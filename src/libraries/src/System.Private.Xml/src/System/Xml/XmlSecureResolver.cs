// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.Net;
    using System.Security;
    using System.Runtime.Versioning;

    public partial class XmlSecureResolver : XmlResolver
    {
        private XmlResolver _resolver;

        public XmlSecureResolver(XmlResolver resolver, string securityUrl)
        {
            _resolver = resolver;
        }

        public override ICredentials Credentials
        {
            set { _resolver.Credentials = value; }
        }

        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            return _resolver.GetEntity(absoluteUri, role, ofObjectToReturn);
        }

        public override Uri ResolveUri(Uri baseUri, string relativeUri)
        {
            return _resolver.ResolveUri(baseUri, relativeUri);
        }
    }
}
