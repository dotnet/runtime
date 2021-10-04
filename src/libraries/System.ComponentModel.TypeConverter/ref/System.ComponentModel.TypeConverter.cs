// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System
{
    public partial class UriTypeConverter : System.ComponentModel.TypeConverter
    {
        public UriTypeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
    }
}
namespace System.ComponentModel
{
    public partial class AddingNewEventArgs : System.EventArgs
    {
        public AddingNewEventArgs() { }
        public AddingNewEventArgs(object? newObject) { }
        public object? NewObject { get { throw null; } set { } }
    }
    public delegate void AddingNewEventHandler(object? sender, System.ComponentModel.AddingNewEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class AmbientValueAttribute : System.Attribute
    {
        public AmbientValueAttribute(bool value) { }
        public AmbientValueAttribute(byte value) { }
        public AmbientValueAttribute(char value) { }
        public AmbientValueAttribute(double value) { }
        public AmbientValueAttribute(short value) { }
        public AmbientValueAttribute(int value) { }
        public AmbientValueAttribute(long value) { }
        public AmbientValueAttribute(object? value) { }
        public AmbientValueAttribute(float value) { }
        public AmbientValueAttribute(string? value) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public AmbientValueAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, string value) { }
        public object? Value { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class ArrayConverter : System.ComponentModel.CollectionConverter
    {
        public ArrayConverter() { }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        [return: System.Diagnostics.CodeAnalysis.NotNullIfNotNullAttribute("value")]
        public override System.ComponentModel.PropertyDescriptorCollection? GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object? value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class AttributeCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public static readonly System.ComponentModel.AttributeCollection Empty;
        protected AttributeCollection() { }
        public AttributeCollection(params System.Attribute[]? attributes) { }
        protected virtual System.Attribute[] Attributes { get { throw null; } }
        public int Count { get { throw null; } }
        public virtual System.Attribute this[int index] { get { throw null; } }
        public virtual System.Attribute? this[[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type attributeType] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public bool Contains(System.Attribute? attribute) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public bool Contains(System.Attribute[]? attributes) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public static System.ComponentModel.AttributeCollection FromExisting(System.ComponentModel.AttributeCollection existing, params System.Attribute[]? newAttributes) { throw null; }
        protected System.Attribute? GetDefaultAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type attributeType) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public bool Matches(System.Attribute? attribute) { throw null; }
        public bool Matches(System.Attribute[]? attributes) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public partial class AttributeProviderAttribute : System.Attribute
    {
        public AttributeProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] string typeName) { }
        public AttributeProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] string typeName, string propertyName) { }
        public AttributeProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)] System.Type type) { }
        public string? PropertyName { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicEvents | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicNestedTypes | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicProperties)]
        public string? TypeName { get { throw null; } }
    }
    public abstract partial class BaseNumberConverter : System.ComponentModel.TypeConverter
    {
        internal BaseNumberConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class BindableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.BindableAttribute Default;
        public static readonly System.ComponentModel.BindableAttribute No;
        public static readonly System.ComponentModel.BindableAttribute Yes;
        public BindableAttribute(bool bindable) { }
        public BindableAttribute(bool bindable, System.ComponentModel.BindingDirection direction) { }
        public BindableAttribute(System.ComponentModel.BindableSupport flags) { }
        public BindableAttribute(System.ComponentModel.BindableSupport flags, System.ComponentModel.BindingDirection direction) { }
        public bool Bindable { get { throw null; } }
        public System.ComponentModel.BindingDirection Direction { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public enum BindableSupport
    {
        No = 0,
        Yes = 1,
        Default = 2,
    }
    public enum BindingDirection
    {
        OneWay = 0,
        TwoWay = 1,
    }
    public partial class BindingList<[System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] T> : System.Collections.ObjectModel.Collection<T>, System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ComponentModel.IBindingList, System.ComponentModel.ICancelAddNew, System.ComponentModel.IRaiseItemChangedEvents
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        public BindingList() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Raises ListChanged events with PropertyDescriptors. PropertyDescriptors require unreferenced code.")]
        public BindingList(System.Collections.Generic.IList<T> list) { }
        public bool AllowEdit { get { throw null; } set { } }
        public bool AllowNew { get { throw null; } set { } }
        public bool AllowRemove { get { throw null; } set { } }
        protected virtual bool IsSortedCore { get { throw null; } }
        public bool RaiseListChangedEvents { get { throw null; } set { } }
        protected virtual System.ComponentModel.ListSortDirection SortDirectionCore { get { throw null; } }
        protected virtual System.ComponentModel.PropertyDescriptor? SortPropertyCore { get { throw null; } }
        protected virtual bool SupportsChangeNotificationCore { get { throw null; } }
        protected virtual bool SupportsSearchingCore { get { throw null; } }
        protected virtual bool SupportsSortingCore { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowEdit { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowNew { get { throw null; } }
        bool System.ComponentModel.IBindingList.AllowRemove { get { throw null; } }
        bool System.ComponentModel.IBindingList.IsSorted { get { throw null; } }
        System.ComponentModel.ListSortDirection System.ComponentModel.IBindingList.SortDirection { get { throw null; } }
        System.ComponentModel.PropertyDescriptor? System.ComponentModel.IBindingList.SortProperty { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsChangeNotification { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSearching { get { throw null; } }
        bool System.ComponentModel.IBindingList.SupportsSorting { get { throw null; } }
        bool System.ComponentModel.IRaiseItemChangedEvents.RaisesItemChangedEvents { get { throw null; } }
        public event System.ComponentModel.AddingNewEventHandler AddingNew { add { } remove { } }
        public event System.ComponentModel.ListChangedEventHandler ListChanged { add { } remove { } }
        public T AddNew() { throw null; }
        protected virtual object? AddNewCore() { throw null; }
        protected virtual void ApplySortCore(System.ComponentModel.PropertyDescriptor prop, System.ComponentModel.ListSortDirection direction) { }
        public virtual void CancelNew(int itemIndex) { }
        protected override void ClearItems() { }
        public virtual void EndNew(int itemIndex) { }
        protected virtual int FindCore(System.ComponentModel.PropertyDescriptor prop, object key) { throw null; }
        protected override void InsertItem(int index, T item) { }
        protected virtual void OnAddingNew(System.ComponentModel.AddingNewEventArgs e) { }
        protected virtual void OnListChanged(System.ComponentModel.ListChangedEventArgs e) { }
        protected override void RemoveItem(int index) { }
        protected virtual void RemoveSortCore() { }
        public void ResetBindings() { }
        public void ResetItem(int position) { }
        protected override void SetItem(int index, T item) { }
        void System.ComponentModel.IBindingList.AddIndex(System.ComponentModel.PropertyDescriptor prop) { }
        object System.ComponentModel.IBindingList.AddNew() { throw null; }
        void System.ComponentModel.IBindingList.ApplySort(System.ComponentModel.PropertyDescriptor prop, System.ComponentModel.ListSortDirection direction) { }
        int System.ComponentModel.IBindingList.Find(System.ComponentModel.PropertyDescriptor prop, object key) { throw null; }
        void System.ComponentModel.IBindingList.RemoveIndex(System.ComponentModel.PropertyDescriptor prop) { }
        void System.ComponentModel.IBindingList.RemoveSort() { }
    }
    public partial class BooleanConverter : System.ComponentModel.TypeConverter
    {
        public BooleanConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class ByteConverter : System.ComponentModel.BaseNumberConverter
    {
        public ByteConverter() { }
    }
    public delegate void CancelEventHandler(object? sender, System.ComponentModel.CancelEventArgs e);
    public partial class CharConverter : System.ComponentModel.TypeConverter
    {
        public CharConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    public enum CollectionChangeAction
    {
        Add = 1,
        Remove = 2,
        Refresh = 3,
    }
    public partial class CollectionChangeEventArgs : System.EventArgs
    {
        public CollectionChangeEventArgs(System.ComponentModel.CollectionChangeAction action, object? element) { }
        public virtual System.ComponentModel.CollectionChangeAction Action { get { throw null; } }
        public virtual object? Element { get { throw null; } }
    }
    public delegate void CollectionChangeEventHandler(object? sender, System.ComponentModel.CollectionChangeEventArgs e);
    public partial class CollectionConverter : System.ComponentModel.TypeConverter
    {
        public CollectionConverter() { }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class ComplexBindingPropertiesAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ComplexBindingPropertiesAttribute Default;
        public ComplexBindingPropertiesAttribute() { }
        public ComplexBindingPropertiesAttribute(string? dataSource) { }
        public ComplexBindingPropertiesAttribute(string? dataSource, string? dataMember) { }
        public string? DataMember { get { throw null; } }
        public string? DataSource { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class ComponentConverter : System.ComponentModel.ReferenceConverter
    {
        public ComponentConverter(System.Type type) : base (default(System.Type)) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public abstract partial class ComponentEditor
    {
        protected ComponentEditor() { }
        public abstract bool EditComponent(System.ComponentModel.ITypeDescriptorContext? context, object component);
        public bool EditComponent(object component) { throw null; }
    }
    public partial class ComponentResourceManager : System.Resources.ResourceManager
    {
        public ComponentResourceManager() { }
        public ComponentResourceManager(System.Type t) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered.")]
        public void ApplyResources(object value, string objectName) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered.")]
        public virtual void ApplyResources(object value, string objectName, System.Globalization.CultureInfo? culture) { }
    }
    public partial class Container : System.ComponentModel.IContainer, System.IDisposable
    {
        public Container() { }
        public virtual System.ComponentModel.ComponentCollection Components { get { throw null; } }
        public virtual void Add(System.ComponentModel.IComponent? component) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of components in the container cannot be statically discovered to validate the name.")]
        public virtual void Add(System.ComponentModel.IComponent? component, string? name) { }
        protected virtual System.ComponentModel.ISite CreateSite(System.ComponentModel.IComponent component, string? name) { throw null; }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~Container() { }
        protected virtual object? GetService(System.Type service) { throw null; }
        public virtual void Remove(System.ComponentModel.IComponent? component) { }
        protected void RemoveWithoutUnsiting(System.ComponentModel.IComponent? component) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of components in the container cannot be statically discovered.")]
        protected virtual void ValidateName(System.ComponentModel.IComponent component, string? name) { }
    }
    public abstract partial class ContainerFilterService
    {
        protected ContainerFilterService() { }
        public virtual System.ComponentModel.ComponentCollection FilterComponents(System.ComponentModel.ComponentCollection components) { throw null; }
    }
    public partial class CultureInfoConverter : System.ComponentModel.TypeConverter
    {
        public CultureInfoConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        protected virtual string GetCultureName(System.Globalization.CultureInfo culture) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public abstract partial class CustomTypeDescriptor : System.ComponentModel.ICustomTypeDescriptor
    {
        protected CustomTypeDescriptor() { }
        protected CustomTypeDescriptor(System.ComponentModel.ICustomTypeDescriptor? parent) { }
        public virtual System.ComponentModel.AttributeCollection GetAttributes() { throw null; }
        public virtual string? GetClassName() { throw null; }
        public virtual string? GetComponentName() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public virtual System.ComponentModel.TypeConverter GetConverter() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        public virtual System.ComponentModel.EventDescriptor? GetDefaultEvent() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public virtual System.ComponentModel.PropertyDescriptor? GetDefaultProperty() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
        public virtual object? GetEditor(System.Type editorBaseType) { throw null; }
        public virtual System.ComponentModel.EventDescriptorCollection GetEvents() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public virtual System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[]? attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public virtual System.ComponentModel.PropertyDescriptorCollection GetProperties() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public virtual System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[]? attributes) { throw null; }
        public virtual object? GetPropertyOwner(System.ComponentModel.PropertyDescriptor? pd) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class DataObjectAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DataObjectAttribute DataObject;
        public static readonly System.ComponentModel.DataObjectAttribute Default;
        public static readonly System.ComponentModel.DataObjectAttribute NonDataObject;
        public DataObjectAttribute() { }
        public DataObjectAttribute(bool isDataObject) { }
        public bool IsDataObject { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public sealed partial class DataObjectFieldAttribute : System.Attribute
    {
        public DataObjectFieldAttribute(bool primaryKey) { }
        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity) { }
        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable) { }
        public DataObjectFieldAttribute(bool primaryKey, bool isIdentity, bool isNullable, int length) { }
        public bool IsIdentity { get { throw null; } }
        public bool IsNullable { get { throw null; } }
        public int Length { get { throw null; } }
        public bool PrimaryKey { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Method)]
    public sealed partial class DataObjectMethodAttribute : System.Attribute
    {
        public DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType methodType) { }
        public DataObjectMethodAttribute(System.ComponentModel.DataObjectMethodType methodType, bool isDefault) { }
        public bool IsDefault { get { throw null; } }
        public System.ComponentModel.DataObjectMethodType MethodType { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool Match([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
    }
    public enum DataObjectMethodType
    {
        Fill = 0,
        Select = 1,
        Update = 2,
        Insert = 3,
        Delete = 4,
    }
    public partial class DateTimeConverter : System.ComponentModel.TypeConverter
    {
        public DateTimeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    public partial class DateTimeOffsetConverter : System.ComponentModel.TypeConverter
    {
        public DateTimeOffsetConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    public partial class DecimalConverter : System.ComponentModel.BaseNumberConverter
    {
        public DecimalConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class DefaultBindingPropertyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DefaultBindingPropertyAttribute Default;
        public DefaultBindingPropertyAttribute() { }
        public DefaultBindingPropertyAttribute(string? name) { }
        public string? Name { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class DefaultEventAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DefaultEventAttribute Default;
        public DefaultEventAttribute(string? name) { }
        public string? Name { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class DefaultPropertyAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DefaultPropertyAttribute Default;
        public DefaultPropertyAttribute(string? name) { }
        public string? Name { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface)]
    public sealed partial class DesignTimeVisibleAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.DesignTimeVisibleAttribute Default;
        public static readonly System.ComponentModel.DesignTimeVisibleAttribute No;
        public static readonly System.ComponentModel.DesignTimeVisibleAttribute Yes;
        public DesignTimeVisibleAttribute() { }
        public DesignTimeVisibleAttribute(bool visible) { }
        public bool Visible { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public partial class DoubleConverter : System.ComponentModel.BaseNumberConverter
    {
        public DoubleConverter() { }
    }
    public partial class EnumConverter : System.ComponentModel.TypeConverter
    {
        public EnumConverter([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type type) { }
        protected virtual System.Collections.IComparer Comparer { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        protected System.Type EnumType { get { throw null; } }
        protected System.ComponentModel.TypeConverter.StandardValuesCollection? Values { get { throw null; } set { } }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
    }
    public abstract partial class EventDescriptor : System.ComponentModel.MemberDescriptor
    {
        protected EventDescriptor(System.ComponentModel.MemberDescriptor descr) : base (default(string)) { }
        protected EventDescriptor(System.ComponentModel.MemberDescriptor descr, System.Attribute[]? attrs) : base (default(string)) { }
        protected EventDescriptor(string name, System.Attribute[]? attrs) : base (default(string)) { }
        public abstract System.Type ComponentType { get; }
        public abstract System.Type EventType { get; }
        public abstract bool IsMulticast { get; }
        public abstract void AddEventHandler(object component, System.Delegate value);
        public abstract void RemoveEventHandler(object component, System.Delegate value);
    }
    public partial class EventDescriptorCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public static readonly System.ComponentModel.EventDescriptorCollection Empty;
        public EventDescriptorCollection(System.ComponentModel.EventDescriptor[]? events) { }
        public EventDescriptorCollection(System.ComponentModel.EventDescriptor[]? events, bool readOnly) { }
        public int Count { get { throw null; } }
        public virtual System.ComponentModel.EventDescriptor? this[int index] { get { throw null; } }
        public virtual System.ComponentModel.EventDescriptor? this[string name] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(System.ComponentModel.EventDescriptor? value) { throw null; }
        public void Clear() { }
        public bool Contains(System.ComponentModel.EventDescriptor? value) { throw null; }
        public virtual System.ComponentModel.EventDescriptor? Find(string name, bool ignoreCase) { throw null; }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.ComponentModel.EventDescriptor? value) { throw null; }
        public void Insert(int index, System.ComponentModel.EventDescriptor? value) { }
        protected void InternalSort(System.Collections.IComparer? sorter) { }
        protected void InternalSort(string[]? names) { }
        public void Remove(System.ComponentModel.EventDescriptor? value) { }
        public void RemoveAt(int index) { }
        public virtual System.ComponentModel.EventDescriptorCollection Sort() { throw null; }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(System.Collections.IComparer comparer) { throw null; }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(string[] names) { throw null; }
        public virtual System.ComponentModel.EventDescriptorCollection Sort(string[] names, System.Collections.IComparer comparer) { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array? array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    public partial class ExpandableObjectConverter : System.ComponentModel.TypeConverter
    {
        public ExpandableObjectConverter() { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class ExtenderProvidedPropertyAttribute : System.Attribute
    {
        public ExtenderProvidedPropertyAttribute() { }
        public System.ComponentModel.PropertyDescriptor? ExtenderProperty { get { throw null; } }
        public System.ComponentModel.IExtenderProvider? Provider { get { throw null; } }
        public System.Type? ReceiverType { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public partial class GuidConverter : System.ComponentModel.TypeConverter
    {
        public GuidConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    public partial class HandledEventArgs : System.EventArgs
    {
        public HandledEventArgs() { }
        public HandledEventArgs(bool defaultHandledValue) { }
        public bool Handled { get { throw null; } set { } }
    }
    public delegate void HandledEventHandler(object? sender, System.ComponentModel.HandledEventArgs e);
    public partial interface IBindingList : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        bool AllowEdit { get; }
        bool AllowNew { get; }
        bool AllowRemove { get; }
        bool IsSorted { get; }
        System.ComponentModel.ListSortDirection SortDirection { get; }
        System.ComponentModel.PropertyDescriptor? SortProperty { get; }
        bool SupportsChangeNotification { get; }
        bool SupportsSearching { get; }
        bool SupportsSorting { get; }
        event System.ComponentModel.ListChangedEventHandler ListChanged;
        void AddIndex(System.ComponentModel.PropertyDescriptor property);
        object? AddNew();
        void ApplySort(System.ComponentModel.PropertyDescriptor property, System.ComponentModel.ListSortDirection direction);
        int Find(System.ComponentModel.PropertyDescriptor property, object key);
        void RemoveIndex(System.ComponentModel.PropertyDescriptor property);
        void RemoveSort();
    }
    public partial interface IBindingListView : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList, System.ComponentModel.IBindingList
    {
        string? Filter { get; [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Members of types used in the filter expression might be trimmed.")] set; }
        System.ComponentModel.ListSortDescriptionCollection SortDescriptions { get; }
        bool SupportsAdvancedSorting { get; }
        bool SupportsFiltering { get; }
        void ApplySort(System.ComponentModel.ListSortDescriptionCollection sorts);
        void RemoveFilter();
    }
    public partial interface ICancelAddNew
    {
        void CancelNew(int itemIndex);
        void EndNew(int itemIndex);
    }
    public partial interface IComNativeDescriptorHandler
    {
        System.ComponentModel.AttributeCollection GetAttributes(object component);
        string GetClassName(object component);
        System.ComponentModel.TypeConverter GetConverter(object component);
        System.ComponentModel.EventDescriptor GetDefaultEvent(object component);
        System.ComponentModel.PropertyDescriptor GetDefaultProperty(object component);
        object GetEditor(object component, System.Type baseEditorType);
        System.ComponentModel.EventDescriptorCollection GetEvents(object component);
        System.ComponentModel.EventDescriptorCollection GetEvents(object component, System.Attribute[]? attributes);
        string GetName(object component);
        System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, System.Attribute[]? attributes);
        object GetPropertyValue(object component, int dispid, ref bool success);
        object GetPropertyValue(object component, string propertyName, ref bool success);
    }
    public partial interface ICustomTypeDescriptor
    {
        System.ComponentModel.AttributeCollection GetAttributes();
        string? GetClassName();
        string? GetComponentName();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        System.ComponentModel.TypeConverter GetConverter();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        System.ComponentModel.EventDescriptor? GetDefaultEvent();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        System.ComponentModel.PropertyDescriptor? GetDefaultProperty();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
        object? GetEditor(System.Type editorBaseType);
        System.ComponentModel.EventDescriptorCollection GetEvents();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        System.ComponentModel.EventDescriptorCollection GetEvents(System.Attribute[]? attributes);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        System.ComponentModel.PropertyDescriptorCollection GetProperties();
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        System.ComponentModel.PropertyDescriptorCollection GetProperties(System.Attribute[]? attributes);
        object? GetPropertyOwner(System.ComponentModel.PropertyDescriptor? pd);
    }
    public partial interface IDataErrorInfo
    {
        string Error { get; }
        string this[string columnName] { get; }
    }
    public partial interface IExtenderProvider
    {
        bool CanExtend(object extendee);
    }
    public partial interface IIntellisenseBuilder
    {
        string Name { get; }
        bool Show(string language, string value, ref string newValue);
    }
    [System.ComponentModel.EditorAttribute("System.Windows.Forms.Design.DataSourceListEditor, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    [System.ComponentModel.MergablePropertyAttribute(false)]
    [System.ComponentModel.TypeConverterAttribute("System.Windows.Forms.Design.DataSourceConverter, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
    public partial interface IListSource
    {
        bool ContainsListCollection { get; }
        System.Collections.IList GetList();
    }
    public partial interface INestedContainer : System.ComponentModel.IContainer, System.IDisposable
    {
        System.ComponentModel.IComponent Owner { get; }
    }
    public partial interface INestedSite : System.ComponentModel.ISite, System.IServiceProvider
    {
        string? FullName { get; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Event | System.AttributeTargets.Field | System.AttributeTargets.Property)]
    public sealed partial class InheritanceAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.InheritanceAttribute Default;
        public static readonly System.ComponentModel.InheritanceAttribute Inherited;
        public static readonly System.ComponentModel.InheritanceAttribute InheritedReadOnly;
        public static readonly System.ComponentModel.InheritanceAttribute NotInherited;
        public InheritanceAttribute() { }
        public InheritanceAttribute(System.ComponentModel.InheritanceLevel inheritanceLevel) { }
        public System.ComponentModel.InheritanceLevel InheritanceLevel { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
        public override string ToString() { throw null; }
    }
    public enum InheritanceLevel
    {
        Inherited = 1,
        InheritedReadOnly = 2,
        NotInherited = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public partial class InstallerTypeAttribute : System.Attribute
    {
        public InstallerTypeAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName) { }
        public InstallerTypeAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type installerType) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public virtual System.Type? InstallerType { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public abstract partial class InstanceCreationEditor
    {
        protected InstanceCreationEditor() { }
        public virtual string Text { get { throw null; } }
        public abstract object? CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Type instanceType);
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
    public partial interface IRaiseItemChangedEvents
    {
        bool RaisesItemChangedEvents { get; }
    }
    public partial interface ISupportInitializeNotification : System.ComponentModel.ISupportInitialize
    {
        bool IsInitialized { get; }
        event System.EventHandler Initialized;
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
    public abstract partial class License : System.IDisposable
    {
        protected License() { }
        public abstract string LicenseKey { get; }
        public abstract void Dispose();
    }
    public partial class LicenseContext : System.IServiceProvider
    {
        public LicenseContext() { }
        public virtual System.ComponentModel.LicenseUsageMode UsageMode { get { throw null; } }
        public virtual string? GetSavedLicenseKey(System.Type type, System.Reflection.Assembly? resourceAssembly) { throw null; }
        public virtual object? GetService(System.Type type) { throw null; }
        public virtual void SetSavedLicenseKey(System.Type type, string key) { }
    }
    public partial class LicenseException : System.SystemException
    {
        protected LicenseException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public LicenseException(System.Type? type) { }
        public LicenseException(System.Type? type, object? instance) { }
        public LicenseException(System.Type? type, object? instance, string? message) { }
        public LicenseException(System.Type? type, object? instance, string? message, System.Exception? innerException) { }
        public System.Type? LicensedType { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public sealed partial class LicenseManager
    {
        internal LicenseManager() { }
        public static System.ComponentModel.LicenseContext CurrentContext { get { throw null; } set { } }
        public static System.ComponentModel.LicenseUsageMode UsageMode { get { throw null; } }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static object? CreateWithContext([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type, System.ComponentModel.LicenseContext creationContext) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public static object? CreateWithContext([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type, System.ComponentModel.LicenseContext creationContext, object[] args) { throw null; }
        public static bool IsLicensed(System.Type type) { throw null; }
        public static bool IsValid(System.Type type) { throw null; }
        public static bool IsValid(System.Type type, object? instance, out System.ComponentModel.License? license) { throw null; }
        public static void LockContext(object contextUser) { }
        public static void UnlockContext(object contextUser) { }
        public static void Validate(System.Type type) { }
        public static System.ComponentModel.License? Validate(System.Type type, object? instance) { throw null; }
    }
    public abstract partial class LicenseProvider
    {
        protected LicenseProvider() { }
        public abstract System.ComponentModel.License? GetLicense(System.ComponentModel.LicenseContext context, System.Type type, object? instance, bool allowExceptions);
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=false, Inherited=false)]
    public sealed partial class LicenseProviderAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.LicenseProviderAttribute Default;
        public LicenseProviderAttribute() { }
        public LicenseProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string? typeName) { }
        public LicenseProviderAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type type) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public System.Type? LicenseProvider { get { throw null; } }
        public override object TypeId { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? value) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public enum LicenseUsageMode
    {
        Runtime = 0,
        Designtime = 1,
    }
    public partial class LicFileLicenseProvider : System.ComponentModel.LicenseProvider
    {
        public LicFileLicenseProvider() { }
        protected virtual string GetKey(System.Type type) { throw null; }
        public override System.ComponentModel.License? GetLicense(System.ComponentModel.LicenseContext context, System.Type type, object? instance, bool allowExceptions) { throw null; }
        protected virtual bool IsKeyValid(string? key, System.Type type) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class ListBindableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ListBindableAttribute Default;
        public static readonly System.ComponentModel.ListBindableAttribute No;
        public static readonly System.ComponentModel.ListBindableAttribute Yes;
        public ListBindableAttribute(bool listBindable) { }
        public ListBindableAttribute(System.ComponentModel.BindableSupport flags) { }
        public bool ListBindable { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public partial class ListChangedEventArgs : System.EventArgs
    {
        public ListChangedEventArgs(System.ComponentModel.ListChangedType listChangedType, System.ComponentModel.PropertyDescriptor? propDesc) { }
        public ListChangedEventArgs(System.ComponentModel.ListChangedType listChangedType, int newIndex) { }
        public ListChangedEventArgs(System.ComponentModel.ListChangedType listChangedType, int newIndex, System.ComponentModel.PropertyDescriptor? propDesc) { }
        public ListChangedEventArgs(System.ComponentModel.ListChangedType listChangedType, int newIndex, int oldIndex) { }
        public System.ComponentModel.ListChangedType ListChangedType { get { throw null; } }
        public int NewIndex { get { throw null; } }
        public int OldIndex { get { throw null; } }
        public System.ComponentModel.PropertyDescriptor? PropertyDescriptor { get { throw null; } }
    }
    public delegate void ListChangedEventHandler(object? sender, System.ComponentModel.ListChangedEventArgs e);
    public enum ListChangedType
    {
        Reset = 0,
        ItemAdded = 1,
        ItemDeleted = 2,
        ItemMoved = 3,
        ItemChanged = 4,
        PropertyDescriptorAdded = 5,
        PropertyDescriptorDeleted = 6,
        PropertyDescriptorChanged = 7,
    }
    public partial class ListSortDescription
    {
        public ListSortDescription(System.ComponentModel.PropertyDescriptor? property, System.ComponentModel.ListSortDirection direction) { }
        public System.ComponentModel.PropertyDescriptor? PropertyDescriptor { get { throw null; } set { } }
        public System.ComponentModel.ListSortDirection SortDirection { get { throw null; } set { } }
    }
    public partial class ListSortDescriptionCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
    {
        public ListSortDescriptionCollection() { }
        public ListSortDescriptionCollection(System.ComponentModel.ListSortDescription?[]? sorts) { }
        public int Count { get { throw null; } }
        public System.ComponentModel.ListSortDescription? this[int index] { get { throw null; } set { } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public bool Contains(object? value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public int IndexOf(object? value) { throw null; }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    public enum ListSortDirection
    {
        Ascending = 0,
        Descending = 1,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public sealed partial class LookupBindingPropertiesAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.LookupBindingPropertiesAttribute Default;
        public LookupBindingPropertiesAttribute() { }
        public LookupBindingPropertiesAttribute(string dataSource, string displayMember, string valueMember, string lookupMember) { }
        public string? DataSource { get { throw null; } }
        public string? DisplayMember { get { throw null; } }
        public string? LookupMember { get { throw null; } }
        public string? ValueMember { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.ComponentModel.DesignerAttribute("System.Windows.Forms.Design.ComponentDocumentDesigner, System.Design, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", "System.ComponentModel.Design.IRootDesigner, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089")]
    [System.ComponentModel.DesignerCategoryAttribute("Component")]
    [System.ComponentModel.TypeConverterAttribute(typeof(System.ComponentModel.ComponentConverter))]
    public partial class MarshalByValueComponent : System.ComponentModel.IComponent, System.IDisposable, System.IServiceProvider
    {
        public MarshalByValueComponent() { }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public virtual System.ComponentModel.IContainer? Container { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public virtual bool DesignMode { get { throw null; } }
        protected System.ComponentModel.EventHandlerList Events { get { throw null; } }
        [System.ComponentModel.BrowsableAttribute(false)]
        [System.ComponentModel.DesignerSerializationVisibilityAttribute(System.ComponentModel.DesignerSerializationVisibility.Hidden)]
        public virtual System.ComponentModel.ISite? Site { get { throw null; } set { } }
        public event System.EventHandler? Disposed { add { } remove { } }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        ~MarshalByValueComponent() { }
        public virtual object? GetService(System.Type service) { throw null; }
        public override string? ToString() { throw null; }
    }
    [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
    public partial class MaskedTextProvider : System.ICloneable
    {
        public MaskedTextProvider(string mask) { }
        public MaskedTextProvider(string mask, bool restrictToAscii) { }
        public MaskedTextProvider(string mask, char passwordChar, bool allowPromptAsInput) { }
        public MaskedTextProvider(string mask, System.Globalization.CultureInfo? culture) { }
        public MaskedTextProvider(string mask, System.Globalization.CultureInfo? culture, bool restrictToAscii) { }
        public MaskedTextProvider(string mask, System.Globalization.CultureInfo? culture, bool allowPromptAsInput, char promptChar, char passwordChar, bool restrictToAscii) { }
        public MaskedTextProvider(string mask, System.Globalization.CultureInfo? culture, char passwordChar, bool allowPromptAsInput) { }
        public bool AllowPromptAsInput { get { throw null; } }
        public bool AsciiOnly { get { throw null; } }
        public int AssignedEditPositionCount { get { throw null; } }
        public int AvailableEditPositionCount { get { throw null; } }
        public System.Globalization.CultureInfo Culture { get { throw null; } }
        public static char DefaultPasswordChar { get { throw null; } }
        public int EditPositionCount { get { throw null; } }
        public System.Collections.IEnumerator EditPositions { get { throw null; } }
        public bool IncludeLiterals { get { throw null; } set { } }
        public bool IncludePrompt { get { throw null; } set { } }
        public static int InvalidIndex { get { throw null; } }
        public bool IsPassword { get { throw null; } set { } }
        public char this[int index] { get { throw null; } }
        public int LastAssignedPosition { get { throw null; } }
        public int Length { get { throw null; } }
        public string Mask { get { throw null; } }
        public bool MaskCompleted { get { throw null; } }
        public bool MaskFull { get { throw null; } }
        public char PasswordChar { get { throw null; } set { } }
        public char PromptChar { get { throw null; } set { } }
        public bool ResetOnPrompt { get { throw null; } set { } }
        public bool ResetOnSpace { get { throw null; } set { } }
        public bool SkipLiterals { get { throw null; } set { } }
        public bool Add(char input) { throw null; }
        public bool Add(char input, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Add(string input) { throw null; }
        public bool Add(string input, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public void Clear() { }
        public void Clear(out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public object Clone() { throw null; }
        public int FindAssignedEditPositionFrom(int position, bool direction) { throw null; }
        public int FindAssignedEditPositionInRange(int startPosition, int endPosition, bool direction) { throw null; }
        public int FindEditPositionFrom(int position, bool direction) { throw null; }
        public int FindEditPositionInRange(int startPosition, int endPosition, bool direction) { throw null; }
        public int FindNonEditPositionFrom(int position, bool direction) { throw null; }
        public int FindNonEditPositionInRange(int startPosition, int endPosition, bool direction) { throw null; }
        public int FindUnassignedEditPositionFrom(int position, bool direction) { throw null; }
        public int FindUnassignedEditPositionInRange(int startPosition, int endPosition, bool direction) { throw null; }
        public static bool GetOperationResultFromHint(System.ComponentModel.MaskedTextResultHint hint) { throw null; }
        public bool InsertAt(char input, int position) { throw null; }
        public bool InsertAt(char input, int position, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool InsertAt(string input, int position) { throw null; }
        public bool InsertAt(string input, int position, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool IsAvailablePosition(int position) { throw null; }
        public bool IsEditPosition(int position) { throw null; }
        public static bool IsValidInputChar(char c) { throw null; }
        public static bool IsValidMaskChar(char c) { throw null; }
        public static bool IsValidPasswordChar(char c) { throw null; }
        public bool Remove() { throw null; }
        public bool Remove(out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool RemoveAt(int position) { throw null; }
        public bool RemoveAt(int startPosition, int endPosition) { throw null; }
        public bool RemoveAt(int startPosition, int endPosition, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Replace(char input, int position) { throw null; }
        public bool Replace(char input, int startPosition, int endPosition, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Replace(char input, int position, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Replace(string input, int position) { throw null; }
        public bool Replace(string input, int startPosition, int endPosition, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Replace(string input, int position, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public bool Set(string input) { throw null; }
        public bool Set(string input, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
        public string ToDisplayString() { throw null; }
        public override string ToString() { throw null; }
        public string ToString(bool ignorePasswordChar) { throw null; }
        public string ToString(bool includePrompt, bool includeLiterals) { throw null; }
        public string ToString(bool ignorePasswordChar, bool includePrompt, bool includeLiterals, int startPosition, int length) { throw null; }
        public string ToString(bool includePrompt, bool includeLiterals, int startPosition, int length) { throw null; }
        public string ToString(bool ignorePasswordChar, int startPosition, int length) { throw null; }
        public string ToString(int startPosition, int length) { throw null; }
        public bool VerifyChar(char input, int position, out System.ComponentModel.MaskedTextResultHint hint) { throw null; }
        public bool VerifyEscapeChar(char input, int position) { throw null; }
        public bool VerifyString(string input) { throw null; }
        public bool VerifyString(string input, out int testPosition, out System.ComponentModel.MaskedTextResultHint resultHint) { throw null; }
    }
    public enum MaskedTextResultHint
    {
        PositionOutOfRange = -55,
        NonEditPosition = -54,
        UnavailableEditPosition = -53,
        PromptCharNotAllowed = -52,
        InvalidInput = -51,
        SignedDigitExpected = -5,
        LetterExpected = -4,
        DigitExpected = -3,
        AlphanumericCharacterExpected = -2,
        AsciiCharacterExpected = -1,
        Unknown = 0,
        CharacterEscaped = 1,
        NoEffect = 2,
        SideEffect = 3,
        Success = 4,
    }
    public abstract partial class MemberDescriptor
    {
        protected MemberDescriptor(System.ComponentModel.MemberDescriptor descr) { }
        protected MemberDescriptor(System.ComponentModel.MemberDescriptor oldMemberDescriptor, System.Attribute[]? newAttributes) { }
        protected MemberDescriptor(string name) { }
        protected MemberDescriptor(string name, System.Attribute[]? attributes) { }
        protected virtual System.Attribute[]? AttributeArray { get { throw null; } set { } }
        public virtual System.ComponentModel.AttributeCollection Attributes { get { throw null; } }
        public virtual string Category { get { throw null; } }
        public virtual string Description { get { throw null; } }
        public virtual bool DesignTimeOnly { get { throw null; } }
        public virtual string DisplayName { get { throw null; } }
        public virtual bool IsBrowsable { get { throw null; } }
        public virtual string Name { get { throw null; } }
        protected virtual int NameHashCode { get { throw null; } }
        protected virtual System.ComponentModel.AttributeCollection CreateAttributeCollection() { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        protected virtual void FillAttributes(System.Collections.IList attributeList) { }
        protected static System.Reflection.MethodInfo? FindMethod([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type componentClass, string name, System.Type[] args, System.Type returnType) { throw null; }
        protected static System.Reflection.MethodInfo? FindMethod([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.NonPublicMethods | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicMethods)] System.Type componentClass, string name, System.Type[] args, System.Type returnType, bool publicOnly) { throw null; }
        public override int GetHashCode() { throw null; }
        protected virtual object? GetInvocationTarget(System.Type type, object instance) { throw null; }
        protected static object GetInvokee(System.Type componentClass, object component) { throw null; }
        protected static System.ComponentModel.ISite? GetSite(object? component) { throw null; }
    }
    public partial class MultilineStringConverter : System.ComponentModel.TypeConverter
    {
        public MultilineStringConverter() { }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection? GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class NestedContainer : System.ComponentModel.Container, System.ComponentModel.IContainer, System.ComponentModel.INestedContainer, System.IDisposable
    {
        public NestedContainer(System.ComponentModel.IComponent owner) { }
        public System.ComponentModel.IComponent Owner { get { throw null; } }
        protected virtual string? OwnerName { get { throw null; } }
        protected override System.ComponentModel.ISite CreateSite(System.ComponentModel.IComponent component, string? name) { throw null; }
        protected override void Dispose(bool disposing) { }
        protected override object? GetService(System.Type service) { throw null; }
    }
    public partial class NullableConverter : System.ComponentModel.TypeConverter
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The UnderlyingType cannot be statically discovered.")]
        public NullableConverter(System.Type type) { }
        public System.Type NullableType { get { throw null; } }
        public System.Type UnderlyingType { get { throw null; } }
        public System.ComponentModel.TypeConverter UnderlyingTypeConverter { get { throw null; } }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override object? CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection? GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection? GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public sealed partial class PasswordPropertyTextAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.PasswordPropertyTextAttribute Default;
        public static readonly System.ComponentModel.PasswordPropertyTextAttribute No;
        public static readonly System.ComponentModel.PasswordPropertyTextAttribute Yes;
        public PasswordPropertyTextAttribute() { }
        public PasswordPropertyTextAttribute(bool password) { }
        public bool Password { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? o) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public abstract partial class PropertyDescriptor : System.ComponentModel.MemberDescriptor
    {
        protected PropertyDescriptor(System.ComponentModel.MemberDescriptor descr) : base (default(string)) { }
        protected PropertyDescriptor(System.ComponentModel.MemberDescriptor descr, System.Attribute[]? attrs) : base (default(string)) { }
        protected PropertyDescriptor(string name, System.Attribute[]? attrs) : base (default(string)) { }
        public abstract System.Type ComponentType { get; }
        public virtual System.ComponentModel.TypeConverter Converter { [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")] get { throw null; } }
        public virtual bool IsLocalizable { get { throw null; } }
        public abstract bool IsReadOnly { get; }
        public abstract System.Type PropertyType { get; }
        public System.ComponentModel.DesignerSerializationVisibility SerializationVisibility { get { throw null; } }
        public virtual bool SupportsChangeEvents { get { throw null; } }
        public virtual void AddValueChanged(object component, System.EventHandler handler) { }
        public abstract bool CanResetValue(object component);
        protected object? CreateInstance([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type type) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        protected override void FillAttributes(System.Collections.IList attributeList) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties(System.Attribute[] filter) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered.")]
        public System.ComponentModel.PropertyDescriptorCollection GetChildProperties(object instance) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of instance cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public virtual System.ComponentModel.PropertyDescriptorCollection GetChildProperties(object? instance, System.Attribute[]? filter) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public virtual object? GetEditor(System.Type editorBaseType) { throw null; }
        public override int GetHashCode() { throw null; }
        protected override object? GetInvocationTarget(System.Type type, object instance) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Calls ComponentType.Assembly.GetType on the non-fully qualified typeName, which the trimmer cannot recognize.")]
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        protected System.Type? GetTypeFromName([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] string? typeName) { throw null; }
        public abstract object? GetValue(object? component);
        protected internal System.EventHandler? GetValueChangedHandler(object component) { throw null; }
        protected virtual void OnValueChanged(object? component, System.EventArgs e) { }
        public virtual void RemoveValueChanged(object component, System.EventHandler handler) { }
        public abstract void ResetValue(object component);
        public abstract void SetValue(object? component, object? value);
        public abstract bool ShouldSerializeValue(object component);
    }
    public partial class PropertyDescriptorCollection : System.Collections.ICollection, System.Collections.IDictionary, System.Collections.IEnumerable, System.Collections.IList
    {
        public static readonly System.ComponentModel.PropertyDescriptorCollection Empty;
        public PropertyDescriptorCollection(System.ComponentModel.PropertyDescriptor[]? properties) { }
        public PropertyDescriptorCollection(System.ComponentModel.PropertyDescriptor[]? properties, bool readOnly) { }
        public int Count { get { throw null; } }
        public virtual System.ComponentModel.PropertyDescriptor this[int index] { get { throw null; } }
        public virtual System.ComponentModel.PropertyDescriptor? this[string name] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        bool System.Collections.IDictionary.IsFixedSize { get { throw null; } }
        bool System.Collections.IDictionary.IsReadOnly { get { throw null; } }
        object? System.Collections.IDictionary.this[object key] { get { throw null; } set { } }
        System.Collections.ICollection System.Collections.IDictionary.Keys { get { throw null; } }
        System.Collections.ICollection System.Collections.IDictionary.Values { get { throw null; } }
        bool System.Collections.IList.IsFixedSize { get { throw null; } }
        bool System.Collections.IList.IsReadOnly { get { throw null; } }
        object? System.Collections.IList.this[int index] { get { throw null; } set { } }
        public int Add(System.ComponentModel.PropertyDescriptor value) { throw null; }
        public void Clear() { }
        public bool Contains(System.ComponentModel.PropertyDescriptor value) { throw null; }
        public void CopyTo(System.Array array, int index) { }
        public virtual System.ComponentModel.PropertyDescriptor? Find(string name, bool ignoreCase) { throw null; }
        public virtual System.Collections.IEnumerator GetEnumerator() { throw null; }
        public int IndexOf(System.ComponentModel.PropertyDescriptor? value) { throw null; }
        public void Insert(int index, System.ComponentModel.PropertyDescriptor value) { }
        protected void InternalSort(System.Collections.IComparer? sorter) { }
        protected void InternalSort(string[]? names) { }
        public void Remove(System.ComponentModel.PropertyDescriptor? value) { }
        public void RemoveAt(int index) { }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort() { throw null; }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(System.Collections.IComparer? comparer) { throw null; }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(string[]? names) { throw null; }
        public virtual System.ComponentModel.PropertyDescriptorCollection Sort(string[]? names, System.Collections.IComparer? comparer) { throw null; }
        void System.Collections.IDictionary.Add(object key, object? value) { }
        void System.Collections.IDictionary.Clear() { }
        bool System.Collections.IDictionary.Contains(object key) { throw null; }
        System.Collections.IDictionaryEnumerator System.Collections.IDictionary.GetEnumerator() { throw null; }
        void System.Collections.IDictionary.Remove(object key) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
        int System.Collections.IList.Add(object? value) { throw null; }
        void System.Collections.IList.Clear() { }
        bool System.Collections.IList.Contains(object? value) { throw null; }
        int System.Collections.IList.IndexOf(object? value) { throw null; }
        void System.Collections.IList.Insert(int index, object? value) { }
        void System.Collections.IList.Remove(object? value) { }
        void System.Collections.IList.RemoveAt(int index) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class PropertyTabAttribute : System.Attribute
    {
        public PropertyTabAttribute() { }
        public PropertyTabAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName) { }
        public PropertyTabAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string tabClassName, System.ComponentModel.PropertyTabScope tabScope) { }
        public PropertyTabAttribute(System.Type tabClass) { }
        public PropertyTabAttribute(System.Type tabClass, System.ComponentModel.PropertyTabScope tabScope) { }
        public System.Type[] TabClasses { get { throw null; } }
        protected string[]? TabClassNames { get { throw null; } }
        public System.ComponentModel.PropertyTabScope[] TabScopes { get { throw null; } }
        public bool Equals(System.ComponentModel.PropertyTabAttribute other) { throw null; }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? other) { throw null; }
        public override int GetHashCode() { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Types referenced by tabClassNames may be trimmed.")]
        protected void InitializeArrays(string[]? tabClassNames, System.ComponentModel.PropertyTabScope[]? tabScopes) { }
        protected void InitializeArrays(System.Type[]? tabClasses, System.ComponentModel.PropertyTabScope[]? tabScopes) { }
    }
    public enum PropertyTabScope
    {
        Static = 0,
        Global = 1,
        Document = 2,
        Component = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true)]
    public sealed partial class ProvidePropertyAttribute : System.Attribute
    {
        public ProvidePropertyAttribute(string propertyName, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string receiverTypeName) { }
        public ProvidePropertyAttribute(string propertyName, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type receiverType) { }
        public string PropertyName { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string ReceiverTypeName { get { throw null; } }
        public override object TypeId { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public partial class RecommendedAsConfigurableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.RecommendedAsConfigurableAttribute Default;
        public static readonly System.ComponentModel.RecommendedAsConfigurableAttribute No;
        public static readonly System.ComponentModel.RecommendedAsConfigurableAttribute Yes;
        public RecommendedAsConfigurableAttribute(bool recommendedAsConfigurable) { }
        public bool RecommendedAsConfigurable { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public partial class ReferenceConverter : System.ComponentModel.TypeConverter
    {
        public ReferenceConverter(System.Type type) { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        protected virtual bool IsValueAllowed(System.ComponentModel.ITypeDescriptorContext context, object value) { throw null; }
    }
    public partial class RefreshEventArgs : System.EventArgs
    {
        public RefreshEventArgs(object? componentChanged) { }
        public RefreshEventArgs(System.Type? typeChanged) { }
        public object? ComponentChanged { get { throw null; } }
        public System.Type? TypeChanged { get { throw null; } }
    }
    public delegate void RefreshEventHandler(System.ComponentModel.RefreshEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.Class)]
    public partial class RunInstallerAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.RunInstallerAttribute Default;
        public static readonly System.ComponentModel.RunInstallerAttribute No;
        public static readonly System.ComponentModel.RunInstallerAttribute Yes;
        public RunInstallerAttribute(bool runInstaller) { }
        public bool RunInstaller { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public partial class SByteConverter : System.ComponentModel.BaseNumberConverter
    {
        public SByteConverter() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Property)]
    public sealed partial class SettingsBindableAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.SettingsBindableAttribute No;
        public static readonly System.ComponentModel.SettingsBindableAttribute Yes;
        public SettingsBindableAttribute(bool bindable) { }
        public bool Bindable { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
    }
    public partial class SingleConverter : System.ComponentModel.BaseNumberConverter
    {
        public SingleConverter() { }
    }
    public partial class StringConverter : System.ComponentModel.TypeConverter
    {
        public StringConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
    }
    public static partial class SyntaxCheck
    {
        public static bool CheckMachineName(string value) { throw null; }
        public static bool CheckPath(string value) { throw null; }
        public static bool CheckRootedPath(string value) { throw null; }
    }
    public partial class TimeSpanConverter : System.ComponentModel.TypeConverter
    {
        public TimeSpanConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class ToolboxItemAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.ToolboxItemAttribute Default;
        public static readonly System.ComponentModel.ToolboxItemAttribute None;
        public ToolboxItemAttribute(bool defaultType) { }
        public ToolboxItemAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] string toolboxItemTypeName) { }
        public ToolboxItemAttribute([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type toolboxItemType) { }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public System.Type? ToolboxItemType { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)]
        public string ToolboxItemTypeName { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, AllowMultiple=true, Inherited=true)]
    public sealed partial class ToolboxItemFilterAttribute : System.Attribute
    {
        public ToolboxItemFilterAttribute(string filterString) { }
        public ToolboxItemFilterAttribute(string filterString, System.ComponentModel.ToolboxItemFilterType filterType) { }
        public string FilterString { get { throw null; } }
        public System.ComponentModel.ToolboxItemFilterType FilterType { get { throw null; } }
        public override object TypeId { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool Match([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override string ToString() { throw null; }
    }
    public enum ToolboxItemFilterType
    {
        Allow = 0,
        Custom = 1,
        Prevent = 2,
        Require = 3,
    }
    public partial class TypeConverter
    {
        public TypeConverter() { }
        public virtual bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public bool CanConvertFrom(System.Type sourceType) { throw null; }
        public virtual bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public bool CanConvertTo(System.Type destinationType) { throw null; }
        public virtual object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public object? ConvertFrom(object value) { throw null; }
        public object? ConvertFromInvariantString(System.ComponentModel.ITypeDescriptorContext? context, string text) { throw null; }
        public object? ConvertFromInvariantString(string text) { throw null; }
        public object? ConvertFromString(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, string text) { throw null; }
        public object? ConvertFromString(System.ComponentModel.ITypeDescriptorContext? context, string text) { throw null; }
        public object? ConvertFromString(string text) { throw null; }
        public virtual object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public object? ConvertTo(object? value, System.Type destinationType) { throw null; }
        public string? ConvertToInvariantString(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
        public string? ConvertToInvariantString(object? value) { throw null; }
        public string? ConvertToString(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value) { throw null; }
        public string? ConvertToString(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
        public string? ConvertToString(object? value) { throw null; }
        public object? CreateInstance(System.Collections.IDictionary propertyValues) { throw null; }
        public virtual object? CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        protected System.Exception GetConvertFromException(object? value) { throw null; }
        protected System.Exception GetConvertToException(object? value, System.Type destinationType) { throw null; }
        public bool GetCreateInstanceSupported() { throw null; }
        public virtual bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered.")]
        public System.ComponentModel.PropertyDescriptorCollection? GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public virtual System.ComponentModel.PropertyDescriptorCollection? GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered.")]
        public System.ComponentModel.PropertyDescriptorCollection? GetProperties(object value) { throw null; }
        public bool GetPropertiesSupported() { throw null; }
        public virtual bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public System.Collections.ICollection? GetStandardValues() { throw null; }
        public virtual System.ComponentModel.TypeConverter.StandardValuesCollection? GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public bool GetStandardValuesExclusive() { throw null; }
        public virtual bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public bool GetStandardValuesSupported() { throw null; }
        public virtual bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public virtual bool IsValid(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
        public bool IsValid(object value) { throw null; }
        protected System.ComponentModel.PropertyDescriptorCollection SortProperties(System.ComponentModel.PropertyDescriptorCollection props, string[] names) { throw null; }
        protected abstract partial class SimplePropertyDescriptor : System.ComponentModel.PropertyDescriptor
        {
            protected SimplePropertyDescriptor(System.Type componentType, string name, System.Type propertyType) : base (default(string), default(System.Attribute[])) { }
            protected SimplePropertyDescriptor(System.Type componentType, string name, System.Type propertyType, System.Attribute[]? attributes) : base (default(string), default(System.Attribute[])) { }
            public override System.Type ComponentType { get { throw null; } }
            public override bool IsReadOnly { get { throw null; } }
            public override System.Type PropertyType { get { throw null; } }
            public override bool CanResetValue(object component) { throw null; }
            public override void ResetValue(object component) { }
            public override bool ShouldSerializeValue(object component) { throw null; }
        }
        public partial class StandardValuesCollection : System.Collections.ICollection, System.Collections.IEnumerable
        {
            public StandardValuesCollection(System.Collections.ICollection? values) { }
            public int Count { get { throw null; } }
            public object? this[int index] { get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            public void CopyTo(System.Array array, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
        }
    }
    public abstract partial class TypeDescriptionProvider
    {
        protected TypeDescriptionProvider() { }
        protected TypeDescriptionProvider(System.ComponentModel.TypeDescriptionProvider parent) { }
        public virtual object? CreateInstance(System.IServiceProvider? provider, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type objectType, System.Type[]? argTypes, object[]? args) { throw null; }
        public virtual System.Collections.IDictionary? GetCache(object instance) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered.")]
        public virtual System.ComponentModel.ICustomTypeDescriptor GetExtendedTypeDescriptor(object instance) { throw null; }
        protected internal virtual System.ComponentModel.IExtenderProvider[] GetExtenderProviders(object instance) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public virtual string? GetFullComponentName(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
        public System.Type GetReflectionType(object instance) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public System.Type GetReflectionType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type objectType) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public virtual System.Type GetReflectionType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type objectType, object? instance) { throw null; }
        public virtual System.Type GetRuntimeType(System.Type reflectionType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of instance cannot be statically discovered.")]
        public System.ComponentModel.ICustomTypeDescriptor? GetTypeDescriptor(object instance) { throw null; }
        public System.ComponentModel.ICustomTypeDescriptor? GetTypeDescriptor([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type objectType) { throw null; }
        public virtual System.ComponentModel.ICustomTypeDescriptor? GetTypeDescriptor([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type objectType, object? instance) { throw null; }
        public virtual bool IsSupportedType(System.Type type) { throw null; }
    }
    public sealed partial class TypeDescriptor
    {
        internal TypeDescriptor() { }
        [System.Diagnostics.CodeAnalysis.DisallowNullAttribute]
        [System.ObsoleteAttribute("TypeDescriptor.ComNativeDescriptorHandler has been deprecated. Use a type description provider to supply type information for COM types instead.")]
        public static System.ComponentModel.IComNativeDescriptorHandler? ComNativeDescriptorHandler { get { throw null; } set { } }
        public static System.Type ComObjectType { [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] get { throw null; } }
        public static System.Type InterfaceType { [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] get { throw null; } }
        public static event System.ComponentModel.RefreshEventHandler? Refreshed { add { } remove { } }
        public static System.ComponentModel.TypeDescriptionProvider AddAttributes(object instance, params System.Attribute[] attributes) { throw null; }
        public static System.ComponentModel.TypeDescriptionProvider AddAttributes(System.Type type, params System.Attribute[] attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Types specified in table may be trimmed, or have their static construtors trimmed.")]
        public static void AddEditorTable(System.Type editorBaseType, System.Collections.Hashtable table) { }
        public static void AddProvider(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void AddProvider(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void AddProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, object instance) { }
        public static void AddProviderTransparent(System.ComponentModel.TypeDescriptionProvider provider, System.Type type) { }
        public static void CreateAssociation(object primary, object secondary) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.Design.IDesigner? CreateDesigner(System.ComponentModel.IComponent component, System.Type designerBaseType) { throw null; }
        public static System.ComponentModel.EventDescriptor CreateEvent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, System.ComponentModel.EventDescriptor oldEventDescriptor, params System.Attribute[] attributes) { throw null; }
        public static System.ComponentModel.EventDescriptor CreateEvent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, string name, System.Type type, params System.Attribute[] attributes) { throw null; }
        public static object? CreateInstance(System.IServiceProvider? provider, [System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicConstructors)] System.Type objectType, System.Type[]? argTypes, object[]? args) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptor CreateProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, System.ComponentModel.PropertyDescriptor oldPropertyDescriptor, params System.Attribute[] attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptor CreateProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, string name, System.Type type, params System.Attribute[] attributes) { throw null; }
        public static object GetAssociation(System.Type type, object primary) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.AttributeCollection GetAttributes(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.AttributeCollection GetAttributes(object component, bool noCustomTypeDesc) { throw null; }
        public static System.ComponentModel.AttributeCollection GetAttributes([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static string? GetClassName(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static string? GetClassName(object component, bool noCustomTypeDesc) { throw null; }
        public static string? GetClassName([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static string? GetComponentName(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static string? GetComponentName(object component, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.TypeConverter GetConverter(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.TypeConverter GetConverter(object component, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Generic TypeConverters may require the generic types to be annotated. For example, NullableConverter requires the underlying type to be DynamicallyAccessedMembers All.")]
        public static System.ComponentModel.TypeConverter GetConverter([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.EventDescriptor? GetDefaultEvent(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.EventDescriptor? GetDefaultEvent(object component, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The built-in EventDescriptor implementation uses Reflection which requires unreferenced code.")]
        public static System.ComponentModel.EventDescriptor? GetDefaultEvent([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptor? GetDefaultProperty(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptor? GetDefaultProperty(object component, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptor? GetDefaultProperty([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of component cannot be statically discovered.")]
        public static object? GetEditor(object component, System.Type editorBaseType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed. The Type of component cannot be statically discovered.")]
        public static object? GetEditor(object component, System.Type editorBaseType, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Editors registered in TypeDescriptor.AddEditorTable may be trimmed.")]
        public static object? GetEditor([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type type, System.Type editorBaseType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, System.Attribute[] attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, System.Attribute[]? attributes, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.EventDescriptorCollection GetEvents(object component, bool noCustomTypeDesc) { throw null; }
        public static System.ComponentModel.EventDescriptorCollection GetEvents([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.EventDescriptorCollection GetEvents([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, System.Attribute[] attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of component cannot be statically discovered.")]
        public static string? GetFullComponentName(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, System.Attribute[]? attributes) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, System.Attribute[]? attributes, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The Type of component cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties(object component, bool noCustomTypeDesc) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("PropertyDescriptor's PropertyType cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public static System.ComponentModel.PropertyDescriptorCollection GetProperties([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.All)] System.Type componentType, System.Attribute[]? attributes) { throw null; }
        public static System.ComponentModel.TypeDescriptionProvider GetProvider(object instance) { throw null; }
        public static System.ComponentModel.TypeDescriptionProvider GetProvider(System.Type type) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("GetReflectionType is not trim compatible because the Type of object cannot be statically discovered.")]
        public static System.Type GetReflectionType(object instance) { throw null; }
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public static System.Type GetReflectionType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicFields | System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] System.Type type) { throw null; }
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
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
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
    public partial class VersionConverter : System.ComponentModel.TypeConverter
    {
        public VersionConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override bool IsValid(System.ComponentModel.ITypeDescriptorContext? context, object? value) { throw null; }
    }
    public partial class WarningException : System.SystemException
    {
        public WarningException() { }
        protected WarningException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public WarningException(string? message) { }
        public WarningException(string? message, System.Exception? innerException) { }
        public WarningException(string? message, string? helpUrl) { }
        public WarningException(string? message, string? helpUrl, string? helpTopic) { }
        public string? HelpTopic { get { throw null; } }
        public string? HelpUrl { get { throw null; } }
        public override void GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
}
namespace System.ComponentModel.Design
{
    public partial class ActiveDesignerEventArgs : System.EventArgs
    {
        public ActiveDesignerEventArgs(System.ComponentModel.Design.IDesignerHost? oldDesigner, System.ComponentModel.Design.IDesignerHost? newDesigner) { }
        public System.ComponentModel.Design.IDesignerHost? NewDesigner { get { throw null; } }
        public System.ComponentModel.Design.IDesignerHost? OldDesigner { get { throw null; } }
    }
    public delegate void ActiveDesignerEventHandler(object? sender, System.ComponentModel.Design.ActiveDesignerEventArgs e);
    public partial class CheckoutException : System.Runtime.InteropServices.ExternalException
    {
        public static readonly System.ComponentModel.Design.CheckoutException Canceled;
        public CheckoutException() { }
        protected CheckoutException(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
        public CheckoutException(string? message) { }
        public CheckoutException(string? message, System.Exception? innerException) { }
        public CheckoutException(string? message, int errorCode) { }
    }
    public partial class CommandID
    {
        public CommandID(System.Guid menuGroup, int commandID) { }
        public virtual System.Guid Guid { get { throw null; } }
        public virtual int ID { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override string ToString() { throw null; }
    }
    public sealed partial class ComponentChangedEventArgs : System.EventArgs
    {
        public ComponentChangedEventArgs(object? component, System.ComponentModel.MemberDescriptor? member, object? oldValue, object? newValue) { }
        public object? Component { get { throw null; } }
        public System.ComponentModel.MemberDescriptor? Member { get { throw null; } }
        public object? NewValue { get { throw null; } }
        public object? OldValue { get { throw null; } }
    }
    public delegate void ComponentChangedEventHandler(object? sender, System.ComponentModel.Design.ComponentChangedEventArgs e);
    public sealed partial class ComponentChangingEventArgs : System.EventArgs
    {
        public ComponentChangingEventArgs(object? component, System.ComponentModel.MemberDescriptor? member) { }
        public object? Component { get { throw null; } }
        public System.ComponentModel.MemberDescriptor? Member { get { throw null; } }
    }
    public delegate void ComponentChangingEventHandler(object? sender, System.ComponentModel.Design.ComponentChangingEventArgs e);
    public partial class ComponentEventArgs : System.EventArgs
    {
        public ComponentEventArgs(System.ComponentModel.IComponent? component) { }
        public virtual System.ComponentModel.IComponent? Component { get { throw null; } }
    }
    public delegate void ComponentEventHandler(object? sender, System.ComponentModel.Design.ComponentEventArgs e);
    public partial class ComponentRenameEventArgs : System.EventArgs
    {
        public ComponentRenameEventArgs(object? component, string? oldName, string? newName) { }
        public object? Component { get { throw null; } }
        public virtual string? NewName { get { throw null; } }
        public virtual string? OldName { get { throw null; } }
    }
    public delegate void ComponentRenameEventHandler(object? sender, System.ComponentModel.Design.ComponentRenameEventArgs e);
    public partial class DesignerCollection : System.Collections.ICollection, System.Collections.IEnumerable
    {
        public DesignerCollection(System.Collections.IList? designers) { }
        public DesignerCollection(System.ComponentModel.Design.IDesignerHost[]? designers) { }
        public int Count { get { throw null; } }
        public virtual System.ComponentModel.Design.IDesignerHost? this[int index] { get { throw null; } }
        int System.Collections.ICollection.Count { get { throw null; } }
        bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
        object System.Collections.ICollection.SyncRoot { get { throw null; } }
        public System.Collections.IEnumerator GetEnumerator() { throw null; }
        void System.Collections.ICollection.CopyTo(System.Array array, int index) { }
        System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { throw null; }
    }
    public partial class DesignerEventArgs : System.EventArgs
    {
        public DesignerEventArgs(System.ComponentModel.Design.IDesignerHost? host) { }
        public System.ComponentModel.Design.IDesignerHost? Designer { get { throw null; } }
    }
    public delegate void DesignerEventHandler(object? sender, System.ComponentModel.Design.DesignerEventArgs e);
    public abstract partial class DesignerOptionService : System.ComponentModel.Design.IDesignerOptionService
    {
        protected DesignerOptionService() { }
        public System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection Options { get { throw null; } }
        protected System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection CreateOptionCollection(System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection parent, string name, object value) { throw null; }
        protected virtual void PopulateOptionCollection(System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection options) { }
        protected virtual bool ShowDialog(System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection options, object optionObject) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The option value's Type cannot be statically discovered.")]
        object System.ComponentModel.Design.IDesignerOptionService.GetOptionValue(string pageName, string valueName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The option value's Type cannot be statically discovered.")]
        void System.ComponentModel.Design.IDesignerOptionService.SetOptionValue(string pageName, string valueName, object value) { }
        [System.ComponentModel.EditorAttribute("", "System.Drawing.Design.UITypeEditor, System.Drawing, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a")]
        public sealed partial class DesignerOptionCollection : System.Collections.ICollection, System.Collections.IEnumerable, System.Collections.IList
        {
            internal DesignerOptionCollection() { }
            public int Count { get { throw null; } }
            public System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection? this[int index] { get { throw null; } }
            public System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection? this[string name] { get { throw null; } }
            public string Name { get { throw null; } }
            public System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection? Parent { get { throw null; } }
            public System.ComponentModel.PropertyDescriptorCollection Properties { [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of DesignerOptionCollection's value cannot be statically discovered.")] get { throw null; } }
            bool System.Collections.ICollection.IsSynchronized { get { throw null; } }
            object System.Collections.ICollection.SyncRoot { get { throw null; } }
            bool System.Collections.IList.IsFixedSize { get { throw null; } }
            bool System.Collections.IList.IsReadOnly { get { throw null; } }
            object? System.Collections.IList.this[int index] { get { throw null; } set { } }
            public void CopyTo(System.Array array, int index) { }
            public System.Collections.IEnumerator GetEnumerator() { throw null; }
            public int IndexOf(System.ComponentModel.Design.DesignerOptionService.DesignerOptionCollection value) { throw null; }
            public bool ShowDialog() { throw null; }
            int System.Collections.IList.Add(object? value) { throw null; }
            void System.Collections.IList.Clear() { }
            bool System.Collections.IList.Contains(object? value) { throw null; }
            int System.Collections.IList.IndexOf(object? value) { throw null; }
            void System.Collections.IList.Insert(int index, object? value) { }
            void System.Collections.IList.Remove(object? value) { }
            void System.Collections.IList.RemoveAt(int index) { }
        }
    }
    public abstract partial class DesignerTransaction : System.IDisposable
    {
        protected DesignerTransaction() { }
        protected DesignerTransaction(string description) { }
        public bool Canceled { get { throw null; } }
        public bool Committed { get { throw null; } }
        public string Description { get { throw null; } }
        public void Cancel() { }
        public void Commit() { }
        protected virtual void Dispose(bool disposing) { }
        ~DesignerTransaction() { }
        protected abstract void OnCancel();
        protected abstract void OnCommit();
        void System.IDisposable.Dispose() { }
    }
    public partial class DesignerTransactionCloseEventArgs : System.EventArgs
    {
        [System.ObsoleteAttribute("This constructor has been deprecated. Use DesignerTransactionCloseEventArgs(bool, bool) instead.")]
        public DesignerTransactionCloseEventArgs(bool commit) { }
        public DesignerTransactionCloseEventArgs(bool commit, bool lastTransaction) { }
        public bool LastTransaction { get { throw null; } }
        public bool TransactionCommitted { get { throw null; } }
    }
    public delegate void DesignerTransactionCloseEventHandler(object? sender, System.ComponentModel.Design.DesignerTransactionCloseEventArgs e);
    public partial class DesignerVerb : System.ComponentModel.Design.MenuCommand
    {
        public DesignerVerb(string text, System.EventHandler handler) : base (default(System.EventHandler), default(System.ComponentModel.Design.CommandID)) { }
        public DesignerVerb(string text, System.EventHandler handler, System.ComponentModel.Design.CommandID startCommandID) : base (default(System.EventHandler), default(System.ComponentModel.Design.CommandID)) { }
        public string Description { get { throw null; } set { } }
        public string Text { get { throw null; } }
        public override string ToString() { throw null; }
    }
    public partial class DesignerVerbCollection : System.Collections.CollectionBase
    {
        public DesignerVerbCollection() { }
        public DesignerVerbCollection(System.ComponentModel.Design.DesignerVerb[] value) { }
        public System.ComponentModel.Design.DesignerVerb? this[int index] { get { throw null; } set { } }
        public int Add(System.ComponentModel.Design.DesignerVerb? value) { throw null; }
        public void AddRange(System.ComponentModel.Design.DesignerVerbCollection value) { }
        public void AddRange(System.ComponentModel.Design.DesignerVerb?[] value) { }
        public bool Contains(System.ComponentModel.Design.DesignerVerb? value) { throw null; }
        public void CopyTo(System.ComponentModel.Design.DesignerVerb?[] array, int index) { }
        public int IndexOf(System.ComponentModel.Design.DesignerVerb? value) { throw null; }
        public void Insert(int index, System.ComponentModel.Design.DesignerVerb? value) { }
        protected override void OnValidate(object value) { }
        public void Remove(System.ComponentModel.Design.DesignerVerb? value) { }
    }
    public partial class DesigntimeLicenseContext : System.ComponentModel.LicenseContext
    {
        public DesigntimeLicenseContext() { }
        public override System.ComponentModel.LicenseUsageMode UsageMode { get { throw null; } }
        public override string? GetSavedLicenseKey(System.Type type, System.Reflection.Assembly? resourceAssembly) { throw null; }
        public override void SetSavedLicenseKey(System.Type type, string key) { }
    }
    public partial class DesigntimeLicenseContextSerializer
    {
        internal DesigntimeLicenseContextSerializer() { }
        public static void Serialize(System.IO.Stream o, string cryptoKey, System.ComponentModel.Design.DesigntimeLicenseContext context) { }
    }
    public enum HelpContextType
    {
        Ambient = 0,
        Window = 1,
        Selection = 2,
        ToolWindowSelection = 3,
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All, AllowMultiple=false, Inherited=false)]
    public sealed partial class HelpKeywordAttribute : System.Attribute
    {
        public static readonly System.ComponentModel.Design.HelpKeywordAttribute Default;
        public HelpKeywordAttribute() { }
        public HelpKeywordAttribute(string keyword) { }
        public HelpKeywordAttribute(System.Type t) { }
        public string? HelpKeyword { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public override bool IsDefaultAttribute() { throw null; }
    }
    public enum HelpKeywordType
    {
        F1Keyword = 0,
        GeneralKeyword = 1,
        FilterKeyword = 2,
    }
    public partial interface IComponentChangeService
    {
        event System.ComponentModel.Design.ComponentEventHandler ComponentAdded;
        event System.ComponentModel.Design.ComponentEventHandler ComponentAdding;
        event System.ComponentModel.Design.ComponentChangedEventHandler ComponentChanged;
        event System.ComponentModel.Design.ComponentChangingEventHandler ComponentChanging;
        event System.ComponentModel.Design.ComponentEventHandler ComponentRemoved;
        event System.ComponentModel.Design.ComponentEventHandler ComponentRemoving;
        event System.ComponentModel.Design.ComponentRenameEventHandler ComponentRename;
        void OnComponentChanged(object component, System.ComponentModel.MemberDescriptor? member, object? oldValue, object? newValue);
        void OnComponentChanging(object component, System.ComponentModel.MemberDescriptor? member);
    }
    public partial interface IComponentDiscoveryService
    {
        System.Collections.ICollection GetComponentTypes(System.ComponentModel.Design.IDesignerHost? designerHost, System.Type? baseType);
    }
    public partial interface IComponentInitializer
    {
        void InitializeExistingComponent(System.Collections.IDictionary? defaultValues);
        void InitializeNewComponent(System.Collections.IDictionary? defaultValues);
    }
    public partial interface IDesigner : System.IDisposable
    {
        System.ComponentModel.IComponent Component { get; }
        System.ComponentModel.Design.DesignerVerbCollection? Verbs { get; }
        void DoDefaultAction();
        void Initialize(System.ComponentModel.IComponent component);
    }
    public partial interface IDesignerEventService
    {
        System.ComponentModel.Design.IDesignerHost? ActiveDesigner { get; }
        System.ComponentModel.Design.DesignerCollection Designers { get; }
        event System.ComponentModel.Design.ActiveDesignerEventHandler ActiveDesignerChanged;
        event System.ComponentModel.Design.DesignerEventHandler DesignerCreated;
        event System.ComponentModel.Design.DesignerEventHandler DesignerDisposed;
        event System.EventHandler SelectionChanged;
    }
    public partial interface IDesignerFilter
    {
        void PostFilterAttributes(System.Collections.IDictionary attributes);
        void PostFilterEvents(System.Collections.IDictionary events);
        void PostFilterProperties(System.Collections.IDictionary properties);
        void PreFilterAttributes(System.Collections.IDictionary attributes);
        void PreFilterEvents(System.Collections.IDictionary events);
        void PreFilterProperties(System.Collections.IDictionary properties);
    }
    public partial interface IDesignerHost : System.ComponentModel.Design.IServiceContainer, System.IServiceProvider
    {
        System.ComponentModel.IContainer Container { get; }
        bool InTransaction { get; }
        bool Loading { get; }
        System.ComponentModel.IComponent RootComponent { get; }
        string RootComponentClassName { get; }
        string TransactionDescription { get; }
        event System.EventHandler Activated;
        event System.EventHandler Deactivated;
        event System.EventHandler LoadComplete;
        event System.ComponentModel.Design.DesignerTransactionCloseEventHandler TransactionClosed;
        event System.ComponentModel.Design.DesignerTransactionCloseEventHandler TransactionClosing;
        event System.EventHandler TransactionOpened;
        event System.EventHandler TransactionOpening;
        void Activate();
        System.ComponentModel.IComponent CreateComponent(System.Type componentClass);
        System.ComponentModel.IComponent CreateComponent(System.Type componentClass, string name);
        System.ComponentModel.Design.DesignerTransaction CreateTransaction();
        System.ComponentModel.Design.DesignerTransaction CreateTransaction(string description);
        void DestroyComponent(System.ComponentModel.IComponent component);
        System.ComponentModel.Design.IDesigner? GetDesigner(System.ComponentModel.IComponent component);
        System.Type? GetType(string typeName);
    }
    public partial interface IDesignerHostTransactionState
    {
        bool IsClosingTransaction { get; }
    }
    public partial interface IDesignerOptionService
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The option value's Type cannot be statically discovered.")]
        object? GetOptionValue(string pageName, string valueName);
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The option value's Type cannot be statically discovered.")]
        void SetOptionValue(string pageName, string valueName, object value);
    }
    public partial interface IDictionaryService
    {
        object? GetKey(object? value);
        object? GetValue(object key);
        void SetValue(object key, object? value);
    }
    public partial interface IEventBindingService
    {
        string CreateUniqueMethodName(System.ComponentModel.IComponent component, System.ComponentModel.EventDescriptor e);
        System.Collections.ICollection GetCompatibleMethods(System.ComponentModel.EventDescriptor e);
        System.ComponentModel.EventDescriptor? GetEvent(System.ComponentModel.PropertyDescriptor property);
        System.ComponentModel.PropertyDescriptorCollection GetEventProperties(System.ComponentModel.EventDescriptorCollection events);
        System.ComponentModel.PropertyDescriptor GetEventProperty(System.ComponentModel.EventDescriptor e);
        bool ShowCode();
        bool ShowCode(System.ComponentModel.IComponent component, System.ComponentModel.EventDescriptor e);
        bool ShowCode(int lineNumber);
    }
    public partial interface IExtenderListService
    {
        System.ComponentModel.IExtenderProvider[] GetExtenderProviders();
    }
    public partial interface IExtenderProviderService
    {
        void AddExtenderProvider(System.ComponentModel.IExtenderProvider provider);
        void RemoveExtenderProvider(System.ComponentModel.IExtenderProvider provider);
    }
    public partial interface IHelpService
    {
        void AddContextAttribute(string name, string value, System.ComponentModel.Design.HelpKeywordType keywordType);
        void ClearContextAttributes();
        System.ComponentModel.Design.IHelpService CreateLocalContext(System.ComponentModel.Design.HelpContextType contextType);
        void RemoveContextAttribute(string name, string value);
        void RemoveLocalContext(System.ComponentModel.Design.IHelpService localContext);
        void ShowHelpFromKeyword(string helpKeyword);
        void ShowHelpFromUrl(string helpUrl);
    }
    public partial interface IInheritanceService
    {
        void AddInheritedComponents(System.ComponentModel.IComponent component, System.ComponentModel.IContainer container);
        System.ComponentModel.InheritanceAttribute GetInheritanceAttribute(System.ComponentModel.IComponent component);
    }
    public partial interface IMenuCommandService
    {
        System.ComponentModel.Design.DesignerVerbCollection Verbs { get; }
        void AddCommand(System.ComponentModel.Design.MenuCommand command);
        void AddVerb(System.ComponentModel.Design.DesignerVerb verb);
        System.ComponentModel.Design.MenuCommand? FindCommand(System.ComponentModel.Design.CommandID commandID);
        bool GlobalInvoke(System.ComponentModel.Design.CommandID commandID);
        void RemoveCommand(System.ComponentModel.Design.MenuCommand command);
        void RemoveVerb(System.ComponentModel.Design.DesignerVerb verb);
        void ShowContextMenu(System.ComponentModel.Design.CommandID menuID, int x, int y);
    }
    public partial interface IReferenceService
    {
        System.ComponentModel.IComponent? GetComponent(object reference);
        string? GetName(object reference);
        object? GetReference(string name);
        object[] GetReferences();
        object[] GetReferences(System.Type baseType);
    }
    public partial interface IResourceService
    {
        System.Resources.IResourceReader? GetResourceReader(System.Globalization.CultureInfo info);
        System.Resources.IResourceWriter GetResourceWriter(System.Globalization.CultureInfo info);
    }
    public partial interface IRootDesigner : System.ComponentModel.Design.IDesigner, System.IDisposable
    {
        System.ComponentModel.Design.ViewTechnology[] SupportedTechnologies { get; }
        object GetView(System.ComponentModel.Design.ViewTechnology technology);
    }
    public partial interface ISelectionService
    {
        object PrimarySelection { get; }
        int SelectionCount { get; }
        event System.EventHandler SelectionChanged;
        event System.EventHandler SelectionChanging;
        bool GetComponentSelected(object component);
        System.Collections.ICollection GetSelectedComponents();
        void SetSelectedComponents(System.Collections.ICollection? components);
        void SetSelectedComponents(System.Collections.ICollection? components, System.ComponentModel.Design.SelectionTypes selectionType);
    }
    public partial interface IServiceContainer : System.IServiceProvider
    {
        void AddService(System.Type serviceType, System.ComponentModel.Design.ServiceCreatorCallback callback);
        void AddService(System.Type serviceType, System.ComponentModel.Design.ServiceCreatorCallback callback, bool promote);
        void AddService(System.Type serviceType, object serviceInstance);
        void AddService(System.Type serviceType, object serviceInstance, bool promote);
        void RemoveService(System.Type serviceType);
        void RemoveService(System.Type serviceType, bool promote);
    }
    public partial interface ITreeDesigner : System.ComponentModel.Design.IDesigner, System.IDisposable
    {
        System.Collections.ICollection Children { get; }
        System.ComponentModel.Design.IDesigner? Parent { get; }
    }
    public partial interface ITypeDescriptorFilterService
    {
        bool FilterAttributes(System.ComponentModel.IComponent component, System.Collections.IDictionary attributes);
        bool FilterEvents(System.ComponentModel.IComponent component, System.Collections.IDictionary events);
        bool FilterProperties(System.ComponentModel.IComponent component, System.Collections.IDictionary properties);
    }
    public partial interface ITypeDiscoveryService
    {
        System.Collections.ICollection GetTypes(System.Type? baseType, bool excludeGlobalTypes);
    }
    public partial interface ITypeResolutionService
    {
        System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name);
        System.Reflection.Assembly? GetAssembly(System.Reflection.AssemblyName name, bool throwOnError);
        string? GetPathOfAssembly(System.Reflection.AssemblyName name);
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        System.Type? GetType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name);
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        System.Type? GetType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError);
        [return: System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        System.Type? GetType([System.Diagnostics.CodeAnalysis.DynamicallyAccessedMembersAttribute(System.Diagnostics.CodeAnalysis.DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string name, bool throwOnError, bool ignoreCase);
        void ReferenceAssembly(System.Reflection.AssemblyName name);
    }
    public partial class MenuCommand
    {
        public MenuCommand(System.EventHandler? handler, System.ComponentModel.Design.CommandID? command) { }
        public virtual bool Checked { get { throw null; } set { } }
        public virtual System.ComponentModel.Design.CommandID? CommandID { get { throw null; } }
        public virtual bool Enabled { get { throw null; } set { } }
        public virtual int OleStatus { get { throw null; } }
        public virtual System.Collections.IDictionary Properties { get { throw null; } }
        public virtual bool Supported { get { throw null; } set { } }
        public virtual bool Visible { get { throw null; } set { } }
        public event System.EventHandler? CommandChanged { add { } remove { } }
        public virtual void Invoke() { }
        public virtual void Invoke(object arg) { }
        protected virtual void OnCommandChanged(System.EventArgs e) { }
        public override string ToString() { throw null; }
    }
    [System.FlagsAttribute]
    public enum SelectionTypes
    {
        Auto = 1,
        [System.ObsoleteAttribute("SelectionTypes.Normal has been deprecated. Use SelectionTypes.Auto instead.")]
        Normal = 1,
        Replace = 2,
        [System.ObsoleteAttribute("SelectionTypes.MouseDown has been deprecated and is not supported.")]
        MouseDown = 4,
        [System.ObsoleteAttribute("SelectionTypes.MouseUp has been deprecated and is not supported.")]
        MouseUp = 8,
        [System.ObsoleteAttribute("SelectionTypes.Click has been deprecated. Use SelectionTypes.Primary instead.")]
        Click = 16,
        Primary = 16,
        [System.ObsoleteAttribute("SelectionTypes.Valid has been deprecated. Use Enum class methods to determine valid values, or use a type converter instead.")]
        Valid = 31,
        Toggle = 32,
        Add = 64,
        Remove = 128,
    }
    public partial class ServiceContainer : System.ComponentModel.Design.IServiceContainer, System.IDisposable, System.IServiceProvider
    {
        public ServiceContainer() { }
        public ServiceContainer(System.IServiceProvider? parentProvider) { }
        protected virtual System.Type[] DefaultServices { get { throw null; } }
        public void AddService(System.Type serviceType, System.ComponentModel.Design.ServiceCreatorCallback callback) { }
        public virtual void AddService(System.Type serviceType, System.ComponentModel.Design.ServiceCreatorCallback callback, bool promote) { }
        public void AddService(System.Type serviceType, object serviceInstance) { }
        public virtual void AddService(System.Type serviceType, object serviceInstance, bool promote) { }
        public void Dispose() { }
        protected virtual void Dispose(bool disposing) { }
        public virtual object? GetService(System.Type serviceType) { throw null; }
        public void RemoveService(System.Type serviceType) { }
        public virtual void RemoveService(System.Type serviceType, bool promote) { }
    }
    public delegate object? ServiceCreatorCallback(System.ComponentModel.Design.IServiceContainer container, System.Type serviceType);
    public partial class StandardCommands
    {
        public static readonly System.ComponentModel.Design.CommandID AlignBottom;
        public static readonly System.ComponentModel.Design.CommandID AlignHorizontalCenters;
        public static readonly System.ComponentModel.Design.CommandID AlignLeft;
        public static readonly System.ComponentModel.Design.CommandID AlignRight;
        public static readonly System.ComponentModel.Design.CommandID AlignToGrid;
        public static readonly System.ComponentModel.Design.CommandID AlignTop;
        public static readonly System.ComponentModel.Design.CommandID AlignVerticalCenters;
        public static readonly System.ComponentModel.Design.CommandID ArrangeBottom;
        public static readonly System.ComponentModel.Design.CommandID ArrangeIcons;
        public static readonly System.ComponentModel.Design.CommandID ArrangeRight;
        public static readonly System.ComponentModel.Design.CommandID BringForward;
        public static readonly System.ComponentModel.Design.CommandID BringToFront;
        public static readonly System.ComponentModel.Design.CommandID CenterHorizontally;
        public static readonly System.ComponentModel.Design.CommandID CenterVertically;
        public static readonly System.ComponentModel.Design.CommandID Copy;
        public static readonly System.ComponentModel.Design.CommandID Cut;
        public static readonly System.ComponentModel.Design.CommandID Delete;
        public static readonly System.ComponentModel.Design.CommandID DocumentOutline;
        public static readonly System.ComponentModel.Design.CommandID F1Help;
        public static readonly System.ComponentModel.Design.CommandID Group;
        public static readonly System.ComponentModel.Design.CommandID HorizSpaceConcatenate;
        public static readonly System.ComponentModel.Design.CommandID HorizSpaceDecrease;
        public static readonly System.ComponentModel.Design.CommandID HorizSpaceIncrease;
        public static readonly System.ComponentModel.Design.CommandID HorizSpaceMakeEqual;
        public static readonly System.ComponentModel.Design.CommandID LineupIcons;
        public static readonly System.ComponentModel.Design.CommandID LockControls;
        public static readonly System.ComponentModel.Design.CommandID MultiLevelRedo;
        public static readonly System.ComponentModel.Design.CommandID MultiLevelUndo;
        public static readonly System.ComponentModel.Design.CommandID Paste;
        public static readonly System.ComponentModel.Design.CommandID Properties;
        public static readonly System.ComponentModel.Design.CommandID PropertiesWindow;
        public static readonly System.ComponentModel.Design.CommandID Redo;
        public static readonly System.ComponentModel.Design.CommandID Replace;
        public static readonly System.ComponentModel.Design.CommandID SelectAll;
        public static readonly System.ComponentModel.Design.CommandID SendBackward;
        public static readonly System.ComponentModel.Design.CommandID SendToBack;
        public static readonly System.ComponentModel.Design.CommandID ShowGrid;
        public static readonly System.ComponentModel.Design.CommandID ShowLargeIcons;
        public static readonly System.ComponentModel.Design.CommandID SizeToControl;
        public static readonly System.ComponentModel.Design.CommandID SizeToControlHeight;
        public static readonly System.ComponentModel.Design.CommandID SizeToControlWidth;
        public static readonly System.ComponentModel.Design.CommandID SizeToFit;
        public static readonly System.ComponentModel.Design.CommandID SizeToGrid;
        public static readonly System.ComponentModel.Design.CommandID SnapToGrid;
        public static readonly System.ComponentModel.Design.CommandID TabOrder;
        public static readonly System.ComponentModel.Design.CommandID Undo;
        public static readonly System.ComponentModel.Design.CommandID Ungroup;
        public static readonly System.ComponentModel.Design.CommandID VerbFirst;
        public static readonly System.ComponentModel.Design.CommandID VerbLast;
        public static readonly System.ComponentModel.Design.CommandID VertSpaceConcatenate;
        public static readonly System.ComponentModel.Design.CommandID VertSpaceDecrease;
        public static readonly System.ComponentModel.Design.CommandID VertSpaceIncrease;
        public static readonly System.ComponentModel.Design.CommandID VertSpaceMakeEqual;
        public static readonly System.ComponentModel.Design.CommandID ViewCode;
        public static readonly System.ComponentModel.Design.CommandID ViewGrid;
        public StandardCommands() { }
    }
    public partial class StandardToolWindows
    {
        public static readonly System.Guid ObjectBrowser;
        public static readonly System.Guid OutputWindow;
        public static readonly System.Guid ProjectExplorer;
        public static readonly System.Guid PropertyBrowser;
        public static readonly System.Guid RelatedLinks;
        public static readonly System.Guid ServerExplorer;
        public static readonly System.Guid TaskList;
        public static readonly System.Guid Toolbox;
        public StandardToolWindows() { }
    }
    public abstract partial class TypeDescriptionProviderService
    {
        protected TypeDescriptionProviderService() { }
        public abstract System.ComponentModel.TypeDescriptionProvider GetProvider(object instance);
        public abstract System.ComponentModel.TypeDescriptionProvider GetProvider(System.Type type);
    }
    public enum ViewTechnology
    {
        [System.ObsoleteAttribute("ViewTechnology.Passthrough has been deprecated. Use ViewTechnology.Default instead.")]
        Passthrough = 0,
        [System.ObsoleteAttribute("ViewTechnology.WindowsForms has been deprecated. Use ViewTechnology.Default instead.")]
        WindowsForms = 1,
        Default = 2,
    }
}
namespace System.ComponentModel.Design.Serialization
{
    public abstract partial class ComponentSerializationService
    {
        protected ComponentSerializationService() { }
        public abstract System.ComponentModel.Design.Serialization.SerializationStore CreateStore();
        public abstract System.Collections.ICollection Deserialize(System.ComponentModel.Design.Serialization.SerializationStore store);
        public abstract System.Collections.ICollection Deserialize(System.ComponentModel.Design.Serialization.SerializationStore store, System.ComponentModel.IContainer container);
        public void DeserializeTo(System.ComponentModel.Design.Serialization.SerializationStore store, System.ComponentModel.IContainer container) { }
        public void DeserializeTo(System.ComponentModel.Design.Serialization.SerializationStore store, System.ComponentModel.IContainer container, bool validateRecycledTypes) { }
        public abstract void DeserializeTo(System.ComponentModel.Design.Serialization.SerializationStore store, System.ComponentModel.IContainer container, bool validateRecycledTypes, bool applyDefaults);
        public abstract System.ComponentModel.Design.Serialization.SerializationStore LoadStore(System.IO.Stream stream);
        public abstract void Serialize(System.ComponentModel.Design.Serialization.SerializationStore store, object value);
        public abstract void SerializeAbsolute(System.ComponentModel.Design.Serialization.SerializationStore store, object value);
        public abstract void SerializeMember(System.ComponentModel.Design.Serialization.SerializationStore store, object owningObject, System.ComponentModel.MemberDescriptor member);
        public abstract void SerializeMemberAbsolute(System.ComponentModel.Design.Serialization.SerializationStore store, object owningObject, System.ComponentModel.MemberDescriptor member);
    }
    public sealed partial class ContextStack
    {
        public ContextStack() { }
        public object? Current { get { throw null; } }
        public object? this[int level] { get { throw null; } }
        public object? this[System.Type type] { get { throw null; } }
        public void Append(object context) { }
        public object? Pop() { throw null; }
        public void Push(object context) { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.Class, Inherited=false)]
    public sealed partial class DefaultSerializationProviderAttribute : System.Attribute
    {
        public DefaultSerializationProviderAttribute(string providerTypeName) { }
        public DefaultSerializationProviderAttribute(System.Type providerType) { }
        public string ProviderTypeName { get { throw null; } }
    }
    public abstract partial class DesignerLoader
    {
        protected DesignerLoader() { }
        public virtual bool Loading { get { throw null; } }
        public abstract void BeginLoad(System.ComponentModel.Design.Serialization.IDesignerLoaderHost host);
        public abstract void Dispose();
        public virtual void Flush() { }
    }
    public partial interface IDesignerLoaderHost : System.ComponentModel.Design.IDesignerHost, System.ComponentModel.Design.IServiceContainer, System.IServiceProvider
    {
        void EndLoad(string baseClassName, bool successful, System.Collections.ICollection? errorCollection);
        void Reload();
    }
    public partial interface IDesignerLoaderHost2 : System.ComponentModel.Design.IDesignerHost, System.ComponentModel.Design.IServiceContainer, System.ComponentModel.Design.Serialization.IDesignerLoaderHost, System.IServiceProvider
    {
        bool CanReloadWithErrors { get; set; }
        bool IgnoreErrorsDuringReload { get; set; }
    }
    public partial interface IDesignerLoaderService
    {
        void AddLoadDependency();
        void DependentLoadComplete(bool successful, System.Collections.ICollection? errorCollection);
        bool Reload();
    }
    public partial interface IDesignerSerializationManager : System.IServiceProvider
    {
        System.ComponentModel.Design.Serialization.ContextStack Context { get; }
        System.ComponentModel.PropertyDescriptorCollection Properties { get; }
        event System.ComponentModel.Design.Serialization.ResolveNameEventHandler ResolveName;
        event System.EventHandler SerializationComplete;
        void AddSerializationProvider(System.ComponentModel.Design.Serialization.IDesignerSerializationProvider provider);
        object CreateInstance(System.Type type, System.Collections.ICollection arguments, string name, bool addToContainer);
        object GetInstance(string name);
        string GetName(object value);
        object GetSerializer(System.Type objectType, System.Type serializerType);
        System.Type GetType(string typeName);
        void RemoveSerializationProvider(System.ComponentModel.Design.Serialization.IDesignerSerializationProvider provider);
        void ReportError(object errorInformation);
        void SetName(object instance, string name);
    }
    public partial interface IDesignerSerializationProvider
    {
        object? GetSerializer(System.ComponentModel.Design.Serialization.IDesignerSerializationManager manager, object currentSerializer, System.Type objectType, System.Type serializerType);
    }
    public partial interface IDesignerSerializationService
    {
        System.Collections.ICollection Deserialize(object serializationData);
        object Serialize(System.Collections.ICollection objects);
    }
    public partial interface INameCreationService
    {
        string CreateName(System.ComponentModel.IContainer? container, System.Type dataType);
        bool IsValidName(string name);
        void ValidateName(string name);
    }
    public sealed partial class InstanceDescriptor
    {
        public InstanceDescriptor(System.Reflection.MemberInfo? member, System.Collections.ICollection? arguments) { }
        public InstanceDescriptor(System.Reflection.MemberInfo? member, System.Collections.ICollection? arguments, bool isComplete) { }
        public System.Collections.ICollection Arguments { get { throw null; } }
        public bool IsComplete { get { throw null; } }
        public System.Reflection.MemberInfo? MemberInfo { get { throw null; } }
        public object? Invoke() { throw null; }
    }
    [System.Runtime.InteropServices.StructLayoutAttribute(System.Runtime.InteropServices.LayoutKind.Sequential)]
    public readonly partial struct MemberRelationship
    {
        private readonly object _dummy;
        private readonly int _dummyPrimitive;
        public static readonly System.ComponentModel.Design.Serialization.MemberRelationship Empty;
        public MemberRelationship(object owner, System.ComponentModel.MemberDescriptor member) { throw null; }
        public bool IsEmpty { get { throw null; } }
        public System.ComponentModel.MemberDescriptor Member { get { throw null; } }
        public object? Owner { get { throw null; } }
        public override bool Equals([System.Diagnostics.CodeAnalysis.NotNullWhenAttribute(true)] object? obj) { throw null; }
        public override int GetHashCode() { throw null; }
        public static bool operator ==(System.ComponentModel.Design.Serialization.MemberRelationship left, System.ComponentModel.Design.Serialization.MemberRelationship right) { throw null; }
        public static bool operator !=(System.ComponentModel.Design.Serialization.MemberRelationship left, System.ComponentModel.Design.Serialization.MemberRelationship right) { throw null; }
    }
    public abstract partial class MemberRelationshipService
    {
        protected MemberRelationshipService() { }
        public System.ComponentModel.Design.Serialization.MemberRelationship this[System.ComponentModel.Design.Serialization.MemberRelationship source] { get { throw null; } set { } }
        public System.ComponentModel.Design.Serialization.MemberRelationship this[object sourceOwner, System.ComponentModel.MemberDescriptor sourceMember] { get { throw null; } set { } }
        protected virtual System.ComponentModel.Design.Serialization.MemberRelationship GetRelationship(System.ComponentModel.Design.Serialization.MemberRelationship source) { throw null; }
        protected virtual void SetRelationship(System.ComponentModel.Design.Serialization.MemberRelationship source, System.ComponentModel.Design.Serialization.MemberRelationship relationship) { }
        public abstract bool SupportsRelationship(System.ComponentModel.Design.Serialization.MemberRelationship source, System.ComponentModel.Design.Serialization.MemberRelationship relationship);
    }
    public partial class ResolveNameEventArgs : System.EventArgs
    {
        public ResolveNameEventArgs(string? name) { }
        public string? Name { get { throw null; } }
        public object? Value { get { throw null; } set { } }
    }
    public delegate void ResolveNameEventHandler(object? sender, System.ComponentModel.Design.Serialization.ResolveNameEventArgs e);
    [System.AttributeUsageAttribute(System.AttributeTargets.Class | System.AttributeTargets.Interface, AllowMultiple=true, Inherited=true)]
    [System.ObsoleteAttribute("RootDesignerSerializerAttribute has been deprecated. Use DesignerSerializerAttribute instead. For example, to specify a root designer for CodeDom, use DesignerSerializerAttribute(...,typeof(TypeCodeDomSerializer)) instead.")]
    public sealed partial class RootDesignerSerializerAttribute : System.Attribute
    {
        public RootDesignerSerializerAttribute(string? serializerTypeName, string? baseSerializerTypeName, bool reloadable) { }
        public RootDesignerSerializerAttribute(string serializerTypeName, System.Type baseSerializerType, bool reloadable) { }
        public RootDesignerSerializerAttribute(System.Type serializerType, System.Type baseSerializerType, bool reloadable) { }
        public bool Reloadable { get { throw null; } }
        public string? SerializerBaseTypeName { get { throw null; } }
        public string? SerializerTypeName { get { throw null; } }
        public override object TypeId { get { throw null; } }
    }
    public abstract partial class SerializationStore : System.IDisposable
    {
        protected SerializationStore() { }
        public abstract System.Collections.ICollection Errors { get; }
        public abstract void Close();
        protected virtual void Dispose(bool disposing) { }
        public abstract void Save(System.IO.Stream stream);
        void System.IDisposable.Dispose() { }
    }
}
namespace System.Drawing
{
    public partial class ColorConverter : System.ComponentModel.TypeConverter
    {
        public ColorConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class PointConverter : System.ComponentModel.TypeConverter
    {
        public PointConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object? value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class RectangleConverter : System.ComponentModel.TypeConverter
    {
        public RectangleConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object? value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class SizeConverter : System.ComponentModel.TypeConverter
    {
        public SizeConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
    public partial class SizeFConverter : System.ComponentModel.TypeConverter
    {
        public SizeFConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        public override object? ConvertFrom(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object value) { throw null; }
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext? context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("The Type of value cannot be statically discovered. The public parameterless constructor or the 'Default' static field may be trimmed from the Attribute's Type.")]
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext? context, object value, System.Attribute[]? attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext? context) { throw null; }
    }
}
namespace System.Security.Authentication.ExtendedProtection
{
    public partial class ExtendedProtectionPolicyTypeConverter : System.ComponentModel.TypeConverter
    {
        public ExtendedProtectionPolicyTypeConverter() { }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Type? destinationType) { throw null; }
        [System.Runtime.Versioning.UnsupportedOSPlatformAttribute("browser")]
        public override object? ConvertTo(System.ComponentModel.ITypeDescriptorContext? context, System.Globalization.CultureInfo? culture, object? value, System.Type destinationType) { throw null; }
    }
}
namespace System.Timers
{
    public partial class ElapsedEventArgs : System.EventArgs
    {
        internal ElapsedEventArgs() { }
        public System.DateTime SignalTime { get { throw null; } }
    }
    public delegate void ElapsedEventHandler(object? sender, System.Timers.ElapsedEventArgs e);
    [System.ComponentModel.DefaultEventAttribute("Elapsed")]
    [System.ComponentModel.DefaultPropertyAttribute("Interval")]
    public partial class Timer : System.ComponentModel.Component, System.ComponentModel.ISupportInitialize
    {
        public Timer() { }
        public Timer(double interval) { }
        [System.ComponentModel.DefaultValueAttribute(true)]
        public bool AutoReset { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(false)]
        public bool Enabled { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(100d)]
        public double Interval { get { throw null; } set { } }
        public override System.ComponentModel.ISite? Site { get { throw null; } set { } }
        [System.ComponentModel.DefaultValueAttribute(null)]
        public System.ComponentModel.ISynchronizeInvoke? SynchronizingObject { get { throw null; } set { } }
        public event System.Timers.ElapsedEventHandler Elapsed { add { } remove { } }
        public void BeginInit() { }
        public void Close() { }
        protected override void Dispose(bool disposing) { }
        public void EndInit() { }
        public void Start() { }
        public void Stop() { }
    }
    [System.AttributeUsageAttribute(System.AttributeTargets.All)]
    public partial class TimersDescriptionAttribute : System.ComponentModel.DescriptionAttribute
    {
        public TimersDescriptionAttribute(string description) { }
        public override string Description { get { throw null; } }
    }
}
