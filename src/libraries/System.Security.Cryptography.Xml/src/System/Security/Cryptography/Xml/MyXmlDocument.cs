// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.Win32;
using System.Collections;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using System.Security.Permissions;
using System.Text;
using System.Threading;
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
