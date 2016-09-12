// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization.Configuration
{
    using System.Configuration;
    using System.Collections;
    using System.Globalization;
    using System.Reflection;
    using System.Threading;
    using System.Xml.Serialization.Advanced;

    internal sealed class SchemaImporterExtensionsSection : ConfigurationSection
    {
        public SchemaImporterExtensionsSection()
        {
        }

        private static string GetSqlTypeSchemaImporter(string typeName)
        {
            return "System.Data.SqlTypes." + typeName + ", " + AssemblyRef.SystemData;
        }

        protected override void InitializeDefault()
        {
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterChar, GetSqlTypeSchemaImporter("TypeCharSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterNChar, GetSqlTypeSchemaImporter("TypeNCharSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterVarChar, GetSqlTypeSchemaImporter("TypeVarCharSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterNVarChar, GetSqlTypeSchemaImporter("TypeNVarCharSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterText, GetSqlTypeSchemaImporter("TypeTextSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterNText, GetSqlTypeSchemaImporter("TypeNTextSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterVarBinary, GetSqlTypeSchemaImporter("TypeVarBinarySchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterBinary, GetSqlTypeSchemaImporter("TypeBinarySchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterImage, GetSqlTypeSchemaImporter("TypeVarImageSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterDecimal, GetSqlTypeSchemaImporter("TypeDecimalSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterNumeric, GetSqlTypeSchemaImporter("TypeNumericSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterBigInt, GetSqlTypeSchemaImporter("TypeBigIntSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterInt, GetSqlTypeSchemaImporter("TypeIntSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterSmallInt, GetSqlTypeSchemaImporter("TypeSmallIntSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterTinyInt, GetSqlTypeSchemaImporter("TypeTinyIntSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterBit, GetSqlTypeSchemaImporter("TypeBitSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterFloat, GetSqlTypeSchemaImporter("TypeFloatSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterReal, GetSqlTypeSchemaImporter("TypeRealSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterDateTime, GetSqlTypeSchemaImporter("TypeDateTimeSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterSmallDateTime, GetSqlTypeSchemaImporter("TypeSmallDateTimeSchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterMoney, GetSqlTypeSchemaImporter("TypeMoneySchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterSmallMoney, GetSqlTypeSchemaImporter("TypeSmallMoneySchemaImporterExtension")));
            this.SchemaImporterExtensions.Add(
                new SchemaImporterExtensionElement(ConfigurationStrings.SqlTypesSchemaImporterUniqueIdentifier, GetSqlTypeSchemaImporter("TypeUniqueIdentifierSchemaImporterExtension")));
        }

        protected override ConfigurationPropertyCollection Properties
        {
            get { return _properties; }
        }

        public SchemaImporterExtensionElementCollection SchemaImporterExtensions
        {
            get { return null; }
        }

        internal SchemaImporterExtensionCollection SchemaImporterExtensionsInternal
        {
            get
            {
                SchemaImporterExtensionCollection extensions = new SchemaImporterExtensionCollection();
                /*foreach(SchemaImporterExtensionElement elem in this.SchemaImporterExtensions) {
                    extensions.Add(elem.Name, elem.Type);
                }*/

                return extensions;
            }
        }

        private ConfigurationPropertyCollection _properties = new ConfigurationPropertyCollection();
    }
}
