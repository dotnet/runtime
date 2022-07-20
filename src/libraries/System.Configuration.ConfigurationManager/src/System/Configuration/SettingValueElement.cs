// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml;

namespace System.Configuration
{
    public sealed class SettingValueElement : ConfigurationElement
    {
        private static volatile ConfigurationPropertyCollection _properties;
        private static readonly XmlDocument _document = new XmlDocument();

        private XmlNode _valueXml;
        private bool _isModified;

        protected internal override ConfigurationPropertyCollection Properties =>
            _properties ??= new ConfigurationPropertyCollection();

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
            return ValueXml?.GetHashCode() ?? 0;
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
