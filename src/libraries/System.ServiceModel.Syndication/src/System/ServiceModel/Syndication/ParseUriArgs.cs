// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.ServiceModel.Syndication
{
    public struct ParseUriArgs
    {
        internal ParseUriArgs(string uriString, UriKind uriKind, XmlQualifiedName elementQualifiedName)
        {
            UriString = uriString;
            UriKind = uriKind;
            ElementQualifiedName = elementQualifiedName;
        }

        public string UriString { get; private set; }
        public UriKind UriKind { get; private set; }
        public XmlQualifiedName ElementQualifiedName { get; private set; }
    }
}
