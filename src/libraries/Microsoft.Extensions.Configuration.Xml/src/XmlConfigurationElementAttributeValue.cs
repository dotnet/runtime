// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElementAttributeValue : IXmlConfigurationValue
    {
        private readonly XmlConfigurationElement[] _elementPath;
        private readonly string _attribute;

        public XmlConfigurationElementAttributeValue(Stack<XmlConfigurationElement> elementPath, string attribute, string value, string lineInfo)
        {
            _elementPath = elementPath?.Reverse()?.ToArray() ?? throw new ArgumentNullException(nameof(elementPath));
            _attribute = attribute ?? throw new ArgumentNullException(nameof(attribute));
            Value = value ?? throw new ArgumentNullException(nameof(value));
            LineInfo = lineInfo;
        }

        /// <summary>
        /// Combines the path to this element with the attribute value to produce a key.
        /// Note that this property cannot be computed during construction,
        /// because the keys of the elements along the path may change when multiple elements with the same name are encountered
        /// </summary>
        public string Key => ConfigurationPath.Combine(_elementPath.Select(e => e.Key).Concat(new[] { _attribute }).Where(key => key != null));

        public string Value { get; }

        public string LineInfo { get; }
    }
}
