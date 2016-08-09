// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System.Configuration;

    /// <summary>
    /// Summary description for SerializationSectionGroup.
    /// </summary>
    internal sealed class SerializationSectionGroup : ConfigurationSectionGroup
    {
        public SerializationSectionGroup() { }

        // public properties
        public SchemaImporterExtensionsSection SchemaImporterExtensions
        {
            get { return (SchemaImporterExtensionsSection)Sections[ConfigurationStrings.SchemaImporterExtensionsSectionName]; }
        }

        public DateTimeSerializationSection DateTimeSerialization
        {
            get { return (DateTimeSerializationSection)Sections[ConfigurationStrings.DateTimeSerializationSectionName]; }
        }

        public XmlSerializerSection XmlSerializer
        {
            get { return (XmlSerializerSection)Sections[ConfigurationStrings.XmlSerializerSectionName]; }
        }
    }
}
