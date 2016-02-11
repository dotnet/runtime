// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------


namespace System.Runtime.Serialization
{
    public partial class DateTimeFormat
    {
        public DateTimeFormat(string formatString) { }
        public DateTimeFormat(string formatString, System.IFormatProvider formatProvider) { }
        public System.Globalization.DateTimeStyles DateTimeStyles { get { return default(System.Globalization.DateTimeStyles); } set { } }
        public System.IFormatProvider FormatProvider { get { return default(System.IFormatProvider); } }
        public string FormatString { get { return default(string); } }
    }
    public enum EmitTypeInformation
    {
        Always = 1,
        AsNeeded = 0,
        Never = 2,
    }
}
namespace System.Runtime.Serialization.Json
{
    public sealed partial class DataContractJsonSerializer
    {
        public DataContractJsonSerializer(System.Type type) { }
        public DataContractJsonSerializer(System.Type type, System.Collections.Generic.IEnumerable<System.Type> knownTypes) { }
        public DataContractJsonSerializer(System.Type type, System.Runtime.Serialization.Json.DataContractJsonSerializerSettings settings) { }
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { get { return default(System.Runtime.Serialization.DateTimeFormat); } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { return default(System.Runtime.Serialization.EmitTypeInformation); } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Type> KnownTypes { get { return default(System.Collections.ObjectModel.ReadOnlyCollection<System.Type>); } }
        public bool SerializeReadOnlyTypes { get { return default(bool); } }
        public bool UseSimpleDictionaryFormat { get { return default(bool); } }
        public object ReadObject(System.IO.Stream stream) { return default(object); }
        public void WriteObject(System.IO.Stream stream, object graph) { }
    }
    public partial class DataContractJsonSerializerSettings
    {
        public DataContractJsonSerializerSettings() { }
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { get { return default(System.Runtime.Serialization.DateTimeFormat); } set { } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { return default(System.Runtime.Serialization.EmitTypeInformation); } set { } }
        public System.Collections.Generic.IEnumerable<System.Type> KnownTypes { get { return default(System.Collections.Generic.IEnumerable<System.Type>); } set { } }
        public int MaxItemsInObjectGraph { get { return default(int); } set { } }
        public string RootName { get { return default(string); } set { } }
        public bool SerializeReadOnlyTypes { get { return default(bool); } set { } }
        public bool UseSimpleDictionaryFormat { get { return default(bool); } set { } }
    }
}
