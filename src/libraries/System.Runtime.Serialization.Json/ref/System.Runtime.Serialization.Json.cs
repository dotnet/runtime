// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// ------------------------------------------------------------------------------
// Changes to this file must follow the https://aka.ms/api-review process.
// ------------------------------------------------------------------------------

namespace System.Runtime.Serialization
{
    public partial class DateTimeFormat
    {
        public DateTimeFormat(string formatString) { }
        public DateTimeFormat(string formatString, System.IFormatProvider formatProvider) { }
        public System.Globalization.DateTimeStyles DateTimeStyles { get { throw null; } set { } }
        public System.IFormatProvider FormatProvider { get { throw null; } }
        public string FormatString { get { throw null; } }
    }
    public enum EmitTypeInformation
    {
        AsNeeded = 0,
        Always = 1,
        Never = 2,
    }
}
namespace System.Runtime.Serialization.Json
{
    public sealed partial class DataContractJsonSerializer : System.Runtime.Serialization.XmlObjectSerializer
    {
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type>? knownTypes) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, System.Runtime.Serialization.Json.DataContractJsonSerializerSettings? settings) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, string? rootName) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, string? rootName, System.Collections.Generic.IEnumerable<System.Type>? knownTypes) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, System.Xml.XmlDictionaryString? rootName) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public DataContractJsonSerializer(System.Type type, System.Xml.XmlDictionaryString? rootName, System.Collections.Generic.IEnumerable<System.Type>? knownTypes) { }
        public System.Runtime.Serialization.DateTimeFormat? DateTimeFormat { get { throw null; } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { throw null; } }
        public bool IgnoreExtensionDataObject { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Type> KnownTypes { get { throw null; } }
        public int MaxItemsInObjectGraph { get { throw null; } }
        public bool SerializeReadOnlyTypes { get { throw null; } }
        public bool UseSimpleDictionaryFormat { get { throw null; } }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override bool IsStartObject(System.Xml.XmlDictionaryReader reader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override bool IsStartObject(System.Xml.XmlReader reader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override object? ReadObject(System.IO.Stream stream) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override object? ReadObject(System.Xml.XmlDictionaryReader reader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override object? ReadObject(System.Xml.XmlDictionaryReader reader, bool verifyObjectName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override object? ReadObject(System.Xml.XmlReader reader) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override object? ReadObject(System.Xml.XmlReader reader, bool verifyObjectName) { throw null; }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteEndObject(System.Xml.XmlDictionaryWriter writer) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteEndObject(System.Xml.XmlWriter writer) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteObject(System.IO.Stream stream, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteObject(System.Xml.XmlDictionaryWriter writer, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteObject(System.Xml.XmlWriter writer, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteObjectContent(System.Xml.XmlDictionaryWriter writer, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteObjectContent(System.Xml.XmlWriter writer, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteStartObject(System.Xml.XmlDictionaryWriter writer, object? graph) { }
        [System.Diagnostics.CodeAnalysis.RequiresUnreferencedCodeAttribute("Data Contract Serialization and Deserialization might require types that cannot be statically analyzed. Make sure all of the required types are preserved.")]
        public override void WriteStartObject(System.Xml.XmlWriter writer, object? graph) { }
    }
    public partial class DataContractJsonSerializerSettings
    {
        public DataContractJsonSerializerSettings() { }
        public System.Runtime.Serialization.DateTimeFormat? DateTimeFormat { get { throw null; } set { } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { throw null; } set { } }
        public bool IgnoreExtensionDataObject { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Type>? KnownTypes { get { throw null; } set { } }
        public int MaxItemsInObjectGraph { get { throw null; } set { } }
        public string? RootName { get { throw null; } set { } }
        public bool SerializeReadOnlyTypes { get { throw null; } set { } }
        public bool UseSimpleDictionaryFormat { get { throw null; } set { } }
    }
    public partial interface IXmlJsonReaderInitializer
    {
        void SetInput(byte[] buffer, int offset, int count, System.Text.Encoding? encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose? onClose);
        void SetInput(System.IO.Stream stream, System.Text.Encoding? encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose? onClose);
    }
    public partial interface IXmlJsonWriterInitializer
    {
        void SetOutput(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream);
    }
    public static partial class JsonReaderWriterFactory
    {
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, System.Text.Encoding? encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose? onClose) { throw null; }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, int offset, int count, System.Xml.XmlDictionaryReaderQuotas quotas) { throw null; }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(byte[] buffer, System.Xml.XmlDictionaryReaderQuotas quotas) { throw null; }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(System.IO.Stream stream, System.Text.Encoding? encoding, System.Xml.XmlDictionaryReaderQuotas quotas, System.Xml.OnXmlDictionaryReaderClose? onClose) { throw null; }
        public static System.Xml.XmlDictionaryReader CreateJsonReader(System.IO.Stream stream, System.Xml.XmlDictionaryReaderQuotas quotas) { throw null; }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream) { throw null; }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding) { throw null; }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream) { throw null; }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream, bool indent) { throw null; }
        public static System.Xml.XmlDictionaryWriter CreateJsonWriter(System.IO.Stream stream, System.Text.Encoding encoding, bool ownsStream, bool indent, string? indentChars) { throw null; }
    }
}
