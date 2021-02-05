// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElement
    {
        public string ElementName { get; }

        public string Name { get; }

        public string LineInfo { get; }

        /// <summary>
        /// The children of this element
        /// </summary>
        public List<XmlConfigurationElement> Children { get; set; }

        /// <summary>
        /// The siblings of this element, including itself
        /// Elements are considered siblings if they share the same element name and name attribute
        /// This list is shared by each sibling
        /// </summary>
        public List<XmlConfigurationElement> Siblings { get; set; }

        public XmlConfigurationElementTextContent? TextContent { get; set; }

        public List<XmlConfigurationElementAttributeValue> Attributes { get; set; }

        public XmlConfigurationElement(string elementName, string name, string lineInfo)
        {
            ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
            Name = name;
            LineInfo = lineInfo;
        }

        public XmlConfigurationElement FindSiblingInChildren(XmlConfigurationElement element)
        {
            if (element is null) throw new ArgumentNullException(nameof(element));

            for (int i = 0; i < Children.Count; i++) {
                var child = Children[i];

                if (child.IsSiblingOf(element))
                    return child;
            }

            return null;
        }

        private bool IsSiblingOf(XmlConfigurationElement xmlConfigurationElement)
        {
            if (xmlConfigurationElement is null)
            {
                throw new ArgumentNullException(nameof(xmlConfigurationElement));
            }

            return string.Equals(ElementName, xmlConfigurationElement.ElementName, StringComparison.OrdinalIgnoreCase)
                && string.Equals(Name, xmlConfigurationElement.Name, StringComparison.OrdinalIgnoreCase);
        }
    }
}
