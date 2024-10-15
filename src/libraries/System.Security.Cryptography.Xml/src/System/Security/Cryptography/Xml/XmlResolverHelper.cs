// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;
using System.Xml;

namespace System.Security.Cryptography.Xml
{
    internal static class XmlResolverHelper
    {
        internal static XmlResolver GetThrowingResolver()
        {
#if NET
            return XmlResolver.ThrowingResolver;
#else
            return XmlThrowingResolver.s_singleton;
#endif
        }

#if !NET
        // An XmlResolver that forbids all external entity resolution.
        // (Copied from XmlResolver.ThrowingResolver.cs.)
        private sealed class XmlThrowingResolver : XmlResolver
        {
            internal static readonly XmlThrowingResolver s_singleton = new();

            // Private constructor ensures existing only one instance of XmlThrowingResolver
            private XmlThrowingResolver() { }

            public override ICredentials Credentials
            {
                set { /* Do nothing */ }
            }

            public override object GetEntity(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                throw new XmlException(SR.Cryptography_Xml_EntityResolutionNotSupported);
            }

            public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                throw new XmlException(SR.Cryptography_Xml_EntityResolutionNotSupported);
            }
        }
#endif
    }
}
