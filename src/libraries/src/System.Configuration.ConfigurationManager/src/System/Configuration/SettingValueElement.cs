// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Xml;

namespace System.Configuration
{
    public sealed class SettingValueElement : ConfigurationElement
    {
        private static volatile ConfigurationPropertyCollection _properties;
        private static XmlDocument _document = new XmlDocument();

        private XmlNode _valueXml;
        private bool _isModified = false;

        protected internal override ConfigurationPropertyCollection Properties
        {
            get
            {
                if (_properties == null)
                {
                    _properties = new ConfigurationPropertyCollection();
                }

                return _properties;
            }
        }

        public XmlNode ValueXml
        {
            get
            {
                return _valueXml;
            }
            set
            {
                _valueXml = value;
                _isModified = true;
            }
        }

        protected internal override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            ValueXml = _document.ReadNode(reader);
        }

        public override bool Equals(object settingValue)
        {
            SettingValueElement u = settingValue as SettingValueElement;
            return (u != null && Equals(u.ValueXml, ValueXml));
        }

        public override int GetHashCode()
        {
            return ValueXml.GetHashCode();
        }

        protected internal override bool IsModified()
        {
            return _isModified;
        }

        protected internal override void ResetModified()
        {
            _isModified = false;
        }

        protected internal override bool SerializeToXmlElement(XmlWriter writer, string elementName)
        {
            if (ValueXml != null)
            {
                if (writer != null)
                {
                    ValueXml?.WriteTo(writer);
                }
                return true;
            }

            return false;
        }

        protected internal override void Reset(ConfigurationElement parentElement)
        {
            base.Reset(parentElement);
            ValueXml = ((SettingValueElement)parentElement).ValueXml;
        }

        protected internal override void Unmerge(ConfigurationElement sourceElement, ConfigurationElement parentElement,
                                                ConfigurationSaveMode saveMode)
        {
            base.Unmerge(sourceElement, parentElement, saveMode);
            ValueXml = ((SettingValueElement)sourceElement).ValueXml;
        }
    }
}
