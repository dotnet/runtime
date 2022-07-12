// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    // This type masks out System.Xml.XmlSecureResolver by being in the local namespace.
    internal sealed class XmlSecureResolver : XmlResolver
    {
        internal XmlSecureResolver(XmlResolver resolver, string securityUrl)
        {
        }

        // Simulate .NET Framework's CAS behavior by throwing SecurityException.
        // Unlike .NET Framework's implementation, the securityUrl ctor parameter has no effect.
        public override object GetEntity(Uri absoluteUri, string role, Type ofObjectToReturn) =>
            throw new SecurityException();
    }
}
