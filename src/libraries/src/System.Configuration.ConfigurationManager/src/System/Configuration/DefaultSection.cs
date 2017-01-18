// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    public sealed class DefaultSection : ConfigurationSection
    {
        private static volatile ConfigurationPropertyCollection s_properties;
        private bool _isModified;

        private string _rawXml = string.Empty;

        public DefaultSection()
        {
            EnsureStaticPropertyBag();
        }

        protected internal override ConfigurationPropertyCollection Properties => EnsureStaticPropertyBag();

        private static ConfigurationPropertyCollection EnsureStaticPropertyBag()
        {
            if (s_properties == null)
            {
                ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection();
                s_properties = properties;
            }

            return s_properties;
        }

        protected internal override bool IsModified()
        {
            return _isModified;
        }

        protected internal override void ResetModified()
        {
            _isModified = false;
        }

        protected internal override void Reset(ConfigurationElement parentSection)
        {
            _rawXml = string.Empty;
            _isModified = false;
        }

        protected internal override void DeserializeSection(XmlReader xmlReader)
        {
            if (!xmlReader.Read() || (xmlReader.NodeType != XmlNodeType.Element))
                throw new ConfigurationErrorsException(SR.Config_base_expected_to_find_element, xmlReader);
            _rawXml = xmlReader.ReadOuterXml();
            _isModified = true;
        }

        protected internal override string SerializeSection(ConfigurationElement parentSection, string name,
            ConfigurationSaveMode saveMode)
        {
            return _rawXml;
        }
    }
}