// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal sealed class XmlConfigurationElementAttributeValue
    {
        public XmlConfigurationElementAttributeValue(string attribute, string value, int? lineNumber, int? linePosition)
        {
            ThrowHelper.ThrowIfNull(attribute);
            ThrowHelper.ThrowIfNull(value);

            Attribute = attribute;
            Value = value;
            LineNumber = lineNumber;
            LinePosition = linePosition;
        }

        public string Attribute { get; }

        public string Value { get; }

        public int? LineNumber { get; }

        public int? LinePosition { get; }
    }
}
