// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.ServiceModel.Syndication
{
    public struct XmlDateTimeData
    {
        public XmlDateTimeData(string dateTimeString, XmlQualifiedName elementQualifiedName)
        {
            DateTimeString = dateTimeString;
            ElementQualifiedName = elementQualifiedName;
        }

        public string DateTimeString { get; }

        public XmlQualifiedName ElementQualifiedName { get; }
    }
}
