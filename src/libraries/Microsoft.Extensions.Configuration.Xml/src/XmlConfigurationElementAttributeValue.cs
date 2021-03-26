// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal sealed class XmlConfigurationElementAttributeValue
    {
        public XmlConfigurationElementAttributeValue(string attribute, string value, int? lineNumber, int? linePosition)
        {
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public string Attribute { get; }

        public string Value { get; }

        public int? LineNumber { get; }

        public int? LinePosition { get; }
    }
}
