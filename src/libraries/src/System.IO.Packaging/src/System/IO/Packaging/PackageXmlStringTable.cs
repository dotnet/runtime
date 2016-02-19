// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Runtime.InteropServices;

namespace System.IO.Packaging
{
    internal static class PackageXmlStringTable
    {
        // Fields
        private static NameTable s_nameTable = new NameTable();
        private static XmlStringTableStruct[] s_xmlstringtable = new XmlStringTableStruct[0x1b];

        // Methods
        static PackageXmlStringTable()
        {
            object nameString = s_nameTable.Add("http://www.w3.org/2001/XMLSchema-instance");
            s_xmlstringtable[1] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("xsi");
            s_xmlstringtable[2] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("xmlns");
            s_xmlstringtable[3] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
            s_xmlstringtable[4] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("http://purl.org/dc/elements/1.1/");
            s_xmlstringtable[5] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("http://purl.org/dc/terms/");
            s_xmlstringtable[6] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("dc");
            s_xmlstringtable[7] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("dcterms");
            s_xmlstringtable[8] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.Add("coreProperties");
            s_xmlstringtable[9] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "NotSpecified");
            nameString = s_nameTable.Add("type");
            s_xmlstringtable[10] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, "NotSpecified");
            nameString = s_nameTable.Add("creator");
            s_xmlstringtable[11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("identifier");
            s_xmlstringtable[12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("title");
            s_xmlstringtable[13] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("subject");
            s_xmlstringtable[14] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("description");
            s_xmlstringtable[15] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("language");
            s_xmlstringtable[0x10] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("created");
            s_xmlstringtable[0x11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = s_nameTable.Add("modified");
            s_xmlstringtable[0x12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = s_nameTable.Add("contentType");
            s_xmlstringtable[0x13] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("keywords");
            s_xmlstringtable[20] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("category");
            s_xmlstringtable[0x15] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("version");
            s_xmlstringtable[0x16] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("lastModifiedBy");
            s_xmlstringtable[0x17] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("contentStatus");
            s_xmlstringtable[0x18] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("revision");
            s_xmlstringtable[0x19] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.Add("lastPrinted");
            s_xmlstringtable[0x1a] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "DateTime");
        }

        private static void CheckIdRange(PackageXmlEnum id)
        {
            if ((id <= PackageXmlEnum.NotDefined) || (id >= (PackageXmlEnum.LastPrinted | PackageXmlEnum.XmlSchemaInstanceNamespace)))
            {
                throw new ArgumentOutOfRangeException(nameof(id));
            }
        }

        internal static PackageXmlEnum GetEnumOf(object xmlString)
        {
            for (int i = 1; i < s_xmlstringtable.GetLength(0); i++)
            {
                if (object.ReferenceEquals(s_xmlstringtable[i].Name, xmlString))
                {
                    return (PackageXmlEnum)i;
                }
            }
            return PackageXmlEnum.NotDefined;
        }

        internal static string GetValueType(PackageXmlEnum id)
        {
            CheckIdRange(id);
            return s_xmlstringtable[(int)id].ValueType;
        }

        internal static PackageXmlEnum GetXmlNamespace(PackageXmlEnum id)
        {
            CheckIdRange(id);
            return s_xmlstringtable[(int)id].Namespace;
        }

        internal static string GetXmlString(PackageXmlEnum id)
        {
            CheckIdRange(id);
            return (string)s_xmlstringtable[(int)id].Name;
        }

        internal static object GetXmlStringAsObject(PackageXmlEnum id)
        {
            CheckIdRange(id);
            return s_xmlstringtable[(int)id].Name;
        }

        // Properties
        internal static NameTable NameTable
        {
            get
            {
                return s_nameTable;
            }
        }

        // Nested Types
        [StructLayout(LayoutKind.Sequential)]
        private struct XmlStringTableStruct
        {
            private object _nameString;
            private PackageXmlEnum _namespace;
            private string _valueType;
            internal XmlStringTableStruct(object nameString, PackageXmlEnum ns, string valueType)
            {
                _nameString = nameString;
                _namespace = ns;
                _valueType = valueType;
            }

            internal object Name
            {
                get
                {
                    return (string)_nameString;
                }
            }
            internal PackageXmlEnum Namespace
            {
                get
                {
                    return _namespace;
                }
            }
            internal string ValueType
            {
                get
                {
                    return _valueType;
                }
            }
        }
    }
}
