// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;
using System.IO;
using System.Runtime.Versioning;
using System.Xml;

namespace System.Configuration
{
    public abstract class ConfigurationSection : ConfigurationElement
    {
        protected ConfigurationSection()
        {
            SectionInformation = new SectionInformation(this);
        }

        public SectionInformation SectionInformation { get; }

        protected internal virtual object GetRuntimeObject()
        {
            return this;
        }

        protected internal override bool IsModified()
        {
            return SectionInformation.IsModifiedFlags() ||
                base.IsModified();
        }

        protected internal override void ResetModified()
        {
            SectionInformation.ResetModifiedFlags();
            base.ResetModified();
        }

        protected internal virtual void DeserializeSection(XmlReader reader)
        {
            if (!reader.Read() || (reader.NodeType != XmlNodeType.Element))
                throw new ConfigurationErrorsException(SR.Config_base_expected_to_find_element, reader);

            DeserializeElement(reader, false);
        }

        protected internal virtual string SerializeSection(ConfigurationElement parentElement, string name,
            ConfigurationSaveMode saveMode)
        {
            if ((CurrentConfiguration != null) &&
                (CurrentConfiguration.TargetFramework != null) &&
                !ShouldSerializeSectionInTargetVersion(CurrentConfiguration.TargetFramework))
                return string.Empty;

            ValidateElement(this, null, true);

            ConfigurationElement tempElement = CreateElement(GetType());
            tempElement.Unmerge(this, parentElement, saveMode);

            StringWriter strWriter = new StringWriter(CultureInfo.InvariantCulture);
            XmlTextWriter writer = new XmlTextWriter(strWriter)
            {
                Formatting = Formatting.Indented,
                Indentation = 4,
                IndentChar = ' '
            };

            tempElement.DataToWriteInternal = saveMode != ConfigurationSaveMode.Minimal;

            if ((CurrentConfiguration != null) && (CurrentConfiguration.TargetFramework != null))
                _configRecord.SectionsStack.Push(this);

            tempElement.SerializeToXmlElement(writer, name);

            if ((CurrentConfiguration != null) && (CurrentConfiguration.TargetFramework != null))
                _configRecord.SectionsStack.Pop();

            writer.Flush();
            return strWriter.ToString();
        }

        protected internal virtual bool ShouldSerializePropertyInTargetVersion(ConfigurationProperty property,
            string propertyName, FrameworkName targetFramework, ConfigurationElement parentConfigurationElement)
        {
            return true;
        }

        protected internal virtual bool ShouldSerializeElementInTargetVersion(ConfigurationElement element,
            string elementName, FrameworkName targetFramework)
        {
            return true;
        }

        protected internal virtual bool ShouldSerializeSectionInTargetVersion(FrameworkName targetFramework)
        {
            return true;
        }
    }
}