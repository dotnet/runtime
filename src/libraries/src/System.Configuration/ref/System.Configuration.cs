// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#pragma warning disable CS0618
using System.Collections;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Configuration.Internal;
using System.Configuration.Provider;
using System.Runtime.Serialization;
using System.Xml;

namespace System.Configuration
{
    public sealed partial class AppSettingsSection : System.Configuration.ConfigurationSection
    {
        public AppSettingsSection() { }
        [System.Configuration.ConfigurationPropertyAttribute("file", DefaultValue = "")]
        public string File { get { throw null; } set { } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("", IsDefaultCollection = true)]
        public System.Configuration.KeyValueConfigurationCollection Settings { get { throw null; } }
        protected internal override void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey) { }
        protected internal override object GetRuntimeObject() { throw null; }
        protected internal override bool IsModified() { throw null; }
        protected internal override void Reset(System.Configuration.ConfigurationElement parentSection) { }
        protected internal override string SerializeSection(System.Configuration.ConfigurationElement parentElement, string name, System.Configuration.ConfigurationSaveMode saveMode) { throw null; }
    }
    public sealed partial class CallbackValidator : System.Configuration.ConfigurationValidatorBase
    {
        public CallbackValidator(System.Type type, System.Configuration.ValidatorCallback callback) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class CallbackValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public CallbackValidatorAttribute() { }
        public string CallbackMethodName { get { throw null; } set { } }
        public System.Type Type { get { throw null; } set { } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public sealed partial class CommaDelimitedStringCollection : System.Collections.Specialized.StringCollection
    {
        public CommaDelimitedStringCollection() { }
        public bool IsModified { get { throw null; } }
        public new bool IsReadOnly { get { throw null; } }
        public new string this[int index] { get { throw null; } set { } }
        public new void Add(string value) { }
        public new void AddRange(string[] range) { }
        public new void Clear() { }
        public System.Configuration.CommaDelimitedStringCollection Clone() { throw null; }
        public new void Insert(int index, string value) { }
        public new void Remove(string value) { }
        public void SetReadOnly() { }
        public override string ToString() { throw null; }
    }
    public sealed partial class CommaDelimitedStringCollectionConverter : System.Configuration.ConfigurationConverterBase
    {
        public CommaDelimitedStringCollectionConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed partial class Configuration
    {
        internal Configuration() { }
        public System.Configuration.AppSettingsSection AppSettings { get { throw null; } }
        public System.Func<string, string> AssemblyStringTransformer { get { throw null; } set { } }
        public System.Configuration.ConnectionStringsSection ConnectionStrings { get { throw null; } }
        public System.Configuration.ContextInformation EvaluationContext { get { throw null; } }
        public string FilePath { get { throw null; } }
        public bool HasFile { get { throw null; } }
        public System.Configuration.ConfigurationLocationCollection Locations { get { throw null; } }
        public bool NamespaceDeclared { get { throw null; } set { } }
        public System.Configuration.ConfigurationSectionGroup RootSectionGroup { get { throw null; } }
        public System.Configuration.ConfigurationSectionGroupCollection SectionGroups { get { throw null; } }
        public System.Configuration.ConfigurationSectionCollection Sections { get { throw null; } }
        public System.Runtime.Versioning.FrameworkName TargetFramework { get { throw null; } set { } }
        public System.Func<string, string> TypeStringTransformer { get { throw null; } set { } }
        public System.Configuration.ConfigurationSection GetSection(string sectionName) { throw null; }
        public System.Configuration.ConfigurationSectionGroup GetSectionGroup(string sectionGroupName) { throw null; }
        public void Save() { }
        public void Save(System.Configuration.ConfigurationSaveMode saveMode) { }
        public void Save(System.Configuration.ConfigurationSaveMode saveMode, bool forceSaveAll) { }
        public void SaveAs(string filename) { }
        public void SaveAs(string filename, System.Configuration.ConfigurationSaveMode saveMode) { }
        public void SaveAs(string filename, System.Configuration.ConfigurationSaveMode saveMode, bool forceSaveAll) { }
    }
    public enum ConfigurationAllowDefinition
    {
        Everywhere = 300,
        MachineOnly = 0,
        MachineToApplication = 200,
        MachineToWebRoot = 100,
    }
    public enum ConfigurationAllowExeDefinition
    {
        MachineOnly = 0,
        MachineToApplication = 100,
        MachineToLocalUser = 300,
        MachineToRoamingUser = 200,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(132))]
    public sealed partial class ConfigurationCollectionAttribute : System.Attribute
    {
        public ConfigurationCollectionAttribute(System.Type itemType) { }
        public string AddItemName { get { throw null; } set { } }
        public string ClearItemsName { get { throw null; } set { } }
        public System.Configuration.ConfigurationElementCollectionType CollectionType { get { throw null; } set { } }
        public System.Type ItemType { get { throw null; } }
        public string RemoveItemName { get { throw null; } set { } }
    }
    public abstract partial class ConfigurationConverterBase : System.ComponentModel.TypeConverter
    {
        protected ConfigurationConverterBase() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Type type) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Type type) { throw null; }
    }
    public abstract partial class ConfigurationElement
    {
        protected ConfigurationElement() { }
        public System.Configuration.Configuration CurrentConfiguration { get { throw null; } }
        public System.Configuration.ElementInformation ElementInformation { get { throw null; } }
        protected internal virtual System.Configuration.ConfigurationElementProperty ElementProperty { get { throw null; } }
        protected System.Configuration.ContextInformation EvaluationContext { get { throw null; } }
        protected bool HasContext { get { throw null; } }
        protected internal object this[System.Configuration.ConfigurationProperty prop] { get { throw null; } set { } }
        protected internal object this[string propertyName] { get { throw null; } set { } }
        public System.Configuration.ConfigurationLockCollection LockAllAttributesExcept { get { throw null; } }
        public System.Configuration.ConfigurationLockCollection LockAllElementsExcept { get { throw null; } }
        public System.Configuration.ConfigurationLockCollection LockAttributes { get { throw null; } }
        public System.Configuration.ConfigurationLockCollection LockElements { get { throw null; } }
        public bool LockItem { get { throw null; } set { } }
        protected internal virtual System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        protected internal virtual void DeserializeElement(System.Xml.XmlReader reader, bool serializeCollectionKey) { }
        public override bool Equals(object compareTo) { throw null; }
        public override int GetHashCode() { throw null; }
        protected virtual string GetTransformedAssemblyString(string assemblyName) { throw null; }
        protected virtual string GetTransformedTypeString(string typeName) { throw null; }
        protected internal virtual void Init() { }
        protected internal virtual void InitializeDefault() { }
        protected internal virtual bool IsModified() { throw null; }
        public virtual bool IsReadOnly() { throw null; }
        protected virtual void ListErrors(System.Collections.IList errorList) { }
        protected virtual bool OnDeserializeUnrecognizedAttribute(string name, string value) { throw null; }
        protected virtual bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader) { throw null; }
        protected virtual object OnRequiredPropertyNotFound(string name) { throw null; }
        protected virtual void PostDeserialize() { }
        protected virtual void PreSerialize(System.Xml.XmlWriter writer) { }
        protected internal virtual void Reset(System.Configuration.ConfigurationElement parentElement) { }
        protected internal virtual void ResetModified() { }
        protected internal virtual bool SerializeElement(System.Xml.XmlWriter writer, bool serializeCollectionKey) { throw null; }
        protected internal virtual bool SerializeToXmlElement(System.Xml.XmlWriter writer, string elementName) { throw null; }
        protected void SetPropertyValue(System.Configuration.ConfigurationProperty prop, object value, bool ignoreLocks) { }
        protected internal virtual void SetReadOnly() { }
        protected internal virtual void Unmerge(System.Configuration.ConfigurationElement sourceElement, System.Configuration.ConfigurationElement parentElement, System.Configuration.ConfigurationSaveMode saveMode) { }
    }
    [System.Diagnostics.DebuggerDisplayAttribute("Count = {Count}")]
    public abstract partial class ConfigurationElementCollection : System.Configuration.ConfigurationElement, System.Collections.ICollection, System.Collections.IEnumerable
    {
        protected ConfigurationElementCollection() { }
        protected ConfigurationElementCollection(System.Collections.IComparer comparer) { }
        protected internal string AddElementName { get { throw null; } set { } }
        protected internal string ClearElementName { get { throw null; } set { } }
        public virtual System.Configuration.ConfigurationElementCollectionType CollectionType { get { throw null; } }
        public int Count { get { throw null; } }
        protected virtual string ElementName { get { throw null; } }
        public bool EmitClear { get { throw null; } set { } }
        public bool IsSynchronized { get { throw null; } }
        protected internal string RemoveElementName { get { throw null; } set { } }
        public object SyncRoot { get { throw null; } }
        protected virtual bool ThrowOnDuplicate { get { throw null; } }
        protected virtual void BaseAdd(System.Configuration.ConfigurationElement element) { }
        protected internal void BaseAdd(System.Configuration.ConfigurationElement element, bool throwIfExists) { }
        protected virtual void BaseAdd(int index, System.Configuration.ConfigurationElement element) { }
        protected internal void BaseClear() { }
        protected internal System.Configuration.ConfigurationElement BaseGet(int index) { throw null; }
        protected internal System.Configuration.ConfigurationElement BaseGet(object key) { throw null; }
        protected internal object[] BaseGetAllKeys() { throw null; }
        protected internal object BaseGetKey(int index) { throw null; }
        protected int BaseIndexOf(System.Configuration.ConfigurationElement element) { throw null; }
        protected internal bool BaseIsRemoved(object key) { throw null; }
        protected internal void BaseRemove(object key) { }
        protected internal void BaseRemoveAt(int index) { }
        public void CopyTo(System.Configuration.ConfigurationElement[] array, int index) { }
        protected abstract System.Configuration.ConfigurationElement CreateNewElement();
        protected virtual System.Configuration.ConfigurationElement CreateNewElement(string elementName) { throw null; }
        public override bool Equals(object compareTo) { throw null; }
        protected abstract object GetElementKey(System.Configuration.ConfigurationElement element);
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override int GetHashCode() { throw null; }
        protected virtual bool IsElementName(string elementName) { throw null; }
        protected virtual bool IsElementRemovable(System.Configuration.ConfigurationElement element) { throw null; }
        protected internal override bool IsModified() { throw null; }
        public override bool IsReadOnly() { throw null; }
        protected override bool OnDeserializeUnrecognizedElement(string elementName, System.Xml.XmlReader reader) { throw null; }
        protected internal override void Reset(System.Configuration.ConfigurationElement parentElement) { }
        protected internal override void ResetModified() { }
        protected internal override bool SerializeElement(System.Xml.XmlWriter writer, bool serializeCollectionKey) { throw null; }
        protected internal override void SetReadOnly() { }
        void System.Collections.ICollection.CopyTo(System.Array arr, int index) { }
        protected internal override void Unmerge(System.Configuration.ConfigurationElement sourceElement, System.Configuration.ConfigurationElement parentElement, System.Configuration.ConfigurationSaveMode saveMode) { }
    }
    public enum ConfigurationElementCollectionType
    {
        AddRemoveClearMap = 1,
        AddRemoveClearMapAlternate = 3,
        BasicMap = 0,
        BasicMapAlternate = 2,
    }
    public sealed partial class ConfigurationElementProperty
    {
        public ConfigurationElementProperty(System.Configuration.ConfigurationValidatorBase validator) { }
        public System.Configuration.ConfigurationValidatorBase Validator { get { throw null; } }
    }
    public partial class ConfigurationException : System.SystemException
    {
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException() { }
        protected ConfigurationException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message, System.Exception inner) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message, System.Exception inner, string filename, int line) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message, System.Exception inner, System.Xml.XmlNode node) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message, string filename, int line) { }
        [System.ObsoleteAttribute("This class is obsolete, to create a new exception create a System.Configuration!System.Configuration.ConfigurationErrorsException")]
        public ConfigurationException(string message, System.Xml.XmlNode node) { }
        public virtual string BareMessage { get { throw null; } }
        public virtual string Filename { get { throw null; } }
        public virtual int Line { get { throw null; } }
        public override string Message { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        [System.ObsoleteAttribute("This class is obsolete, use System.Configuration!System.Configuration.ConfigurationErrorsException.GetFilename instead")]
        public static string GetXmlNodeFilename(System.Xml.XmlNode node) { throw null; }
        [System.ObsoleteAttribute("This class is obsolete, use System.Configuration!System.Configuration.ConfigurationErrorsException.GetLinenumber instead")]
        public static int GetXmlNodeLineNumber(System.Xml.XmlNode node) { throw null; }
    }
    public partial class ConfigurationErrorsException : System.Configuration.ConfigurationException
    {
        public ConfigurationErrorsException() { }
        protected ConfigurationErrorsException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ConfigurationErrorsException(string message) { }
        public ConfigurationErrorsException(string message, System.Exception inner) { }
        public ConfigurationErrorsException(string message, System.Exception inner, string filename, int line) { }
        public ConfigurationErrorsException(string message, System.Exception inner, System.Xml.XmlNode node) { }
        public ConfigurationErrorsException(string message, System.Exception inner, System.Xml.XmlReader reader) { }
        public ConfigurationErrorsException(string message, string filename, int line) { }
        public ConfigurationErrorsException(string message, System.Xml.XmlNode node) { }
        public ConfigurationErrorsException(string message, System.Xml.XmlReader reader) { }
        public override string BareMessage { get { throw null; } }
        public System.Collections.ICollection Errors { get { throw null; } }
        public override string Filename { get { throw null; } }
        public override int Line { get { throw null; } }
        public override string Message { get { throw null; } }
        public static string GetFilename(System.Xml.XmlNode node) { throw null; }
        public static string GetFilename(System.Xml.XmlReader reader) { throw null; }
        public static int GetLineNumber(System.Xml.XmlNode node) { throw null; }
        public static int GetLineNumber(System.Xml.XmlReader reader) { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class ConfigurationFileMap : System.ICloneable
    {
        public ConfigurationFileMap() { }
        public ConfigurationFileMap(string machineConfigFilename) { }
        public string MachineConfigFilename { get { throw null; } set { } }
        public virtual object Clone() { throw null; }
    }
    public partial class ConfigurationLocation
    {
        internal ConfigurationLocation() { }
        public string Path { get { throw null; } }
        public System.Configuration.Configuration OpenConfiguration() { throw null; }
    }
    public partial class ConfigurationLocationCollection : System.Collections.ReadOnlyCollectionBase
    {
        internal ConfigurationLocationCollection() { }
        public System.Configuration.ConfigurationLocation this[int index] { get { throw null; } }
    }
    public sealed partial class ConfigurationLockCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        internal ConfigurationLockCollection() { }
        public string AttributeList { get { throw null; } }
        public int Count { get { throw null; } }
        public bool HasParentElements { get { throw null; } }
        public bool IsModified { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void Add(string name) { }
        public void Clear() { }
        public bool Contains(string name) { throw null; }
        public void CopyTo(string[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public bool IsReadOnly(string name) { throw null; }
        public void Remove(string name) { }
        public void SetFromList(string attributeList) { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public static partial class ConfigurationManager
    {
        public static System.Collections.Specialized.NameValueCollection AppSettings { get { throw null; } }
        public static System.Configuration.ConnectionStringSettingsCollection ConnectionStrings { get { throw null; } }
        public static object GetSection(string sectionName) { throw null; }
        public static System.Configuration.Configuration OpenExeConfiguration(System.Configuration.ConfigurationUserLevel userLevel) { throw null; }
        public static System.Configuration.Configuration OpenExeConfiguration(string exePath) { throw null; }
        public static System.Configuration.Configuration OpenMachineConfiguration() { throw null; }
        public static System.Configuration.Configuration OpenMappedExeConfiguration(System.Configuration.ExeConfigurationFileMap fileMap, System.Configuration.ConfigurationUserLevel userLevel) { throw null; }
        public static System.Configuration.Configuration OpenMappedExeConfiguration(System.Configuration.ExeConfigurationFileMap fileMap, System.Configuration.ConfigurationUserLevel userLevel, bool preLoad) { throw null; }
        public static System.Configuration.Configuration OpenMappedMachineConfiguration(System.Configuration.ConfigurationFileMap fileMap) { throw null; }
        public static void RefreshSection(string sectionName) { }
    }

    public sealed partial class ConfigurationProperty
    {
        public ConfigurationProperty(string name, System.Type type) { }
        public ConfigurationProperty(string name, System.Type type, object defaultValue) { }
        public ConfigurationProperty(string name, System.Type type, object defaultValue, System.ComponentModel.TypeConverter typeConverter, System.Configuration.ConfigurationValidatorBase validator, System.Configuration.ConfigurationPropertyOptions options) { }
        public ConfigurationProperty(string name, System.Type type, object defaultValue, System.ComponentModel.TypeConverter typeConverter, System.Configuration.ConfigurationValidatorBase validator, System.Configuration.ConfigurationPropertyOptions options, string description) { }
        public ConfigurationProperty(string name, System.Type type, object defaultValue, System.Configuration.ConfigurationPropertyOptions options) { }
        public System.ComponentModel.TypeConverter Converter { get { throw null; } }
        public object DefaultValue { get { throw null; } }
        public string Description { get { throw null; } }
        public bool IsAssemblyStringTransformationRequired { get { throw null; } }
        public bool IsDefaultCollection { get { throw null; } }
        public bool IsKey { get { throw null; } }
        public bool IsRequired { get { throw null; } }
        public bool IsTypeStringTransformationRequired { get { throw null; } }
        public bool IsVersionCheckRequired { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public System.Configuration.ConfigurationValidatorBase Validator { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class ConfigurationPropertyAttribute : System.Attribute
    {
        public ConfigurationPropertyAttribute(string name) { }
        public object DefaultValue { get { throw null; } set { } }
        public bool IsDefaultCollection { get { throw null; } set { } }
        public bool IsKey { get { throw null; } set { } }
        public bool IsRequired { get { throw null; } set { } }
        public string Name { get { throw null; } }
        public System.Configuration.ConfigurationPropertyOptions Options { get { throw null; } set { } }
    }
    public partial class ConfigurationPropertyCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ConfigurationPropertyCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Configuration.ConfigurationProperty this[string name] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public void Add(System.Configuration.ConfigurationProperty property) { }
        public void Clear() { }
        public bool Contains(string name) { throw null; }
        public void CopyTo(System.Configuration.ConfigurationProperty[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public bool Remove(string name) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    [System.FlagsAttribute]
    public enum ConfigurationPropertyOptions
    {
        IsAssemblyStringTransformationRequired = 16,
        IsDefaultCollection = 1,
        IsKey = 4,
        IsRequired = 2,
        IsTypeStringTransformationRequired = 8,
        IsVersionCheckRequired = 32,
        None = 0,
    }
    public enum ConfigurationSaveMode
    {
        Full = 2,
        Minimal = 1,
        Modified = 0,
    }
    public abstract partial class ConfigurationSection : System.Configuration.ConfigurationElement
    {
        protected ConfigurationSection() { }
        public System.Configuration.SectionInformation SectionInformation { get { throw null; } }
        protected internal virtual void DeserializeSection(System.Xml.XmlReader reader) { }
        protected internal virtual object GetRuntimeObject() { throw null; }
        protected internal override bool IsModified() { throw null; }
        protected internal override void ResetModified() { }
        protected internal virtual string SerializeSection(System.Configuration.ConfigurationElement parentElement, string name, System.Configuration.ConfigurationSaveMode saveMode) { throw null; }
        protected internal virtual bool ShouldSerializeElementInTargetVersion(System.Configuration.ConfigurationElement element, string elementName, System.Runtime.Versioning.FrameworkName targetFramework) { throw null; }
        protected internal virtual bool ShouldSerializePropertyInTargetVersion(System.Configuration.ConfigurationProperty property, string propertyName, System.Runtime.Versioning.FrameworkName targetFramework, System.Configuration.ConfigurationElement parentConfigurationElement) { throw null; }
        protected internal virtual bool ShouldSerializeSectionInTargetVersion(System.Runtime.Versioning.FrameworkName targetFramework) { throw null; }
    }
    public sealed partial class ConfigurationSectionCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        internal ConfigurationSectionCollection() { }
        public override int Count { get { throw null; } }
        public System.Configuration.ConfigurationSection this[int index] { get { throw null; } }
        public System.Configuration.ConfigurationSection this[string name] { get { throw null; } }
        public override System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys { get { throw null; } }
        public void Add(string name, System.Configuration.ConfigurationSection section) { }
        public void Clear() { }
        public void CopyTo(System.Configuration.ConfigurationSection[] array, int index) { }
        public System.Configuration.ConfigurationSection Get(int index) { throw null; }
        public System.Configuration.ConfigurationSection Get(string name) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public string GetKey(int index) { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public partial class ConfigurationSectionGroup
    {
        public ConfigurationSectionGroup() { }
        public bool IsDeclarationRequired { get { throw null; } }
        public bool IsDeclared { get { throw null; } }
        public string Name { get { throw null; } }
        public string SectionGroupName { get { throw null; } }
        public System.Configuration.ConfigurationSectionGroupCollection SectionGroups { get { throw null; } }
        public System.Configuration.ConfigurationSectionCollection Sections { get { throw null; } }
        public string Type { get { throw null; } set { } }
        public void ForceDeclaration() { }
        public void ForceDeclaration(bool force) { }
        protected internal virtual bool ShouldSerializeSectionGroupInTargetVersion(System.Runtime.Versioning.FrameworkName targetFramework) { throw null; }
    }
    public sealed partial class ConfigurationSectionGroupCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        internal ConfigurationSectionGroupCollection() { }
        public override int Count { get { throw null; } }
        public System.Configuration.ConfigurationSectionGroup this[int index] { get { throw null; } }
        public System.Configuration.ConfigurationSectionGroup this[string name] { get { throw null; } }
        public override System.Collections.Specialized.NameObjectCollectionBase.KeysCollection Keys { get { throw null; } }
        public void Add(string name, System.Configuration.ConfigurationSectionGroup sectionGroup) { }
        public void Clear() { }
        public void CopyTo(System.Configuration.ConfigurationSectionGroup[] array, int index) { }
        public System.Configuration.ConfigurationSectionGroup Get(int index) { throw null; }
        public System.Configuration.ConfigurationSectionGroup Get(string name) { throw null; }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public string GetKey(int index) { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public enum ConfigurationUserLevel
    {
        None = 0,
        PerUserRoaming = 10,
        PerUserRoamingAndLocal = 20,
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public partial class ConfigurationValidatorAttribute : System.Attribute
    {
        protected ConfigurationValidatorAttribute() { }
        public ConfigurationValidatorAttribute(System.Type validator) { }
        public virtual System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
        public System.Type ValidatorType { get { throw null; } }
    }
    public abstract partial class ConfigurationValidatorBase
    {
        protected ConfigurationValidatorBase() { }
        public virtual bool CanValidate(System.Type type) { throw null; }
        public abstract void Validate(object value);
    }
    public sealed partial class ConnectionStringSettings : System.Configuration.ConfigurationElement
    {
        public ConnectionStringSettings() { }
        public ConnectionStringSettings(string name, string connectionString) { }
        public ConnectionStringSettings(string name, string connectionString, string providerName) { }
        [System.Configuration.ConfigurationPropertyAttribute("connectionString", Options = (System.Configuration.ConfigurationPropertyOptions)(2), DefaultValue = "")]
        public string ConnectionString { get { throw null; } set { } }
        [System.Configuration.ConfigurationPropertyAttribute("name", Options = (System.Configuration.ConfigurationPropertyOptions)(6), DefaultValue = "")]
        public string Name { get { throw null; } set { } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("providerName", DefaultValue = "System.Data.SqlClient")]
        public string ProviderName { get { throw null; } set { } }
        public override string ToString() { throw null; }
    }
    [System.Configuration.ConfigurationCollectionAttribute(typeof(System.Configuration.ConnectionStringSettings))]
    public sealed partial class ConnectionStringSettingsCollection : System.Configuration.ConfigurationElementCollection
    {
        public ConnectionStringSettingsCollection() { }
        public System.Configuration.ConnectionStringSettings this[int index] { get { throw null; } set { } }
        public new System.Configuration.ConnectionStringSettings this[string name] { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        public void Add(System.Configuration.ConnectionStringSettings settings) { }
        protected override void BaseAdd(int index, System.Configuration.ConfigurationElement element) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { throw null; }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { throw null; }
        public int IndexOf(System.Configuration.ConnectionStringSettings settings) { throw null; }
        public void Remove(System.Configuration.ConnectionStringSettings settings) { }
        public void Remove(string name) { }
        public void RemoveAt(int index) { }
    }
    public sealed partial class ConnectionStringsSection : System.Configuration.ConfigurationSection
    {
        public ConnectionStringsSection() { }
        [System.Configuration.ConfigurationPropertyAttribute("", Options = (System.Configuration.ConfigurationPropertyOptions)(1))]
        public System.Configuration.ConnectionStringSettingsCollection ConnectionStrings { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        protected internal override object GetRuntimeObject() { throw null; }
    }
    public sealed partial class ContextInformation
    {
        internal ContextInformation() { }
        public object HostingContext { get { throw null; } }
        public bool IsMachineLevel { get { throw null; } }
        public object GetSection(string sectionName) { throw null; }
    }
    public sealed partial class DefaultSection : System.Configuration.ConfigurationSection
    {
        public DefaultSection() { }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        protected internal override void DeserializeSection(System.Xml.XmlReader xmlReader) { }
        protected internal override bool IsModified() { throw null; }
        protected internal override void Reset(System.Configuration.ConfigurationElement parentSection) { }
        protected internal override void ResetModified() { }
        protected internal override string SerializeSection(System.Configuration.ConfigurationElement parentSection, string name, System.Configuration.ConfigurationSaveMode saveMode) { throw null; }
    }
    public sealed partial class DefaultValidator : System.Configuration.ConfigurationValidatorBase
    {
        public DefaultValidator() { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    public sealed partial class ElementInformation
    {
        internal ElementInformation() { }
        public System.Collections.ICollection Errors { get { throw null; } }
        public bool IsCollection { get { throw null; } }
        public bool IsLocked { get { throw null; } }
        public bool IsPresent { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public System.Configuration.PropertyInformationCollection Properties { get { throw null; } }
        public string Source { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public System.Configuration.ConfigurationValidatorBase Validator { get { throw null; } }
    }
    public sealed partial class ExeConfigurationFileMap : System.Configuration.ConfigurationFileMap
    {
        public ExeConfigurationFileMap() { }
        public ExeConfigurationFileMap(string machineConfigFileName) { }
        public string ExeConfigFilename { get { throw null; } set { } }
        public string LocalUserConfigFilename { get { throw null; } set { } }
        public string RoamingUserConfigFilename { get { throw null; } set { } }
        public override object Clone() { throw null; }
    }
    public sealed partial class ExeContext
    {
        internal ExeContext() { }
        public string ExePath { get { throw null; } }
        public System.Configuration.ConfigurationUserLevel UserLevel { get { throw null; } }
    }
    public sealed partial class GenericEnumConverter : System.Configuration.ConfigurationConverterBase
    {
        public GenericEnumConverter(System.Type typeEnum) { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed partial class IgnoreSection : System.Configuration.ConfigurationSection
    {
        public IgnoreSection() { }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        protected internal override void DeserializeSection(System.Xml.XmlReader xmlReader) { }
        protected internal override bool IsModified() { throw null; }
        protected internal override void Reset(System.Configuration.ConfigurationElement parentSection) { }
        protected internal override void ResetModified() { }
        protected internal override string SerializeSection(System.Configuration.ConfigurationElement parentSection, string name, System.Configuration.ConfigurationSaveMode saveMode) { throw null; }
    }
    public sealed partial class InfiniteIntConverter : System.Configuration.ConfigurationConverterBase
    {
        public InfiniteIntConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed partial class InfiniteTimeSpanConverter : System.Configuration.ConfigurationConverterBase
    {
        public InfiniteTimeSpanConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public partial class IntegerValidator : System.Configuration.ConfigurationValidatorBase
    {
        public IntegerValidator(int minValue, int maxValue) { }
        public IntegerValidator(int minValue, int maxValue, bool rangeIsExclusive) { }
        public IntegerValidator(int minValue, int maxValue, bool rangeIsExclusive, int resolution) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class IntegerValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public IntegerValidatorAttribute() { }
        public bool ExcludeRange { get { throw null; } set { } }
        public int MaxValue { get { throw null; } set { } }
        public int MinValue { get { throw null; } set { } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    [System.Configuration.ConfigurationCollectionAttribute(typeof(System.Configuration.KeyValueConfigurationElement))]
    public partial class KeyValueConfigurationCollection : System.Configuration.ConfigurationElementCollection
    {
        public KeyValueConfigurationCollection() { }
        public string[] AllKeys { get { throw null; } }
        public new System.Configuration.KeyValueConfigurationElement this[string key] { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        protected override bool ThrowOnDuplicate { get { throw null; } }
        public void Add(System.Configuration.KeyValueConfigurationElement keyValue) { }
        public void Add(string key, string value) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { throw null; }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { throw null; }
        public void Remove(string key) { }
    }
    public partial class KeyValueConfigurationElement : System.Configuration.ConfigurationElement
    {
        public KeyValueConfigurationElement(string key, string value) { }
        [System.Configuration.ConfigurationPropertyAttribute("key", Options = (System.Configuration.ConfigurationPropertyOptions)(4), DefaultValue = "")]
        public string Key { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("value", DefaultValue = "")]
        public string Value { get { throw null; } set { } }
        protected internal override void Init() { }
    }
    public partial class LongValidator : System.Configuration.ConfigurationValidatorBase
    {
        public LongValidator(long minValue, long maxValue) { }
        public LongValidator(long minValue, long maxValue, bool rangeIsExclusive) { }
        public LongValidator(long minValue, long maxValue, bool rangeIsExclusive, long resolution) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class LongValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public LongValidatorAttribute() { }
        public bool ExcludeRange { get { throw null; } set { } }
        public long MaxValue { get { throw null; } set { } }
        public long MinValue { get { throw null; } set { } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    [System.Configuration.ConfigurationCollectionAttribute(typeof(System.Configuration.NameValueConfigurationElement))]
    public sealed partial class NameValueConfigurationCollection : System.Configuration.ConfigurationElementCollection
    {
        public NameValueConfigurationCollection() { }
        public string[] AllKeys { get { throw null; } }
        public new System.Configuration.NameValueConfigurationElement this[string name] { get { throw null; } set { } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        public void Add(System.Configuration.NameValueConfigurationElement nameValue) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { throw null; }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { throw null; }
        public void Remove(System.Configuration.NameValueConfigurationElement nameValue) { }
        public void Remove(string name) { }
    }
    public sealed partial class NameValueConfigurationElement : System.Configuration.ConfigurationElement
    {
        public NameValueConfigurationElement(string name, string value) { }
        [System.Configuration.ConfigurationPropertyAttribute("name", IsKey = true, DefaultValue = "")]
        public string Name { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("value", DefaultValue = "")]
        public string Value { get { throw null; } set { } }
    }
    public enum OverrideMode
    {
        Allow = 1,
        Deny = 2,
        Inherit = 0,
    }
    public partial class PositiveTimeSpanValidator : System.Configuration.ConfigurationValidatorBase
    {
        public PositiveTimeSpanValidator() { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class PositiveTimeSpanValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public PositiveTimeSpanValidatorAttribute() { }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public sealed partial class PropertyInformation
    {
        internal PropertyInformation() { }
        public System.ComponentModel.TypeConverter Converter { get { throw null; } }
        public object DefaultValue { get { throw null; } }
        public string Description { get { throw null; } }
        public bool IsKey { get { throw null; } }
        public bool IsLocked { get { throw null; } }
        public bool IsModified { get { throw null; } }
        public bool IsRequired { get { throw null; } }
        public int LineNumber { get { throw null; } }
        public string Name { get { throw null; } }
        public string Source { get { throw null; } }
        public System.Type Type { get { throw null; } }
        public System.Configuration.ConfigurationValidatorBase Validator { get { throw null; } }
        public object Value { get { throw null; } set { } }
        public System.Configuration.PropertyValueOrigin ValueOrigin { get { throw null; } }
    }
    public sealed partial class PropertyInformationCollection : System.Collections.Specialized.NameObjectCollectionBase
    {
        internal PropertyInformationCollection() { }
        public System.Configuration.PropertyInformation this[string propertyName] { get { throw null; } }
        public void CopyTo(System.Configuration.PropertyInformation[] array, int index) { }
        public override System.Collections.IEnumerator GetEnumerator() { throw null; }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public enum PropertyValueOrigin
    {
        Default = 0,
        Inherited = 1,
        SetHere = 2,
    }
    public static partial class ProtectedConfiguration
    {
        public const string DataProtectionProviderName = "DataProtectionConfigurationProvider";
        public const string ProtectedDataSectionName = "configProtectedData";
        public const string RsaProviderName = "RsaProtectedConfigurationProvider";
        public static string DefaultProvider { get { throw null; } }
        public static System.Configuration.ProtectedConfigurationProviderCollection Providers { get { throw null; } }
    }
    public abstract partial class ProtectedConfigurationProvider : System.Configuration.Provider.ProviderBase
    {
        protected ProtectedConfigurationProvider() { }
        public abstract System.Xml.XmlNode Decrypt(System.Xml.XmlNode encryptedNode);
        public abstract System.Xml.XmlNode Encrypt(System.Xml.XmlNode node);
    }
    public partial class ProtectedConfigurationProviderCollection : System.Configuration.Provider.ProviderCollection
    {
        public ProtectedConfigurationProviderCollection() { }
        public new System.Configuration.ProtectedConfigurationProvider this[string name] { get { throw null; } }
        public override void Add(System.Configuration.Provider.ProviderBase provider) { }
    }
    public sealed partial class ProtectedConfigurationSection : System.Configuration.ConfigurationSection
    {
        public ProtectedConfigurationSection() { }
        [System.Configuration.ConfigurationPropertyAttribute("defaultProvider", DefaultValue = "RsaProtectedConfigurationProvider")]
        public string DefaultProvider { get { throw null; } set { } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("providers")]
        public System.Configuration.ProviderSettingsCollection Providers { get { throw null; } }
    }
    public partial class ProtectedProviderSettings : System.Configuration.ConfigurationElement
    {
        public ProtectedProviderSettings() { }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("", IsDefaultCollection = true, Options = (System.Configuration.ConfigurationPropertyOptions)(1))]
        public System.Configuration.ProviderSettingsCollection Providers { get { throw null; } }
    }
    public sealed partial class ProviderSettings : System.Configuration.ConfigurationElement
    {
        public ProviderSettings() { }
        public ProviderSettings(string name, string type) { }
        [System.Configuration.ConfigurationPropertyAttribute("name", IsRequired = true, IsKey = true)]
        public string Name { get { throw null; } set { } }
        public System.Collections.Specialized.NameValueCollection Parameters { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        [System.Configuration.ConfigurationPropertyAttribute("type", IsRequired = true)]
        public string Type { get { throw null; } set { } }
        protected internal override bool IsModified() { throw null; }
        protected override bool OnDeserializeUnrecognizedAttribute(string name, string value) { throw null; }
        protected internal override void Reset(System.Configuration.ConfigurationElement parentElement) { }
        protected internal override void Unmerge(System.Configuration.ConfigurationElement sourceElement, System.Configuration.ConfigurationElement parentElement, System.Configuration.ConfigurationSaveMode saveMode) { }
    }
    [System.Configuration.ConfigurationCollectionAttribute(typeof(System.Configuration.ProviderSettings))]
    public sealed partial class ProviderSettingsCollection : System.Configuration.ConfigurationElementCollection
    {
        public ProviderSettingsCollection() { }
        public System.Configuration.ProviderSettings this[int index] { get { throw null; } set { } }
        public new System.Configuration.ProviderSettings this[string key] { get { throw null; } }
        protected internal override System.Configuration.ConfigurationPropertyCollection Properties { get { throw null; } }
        public void Add(System.Configuration.ProviderSettings provider) { }
        public void Clear() { }
        protected override System.Configuration.ConfigurationElement CreateNewElement() { throw null; }
        protected override object GetElementKey(System.Configuration.ConfigurationElement element) { throw null; }
        public void Remove(string name) { }
    }
    public partial class RegexStringValidator : System.Configuration.ConfigurationValidatorBase
    {
        public RegexStringValidator(string regex) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class RegexStringValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public RegexStringValidatorAttribute(string regex) { }
        public string Regex { get { throw null; } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public sealed partial class SectionInformation
    {
        internal SectionInformation() { }
        public System.Configuration.ConfigurationAllowDefinition AllowDefinition { get { throw null; } set { } }
        public System.Configuration.ConfigurationAllowExeDefinition AllowExeDefinition { get { throw null; } set { } }
        public bool AllowLocation { get { throw null; } set { } }
        public bool AllowOverride { get { throw null; } set { } }
        public string ConfigSource { get { throw null; } set { } }
        public bool ForceSave { get { throw null; } set { } }
        public bool InheritInChildApplications { get { throw null; } set { } }
        public bool IsDeclarationRequired { get { throw null; } }
        public bool IsDeclared { get { throw null; } }
        public bool IsLocked { get { throw null; } }
        public bool IsProtected { get { throw null; } }
        public string Name { get { throw null; } }
        public System.Configuration.OverrideMode OverrideMode { get { throw null; } set { } }
        public System.Configuration.OverrideMode OverrideModeDefault { get { throw null; } set { } }
        public System.Configuration.OverrideMode OverrideModeEffective { get { throw null; } }
        public System.Configuration.ProtectedConfigurationProvider ProtectionProvider { get { throw null; } }
        public bool RequirePermission { get { throw null; } set { } }
        public bool RestartOnExternalChanges { get { throw null; } set { } }
        public string SectionName { get { throw null; } }
        public string Type { get { throw null; } set { } }
        public void ForceDeclaration() { }
        public void ForceDeclaration(bool force) { }
        public System.Configuration.ConfigurationSection GetParentSection() { throw null; }
        public string GetRawXml() { throw null; }
        public void ProtectSection(string protectionProvider) { }
        public void RevertToParent() { }
        public void SetRawXml(string rawXml) { }
        public void UnprotectSection() { }
    }
    public partial class StringValidator : System.Configuration.ConfigurationValidatorBase
    {
        public StringValidator(int minLength) { }
        public StringValidator(int minLength, int maxLength) { }
        public StringValidator(int minLength, int maxLength, string invalidCharacters) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class StringValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public StringValidatorAttribute() { }
        public string InvalidCharacters { get { throw null; } set { } }
        public int MaxLength { get { throw null; } set { } }
        public int MinLength { get { throw null; } set { } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public sealed partial class SubclassTypeValidator : System.Configuration.ConfigurationValidatorBase
    {
        public SubclassTypeValidator(System.Type baseClass) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class SubclassTypeValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public SubclassTypeValidatorAttribute(System.Type baseClass) { }
        public System.Type BaseClass { get { throw null; } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public partial class TimeSpanMinutesConverter : System.Configuration.ConfigurationConverterBase
    {
        public TimeSpanMinutesConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed partial class TimeSpanMinutesOrInfiniteConverter : System.Configuration.TimeSpanMinutesConverter
    {
        public TimeSpanMinutesOrInfiniteConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public partial class TimeSpanSecondsConverter : System.Configuration.ConfigurationConverterBase
    {
        public TimeSpanSecondsConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed partial class TimeSpanSecondsOrInfiniteConverter : System.Configuration.TimeSpanSecondsConverter
    {
        public TimeSpanSecondsOrInfiniteConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public partial class TimeSpanValidator : System.Configuration.ConfigurationValidatorBase
    {
        public TimeSpanValidator(System.TimeSpan minValue, System.TimeSpan maxValue) { }
        public TimeSpanValidator(System.TimeSpan minValue, System.TimeSpan maxValue, bool rangeIsExclusive) { }
        public TimeSpanValidator(System.TimeSpan minValue, System.TimeSpan maxValue, bool rangeIsExclusive, long resolutionInSeconds) { }
        public override bool CanValidate(System.Type type) { throw null; }
        public override void Validate(object value) { }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(128))]
    public sealed partial class TimeSpanValidatorAttribute : System.Configuration.ConfigurationValidatorAttribute
    {
        public const string TimeSpanMaxValue = "10675199.02:48:05.4775807";
        public const string TimeSpanMinValue = "-10675199.02:48:05.4775808";
        public TimeSpanValidatorAttribute() { }
        public bool ExcludeRange { get { throw null; } set { } }
        public System.TimeSpan MaxValue { get { throw null; } }
        public string MaxValueString { get { throw null; } set { } }
        public System.TimeSpan MinValue { get { throw null; } }
        public string MinValueString { get { throw null; } set { } }
        public override System.Configuration.ConfigurationValidatorBase ValidatorInstance { get { throw null; } }
    }
    public sealed partial class TypeNameConverter : System.Configuration.ConfigurationConverterBase
    {
        public TypeNameConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public delegate void ValidatorCallback(object value);
    public sealed partial class WhiteSpaceTrimStringConverter : System.Configuration.ConfigurationConverterBase
    {
        public WhiteSpaceTrimStringConverter() { }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object data) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext ctx, System.Globalization.CultureInfo ci, object value, System.Type type) { throw null; }
    }
    public sealed class ApplicationScopedSettingAttribute : SettingAttribute
    {
        public ApplicationScopedSettingAttribute() { throw null; }
    }
    public abstract class ApplicationSettingsBase : SettingsBase, INotifyPropertyChanged
    {
        protected ApplicationSettingsBase() { throw null; }
        protected ApplicationSettingsBase(IComponent owner) { throw null; }
        protected ApplicationSettingsBase(IComponent owner, string settingsKey) { throw null; }
        protected ApplicationSettingsBase(string settingsKey) { throw null; }
        public override SettingsContext Context { get; }
        public override SettingsPropertyCollection Properties { get; }
        public override SettingsPropertyValueCollection PropertyValues { get; }
        public override SettingsProviderCollection Providers { get; }
        public string SettingsKey { get; set; }
        public override object this[string propertyName] { get { throw null; } set { throw null; } }
        public event PropertyChangedEventHandler PropertyChanged;
        public event SettingChangingEventHandler SettingChanging;
        public event SettingsLoadedEventHandler SettingsLoaded;
        public event SettingsSavingEventHandler SettingsSaving;
        public object GetPreviousVersion(string propertyName) { throw null; }
        protected virtual void OnPropertyChanged(object sender, PropertyChangedEventArgs e) { throw null; }
        protected virtual void OnSettingChanging(object sender, SettingChangingEventArgs e) { throw null; }
        protected virtual void OnSettingsLoaded(object sender, SettingsLoadedEventArgs e) { throw null; }
        protected virtual void OnSettingsSaving(object sender, CancelEventArgs e) { throw null; }
        public void Reload() { throw null; }
        public void Reset() { throw null; }
        public override void Save() { throw null; }
        public virtual void Upgrade() { throw null; }
    }
    public sealed class ApplicationSettingsGroup : ConfigurationSectionGroup
    {
        public ApplicationSettingsGroup() { throw null; }
    }
    public class AppSettingsReader
    {
        public AppSettingsReader() { throw null; }
        public object GetValue(string key, Type type) { throw null; }
    }
    public sealed class ClientSettingsSection : ConfigurationSection
    {
        public ClientSettingsSection() { throw null; }
        public SettingElementCollection Settings { get; }
    }
    public sealed class ConfigurationSettings
    {
        [Obsolete("This method is obsolete, it has been replaced by System.Configuration!System.Configuration.ConfigurationManager.AppSettings")]
        public static NameValueCollection AppSettings { get; }
        [Obsolete("This method is obsolete, it has been replaced by System.Configuration!System.Configuration.ConfigurationManager.GetSection")]
        public static object GetConfig(string sectionName) { throw null; }
    }
    public sealed class ConfigXmlDocument : XmlDocument, IConfigErrorInfo
    {
        public ConfigXmlDocument() { throw null; }
        public string Filename { get; }
        public int LineNumber { get; }
        string System.Configuration.Internal.IConfigErrorInfo.Filename { get { throw null; } }
        int System.Configuration.Internal.IConfigErrorInfo.LineNumber { get { throw null; } }
        public override XmlAttribute CreateAttribute(string prefix, string localName, string namespaceUri) { throw null; }
        public override XmlCDataSection CreateCDataSection(string data) { throw null; }
        public override XmlComment CreateComment(string data) { throw null; }
        public override XmlElement CreateElement(string prefix, string localName, string namespaceUri) { throw null; }
        public override XmlSignificantWhitespace CreateSignificantWhitespace(string data) { throw null; }
        public override XmlText CreateTextNode(string text) { throw null; }
        public override XmlWhitespace CreateWhitespace(string data) { throw null; }
        public override void Load(string filename) { throw null; }
        public void LoadSingleElement(string filename, XmlTextReader sourceReader) { throw null; }
    }
    public sealed class DefaultSettingValueAttribute : Attribute
    {
        public DefaultSettingValueAttribute(string value) { throw null; }
        public string Value { get; }
    }
    public class DictionarySectionHandler : IConfigurationSectionHandler
    {
        public DictionarySectionHandler() { throw null; }
        protected virtual string KeyAttributeName { get; }
        protected virtual string ValueAttributeName { get; }
        public virtual object Create(object parent, object context, XmlNode section) { throw null; }
    }
    public interface IApplicationSettingsProvider
    {
        SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property);
        void Reset(SettingsContext context);
        void Upgrade(SettingsContext context, SettingsPropertyCollection properties);
    }
    public interface IConfigurationSectionHandler
    {
        object Create(object parent, object configContext, XmlNode section);
    }
    public interface IConfigurationSystem
    {
        object GetConfig(string configKey);
        void Init();
    }
    public class IgnoreSectionHandler : IConfigurationSectionHandler
    {
        public IgnoreSectionHandler() { throw null; }
        public virtual object Create(object parent, object configContext, XmlNode section) { throw null; }
    }
    public interface IPersistComponentSettings
    {
        bool SaveSettings { get; set; }
        string SettingsKey { get; set; }
        void LoadComponentSettings();
        void ResetComponentSettings();
        void SaveComponentSettings();
    }
    public sealed class IriParsingElement : ConfigurationElement
    {
        public IriParsingElement() { throw null; }
        public bool Enabled { get; set; }
    }
    public interface ISettingsProviderService
    {
        SettingsProvider GetSettingsProvider(SettingsProperty property);
    }
    public class LocalFileSettingsProvider : SettingsProvider, IApplicationSettingsProvider
    {
        public LocalFileSettingsProvider() { throw null; }
        public override string ApplicationName { get; set; }
        public SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property) { throw null; }
        public override SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection properties) { throw null; }
        public override void Initialize(string name, NameValueCollection values) { throw null; }
        public void Reset(SettingsContext context) { throw null; }
        public override void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection values) { throw null; }
        public void Upgrade(SettingsContext context, SettingsPropertyCollection properties) { throw null; }
    }
    public class NameValueFileSectionHandler : IConfigurationSectionHandler
    {
        public NameValueFileSectionHandler() { throw null; }
        public object Create(object parent, object configContext, XmlNode section) { throw null; }
    }
    public class NameValueSectionHandler : IConfigurationSectionHandler
    {
        public NameValueSectionHandler() { throw null; }
        protected virtual string KeyAttributeName { get; }
        protected virtual string ValueAttributeName { get; }
        public object Create(object parent, object context, XmlNode section) { throw null; }
    }
    public sealed class NoSettingsVersionUpgradeAttribute : Attribute
    {
        public NoSettingsVersionUpgradeAttribute() { throw null; }
    }
    public sealed class SchemeSettingElement : ConfigurationElement
    {
        public SchemeSettingElement() { throw null; }
        public GenericUriParserOptions GenericUriParserOptions { get; }
        public string Name { get; }
    }
    public sealed class SchemeSettingElementCollection : ConfigurationElementCollection
    {
        public SchemeSettingElementCollection() { throw null; }
        public override ConfigurationElementCollectionType CollectionType { get; }
        public SchemeSettingElement this[int index] { get { throw null; } }
        public new SchemeSettingElement this[string name] { get { throw null; } }
        protected override ConfigurationElement CreateNewElement() { throw null; }
        protected override object GetElementKey(ConfigurationElement element) { throw null; }
        public int IndexOf(SchemeSettingElement element) { throw null; }
    }
    public class SettingAttribute : Attribute
    {
        public SettingAttribute() { throw null; }
    }
    public class SettingChangingEventArgs : CancelEventArgs
    {
        public SettingChangingEventArgs(string settingName, string settingClass, string settingKey, object newValue, bool cancel) { throw null; }
        public object NewValue { get; }
        public string SettingClass { get; }
        public string SettingKey { get; }
        public string SettingName { get; }
    }
    public delegate void SettingChangingEventHandler(object sender, SettingChangingEventArgs e);
    public sealed class SettingElement : ConfigurationElement
    {
        public SettingElement() { throw null; }
        public SettingElement(string name, SettingsSerializeAs serializeAs) { throw null; }
        public string Name { get; set; }
        public SettingsSerializeAs SerializeAs { get; set; }
        public SettingValueElement Value { get; set; }
        public override bool Equals(object settings) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public sealed class SettingElementCollection : ConfigurationElementCollection
    {
        public SettingElementCollection() { throw null; }
        public override ConfigurationElementCollectionType CollectionType { get; }
        protected override string ElementName { get; }
        public void Add(SettingElement element) { throw null; }
        public void Clear() { throw null; }
        protected override ConfigurationElement CreateNewElement() { throw null; }
        public SettingElement Get(string elementKey) { throw null; }
        protected override object GetElementKey(ConfigurationElement element) { throw null; }
        public void Remove(SettingElement element) { throw null; }
    }
    public class SettingsAttributeDictionary : Hashtable
    {
        public SettingsAttributeDictionary() { throw null; }
        public SettingsAttributeDictionary(SettingsAttributeDictionary attributes) { throw null; }
    }
    public abstract class SettingsBase
    {
        protected SettingsBase() { throw null; }
        public virtual SettingsContext Context { get; }
        public bool IsSynchronized { get; }
        public virtual SettingsPropertyCollection Properties { get; }
        public virtual SettingsPropertyValueCollection PropertyValues { get; }
        public virtual SettingsProviderCollection Providers { get; }
        public virtual object this[string propertyName] { get { throw null; } set { throw null; } }
        public void Initialize(SettingsContext context, SettingsPropertyCollection properties, SettingsProviderCollection providers) { throw null; }
        public virtual void Save() { throw null; }
        public static SettingsBase Synchronized(SettingsBase settingsBase) { throw null; }
    }
    public class SettingsContext : Hashtable
    {
        public SettingsContext() { throw null; }
    }
    public sealed class SettingsDescriptionAttribute : Attribute
    {
        public SettingsDescriptionAttribute(string description) { throw null; }
        public string Description { get; }
    }
    public sealed class SettingsGroupDescriptionAttribute : Attribute
    {
        public SettingsGroupDescriptionAttribute(string description) { throw null; }
        public string Description { get; }
    }
    public sealed class SettingsGroupNameAttribute : Attribute
    {
        public SettingsGroupNameAttribute(string groupName) { throw null; }
        public string GroupName { get; }
    }
    public class SettingsLoadedEventArgs : EventArgs
    {
        public SettingsLoadedEventArgs(SettingsProvider provider) { throw null; }
        public SettingsProvider Provider { get; }
    }
    public delegate void SettingsLoadedEventHandler(object sender, SettingsLoadedEventArgs e);
    public enum SettingsManageability
    {
        Roaming = 0,
    }
    public sealed class SettingsManageabilityAttribute : Attribute
    {
        public SettingsManageabilityAttribute(SettingsManageability manageability) { throw null; }
        public SettingsManageability Manageability { get; }
    }
    public class SettingsProperty
    {
        public SettingsProperty(SettingsProperty propertyToCopy) { throw null; }
        public SettingsProperty(string name) { throw null; }
        public SettingsProperty(string name, Type propertyType, SettingsProvider provider, bool isReadOnly, object defaultValue, SettingsSerializeAs serializeAs, SettingsAttributeDictionary attributes, bool throwOnErrorDeserializing, bool throwOnErrorSerializing) { throw null; }
        public virtual SettingsAttributeDictionary Attributes { get; }
        public virtual object DefaultValue { get; set; }
        public virtual bool IsReadOnly { get; set; }
        public virtual string Name { get; set; }
        public virtual Type PropertyType { get; set; }
        public virtual SettingsProvider Provider { get; set; }
        public virtual SettingsSerializeAs SerializeAs { get; set; }
        public bool ThrowOnErrorDeserializing { get; set; }
        public bool ThrowOnErrorSerializing { get; set; }
    }
    public class SettingsPropertyCollection : ICloneable, ICollection, IEnumerable
    {
        public SettingsPropertyCollection() { throw null; }
        public int Count { get; }
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
        public SettingsProperty this[string name] { get { throw null; } }
        public void Add(SettingsProperty property) { throw null; }
        public void Clear() { throw null; }
        public object Clone() { throw null; }
        public void CopyTo(Array array, int index) { throw null; }
        public IEnumerator GetEnumerator() { throw null; }
        protected virtual void OnAdd(SettingsProperty property) { throw null; }
        protected virtual void OnAddComplete(SettingsProperty property) { throw null; }
        protected virtual void OnClear() { throw null; }
        protected virtual void OnClearComplete() { throw null; }
        protected virtual void OnRemove(SettingsProperty property) { throw null; }
        protected virtual void OnRemoveComplete(SettingsProperty property) { throw null; }
        public void Remove(string name) { throw null; }
        public void SetReadOnly() { throw null; }
    }
    public class SettingsPropertyIsReadOnlyException : Exception
    {
        public SettingsPropertyIsReadOnlyException() { throw null; }
        protected SettingsPropertyIsReadOnlyException(SerializationInfo info, StreamingContext context) { throw null; }
        public SettingsPropertyIsReadOnlyException(string message) { throw null; }
        public SettingsPropertyIsReadOnlyException(string message, Exception innerException) { throw null; }
    }
    public class SettingsPropertyNotFoundException : Exception
    {
        public SettingsPropertyNotFoundException() { throw null; }
        protected SettingsPropertyNotFoundException(SerializationInfo info, StreamingContext context) { throw null; }
        public SettingsPropertyNotFoundException(string message) { throw null; }
        public SettingsPropertyNotFoundException(string message, Exception innerException) { throw null; }
    }
    public class SettingsPropertyValue
    {
        public SettingsPropertyValue(SettingsProperty property) { throw null; }
        public bool Deserialized { get; set; }
        public bool IsDirty { get; set; }
        public string Name { get; }
        public SettingsProperty Property { get; }
        public object PropertyValue { get; set; }
        public object SerializedValue { get; set; }
        public bool UsingDefaultValue { get; }
    }
    public class SettingsPropertyValueCollection : ICloneable, ICollection, IEnumerable
    {
        public SettingsPropertyValueCollection() { throw null; }
        public int Count { get; }
        public bool IsSynchronized { get; }
        public object SyncRoot { get; }
        public SettingsPropertyValue this[string name] { get { throw null; } }
        public void Add(SettingsPropertyValue property) { throw null; }
        public void Clear() { throw null; }
        public object Clone() { throw null; }
        public void CopyTo(Array array, int index) { throw null; }
        public IEnumerator GetEnumerator() { throw null; }
        public void Remove(string name) { throw null; }
        public void SetReadOnly() { throw null; }
    }
    public class SettingsPropertyWrongTypeException : Exception
    {
        public SettingsPropertyWrongTypeException() { throw null; }
        protected SettingsPropertyWrongTypeException(SerializationInfo info, StreamingContext context) { throw null; }
        public SettingsPropertyWrongTypeException(string message) { throw null; }
        public SettingsPropertyWrongTypeException(string message, Exception innerException) { throw null; }
    }
    public abstract class SettingsProvider : ProviderBase
    {
        protected SettingsProvider() { throw null; }
        public abstract string ApplicationName { get; set; }
        public abstract SettingsPropertyValueCollection GetPropertyValues(SettingsContext context, SettingsPropertyCollection collection);
        public abstract void SetPropertyValues(SettingsContext context, SettingsPropertyValueCollection collection);
    }
    public sealed class SettingsProviderAttribute : Attribute
    {
        public SettingsProviderAttribute(string providerTypeName) { throw null; }
        public SettingsProviderAttribute(Type providerType) { throw null; }
        public string ProviderTypeName { get; }
    }
    public class SettingsProviderCollection : ProviderCollection
    {
        public SettingsProviderCollection() { throw null; }
        public new SettingsProvider this[string name] { get { throw null; } }
        public override void Add(ProviderBase provider) { throw null; }
    }
    public delegate void SettingsSavingEventHandler(object sender, CancelEventArgs e);
    public enum SettingsSerializeAs
    {
        Binary = 2,
        ProviderSpecific = 3,
        String = 0,
        Xml = 1,
    }
    public sealed class SettingsSerializeAsAttribute : Attribute
    {
        public SettingsSerializeAsAttribute(SettingsSerializeAs serializeAs) { throw null; }
        public SettingsSerializeAs SerializeAs { get; }
    }
    public sealed class SettingValueElement : ConfigurationElement
    {
        public SettingValueElement() { throw null; }
        public XmlNode ValueXml { get; set; }
        public override bool Equals(object settingValue) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public class SingleTagSectionHandler : IConfigurationSectionHandler
    {
        public SingleTagSectionHandler() { throw null; }
        public virtual object Create(object parent, object context, XmlNode section) { throw null; }
    }
    public enum SpecialSetting
    {
        ConnectionString = 0,
        WebServiceUrl = 1,
    }
    public sealed class SpecialSettingAttribute : Attribute
    {
        public SpecialSettingAttribute(SpecialSetting specialSetting) { throw null; }
        public SpecialSetting SpecialSetting { get; }
    }
    public sealed class UserScopedSettingAttribute : SettingAttribute
    {
        public UserScopedSettingAttribute() { throw null; }
    }
    public sealed class UserSettingsGroup : ConfigurationSectionGroup
    {
        public UserSettingsGroup() { throw null; }
    }
}
namespace System.Configuration.Internal
{
    public partial class DelegatingConfigHost : System.Configuration.Internal.IInternalConfigHost
    {
        protected DelegatingConfigHost() { }
        protected System.Configuration.Internal.IInternalConfigHost Host { get { throw null; } set { } }
        public virtual bool IsRemote { get { throw null; } }
        public virtual bool SupportsChangeNotifications { get { throw null; } }
        public virtual bool SupportsLocation { get { throw null; } }
        public virtual bool SupportsPath { get { throw null; } }
        public virtual bool SupportsRefresh { get { throw null; } }
        public virtual object CreateConfigurationContext(string configPath, string locationSubPath) { throw null; }
        public virtual object CreateDeprecatedConfigContext(string configPath) { throw null; }
        public virtual string DecryptSection(string encryptedXml, System.Configuration.ProtectedConfigurationProvider protectionProvider, System.Configuration.ProtectedConfigurationSection protectedConfigSection) { throw null; }
        public virtual void DeleteStream(string streamName) { }
        public virtual string EncryptSection(string clearTextXml, System.Configuration.ProtectedConfigurationProvider protectionProvider, System.Configuration.ProtectedConfigurationSection protectedConfigSection) { throw null; }
        public virtual string GetConfigPathFromLocationSubPath(string configPath, string locationSubPath) { throw null; }
        public virtual System.Type GetConfigType(string typeName, bool throwOnError) { throw null; }
        public virtual string GetConfigTypeName(System.Type t) { throw null; }
        public virtual string GetStreamName(string configPath) { throw null; }
        public virtual string GetStreamNameForConfigSource(string streamName, string configSource) { throw null; }
        public virtual object GetStreamVersion(string streamName) { throw null; }
        public virtual void Init(System.Configuration.Internal.IInternalConfigRoot configRoot, params object[] hostInitParams) { }
        public virtual void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath, System.Configuration.Internal.IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams) { configPath = default(string); locationConfigPath = default(string); }
        public virtual bool IsAboveApplication(string configPath) { throw null; }
        public virtual bool IsConfigRecordRequired(string configPath) { throw null; }
        public virtual bool IsDefinitionAllowed(string configPath, System.Configuration.ConfigurationAllowDefinition allowDefinition, System.Configuration.ConfigurationAllowExeDefinition allowExeDefinition) { throw null; }
        public virtual bool IsFile(string streamName) { throw null; }
        public virtual bool IsInitDelayed(System.Configuration.Internal.IInternalConfigRecord configRecord) { throw null; }
        public virtual bool IsLocationApplicable(string configPath) { throw null; }
        public virtual bool IsSecondaryRoot(string configPath) { throw null; }
        public virtual System.IO.Stream OpenStreamForRead(string streamName) { throw null; }
        public virtual System.IO.Stream OpenStreamForRead(string streamName, bool assertPermissions) { throw null; }
        public virtual System.IO.Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext) { throw null; }
        public virtual System.IO.Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext, bool assertPermissions) { throw null; }
        public virtual bool PrefetchAll(string configPath, string streamName) { throw null; }
        public virtual bool PrefetchSection(string sectionGroupName, string sectionName) { throw null; }
        public virtual void RequireCompleteInit(System.Configuration.Internal.IInternalConfigRecord configRecord) { }
        public virtual object StartMonitoringStreamForChanges(string streamName, System.Configuration.Internal.StreamChangeCallback callback) { throw null; }
        public virtual void StopMonitoringStreamForChanges(string streamName, System.Configuration.Internal.StreamChangeCallback callback) { }
        public virtual void VerifyDefinitionAllowed(string configPath, System.Configuration.ConfigurationAllowDefinition allowDefinition, System.Configuration.ConfigurationAllowExeDefinition allowExeDefinition, System.Configuration.Internal.IConfigErrorInfo errorInfo) { }
        public virtual void WriteCompleted(string streamName, bool success, object writeContext) { }
        public virtual void WriteCompleted(string streamName, bool success, object writeContext, bool assertPermissions) { }
    }
    public partial interface IConfigErrorInfo
    {
        string Filename { get; }
        int LineNumber { get; }
    }
    public partial interface IConfigSystem
    {
        System.Configuration.Internal.IInternalConfigHost Host { get; }
        System.Configuration.Internal.IInternalConfigRoot Root { get; }
        void Init(System.Type typeConfigHost, params object[] hostInitParams);
    }
    public partial interface IConfigurationManagerHelper
    {
        void EnsureNetConfigLoaded();
    }
    public partial interface IConfigurationManagerInternal
    {
        string ApplicationConfigUri { get; }
        string ExeLocalConfigDirectory { get; }
        string ExeLocalConfigPath { get; }
        string ExeProductName { get; }
        string ExeProductVersion { get; }
        string ExeRoamingConfigDirectory { get; }
        string ExeRoamingConfigPath { get; }
        string MachineConfigPath { get; }
        bool SetConfigurationSystemInProgress { get; }
        bool SupportsUserConfig { get; }
        string UserConfigFilename { get; }
    }
    public partial interface IInternalConfigClientHost
    {
        string GetExeConfigPath();
        string GetLocalUserConfigPath();
        string GetRoamingUserConfigPath();
        bool IsExeConfig(string configPath);
        bool IsLocalUserConfig(string configPath);
        bool IsRoamingUserConfig(string configPath);
    }
    public partial interface IInternalConfigConfigurationFactory
    {
        System.Configuration.Configuration Create(System.Type typeConfigHost, params object[] hostInitConfigurationParams);
        string NormalizeLocationSubPath(string subPath, System.Configuration.Internal.IConfigErrorInfo errorInfo);
    }
    public partial interface IInternalConfigHost
    {
        bool IsRemote { get; }
        bool SupportsChangeNotifications { get; }
        bool SupportsLocation { get; }
        bool SupportsPath { get; }
        bool SupportsRefresh { get; }
        object CreateConfigurationContext(string configPath, string locationSubPath);
        object CreateDeprecatedConfigContext(string configPath);
        string DecryptSection(string encryptedXml, System.Configuration.ProtectedConfigurationProvider protectionProvider, System.Configuration.ProtectedConfigurationSection protectedConfigSection);
        void DeleteStream(string streamName);
        string EncryptSection(string clearTextXml, System.Configuration.ProtectedConfigurationProvider protectionProvider, System.Configuration.ProtectedConfigurationSection protectedConfigSection);
        string GetConfigPathFromLocationSubPath(string configPath, string locationSubPath);
        System.Type GetConfigType(string typeName, bool throwOnError);
        string GetConfigTypeName(System.Type t);
        string GetStreamName(string configPath);
        string GetStreamNameForConfigSource(string streamName, string configSource);
        object GetStreamVersion(string streamName);
        void Init(System.Configuration.Internal.IInternalConfigRoot configRoot, params object[] hostInitParams);
        void InitForConfiguration(ref string locationSubPath, out string configPath, out string locationConfigPath, System.Configuration.Internal.IInternalConfigRoot configRoot, params object[] hostInitConfigurationParams);
        bool IsAboveApplication(string configPath);
        bool IsConfigRecordRequired(string configPath);
        bool IsDefinitionAllowed(string configPath, System.Configuration.ConfigurationAllowDefinition allowDefinition, System.Configuration.ConfigurationAllowExeDefinition allowExeDefinition);
        bool IsFile(string streamName);
        bool IsInitDelayed(System.Configuration.Internal.IInternalConfigRecord configRecord);
        bool IsLocationApplicable(string configPath);
        bool IsSecondaryRoot(string configPath);
        System.IO.Stream OpenStreamForRead(string streamName);
        System.IO.Stream OpenStreamForRead(string streamName, bool assertPermissions);
        System.IO.Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext);
        System.IO.Stream OpenStreamForWrite(string streamName, string templateStreamName, ref object writeContext, bool assertPermissions);
        bool PrefetchAll(string configPath, string streamName);
        bool PrefetchSection(string sectionGroupName, string sectionName);
        void RequireCompleteInit(System.Configuration.Internal.IInternalConfigRecord configRecord);
        object StartMonitoringStreamForChanges(string streamName, System.Configuration.Internal.StreamChangeCallback callback);
        void StopMonitoringStreamForChanges(string streamName, System.Configuration.Internal.StreamChangeCallback callback);
        void VerifyDefinitionAllowed(string configPath, System.Configuration.ConfigurationAllowDefinition allowDefinition, System.Configuration.ConfigurationAllowExeDefinition allowExeDefinition, System.Configuration.Internal.IConfigErrorInfo errorInfo);
        void WriteCompleted(string streamName, bool success, object writeContext);
        void WriteCompleted(string streamName, bool success, object writeContext, bool assertPermissions);
    }
    public partial interface IInternalConfigRecord
    {
        string ConfigPath { get; }
        bool HasInitErrors { get; }
        string StreamName { get; }
        object GetLkgSection(string configKey);
        object GetSection(string configKey);
        void RefreshSection(string configKey);
        void Remove();
        void ThrowIfInitErrors();
    }
    public partial interface IInternalConfigRoot
    {
        bool IsDesignTime { get; }
        event System.Configuration.Internal.InternalConfigEventHandler ConfigChanged;
        event System.Configuration.Internal.InternalConfigEventHandler ConfigRemoved;
        System.Configuration.Internal.IInternalConfigRecord GetConfigRecord(string configPath);
        object GetSection(string section, string configPath);
        string GetUniqueConfigPath(string configPath);
        System.Configuration.Internal.IInternalConfigRecord GetUniqueConfigRecord(string configPath);
        void Init(System.Configuration.Internal.IInternalConfigHost host, bool isDesignTime);
        void RemoveConfig(string configPath);
    }
    public partial interface IInternalConfigSettingsFactory
    {
        void CompleteInit();
        void SetConfigurationSystem(System.Configuration.Internal.IInternalConfigSystem internalConfigSystem, bool initComplete);
    }
    public partial interface IInternalConfigSystem
    {
        bool SupportsUserConfig { get; }
        object GetSection(string configKey);
        void RefreshConfig(string sectionName);
    }
    public sealed partial class InternalConfigEventArgs : System.EventArgs
    {
        public InternalConfigEventArgs(string configPath) { }
        public string ConfigPath { get { throw null; } set { } }
    }
    public delegate void InternalConfigEventHandler(object sender, System.Configuration.Internal.InternalConfigEventArgs e);
    public delegate void StreamChangeCallback(string streamName);
}
namespace System.Configuration.Provider
{
    public abstract partial class ProviderBase
    {
        protected ProviderBase() { }
        public virtual string Description { get { throw null; } }
        public virtual string Name { get { throw null; } }
        public virtual void Initialize(string name, System.Collections.Specialized.NameValueCollection config) { }
    }
    public partial class ProviderCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public ProviderCollection() { }
        public int Count { get { throw null; } }
        public bool IsSynchronized { get { throw null; } }
        public System.Configuration.Provider.ProviderBase this[string name] { get { throw null; } }
        public object SyncRoot { get { throw null; } }
        public virtual void Add(System.Configuration.Provider.ProviderBase provider) { }
        public void Clear() { }
        public void CopyTo(System.Configuration.Provider.ProviderBase[] array, int index) { }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public void Remove(string name) { }
        public void SetReadOnly() { }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public partial class ProviderException : System.Exception
    {
        public ProviderException() { }
        protected ProviderException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public ProviderException(string message) { }
        public ProviderException(string message, System.Exception innerException) { }
    }
}

#pragma warning restore CS0618



