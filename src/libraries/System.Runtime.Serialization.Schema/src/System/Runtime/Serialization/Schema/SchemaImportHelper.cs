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
        internal static bool IsCollectionItemNullable(this DataContract collectionDataContract)
        {
            if (collectionDataContract.GetContractType() == DataContractType.CollectionDataContract)
            {
                // This would be easier if we had included a way to read 'IsItemTypeNullable' from
                // CollectionDataContract. But let's do our best to see if we can figure out the
                // nullability of the collection item type.

                // If the item type is not a value type, then it's nullable.
                // Dictionary KeyValuePair's are imported as value types.
                DataContract itemContract = collectionDataContract.BaseContract!;
                if (!itemContract.IsValueType)
                    return true;

                // GetArrayTypeName() generates the default DCS stable XML name for a collection of this item contract
                // One of these variants should match for our item type, unless the imported schema is using a non-standard
                // XML name for the collection item type.

                // First check the standard non-nullable collection XML name for this item type.
                XmlQualifiedName nonNullableCollectionType = itemContract.GetArrayTypeName(isNullable: false);
                if (collectionDataContract.XmlName.Name == nonNullableCollectionType.Name &&
                    collectionDataContract.XmlName.Namespace == nonNullableCollectionType.Namespace)
                {
                    return false;
                }

                // Then check the standard nullable collection XML name for this item type.
                XmlQualifiedName nullableCollectionType = itemContract.GetArrayTypeName(isNullable: true);
                if (collectionDataContract.XmlName.Name == nullableCollectionType.Name &&
                    collectionDataContract.XmlName.Namespace == nullableCollectionType.Namespace)
                {
                    return true;
                }

                // If we get here, then the collection is using non-standard XML naming.
                // UnderlyingType might be an actual CLR type, or it might be the SchemaDefinedType placeholder.
                // This is just a best effort attempt at this point.
                return SchemaImportHelper.IsTypeNullable(itemContract.UnderlyingType);
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
