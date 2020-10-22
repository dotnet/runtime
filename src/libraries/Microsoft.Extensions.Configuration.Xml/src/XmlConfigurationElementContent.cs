// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElementContent
        : IXmlConfigurationValue
    {
        private readonly XmlConfigurationElement[] _elementPath;

        public XmlConfigurationElementContent(Stack<XmlConfigurationElement> elementPath, string content, string lineInfo)
        {
            Value = content ?? throw new ArgumentNullException(nameof(content));
            LineInfo = lineInfo ?? throw new ArgumentNullException(nameof(lineInfo));
            _elementPath = elementPath?.Reverse().ToArray() ?? throw new ArgumentNullException(nameof(elementPath));
        }

        /// <summary>
        /// Combines the path to this element to produce a key.
        /// Note that this property cannot be computed during construction,
        /// because the keys of the elements along the path may change when multiple elements with the same name are encountered
        /// </summary>
        public string Key => ConfigurationPath.Combine(_elementPath.Select(e => e.Key).Where(key => key != null));

        public string Value { get; }

        public string LineInfo { get; }
    }
}
