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
    }
    public partial class ByteConverter : System.ComponentModel.BaseNumberConverter
    {
        public ByteConverter() { }
    }
    public partial class CharConverter : System.ComponentModel.TypeConverter
    {
        public CharConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class CollectionConverter : System.ComponentModel.TypeConverter
    {
        public CollectionConverter() { }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
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
    public partial class DoubleConverter : System.ComponentModel.BaseNumberConverter
    {
        public DoubleConverter() { }
    }
    public partial class EnumConverter : System.ComponentModel.TypeConverter
    {
        public EnumConverter(System.Type type) { }
        protected System.Type EnumType { get { return default(System.Type); } }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
    }
    public partial class GuidConverter : System.ComponentModel.TypeConverter
    {
        public GuidConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
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
    public partial interface ITypeDescriptorContext : System.IServiceProvider
    {
        System.ComponentModel.IContainer Container { get; }
        object Instance { get; }
        System.ComponentModel.PropertyDescriptor PropertyDescriptor { get; }
        void OnComponentChanged();
        bool OnComponentChanging();
    }
    public partial class MultilineStringConverter : System.ComponentModel.TypeConverter
    {
        public MultilineStringConverter() { }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
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
    }
    public abstract partial class PropertyDescriptor
    {
        internal PropertyDescriptor() { }
    }
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
        public object ConvertFromInvariantString(string text) { return default(object); }
        public object ConvertFromString(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, string text) { return default(object); }
        public object ConvertFromString(string text) { return default(object); }
        public virtual object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
        public object ConvertTo(object value, System.Type destinationType) { return default(object); }
        public string ConvertToInvariantString(object value) { return default(string); }
        public string ConvertToString(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(string); }
        public string ConvertToString(object value) { return default(string); }
        protected System.Exception GetConvertFromException(object value) { return default(System.Exception); }
        protected System.Exception GetConvertToException(object value, System.Type destinationType) { return default(System.Exception); }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(32767))]
    public sealed partial class TypeConverterAttribute : System.Attribute
    {
        public TypeConverterAttribute(string typeName) { }
        public TypeConverterAttribute(System.Type type) { }
        public string ConverterTypeName { get { return default(string); } }
        public override bool Equals(object obj) { return default(bool); }
        public override int GetHashCode() { return default(int); }
    }
    public sealed partial class TypeDescriptor
    {
        internal TypeDescriptor() { }
        public static System.ComponentModel.TypeConverter GetConverter(System.Type type) { return default(System.ComponentModel.TypeConverter); }
    }
    public abstract partial class TypeListConverter : System.ComponentModel.TypeConverter
    {
        protected TypeListConverter(System.Type[] types) { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { return default(bool); }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { return default(bool); }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { return default(object); }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { return default(object); }
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
