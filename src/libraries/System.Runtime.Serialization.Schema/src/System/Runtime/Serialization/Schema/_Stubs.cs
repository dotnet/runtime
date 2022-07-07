// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// TODO smolloy - stubs. remove once API is determined.

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Xml;
using System.Xml.Schema;

using DataContractDictionary = System.Collections.Generic.Dictionary<System.Xml.XmlQualifiedName, System.Runtime.Serialization.DataContract>;

namespace System.Runtime.Serialization.HideStubs
{
    public sealed class DataContractSet
    {
        // =======================================================================================================
        // These existed internal in Core
        public DataContractSet(DataContractSet copySet) { }
        public void Add(Type type) { }
        public DataContract GetDataContract(Type type) { return new DataContract(); }
        public IEnumerator<KeyValuePair<XmlQualifiedName, DataContract>> GetEnumerator() { return new Dictionary<XmlQualifiedName, DataContract>().GetEnumerator(); }
        public Dictionary<DataContract, object> ProcessedContracts => new Dictionary<DataContract, object>();


        // =======================================================================================================
        // These existed internal in 4.8 and are brought back for schema support
        public DataContractSet(object? a, object? b, object? c) { }
        public DataContract? GetDataContract(XmlQualifiedName qname) { return null; }   // Technically new. This was a this[] indexer in 4.8.
        public DataContractDictionary? KnownTypesForObject { get; /*internal still OK for set, SchemaImporter.cs is the only writer.*/ }


        // =======================================================================================================
        // These are new API's, internal or otherwise. All of them are simple wrappers or a gateway to an existing internal method
        // on an internal type, except for the last. [GetReferencedTypeOnImport]

        // Forward to SchemaImporter.cs
        public static void CompileSchemaSet(XmlSchemaSet schemaSet) { }
        // Forward to SchemaExporter.cs
        public void ExportSchemaSet(XmlSchemaSet schemaSet) { }
        // Forward to SchemaImporter.cs
        public void ImportSchemaSet(XmlSchemaSet schemaSet, ICollection<XmlQualifiedName>? typeNames, ICollection<XmlSchemaElement> elements, XmlQualifiedName[] elementTypeNames /*filled on return*/, bool importXmlDataType) { }
        // Wrapper around a trio of internal methods of the same name with a dash of logic that was in but does not need to live in CodeExporter.
        //  Using this wrapper exposes one method instead of 3.
        public bool TryGetReferencedType(XmlQualifiedName stableName, DataContract? dataContract, [NotNullWhen(true)] out Type? type) { type = null; return false; }
        // Wrapper around GetSurrogateData - which is internal brought back for schema support. The wrapper allows us to keep knowledge/logic of
        //  surrogate providers internal to DataContractSet.
        public bool TryGetSurrogateData(object key, out object? value) { value = null; return false; }
        // This is new. Pushing surrogate functionality into DCSet so we don't have to manage surrogates externally. (Since ExtendedSurrogate is internal.)
        public Type? GetReferencedTypeOnImport(DataContract dataContract) { return null; }  // See the commented code in CodeExporter.cs for what this is supposed to do.
    }

    public class DataMember
    {
        // =======================================================================================================
        // All were existing internal properties.
        public bool EmitDefaultValue;
        public bool IsNullable;
        public bool IsRequired;
        public DataContract MemberTypeContract = null!;  // ** It isn't used externally, but MemberType kind of goes hand in logical hand here as well, no?
        public string Name = string.Empty;
        public int Order;
    }

    public class GenericInfo
    {
        // =======================================================================================================
        // This entire class existed in 4.8 and was brought back for schema support. All API's here existed internal in 4.8.
        public IList<GenericInfo>? Parameters => null;
        public XmlQualifiedName StableName => XmlQualifiedName.Empty;   // Can these stable names return null?
        public XmlQualifiedName GetExpandedStableName() { return StableName; }
    }

    public class DataContract
    {
        // =======================================================================================================
        // These existed internal in Core
        public Type UnderlyingType => typeof(DataContract);
        public Type OriginalUnderlyingType => UnderlyingType;
        public XmlQualifiedName StableName { get => XmlQualifiedName.Empty; internal set { } }
        public bool HasRoot { get; internal set; }
        public bool IsBuiltInDataContract { get; }
        public bool IsISerializable { get; internal set; }
        public bool IsReference { get; internal set; }
        public bool IsValueType { get; internal set; }
        public XmlDictionaryString? TopLevelElementName { get; internal set; }
        public XmlDictionaryString? TopLevelElementNamespace { get; internal set; }
        public static bool IsTypeSerializable(Type type) { return false; }
        public static DataContract GetDataContract(Type type) { return new DataContract(); }
        public static DataContract? GetBuiltInDataContract(string name, string ns) { return null; }
        public static XmlQualifiedName GetStableName(Type type) { return XmlQualifiedName.Empty; }
        // This API exists and is used internally. It can easily be copied externally if we want to reduce the API surface here.
        // But it's also a semi-logical exposure of a DC-intended static utility function. It makes sense to just expose and re-use it.
        public static string EncodeLocalName(string localName) { return "string"; }
        public XmlQualifiedName GetArrayTypeName(bool isNullable) { return XmlQualifiedName.Empty; }


        // =======================================================================================================
        // These existed internal in 4.8 and are brought back for schema support
        public GenericInfo? GenericInfo => null;
        public DataContract BindGenericParameters(DataContract[] paramContracts,  Dictionary<DataContract, DataContract> boundContracts) { return new DataContract(); }


        // =======================================================================================================
        // These are new API's, internal or otherwise.

        // This one - similar to a couple DCSet API's - allows us to keep surrogate execution logic internal to DC/DCSet.
        //  Basically and entry to [surrogate_caller.GetDataContractType()]
        public static Type GetSurrogateType(ISerializationSurrogateProvider surrogateProvider, Type type) { return type; }
    }



    public class ClassDataContract : DataContract
    {
        // BaseContract
        // Members
    }
    public class CollectionDataContract : DataContract
    {
        // IsCollection() static - This could be on base DataContract?
        // IsDictionary
        // IsItemTypeNullable
        // ItemContract
        // ItemName
        // KeyName
        // ValueName
    }
    public class EnumDataContract : DataContract
    {
        // BaseContractName
        // GetBaseType  << and ^^ can be combined into one Type GetType()

        // GetStringFromEnumValue()     This is the one non-data item. Should we bring an enum-specific function up to DataContract? or perhaps this and IsUlong can go away if Values just returns int[] or string[] appropriately
        // IsULong
        // IsFlags
        // Members
        // Values
    }
    public class PrimitiveDataContract : DataContract { }
    public class XmlDataContract : DataContract
    {
        // IsAnonymous
        // IsTopLevelElementNullable
        // IsTypeDefinedOnImport
        // XsdType
    }
}
