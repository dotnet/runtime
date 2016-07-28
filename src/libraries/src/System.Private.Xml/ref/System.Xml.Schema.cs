// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Xml.Schema {
  public partial interface IXmlSchemaInfo {
    bool IsDefault { get; }
    bool IsNil { get; }
    System.Xml.Schema.XmlSchemaSimpleType MemberType { get; }
    System.Xml.Schema.XmlSchemaAttribute SchemaAttribute { get; }
    System.Xml.Schema.XmlSchemaElement SchemaElement { get; }
    System.Xml.Schema.XmlSchemaType SchemaType { get; }
    System.Xml.Schema.XmlSchemaValidity Validity { get; }
  }
  public partial class ValidationEventArgs : System.EventArgs {
    internal ValidationEventArgs() { }
    public System.Xml.Schema.XmlSchemaException Exception { get { return default(System.Xml.Schema.XmlSchemaException); } }
    public string Message { get { return default(string); } }
    public System.Xml.Schema.XmlSeverityType Severity { get { return default(System.Xml.Schema.XmlSeverityType); } }
  }
  public delegate void ValidationEventHandler(object sender, System.Xml.Schema.ValidationEventArgs e);
  public sealed partial class XmlAtomicValue : System.Xml.XPath.XPathItem {
    internal XmlAtomicValue() { }
    public override bool IsNode { get { return default(bool); } }
    public override object TypedValue { get { return default(object); } }
    public override string Value { get { return default(string); } }
    public override bool ValueAsBoolean { get { return default(bool); } }
    public override System.DateTime ValueAsDateTime { get { return default(System.DateTime); } }
    public override double ValueAsDouble { get { return default(double); } }
    public override int ValueAsInt { get { return default(int); } }
    public override long ValueAsLong { get { return default(long); } }
    public override System.Type ValueType { get { return default(System.Type); } }
    public override System.Xml.Schema.XmlSchemaType XmlType { get { return default(System.Xml.Schema.XmlSchemaType); } }
    public System.Xml.Schema.XmlAtomicValue Clone() { return default(System.Xml.Schema.XmlAtomicValue); }
    public override string ToString() { return default(string); }
    public override object ValueAs(System.Type type, System.Xml.IXmlNamespaceResolver nsResolver) { return default(object); }
  }
  public partial class XmlSchema : System.Xml.Schema.XmlSchemaObject {
    public const string InstanceNamespace = "http://www.w3.org/2001/XMLSchema-instance";
    public const string Namespace = "http://www.w3.org/2001/XMLSchema";
    public XmlSchema() { }
    public System.Xml.Schema.XmlSchemaForm AttributeFormDefault { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectTable Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod BlockDefault { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaForm ElementFormDefault { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public System.Xml.Schema.XmlSchemaObjectTable Elements { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod FinalDefault { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaObjectTable Groups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public string Id { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Includes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public bool IsCompiled { get { return default(bool); } }
    public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.Schema.XmlSchemaObjectTable Notations { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public string TargetNamespace { get { return default(string); } set { } }
    public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
    public string Version { get { return default(string); } set { } }
    [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
    public void Compile(System.Xml.Schema.ValidationEventHandler validationEventHandler) { }
    [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
    public void Compile(System.Xml.Schema.ValidationEventHandler validationEventHandler, System.Xml.XmlResolver resolver) { }
    public static System.Xml.Schema.XmlSchema Read(System.IO.Stream stream, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
    public static System.Xml.Schema.XmlSchema Read(System.IO.TextReader reader, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
    public static System.Xml.Schema.XmlSchema Read(System.Xml.XmlReader reader, System.Xml.Schema.ValidationEventHandler validationEventHandler) { return default(System.Xml.Schema.XmlSchema); }
    public void Write(System.IO.Stream stream) { }
    public void Write(System.IO.Stream stream, System.Xml.XmlNamespaceManager namespaceManager) { }
    public void Write(System.IO.TextWriter writer) { }
    public void Write(System.IO.TextWriter writer, System.Xml.XmlNamespaceManager namespaceManager) { }
    public void Write(System.Xml.XmlWriter writer) { }
    public void Write(System.Xml.XmlWriter writer, System.Xml.XmlNamespaceManager namespaceManager) { }
  }
  public partial class XmlSchemaAll : System.Xml.Schema.XmlSchemaGroupBase {
    public XmlSchemaAll() { }
    public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
  }
  public partial class XmlSchemaAnnotated : System.Xml.Schema.XmlSchemaObject {
    public XmlSchemaAnnotated() { }
    public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
    public string Id { get { return default(string); } set { } }
    public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
  }
  public partial class XmlSchemaAnnotation : System.Xml.Schema.XmlSchemaObject {
    public XmlSchemaAnnotation() { }
    public string Id { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
  }
  public partial class XmlSchemaAny : System.Xml.Schema.XmlSchemaParticle {
    public XmlSchemaAny() { }
    public string Namespace { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { return default(System.Xml.Schema.XmlSchemaContentProcessing); } set { } }
  }
  public partial class XmlSchemaAnyAttribute : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaAnyAttribute() { }
    public string Namespace { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaContentProcessing ProcessContents { get { return default(System.Xml.Schema.XmlSchemaContentProcessing); } set { } }
  }
  public partial class XmlSchemaAppInfo : System.Xml.Schema.XmlSchemaObject {
    public XmlSchemaAppInfo() { }
    public System.Xml.XmlNode[] Markup { get { return default(System.Xml.XmlNode[]); } set { } }
    public string Source { get { return default(string); } set { } }
  }
  public partial class XmlSchemaAttribute : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaAttribute() { }
    public System.Xml.Schema.XmlSchemaSimpleType AttributeSchemaType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } }
    [System.ObsoleteAttribute("This property has been deprecated. Please use AttributeSchemaType property that returns a strongly typed attribute type. http://go.microsoft.com/fwlink/?linkid=14202")]
    public object AttributeType { get { return default(object); } }
    public string DefaultValue { get { return default(string); } set { } }
    public string FixedValue { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaSimpleType SchemaType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.XmlQualifiedName SchemaTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaUse Use { get { return default(System.Xml.Schema.XmlSchemaUse); } set { } }
  }
  public partial class XmlSchemaAttributeGroup : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaAttributeGroup() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public string Name { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    public System.Xml.Schema.XmlSchemaAttributeGroup RedefinedAttributeGroup { get { return default(System.Xml.Schema.XmlSchemaAttributeGroup); } }
  }
  public partial class XmlSchemaAttributeGroupRef : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaAttributeGroupRef() { }
    public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaChoice : System.Xml.Schema.XmlSchemaGroupBase {
    public XmlSchemaChoice() { }
    public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
  }
  [System.ObsoleteAttribute("Use System.Xml.Schema.XmlSchemaSet for schema compilation and validation. http://go.microsoft.com/fwlink/?linkid=14202")]
  public sealed partial class XmlSchemaCollection : System.Collections.ICollection, System.Collections.IEnumerable {
    public XmlSchemaCollection() { }
    public XmlSchemaCollection(System.Xml.XmlNameTable nametable) { }
    public int Count { get { return default(int); } }
    public System.Xml.Schema.XmlSchema this[string ns] { get { return default(System.Xml.Schema.XmlSchema); } }
    public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
    int System.Collections.ICollection.Count { get { return default(int); } }
    bool System.Collections.ICollection.IsSynchronized { get { return default(bool); } }
    object System.Collections.ICollection.SyncRoot { get { return default(object); } }
    public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
    public System.Xml.Schema.XmlSchema Add(string ns, string uri) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(string ns, System.Xml.XmlReader reader) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(string ns, System.Xml.XmlReader reader, System.Xml.XmlResolver resolver) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema, System.Xml.XmlResolver resolver) { return default(System.Xml.Schema.XmlSchema); }
    public void Add(System.Xml.Schema.XmlSchemaCollection schema) { }
    public bool Contains(string ns) { return default(bool); }
    public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
    public void CopyTo(System.Xml.Schema.XmlSchema[] array, int index) { }
    public System.Xml.Schema.XmlSchemaCollectionEnumerator GetEnumerator() { return default(System.Xml.Schema.XmlSchemaCollectionEnumerator); }
    void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return default(System.Collections.IEnumerator); }
  }
  public sealed partial class XmlSchemaCollectionEnumerator : System.Collections.IEnumerator {
    internal XmlSchemaCollectionEnumerator() { }
    public System.Xml.Schema.XmlSchema Current { get { return default(System.Xml.Schema.XmlSchema); } }
    object System.Collections.IEnumerator.Current { get { return default(object); } }
    public bool MoveNext() { return default(bool); }
    bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
    void System.Collections.IEnumerator.Reset() { }
  }
  public sealed partial class XmlSchemaCompilationSettings {
    public XmlSchemaCompilationSettings() { }
    public bool EnableUpaCheck { get { return default(bool); } set { } }
  }
  public partial class XmlSchemaComplexContent : System.Xml.Schema.XmlSchemaContentModel {
    public XmlSchemaComplexContent() { }
    public override System.Xml.Schema.XmlSchemaContent Content { get { return default(System.Xml.Schema.XmlSchemaContent); } set { } }
    public bool IsMixed { get { return default(bool); } set { } }
  }
  public partial class XmlSchemaComplexContentExtension : System.Xml.Schema.XmlSchemaContent {
    public XmlSchemaComplexContentExtension() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
  }
  public partial class XmlSchemaComplexContentRestriction : System.Xml.Schema.XmlSchemaContent {
    public XmlSchemaComplexContentRestriction() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
  }
  public partial class XmlSchemaComplexType : System.Xml.Schema.XmlSchemaType {
    public XmlSchemaComplexType() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.Schema.XmlSchemaObjectTable AttributeUses { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaAnyAttribute AttributeWildcard { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
    public System.Xml.Schema.XmlSchemaContentModel ContentModel { get { return default(System.Xml.Schema.XmlSchemaContentModel); } set { } }
    public System.Xml.Schema.XmlSchemaContentType ContentType { get { return default(System.Xml.Schema.XmlSchemaContentType); } }
    public System.Xml.Schema.XmlSchemaParticle ContentTypeParticle { get { return default(System.Xml.Schema.XmlSchemaParticle); } }
    public bool IsAbstract { get { return default(bool); } set { } }
    public override bool IsMixed { get { return default(bool); } set { } }
    public System.Xml.Schema.XmlSchemaParticle Particle { get { return default(System.Xml.Schema.XmlSchemaParticle); } set { } }
  }
  public abstract partial class XmlSchemaContent : System.Xml.Schema.XmlSchemaAnnotated {
    protected XmlSchemaContent() { }
  }
  public abstract partial class XmlSchemaContentModel : System.Xml.Schema.XmlSchemaAnnotated {
    protected XmlSchemaContentModel() { }
    public abstract System.Xml.Schema.XmlSchemaContent Content { get; set; }
  }
  public enum XmlSchemaContentProcessing {
    Lax = 2,
    None = 0,
    Skip = 1,
    Strict = 3,
  }
  public enum XmlSchemaContentType {
    ElementOnly = 2,
    Empty = 1,
    Mixed = 3,
    TextOnly = 0,
  }
  public abstract partial class XmlSchemaDatatype {
    protected XmlSchemaDatatype() { }
    public abstract System.Xml.XmlTokenizedType TokenizedType { get; }
    public virtual System.Xml.Schema.XmlTypeCode TypeCode { get { return default(System.Xml.Schema.XmlTypeCode); } }
    public abstract System.Type ValueType { get; }
    public virtual System.Xml.Schema.XmlSchemaDatatypeVariety Variety { get { return default(System.Xml.Schema.XmlSchemaDatatypeVariety); } }
    public virtual object ChangeType(object value, System.Type targetType) { return default(object); }
    public virtual object ChangeType(object value, System.Type targetType, System.Xml.IXmlNamespaceResolver namespaceResolver) { return default(object); }
    public virtual bool IsDerivedFrom(System.Xml.Schema.XmlSchemaDatatype datatype) { return default(bool); }
    public abstract object ParseValue(string s, System.Xml.XmlNameTable nameTable, System.Xml.IXmlNamespaceResolver nsmgr);
  }
  public enum XmlSchemaDatatypeVariety {
    Atomic = 0,
    List = 1,
    Union = 2,
  }
  [System.FlagsAttribute]
  public enum XmlSchemaDerivationMethod {
    All = 255,
    Empty = 0,
    Extension = 2,
    List = 8,
    None = 256,
    Restriction = 4,
    Substitution = 1,
    Union = 16,
  }
  public partial class XmlSchemaDocumentation : System.Xml.Schema.XmlSchemaObject {
    public XmlSchemaDocumentation() { }
    public string Language { get { return default(string); } set { } }
    public System.Xml.XmlNode[] Markup { get { return default(System.Xml.XmlNode[]); } set { } }
    public string Source { get { return default(string); } set { } }
  }
  public partial class XmlSchemaElement : System.Xml.Schema.XmlSchemaParticle {
    public XmlSchemaElement() { }
    public System.Xml.Schema.XmlSchemaDerivationMethod Block { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaDerivationMethod BlockResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
    public System.Xml.Schema.XmlSchemaObjectCollection Constraints { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public string DefaultValue { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaType ElementSchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } }
    [System.ObsoleteAttribute("This property has been deprecated. Please use ElementSchemaType property that returns a strongly typed element type. http://go.microsoft.com/fwlink/?linkid=14202")]
    public object ElementType { get { return default(object); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
    public string FixedValue { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
    public bool IsAbstract { get { return default(bool); } set { } }
    public bool IsNillable { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaType SchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } set { } }
    public System.Xml.XmlQualifiedName SchemaTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.XmlQualifiedName SubstitutionGroup { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaEnumerationFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaEnumerationFacet() { }
  }
  public partial class XmlSchemaException : System.Exception {
    public XmlSchemaException() { }
    public XmlSchemaException(string message) { }
    public XmlSchemaException(string message, System.Exception innerException) { }
    public XmlSchemaException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
    public int LineNumber { get { return default(int); } }
    public int LinePosition { get { return default(int); } }
    public override string Message { get { return default(string); } }
    public System.Xml.Schema.XmlSchemaObject SourceSchemaObject { get { return default(System.Xml.Schema.XmlSchemaObject); } }
    public string SourceUri { get { return default(string); } }
  }
  public abstract partial class XmlSchemaExternal : System.Xml.Schema.XmlSchemaObject {
    protected XmlSchemaExternal() { }
    public string Id { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchema Schema { get { return default(System.Xml.Schema.XmlSchema); } set { } }
    public string SchemaLocation { get { return default(string); } set { } }
    public System.Xml.XmlAttribute[] UnhandledAttributes { get { return default(System.Xml.XmlAttribute[]); } set { } }
  }
  public abstract partial class XmlSchemaFacet : System.Xml.Schema.XmlSchemaAnnotated {
    protected XmlSchemaFacet() { }
    public virtual bool IsFixed { get { return default(bool); } set { } }
    public string Value { get { return default(string); } set { } }
  }
  public enum XmlSchemaForm {
    None = 0,
    Qualified = 1,
    Unqualified = 2,
  }
  public partial class XmlSchemaFractionDigitsFacet : System.Xml.Schema.XmlSchemaNumericFacet {
    public XmlSchemaFractionDigitsFacet() { }
  }
  public partial class XmlSchemaGroup : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaGroup() { }
    public string Name { get { return default(string); } set { } }
    public System.Xml.Schema.XmlSchemaGroupBase Particle { get { return default(System.Xml.Schema.XmlSchemaGroupBase); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
  }
  public abstract partial class XmlSchemaGroupBase : System.Xml.Schema.XmlSchemaParticle {
    protected XmlSchemaGroupBase() { }
    public abstract System.Xml.Schema.XmlSchemaObjectCollection Items { get; }
  }
  public partial class XmlSchemaGroupRef : System.Xml.Schema.XmlSchemaParticle {
    public XmlSchemaGroupRef() { }
    public System.Xml.Schema.XmlSchemaGroupBase Particle { get { return default(System.Xml.Schema.XmlSchemaGroupBase); } }
    public System.Xml.XmlQualifiedName RefName { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaIdentityConstraint : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaIdentityConstraint() { }
    public System.Xml.Schema.XmlSchemaObjectCollection Fields { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public string Name { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    public System.Xml.Schema.XmlSchemaXPath Selector { get { return default(System.Xml.Schema.XmlSchemaXPath); } set { } }
  }
  public partial class XmlSchemaImport : System.Xml.Schema.XmlSchemaExternal {
    public XmlSchemaImport() { }
    public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
    public string Namespace { get { return default(string); } set { } }
  }
  public partial class XmlSchemaInclude : System.Xml.Schema.XmlSchemaExternal {
    public XmlSchemaInclude() { }
    public System.Xml.Schema.XmlSchemaAnnotation Annotation { get { return default(System.Xml.Schema.XmlSchemaAnnotation); } set { } }
  }
  public sealed partial class XmlSchemaInference {
    public XmlSchemaInference() { }
    public System.Xml.Schema.XmlSchemaInference.InferenceOption Occurrence { get { return default(System.Xml.Schema.XmlSchemaInference.InferenceOption); } set { } }
    public System.Xml.Schema.XmlSchemaInference.InferenceOption TypeInference { get { return default(System.Xml.Schema.XmlSchemaInference.InferenceOption); } set { } }
    public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument) { return default(System.Xml.Schema.XmlSchemaSet); }
    public System.Xml.Schema.XmlSchemaSet InferSchema(System.Xml.XmlReader instanceDocument, System.Xml.Schema.XmlSchemaSet schemas) { return default(System.Xml.Schema.XmlSchemaSet); }
    public enum InferenceOption {
      Relaxed = 1,
      Restricted = 0,
    }
  }
  public partial class XmlSchemaInferenceException : System.Xml.Schema.XmlSchemaException {
    public XmlSchemaInferenceException() { }
    public XmlSchemaInferenceException(string message) { }
    public XmlSchemaInferenceException(string message, System.Exception innerException) { }
    public XmlSchemaInferenceException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
  }
  public partial class XmlSchemaInfo : System.Xml.Schema.IXmlSchemaInfo {
    public XmlSchemaInfo() { }
    public System.Xml.Schema.XmlSchemaContentType ContentType { get { return default(System.Xml.Schema.XmlSchemaContentType); } set { } }
    public bool IsDefault { get { return default(bool); } set { } }
    public bool IsNil { get { return default(bool); } set { } }
    public System.Xml.Schema.XmlSchemaSimpleType MemberType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.Schema.XmlSchemaAttribute SchemaAttribute { get { return default(System.Xml.Schema.XmlSchemaAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaElement SchemaElement { get { return default(System.Xml.Schema.XmlSchemaElement); } set { } }
    public System.Xml.Schema.XmlSchemaType SchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } set { } }
    public System.Xml.Schema.XmlSchemaValidity Validity { get { return default(System.Xml.Schema.XmlSchemaValidity); } set { } }
  }
  public partial class XmlSchemaKey : System.Xml.Schema.XmlSchemaIdentityConstraint {
    public XmlSchemaKey() { }
  }
  public partial class XmlSchemaKeyref : System.Xml.Schema.XmlSchemaIdentityConstraint {
    public XmlSchemaKeyref() { }
    public System.Xml.XmlQualifiedName Refer { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet {
    public XmlSchemaLengthFacet() { }
  }
  public partial class XmlSchemaMaxExclusiveFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaMaxExclusiveFacet() { }
  }
  public partial class XmlSchemaMaxInclusiveFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaMaxInclusiveFacet() { }
  }
  public partial class XmlSchemaMaxLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet {
    public XmlSchemaMaxLengthFacet() { }
  }
  public partial class XmlSchemaMinExclusiveFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaMinExclusiveFacet() { }
  }
  public partial class XmlSchemaMinInclusiveFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaMinInclusiveFacet() { }
  }
  public partial class XmlSchemaMinLengthFacet : System.Xml.Schema.XmlSchemaNumericFacet {
    public XmlSchemaMinLengthFacet() { }
  }
  public partial class XmlSchemaNotation : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaNotation() { }
    public string Name { get { return default(string); } set { } }
    public string Public { get { return default(string); } set { } }
    public string System { get { return default(string); } set { } }
  }
  public abstract partial class XmlSchemaNumericFacet : System.Xml.Schema.XmlSchemaFacet {
    protected XmlSchemaNumericFacet() { }
  }
  public abstract partial class XmlSchemaObject {
    protected XmlSchemaObject() { }
    public int LineNumber { get { return default(int); } set { } }
    public int LinePosition { get { return default(int); } set { } }
    public System.Xml.Serialization.XmlSerializerNamespaces Namespaces { get { return default(System.Xml.Serialization.XmlSerializerNamespaces); } set { } }
    public System.Xml.Schema.XmlSchemaObject Parent { get { return default(System.Xml.Schema.XmlSchemaObject); } set { } }
    public string SourceUri { get { return default(string); } set { } }
  }
  public partial class XmlSchemaObjectCollection : System.Collections.CollectionBase {
    public XmlSchemaObjectCollection() { }
    public XmlSchemaObjectCollection(System.Xml.Schema.XmlSchemaObject parent) { }
    public virtual System.Xml.Schema.XmlSchemaObject this[int index] { get { return default(System.Xml.Schema.XmlSchemaObject); } set { } }
    public int Add(System.Xml.Schema.XmlSchemaObject item) { return default(int); }
    public bool Contains(System.Xml.Schema.XmlSchemaObject item) { return default(bool); }
    public void CopyTo(System.Xml.Schema.XmlSchemaObject[] array, int index) { }
    public new System.Xml.Schema.XmlSchemaObjectEnumerator GetEnumerator() { return default(System.Xml.Schema.XmlSchemaObjectEnumerator); }
    public int IndexOf(System.Xml.Schema.XmlSchemaObject item) { return default(int); }
    public void Insert(int index, System.Xml.Schema.XmlSchemaObject item) { }
    protected override void OnClear() { }
    protected override void OnInsert(int index, object item) { }
    protected override void OnRemove(int index, object item) { }
    protected override void OnSet(int index, object oldValue, object newValue) { }
    public void Remove(System.Xml.Schema.XmlSchemaObject item) { }
  }
  public partial class XmlSchemaObjectEnumerator : System.Collections.IEnumerator {
    internal XmlSchemaObjectEnumerator() { }
    public System.Xml.Schema.XmlSchemaObject Current { get { return default(System.Xml.Schema.XmlSchemaObject); } }
    object System.Collections.IEnumerator.Current { get { return default(object); } }
    public bool MoveNext() { return default(bool); }
    public void Reset() { }
    bool System.Collections.IEnumerator.MoveNext() { return default(bool); }
    void System.Collections.IEnumerator.Reset() { }
  }
  public partial class XmlSchemaObjectTable {
    internal XmlSchemaObjectTable() { }
    public int Count { get { return default(int); } }
    public System.Xml.Schema.XmlSchemaObject this[System.Xml.XmlQualifiedName name] { get { return default(System.Xml.Schema.XmlSchemaObject); } }
    public System.Collections.ICollection Names { get { return default(System.Collections.ICollection); } }
    public System.Collections.ICollection Values { get { return default(System.Collections.ICollection); } }
    public bool Contains(System.Xml.XmlQualifiedName name) { return default(bool); }
    public System.Collections.IDictionaryEnumerator GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
  }
  public abstract partial class XmlSchemaParticle : System.Xml.Schema.XmlSchemaAnnotated {
    protected XmlSchemaParticle() { }
    public decimal MaxOccurs { get { return default(decimal); } set { } }
    public string MaxOccursString { get { return default(string); } set { } }
    public decimal MinOccurs { get { return default(decimal); } set { } }
    public string MinOccursString { get { return default(string); } set { } }
  }
  public partial class XmlSchemaPatternFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaPatternFacet() { }
  }
  public partial class XmlSchemaRedefine : System.Xml.Schema.XmlSchemaExternal {
    public XmlSchemaRedefine() { }
    public System.Xml.Schema.XmlSchemaObjectTable AttributeGroups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectTable Groups { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.Schema.XmlSchemaObjectTable SchemaTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
  }
  public partial class XmlSchemaSequence : System.Xml.Schema.XmlSchemaGroupBase {
    public XmlSchemaSequence() { }
    public override System.Xml.Schema.XmlSchemaObjectCollection Items { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
  }
  public partial class XmlSchemaSet {
    public XmlSchemaSet() { }
    public XmlSchemaSet(System.Xml.XmlNameTable nameTable) { }
    public System.Xml.Schema.XmlSchemaCompilationSettings CompilationSettings { get { return default(System.Xml.Schema.XmlSchemaCompilationSettings); } set { } }
    public int Count { get { return default(int); } }
    public System.Xml.Schema.XmlSchemaObjectTable GlobalAttributes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectTable GlobalElements { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public System.Xml.Schema.XmlSchemaObjectTable GlobalTypes { get { return default(System.Xml.Schema.XmlSchemaObjectTable); } }
    public bool IsCompiled { get { return default(bool); } }
    public System.Xml.XmlNameTable NameTable { get { return default(System.Xml.XmlNameTable); } }
    public System.Xml.XmlResolver XmlResolver { set { } }
    public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
    public System.Xml.Schema.XmlSchema Add(string targetNamespace, string schemaUri) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(string targetNamespace, System.Xml.XmlReader schemaDocument) { return default(System.Xml.Schema.XmlSchema); }
    public System.Xml.Schema.XmlSchema Add(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
    public void Add(System.Xml.Schema.XmlSchemaSet schemas) { }
    public void Compile() { }
    public bool Contains(string targetNamespace) { return default(bool); }
    public bool Contains(System.Xml.Schema.XmlSchema schema) { return default(bool); }
    public void CopyTo(System.Xml.Schema.XmlSchema[] schemas, int index) { }
    public System.Xml.Schema.XmlSchema Remove(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
    public bool RemoveRecursive(System.Xml.Schema.XmlSchema schemaToRemove) { return default(bool); }
    public System.Xml.Schema.XmlSchema Reprocess(System.Xml.Schema.XmlSchema schema) { return default(System.Xml.Schema.XmlSchema); }
    public System.Collections.ICollection Schemas() { return default(System.Collections.ICollection); }
    public System.Collections.ICollection Schemas(string targetNamespace) { return default(System.Collections.ICollection); }
  }
  public partial class XmlSchemaSimpleContent : System.Xml.Schema.XmlSchemaContentModel {
    public XmlSchemaSimpleContent() { }
    public override System.Xml.Schema.XmlSchemaContent Content { get { return default(System.Xml.Schema.XmlSchemaContent); } set { } }
  }
  public partial class XmlSchemaSimpleContentExtension : System.Xml.Schema.XmlSchemaContent {
    public XmlSchemaSimpleContentExtension() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaSimpleContentRestriction : System.Xml.Schema.XmlSchemaContent {
    public XmlSchemaSimpleContentRestriction() { }
    public System.Xml.Schema.XmlSchemaAnyAttribute AnyAttribute { get { return default(System.Xml.Schema.XmlSchemaAnyAttribute); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Attributes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.Schema.XmlSchemaSimpleType BaseType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
  }
  public partial class XmlSchemaSimpleType : System.Xml.Schema.XmlSchemaType {
    public XmlSchemaSimpleType() { }
    public System.Xml.Schema.XmlSchemaSimpleTypeContent Content { get { return default(System.Xml.Schema.XmlSchemaSimpleTypeContent); } set { } }
  }
  public abstract partial class XmlSchemaSimpleTypeContent : System.Xml.Schema.XmlSchemaAnnotated {
    protected XmlSchemaSimpleTypeContent() { }
  }
  public partial class XmlSchemaSimpleTypeList : System.Xml.Schema.XmlSchemaSimpleTypeContent {
    public XmlSchemaSimpleTypeList() { }
    public System.Xml.Schema.XmlSchemaSimpleType BaseItemType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.Schema.XmlSchemaSimpleType ItemType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.XmlQualifiedName ItemTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
  }
  public partial class XmlSchemaSimpleTypeRestriction : System.Xml.Schema.XmlSchemaSimpleTypeContent {
    public XmlSchemaSimpleTypeRestriction() { }
    public System.Xml.Schema.XmlSchemaSimpleType BaseType { get { return default(System.Xml.Schema.XmlSchemaSimpleType); } set { } }
    public System.Xml.XmlQualifiedName BaseTypeName { get { return default(System.Xml.XmlQualifiedName); } set { } }
    public System.Xml.Schema.XmlSchemaObjectCollection Facets { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
  }
  public partial class XmlSchemaSimpleTypeUnion : System.Xml.Schema.XmlSchemaSimpleTypeContent {
    public XmlSchemaSimpleTypeUnion() { }
    public System.Xml.Schema.XmlSchemaSimpleType[] BaseMemberTypes { get { return default(System.Xml.Schema.XmlSchemaSimpleType[]); } }
    public System.Xml.Schema.XmlSchemaObjectCollection BaseTypes { get { return default(System.Xml.Schema.XmlSchemaObjectCollection); } }
    public System.Xml.XmlQualifiedName[] MemberTypes { get { return default(System.Xml.XmlQualifiedName[]); } set { } }
  }
  public partial class XmlSchemaTotalDigitsFacet : System.Xml.Schema.XmlSchemaNumericFacet {
    public XmlSchemaTotalDigitsFacet() { }
  }
  public partial class XmlSchemaType : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaType() { }
    [System.ObsoleteAttribute("This property has been deprecated. Please use BaseXmlSchemaType property that returns a strongly typed base schema type. http://go.microsoft.com/fwlink/?linkid=14202")]
    public object BaseSchemaType { get { return default(object); } }
    public System.Xml.Schema.XmlSchemaType BaseXmlSchemaType { get { return default(System.Xml.Schema.XmlSchemaType); } }
    public System.Xml.Schema.XmlSchemaDatatype Datatype { get { return default(System.Xml.Schema.XmlSchemaDatatype); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod DerivedBy { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
    public System.Xml.Schema.XmlSchemaDerivationMethod Final { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } set { } }
    public System.Xml.Schema.XmlSchemaDerivationMethod FinalResolved { get { return default(System.Xml.Schema.XmlSchemaDerivationMethod); } }
    public virtual bool IsMixed { get { return default(bool); } set { } }
    public string Name { get { return default(string); } set { } }
    public System.Xml.XmlQualifiedName QualifiedName { get { return default(System.Xml.XmlQualifiedName); } }
    public System.Xml.Schema.XmlTypeCode TypeCode { get { return default(System.Xml.Schema.XmlTypeCode); } }
    public static System.Xml.Schema.XmlSchemaComplexType GetBuiltInComplexType(System.Xml.Schema.XmlTypeCode typeCode) { return default(System.Xml.Schema.XmlSchemaComplexType); }
    public static System.Xml.Schema.XmlSchemaComplexType GetBuiltInComplexType(System.Xml.XmlQualifiedName qualifiedName) { return default(System.Xml.Schema.XmlSchemaComplexType); }
    public static System.Xml.Schema.XmlSchemaSimpleType GetBuiltInSimpleType(System.Xml.Schema.XmlTypeCode typeCode) { return default(System.Xml.Schema.XmlSchemaSimpleType); }
    public static System.Xml.Schema.XmlSchemaSimpleType GetBuiltInSimpleType(System.Xml.XmlQualifiedName qualifiedName) { return default(System.Xml.Schema.XmlSchemaSimpleType); }
    public static bool IsDerivedFrom(System.Xml.Schema.XmlSchemaType derivedType, System.Xml.Schema.XmlSchemaType baseType, System.Xml.Schema.XmlSchemaDerivationMethod except) { return default(bool); }
  }
  public partial class XmlSchemaUnique : System.Xml.Schema.XmlSchemaIdentityConstraint {
    public XmlSchemaUnique() { }
  }
  public enum XmlSchemaUse {
    None = 0,
    Optional = 1,
    Prohibited = 2,
    Required = 3,
  }
  public partial class XmlSchemaValidationException : System.Xml.Schema.XmlSchemaException {
    public XmlSchemaValidationException() { }
    public XmlSchemaValidationException(string message) { }
    public XmlSchemaValidationException(string message, System.Exception innerException) { }
    public XmlSchemaValidationException(string message, System.Exception innerException, int lineNumber, int linePosition) { }
    public object SourceObject { get { return default(object); } }
    protected internal void SetSourceObject(object sourceObject) { }
  }
  [System.FlagsAttribute]
  public enum XmlSchemaValidationFlags {
    AllowXmlAttributes = 16,
    None = 0,
    ProcessIdentityConstraints = 8,
    ProcessInlineSchema = 1,
    ProcessSchemaLocation = 2,
    ReportValidationWarnings = 4,
  }
  public sealed partial class XmlSchemaValidator {
    public XmlSchemaValidator(System.Xml.XmlNameTable nameTable, System.Xml.Schema.XmlSchemaSet schemas, System.Xml.IXmlNamespaceResolver namespaceResolver, System.Xml.Schema.XmlSchemaValidationFlags validationFlags) { }
    public System.Xml.IXmlLineInfo LineInfoProvider { get { return default(System.Xml.IXmlLineInfo); } set { } }
    public System.Uri SourceUri { get { return default(System.Uri); } set { } }
    public object ValidationEventSender { get { return default(object); } set { } }
    public System.Xml.XmlResolver XmlResolver { set { } }
    public event System.Xml.Schema.ValidationEventHandler ValidationEventHandler { add { } remove { } }
    public void AddSchema(System.Xml.Schema.XmlSchema schema) { }
    public void EndValidation() { }
    public System.Xml.Schema.XmlSchemaAttribute[] GetExpectedAttributes() { return default(System.Xml.Schema.XmlSchemaAttribute[]); }
    public System.Xml.Schema.XmlSchemaParticle[] GetExpectedParticles() { return default(System.Xml.Schema.XmlSchemaParticle[]); }
    public void GetUnspecifiedDefaultAttributes(System.Collections.ArrayList defaultAttributes) { }
    public void Initialize() { }
    public void Initialize(System.Xml.Schema.XmlSchemaObject partialValidationType) { }
    public void SkipToEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
    public object ValidateAttribute(string localName, string namespaceUri, string attributeValue, System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
    public object ValidateAttribute(string localName, string namespaceUri, System.Xml.Schema.XmlValueGetter attributeValue, System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
    public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
    public void ValidateElement(string localName, string namespaceUri, System.Xml.Schema.XmlSchemaInfo schemaInfo, string xsiType, string xsiNil, string xsiSchemaLocation, string xsiNoNamespaceSchemaLocation) { }
    public object ValidateEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo) { return default(object); }
    public object ValidateEndElement(System.Xml.Schema.XmlSchemaInfo schemaInfo, object typedValue) { return default(object); }
    public void ValidateEndOfAttributes(System.Xml.Schema.XmlSchemaInfo schemaInfo) { }
    public void ValidateText(string elementValue) { }
    public void ValidateText(System.Xml.Schema.XmlValueGetter elementValue) { }
    public void ValidateWhitespace(string elementValue) { }
    public void ValidateWhitespace(System.Xml.Schema.XmlValueGetter elementValue) { }
  }
  public enum XmlSchemaValidity {
    Invalid = 2,
    NotKnown = 0,
    Valid = 1,
  }
  public partial class XmlSchemaWhiteSpaceFacet : System.Xml.Schema.XmlSchemaFacet {
    public XmlSchemaWhiteSpaceFacet() { }
  }
  public partial class XmlSchemaXPath : System.Xml.Schema.XmlSchemaAnnotated {
    public XmlSchemaXPath() { }
    public string XPath { get { return default(string); } set { } }
  }
  public enum XmlSeverityType {
    Error = 0,
    Warning = 1,
  }
  public enum XmlTypeCode {
    AnyAtomicType = 10,
    AnyUri = 28,
    Attribute = 5,
    Base64Binary = 27,
    Boolean = 13,
    Byte = 46,
    Comment = 8,
    Date = 20,
    DateTime = 18,
    DayTimeDuration = 54,
    Decimal = 14,
    Document = 3,
    Double = 16,
    Duration = 17,
    Element = 4,
    Entity = 39,
    Float = 15,
    GDay = 24,
    GMonth = 25,
    GMonthDay = 23,
    GYear = 22,
    GYearMonth = 21,
    HexBinary = 26,
    Id = 37,
    Idref = 38,
    Int = 44,
    Integer = 40,
    Item = 1,
    Language = 33,
    Long = 43,
    Name = 35,
    Namespace = 6,
    NCName = 36,
    NegativeInteger = 42,
    NmToken = 34,
    Node = 2,
    None = 0,
    NonNegativeInteger = 47,
    NonPositiveInteger = 41,
    NormalizedString = 31,
    Notation = 30,
    PositiveInteger = 52,
    ProcessingInstruction = 7,
    QName = 29,
    Short = 45,
    String = 12,
    Text = 9,
    Time = 19,
    Token = 32,
    UnsignedByte = 51,
    UnsignedInt = 49,
    UnsignedLong = 48,
    UnsignedShort = 50,
    UntypedAtomic = 11,
    YearMonthDuration = 53,
  }
  public delegate object XmlValueGetter();
}
