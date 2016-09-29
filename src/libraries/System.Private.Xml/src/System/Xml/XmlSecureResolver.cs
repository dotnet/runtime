// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml
{
    using System.Net;
    using System.Security;
    using System.Security.Policy;
    using System.Runtime.Versioning;

    public partial class XmlSecureResolver : XmlResolver
    {
        private XmlResolver _resolver;

        public XmlSecureResolver(XmlResolver resolver, string securityUrl)
        {
            _resolver = resolver;
        }

#if CAS
        internal XmlSecureResolver(XmlResolver resolver, Evidence evidence) : this(resolver, SecurityManager.GetStandardSandbox(evidence)) { }

        internal XmlSecureResolver(XmlResolver resolver, PermissionSet permissionSet)
        {
            _resolver = resolver;
        }
#endif

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

#if CAS
        internal static Evidence CreateEvidenceForUrl(string securityUrl)
        {
            return new Evidence();
        }
#endif

        [Serializable]
        private class UncDirectory
        {
            private string _uncDir;

            public UncDirectory(string uncDirectory)
            {
                _uncDir = uncDirectory;
            }

            private SecurityElement ToXml()
            {
                SecurityElement root = new SecurityElement("System.Xml.XmlSecureResolver");
                root.AddAttribute("version", "1");
                root.AddChild(new SecurityElement("UncDirectory", _uncDir));
                return root;
            }

            public override string ToString()
            {
                return ToXml().ToString();
            }
        }
    }
}
