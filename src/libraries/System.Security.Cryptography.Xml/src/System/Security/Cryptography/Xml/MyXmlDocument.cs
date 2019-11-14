// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Security.Cryptography.Xml
{
    internal class MyXmlDocument : XmlDocument
    {
        protected override XmlAttribute CreateDefaultAttribute(string prefix, string localName, string namespaceURI)
        {
            return CreateAttribute(prefix, localName, namespaceURI);
        }
    }
}
