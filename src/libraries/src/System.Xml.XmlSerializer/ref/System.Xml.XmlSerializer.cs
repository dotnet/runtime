// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Xml.Serialization
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = false)]
    public partial class XmlAnyAttributeAttribute : System.Attribute
    {
        public XmlAnyAttributeAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = true)]
    public partial class XmlAnyElementAttribute : System.Attribute
    {
        public XmlAnyElementAttribute() { }
        public XmlAnyElementAttribute(string name) { }
        public XmlAnyElementAttribute(string name, string ns) { }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    public partial class XmlAnyElementAttributes
    {
        public XmlAnyElementAttributes() { }
        public System.Xml.Serialization.XmlAnyElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlAnyElementAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlAnyElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlAnyElementAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlAnyElementAttribute attribute) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = false)]
    public partial class XmlArrayAttribute : System.Attribute
    {
        public XmlArrayAttribute() { }
        public XmlArrayAttribute(string elementName) { }
        public string ElementName { get { return default(string); } set { } }
        public System.Xml.Schema.XmlSchemaForm Form { get { return default(System.Xml.Schema.XmlSchemaForm); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = true)]
    public partial class XmlArrayItemAttribute : System.Attribute
    {
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
    public partial class XmlArrayItemAttributes
    {
        public XmlArrayItemAttributes() { }
        public System.Xml.Serialization.XmlArrayItemAttribute this[int index] { get { return default(System.Xml.Serialization.XmlArrayItemAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlArrayItemAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlArrayItemAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlArrayItemAttribute attribute) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlAttributeAttribute : System.Attribute
    {
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
    public partial class XmlAttributeOverrides
    {
        public XmlAttributeOverrides() { }
        public System.Xml.Serialization.XmlAttributes this[System.Type type] { get { return default(System.Xml.Serialization.XmlAttributes); } }
        public System.Xml.Serialization.XmlAttributes this[System.Type type, string member] { get { return default(System.Xml.Serialization.XmlAttributes); } }
        public void Add(System.Type type, string member, System.Xml.Serialization.XmlAttributes attributes) { }
        public void Add(System.Type type, System.Xml.Serialization.XmlAttributes attributes) { }
    }
    public partial class XmlAttributes
    {
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
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = false)]
    public partial class XmlChoiceIdentifierAttribute : System.Attribute
    {
        public XmlChoiceIdentifierAttribute() { }
        public XmlChoiceIdentifierAttribute(string name) { }
        public string MemberName { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = true)]
    public partial class XmlElementAttribute : System.Attribute
    {
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
    public partial class XmlElementAttributes
    {
        public XmlElementAttributes() { }
        public System.Xml.Serialization.XmlElementAttribute this[int index] { get { return default(System.Xml.Serialization.XmlElementAttribute); } set { } }
        public int Add(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
        public bool Contains(System.Xml.Serialization.XmlElementAttribute attribute) { return default(bool); }
        public void CopyTo(System.Xml.Serialization.XmlElementAttribute[] array, int index) { }
        public int IndexOf(System.Xml.Serialization.XmlElementAttribute attribute) { return default(int); }
        public void Insert(int index, System.Xml.Serialization.XmlElementAttribute attribute) { }
        public void Remove(System.Xml.Serialization.XmlElementAttribute attribute) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256))]
    public partial class XmlEnumAttribute : System.Attribute
    {
        public XmlEnumAttribute() { }
        public XmlEnumAttribute(string name) { }
        public string Name { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlIgnoreAttribute : System.Attribute
    {
        public XmlIgnoreAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1100), AllowMultiple = true)]
    public partial class XmlIncludeAttribute : System.Attribute
    {
        public XmlIncludeAttribute(System.Type type) { }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624), AllowMultiple = false)]
    public partial class XmlNamespaceDeclarationsAttribute : System.Attribute
    {
        public XmlNamespaceDeclarationsAttribute() { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(9244))]
    public partial class XmlRootAttribute : System.Attribute
    {
        public XmlRootAttribute() { }
        public XmlRootAttribute(string elementName) { }
        public string DataType { get { return default(string); } set { } }
        public string ElementName { get { return default(string); } set { } }
        public bool IsNullable { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    public partial class XmlSerializer
    {
        protected XmlSerializer() { }
        public XmlSerializer(System.Type type) { }
        public XmlSerializer(System.Type type, string defaultNamespace) { }
        public XmlSerializer(System.Type type, System.Type[] extraTypes) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlAttributeOverrides overrides, System.Type[] extraTypes, System.Xml.Serialization.XmlRootAttribute root, string defaultNamespace) { }
        public XmlSerializer(System.Type type, System.Xml.Serialization.XmlRootAttribute root) { }
        public virtual bool CanDeserialize(System.Xml.XmlReader xmlReader) { return default(bool); }
        public object Deserialize(System.IO.Stream stream) { return default(object); }
        public object Deserialize(System.IO.TextReader textReader) { return default(object); }
        public object Deserialize(System.Xml.XmlReader xmlReader) { return default(object); }
        public static System.Xml.Serialization.XmlSerializer[] FromTypes(System.Type[] types) { return default(System.Xml.Serialization.XmlSerializer[]); }
        public void Serialize(System.IO.Stream stream, object o) { }
        public void Serialize(System.IO.Stream stream, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public void Serialize(System.IO.TextWriter textWriter, object o) { }
        public void Serialize(System.IO.TextWriter textWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o) { }
        public void Serialize(System.Xml.XmlWriter xmlWriter, object o, System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
    }
    public partial class XmlSerializerNamespaces
    {
        public XmlSerializerNamespaces() { }
        public XmlSerializerNamespaces(System.Xml.Serialization.XmlSerializerNamespaces namespaces) { }
        public XmlSerializerNamespaces(System.Xml.XmlQualifiedName[] namespaces) { }
        public int Count { get { return default(int); } }
        public void Add(string prefix, string ns) { }
        public System.Xml.XmlQualifiedName[] ToArray() { return default(System.Xml.XmlQualifiedName[]); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(10624))]
    public partial class XmlTextAttribute : System.Attribute
    {
        public XmlTextAttribute() { }
        public XmlTextAttribute(System.Type type) { }
        public string DataType { get { return default(string); } set { } }
        public System.Type Type { get { return default(System.Type); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(1052))]
    public partial class XmlTypeAttribute : System.Attribute
    {
        public XmlTypeAttribute() { }
        public XmlTypeAttribute(string typeName) { }
        public bool AnonymousType { get { return default(bool); } set { } }
        public bool IncludeInSchema { get { return default(bool); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public string TypeName { get { return default(string); } set { } }
    }
}
