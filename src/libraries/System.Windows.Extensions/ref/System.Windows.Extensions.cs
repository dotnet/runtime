// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Drawing
{
    public partial class FontConverter : System.ComponentModel.TypeConverter
    {
        public FontConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override object CreateInstance(System.ComponentModel.ITypeDescriptorContext context, System.Collections.IDictionary propertyValues) { throw null; }
        public override bool GetCreateInstanceSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        public sealed partial class FontNameConverter : System.ComponentModel.TypeConverter, System.IDisposable
        {
            public FontNameConverter() { }
            public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
            public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
            public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
            public override bool GetStandardValuesExclusive(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
            public override bool GetStandardValuesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
            void System.IDisposable.Dispose() { }
        }
        public partial class FontUnitConverter : System.ComponentModel.EnumConverter
        {
            public FontUnitConverter() : base (default(System.Type)) { }
            public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
        }
    }
    public partial class IconConverter : System.ComponentModel.ExpandableObjectConverter
    {
        public IconConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
    }
    public partial class ImageConverter : System.ComponentModel.TypeConverter
    {
        public ImageConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.PropertyDescriptorCollection GetProperties(System.ComponentModel.ITypeDescriptorContext context, object value, System.Attribute[] attributes) { throw null; }
        public override bool GetPropertiesSupported(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
    }
    public partial class ImageFormatConverter : System.ComponentModel.TypeConverter
    {
        public ImageFormatConverter() { }
        public override bool CanConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Type sourceType) { throw null; }
        public override bool CanConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType) { throw null; }
        public override object ConvertFrom(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value) { throw null; }
        public override object ConvertTo(System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, System.Type destinationType) { throw null; }
        public override System.ComponentModel.TypeConverter.StandardValuesCollection GetStandardValues(System.ComponentModel.ITypeDescriptorContext context) { throw null; }
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
namespace System.Media
{
    public partial class SoundPlayer : System.ComponentModel.Component, System.Runtime.Serialization.ISerializable
    {
        public SoundPlayer() { }
        public SoundPlayer(System.IO.Stream stream) { }
        protected SoundPlayer(System.Runtime.Serialization.SerializationInfo serializationInfo, System.Runtime.Serialization.StreamingContext context) { }
        public SoundPlayer(string soundLocation) { }
        public bool IsLoadCompleted { get { throw null; } }
        public int LoadTimeout { get { throw null; } set { } }
        public string SoundLocation { get { throw null; } set { } }
        public System.IO.Stream Stream { get { throw null; } set { } }
        public object Tag { get { throw null; } set { } }
        public event System.ComponentModel.AsyncCompletedEventHandler LoadCompleted { add { } remove { } }
        public event System.EventHandler SoundLocationChanged { add { } remove { } }
        public event System.EventHandler StreamChanged { add { } remove { } }
        public void Load() { }
        public void LoadAsync() { }
        protected virtual void OnLoadCompleted(System.ComponentModel.AsyncCompletedEventArgs e) { }
        protected virtual void OnSoundLocationChanged(System.EventArgs e) { }
        protected virtual void OnStreamChanged(System.EventArgs e) { }
        public void Play() { }
        public void PlayLooping() { }
        public void PlaySync() { }
        public void Stop() { }
        void System.Runtime.Serialization.ISerializable.GetObjectData(System.Runtime.Serialization.SerializationInfo info, System.Runtime.Serialization.StreamingContext context) { }
    }
    public partial class SystemSound
    {
        internal SystemSound() { }
        public void Play() { }
    }
    public static partial class SystemSounds
    {
        public static System.Media.SystemSound Asterisk { get { throw null; } }
        public static System.Media.SystemSound Beep { get { throw null; } }
        public static System.Media.SystemSound Exclamation { get { throw null; } }
        public static System.Media.SystemSound Hand { get { throw null; } }
        public static System.Media.SystemSound Question { get { throw null; } }
    }
}
namespace System.Security.Cryptography.X509Certificates
{
    public sealed partial class X509Certificate2UI
    {
        public X509Certificate2UI() { }
        public static void DisplayCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate) { }
        public static void DisplayCertificate(System.Security.Cryptography.X509Certificates.X509Certificate2 certificate, System.IntPtr hwndParent) { }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection SelectFromCollection(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates, string title, string message, System.Security.Cryptography.X509Certificates.X509SelectionFlag selectionFlag) { throw null; }
        public static System.Security.Cryptography.X509Certificates.X509Certificate2Collection SelectFromCollection(System.Security.Cryptography.X509Certificates.X509Certificate2Collection certificates, string title, string message, System.Security.Cryptography.X509Certificates.X509SelectionFlag selectionFlag, System.IntPtr hwndParent) { throw null; }
    }
    public enum X509SelectionFlag
    {
        SingleSelection = 0,
        MultiSelection = 1,
    }
}
