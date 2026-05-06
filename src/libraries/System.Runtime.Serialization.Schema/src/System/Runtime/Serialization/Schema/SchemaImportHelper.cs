// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization.DataContracts;
using System.Xml;

namespace System.Runtime.Serialization
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
            "ClassDataContract" => DataContractType.ClassDataContract,
            "CollectionDataContract" => DataContractType.CollectionDataContract,
            "EnumDataContract" => DataContractType.EnumDataContract,
            "PrimitiveDataContract" => DataContractType.PrimitiveDataContract,
            "XmlDataContract" => DataContractType.XmlDataContract,
            _ => DataContractType.Unknown
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

        [RequiresUnreferencedCode(ImportGlobals.SerializerTrimmerWarning)]
        internal static bool IsItemTypeNullable(this DataContract collectionDataContract)
        {
            if (collectionDataContract.GetContractType() == DataContractType.CollectionDataContract)
            {
                // ItemContract - aka BaseContract - is never null for CollectionDataContract
                return SchemaImportHelper.IsTypeNullable(collectionDataContract.BaseContract!.UnderlyingType);
            }

            return false;
        }
    }

    internal static class SchemaImportHelper
    {
        internal static bool IsTypeNullable(Type type)
        {
            return !type.IsValueType ||
                    (type.IsGenericType &&
                    type.GetGenericTypeDefinition() == typeof(Nullable<>));
        }

        internal static string GetCollectionNamespace(string elementNs)
        {
            return IsBuiltInNamespace(elementNs) ? ImportGlobals.CollectionsNamespace : elementNs;
        }

        internal static string GetDataContractNamespaceFromUri(string uriString)
        {
            return uriString.StartsWith(ImportGlobals.DataContractXsdBaseNamespace, StringComparison.Ordinal) ? uriString.Substring(ImportGlobals.DataContractXsdBaseNamespace.Length) : uriString;
        }

        internal static string GetDefaultXmlNamespace(string? clrNs)
        {
            return new Uri(ImportGlobals.DataContractXsdBaseNamespaceUri, clrNs ?? string.Empty).AbsoluteUri;
        }

        internal static bool IsBuiltInNamespace(string ns)
        {
            return (ns == ImportGlobals.SchemaNamespace || ns == ImportGlobals.SerializationNamespace);
        }

        // This should match the behavior of DataContract.EncodeLocalName
        internal static string EncodeLocalName(string localName)
        {
            if (IsAsciiLocalName(localName))
                return localName;

            if (IsValidNCName(localName))
                return localName;

            return XmlConvert.EncodeLocalName(localName);
        }

        private static bool IsAsciiLocalName(string localName)
        {
            if (localName.Length == 0)
                return false;
            if (!char.IsAsciiLetter(localName[0]))
                return false;
            for (int i = 1; i < localName.Length; i++)
            {
                char ch = localName[i];
                if (!char.IsAsciiLetterOrDigit(ch))
                    return false;
            }
            return true;
        }

        private static bool IsValidNCName(string name)
        {
            try
            {
                XmlConvert.VerifyNCName(name);
                return true;
            }
            catch (XmlException)
            {
                return false;
            }
        }
    }
}
