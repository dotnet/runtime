// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal sealed class XmlConfigurationElement
    {
        public string ElementName { get; }

        public string? Name { get; }

        /// <summary>
        /// A composition of ElementName and Name, that serves as the basis for detecting siblings
        /// </summary>
        public string SiblingName { get; }

        /// <summary>
        /// The children of this element
        /// </summary>
        public IDictionary<string, List<XmlConfigurationElement>>? ChildrenBySiblingName { get; set; }

        /// <summary>
        /// Performance optimization: do not initialize a dictionary and a list for elements with a single child
        /// </summary>
        public XmlConfigurationElement? SingleChild { get; set; }

        public XmlConfigurationElementTextContent? TextContent { get; set; }

        public List<XmlConfigurationElementAttributeValue>? Attributes { get; set; }

        public XmlConfigurationElement(string elementName, string? name)
        {
            ThrowHelper.ThrowIfNull(elementName);

            ElementName = elementName;
            Name = name;
            SiblingName = string.IsNullOrEmpty(Name) ? ElementName : ElementName + ":" + Name;
        }
    }
}
