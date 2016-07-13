// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Serialization {
  public abstract partial class CodeExporter {
    internal CodeExporter() { }
  }
  [System.FlagsAttribute]
  public enum CodeGenerationOptions {
    [System.Xml.Serialization.XmlEnumAttribute("enableDataBinding")]
    EnableDataBinding = 16,
    [System.Xml.Serialization.XmlEnumAttribute("newAsync")]
    GenerateNewAsync = 2,
    [System.Xml.Serialization.XmlEnumAttribute("oldAsync")]
    GenerateOldAsync = 4,
    [System.Xml.Serialization.XmlEnumAttribute("order")]
    GenerateOrder = 8,
    [System.Xml.Serialization.XmlEnumAttribute("properties")]
    GenerateProperties = 1,
    [System.Xml.Serialization.XmlIgnoreAttribute]
    None = 0,
  }
  public partial class CodeIdentifier {
    [System.ObsoleteAttribute("This class should never get constructed as it contains only static methods.")]
    public CodeIdentifier() { }
    public static string MakeCamel(string identifier) { return default(string); }
    public static string MakePascal(string identifier) { return default(string); }
    public static string MakeValid(string identifier) { return default(string); }
  }
  public partial class CodeIdentifiers {
    public CodeIdentifiers() { }
    public CodeIdentifiers(bool caseSensitive) { }
    public bool UseCamelCasing { get { return default(bool); } set { } }
    public void Add(string identifier, object value) { }
    public void AddReserved(string identifier) { }
    public string AddUnique(string identifier, object value) { return default(string); }
    public void Clear() { }
    public bool IsInUse(string identifier) { return default(bool); }
    public string MakeRightCase(string identifier) { return default(string); }
    public string MakeUnique(string identifier) { return default(string); }
    public void Remove(string identifier) { }
    public void RemoveReserved(string identifier) { }
    public object ToArray(System.Type type) { return default(object); }
  }
  public partial class ImportContext {
    public ImportContext(System.Xml.Serialization.CodeIdentifiers identifiers, bool shareTypes) { }
    public bool ShareTypes { get { return default(bool); } }
    public System.Xml.Serialization.CodeIdentifiers TypeIdentifiers { get { return default(System.Xml.Serialization.CodeIdentifiers); } }
    public System.Collections.Specialized.StringCollection Warnings { get { return default(System.Collections.Specialized.StringCollection); } }
  }
  public partial interface IXmlSerializable {
    System.Xml.Schema.XmlSchema GetSchema();
    void ReadXml(System.Xml.XmlReader reader);
    void WriteXml(System.Xml.XmlWriter writer);
  }
  public partial interface IXmlTextParser {
    bool Normalized { get; set; }
    System.Xml.WhitespaceHandling WhitespaceHandling { get; set; }
  }
  public abstract partial class SchemaImporter {
    internal SchemaImporter() { }
    public System.Xml.Serialization.Advanced.SchemaImporterExtensionCollection Extensions { get { return default(System.Xml.Serialization.Advanced.SchemaImporterExtensionCollection); } }
  }
  public partial class SoapAttributeAttribute : System.Attribute {
    public SoapAttributeAttribute() { }
    public SoapAttributeAttribute(string attributeName) { }
    public string AttributeName { get { return default(string); } set { } }
    public string DataType { get { return default(string); } set { } }
    public string Namespace { get { return default(string); } set { } }
  }
  public partial class SoapAttributeOverrides {
    public SoapAttributeOverrides() { }
    public System.Xml.Serialization.SoapAttributes this[System.Type type] { get { return default(System.Xml.Serialization.SoapAttributes); } }
    public System.Xml.Serialization.SoapAttributes this[System.Type type, string member] { get { return default(System.Xml.Serialization.SoapAttributes); } }
    public void Add(System.Type type, string member, System.Xml.Serialization.SoapAttributes attributes) { }
    public void Add(System.Type type, System.Xml.Serialization.SoapAttributes attributes) { }
  }
  public partial class SoapAttributes {
    public SoapAttributes() { }
    public System.Xml.Serialization.SoapAttributeAttribute SoapAttribute { get { return default(System.Xml.Serialization.SoapAttributeAttribute); } set { } }
    public object SoapDefaultValue { get { return default(object); } set { } }
    public System.Xml.Serialization.SoapElementAttribute SoapElement { get { return default(System.Xml.Serialization.SoapElementAttribute); } set { } }
    public System.Xml.Serialization.SoapEnumAttribute SoapEnum { get { return default(System.Xml.Serialization.SoapEnumAttribute); } set { } }
    public bool SoapIgnore { get { return default(bool); } set { } }
    public System.Xml.Serialization.SoapTypeAttribute SoapType { get { return default(System.Xml.Serialization.SoapTypeAttribute); } set { } }
  }
  public partial class SoapCodeExporter : System.Xml.Serialization.CodeExporter {
    internal SoapCodeExporter() { }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
    public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
  }
  public partial class SoapElementAttribute : System.Attribute {
    public SoapElementAttribute() { }
    public SoapElementAttribute(string elementName) { }
    public string DataType { get { return default(string); } set { } }
    public string ElementName { get { return default(string); } set { } }
    public bool IsNullable { get { return default(bool); } set { } }
  }
  public partial class SoapEnumAttribute : System.Attribute {
    public SoapEnumAttribute() { }
    public SoapEnumAttribute(string name) { }
    public string Name { get { return default(string); } set { } }
  }
  public partial class SoapIgnoreAttribute : System.Attribute {
    public SoapIgnoreAttribute() { }
  }
  public partial class SoapIncludeAttribute : System.Attribute {
    public SoapIncludeAttribute(System.Type type) { }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public partial class SoapReflectionImporter {
    public SoapReflectionImporter() { }
    public SoapReflectionImporter(string defaultNamespace) { }
    public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides attributeOverrides) { }
    public SoapReflectionImporter(System.Xml.Serialization.SoapAttributeOverrides attributeOverrides, string defaultNamespace) { }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool writeAccessors, bool validate, System.Xml.Serialization.XmlMappingAccess access) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public void IncludeType(System.Type type) { }
  }
  public partial class SoapSchemaExporter {
    public SoapSchemaExporter(System.Xml.Serialization.XmlSchemas schemas) { }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping, bool exportEnclosingType) { }
    public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
  }
  public partial class SoapSchemaImporter : System.Xml.Serialization.SchemaImporter {
    public SoapSchemaImporter(System.Xml.Serialization.XmlSchemas schemas) { }
    public SoapSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeGenerationOptions options, System.Xml.Serialization.ImportContext context) { }
    public SoapSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers typeIdentifiers) { }
    public SoapSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers typeIdentifiers, System.Xml.Serialization.CodeGenerationOptions options) { }
    public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember member) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember[] members) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember[] members, bool hasWrapperElement) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember[] members, bool hasWrapperElement, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlMembersMapping); }
  }
  public partial class SoapSchemaMember {
    public SoapSchemaMember() { }
    public string MemberName { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName MemberType { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class SoapTypeAttribute : System.Attribute {
    public SoapTypeAttribute() { }
    public SoapTypeAttribute(string typeName) { }
    public SoapTypeAttribute(string typeName, string ns) { }
    public bool IncludeInSchema { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public string TypeName { get { return default(string); } set { } }
  }
  public partial class UnreferencedObjectEventArgs : System.EventArgs {
    public UnreferencedObjectEventArgs(object o, string id) { }
    public string UnreferencedId { get { return default(string); } }
    public object UnreferencedObject { get { return default(object); } }
  }
  public delegate void UnreferencedObjectEventHandler(object sender, System.Xml.Serialization.UnreferencedObjectEventArgs e);
  public partial class XmlAnyAttributeAttribute : System.Attribute {
    public XmlAnyAttributeAttribute() { }
  }
  public partial class XmlAnyElementAttribute : System.Attribute {
    public XmlAnyElementAttribute() { }
    public XmlAnyElementAttribute(string name) { }
    public XmlAnyElementAttribute(string name, string ns) { }
    public string Name { get { return default(string); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public int Order { get { return default(int); } set { } }
  }
  public partial class XmlAnyElementAttributes : System.Collections.CollectionBase {
    public XmlAnyElementAttributes() { }
    public System.Xml.Serialization.XmlAnyElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlAnyElementAttribute); } set { } }
    public int Add(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
    public bool Contains(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(bool); }
    public void CopyTo(System.Xml.Serialization.XmlAnyElementAttribute[] array, int index) { }
    public int IndexOf(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
    public void Insert(int index, System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
    public void Remove(System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
  }
  public partial class XmlArrayAttribute : System.Attribute {
    public XmlArrayAttribute() { }
    public XmlArrayAttribute(string elementName) { }
    public string ElementName { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public bool IsNullable { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public int Order { get { return default(int); } set { } }
  }
  public partial class XmlArrayItemAttribute : System.Attribute {
    public XmlArrayItemAttribute() { }
    public XmlArrayItemAttribute(string elementName) { }
    public XmlArrayItemAttribute(string elementName, System.Type type) { }
    public XmlArrayItemAttribute(System.Type type) { }
    public string DataType { get { return default(string); } set { } }
    public string ElementName { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public bool IsNullable { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public int NestingLevel { get { return default(int); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public partial class XmlArrayItemAttributes : System.Collections.CollectionBase {
    public XmlArrayItemAttributes() { }
    public System.Xml.Serialization.XmlArrayItemAttribute this[int index] { get { return default(System.Xml.Serialization.XmlArrayItemAttribute); } set { } }
    public int Add(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
    public bool Contains(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(bool); }
    public void CopyTo(System.Xml.Serialization.XmlArrayItemAttribute[] array, int index) { }
    public int IndexOf(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
    public void Insert(int index, System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
    public void Remove(System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
  }
  public partial class XmlAttributeAttribute : System.Attribute {
    public XmlAttributeAttribute() { }
    public XmlAttributeAttribute(string attributeName) { }
    public XmlAttributeAttribute(string attributeName, System.Type type) { }
    public XmlAttributeAttribute(System.Type type) { }
    public string AttributeName { get { return default(string); } set { } }
    public string DataType { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public partial class XmlAttributeEventArgs : System.EventArgs {
    internal XmlAttributeEventArgs() { }
    public System.Xml.XmlAttribute Attr { get { return default(System.Xml.XmlAttribute); } }
    public string ExpectedAttributes { get { return default(string); } }
    public int LineNumber { get { return default(int); } }
    public int LinePosition { get { return default(int); } }
    public object ObjectBeingDeserialized { get { return default(object); } }
  }
  public delegate void XmlAttributeEventHandler(object sender, System.Xml.Serialization.XmlAttributeEventArgs e);
  public partial class XmlAttributeOverrides {
    public XmlAttributeOverrides() { }
    public System.Xml.Serialization.XmlAttributes this[System.Type type] { get { return default(System.Xml.Serialization.XmlAttributes); } }
    public System.Xml.Serialization.XmlAttributes this[System.Type type, string member] { get { return default(System.Xml.Serialization.XmlAttributes); } }
    public void Add(System.Type type, string member, System.Xml.Serialization.XmlAttributes attributes) { }
    public void Add(System.Type type, System.Xml.Serialization.XmlAttributes attributes) { }
  }
  public partial class XmlAttributes {
    public XmlAttributes() { }
    public System.Xml.Serialization.XmlAnyAttributeAttribute XmlAnyAttribute { get { return default(System.Xml.Serialization.XmlAnyAttributeAttribute); } set { } }
    public System.Xml.Serialization.XmlAnyElementAttributes XmlAnyElements { get { return default(System.Xml.Serialization.XmlAnyElementAttributes); } }
    public System.Xml.Serialization.XmlArrayAttribute XmlArray { get { return default(System.Xml.Serialization.XmlArrayAttribute); } set { } }
    public System.Xml.Serialization.XmlArrayItemAttributes XmlArrayItems { get { return default(System.Xml.Serialization.XmlArrayItemAttributes); } }
    public System.Xml.Serialization.XmlAttributeAttribute XmlAttribute { get { return default(System.Xml.Serialization.XmlAttributeAttribute); } set { } }
    public System.Xml.Serialization.XmlChoiceIdentifierAttribute XmlChoiceIdentifier { get { return default(System.Xml.Serialization.XmlChoiceIdentifierAttribute); } }
    public object XmlDefaultValue { get { return default(object); } set { } }
    public System.Xml.Serialization.XmlElementAttributes XmlElements { get { return default(System.Xml.Serialization.XmlElementAttributes); } }
    public System.Xml.Serialization.XmlEnumAttribute XmlEnum { get { return default(System.Xml.Serialization.XmlEnumAttribute); } set { } }
    public bool XmlIgnore { get { return default(bool); } set { } }
    public bool Xmlns { get { return default(bool); } set { } }
    public System.Xml.Serialization.XmlRootAttribute XmlRoot { get { return default(System.Xml.Serialization.XmlRootAttribute); } set { } }
    public System.Xml.Serialization.XmlTextAttribute XmlText { get { return default(System.Xml.Serialization.XmlTextAttribute); } set { } }
    public System.Xml.Serialization.XmlTypeAttribute XmlType { get { return default(System.Xml.Serialization.XmlTypeAttribute); } set { } }
  }
  public partial class XmlChoiceIdentifierAttribute : System.Attribute {
    public XmlChoiceIdentifierAttribute() { }
    public XmlChoiceIdentifierAttribute(string name) { }
    public string MemberName { get { return default(string); } set { } }
  }
  public partial class XmlCodeExporter : System.Xml.Serialization.CodeExporter {
    internal XmlCodeExporter() { }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
    public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
  }
  [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
  public partial struct XmlDeserializationEvents {
    public System.Xml.Serialization.XmlAttributeEventHandler OnUnknownAttribute { get { return default(System.Xml.Serialization.XmlAttributeEventHandler); } set { } }
    public System.Xml.Serialization.XmlElementEventHandler OnUnknownElement { get { return default(System.Xml.Serialization.XmlElementEventHandler); } set { } }
    public System.Xml.Serialization.XmlNodeEventHandler OnUnknownNode { get { return default(System.Xml.Serialization.XmlNodeEventHandler); } set { } }
    public System.Xml.Serialization.UnreferencedObjectEventHandler OnUnreferencedObject { get { return default(System.Xml.Serialization.UnreferencedObjectEventHandler); } set { } }
  }
  public partial class XmlElementAttribute : System.Attribute {
    public XmlElementAttribute() { }
    public XmlElementAttribute(string elementName) { }
    public XmlElementAttribute(string elementName, System.Type type) { }
    public XmlElementAttribute(System.Type type) { }
    public string DataType { get { return default(string); } set { } }
    public string ElementName { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public bool IsNullable { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public int Order { get { return default(int); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public partial class XmlElementAttributes : System.Collections.CollectionBase {
    public XmlElementAttributes() { }
    public System.Xml.Serialization.XmlElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlElementAttribute); } set { } }
    public int Add(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
    public bool Contains(System.Xml.Serialization.XmlElementAttribute attribute) { return default(bool); }
    public void CopyTo(System.Xml.Serialization.XmlElementAttribute[] array, int index) { }
    public int IndexOf(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
    public void Insert(int index, System.Xml.Serialization.XmlElementAttribute attribute) { }
    public void Remove(System.Xml.Serialization.XmlElementAttribute attribute) { }
  }
  public partial class XmlElementEventArgs : System.EventArgs {
    internal XmlElementEventArgs() { }
    public System.Xml.XmlElement Element { get { return default(System.Xml.XmlElement); } }
    public string ExpectedElements { get { return default(string); } }
    public int LineNumber { get { return default(int); } }
    public int LinePosition { get { return default(int); } }
    public object ObjectBeingDeserialized { get { return default(object); } }
  }
  public delegate void XmlElementEventHandler(object sender, System.Xml.Serialization.XmlElementEventArgs e);
  public partial class XmlEnumAttribute : System.Attribute {
    public XmlEnumAttribute() { }
    public XmlEnumAttribute(string name) { }
    public string Name { get { return default(string); } set { } }
  }
  public partial class XmlIgnoreAttribute : System.Attribute {
    public XmlIgnoreAttribute() { }
  }
  public partial class XmlIncludeAttribute : System.Attribute {
    public XmlIncludeAttribute(System.Type type) { }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public abstract partial class XmlMapping {
    internal XmlMapping() { }
    public string ElementName { get { return default(string); } }
    public string Namespace { get { return default(string); } }
    public string XsdElementName { get { return default(string); } }
    public void SetKey(string key) { }
  }
  [System.FlagsAttribute]
  public enum XmlMappingAccess {
    None = 0,
    Read = 1,
    Write = 2,
  }
  public partial class XmlMemberMapping {
    internal XmlMemberMapping() { }
    public bool Any { get { return default(bool); } }
    public bool CheckSpecified { get { return default(bool); } }
    public string ElementName { get { return default(string); } }
    public string MemberName { get { return default(string); } }
    public string Namespace { get { return default(string); } }
    public string TypeFullName { get { return default(string); } }
    public string TypeName { get { return default(string); } }
    public string TypeNamespace { get { return default(string); } }
    public string XsdElementName { get { return default(string); } }
  }
  public partial class XmlMembersMapping : System.Xml.Serialization.XmlMapping {
    internal XmlMembersMapping() { }
    public int Count { get { return default(int); } }
    public System.Xml.Serialization.XmlMemberMapping this[int index] { get { return default(System.Xml.Serialization.XmlMemberMapping); } }
    public string TypeName { get { return default(string); } }
    public string TypeNamespace { get { return default(string); } }
  }
  public partial class XmlNamespaceDeclarationsAttribute : System.Attribute {
    public XmlNamespaceDeclarationsAttribute() { }
  }
  public partial class XmlNodeEventArgs : System.EventArgs {
    internal XmlNodeEventArgs() { }
    public int LineNumber { get { return default(int); } }
    public int LinePosition { get { return default(int); } }
    public string LocalName { get { return default(string); } }
    public string Name { get { return default(string); } }
    public string NamespaceURI { get { return default(string); } }
    public System.Xml.XmlNodeType NodeType { get { return default(System.Xml.XmlNodeType); } }
    public object ObjectBeingDeserialized { get { return default(object); } }
    public string Text { get { return default(string); } }
  }
  public delegate void XmlNodeEventHandler(object sender, System.Xml.Serialization.XmlNodeEventArgs e);
  public partial class XmlReflectionImporter {
    public XmlReflectionImporter() { }
    public XmlReflectionImporter(string defaultNamespace) { }
    public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides attributeOverrides) { }
    public XmlReflectionImporter(System.Xml.Serialization.XmlAttributeOverrides attributeOverrides, string defaultNamespace) { }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string elementName, string ns, System.Xml.Serialization.XmlReflectionMember[] members, bool hasWrapperElement, bool rpc, bool openModel, System.Xml.Serialization.XmlMappingAccess access) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Type type, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public void IncludeType(System.Type type) { }
  }
  public partial class XmlReflectionMember {
    public XmlReflectionMember() { }
    public bool IsReturnValue { get { return default(bool); } set { } }
    public string MemberName { get { return default(string); } set { } }
    public System.Type MemberType { get { return default(System.Type); } set { } }
    public bool OverrideIsNullable { get { return default(bool); } set { } }
    public System.Xml.Serialization.SoapAttributes SoapAttributes { get { return default(System.Xml.Serialization.SoapAttributes); } set { } }
    public System.Xml.Serialization.XmlAttributes XmlAttributes { get { return default(System.Xml.Serialization.XmlAttributes); } set { } }
  }
  public partial class XmlRootAttribute : System.Attribute {
    public XmlRootAttribute() { }
    public XmlRootAttribute(string elementName) { }
    public string DataType { get { return default(string); } set { } }
    public string ElementName { get { return default(string); } set { } }
    public bool IsNullable { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
  }
  public partial class XmlSchemaEnumerator : System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerator, System.IDisposable {
    public XmlSchemaEnumerator(System.Xml.Serialization.XmlSchemas list) { }
    public System.Xml.Schema.XmlSchema Current { get { return default(System.Xml.Schema.XmlSchema); } }
    object System.Collections.IEnumerator.Current { get { return default(object); } }
    public void Dispose() { }
    public bool MoveNext() { return default(bool); }
    void System.Collections.IEnumerator.Reset() { }
  }
  public partial class XmlSchemaExporter {
    public XmlSchemaExporter(System.Xml.Serialization.XmlSchemas schemas) { }
    public string ExportAnyType(string ns) { return default(string); }
    public string ExportAnyType(System.Xml.Serialization.XmlMembersMapping members) { return default(string); }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { }
    public void ExportMembersMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping, bool exportEnclosingType) { }
    public System.Xml.XmlQualifiedName ExportTypeMapping(System.Xml.Serialization.XmlMembersMapping xmlMembersMapping) { return default(System.Xml.XmlQualifiedName); }
    public void ExportTypeMapping(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
  }
  public partial class XmlSchemaImporter : System.Xml.Serialization.SchemaImporter {
    public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas) { }
    public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeGenerationOptions options, System.Xml.Serialization.ImportContext context) { }
    public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers typeIdentifiers) { }
    public XmlSchemaImporter(System.Xml.Serialization.XmlSchemas schemas, System.Xml.Serialization.CodeIdentifiers typeIdentifiers, System.Xml.Serialization.CodeGenerationOptions options) { }
    public System.Xml.Serialization.XmlMembersMapping ImportAnyType(System.Xml.XmlQualifiedName typeName, string elementName) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type baseType) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportDerivedTypeMapping(System.Xml.XmlQualifiedName name, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(string name, string ns, System.Xml.Serialization.SoapSchemaMember[] members) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName name) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlMembersMapping ImportMembersMapping(System.Xml.XmlQualifiedName[] names, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlMembersMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type baseType) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportSchemaType(System.Xml.XmlQualifiedName typeName, System.Type baseType, bool baseTypeCanBeIndirect) { return default(System.Xml.Serialization.XmlTypeMapping); }
    public System.Xml.Serialization.XmlTypeMapping ImportTypeMapping(System.Xml.XmlQualifiedName name) { return default(System.Xml.Serialization.XmlTypeMapping); }
  }
  public sealed partial class XmlSchemaProviderAttribute : System.Attribute {
    public XmlSchemaProviderAttribute(string methodName) { }
    public bool IsAny { get { return default(bool); } set { } }
    public string MethodName { get { return default(string); } }
  }
  public partial class XmlSchemas : System.Collections.CollectionBase, System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>, System.Collections.IEnumerable {
    public XmlSchemas() { }
    public bool IsCompiled { get { return default(bool); } }
    public System.Xml.Schema.XmlSchema this[int index] { get { return default(System.Xml.Schema.XmlSchema); } set { } }
    public System.Xml.Schema.XmlSchema this[string ns] { get { return default(System.Xml.Schema.XmlSchema); } }
    public int Add(System.Xml.Schema.XmlSchema schema) { return default(int); }
    public int Add(System.Xml.Schema.XmlSchema schema, System.Uri baseUri) { return default(int); }
    public void Add(System.Xml.Serialization.XmlSchemas schemas) { }
    public void AddReference(System.Xml.Schema.XmlSchema schema) { }
    public void Compile(System.Xml.Schema.ValidationEventHandler handler, bool fullCompile) { }
    public bool Contains(string targetNamespace) { return default(bool); }
    public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
    public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
    public object Find(System.Xml.XmlQualifiedName name, System.Type type) { return default(object); }
    public System.Collections.IList GetSchemas(string ns) { return default(System.Collections.IList); }
    public int IndexOf(System.Xml.Schema.XmlSchema schema) { return default(int); }
    public void Insert(int index, System.Xml.Schema.XmlSchema schema) { }
    public static bool IsDataSet(System.Xml.Schema.XmlSchema schema) { return default(bool); }
    protected override void OnClear() { }
    protected override void OnInsert(int index, object value) { }
    protected override void OnRemove(int index, object value) { }
    protected override void OnSet(int index, object oldValue, object newValue) { }
    public void Remove(System.Xml.Schema.XmlSchema schema) { }
    System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema> System.Collections.Generic.IEnumerable<System.Xml.Schema.XmlSchema>.GetEnumerator() { return default(System.Collections.Generic.IEnumerator<System.Xml.Schema.XmlSchema>); }
  }
  public delegate void XmlSerializationCollectionFixupCallback(object collection, object collectionItems);
  public delegate void XmlSerializationFixupCallback(object fixup);
  public abstract partial class XmlSerializationGeneratedCode {
    protected XmlSerializationGeneratedCode() { }
  }
  public delegate object XmlSerializationReadCallback();
  public abstract partial class XmlSerializationReader : System.Xml.Serialization.XmlSerializationGeneratedCode {
    protected XmlSerializationReader() { }
    protected bool DecodeName { get { return default(bool); } set { } }
    protected System.Xml.XmlDocument Document { get { return default(System.Xml.XmlDocument); } }
    protected bool IsReturnValue { get { return default(bool); } set { } }
    protected System.Xml.XmlReader Reader { get { return default(System.Xml.XmlReader); } }
    protected int ReaderCount { get { return default(int); } }
    protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.CollectionFixup fixup) { }
    protected void AddFixup(System.Xml.Serialization.XmlSerializationReader.Fixup fixup) { }
    protected void AddReadCallback(string name, string ns, System.Type type, System.Xml.Serialization.XmlSerializationReadCallback read) { }
    protected void AddTarget(string id, object o) { }
    protected void CheckReaderCount(ref int whileIterations, ref int readerCount) { }
    protected string CollapseWhitespace(string value) { return default(string); }
    protected System.Exception CreateAbstractTypeException(string name, string ns) { return default(System.Exception); }
    protected System.Exception CreateBadDerivationException(string xsdDerived, string nsDerived, string xsdBase, string nsBase, string clrDerived, string clrBase) { return default(System.Exception); }
    protected System.Exception CreateCtorHasSecurityException(string typeName) { return default(System.Exception); }
    protected System.Exception CreateInaccessibleConstructorException(string typeName) { return default(System.Exception); }
    protected System.Exception CreateInvalidCastException(System.Type type, object value) { return default(System.Exception); }
    protected System.Exception CreateInvalidCastException(System.Type type, object value, string id) { return default(System.Exception); }
    protected System.Exception CreateMissingIXmlSerializableType(string name, string ns, string clrType) { return default(System.Exception); }
    protected System.Exception CreateReadOnlyCollectionException(string name) { return default(System.Exception); }
    protected System.Exception CreateUnknownConstantException(string value, System.Type enumType) { return default(System.Exception); }
    protected System.Exception CreateUnknownNodeException() { return default(System.Exception); }
    protected System.Exception CreateUnknownTypeException(System.Xml.XmlQualifiedName type) { return default(System.Exception); }
    protected System.Array EnsureArrayIndex(System.Array a, int index, System.Type elementType) { return default(System.Array); }
    protected void FixupArrayRefs(object fixup) { }
    protected int GetArrayLength(string name, string ns) { return default(int); }
    protected bool GetNullAttr() { return default(bool); }
    protected object GetTarget(string id) { return default(object); }
    protected System.Xml.XmlQualifiedName GetXsiType() { return default(System.Xml.XmlQualifiedName); }
    protected abstract void InitCallbacks();
    protected abstract void InitIDs();
    protected bool IsXmlnsAttribute(string name) { return default(bool); }
    protected void ParseWsdlArrayType(System.Xml.XmlAttribute attr) { }
    protected System.Xml.XmlQualifiedName ReadElementQualifiedName() { return default(System.Xml.XmlQualifiedName); }
    protected void ReadEndElement() { }
    protected bool ReadNull() { return default(bool); }
    protected System.Xml.XmlQualifiedName ReadNullableQualifiedName() { return default(System.Xml.XmlQualifiedName); }
    protected string ReadNullableString() { return default(string); }
    protected bool ReadReference(out string fixupReference) { fixupReference = default(string); return default(bool); }
    protected object ReadReferencedElement() { return default(object); }
    protected object ReadReferencedElement(string name, string ns) { return default(object); }
    protected void ReadReferencedElements() { }
    protected object ReadReferencingElement(string name, string ns, bool elementCanBeType, out string fixupReference) { fixupReference = default(string); return default(object); }
    protected object ReadReferencingElement(string name, string ns, out string fixupReference) { fixupReference = default(string); return default(object); }
    protected object ReadReferencingElement(out string fixupReference) { fixupReference = default(string); return default(object); }
    protected System.Xml.Serialization.IXmlSerializable ReadSerializable(System.Xml.Serialization.IXmlSerializable serializable) { return default(System.Xml.Serialization.IXmlSerializable); }
    protected System.Xml.Serialization.IXmlSerializable ReadSerializable(System.Xml.Serialization.IXmlSerializable serializable, bool wrappedAny) { return default(System.Xml.Serialization.IXmlSerializable); }
    protected string ReadString(string value) { return default(string); }
    protected string ReadString(string value, bool trim) { return default(string); }
    protected object ReadTypedNull(System.Xml.XmlQualifiedName type) { return default(object); }
    protected object ReadTypedPrimitive(System.Xml.XmlQualifiedName type) { return default(object); }
    protected System.Xml.XmlDocument ReadXmlDocument(bool wrapped) { return default(System.Xml.XmlDocument); }
    protected System.Xml.XmlNode ReadXmlNode(bool wrapped) { return default(System.Xml.XmlNode); }
    protected void Referenced(object o) { }
    protected static System.Reflection.Assembly ResolveDynamicAssembly(string assemblyFullName) { return default(System.Reflection.Assembly); }
    protected System.Array ShrinkArray(System.Array a, int length, System.Type elementType, bool isNullable) { return default(System.Array); }
    protected byte[] ToByteArrayBase64(bool isNull) { return default(byte[]); }
    protected static byte[] ToByteArrayBase64(string value) { return default(byte[]); }
    protected byte[] ToByteArrayHex(bool isNull) { return default(byte[]); }
    protected static byte[] ToByteArrayHex(string value) { return default(byte[]); }
    protected static char ToChar(string value) { return default(char); }
    protected static System.DateTime ToDate(string value) { return default(System.DateTime); }
    protected static System.DateTime ToDateTime(string value) { return default(System.DateTime); }
    protected static long ToEnum(string value, System.Collections.Hashtable h, string typeName) { return default(long); }
    protected static System.DateTime ToTime(string value) { return default(System.DateTime); }
    protected static string ToXmlName(string value) { return default(string); }
    protected static string ToXmlNCName(string value) { return default(string); }
    protected static string ToXmlNmToken(string value) { return default(string); }
    protected static string ToXmlNmTokens(string value) { return default(string); }
    protected System.Xml.XmlQualifiedName ToXmlQualifiedName(string value) { return default(System.Xml.XmlQualifiedName); }
    protected void UnknownAttribute(object o, System.Xml.XmlAttribute attr) { }
    protected void UnknownAttribute(object o, System.Xml.XmlAttribute attr, string qnames) { }
    protected void UnknownElement(object o, System.Xml.XmlElement elem) { }
    protected void UnknownElement(object o, System.Xml.XmlElement elem, string qnames) { }
    protected void UnknownNode(object o) { }
    protected void UnknownNode(object o, string qnames) { }
    protected void UnreferencedObject(string id, object o) { }
    protected partial class CollectionFixup {
      public CollectionFixup(object collection, System.Xml.Serialization.XmlSerializationCollectionFixupCallback callback, object collectionItems) { }
      public System.Xml.Serialization.XmlSerializationCollectionFixupCallback Callback { get { return default(System.Xml.Serialization.XmlSerializationCollectionFixupCallback); } }
      public object Collection { get { return default(object); } }
      public object CollectionItems { get { return default(object); } }
    }
    protected partial class Fixup {
      public Fixup(object o, System.Xml.Serialization.XmlSerializationFixupCallback callback, int count) { }
      public Fixup(object o, System.Xml.Serialization.XmlSerializationFixupCallback callback, string[] ids) { }
      public System.Xml.Serialization.XmlSerializationFixupCallback Callback { get { return default(System.Xml.Serialization.XmlSerializationFixupCallback); } }
      public string[] Ids { get { return default(string[]); } }
      public object Source { get { return default(object); } set { } }
    }
  }
  public delegate void XmlSerializationWriteCallback(object o);
  public abstract partial class XmlSerializationWriter : System.Xml.Serialization.XmlSerializationGeneratedCode {
    protected XmlSerializationWriter() { }
    protected bool EscapeName { get { return default(bool); } set { } }
    protected System.Collections.ArrayList Namespaces { get { return default(System.Collections.ArrayList); } set { } }
    protected System.Xml.XmlWriter Writer { get { return default(System.Xml.XmlWriter); } set { } }
    protected void AddWriteCallback(System.Type type, string typeName, string typeNs, System.Xml.Serialization.XmlSerializationWriteCallback callback) { }
    protected System.Exception CreateChoiceIdentifierValueException(string value, string identifier, string name, string ns) { return default(System.Exception); }
    protected System.Exception CreateInvalidAnyTypeException(object o) { return default(System.Exception); }
    protected System.Exception CreateInvalidAnyTypeException(System.Type type) { return default(System.Exception); }
    protected System.Exception CreateInvalidChoiceIdentifierValueException(string type, string identifier) { return default(System.Exception); }
    protected System.Exception CreateInvalidEnumValueException(object value, string typeName) { return default(System.Exception); }
    protected System.Exception CreateMismatchChoiceException(string value, string elementName, string enumValue) { return default(System.Exception); }
    protected System.Exception CreateUnknownAnyElementException(string name, string ns) { return default(System.Exception); }
    protected System.Exception CreateUnknownTypeException(object o) { return default(System.Exception); }
    protected System.Exception CreateUnknownTypeException(System.Type type) { return default(System.Exception); }
    protected static byte[] FromByteArrayBase64(byte[] value) { return default(byte[]); }
    protected static string FromByteArrayHex(byte[] value) { return default(string); }
    protected static string FromChar(char value) { return default(string); }
    protected static string FromDate(System.DateTime value) { return default(string); }
    protected static string FromDateTime(System.DateTime value) { return default(string); }
    protected static string FromEnum(long value, string[] values, long[] ids) { return default(string); }
    protected static string FromEnum(long value, string[] values, long[] ids, string typeName) { return default(string); }
    protected static string FromTime(System.DateTime value) { return default(string); }
    protected static string FromXmlName(string name) { return default(string); }
    protected static string FromXmlNCName(string ncName) { return default(string); }
    protected static string FromXmlNmToken(string nmToken) { return default(string); }
    protected static string FromXmlNmTokens(string nmTokens) { return default(string); }
    protected string FromXmlQualifiedName(System.Xml.XmlQualifiedName xmlQualifiedName) { return default(string); }
    protected string FromXmlQualifiedName(System.Xml.XmlQualifiedName xmlQualifiedName, bool ignoreEmpty) { return default(string); }
    protected abstract void InitCallbacks();
    protected static System.Reflection.Assembly ResolveDynamicAssembly(string assemblyFullName) { return default(System.Reflection.Assembly); }
    protected void TopLevelElement() { }
    protected void WriteAttribute(string localName, byte[] value) { }
    protected void WriteAttribute(string localName, string value) { }
    protected void WriteAttribute(string localName, string ns, byte[] value) { }
    protected void WriteAttribute(string localName, string ns, string value) { }
    protected void WriteAttribute(string prefix, string localName, string ns, string value) { }
    protected void WriteElementEncoded(System.Xml.XmlNode node, string name, string ns, bool isNullable, bool any) { }
    protected void WriteElementLiteral(System.Xml.XmlNode node, string name, string ns, bool isNullable, bool any) { }
    protected void WriteElementQualifiedName(string localName, string ns, System.Xml.XmlQualifiedName value) { }
    protected void WriteElementQualifiedName(string localName, string ns, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName value) { }
    protected void WriteElementQualifiedName(string localName, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementString(string localName, string value) { }
    protected void WriteElementString(string localName, string ns, string value) { }
    protected void WriteElementString(string localName, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementString(string localName, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementStringRaw(string localName, byte[] value) { }
    protected void WriteElementStringRaw(string localName, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementStringRaw(string localName, string value) { }
    protected void WriteElementStringRaw(string localName, string ns, byte[] value) { }
    protected void WriteElementStringRaw(string localName, string ns, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementStringRaw(string localName, string ns, string value) { }
    protected void WriteElementStringRaw(string localName, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteElementStringRaw(string localName, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteEmptyTag(string name) { }
    protected void WriteEmptyTag(string name, string ns) { }
    protected void WriteEndElement() { }
    protected void WriteEndElement(object o) { }
    protected void WriteId(object o) { }
    protected void WriteNamespaceDeclarations(System.Xml.Serialization.XmlSerializerNamespaces xmlns) { }
    protected void WriteNullableQualifiedNameEncoded(string name, string ns, System.Xml.XmlQualifiedName value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteNullableQualifiedNameLiteral(string name, string ns, System.Xml.XmlQualifiedName value) { }
    protected void WriteNullableStringEncoded(string name, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteNullableStringEncodedRaw(string name, string ns, byte[] value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteNullableStringEncodedRaw(string name, string ns, string value, System.Xml.XmlQualifiedName xsiType) { }
    protected void WriteNullableStringLiteral(string name, string ns, string value) { }
    protected void WriteNullableStringLiteralRaw(string name, string ns, byte[] value) { }
    protected void WriteNullableStringLiteralRaw(string name, string ns, string value) { }
    protected void WriteNullTagEncoded(string name) { }
    protected void WriteNullTagEncoded(string name, string ns) { }
    protected void WriteNullTagLiteral(string name) { }
    protected void WriteNullTagLiteral(string name, string ns) { }
    protected void WritePotentiallyReferencingElement(string n, string ns, object o) { }
    protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType) { }
    protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType, bool suppressReference) { }
    protected void WritePotentiallyReferencingElement(string n, string ns, object o, System.Type ambientType, bool suppressReference, bool isNullable) { }
    protected void WriteReferencedElements() { }
    protected void WriteReferencingElement(string n, string ns, object o) { }
    protected void WriteReferencingElement(string n, string ns, object o, bool isNullable) { }
    protected void WriteRpcResult(string name, string ns) { }
    protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable serializable, string name, string ns, bool isNullable) { }
    protected void WriteSerializable(System.Xml.Serialization.IXmlSerializable serializable, string name, string ns, bool isNullable, bool wrapped) { }
    protected void WriteStartDocument() { }
    protected void WriteStartElement(string name) { }
    protected void WriteStartElement(string name, string ns) { }
    protected void WriteStartElement(string name, string ns, bool writePrefixed) { }
    protected void WriteStartElement(string name, string ns, object o) { }
    protected void WriteStartElement(string name, string ns, object o, bool writePrefixed) { }
    protected void WriteStartElement(string name, string ns, object o, bool writePrefixed, System.Xml.Serialization.XmlSerializerNamespaces xmlns) { }
    protected void WriteTypedPrimitive(string name, string ns, object o, bool xsiType) { }
    protected void WriteValue(byte[] value) { }
    protected void WriteValue(string value) { }
    protected void WriteXmlAttribute(System.Xml.XmlNode node) { }
    protected void WriteXmlAttribute(System.Xml.XmlNode node, object container) { }
    protected void WriteXsiType(string name, string ns) { }
  }
  public partial class XmlSerializer {
    protected XmlSerializer() { }
    public XmlSerializer(System.Type type) { }
    public XmlSerializer(System.Type type, string defaultNamespace) { }
    public XmlSerializer(System.Type type, System.Type[] extraTypes) { }
    public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides) { }
    public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { }
    public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace, string location) { }
    [System.ObsoleteAttribute("This method is obsolete and will be removed in a future release of the .NET Framework. Please use a XmlSerializer constructor overload which does not take an Evidence parameter. See http://go2.microsoft.com/fwlink/?LinkId=131738 for more information.")]
    public XmlSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { }
    public XmlSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { }
    public event System.Xml.Serialization.XmlAttributeEventHandler UnknownAttribute { add { } remove { } }
    public event System.Xml.Serialization.XmlElementEventHandler UnknownElement { add { } remove { } }
    public event System.Xml.Serialization.XmlNodeEventHandler UnknownNode { add { } remove { } }
    public event System.Xml.Serialization.UnreferencedObjectEventHandler UnreferencedObject { add { } remove { } }
    public virtual bool CanDeserialize(System.Xml.XmlReader xmlReader) { return default(bool); }
    protected virtual System.Xml.Serialization.XmlSerializationReader CreateReader() { return default(System.Xml.Serialization.XmlSerializationReader); }
    protected virtual System.Xml.Serialization.XmlSerializationWriter CreateWriter() { return default(System.Xml.Serialization.XmlSerializationWriter); }
    public object Deserialize(System.IO.Stream stream) { return default(object); }
    public object Deserialize(System.IO.TextReader textReader) { return default(object); }
    protected virtual object Deserialize(System.Xml.Serialization.XmlSerializationReader reader) { return default(object); }
    public object Deserialize(System.Xml.XmlReader xmlReader) { return default(object); }
    public object Deserialize(System.Xml.XmlReader xmlReader, string encodingStyle) { return default(object); }
    public object Deserialize(System.Xml.XmlReader xmlReader, string encodingStyle, System.Xml.Serialization.XmlDeserializationEvents events) { return default(object); }
    public object Deserialize(System.Xml.XmlReader xmlReader, System.Xml.Serialization.XmlDeserializationEvents events) { return default(object); }
    public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[] mappings) { return default(System.Xml.Serialization.XmlSerializer[]); }
    [System.ObsoleteAttribute("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of FromMappings which does not take an Evidence parameter. See http://go2.microsoft.com/fwlink/?LinkId=131738 for more information.")]
    public static System.Xml.Serialization.XmlSerializer[] FromMappings(System.Xml.Serialization.XmlMapping[] mappings, System.Type type) { return default(System.Xml.Serialization.XmlSerializer[]); }
    public static System.Xml.Serialization.XmlSerializer[] FromTypes(System.Type[] types) { return default(System.Xml.Serialization.XmlSerializer[]); }
    public static System.Reflection.Assembly GenerateSerializer(System.Type[] types, System.Xml.Serialization.XmlMapping[] mappings) { return default(System.Reflection.Assembly); }
    public static string GetXmlSerializerAssemblyName(System.Type type) { return default(string); }
    public static string GetXmlSerializerAssemblyName(System.Type type, string defaultNamespace) { return default(string); }
    public void Serialize(System.IO.Stream stream, object o) { }
    public void Serialize(System.IO.Stream stream, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
    public void Serialize(System.IO.TextWriter textWriter, object o) { }
    public void Serialize(System.IO.TextWriter textWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
    protected virtual void Serialize(object o, System.Xml.Serialization.XmlSerializationWriter writer) { }
    public void Serialize(System.Xml.XmlWriter xmlWriter, object o) { }
    public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
    public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces, string encodingStyle) { }
    public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces, string encodingStyle, string id) { }
  }
  public sealed partial class XmlSerializerAssemblyAttribute : System.Attribute {
    public XmlSerializerAssemblyAttribute() { }
    public XmlSerializerAssemblyAttribute(string assemblyName) { }
    public XmlSerializerAssemblyAttribute(string assemblyName, string codeBase) { }
    public string AssemblyName { get { return default(string); } set { } }
    public string CodeBase { get { return default(string); } set { } }
  }
  public partial class XmlSerializerFactory {
    public XmlSerializerFactory() { }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, string defaultNamespace) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Type[] extraTypes) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace, string location) { return default(System.Xml.Serialization.XmlSerializer); }
    [System.ObsoleteAttribute("This method is obsolete and will be removed in a future release of the .NET Framework. Please use an overload of CreateSerializer which does not take an Evidence parameter. See http://go2.microsoft.com/fwlink/?LinkId=131738 for more information.")]
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { return default(System.Xml.Serialization.XmlSerializer); }
    public System.Xml.Serialization.XmlSerializer CreateSerializer(System.Xml.Serialization.XmlTypeMapping xmlTypeMapping) { return default(System.Xml.Serialization.XmlSerializer); }
  }
  public abstract partial class XmlSerializerImplementation {
    protected XmlSerializerImplementation() { }
    public virtual System.Xml.Serialization.XmlSerializationReader Reader { get { return default(System.Xml.Serialization.XmlSerializationReader); } }
    public virtual System.Collections.Hashtable ReadMethods { get { return default(System.Collections.Hashtable); } }
    public virtual System.Collections.Hashtable TypedSerializers { get { return default(System.Collections.Hashtable); } }
    public virtual System.Collections.Hashtable WriteMethods { get { return default(System.Collections.Hashtable); } }
    public virtual System.Xml.Serialization.XmlSerializationWriter Writer { get { return default(System.Xml.Serialization.XmlSerializationWriter); } }
    public virtual bool CanSerialize(System.Type type) { return default(bool); }
    public virtual System.Xml.Serialization.XmlSerializer GetSerializer(System.Type type) { return default(System.Xml.Serialization.XmlSerializer); }
  }
  public partial class XmlSerializerNamespaces {
    public XmlSerializerNamespaces() { }
    public XmlSerializerNamespaces(System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
    public XmlSerializerNamespaces(System.Xml.XmlQualifiedName[] namespaces) { }
    public int Count { get { return default(int); } }
    public void Add(string prefix, string ns) { }
    public System.Xml.XmlQualifiedName[] ToArray() { return default(System.Xml.XmlQualifiedName[]); }
  }
  public sealed partial class XmlSerializerVersionAttribute : System.Attribute {
    public XmlSerializerVersionAttribute() { }
    public XmlSerializerVersionAttribute(System.Type type) { }
    public string Namespace { get { return default(string); } set { } }
    public string ParentAssemblyId { get { return default(string); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
    public string Version { get { return default(string); } set { } }
  }
  public partial class XmlTextAttribute : System.Attribute {
    public XmlTextAttribute() { }
    public XmlTextAttribute(System.Type type) { }
    public string DataType { get { return default(string); } set { } }
    public System.Type Type { get { return default(System.Type); } set { } }
  }
  public partial class XmlTypeAttribute : System.Attribute {
    public XmlTypeAttribute() { }
    public XmlTypeAttribute(string typeName) { }
    public bool AnonymousType { get { return default(bool); } set { } }
    public bool IncludeInSchema { get { return default(bool); } set { } }
    public string Namespace { get { return default(string); } set { } }
    public string TypeName { get { return default(string); } set { } }
  }
  public partial class XmlTypeMapping : System.Xml.Serialization.XmlMapping {
    internal XmlTypeMapping() { }
    public string TypeFullName { get { return default(string); } }
    public string TypeName { get { return default(string); } }
    public string XsdTypeName { get { return default(string); } }
    public string XsdTypeNamespace { get { return default(string); } }
  }
}
namespace System.Xml.Serialization.Advanced {
  public abstract partial class SchemaImporterExtension {
    protected SchemaImporterExtension() { }
  }
  public partial class SchemaImporterExtensionCollection : System.Collections.CollectionBase {
    public SchemaImporterExtensionCollection() { }
    public System.Xml.Serialization.Advanced.SchemaImporterExtension this[int index] { get { return default(System.Xml.Serialization.Advanced.SchemaImporterExtension); } set { } }
    public int Add(string name, System.Type type) { return default(int); }
    public int Add(System.Xml.Serialization.Advanced.SchemaImporterExtension extension) { return default(int); }
    public new void Clear() { }
    public bool Contains(System.Xml.Serialization.Advanced.SchemaImporterExtension extension) { return default(bool); }
    public void CopyTo(System.Xml.Serialization.Advanced.SchemaImporterExtension[] array, int index) { }
    public int IndexOf(System.Xml.Serialization.Advanced.SchemaImporterExtension extension) { return default(int); }
    public void Insert(int index, System.Xml.Serialization.Advanced.SchemaImporterExtension extension) { }
    public void Remove(string name) { }
    public void Remove(System.Xml.Serialization.Advanced.SchemaImporterExtension extension) { }
  }
}