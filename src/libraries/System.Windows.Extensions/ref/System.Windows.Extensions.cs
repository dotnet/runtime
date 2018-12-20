// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Drawing
{
    public class FontConverter : System.ComponentModel.TypeConverter
    {
        public FontConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, Attribute[] attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }

        public sealed class FontNameConverter : System.ComponentModel.TypeConverter, IDisposable
        {
            public FontNameConverter() { }
            void IDisposable.Dispose() { }
            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) { throw null; }
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
            public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
            public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
            public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        }

        public class FontUnitConverter : System.ComponentModel.EnumConverter
        {
            public FontUnitConverter() : base(typeof(object)) { }
            public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        }
    }
    public class IconConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public IconConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) { throw null; }
    }
    public class ImageConverter : System.ComponentModel.TypeConverter
    {
        public ImageConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, Attribute[] attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
    }
    public class ImageFormatConverter : System.ComponentModel.TypeConverter
    {
        public ImageFormatConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType) { throw null; }
        public override StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
    }
}
namespace System.Drawing.Printing
{
    public partial class MarginsConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public MarginsConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    public enum X509SelectionFlag
    {
        SingleSelection = 0x00,
        MultiSelection = 0x01
    }

    public sealed partial class X509Certificate2UI
    { 
        public static void DisplayCertificate(X509Certificates.X509Certificate2 certificate) { throw null; }
        public static void DisplayCertificate(X509Certificates.X509Certificate2 certificate, IntPtr hwndParent) { throw null; }
        public static X509Certificates.X509Certificate2Collection SelectFromCollection(X509Certificates.X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag) { throw null; }
        public static X509Certificates.X509Certificate2Collection SelectFromCollection(X509Certificates.X509Certificate2Collection certificates, string title, string message, X509SelectionFlag selectionFlag, IntPtr hwndParent) { throw null; }
    }
}
