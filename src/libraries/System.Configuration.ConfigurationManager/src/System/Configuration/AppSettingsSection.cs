// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.Specialized;
using System.IO;
using System.Xml;

namespace System.Configuration
{
    public sealed class AppSettingsSection : ConfigurationSection
    {
        private static volatile ConfigurationPropertyCollection s_properties;
        private static volatile ConfigurationProperty s_propAppSettings;
        private static volatile ConfigurationProperty s_propFile;

        private KeyValueInternalCollection _keyValueCollection;

        public AppSettingsSection()
        {
            EnsureStaticPropertyBag();
        }

        protected internal override ConfigurationPropertyCollection Properties => EnsureStaticPropertyBag();

        internal NameValueCollection InternalSettings
            => _keyValueCollection ??= new KeyValueInternalCollection(this);

        [ConfigurationProperty("", IsDefaultCollection = true)]
        public KeyValueConfigurationCollection Settings => (KeyValueConfigurationCollection)base[s_propAppSettings];

        [ConfigurationProperty("file", DefaultValue = "")]
        public string File
        {
            get
            {
                string fileValue = (string)base[s_propFile];
                return fileValue ?? string.Empty;
            }
            set { base[s_propFile] = value; }
        }

        private static ConfigurationPropertyCollection EnsureStaticPropertyBag()
        {
            if (s_properties != null) return s_properties;

            ConfigurationProperty propAppSettings = new ConfigurationProperty(
                name: null,
                type: typeof(KeyValueConfigurationCollection),
                defaultValue: null,
                options: ConfigurationPropertyOptions.IsDefaultCollection);

            ConfigurationProperty propFile = new ConfigurationProperty(
                name: "file",
                type: typeof(string),
                defaultValue: string.Empty,
                options: ConfigurationPropertyOptions.None);

            ConfigurationPropertyCollection properties = new ConfigurationPropertyCollection
            {
                propAppSettings,
                propFile
            };

            s_propAppSettings = propAppSettings;
            s_propFile = propFile;
            s_properties = properties;

            return s_properties;
        }

        protected internal override object GetRuntimeObject()
        {
            SetReadOnly();
            return InternalSettings; // return the read only object
        }

        protected internal override void Reset(ConfigurationElement parentSection)
        {
            _keyValueCollection = null;
            base.Reset(parentSection);
            if (!string.IsNullOrEmpty((string)base[s_propFile]))
            {
                // don't inherit from the parent by ignoring locks
                SetPropertyValue(prop: s_propFile, value: null, ignoreLocks: true);
            }
        }

        protected internal override void DeserializeElement(XmlReader reader, bool serializeCollectionKey)
        {
            string elementName = reader.Name;

            base.DeserializeElement(reader, serializeCollectionKey);
            if (!(File?.Length > 0)) return;

            string sourceFileFullPath;

            // Determine file location
            string configFile = ElementInformation.Source;

            sourceFileFullPath = string.IsNullOrEmpty(configFile)
                ? File
                : Path.Combine(Path.GetDirectoryName(configFile), File);

            if (!IO.File.Exists(sourceFileFullPath)) return;
            int lineOffset;
            string rawXml;

            using (Stream sourceFileStream = new FileStream(sourceFileFullPath, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (XmlUtil xmlUtil = new XmlUtil(sourceFileStream, sourceFileFullPath, true))
            {
                if (xmlUtil.Reader.Name != elementName)
                    throw new ConfigurationErrorsException(
                        SR.Format(SR.Config_name_value_file_section_file_invalid_root, elementName),
                        xmlUtil);

                lineOffset = xmlUtil.Reader.LineNumber;
                rawXml = xmlUtil.CopySection();

                // Detect if there is any XML left over after the section
                while (!xmlUtil.Reader.EOF)
                {
                    XmlNodeType t = xmlUtil.Reader.NodeType;
                    if (t != XmlNodeType.Comment)
                        throw new ConfigurationErrorsException(SR.Config_source_file_format, xmlUtil);

                    xmlUtil.Reader.Read();
                }
            }

            ConfigXmlReader internalReader = new ConfigXmlReader(rawXml, sourceFileFullPath, lineOffset);
            internalReader.Read();

            if (internalReader.MoveToNextAttribute())
                throw new ConfigurationErrorsException(
                    SR.Format(SR.Config_base_unrecognized_attribute, internalReader.Name),
                    (XmlReader)internalReader);

            internalReader.MoveToElement();

            base.DeserializeElement(internalReader, serializeCollectionKey);
        }
    }
}
