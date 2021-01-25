// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.ServiceModel.Syndication
{
    public struct XmlUriData
    {
        public XmlUriData(string uriString, UriKind uriKind, XmlQualifiedName elementQualifiedName)
        {
            UriString = uriString;
            UriKind = uriKind;
            ElementQualifiedName = elementQualifiedName;
        }

        public XmlQualifiedName ElementQualifiedName { get; }

        public UriKind UriKind { get; }

        public string UriString { get; }
    }
}
