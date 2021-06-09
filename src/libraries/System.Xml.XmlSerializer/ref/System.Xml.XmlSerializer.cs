// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Xml.Serialization
{
    [System.FlagsAttribute]
    public enum CodeGenerationOptions
    {
        [System.Xml.Serialization.XmlIgnoreAttribute]
        None = 0,
        [System.Xml.Serialization.XmlEnumAttribute("properties")]
        GenerateProperties = 1,
        [System.Xml.Serialization.XmlEnumAttribute("newAsync")]
        GenerateNewAsync = 2,
        [System.Xml.Serialization.XmlEnumAttribute("oldAsync")]
        GenerateOldAsync = 4,
        [System.Xml.Serialization.XmlEnumAttribute("order")]
        GenerateOrder = 8,
        [System.Xml.Serialization.XmlEnumAttribute("enableDataBinding")]
        EnableDataBinding = 16,
    }
    public partial class CodeIdentifier
    {
        [System.ObsoleteAttribute("This class should never get constructed as it contains only static methods.")]
        public CodeIdentifier() { }
        public static string MakeCamel(string identifier) { throw null; }
        public static string MakePascal(string identifier) { throw null; }
        public static string MakeValid(string identifier) { throw null; }
    }
    public partial class CodeIdentifiers
    {
        public CodeIdentifiers() { }
        public CodeIdentifiers(bool caseSensitive) { }
        public bool UseCamelCasing { get { throw null; } set { } }
        public void Add(string identifier, object? value) { }
        public void AddReserved(string identifier) { }
        public string AddUnique(string identifier, object? value) { throw null; }
        public void Clear() { }
        public bool IsInUse(string identifier) { throw null; }
        public string MakeRightCase(string identifier) { throw null; }
        public string MakeUnique(string identifier) { throw null; }
        public void Remove(string identifier) { }
        public void RemoveReserved(string identifier) { }
        public object ToArray(System.Type type) { throw null; }
    }
    public partial class ImportContext
    {
        public ImportContext(System.Xml.Serialization.CodeIdentifiers? identifiers, bool shareTypes) { }
        public bool ShareTypes { get { throw null; } }
        public System.Xml.Serialization.CodeIdentifiers TypeIdentifiers { get { throw null; } }
        public System.Collections.Specialized.StringCollection Warnings { get { throw null; } }
    }
    public partial interface IXmlTextParser
    {
        bool Normalized { get; set; }
        System.Xml.WhitespaceHandling WhitespaceHandling { get; set; }
    }
    public abstract partial class SchemaImporter
    {
        internal SchemaImporter() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class SoapAttributeAttribute : System.Attribute
    {
        public SoapAttributeAttribute() { }
        public SoapAttributeAttribute(string attributeName) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string AttributeName { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
    }
    public partial class SoapAttributeOverrides
    {
        public SoapAttributeOverrides() { }
        public System.Xml.Serialization.SoapAttributes? this[System.Type type] { get { throw null; } }
        public System.Xml.Serialization.SoapAttributes? this[System.Type type, string member] { get { throw null; } }
        public void Add(System.Type type, string member, System.Xml.Serialization.SoapAttributes? attributes) { }
        public void Add(System.Type type, System.Xml.Serialization.SoapAttributes? attributes) { }
    }
    public partial class SoapAttributes
    {
        public SoapAttributes() { }
        public SoapAttributes(System.Reflection.ICustomAttributeProvider provider) { }
        public System.Xml.Serialization.SoapAttributeAttribute? SoapAttribute { get { throw null; } set { } }
        public object? SoapDefaultValue { get { throw null; } set { } }
        public System.Xml.Serialization.SoapElementAttribute? SoapElement { get { throw null; } set { } }
        public System.Xml.Serialization.SoapEnumAttribute? SoapEnum { get { throw null; } set { } }
        public bool SoapIgnore { get { throw null; } set { } }
        public System.Xml.Serialization.SoapTypeAttribute? SoapType { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class SoapElementAttribute : System.Attribute
    {
        public SoapElementAttribute() { }
        public SoapElementAttribute(string? elementName) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ElementName { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field)]
    public partial class SoapEnumAttribute : System.Attribute
    {
        public SoapEnumAttribute() { }
        public SoapEnumAttribute(string name) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string Name { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue)]
    public partial class SoapIgnoreAttribute : System.Attribute
    {
        public SoapIgnoreAttribute() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true)]
    public partial class SoapIncludeAttribute : System.Attribute
    {
        public SoapIncludeAttribute(System.Type type) { }
        public System.Type Type { get { throw null; } set { } }
    }
    public partial class SoapReflectionImporter
    {
        public SoapReflectionImporter() { }
        public SoapReflectionImporter(string? defaultNamespace) { }
        public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides? attributeOverrides) { }
        public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides? attributeOverrides, string? defaultNamespace) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, System.Xml.Serialization.XmlMappingAccess access) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void IncludeType(System.Type type) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void IncludeTypes(System.Reflection.ICustomAttributeProvider provider) { }
    }
    public partial class SoapSchemaMember
    {
        public SoapSchemaMember() { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string MemberName { get { throw null; } set { } }
        public System.Xml.XmlQualifiedName? MemberType { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct)]
    public partial class SoapTypeAttribute : System.Attribute
    {
        public SoapTypeAttribute() { }
        public SoapTypeAttribute(string? typeName) { }
        public SoapTypeAttribute(string? typeName, string? ns) { }
        public bool IncludeInSchema { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string TypeName { get { throw null; } set { } }
    }
    public partial class UnreferencedObjectEventArgs : System.EventArgs
    {
        public UnreferencedObjectEventArgs(object? o, string? id) { }
        public string? UnreferencedId { get { throw null; } }
        public object? UnreferencedObject { get { throw null; } }
    }
    public delegate void UnreferencedObjectEventHandler(object? sender, System.Xml.Serialization.UnreferencedObjectEventArgs e);
    public partial class XmlAnyElementAttributes : System.Collections.CollectionBase
    {
        public XmlAnyElementAttributes() { }
        public System.Xml.Serialization.XmlAnyElementAttribute? this[int index] { get { throw null; } set { } }
        public int Add(System.Xml.Serialization.XmlAnyElementAttribute? attribute) { throw null; }
        public bool Contains(System.Xml.Serialization.XmlAnyElementAttribute? attribute) { throw null; }
        public void CopyTo(System.Xml.Serialization.XmlAnyElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlAnyElementAttribute? attribute) { throw null; }
        public void Insert(int index, System.Xml.Serialization.XmlAnyElementAttribute? attribute) { }
        public void Remove(System.Xml.Serialization.XmlAnyElementAttribute? attribute) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple=false)]
    public partial class XmlArrayAttribute : System.Attribute
    {
        public XmlArrayAttribute() { }
        public XmlArrayAttribute(string? elementName) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ElementName { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        public int Order { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple=true)]
    public partial class XmlArrayItemAttribute : System.Attribute
    {
        public XmlArrayItemAttribute() { }
        public XmlArrayItemAttribute(string? elementName) { }
        public XmlArrayItemAttribute(string? elementName, System.Type? type) { }
        public XmlArrayItemAttribute(System.Type? type) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string DataType { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string ElementName { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { throw null; } set { } }
        public bool IsNullable { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        public int NestingLevel { get { throw null; } set { } }
        public System.Type? Type { get { throw null; } set { } }
    }
    public partial class XmlArrayItemAttributes : System.Collections.CollectionBase
    {
        public XmlArrayItemAttributes() { }
        public System.Xml.Serialization.XmlArrayItemAttribute? this[int index] { get { throw null; } set { } }
        public int Add(System.Xml.Serialization.XmlArrayItemAttribute? attribute) { throw null; }
        public bool Contains(System.Xml.Serialization.XmlArrayItemAttribute? attribute) { throw null; }
        public void CopyTo(System.Xml.Serialization.XmlArrayItemAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlArrayItemAttribute? attribute) { throw null; }
        public void Insert(int index, System.Xml.Serialization.XmlArrayItemAttribute? attribute) { }
        public void Remove(System.Xml.Serialization.XmlArrayItemAttribute? attribute) { }
    }
    public partial class XmlAttributeEventArgs : System.EventArgs
    {
        internal XmlAttributeEventArgs() { }
        public System.Xml.XmlAttribute Attr { get { throw null; } }
        public string ExpectedAttributes { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public object? ObjectBeingDeserialized { get { throw null; } }
    }
    public delegate void XmlAttributeEventHandler(object? sender, System.Xml.Serialization.XmlAttributeEventArgs e);
    public partial class XmlAttributeOverrides
    {
        public XmlAttributeOverrides() { }
        public System.Xml.Serialization.XmlAttributes? this[System.Type type] { get { throw null; } }
        public System.Xml.Serialization.XmlAttributes? this[System.Type type, string member] { get { throw null; } }
        public void Add(System.Type type, string member, System.Xml.Serialization.XmlAttributes? attributes) { }
        public void Add(System.Type type, System.Xml.Serialization.XmlAttributes attributes) { }
    }
    public partial class XmlAttributes
    {
        public XmlAttributes() { }
        public XmlAttributes(System.Reflection.ICustomAttributeProvider provider) { }
        public System.Xml.Serialization.XmlAnyAttributeAttribute? XmlAnyAttribute { get { throw null; } set { } }
        public System.Xml.Serialization.XmlAnyElementAttributes XmlAnyElements { get { throw null; } }
        public System.Xml.Serialization.XmlArrayAttribute? XmlArray { get { throw null; } set { } }
        public System.Xml.Serialization.XmlArrayItemAttributes XmlArrayItems { get { throw null; } }
        public System.Xml.Serialization.XmlAttributeAttribute? XmlAttribute { get { throw null; } set { } }
        public System.Xml.Serialization.XmlChoiceIdentifierAttribute? XmlChoiceIdentifier { get { throw null; } }
        public object? XmlDefaultValue { get { throw null; } set { } }
        public System.Xml.Serialization.XmlElementAttributes XmlElements { get { throw null; } }
        public System.Xml.Serialization.XmlEnumAttribute? XmlEnum { get { throw null; } set { } }
        public bool XmlIgnore { get { throw null; } set { } }
        public bool Xmlns { get { throw null; } set { } }
        public System.Xml.Serialization.XmlRootAttribute? XmlRoot { get { throw null; } set { } }
        public System.Xml.Serialization.XmlTextAttribute? XmlText { get { throw null; } set { } }
        public System.Xml.Serialization.XmlTypeAttribute? XmlType { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Field | System.AttributeTargets.Parameter | System.AttributeTargets.Property | System.AttributeTargets.ReturnValue, AllowMultiple=false)]
    public partial class XmlChoiceIdentifierAttribute : System.Attribute
    {
        public XmlChoiceIdentifierAttribute() { }
        public XmlChoiceIdentifierAttribute(string? name) { }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string MemberName { get { throw null; } set { } }
    }
    public partial struct XmlDeserializationEvents
    {
        private object _dummy;
        private int _dummyPrimitive;
        public System.Xml.Serialization.XmlAttributeEventHandler? OnUnknownAttribute { get { throw null; } set { } }
        public System.Xml.Serialization.XmlElementEventHandler? OnUnknownElement { get { throw null; } set { } }
        public System.Xml.Serialization.XmlNodeEventHandler? OnUnknownNode { get { throw null; } set { } }
        public System.Xml.Serialization.UnreferencedObjectEventHandler? OnUnreferencedObject { get { throw null; } set { } }
    }
    public partial class XmlElementAttributes : System.Collections.CollectionBase
    {
        public XmlElementAttributes() { }
        public System.Xml.Serialization.XmlElementAttribute? this[int index] { get { throw null; } set { } }
        public int Add(System.Xml.Serialization.XmlElementAttribute? attribute) { throw null; }
        public bool Contains(System.Xml.Serialization.XmlElementAttribute? attribute) { throw null; }
        public void CopyTo(System.Xml.Serialization.XmlElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlElementAttribute? attribute) { throw null; }
        public void Insert(int index, System.Xml.Serialization.XmlElementAttribute? attribute) { }
        public void Remove(System.Xml.Serialization.XmlElementAttribute? attribute) { }
    }
    public partial class XmlElementEventArgs : System.EventArgs
    {
        internal XmlElementEventArgs() { }
        public System.Xml.XmlElement Element { get { throw null; } }
        public string ExpectedElements { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public object? ObjectBeingDeserialized { get { throw null; } }
    }
    public delegate void XmlElementEventHandler(object? sender, System.Xml.Serialization.XmlElementEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface | System.AttributeTargets.Method | System.AttributeTargets.Struct, AllowMultiple=true)]
    public partial class XmlIncludeAttribute : System.Attribute
    {
        public XmlIncludeAttribute(System.Type? type) { }
        public System.Type? Type { get { throw null; } set { } }
    }
    public abstract partial class XmlMapping
    {
        internal XmlMapping() { }
        public string ElementName { get { throw null; } }
        public string? Namespace { get { throw null; } }
        public string XsdElementName { get { throw null; } }
        public void SetKey(string? key) { }
    }
    [System.FlagsAttribute]
    public enum XmlMappingAccess
    {
        None = 0,
        Read = 1,
        Write = 2,
    }
    public partial class XmlMemberMapping
    {
        internal XmlMemberMapping() { }
        public bool Any { get { throw null; } }
        public bool CheckSpecified { get { throw null; } }
        public string ElementName { get { throw null; } }
        public string MemberName { get { throw null; } }
        public string? Namespace { get { throw null; } }
        public string TypeFullName { get { throw null; } }
        public string? TypeName { get { throw null; } }
        public string? TypeNamespace { get { throw null; } }
        public string XsdElementName { get { throw null; } }
    }
    public partial class XmlMembersMapping : System.Xml.Serialization.XmlMapping
    {
        internal XmlMembersMapping() { }
        public int Count { get { throw null; } }
        public System.Xml.Serialization.XmlMemberMapping this[int index] { get { throw null; } }
        public string? TypeName { get { throw null; } }
        public string? TypeNamespace { get { throw null; } }
    }
    public partial class XmlNodeEventArgs : System.EventArgs
    {
        internal XmlNodeEventArgs() { }
        public int LineNumber { get { throw null; } }
        public int LinePosition { get { throw null; } }
        public string LocalName { get { throw null; } }
        public string Name { get { throw null; } }
        public string NamespaceURI { get { throw null; } }
        public System.Xml.XmlNodeType NodeType { get { throw null; } }
        public object? ObjectBeingDeserialized { get { throw null; } }
        public string? Text { get { throw null; } }
    }
    public delegate void XmlNodeEventHandler(object? sender, System.Xml.Serialization.XmlNodeEventArgs e);
    public partial class XmlReflectionImporter
    {
        public XmlReflectionImporter() { }
        public XmlReflectionImporter(string? defaultNamespace) { }
        public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides? attributeOverrides) { }
        public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides? attributeOverrides, string? defaultNamespace) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string? elementName, string? ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel, System.Xml.Serialization.XmlMappingAccess access) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute? root) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute? root, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void IncludeType(System.Type type) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void IncludeTypes(System.Reflection.ICustomAttributeProvider provider) { }
    }
    public partial class XmlReflectionMember
    {
        public XmlReflectionMember() { }
        public bool IsReturnValue { get { throw null; } set { } }
        public string MemberName { get { throw null; } set { } }
        public System.Type? MemberType { get { throw null; } set { } }
        public bool OverrideIsNullable { get { throw null; } set { } }
        public System.Xml.Serialization.SoapAttributes SoapAttributes { get { throw null; } set { } }
        public System.Xml.Serialization.XmlAttributes XmlAttributes { get { throw null; } set { } }
    }
    public partial class XmlSchemaEnumerator : System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerator, System.IDisposable
    {
        public XmlSchemaEnumerator(System.Xml.Serialization.XmlSchemas list) { }
        public System.Xml.Schema.XmlSchema Current { get { throw null; } }
        object System.Collections.IEnumerator.Current { get { throw null; } }
        public void Dispose() { }
        public bool MoveNext() { throw null; }
        void System.Collections.IEnumerator.Reset() { }
    }
    public partial class XmlSchemaExporter
    {
        public XmlSchemaExporter(System.Xml.Serialization.XmlSchemas schemas) { }
        public string ExportAnyType(string? ns) { throw null; }
        public string? ExportAnyType(System.Xml.Serialization.XmlMembersMapping members) { throw null; }
        public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
        public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping, bool exportEnclosingType) { }
        public System.Xml.XmlQualifiedName? ExportTypeMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { throw null; }
        public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
    }
    public partial class XmlSchemaImporter : System.Xml.Serialization.SchemaImporter
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers? typeIdentifiers) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping? ImportAnyType(System.Xml.XmlQualifiedName typeName, string elementName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type? baseType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type? baseType, bool baseTypeCanBeIndirect) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string? ns, System.Xml.Serialization.SoapSchemaMember[] members) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName name) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names, System.Type? baseType, bool baseTypeCanBeIndirect) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type? baseType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type? baseType, bool baseTypeCanBeIndirect) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Xml.XmlQualifiedName name) { throw null; }
    }
    public partial class XmlSchemas : System.Collections.CollectionBase, System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerable
    {
        public XmlSchemas() { }
        public bool IsCompiled { get { throw null; } }
        public System.Xml.Schema.XmlSchema this[int index] { get { throw null; } set { } }
        public System.Xml.Schema.XmlSchema? this[string? ns] { get { throw null; } }
        public int Add(System.Xml.Schema.XmlSchema schema) { throw null; }
        public int Add(System.Xml.Schema.XmlSchema schema, System.Uri? baseUri) { throw null; }
        public void Add(System.Xml.Serialization.XmlSchemas schemas) { }
        public void AddReference(System.Xml.Schema.XmlSchema schema) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Compile(System.Xml.Schema.ValidationEventHandler? handler, bool fullCompile) { }
        public bool Contains(string? targetNamespace) { throw null; }
        public bool Contains(System.Xml.Schema.XmlSchema schema) { throw null; }
        public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
        public object? Find(System.Xml.XmlQualifiedName name, System.Type type) { throw null; }
        public System.Collections.IList GetSchemas(string? ns) { throw null; }
        public int IndexOf(System.Xml.Schema.XmlSchema schema) { throw null; }
        public void Insert(int index, System.Xml.Schema.XmlSchema schema) { }
        public static bool IsDataSet(System.Xml.Schema.XmlSchema schema) { throw null; }
        protected override void OnClear() { }
        protected override void OnInsert(int index, object? value) { }
        protected override void OnRemove(int index, object? value) { }
        protected override void OnSet(int index, object? oldValue, object? newValue) { }
        public void Remove(System.Xml.Schema.XmlSchema schema) { }
        System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema> System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>.GetEnumerator() { throw null; }
    }
    public delegate void XmlSerializationCollectionFixupCallback(object? collection, object? collectionItems);
    public delegate void XmlSerializationFixupCallback(object fixup);
    public abstract partial class XmlSerializationGeneratedCode
    {
        protected XmlSerializationGeneratedCode() { }
    }
    public delegate object? XmlSerializationReadCallback();
    public abstract partial class XmlSerializationReader : System.Xml.Serialization.XmlSerializationGeneratedCode
    {
        protected XmlSerializationReader() { }
        protected bool DecodeName { get { throw null; } set { } }
        protected System.Xml.XmlDocument Document { get { throw null; } }
        protected bool IsReturnValue { get { throw null; } set { } }
        protected System.Xml.XmlReader Reader { get { throw null; } }
        protected int ReaderCount { get { throw null; } }
        protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.CollectionFixup? fixup) { }
        protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.Fixup? fixup) { }
        protected void AddReadCallback(string name, string ns, System.Type type, System.Xml.Serialization.XmlSerializationReadCallback read) { }
        protected void AddTarget(string? id, object? o) { }
        protected void CheckReaderCount(ref int whileIterations, ref int readerCount) { }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected string? CollapseWhitespace(string? value) { throw null; }
        protected System.Exception CreateAbstractTypeException(string name, string? ns) { throw null; }
        protected System.Exception CreateBadDerivationException(string? xsdDerived, string? nsDerived, string? xsdBase, string? nsBase, string? clrDerived, string? clrBase) { throw null; }
        protected System.Exception CreateCtorHasSecurityException(string typeName) { throw null; }
        protected System.Exception CreateInaccessibleConstructorException(string typeName) { throw null; }
        protected System.Exception CreateInvalidCastException(System.Type type, object? value) { throw null; }
        protected System.Exception CreateInvalidCastException(System.Type type, object? value, string? id) { throw null; }
        protected System.Exception CreateMissingIXmlSerializableType(string? name, string? ns, string? clrType) { throw null; }
        protected System.Exception CreateReadOnlyCollectionException(string name) { throw null; }
        protected System.Exception CreateUnknownConstantException(string? value, System.Type enumType) { throw null; }
        protected System.Exception CreateUnknownNodeException() { throw null; }
        protected System.Exception CreateUnknownTypeException(System.Xml.XmlQualifiedName type) { throw null; }
        protected System.Array EnsureArrayIndex(System.Array? a, int index, System.Type elementType) { throw null; }
        protected void FixupArrayRefs(object fixup) { }
        protected int GetArrayLength(string name, string ns) { throw null; }
        protected bool GetNullAttr() { throw null; }
        protected object GetTarget(string id) { throw null; }
        protected System.Xml.XmlQualifiedName? GetXsiType() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected abstract void InitCallbacks();
        protected abstract void InitIDs();
        protected bool IsXmlnsAttribute(string name) { throw null; }
        protected void ParseWsdlArrayType(System.Xml.XmlAttribute attr) { }
        protected System.Xml.XmlQualifiedName ReadElementQualifiedName() { throw null; }
        protected void ReadEndElement() { }
        protected bool ReadNull() { throw null; }
        protected System.Xml.XmlQualifiedName? ReadNullableQualifiedName() { throw null; }
        protected string? ReadNullableString() { throw null; }
        protected bool ReadReference([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] out string? fixupReference) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected object? ReadReferencedElement() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected object? ReadReferencedElement(string? name, string? ns) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void ReadReferencedElements() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected object? ReadReferencingElement(string? name, string? ns, bool elementCanBeType, out string? fixupReference) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected object? ReadReferencingElement(string? name, string? ns, out string? fixupReference) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected object? ReadReferencingElement(out string? fixupReference) { throw null; }
        protected System.Xml.Serialization.IXmlSerializable ReadSerializable(System.Xml.Serialization.IXmlSerializable serializable) { throw null; }
        protected System.Xml.Serialization.IXmlSerializable ReadSerializable(System.Xml.Serialization.IXmlSerializable serializable, bool wrappedAny) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected string? ReadString(string? value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected string? ReadString(string? value, bool trim) { throw null; }
        protected object? ReadTypedNull(System.Xml.XmlQualifiedName type) { throw null; }
        protected object? ReadTypedPrimitive(System.Xml.XmlQualifiedName type) { throw null; }
        protected System.Xml.XmlDocument? ReadXmlDocument(bool wrapped) { throw null; }
        protected System.Xml.XmlNode? ReadXmlNode(bool wrapped) { throw null; }
        protected void Referenced(object? o) { }
        protected static System.Reflection.Assembly? ResolveDynamicAssembly(string assemblyFullName) { throw null; }
        protected System.Array? ShrinkArray(System.Array? a, int length, System.Type elementType, bool isNullable) { throw null; }
        protected byte[]? ToByteArrayBase64(bool isNull) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static byte[]? ToByteArrayBase64(string? value) { throw null; }
        protected byte[]? ToByteArrayHex(bool isNull) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static byte[]? ToByteArrayHex(string? value) { throw null; }
        protected static char ToChar(string value) { throw null; }
        protected static System.DateTime ToDate(string value) { throw null; }
        protected static System.DateTime ToDateTime(string value) { throw null; }
        protected static long ToEnum(string value, System.Collections.Hashtable h, string typeName) { throw null; }
        protected static System.DateTime ToTime(string value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static string? ToXmlName(string? value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static string? ToXmlNCName(string? value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static string? ToXmlNmToken(string? value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static string? ToXmlNmTokens(string? value) { throw null; }
        protected System.Xml.XmlQualifiedName ToXmlQualifiedName(string? value) { throw null; }
        protected void UnknownAttribute(object? o, System.Xml.XmlAttribute attr) { }
        protected void UnknownAttribute(object? o, System.Xml.XmlAttribute attr, string? qnames) { }
        protected void UnknownElement(object? o, System.Xml.XmlElement elem) { }
        protected void UnknownElement(object? o, System.Xml.XmlElement elem, string? qnames) { }
        protected void UnknownNode(object? o) { }
        protected void UnknownNode(object? o, string? qnames) { }
        protected void UnreferencedObject(string? id, object? o) { }
        protected partial class CollectionFixup
        {
            public CollectionFixup(object? collection, System.Xml.Serialization.XmlSerializationCollectionFixupCallback callback, object collectionItems) { }
            public System.Xml.Serialization.XmlSerializationCollectionFixupCallback Callback { get { throw null; } }
            public object? Collection { get { throw null; } }
            public object CollectionItems { get { throw null; } }
        }
        protected partial class Fixup
        {
            public Fixup(object? o, System.Xml.Serialization.XmlSerializationFixupCallback callback, int count) { }
            public Fixup(object? o, System.Xml.Serialization.XmlSerializationFixupCallback callback, string?[]? ids) { }
            public System.Xml.Serialization.XmlSerializationFixupCallback Callback { get { throw null; } }
            public string?[]? Ids { get { throw null; } }
            public object? Source { get { throw null; } set { } }
        }
    }
    public delegate void XmlSerializationWriteCallback(object o);
    public abstract partial class XmlSerializationWriter : System.Xml.Serialization.XmlSerializationGeneratedCode
    {
        protected XmlSerializationWriter() { }
        protected bool EscapeName { get { throw null; } set { } }
        protected System.Collections.ArrayList? Namespaces { get { throw null; } set { } }
        protected System.Xml.XmlWriter Writer { get { throw null; } set { } }
        protected void AddWriteCallback(System.Type type, string typeName, string? typeNs, System.Xml.Serialization.XmlSerializationWriteCallback callback) { }
        protected System.Exception CreateChoiceIdentifierValueException(string value, string identifier, string name, string ns) { throw null; }
        protected System.Exception CreateInvalidAnyTypeException(object o) { throw null; }
        protected System.Exception CreateInvalidAnyTypeException(System.Type type) { throw null; }
        protected System.Exception CreateInvalidChoiceIdentifierValueException(string type, string identifier) { throw null; }
        protected System.Exception CreateInvalidEnumValueException(object value, string typeName) { throw null; }
        protected System.Exception CreateMismatchChoiceException(string value, string elementName, string enumValue) { throw null; }
        protected System.Exception CreateUnknownAnyElementException(string name, string ns) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected System.Exception CreateUnknownTypeException(object o) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected System.Exception CreateUnknownTypeException(System.Type type) { throw null; }
        protected static byte[] FromByteArrayBase64(byte[] value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        protected static string? FromByteArrayHex(byte[]? value) { throw null; }
        protected static string FromChar(char value) { throw null; }
        protected static string FromDate(System.DateTime value) { throw null; }
        protected static string FromDateTime(System.DateTime value) { throw null; }
        protected static string FromEnum(long value, string[] values, long[] ids) { throw null; }
        protected static string FromEnum(long value, string[] values, long[] ids, string typeName) { throw null; }
        protected static string FromTime(System.DateTime value) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("name")]
        protected static string? FromXmlName(string? name) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("ncName")]
        protected static string? FromXmlNCName(string? ncName) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("nmToken")]
        protected static string? FromXmlNmToken(string? nmToken) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("nmTokens")]
        protected static string? FromXmlNmTokens(string? nmTokens) { throw null; }
        protected string? FromXmlQualifiedName(System.Xml.XmlQualifiedName? xmlQualifiedName) { throw null; }
        protected string? FromXmlQualifiedName(System.Xml.XmlQualifiedName? xmlQualifiedName, bool ignoreEmpty) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected abstract void InitCallbacks();
        protected static System.Reflection.Assembly? ResolveDynamicAssembly(string assemblyFullName) { throw null; }
        protected void TopLevelElement() { }
        protected void WriteAttribute(string localName, byte[]? value) { }
        protected void WriteAttribute(string localName, string? value) { }
        protected void WriteAttribute(string localName, string ns, byte[]? value) { }
        protected void WriteAttribute(string localName, string? ns, string? value) { }
        protected void WriteAttribute(string? prefix, string localName, string? ns, string? value) { }
        protected void WriteElementEncoded(System.Xml.XmlNode? node, string name, string? ns, bool isNullable, bool any) { }
        protected void WriteElementLiteral(System.Xml.XmlNode? node, string name, string? ns, bool isNullable, bool any) { }
        protected void WriteElementQualifiedName(string localName, string? ns, System.Xml.XmlQualifiedName? value) { }
        protected void WriteElementQualifiedName(string localName, string? ns, System.Xml.XmlQualifiedName? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName? value) { }
        protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementString(string localName, string? value) { }
        protected void WriteElementString(string localName, string? ns, string? value) { }
        protected void WriteElementString(string localName, string? ns, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementString(string localName, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementStringRaw(string localName, byte[]? value) { }
        protected void WriteElementStringRaw(string localName, byte[]? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementStringRaw(string localName, string? value) { }
        protected void WriteElementStringRaw(string localName, string? ns, byte[]? value) { }
        protected void WriteElementStringRaw(string localName, string? ns, byte[]? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementStringRaw(string localName, string? ns, string? value) { }
        protected void WriteElementStringRaw(string localName, string? ns, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteElementStringRaw(string localName, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteEmptyTag(string? name) { }
        protected void WriteEmptyTag(string? name, string? ns) { }
        protected void WriteEndElement() { }
        protected void WriteEndElement(object? o) { }
        protected void WriteId(object o) { }
        protected void WriteNamespaceDeclarations(System.Xml.Serialization.XmlSerializerNamespaces? xmlns) { }
        protected void WriteNullableQualifiedNameEncoded(string name, string? ns, System.Xml.XmlQualifiedName? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteNullableQualifiedNameLiteral(string name, string? ns, System.Xml.XmlQualifiedName? value) { }
        protected void WriteNullableStringEncoded(string name, string? ns, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteNullableStringEncodedRaw(string name, string? ns, byte[]? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteNullableStringEncodedRaw(string name, string? ns, string? value, System.Xml.XmlQualifiedName? xsiType) { }
        protected void WriteNullableStringLiteral(string name, string? ns, string? value) { }
        protected void WriteNullableStringLiteralRaw(string name, string? ns, byte[]? value) { }
        protected void WriteNullableStringLiteralRaw(string name, string? ns, string? value) { }
        protected void WriteNullTagEncoded(string? name) { }
        protected void WriteNullTagEncoded(string? name, string? ns) { }
        protected void WriteNullTagLiteral(string? name) { }
        protected void WriteNullTagLiteral(string? name, string? ns) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o, System.Type? ambientType) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WritePotentiallyReferencingElement(string n, string? ns, object? o, System.Type? ambientType, bool suppressReference) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WritePotentiallyReferencingElement(string? n, string? ns, object? o, System.Type? ambientType, bool suppressReference, bool isNullable) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WriteReferencedElements() { }
        protected void WriteReferencingElement(string n, string? ns, object? o) { }
        protected void WriteReferencingElement(string n, string? ns, object? o, bool isNullable) { }
        protected void WriteRpcResult(string name, string? ns) { }
        protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable? serializable, string name, string ns, bool isNullable) { }
        protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable? serializable, string name, string? ns, bool isNullable, bool wrapped) { }
        protected void WriteStartDocument() { }
        protected void WriteStartElement(string name) { }
        protected void WriteStartElement(string name, string? ns) { }
        protected void WriteStartElement(string name, string? ns, bool writePrefixed) { }
        protected void WriteStartElement(string name, string? ns, object? o) { }
        protected void WriteStartElement(string name, string? ns, object? o, bool writePrefixed) { }
        protected void WriteStartElement(string name, string? ns, object? o, bool writePrefixed, System.Xml.Serialization.XmlSerializerNamespaces? xmlns) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        protected void WriteTypedPrimitive(string? name, string? ns, object o, bool xsiType) { }
        protected void WriteValue(byte[]? value) { }
        protected void WriteValue(string? value) { }
        protected void WriteXmlAttribute(System.Xml.XmlNode node) { }
        protected void WriteXmlAttribute(System.Xml.XmlNode node, object? container) { }
        protected void WriteXsiType(string name, string? ns) { }
    }
    public partial class XmlSerializer
    {
        protected XmlSerializer() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, string? defaultNamespace) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, System.Type[]? extraTypes) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides, System.Type[]? extraTypes, System.Xml.Serialization.XmlRootAttribute? root, string? defaultNamespace) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides, System.Type[]? extraTypes, System.Xml.Serialization.XmlRootAttribute? root, string? defaultNamespace, string? location) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute? root) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public XmlSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
        public event System.Xml.Serialization.XmlAttributeEventHandler UnknownAttribute { add { } remove { } }
        public event System.Xml.Serialization.XmlElementEventHandler UnknownElement { add { } remove { } }
        public event System.Xml.Serialization.XmlNodeEventHandler UnknownNode { add { } remove { } }
        public event System.Xml.Serialization.UnreferencedObjectEventHandler UnreferencedObject { add { } remove { } }
        public virtual bool CanDeserialize(System.Xml.XmlReader xmlReader) { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializationReader CreateReader() { throw null; }
        protected virtual System.Xml.Serialization.XmlSerializationWriter CreateWriter() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.IO.Stream stream) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.IO.TextReader textReader) { throw null; }
        protected virtual object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.Xml.XmlReader xmlReader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.Xml.XmlReader xmlReader, string? encodingStyle) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.Xml.XmlReader xmlReader, string? encodingStyle, System.Xml.Serialization.XmlDeserializationEvents events) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from deserialized types may be trimmed if not referenced directly")]
        public object? Deserialize(System.Xml.XmlReader xmlReader, System.Xml.Serialization.XmlDeserializationEvents events) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[]? mappings) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[]? mappings, System.Type? type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public static System.Xml.Serialization.XmlSerializer[] FromTypes(System.Type[]? types) { throw null; }
        public static string GetXmlSerializerAssemblyName(System.Type type) { throw null; }
        public static string GetXmlSerializerAssemblyName(System.Type type, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.IO.Stream stream, object? o) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.IO.Stream stream, object? o, System.Xml.Serialization.XmlSerializerNamespaces? namespaces) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.IO.TextWriter textWriter, object? o) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.IO.TextWriter textWriter, object? o, System.Xml.Serialization.XmlSerializerNamespaces? namespaces) { }
        protected virtual void Serialize(object? o, System.Xml.Serialization.XmlSerializationWriter writer) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.Xml.XmlWriter xmlWriter, object? o) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.Xml.XmlWriter xmlWriter, object? o, System.Xml.Serialization.XmlSerializerNamespaces? namespaces) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.Xml.XmlWriter xmlWriter, object? o, System.Xml.Serialization.XmlSerializerNamespaces? namespaces, string? encodingStyle) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public void Serialize(System.Xml.XmlWriter xmlWriter, object? o, System.Xml.Serialization.XmlSerializerNamespaces? namespaces, string? encodingStyle, string? id) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct, AllowMultiple=false)]
    public sealed partial class XmlSerializerAssemblyAttribute : System.Attribute
    {
        public XmlSerializerAssemblyAttribute() { }
        public XmlSerializerAssemblyAttribute(string? assemblyName) { }
        public XmlSerializerAssemblyAttribute(string? assemblyName, string? codeBase) { }
        public string? AssemblyName { get { throw null; } set { } }
        public string? CodeBase { get { throw null; } set { } }
    }
    public partial class XmlSerializerFactory
    {
        public XmlSerializerFactory() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Type[]? extraTypes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides, System.Type[]? extraTypes, System.Xml.Serialization.XmlRootAttribute? root, string? defaultNamespace) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides? overrides, System.Type[]? extraTypes, System.Xml.Serialization.XmlRootAttribute? root, string? defaultNamespace, string? location) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute? root) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members from serialized types may be trimmed if not referenced directly")]
        public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { throw null; }
    }
    public abstract partial class XmlSerializerImplementation
    {
        protected XmlSerializerImplementation() { }
        public virtual System.Xml.Serialization.XmlSerializationReader Reader { get { throw null; } }
        public virtual System.Collections.Hashtable ReadMethods { get { throw null; } }
        public virtual System.Collections.Hashtable TypedSerializers { get { throw null; } }
        public virtual System.Collections.Hashtable WriteMethods { get { throw null; } }
        public virtual System.Xml.Serialization.XmlSerializationWriter Writer { get { throw null; } }
        public virtual bool CanSerialize(System.Type type) { throw null; }
        public virtual System.Xml.Serialization.XmlSerializer GetSerializer(System.Type type) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Assembly)]
    public sealed partial class XmlSerializerVersionAttribute : System.Attribute
    {
        public XmlSerializerVersionAttribute() { }
        public XmlSerializerVersionAttribute(System.Type? type) { }
        public string? Namespace { get { throw null; } set { } }
        public string? ParentAssemblyId { get { throw null; } set { } }
        public System.Type? Type { get { throw null; } set { } }
        public string? Version { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Enum | System.AttributeTargets.Interface | System.AttributeTargets.Struct)]
    public partial class XmlTypeAttribute : System.Attribute
    {
        public XmlTypeAttribute() { }
        public XmlTypeAttribute(string? typeName) { }
        public bool AnonymousType { get { throw null; } set { } }
        public bool IncludeInSchema { get { throw null; } set { } }
        public string? Namespace { get { throw null; } set { } }
        [System.Diagnostics.CodeAnalysis.AllowNullAttribute]
        public string TypeName { get { throw null; } set { } }
    }
    public partial class XmlTypeMapping : System.Xml.Serialization.XmlMapping
    {
        internal XmlTypeMapping() { }
        public string TypeFullName { get { throw null; } }
        public string TypeName { get { throw null; } }
        public string? XsdTypeName { get { throw null; } }
        public string? XsdTypeNamespace { get { throw null; } }
    }
}
