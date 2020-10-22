// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Extensions.Configuration.Xml
{
    internal class XmlConfigurationElement
    {
        public string ElementName { get; }

        public string Name { get; }

        public string LineInfo { get; }

        public bool Multiple { get; set; }

        public int Index { get; set; }

        /// <summary>
        /// The parent element, or null if this is the root element
        /// </summary>
        public XmlConfigurationElement Parent { get; }

        public XmlConfigurationElement(XmlConfigurationElement parent, string elementName, string name, string lineInfo)
        {
            Parent = parent;
            ElementName = elementName ?? throw new ArgumentNullException(nameof(elementName));
            Name = name;
            LineInfo = lineInfo;
        }

        public string Key
        {
            get
            {
                var tokens = new List<string>(3);

                // the root element does not contribute to the prefix
                if (Parent != null) tokens.Add(ElementName);

                // the name attribute always contributes to the prefix
                if (Name != null) tokens.Add(Name);

                // the index only contributes to the prefix when there are multiple elements wih the same name
                if (Multiple) tokens.Add(Index.ToString());

                // the root element without a name attribute does not contribute to prefix at all
                if (!tokens.Any()) return null;

                return string.Join(ConfigurationPath.KeyDelimiter, tokens);
            }
        }

        public bool IsSibling(XmlConfigurationElement xmlConfigurationElement)
        {
            return Parent != null
                && xmlConfigurationElement.Parent == Parent
                && string.Equals(ElementName, xmlConfigurationElement.ElementName)
                && string.Equals(Name, xmlConfigurationElement.Name);
        }
    }
}
