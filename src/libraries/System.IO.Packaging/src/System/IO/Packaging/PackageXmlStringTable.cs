// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;

namespace System.IO.Packaging
{
    internal static class PackageXmlStringTable
    {
        // Fields
        private static readonly XmlStringTableStruct[] s_xmlstringtable = new XmlStringTableStruct[0x1b];

        // Methods
        static PackageXmlStringTable()
        {
            string nameString = "http://www.w3.org/2001/XMLSchema-instance";
            s_xmlstringtable[1] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "xsi";
            s_xmlstringtable[2] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "xmlns";
            s_xmlstringtable[3] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "http://schemas.openxmlformats.org/package/2006/metadata/core-properties";
            s_xmlstringtable[4] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "http://purl.org/dc/elements/1.1/";
            s_xmlstringtable[5] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "http://purl.org/dc/terms/";
            s_xmlstringtable[6] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "dc";
            s_xmlstringtable[7] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "dcterms";
            s_xmlstringtable[8] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = "coreProperties";
            s_xmlstringtable[9] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "NotSpecified");
            nameString = "type";
            s_xmlstringtable[10] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, "NotSpecified");
            nameString = "creator";
            s_xmlstringtable[11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "identifier";
            s_xmlstringtable[12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "title";
            s_xmlstringtable[13] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "subject";
            s_xmlstringtable[14] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "description";
            s_xmlstringtable[15] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "language";
            s_xmlstringtable[0x10] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = "created";
            s_xmlstringtable[0x11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = "modified";
            s_xmlstringtable[0x12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = "contentType";
            s_xmlstringtable[0x13] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "keywords";
            s_xmlstringtable[20] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "category";
            s_xmlstringtable[0x15] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "version";
            s_xmlstringtable[0x16] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "lastModifiedBy";
            s_xmlstringtable[0x17] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "contentStatus";
            s_xmlstringtable[0x18] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "revision";
            s_xmlstringtable[0x19] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = "lastPrinted";
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

        internal static string? GetValueType(PackageXmlEnum id)
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
            return s_xmlstringtable[(int)id].Name;
        }

        internal static object GetXmlStringAsObject(PackageXmlEnum id)
        {
            CheckIdRange(id);
            return s_xmlstringtable[(int)id].Name;
        }

        internal static NameTable CreateNameTable()
        {
            // Preserve reference comparisons with the shared literals without sharing parsed names.
            NameTable nameTable = new NameTable();
            for (int i = 1; i < s_xmlstringtable.Length; i++)
            {
                nameTable.Add(s_xmlstringtable[i].Name);
            }

            return nameTable;
        }

        // Nested Types
        [StructLayout(LayoutKind.Sequential)]
        private readonly struct XmlStringTableStruct
        {
            private readonly string _nameString;
            private readonly PackageXmlEnum _namespace;
            private readonly string? _valueType;

            internal XmlStringTableStruct(string nameString, PackageXmlEnum ns, string? valueType)
            {
                _nameString = nameString;
                _namespace = ns;
                _valueType = valueType;
            }

            internal string Name => _nameString;

            internal PackageXmlEnum Namespace => _namespace;

            internal string? ValueType => _valueType;
        }
    }
}
