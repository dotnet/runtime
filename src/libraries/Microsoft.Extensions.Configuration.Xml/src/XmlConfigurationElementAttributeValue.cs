// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElementAttributeValue
    {
        public XmlConfigurationElementAttributeValue(string attribute, string value, string lineInfo)
        {
            Attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            LineInfo = lineInfo;
        }

        public string Attribute { get; }

        public string Value { get; }

        public string LineInfo { get; }
    }
}
