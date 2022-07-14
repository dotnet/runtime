// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Xml.Schema;

namespace System.Runtime.Serialization.Schema
{
    internal enum DataContractType
    {
        ClassDataContract,
        CollectionDataContract,
        EnumDataContract,
        PrimitiveDataContract,
        XmlDataContract,
        Unknown = -1
    }

    internal static class DataContractExtensions
    {
        internal static DataContractType GetContractType(this DataContract dataContract) => dataContract.ContractType switch
        {
            "ClassDataContract" => Schema.DataContractType.ClassDataContract,
            "CollectionDataContract" => Schema.DataContractType.CollectionDataContract,
            "EnumDataContract" => Schema.DataContractType.EnumDataContract,
            "PrimitiveDataContract" => Schema.DataContractType.PrimitiveDataContract,
            "XmlDataContract" => Schema.DataContractType.XmlDataContract,
            _ => Schema.DataContractType.Unknown
        };

        internal static bool Is(this DataContract dataContract, DataContractType dcType)
        {
            return (dataContract.GetContractType() == dcType);
        }

        internal static DataContract? As(this DataContract dataContract, DataContractType dcType)
        {
            if (dataContract.GetContractType() == dcType)
                return dataContract;
            return null;
        }

        internal static bool IsItemTypeNullable(this DataContract collectionDataContract)
        {
            if (collectionDataContract.GetContractType() == DataContractType.CollectionDataContract)
            {
                // ItemContract - aka BaseContract - is never null for CollectionDataContract
                return SchemaHelper.IsTypeNullable(collectionDataContract.BaseContract!.UnderlyingType);
            }

            return false;
        }
    }

    internal static class SchemaHelper
    {
        internal static void CompileSchemaSet(XmlSchemaSet schemaSet)
        {
            if (schemaSet.Contains(XmlSchema.Namespace))
                schemaSet.Compile();
            else
            {
                // Add base XSD schema with top level element named "schema"
                XmlSchema xsdSchema = new XmlSchema();
                xsdSchema.TargetNamespace = XmlSchema.Namespace;
                XmlSchemaElement element = new XmlSchemaElement();
                element.Name = Globals.SchemaLocalName;
                element.SchemaType = new XmlSchemaComplexType();
                xsdSchema.Items.Add(element);
                schemaSet.Add(xsdSchema);
                schemaSet.Compile();
            }
        }

        internal static bool IsTypeNullable(Type type)
        {
            return !type.IsValueType ||
                    (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        internal static string GetCollectionNamespace(string elementNs)
        {
            return IsBuiltInNamespace(elementNs) ? Globals.CollectionsNamespace : elementNs;
        }

        internal static string GetDataContractNamespaceFromUri(string uriString)
        {
            return uriString.StartsWith(Globals.DataContractXsdBaseNamespace, StringComparison.Ordinal) ? uriString.Substring(Globals.DataContractXsdBaseNamespace.Length) : uriString;
        }

        internal static string GetDefaultStableNamespace(Type type)
        {
            if (type.IsGenericParameter)
                return "{ns}";
            return GetDefaultStableNamespace(type.Namespace);
        }

        internal static string GetDefaultStableNamespace(string? clrNs)
        {
            if (clrNs == null) clrNs = string.Empty;
            return new Uri(Globals.DataContractXsdBaseNamespaceUri, clrNs).AbsoluteUri;
        }

        internal static bool IsBuiltInNamespace(string ns)
        {
            return (ns == Globals.SchemaNamespace || ns == Globals.SerializationNamespace);
        }
    }
}
