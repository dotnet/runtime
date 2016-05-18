// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.ComponentModel
{
    public partial class ArrayConverter : System.ComponentModel.CollectionConverter
    {
        public ArrayConverter() { }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
    }
    public partial class AttributeCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public static readonly System.ComponentModel.AttributeCollection Empty;
        protected AttributeCollection() { }
        public AttributeCollection(params System.Attribute[] attributes) { }
        protected virtual System.Attribute[] Attributes { get; }
        public bool Contains(System.Attribute attribute) { return default(bool); }
        public bool Contains(System.Attribute[] attributes) { return default(bool); }
        public void CopyTo(System.Array array, int index) { }
        public int Count { get; }
        public static System.ComponentModel.AttributeCollection FromExisting(System.ComponentModel.AttributeCollection existing, params System.Attribute[] newAttributes) { return default(System.ComponentModel.AttributeCollection); }
        protected System.Attribute GetDefaultAttribute(System.Type attributeType) { return default(System.Attribute); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        bool System.Collections.ICollection.IsSynchronized { get; }
        public bool Matches(System.Attribute attribute) { return default(bool); }
        public bool Matches(System.Attribute[] attributes) { return default(bool); }
        object System.Collections.ICollection.SyncRoot { get; }
        public virtual System.Attribute this[int index] { get { return default(System.Attribute); } }
        public virtual System.Attribute this[System.Type attributeType] { get { return default(System.Attribute); } }
    }
    public partial class AttributeProviderAttribute : System.Attribute
    {
        public AttributeProviderAttribute(string typeName) { }
        public AttributeProviderAttribute(string typeName, string propertyName) { }
        public AttributeProviderAttribute(System.Type type) { }
        public string PropertyName { get; }
        public string TypeName { get; }
    }
    public abstract partial class BaseNumberConverter : System.ComponentModel.TypeConverter
    {
        protected BaseNumberConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type t) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class BooleanConverter : System.ComponentModel.TypeConverter
    {
        public BooleanConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { return default(System.ComponentModel.TypeConverter.StandardValuesCollection); }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
    }
    public partial class ByteConverter : System.ComponentModel.BaseNumberConverter
    {
        public ByteConverter() { }
    }
    public delegate void CancelEventHandler(object sender, System.ComponentModel.CancelEventArgs e);
    public partial class CharConverter : System.ComponentModel.TypeConverter
    {
        public CharConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public enum CollectionChangeAction
    {
        Add = 1,
        Refresh = 3,
        Remove = 2,
    }
    public partial class CollectionChangeEventArgs : System.EventArgs
    {
        public CollectionChangeEventArgs(System.ComponentModel.CollectionChangeAction action, object element) { }
        public virtual System.ComponentModel.CollectionChangeAction Action { get; }
        public virtual object Element { get; }
    }
    public delegate void CollectionChangeEventHandler(object sender, System.ComponentModel.CollectionChangeEventArgs e);
    public partial class CollectionConverter : System.ComponentModel.TypeConverter
    {
        public CollectionConverter() { }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
    }
    public abstract partial class CustomTypeDescriptor : System.ComponentModel.ICustomTypeDescriptor
    {
        protected CustomTypeDescriptor() { }
        protected CustomTypeDescriptor(System.ComponentModel.ICustomTypeDescriptor parent) { }
        public virtual System.ComponentModel.AttributeCollection GetAttributes() { return default(System.ComponentModel.AttributeCollection); }
        public virtual string GetClassName() { return default(string); }
        public virtual string GetComponentName() { return default(string); }
        public virtual System.ComponentModel.TypeConverter GetConverter() { return default(System.ComponentModel.TypeConverter); }
        public virtual System.ComponentModel.EventDescriptor GetDefaultEvent() { return default(System.ComponentModel.EventDescriptor); }
        public virtual System.ComponentModel.PropertyDescriptor GetDefaultProperty() { return default(System.ComponentModel.PropertyDescriptor); }
        public virtual object GetEditor(System.Type editorBaseType) { return default(object); }
        public virtual System.ComponentModel.EventDescriptorCollection GetEvents() { return default(System.ComponentModel.EventDescriptorCollection); }
        public virtual System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection GetProperties() { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual object GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd) { return default(object); }
    }
    public partial class DateTimeConverter : System.ComponentModel.TypeConverter
    {
        public DateTimeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class DateTimeOffsetConverter : System.ComponentModel.TypeConverter
    {
        public DateTimeOffsetConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class DecimalConverter : System.ComponentModel.BaseNumberConverter
    {
        public DecimalConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public sealed partial class DefaultEventAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DefaultEventAttribute Default;
        public DefaultEventAttribute(string name) { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public string Name { get; }
    }
    public sealed partial class DefaultPropertyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DefaultPropertyAttribute Default;
        public DefaultPropertyAttribute(string name) { }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
        public string Name { get; }
    }
    public partial class DoubleConverter : System.ComponentModel.BaseNumberConverter
    {
        public DoubleConverter() { }
    }
    public partial class EnumConverter : System.ComponentModel.TypeConverter
    {
        public EnumConverter(System.Type type) { }
        protected System.Type EnumType { get { return default(System.Type); } }
        protected virtual System.Collections.IComparer Comparer { get; }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { return default(System.ComponentModel.TypeConverter.StandardValuesCollection); }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(bool); }
        protected System.ComponentModel.TypeConverter.StandardValuesCollection Values { get; set; }
    }
    public abstract partial class EventDescriptor : System.ComponentModel.MemberDescriptor
    {
        protected EventDescriptor(System.ComponentModel.MemberDescriptor descr) : base(default(System.ComponentModel.MemberDescriptor)) { }
        protected EventDescriptor(System.ComponentModel.MemberDescriptor descr, System.Attribute[] attrs) : base(default(System.ComponentModel.MemberDescriptor), default(System.Attribute[])) { }
        protected EventDescriptor(string name, System.Attribute[] attrs) : base(default(string), default(System.Attribute[])) { }
        public abstract void AddEventHandler(object component, System.Delegate value);
        public abstract System.Type ComponentType { get; }
        public abstract System.Type EventType { get; }
        public abstract bool IsMulticast { get; }
        public abstract void RemoveEventHandler(object component, System.Delegate value);
    }
    public partial class EventDescriptorCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public static readonly System.ComponentModel.EventDescriptorCollection Empty;
        public EventDescriptorCollection(System.ComponentModel.EventDescriptor[] events) { }
        public EventDescriptorCollection(System.ComponentModel.EventDescriptor[] events, bool readOnly) { }
        int System.Collections.IList.Add(object value) { return default(int); }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        public int Count { get; }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        bool System.Collections.ICollection.IsSynchronized { get; }
        object System.Collections.ICollection.SyncRoot { get; }
        bool System.Collections.IList.IsFixedSize { get; }
        bool System.Collections.IList.IsReadOnly { get; }
        void System.Collections.IList.Remove(object value) { }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public virtual System.ComponentModel.EventDescriptor this[int index] { get { return default(System.ComponentModel.EventDescriptor); } }
        public virtual System.ComponentModel.EventDescriptor this[string name] { get { return default(System.ComponentModel.EventDescriptor); } }
        public int Add(System.ComponentModel.EventDescriptor value) { return default(int); }
        public void Clear() { }
        public bool Contains(System.ComponentModel.EventDescriptor value) { return default(bool); }
        public virtual System.ComponentModel.EventDescriptor Find(string name, bool ignoreCase) { return default(System.ComponentModel.EventDescriptor); }
        public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public int IndexOf(System.ComponentModel.EventDescriptor value) { return default(int); }
        public void Insert(int index, System.ComponentModel.EventDescriptor value) { }
        protected void InternalSort(System.Collections.IComparer sorter) { }
        protected void InternalSort(string[] names) { }
        public void Remove(System.ComponentModel.EventDescriptor value) { }
        public void RemoveAt(int index) { }
        public virtual System.ComponentModel.EventDescriptorCollection Sort() { return default(System.ComponentModel.EventDescriptorCollection); }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(System.Collections.IComparer comparer) { return default(System.ComponentModel.EventDescriptorCollection); }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(string[] names) { return default(System.ComponentModel.EventDescriptorCollection); }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(string[] names, System.Collections.IComparer comparer) { return default(System.ComponentModel.EventDescriptorCollection); }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
    }
    public sealed partial class ExtenderProvidedPropertyAttribute : System.Attribute
    {
        public ExtenderProvidedPropertyAttribute() { }
        public System.ComponentModel.PropertyDescriptor ExtenderProperty { get; }
        public System.ComponentModel.IExtenderProvider Provider { get; }
        public System.Type ReceiverType { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public partial class GuidConverter : System.ComponentModel.TypeConverter
    {
        public GuidConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class HandledEventArgs : System.EventArgs
    {
        public HandledEventArgs() { }
        public HandledEventArgs(bool defaultHandledValue) { }
        public bool Handled { get; set; }
    }
    public delegate void HandledEventHandler(object sender, System.ComponentModel.HandledEventArgs e);
    public partial interface ICustomTypeDescriptor
    {
        System.ComponentModel.AttributeCollection GetAttributes();
        string GetClassName();
        string GetComponentName();
        System.ComponentModel.TypeConverter GetConverter();
        System.ComponentModel.EventDescriptor GetDefaultEvent();
        System.ComponentModel.PropertyDescriptor GetDefaultProperty();
        object GetEditor(System.Type editorBaseType);
        System.ComponentModel.EventDescriptorCollection GetEvents();
        System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[] attributes);
        System.ComponentModel.PropertyDescriptorCollection GetProperties();
        System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[] attributes);
        object GetPropertyOwner(System.ComponentModel.PropertyDescriptor pd);
    }
    public partial interface IExtenderProvider
    {
        bool CanExtend(object extendee);
    }
    public partial interface IListSource
    {
        bool ContainsListCollection { get; }
        System.Collections.IList GetList();
    }
    public partial class Int16Converter : System.ComponentModel.BaseNumberConverter
    {
        public Int16Converter() { }
    }
    public partial class Int32Converter : System.ComponentModel.BaseNumberConverter
    {
        public Int32Converter() { }
    }
    public partial class Int64Converter : System.ComponentModel.BaseNumberConverter
    {
        public Int64Converter() { }
    }
    public partial class InvalidAsynchronousStateException : System.ArgumentException
    {
        public InvalidAsynchronousStateException() { }
        public InvalidAsynchronousStateException(string message) { }
        public InvalidAsynchronousStateException(string message, System.Exception innerException) { }
    }
    public partial interface ITypeDescriptorContext : System.IServiceProvider
    {
        System.ComponentModel.IContainer Container { get; }
        object Instance { get; }
        System.ComponentModel.PropertyDescriptor PropertyDescriptor { get; }
        void OnComponentChanged();
        bool OnComponentChanging();
    }
    public partial interface ITypedList
    {
        System.ComponentModel.PropertyDescriptorCollection GetItemProperties(System.ComponentModel.PropertyDescriptor[] listAccessors);
        string GetListName(System.ComponentModel.PropertyDescriptor[] listAccessors);
    }
    public abstract partial class MemberDescriptor
    {
        protected MemberDescriptor(System.ComponentModel.MemberDescriptor descr) { }
        protected MemberDescriptor(System.ComponentModel.MemberDescriptor oldMemberDescriptor, System.Attribute[] newAttributes) { }
        protected MemberDescriptor(string name) { }
        protected MemberDescriptor(string name, System.Attribute[] attributes) { }
        protected virtual System.Attribute[] AttributeArray { get; set; }
        public virtual System.ComponentModel.AttributeCollection Attributes { get; }
        public virtual string Category { get; }
        public virtual string Description { get; }
        public virtual bool DesignTimeOnly { get; }
        public virtual string DisplayName { get; }
        public virtual bool IsBrowsable { get; }
        public virtual string Name { get; }
        protected virtual int NameHashCode { get; }
        protected virtual System.ComponentModel.AttributeCollection CreateAttributeCollection() { return default(System.ComponentModel.AttributeCollection); }
        public override bool Equals(object obj) { return default(bool); }
        protected virtual void FillAttributes(System.Collections.IList attributeList) { }
        protected static System.Reflection.MethodInfo FindMethod(System.Type componentClass, string name, System.Type[] args, System.Type returnType) { return default(System.Reflection.MethodInfo); }
        protected static System.Reflection.MethodInfo FindMethod(System.Type componentClass, string name, System.Type[] args, System.Type returnType, bool publicOnly) { return default(System.Reflection.MethodInfo); }
        public override int GetHashCode() { return default(int); }
        protected virtual object GetInvocationTarget(System.Type type, object instance) { return default(object); }
        protected static System.ComponentModel.ISite GetSite(object component) { return default(System.ComponentModel.ISite); }
    }
    public partial class MultilineStringConverter : System.ComponentModel.TypeConverter
    {
        public MultilineStringConverter() { }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
    }
    public partial class NullableConverter : System.ComponentModel.TypeConverter
    {
        public NullableConverter(System.Type type) { }
        public System.Type NullableType { get { return default(System.Type); } }
        public System.Type UnderlyingType { get { return default(System.Type); } }
        public System.ComponentModel.TypeConverter UnderlyingTypeConverter { get { return default(System.ComponentModel.TypeConverter); } }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Collections.IDictionary propertyValues) { return default(object); }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { return default(System.ComponentModel.TypeConverter.StandardValuesCollection); }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(bool); }
    }
    public abstract partial class PropertyDescriptor : System.ComponentModel.MemberDescriptor
    {
        protected PropertyDescriptor(System.ComponentModel.MemberDescriptor descr) : base(default(System.ComponentModel.MemberDescriptor)) { }
        protected PropertyDescriptor(System.ComponentModel.MemberDescriptor descr, System.Attribute[] attrs) : base(default(System.ComponentModel.MemberDescriptor), default(System.Attribute[])) { }
        protected PropertyDescriptor(string name, System.Attribute[] attrs) : base(default(string), default(System.Attribute[])) { }
        public abstract System.Type ComponentType { get; }
        public virtual System.ComponentModel.TypeConverter Converter { get; }
        public virtual bool IsLocalizable { get; }
        public abstract bool IsReadOnly { get; }
        public abstract System.Type PropertyType { get; }
        public virtual bool SupportsChangeEvents { get; }
        public virtual void AddValueChanged(object component, System.EventHandler handler) { }
        public abstract bool CanResetValue(object component);
        protected object CreateInstance(System.Type type) { return default(object); }
        public override bool Equals(object obj) { return default(bool); }
        protected override void FillAttributes(System.Collections.IList attributeList) { }
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties() { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties(System.Attribute[] filter) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties(object instance) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection GetChildProperties(object instance, System.Attribute[] filter) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual object GetEditor(System.Type editorBaseType) { return default(object);}
        public override int GetHashCode() { return default(int); }
        protected override object GetInvocationTarget(System.Type type, object instance) { return default(object); }
        protected System.Type GetTypeFromName(string typeName) { return default(System.Type); }
        public abstract object GetValue(object component);
        protected internal System.EventHandler GetValueChangedHandler(object component) { return default(System.EventHandler); }
        protected virtual void OnValueChanged(object component, System.EventArgs e) { }
        public virtual void RemoveValueChanged(object component, System.EventHandler handler) { }
        public abstract void ResetValue(object component);
        public System.ComponentModel.DesignerSerializationVisibility SerializationVisibility { get { return default(System.ComponentModel.DesignerSerializationVisibility); } }
        public abstract void SetValue(object component, object value);
        public abstract bool ShouldSerializeValue(object component);
    }
    public partial class PropertyDescriptorCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IDictionary, System.Collections.IList
    {
        public static readonly System.ComponentModel.PropertyDescriptorCollection Empty;
        public PropertyDescriptorCollection(System.ComponentModel.PropertyDescriptor[] properties) { }
        public PropertyDescriptorCollection(System.ComponentModel.PropertyDescriptor[] properties, bool readOnly) { }
        public int Count { get; }
        bool System.Collections.ICollection.IsSynchronized { get; }
        object System.Collections.ICollection.SyncRoot { get; }
        bool System.Collections.IDictionary.IsFixedSize { get; }
        void System.Collections.IDictionary.Add(object key, object value) { }
        bool System.Collections.IDictionary.Contains(object key) { return default(bool); }
        object System.Collections.IDictionary.this[object key] { get { return default(object); } set { } }
        void System.Collections.IDictionary.Remove(object key) { }
        bool System.Collections.IDictionary.IsReadOnly { get; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { return default(System.Collections.IDictionaryEnumerator); }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get; }
        System.Collections.ICollection System.Collections.IDictionary.Values { get; }
        bool System.Collections.IList.IsFixedSize { get; }
        bool System.Collections.IList.IsReadOnly { get; }
        int System.Collections.IList.Add(object value) { return default(int); }
        void System.Collections.IList.Remove(object value) { }
        bool System.Collections.IList.Contains(object value) { return default(bool); }
        int System.Collections.IList.IndexOf(object value) { return default(int); }
        void System.Collections.IList.Insert(int index, object value) { }
        object System.Collections.IList.this[int index] { get { return default(object); } set { } }
        public virtual System.ComponentModel.PropertyDescriptor this[int index] { get { return default(System.ComponentModel.PropertyDescriptor); } }
        public virtual System.ComponentModel.PropertyDescriptor this[string name] { get { return default(System.ComponentModel.PropertyDescriptor); } }
        public int Add(System.ComponentModel.PropertyDescriptor value) { return default(int); }
        public void Clear() { }
        public bool Contains(System.ComponentModel.PropertyDescriptor value) { return default(bool); }
        public void CopyTo(System.Array array, int index) { }
        public virtual System.ComponentModel.PropertyDescriptor Find(string name, bool ignoreCase) { return default(System.ComponentModel.PropertyDescriptor); }
        public virtual System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        public int IndexOf(System.ComponentModel.PropertyDescriptor value) { return default(int); }
        public void Insert(int index, System.ComponentModel.PropertyDescriptor value) { }
        protected void InternalSort(System.Collections.IComparer sorter) { }
        protected void InternalSort(string[] names) { }
        public void Remove(System.ComponentModel.PropertyDescriptor value) { }
        public void RemoveAt(int index) { }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort() { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(System.Collections.IComparer comparer) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(string[] names) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(string[] names, System.Collections.IComparer comparer) { return default(System.ComponentModel.PropertyDescriptorCollection); }
    }
    public sealed partial class ProvidePropertyAttribute : System.Attribute
    {
        public ProvidePropertyAttribute(string propertyName, string receiverTypeName) { }
        public ProvidePropertyAttribute(string propertyName, System.Type receiverType) { }
        public string PropertyName { get; }
        public string ReceiverTypeName { get; }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public partial class RefreshEventArgs : System.EventArgs
    {
        public RefreshEventArgs(object componentChanged) { }
        public RefreshEventArgs(System.Type typeChanged) { }
        public object ComponentChanged { get; }
        public System.Type TypeChanged { get; }
    }
    public delegate void RefreshEventHandler(System.ComponentModel.RefreshEventArgs e);
    public partial class SByteConverter : System.ComponentModel.BaseNumberConverter
    {
        public SByteConverter() { }
    }
    public partial class SingleConverter : System.ComponentModel.BaseNumberConverter
    {
        public SingleConverter() { }
    }
    public partial class StringConverter : System.ComponentModel.TypeConverter
    {
        public StringConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
    }
    public partial class TimeSpanConverter : System.ComponentModel.TypeConverter
    {
        public TimeSpanConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class TypeConverter
    {
        public TypeConverter() { }
        public virtual bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public bool CanConvertFrom(System.Type sourceType) { return default(bool); }
        public virtual bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public bool CanConvertTo(System.Type destinationType) { return default(bool); }
        public virtual object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public object ConvertFrom(object value) { return default(object); }
        public object ConvertFromInvariantString(System.ComponentModel.ITypeDescriptorContext context, string text) { return default(object); }
        public object ConvertFromInvariantString(string text) { return default(object); }
        public object ConvertFromString(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, string text) { return default(object); }
        public object ConvertFromString(System.ComponentModel.ITypeDescriptorContext context, string text) { return default(object); }
        public object ConvertFromString(string text) { return default(object); }
        public virtual object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public object ConvertTo(object value, System.Type destinationType) { return default(object); }
        public string ConvertToInvariantString(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(string); }
        public string ConvertToInvariantString(object value) { return default(string); }
        public string ConvertToString(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(string); }
        public string ConvertToString(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(string); }
        public string ConvertToString(object value) { return default(string); }
        public object CreateInstance(System.Collections.IDictionary propertyValues) { return default(object); }
        public virtual object CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Collections.IDictionary propertyValues) { return default(object); }
        protected System.Exception GetConvertFromException(object value) { return default(System.Exception); }
        protected System.Exception GetConvertToException(object value, System.Type destinationType) { return default(System.Exception); }
        public bool GetCreateInstanceSupported() { return default(bool); }
        public virtual bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public virtual System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public System.ComponentModel.PropertyDescriptorCollection GetProperties(object value) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public bool GetPropertiesSupported() { return default(bool); }
        public virtual bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public System.Collections.ICollection GetStandardValues() { return default(System.Collections.ICollection); }
        public virtual System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { return default(System.ComponentModel.TypeConverter.StandardValuesCollection); }
        public bool GetStandardValuesExclusive() { return default(bool); }
        public virtual bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public bool GetStandardValuesSupported() { return default(bool); }
        public virtual bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public virtual bool IsValid(System.ComponentModel.ITypeDescriptorContext context, object value) { return default(bool); }
        public bool IsValid(object value) { return default(bool); }
        protected System.ComponentModel.PropertyDescriptorCollection SortProperties(System.ComponentModel.PropertyDescriptorCollection props, string[] names) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        protected abstract partial class SimplePropertyDescriptor : System.ComponentModel.PropertyDescriptor
        {
            protected SimplePropertyDescriptor(System.Type componentType, string name, System.Type propertyType) : base(default(System.ComponentModel.MemberDescriptor)) { }
            protected SimplePropertyDescriptor(System.Type componentType, string name, System.Type propertyType, System.Attribute[] attributes) : base(default(System.ComponentModel.MemberDescriptor)) { }
            public override System.Type ComponentType { get; }
            public override bool IsReadOnly { get; }
            public override System.Type PropertyType { get; }
            public override bool CanResetValue(object component) { return default(bool); }
            public override void ResetValue(object component) { }
            public override bool ShouldSerializeValue(object component) { return default(bool); }
        }
        public partial class StandardValuesCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public StandardValuesCollection(System.Collections.ICollection values) { }
            public int Count { get; }
            bool System.Collections.ICollection.IsSynchronized { get; }
            object System.Collections.ICollection.SyncRoot { get; }
            public object this[int index] { get { return default(object); } }
            public void CopyTo(System.Array array, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { return default(System.Collections.IEnumerator); }
        }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767))]
    public sealed partial class TypeConverterAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.TypeConverterAttribute Default;
        public TypeConverterAttribute() { }
        public TypeConverterAttribute(string typeName) { }
        public TypeConverterAttribute(System.Type type) { }
        public string ConverterTypeName { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public abstract partial class TypeDescriptionProvider
    {
        protected TypeDescriptionProvider() { }
        protected TypeDescriptionProvider(System.ComponentModel.TypeDescriptionProvider parent) { }
        public virtual object CreateInstance(System.IServiceProvider provider, System.Type objectType, System.Type[] argTypes, object[] args) { return default(object); }
        public virtual System.Collections.IDictionary GetCache(object instance) { return default(System.Collections.IDictionary); }
        public virtual System.ComponentModel.ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance) { return default(System.ComponentModel.ICustomTypeDescriptor); }
        protected internal virtual System.ComponentModel.IExtenderProvider[] GetExtenderProviders(object instance) { return default(System.ComponentModel.IExtenderProvider[]); }
        public virtual string GetFullComponentName(object component) { return default(string); }
        public System.Type GetReflectionType(object instance) { return default(System.Type); }
        public System.Type GetReflectionType(System.Type objectType) { return default(System.Type); }
        public virtual System.Type GetReflectionType(System.Type objectType, object instance) { return default(System.Type); }
        public virtual System.Type GetRuntimeType(System.Type reflectionType) { return default(System.Type); }
        public System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(object instance) { return default(System.ComponentModel.ICustomTypeDescriptor); }
        public System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType) { return default(System.ComponentModel.ICustomTypeDescriptor); }
        public virtual System.ComponentModel.ICustomTypeDescriptor GetTypeDescriptor(System.Type objectType, object instance) { return default(System.ComponentModel.ICustomTypeDescriptor); }
        public virtual bool IsSupportedType(System.Type type) { return default(bool); }
    }
    public sealed partial class TypeDescriptionProviderAttribute : System.Attribute
    {
        public TypeDescriptionProviderAttribute(string typeName) { }
        public TypeDescriptionProviderAttribute(System.Type type) { }
        public string TypeName { get; }
    }
    public sealed partial class TypeDescriptor
    {
        internal TypeDescriptor() { }
        public static System.Type InterfaceType { get; }
        public static event System.ComponentModel.RefreshEventHandler Refreshed { add { } remove { } }
        public static System.ComponentModel.TypeDescriptionProvider AddAttributes(object instance, params System.Attribute[] attributes) { return default(System.ComponentModel.TypeDescriptionProvider); }
        public static System.ComponentModel.TypeDescriptionProvider AddAttributes(System.Type type, params System.Attribute[] attributes) { return default(System.ComponentModel.TypeDescriptionProvider); }
        public static void AddEditorTable(System.Type editorBaseType, System.Collections.Hashtable table) { }
        public static void AddProvider(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void AddProvider(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void AddProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void AddProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void CreateAssociation(object primary, object secondary) { }
        public static System.ComponentModel.EventDescriptor CreateEvent(System.Type componentType, System.ComponentModel.EventDescriptor oldEventDescriptor, params System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptor); }
        public static System.ComponentModel.EventDescriptor CreateEvent(System.Type componentType, string name, System.Type type, params System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptor); }
        public static object CreateInstance(System.IServiceProvider provider, System.Type objectType, System.Type[] argTypes, object[] args) { return default(object); }
        public static System.ComponentModel.PropertyDescriptor CreateProperty(System.Type componentType, System.ComponentModel.PropertyDescriptor oldPropertyDescriptor, params System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptor); }
        public static System.ComponentModel.PropertyDescriptor CreateProperty(System.Type componentType, string name, System.Type type, params System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptor); }
        public static object GetAssociation(System.Type type, object primary) { return default(object); }
        public static System.ComponentModel.AttributeCollection GetAttributes(object component) { return default(System.ComponentModel.AttributeCollection); }
        public static System.ComponentModel.AttributeCollection GetAttributes(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.AttributeCollection); }
        public static System.ComponentModel.AttributeCollection GetAttributes(System.Type componentType) { return default(System.ComponentModel.AttributeCollection); }
        public static string GetClassName(object component) { return default(string); }
        public static string GetClassName(object component, bool noCustomTypeDesc) { return default(string); }
        public static string GetClassName(System.Type componentType) { return default(string); }
        public static string GetComponentName(object component) { return default(string); }
        public static string GetComponentName(object component, bool noCustomTypeDesc) { return default(string); }
        public static System.ComponentModel.TypeConverter GetConverter(object component) { return default(System.ComponentModel.TypeConverter); }
        public static System.ComponentModel.TypeConverter GetConverter(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.TypeConverter); }
        public static System.ComponentModel.TypeConverter GetConverter(System.Type type) { return default(System.ComponentModel.TypeConverter); }
        public static System.ComponentModel.EventDescriptor GetDefaultEvent(object component) { return default(System.ComponentModel.EventDescriptor); }
        public static System.ComponentModel.EventDescriptor GetDefaultEvent(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.EventDescriptor); }
        public static System.ComponentModel.EventDescriptor GetDefaultEvent(System.Type componentType) { return default(System.ComponentModel.EventDescriptor); }
        public static System.ComponentModel.PropertyDescriptor GetDefaultProperty(object component) { return default(System.ComponentModel.PropertyDescriptor); }
        public static System.ComponentModel.PropertyDescriptor GetDefaultProperty(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.PropertyDescriptor); }
        public static System.ComponentModel.PropertyDescriptor GetDefaultProperty(System.Type componentType) { return default(System.ComponentModel.PropertyDescriptor); }
        public static object GetEditor(object component, System.Type editorBaseType) { return default(object); }
        public static object GetEditor(object component, System.Type editorBaseType, bool noCustomTypeDesc) { return default(object); }
        public static object GetEditor(System.Type type, System.Type editorBaseType) { return default(object); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, System.Attribute[] attributes, bool noCustomTypeDesc) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(System.Type componentType) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static System.ComponentModel.EventDescriptorCollection GetEvents(System.Type componentType, System.Attribute[] attributes) { return default(System.ComponentModel.EventDescriptorCollection); }
        public static string GetFullComponentName(object component) { return default(string); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, System.Attribute[] attributes, bool noCustomTypeDesc) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, bool noCustomTypeDesc) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Type componentType) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Type componentType, System.Attribute[] attributes) { return default(System.ComponentModel.PropertyDescriptorCollection); }
        public static System.ComponentModel.TypeDescriptionProvider GetProvider(object instance) { return default(System.ComponentModel.TypeDescriptionProvider); }
        public static System.ComponentModel.TypeDescriptionProvider GetProvider(System.Type type) { return default(System.ComponentModel.TypeDescriptionProvider); }
        public static System.Type GetReflectionType(object instance) { return default(System.Type); }
        public static System.Type GetReflectionType(System.Type type) { return default(System.Type); }
        public static void Refresh(object component) { }
        public static void Refresh(System.Reflection.Assembly assembly) { }
        public static void Refresh(System.Reflection.Module module) { }
        public static void Refresh(System.Type type) { }
        public static void RemoveAssociation(object primary, object secondary) { }
        public static void RemoveAssociations(object primary) { }
        public static void RemoveProvider(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void RemoveProvider(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void RemoveProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void RemoveProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void SortDescriptorArray(System.Collections.IList infos) { }
    }
    public abstract partial class TypeListConverter : System.ComponentModel.TypeConverter
    {
        protected TypeListConverter(System.Type[] types) { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { return default(System.ComponentModel.TypeConverter.StandardValuesCollection); }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { return default(bool); }
    }
    public partial class UInt16Converter : System.ComponentModel.BaseNumberConverter
    {
        public UInt16Converter() { }
    }
    public partial class UInt32Converter : System.ComponentModel.BaseNumberConverter
    {
        public UInt32Converter() { }
    }
    public partial class UInt64Converter : System.ComponentModel.BaseNumberConverter
    {
        public UInt64Converter() { }
    }
}

namespace System
{
    public partial class UriTypeConverter : System.ComponentModel.TypeConverter
    {
        public UriTypeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
}
