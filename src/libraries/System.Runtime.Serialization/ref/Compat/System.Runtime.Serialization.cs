// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Runtime.Serialization
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited=false, AllowMultiple=false)]
    public sealed partial class CollectionDataContractAttribute : System.Attribute
    {
        public CollectionDataContractAttribute() { }
        public bool IsItemNameSetExplicitly { get { return default(bool); } }
        public bool IsKeyNameSetExplicitly { get { return default(bool); } }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsNamespaceSetExplicitly { get { return default(bool); } }
        public bool IsReference { get { return default(bool); } set { } }
        public bool IsReferenceSetExplicitly { get { return default(bool); } }
        public bool IsValueNameSetExplicitly { get { return default(bool); } }
        public string ItemName { get { return default(string); } set { } }
        public string KeyName { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public string ValueName { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), Inherited=false, AllowMultiple=true)]
    public sealed partial class ContractNamespaceAttribute : System.Attribute
    {
        public ContractNamespaceAttribute(string contractNamespace) { }
        public string ClrNamespace { get { return default(string); } set { } }
        public string ContractNamespace { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(28), Inherited=false, AllowMultiple=false)]
    public sealed partial class DataContractAttribute : System.Attribute
    {
        public DataContractAttribute() { }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsNamespaceSetExplicitly { get { return default(bool); } }
        public bool IsReference { get { return default(bool); } set { } }
        public bool IsReferenceSetExplicitly { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    public abstract partial class DataContractResolver
    {
        protected DataContractResolver() { }
        public abstract System.Type ResolveName(string typeName, string typeNamespace, System.Type declaredType, System.Runtime.Serialization.DataContractResolver knownTypeResolver);
        public abstract bool TryResolveType(System.Type type, System.Type declaredType, System.Runtime.Serialization.DataContractResolver knownTypeResolver, out System.Xml.XmlDictionaryString typeName, out System.Xml.XmlDictionaryString typeNamespace);
    }
    public sealed partial class DataContractSerializer : System.Runtime.Serialization.XmlObjectSerializer
    {
        public DataContractSerializer(System.Type type) { }
        public DataContractSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate) { }
        public DataContractSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, System.Runtime.Serialization.DataContractResolver dataContractResolver) { }
        public DataContractSerializer(System.Type type, System.Runtime.Serialization.DataContractSerializerSettings settings) { }
        public DataContractSerializer(System.Type type, string rootName, string rootNamespace) { }
        public DataContractSerializer(System.Type type, string rootName, string rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractSerializer(System.Type type, string rootName, string rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate) { }
        public DataContractSerializer(System.Type type, string rootName, string rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, System.Runtime.Serialization.DataContractResolver dataContractResolver) { }
        public DataContractSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace) { }
        public DataContractSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate) { }
        public DataContractSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, bool preserveObjectReferences, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, System.Runtime.Serialization.DataContractResolver dataContractResolver) { }
        public System.Runtime.Serialization.DataContractResolver DataContractResolver { get { return default(System.Runtime.Serialization.DataContractResolver); } }
        public System.Runtime.Serialization.IDataContractSurrogate DataContractSurrogate { get { return default(System.Runtime.Serialization.IDataContractSurrogate); } }
        public bool IgnoreExtensionDataObject { get { return default(bool); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Type> KnownTypes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Type>); } }
        public int MaxItemsInObjectGraph { get { return default(int); } }
        public bool PreserveObjectReferences { get { return default(bool); } }
        public bool SerializeReadOnlyTypes { get { return default(bool); } }
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader) { return default(bool); }
        public override bool IsStartObject(System.Xml.XmlReader reader) { return default(bool); }
        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName) { return default(object); }
        public object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName, System.Runtime.Serialization.DataContractResolver dataContractResolver) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader, bool verifyObjectName) { return default(object); }
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer) { }
        public override void WriteEndObject(System.Xml.XmlWriter writer) { }
        public void WriteObject(System.Xml.XmlDictionaryWriter writer, object graph, System.Runtime.Serialization.DataContractResolver dataContractResolver) { }
        public override void WriteObject(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlWriter writer, object graph) { }
    }
    public partial class DataContractSerializerSettings
    {
        public DataContractSerializerSettings() { }
        public System.Runtime.Serialization.DataContractResolver DataContractResolver { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.DataContractResolver); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Runtime.Serialization.IDataContractSurrogate DataContractSurrogate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.IDataContractSurrogate); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IgnoreExtensionDataObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<System.Type> KnownTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.Generic.IEnumerable<System.Type>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxItemsInObjectGraph { get { return default(int); } set { } }
        public bool PreserveObjectReferences { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Xml.XmlDictionaryString RootName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Xml.XmlDictionaryString); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Xml.XmlDictionaryString RootNamespace { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Xml.XmlDictionaryString); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SerializeReadOnlyTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false, AllowMultiple=false)]
    public sealed partial class DataMemberAttribute : System.Attribute
    {
        public DataMemberAttribute() { }
        public bool EmitDefaultValue { get { return default(bool); } set { } }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsRequired { get { return default(bool); } set { } }
        public string Name { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    public partial class DateTimeFormat
    {
        public DateTimeFormat(string formatString) { }
        public DateTimeFormat(string formatString, System.IFormatProvider formatProvider) { }
        public System.Globalization.DateTimeStyles DateTimeStyles { get { return default(System.Globalization.DateTimeStyles); } set { } }
        public System.IFormatProvider FormatProvider { get { return default(System.IFormatProvider); } }
        public string FormatString { get { return default(string); } }
    }
    public enum EmitTypeInformation
    {
        Always = 1,
        AsNeeded = 0,
        Never = 2,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited=false, AllowMultiple=false)]
    public sealed partial class EnumMemberAttribute : System.Attribute
    {
        public EnumMemberAttribute() { }
        public bool IsValueSetExplicitly { get { return default(bool); } }
        public string Value { get { return default(string); } set { } }
    }
    public partial class ExportOptions
    {
        public ExportOptions() { }
        public System.Runtime.Serialization.IDataContractSurrogate DataContractSurrogate { get { return default(System.Runtime.Serialization.IDataContractSurrogate); } set { } }
        public System.Collections.ObjectModel.Collection<System.Type> KnownTypes { get { return default(System.Collections.ObjectModel.Collection<System.Type>); } }
    }
    public sealed partial class ExtensionDataObject
    {
        internal ExtensionDataObject() { }
    }
    public partial interface IDataContractSurrogate
    {
        object GetCustomDataToExport(System.Reflection.MemberInfo memberInfo, System.Type dataContractType);
        object GetCustomDataToExport(System.Type clrType, System.Type dataContractType);
        System.Type GetDataContractType(System.Type type);
        object GetDeserializedObject(object obj, System.Type targetType);
        void GetKnownCustomDataTypes(System.Collections.ObjectModel.Collection<System.Type> customDataTypes);
        object GetObjectToSerialize(object obj, System.Type targetType);
        System.Type GetReferencedTypeOnImport(string typeName, string typeNamespace, object customData);
    }
    public partial interface IExtensibleDataObject
    {
        System.Runtime.Serialization.ExtensionDataObject ExtensionData { get; set; }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited=false, AllowMultiple=false)]
    public sealed partial class IgnoreDataMemberAttribute : System.Attribute
    {
        public IgnoreDataMemberAttribute() { }
    }
    public partial class InvalidDataContractException : System.Exception
    {
        public InvalidDataContractException() { }
        protected InvalidDataContractException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public InvalidDataContractException(string message) { }
        public InvalidDataContractException(string message, System.Exception innerException) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited=true, AllowMultiple=true)]
    public sealed partial class KnownTypeAttribute : System.Attribute
    {
        public KnownTypeAttribute(string methodName) { }
        public KnownTypeAttribute(System.Type type) { }
        public string MethodName { get { return default(string); } }
        public System.Type Type { get { return default(System.Type); } }
    }
    public sealed partial class NetDataContractSerializer : System.Runtime.Serialization.XmlObjectSerializer, System.Runtime.Serialization.IFormatter
    {
        public NetDataContractSerializer() { }
        public NetDataContractSerializer(System.Runtime.Serialization.StreamingContext context) { }
        public NetDataContractSerializer(System.Runtime.Serialization.StreamingContext context, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.Formatters.FormatterAssemblyStyle assemblyFormat, System.Runtime.Serialization.ISurrogateSelector surrogateSelector) { }
        public NetDataContractSerializer(string rootName, string rootNamespace) { }
        public NetDataContractSerializer(string rootName, string rootNamespace, System.Runtime.Serialization.StreamingContext context, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.Formatters.FormatterAssemblyStyle assemblyFormat, System.Runtime.Serialization.ISurrogateSelector surrogateSelector) { }
        public NetDataContractSerializer(System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace) { }
        public NetDataContractSerializer(System.Xml.XmlDictionaryString rootName, System.Xml.XmlDictionaryString rootNamespace, System.Runtime.Serialization.StreamingContext context, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.Formatters.FormatterAssemblyStyle assemblyFormat, System.Runtime.Serialization.ISurrogateSelector surrogateSelector) { }
        public System.Runtime.Serialization.Formatters.FormatterAssemblyStyle AssemblyFormat { get { return default(System.Runtime.Serialization.Formatters.FormatterAssemblyStyle); } set { } }
        public System.Runtime.Serialization.SerializationBinder Binder { get { return default(System.Runtime.Serialization.SerializationBinder); } set { } }
        public System.Runtime.Serialization.StreamingContext Context { get { return default(System.Runtime.Serialization.StreamingContext); } set { } }
        public bool IgnoreExtensionDataObject { get { return default(bool); } }
        public int MaxItemsInObjectGraph { get { return default(int); } }
        public System.Runtime.Serialization.ISurrogateSelector SurrogateSelector { get { return default(System.Runtime.Serialization.ISurrogateSelector); } set { } }
        public object Deserialize(System.IO.Stream stream) { return default(object); }
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader) { return default(bool); }
        public override bool IsStartObject(System.Xml.XmlReader reader) { return default(bool); }
        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader, bool verifyObjectName) { return default(object); }
        public void Serialize(System.IO.Stream stream, object graph) { }
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer) { }
        public override void WriteEndObject(System.Xml.XmlWriter writer) { }
        public override void WriteObject(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlWriter writer, object graph) { }
    }
    public abstract partial class XmlObjectSerializer
    {
        protected XmlObjectSerializer() { }
        public abstract bool IsStartObject(System.Xml.XmlDictionaryReader reader);
        public virtual bool IsStartObject(System.Xml.XmlReader reader) { return default(bool); }
        public virtual object ReadObject(System.IO.Stream stream) { return default(object); }
        public virtual object ReadObject(System.Xml.XmlDictionaryReader reader) { return default(object); }
        public abstract object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName);
        public virtual object ReadObject(System.Xml.XmlReader reader) { return default(object); }
        public virtual object ReadObject(System.Xml.XmlReader reader, bool verifyObjectName) { return default(object); }
        public abstract void WriteEndObject(System.Xml.XmlDictionaryWriter writer);
        public virtual void WriteEndObject(System.Xml.XmlWriter writer) { }
        public virtual void WriteObject(System.IO.Stream stream, object graph) { }
        public virtual void WriteObject(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public virtual void WriteObject(System.Xml.XmlWriter writer, object graph) { }
        public abstract void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph);
        public virtual void WriteObjectContent(System.Xml.XmlWriter writer, object graph) { }
        public abstract void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph);
        public virtual void WriteStartObject(System.Xml.XmlWriter writer, object graph) { }
    }
    public static partial class XmlSerializableServices
    {
        public static System.Xml.XmlNode[] ReadNodes(System.Xml.XmlReader xmlReader) { return default(System.Xml.XmlNode[]); }
        public static void WriteNodes(System.Xml.XmlWriter xmlWriter, System.Xml.XmlNode[] nodes) { }
    }
    public static partial class XPathQueryGenerator
    {
        public static string CreateFromDataContractSerializer(System.Type type, System.Reflection.MemberInfo[] pathToMember, System.Text.StringBuilder rootElementXpath, out System.Xml.XmlNamespaceManager namespaces) { namespaces = default(System.Xml.XmlNamespaceManager); return default(string); }
        public static string CreateFromDataContractSerializer(System.Type type, System.Reflection.MemberInfo[] pathToMember, out System.Xml.XmlNamespaceManager namespaces) { namespaces = default(System.Xml.XmlNamespaceManager); return default(string); }
    }
}
namespace System.Runtime.Serialization.Json
{
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed partial class DataContractJsonSerializer : System.Runtime.Serialization.XmlObjectSerializer
    {
        public DataContractJsonSerializer(System.Type type) { }
        public DataContractJsonSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractJsonSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, bool alwaysEmitTypeInformation) { }
        public DataContractJsonSerializer(System.Type type, System.Runtime.Serialization.Json.DataContractJsonSerializerSettings settings) { }
        public DataContractJsonSerializer(System.Type type, string rootName) { }
        public DataContractJsonSerializer(System.Type type, string rootName, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractJsonSerializer(System.Type type, string rootName, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, bool alwaysEmitTypeInformation) { }
        public DataContractJsonSerializer(System.Type type, System.Xml.XmlDictionaryString rootName) { }
        public DataContractJsonSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractJsonSerializer(System.Type type, System.Xml.XmlDictionaryString rootName, System.Collections.Generic.IEnumerable<System.Type> knownTypes, int maxItemsInObjectGraph, bool ignoreExtensionDataObject, System.Runtime.Serialization.IDataContractSurrogate dataContractSurrogate, bool alwaysEmitTypeInformation) { }
        public System.Runtime.Serialization.IDataContractSurrogate DataContractSurrogate { get { return default(System.Runtime.Serialization.IDataContractSurrogate); } }
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { get { return default(System.Runtime.Serialization.DateTimeFormat); } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { return default(System.Runtime.Serialization.EmitTypeInformation); } }
        public bool IgnoreExtensionDataObject { get { return default(bool); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Type> KnownTypes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Type>); } }
        public int MaxItemsInObjectGraph { get { return default(int); } }
        public bool SerializeReadOnlyTypes { get { return default(bool); } }
        public bool UseSimpleDictionaryFormat { get { return default(bool); } }
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader) { return default(bool); }
        public override bool IsStartObject(System.Xml.XmlReader reader) { return default(bool); }
        public override object ReadObject(System.IO.Stream stream) { return default(object); }
        public override object ReadObject(System.Xml.XmlDictionaryReader reader) { return default(object); }
        public override object ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader) { return default(object); }
        public override object ReadObject(System.Xml.XmlReader reader, bool verifyObjectName) { return default(object); }
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer) { }
        public override void WriteEndObject(System.Xml.XmlWriter writer) { }
        public override void WriteObject(System.IO.Stream stream, object graph) { }
        public override void WriteObject(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteObject(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteObjectContent(System.Xml.XmlWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object graph) { }
        public override void WriteStartObject(System.Xml.XmlWriter writer, object graph) { }
    }
    public partial class DataContractJsonSerializerSettings
    {
        public DataContractJsonSerializerSettings() { }
        public System.Runtime.Serialization.IDataContractSurrogate DataContractSurrogate { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.IDataContractSurrogate); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.DateTimeFormat); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Runtime.Serialization.EmitTypeInformation); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool IgnoreExtensionDataObject { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public System.Collections.Generic.IEnumerable<System.Type> KnownTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(System.Collections.Generic.IEnumerable<System.Type>); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public int MaxItemsInObjectGraph { get { return default(int); } set { } }
        public string RootName { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(string); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool SerializeReadOnlyTypes { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
        public bool UseSimpleDictionaryFormat { [System.Runtime.CompilerServices.CompilerGeneratedAttribute]get { return default(bool); } [System.Runtime.CompilerServices.CompilerGeneratedAttribute]set { } }
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public partial interface IXmlJsonReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose);
        void SetInput(System.IO.Stream stream, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose);
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public partial interface IXmlJsonWriterInitializer
    {
        void SetOutput(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream);
    }
    [System.Runtime.CompilerServices.TypeForwardedFromAttribute("System.ServiceModel.Web, Version=3.5.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public static partial class JsonReaderWriterFactory
    {
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(System.IO.Stream stream, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(System.IO.Stream stream, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream, bool indent) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream, bool indent, string indentChars) { return default(System.Xml.XmlDictionaryWriter); }
    }
}
namespace System.Xml
{
    public partial interface IFragmentCapableXmlDictionaryWriter
    {
        bool CanFragment { get; }
        void EndFragment();
        void StartFragment(System.IO.Stream stream, bool generateSelfContainedTextFragment);
        void WriteFragment(byte[] buffer, int offset, int count);
    }
    public partial interface IStreamProvider
    {
        System.IO.Stream GetStream();
        void ReleaseStream(System.IO.Stream stream);
    }
    public partial interface IXmlBinaryReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session, System.Xml.OnXmlDictionaryReaderClose onClose);
        void SetInput(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session, System.Xml.OnXmlDictionaryReaderClose onClose);
    }
    public partial interface IXmlBinaryWriterInitializer
    {
        void SetOutput(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlBinaryWriterSession session, bool ownsStream);
    }
    public partial interface IXmlDictionary
    {
        bool TryLookup(int key, out System.Xml.XmlDictionaryString result);
        bool TryLookup(string value, out System.Xml.XmlDictionaryString result);
        bool TryLookup(System.Xml.XmlDictionaryString value, out System.Xml.XmlDictionaryString result);
    }
    public partial interface IXmlMtomReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas, int maxBufferSize, System.Xml.OnXmlDictionaryReaderClose onClose);
        void SetInput(System.IO.Stream stream, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas, int maxBufferSize, System.Xml.OnXmlDictionaryReaderClose onClose);
    }
    public partial interface IXmlMtomWriterInitializer
    {
        void SetOutput(System.IO.Stream stream, System.Text.Encoding encoding, int maxSizeInBytes, string startInfo, string boundary, string startUri, bool writeMessageHeaders, bool ownsStream);
    }
    public partial interface IXmlTextReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose);
        void SetInput(System.IO.Stream stream, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose);
    }
    public partial interface IXmlTextWriterInitializer
    {
        void SetOutput(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream);
    }
    public delegate void OnXmlDictionaryReaderClose(System.Xml.XmlDictionaryReader reader);
    public partial class UniqueId
    {
        public UniqueId() { }
        public UniqueId(byte[] guid) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UniqueId(byte[] guid, int offset) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UniqueId(char[] chars, int offset, int count) { }
        public UniqueId(System.Guid guid) { }
        [System.Security.SecuritySafeCriticalAttribute]
        public UniqueId(string value) { }
        public int CharArrayLength { [System.Security.SecuritySafeCriticalAttribute]get { return default(int); } }
        public bool IsGuid { get { return default(bool); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public static bool operator ==(System.Xml.UniqueId id1, System.Xml.UniqueId id2) { return default(bool); }
        public static bool operator !=(System.Xml.UniqueId id1, System.Xml.UniqueId id2) { return default(bool); }
        [System.Security.SecuritySafeCriticalAttribute]
        public int ToCharArray(char[] chars, int offset) { return default(int); }
        [System.Security.SecuritySafeCriticalAttribute]
        public override string ToString() { return default(string); }
        [System.Security.SecuritySafeCriticalAttribute]
        public bool TryGetGuid(byte[] buffer, int offset) { return default(bool); }
        public bool TryGetGuid(out System.Guid guid) { guid = default(System.Guid); return default(bool); }
    }
    public partial class XmlBinaryReaderSession : System.Xml.IXmlDictionary
    {
        public XmlBinaryReaderSession() { }
        public System.Xml.XmlDictionaryString Add(int id, string value) { return default(System.Xml.XmlDictionaryString); }
        public void Clear() { }
        public bool TryLookup(int key, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
        public bool TryLookup(string value, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
        public bool TryLookup(System.Xml.XmlDictionaryString value, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
    }
    public partial class XmlBinaryWriterSession
    {
        public XmlBinaryWriterSession() { }
        public void Reset() { }
        public virtual bool TryAdd(System.Xml.XmlDictionaryString value, out int key) { key = default(int); return default(bool); }
    }
    public partial class XmlDictionary : System.Xml.IXmlDictionary
    {
        public XmlDictionary() { }
        public XmlDictionary(int capacity) { }
        public static System.Xml.IXmlDictionary Empty { get { return default(System.Xml.IXmlDictionary); } }
        public virtual System.Xml.XmlDictionaryString Add(string value) { return default(System.Xml.XmlDictionaryString); }
        public virtual bool TryLookup(int key, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
        public virtual bool TryLookup(string value, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
        public virtual bool TryLookup(System.Xml.XmlDictionaryString value, out System.Xml.XmlDictionaryString result) { result = default(System.Xml.XmlDictionaryString); return default(bool); }
    }
    public abstract partial class XmlDictionaryReader : System.Xml.XmlReader
    {
        protected XmlDictionaryReader() { }
        public virtual bool CanCanonicalize { get { return default(bool); } }
        public virtual System.Xml.XmlDictionaryReaderQuotas Quotas { get { return default(System.Xml.XmlDictionaryReaderQuotas); } }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(byte[] buffer, int offset, int count, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(byte[] buffer, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.XmlBinaryReaderSession session, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateBinaryReader(System.IO.Stream stream, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateDictionaryReader(System.Xml.XmlReader reader) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas, int maxBufferSize, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(byte[] buffer, int offset, int count, System.Text.Encoding[] encodings, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(System.IO.Stream stream, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(System.IO.Stream stream, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(System.IO.Stream stream, System.Text.Encoding[] encodings, string contentType, System.Xml.XmlDictionaryReaderQuotas quotas, int maxBufferSize, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateMtomReader(System.IO.Stream stream, System.Text.Encoding[] encodings, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateTextReader(byte[] buffer, int offset, int count, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateTextReader(byte[] buffer, int offset, int count, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateTextReader(byte[] buffer, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateTextReader(System.IO.Stream stream, System.Text.Encoding encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose onClose) { return default(System.Xml.XmlDictionaryReader); }
        public static System.Xml.XmlDictionaryReader CreateTextReader(System.IO.Stream stream, System.Xml.XmlDictionaryReaderQuotas quotas) { return default(System.Xml.XmlDictionaryReader); }
        public virtual void EndCanonicalization() { }
        public virtual string GetAttribute(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(string); }
        public virtual void GetNonAtomizedNames(out string localName, out string namespaceUri) { localName = default(string); namespaceUri = default(string); }
        public virtual int IndexOfLocalName(string[] localNames, string namespaceUri) { return default(int); }
        public virtual int IndexOfLocalName(System.Xml.XmlDictionaryString[] localNames, System.Xml.XmlDictionaryString namespaceUri) { return default(int); }
        public virtual bool IsLocalName(string localName) { return default(bool); }
        public virtual bool IsLocalName(System.Xml.XmlDictionaryString localName) { return default(bool); }
        public virtual bool IsNamespaceUri(string namespaceUri) { return default(bool); }
        public virtual bool IsNamespaceUri(System.Xml.XmlDictionaryString namespaceUri) { return default(bool); }
        public virtual bool IsStartArray(out System.Type type) { type = default(System.Type); return default(bool); }
        public virtual bool IsStartElement(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(bool); }
        protected bool IsTextNode(System.Xml.XmlNodeType nodeType) { return default(bool); }
        public virtual void MoveToStartElement() { }
        public virtual void MoveToStartElement(string name) { }
        public virtual void MoveToStartElement(string localName, string namespaceUri) { }
        public virtual void MoveToStartElement(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public virtual int ReadArray(string localName, string namespaceUri, bool[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, System.DateTime[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, decimal[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, double[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, System.Guid[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, short[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, int[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, long[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, float[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(string localName, string namespaceUri, System.TimeSpan[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, bool[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.DateTime[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, decimal[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, double[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.Guid[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, short[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, int[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, long[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, float[] array, int offset, int count) { return default(int); }
        public virtual int ReadArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.TimeSpan[] array, int offset, int count) { return default(int); }
        public virtual bool[] ReadBooleanArray(string localName, string namespaceUri) { return default(bool[]); }
        public virtual bool[] ReadBooleanArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(bool[]); }
        public override object ReadContentAs(System.Type type, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
        public virtual byte[] ReadContentAsBase64() { return default(byte[]); }
        public virtual byte[] ReadContentAsBinHex() { return default(byte[]); }
        protected byte[] ReadContentAsBinHex(int maxByteArrayContentLength) { return default(byte[]); }
        public virtual int ReadContentAsChars(char[] chars, int offset, int count) { return default(int); }
        public override decimal ReadContentAsDecimal() { return default(decimal); }
        public override float ReadContentAsFloat() { return default(float); }
        public virtual System.Guid ReadContentAsGuid() { return default(System.Guid); }
        public virtual void ReadContentAsQualifiedName(out string localName, out string namespaceUri) { localName = default(string); namespaceUri = default(string); }
        public override string ReadContentAsString() { return default(string); }
        protected string ReadContentAsString(int maxStringContentLength) { return default(string); }
        public virtual string ReadContentAsString(string[] strings, out int index) { index = default(int); return default(string); }
        public virtual string ReadContentAsString(System.Xml.XmlDictionaryString[] strings, out int index) { index = default(int); return default(string); }
        public virtual System.TimeSpan ReadContentAsTimeSpan() { return default(System.TimeSpan); }
        public virtual System.Xml.UniqueId ReadContentAsUniqueId() { return default(System.Xml.UniqueId); }
        public virtual System.DateTime[] ReadDateTimeArray(string localName, string namespaceUri) { return default(System.DateTime[]); }
        public virtual System.DateTime[] ReadDateTimeArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(System.DateTime[]); }
        public virtual decimal[] ReadDecimalArray(string localName, string namespaceUri) { return default(decimal[]); }
        public virtual decimal[] ReadDecimalArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(decimal[]); }
        public virtual double[] ReadDoubleArray(string localName, string namespaceUri) { return default(double[]); }
        public virtual double[] ReadDoubleArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(double[]); }
        public virtual byte[] ReadElementContentAsBase64() { return default(byte[]); }
        public virtual byte[] ReadElementContentAsBinHex() { return default(byte[]); }
        public override bool ReadElementContentAsBoolean() { return default(bool); }
        public override System.DateTime ReadElementContentAsDateTime() { return default(System.DateTime); }
        public override decimal ReadElementContentAsDecimal() { return default(decimal); }
        public override double ReadElementContentAsDouble() { return default(double); }
        public override float ReadElementContentAsFloat() { return default(float); }
        public virtual System.Guid ReadElementContentAsGuid() { return default(System.Guid); }
        public override int ReadElementContentAsInt() { return default(int); }
        public override long ReadElementContentAsLong() { return default(long); }
        public override string ReadElementContentAsString() { return default(string); }
        public virtual System.TimeSpan ReadElementContentAsTimeSpan() { return default(System.TimeSpan); }
        public virtual System.Xml.UniqueId ReadElementContentAsUniqueId() { return default(System.Xml.UniqueId); }
        public virtual void ReadFullStartElement() { }
        public virtual void ReadFullStartElement(string name) { }
        public virtual void ReadFullStartElement(string localName, string namespaceUri) { }
        public virtual void ReadFullStartElement(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public virtual System.Guid[] ReadGuidArray(string localName, string namespaceUri) { return default(System.Guid[]); }
        public virtual System.Guid[] ReadGuidArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(System.Guid[]); }
        public virtual short[] ReadInt16Array(string localName, string namespaceUri) { return default(short[]); }
        public virtual short[] ReadInt16Array(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(short[]); }
        public virtual int[] ReadInt32Array(string localName, string namespaceUri) { return default(int[]); }
        public virtual int[] ReadInt32Array(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(int[]); }
        public virtual long[] ReadInt64Array(string localName, string namespaceUri) { return default(long[]); }
        public virtual long[] ReadInt64Array(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(long[]); }
        public virtual float[] ReadSingleArray(string localName, string namespaceUri) { return default(float[]); }
        public virtual float[] ReadSingleArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(float[]); }
        public virtual void ReadStartElement(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public override string ReadString() { return default(string); }
        protected string ReadString(int maxStringContentLength) { return default(string); }
        public virtual System.TimeSpan[] ReadTimeSpanArray(string localName, string namespaceUri) { return default(System.TimeSpan[]); }
        public virtual System.TimeSpan[] ReadTimeSpanArray(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { return default(System.TimeSpan[]); }
        public virtual int ReadValueAsBase64(byte[] buffer, int offset, int count) { return default(int); }
        public virtual void StartCanonicalization(System.IO.Stream stream, bool includeComments, string[] inclusivePrefixes) { }
        public virtual bool TryGetArrayLength(out int count) { count = default(int); return default(bool); }
        public virtual bool TryGetBase64ContentLength(out int length) { length = default(int); return default(bool); }
        public virtual bool TryGetLocalNameAsDictionaryString(out System.Xml.XmlDictionaryString localName) { localName = default(System.Xml.XmlDictionaryString); return default(bool); }
        public virtual bool TryGetNamespaceUriAsDictionaryString(out System.Xml.XmlDictionaryString namespaceUri) { namespaceUri = default(System.Xml.XmlDictionaryString); return default(bool); }
        public virtual bool TryGetValueAsDictionaryString(out System.Xml.XmlDictionaryString value) { value = default(System.Xml.XmlDictionaryString); return default(bool); }
    }
    public sealed partial class XmlDictionaryReaderQuotas
    {
        public XmlDictionaryReaderQuotas() { }
        public static System.Xml.XmlDictionaryReaderQuotas Max { get { return default(System.Xml.XmlDictionaryReaderQuotas); } }
        [System.ComponentModel.DefaultValueAttribute(16384)]
        public int MaxArrayLength { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(4096)]
        public int MaxBytesPerRead { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(32)]
        public int MaxDepth { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(16384)]
        public int MaxNameTableCharCount { get { return default(int); } set { } }
        [System.ComponentModel.DefaultValueAttribute(8192)]
        public int MaxStringContentLength { get { return default(int); } set { } }
        public System.Xml.XmlDictionaryReaderQuotaTypes ModifiedQuotas { get { return default(System.Xml.XmlDictionaryReaderQuotaTypes); } }
        public void CopyTo(System.Xml.XmlDictionaryReaderQuotas quotas) { }
    }
    [System.FlagsAttribute]
    public enum XmlDictionaryReaderQuotaTypes
    {
        MaxArrayLength = 4,
        MaxBytesPerRead = 8,
        MaxDepth = 1,
        MaxNameTableCharCount = 16,
        MaxStringContentLength = 2,
    }
    public partial class XmlDictionaryString
    {
        public XmlDictionaryString(System.Xml.IXmlDictionary dictionary, string value, int key) { }
        public System.Xml.IXmlDictionary Dictionary { get { return default(System.Xml.IXmlDictionary); } }
        public static System.Xml.XmlDictionaryString Empty { get { return default(System.Xml.XmlDictionaryString); } }
        public int Key { get { return default(int); } }
        public string Value { get { return default(string); } }
        public override string ToString() { return default(string); }
    }
    public abstract partial class XmlDictionaryWriter : System.Xml.XmlWriter
    {
        protected XmlDictionaryWriter() { }
        public virtual bool CanCanonicalize { get { return default(bool); } }
        public static System.Xml.XmlDictionaryWriter CreateBinaryWriter(System.IO.Stream stream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateBinaryWriter(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateBinaryWriter(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlBinaryWriterSession session) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateBinaryWriter(System.IO.Stream stream, System.Xml.IXmlDictionary dictionary, System.Xml.XmlBinaryWriterSession session, bool ownsStream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateDictionaryWriter(System.Xml.XmlWriter writer) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateMtomWriter(System.IO.Stream stream, System.Text.Encoding encoding, int maxSizeInBytes, string startInfo) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateMtomWriter(System.IO.Stream stream, System.Text.Encoding encoding, int maxSizeInBytes, string startInfo, string boundary, string startUri, bool writeMessageHeaders, bool ownsStream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateTextWriter(System.IO.Stream stream) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateTextWriter(System.IO.Stream stream, System.Text.Encoding encoding) { return default(System.Xml.XmlDictionaryWriter); }
        public static System.Xml.XmlDictionaryWriter CreateTextWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream) { return default(System.Xml.XmlDictionaryWriter); }
        public virtual void EndCanonicalization() { }
        public virtual void StartCanonicalization(System.IO.Stream stream, bool includeComments, string[] inclusivePrefixes) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, bool[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, System.DateTime[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, decimal[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, double[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, System.Guid[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, short[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, int[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, long[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, float[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, string localName, string namespaceUri, System.TimeSpan[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, bool[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.DateTime[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, decimal[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, double[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.Guid[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, short[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, int[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, long[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, float[] array, int offset, int count) { }
        public virtual void WriteArray(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, System.TimeSpan[] array, int offset, int count) { }
        public void WriteAttributeString(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, string value) { }
        public void WriteAttributeString(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, string value) { }
        public override System.Threading.Tasks.Task WriteBase64Async(byte[] buffer, int index, int count) { return default(System.Threading.Tasks.Task); }
        public void WriteElementString(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, string value) { }
        public void WriteElementString(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri, string value) { }
        public virtual void WriteNode(System.Xml.XmlDictionaryReader reader, bool defattr) { }
        public override void WriteNode(System.Xml.XmlReader reader, bool defattr) { }
        public virtual void WriteQualifiedName(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public virtual void WriteStartAttribute(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public void WriteStartAttribute(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public virtual void WriteStartElement(string prefix, System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public void WriteStartElement(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString namespaceUri) { }
        public virtual void WriteString(System.Xml.XmlDictionaryString value) { }
        protected virtual void WriteTextNode(System.Xml.XmlDictionaryReader reader, bool isAttribute) { }
        public virtual void WriteValue(System.Guid value) { }
        public virtual void WriteValue(System.TimeSpan value) { }
        public virtual void WriteValue(System.Xml.IStreamProvider value) { }
        public virtual void WriteValue(System.Xml.UniqueId value) { }
        public virtual void WriteValue(System.Xml.XmlDictionaryString value) { }
        public virtual System.Threading.Tasks.Task WriteValueAsync(System.Xml.IStreamProvider value) { return default(System.Threading.Tasks.Task); }
        public virtual void WriteXmlAttribute(string localName, string value) { }
        public virtual void WriteXmlAttribute(System.Xml.XmlDictionaryString localName, System.Xml.XmlDictionaryString value) { }
        public virtual void WriteXmlnsAttribute(string prefix, string namespaceUri) { }
        public virtual void WriteXmlnsAttribute(string prefix, System.Xml.XmlDictionaryString namespaceUri) { }
    }
}
