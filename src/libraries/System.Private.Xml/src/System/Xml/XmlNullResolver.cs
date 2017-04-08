// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Net;

namespace System.Xml
{
    internal class XmlNullResolver : XmlResolver
    {
        public static readonly XmlNullResolver Singleton = new XmlNullResolver();

        // Private constructor ensures existing only one instance of XmlNullResolver
        private XmlNullResolver() { }

        public override Object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn)
        {
            throw new XmlException(SR.Xml_NullResolver, string.Empty);
        }

        public override ICredentials Credentials
        {
            set { /* Do nothing */ }
        }
    }
}
