// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

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
        private static readonly ThreadSafeNameTable s_nameTable = new ThreadSafeNameTable();
        private static readonly XmlStringTableStruct[] s_xmlstringtable = new XmlStringTableStruct[0x1b];

        // Methods
        static PackageXmlStringTable()
        {
            object nameString = s_nameTable.AddNoLock("http://www.w3.org/2001/XMLSchema-instance");
            s_xmlstringtable[1] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("xsi");
            s_xmlstringtable[2] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("xmlns");
            s_xmlstringtable[3] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("http://schemas.openxmlformats.org/package/2006/metadata/core-properties");
            s_xmlstringtable[4] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("http://purl.org/dc/elements/1.1/");
            s_xmlstringtable[5] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("http://purl.org/dc/terms/");
            s_xmlstringtable[6] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("dc");
            s_xmlstringtable[7] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("dcterms");
            s_xmlstringtable[8] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, null);
            nameString = s_nameTable.AddNoLock("coreProperties");
            s_xmlstringtable[9] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "NotSpecified");
            nameString = s_nameTable.AddNoLock("type");
            s_xmlstringtable[10] = new XmlStringTableStruct(nameString, PackageXmlEnum.NotDefined, "NotSpecified");
            nameString = s_nameTable.AddNoLock("creator");
            s_xmlstringtable[11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("identifier");
            s_xmlstringtable[12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("title");
            s_xmlstringtable[13] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("subject");
            s_xmlstringtable[14] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("description");
            s_xmlstringtable[15] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("language");
            s_xmlstringtable[0x10] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("created");
            s_xmlstringtable[0x11] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = s_nameTable.AddNoLock("modified");
            s_xmlstringtable[0x12] = new XmlStringTableStruct(nameString, PackageXmlEnum.DublinCoreTermsNamespace, "DateTime");
            nameString = s_nameTable.AddNoLock("contentType");
            s_xmlstringtable[0x13] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("keywords");
            s_xmlstringtable[20] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("category");
            s_xmlstringtable[0x15] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("version");
            s_xmlstringtable[0x16] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("lastModifiedBy");
            s_xmlstringtable[0x17] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("contentStatus");
            s_xmlstringtable[0x18] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("revision");
            s_xmlstringtable[0x19] = new XmlStringTableStruct(nameString, PackageXmlEnum.PackageCorePropertiesNamespace, "String");
            nameString = s_nameTable.AddNoLock("lastPrinted");
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
            private readonly object _nameString;
            private readonly PackageXmlEnum _namespace;
            private readonly string? _valueType;
            internal XmlStringTableStruct(object nameString, PackageXmlEnum ns, string? valueType)
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
            internal string? ValueType
            {
                get
                {
                    return _valueType;
                }
            }
        }

        private sealed class ThreadSafeNameTable : NameTable
        {
            public override string Add(char[] array, int offset, int length)
            {
                lock (this)
                {
                    return base.Add(array, offset, length);
                }
            }

            public override string Add(string array)
            {
                lock (this)
                {
                    return base.Add(array);
                }
            }

            // can be used only from static ctor (which is always executed by a single thread)
            internal string AddNoLock(string array) => base.Add(array);

            public override string? Get(char[] array, int offset, int length)
            {
                lock (this)
                {
                    return base.Get(array, offset, length);
                }
            }

            public override string? Get(string array)
            {
                lock (this)
                {
                    return base.Get(array);
                }
            }
        }
    }
}
