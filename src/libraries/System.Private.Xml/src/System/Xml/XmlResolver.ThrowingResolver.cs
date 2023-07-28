// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Net;
using System.Threading.Tasks;

namespace System.Xml
{
    public abstract partial class XmlResolver
    {
        /// <summary>
        /// Gets an XML resolver which forbids entity resolution.
        /// </summary>
        /// <value>An XML resolver which forbids entity resolution.</value>
        /// <remarks>
        /// Calling <see cref="GetEntity"/> or <see cref="GetEntityAsync"/> on the
        /// <see cref="XmlResolver"/> instance returned by this property is forbidden
        /// and will result in <see cref="XmlException"/> being thrown.
        ///
        /// Use <see cref="ThrowingResolver"/> when external entity resolution must be
        /// prohibited, even when DTD processing is otherwise enabled.
        /// </remarks>
        public static XmlResolver ThrowingResolver => XmlThrowingResolver.s_singleton;

        // An XmlResolver that forbids all external entity resolution.
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
                throw new XmlException(SR.Format(SR.Xml_NullResolver, absoluteUri));
            }

            public override Task<object> GetEntityAsync(Uri absoluteUri, string? role, Type? ofObjectToReturn)
            {
                throw new XmlException(SR.Format(SR.Xml_NullResolver, absoluteUri));
            }
        }
    }
}
