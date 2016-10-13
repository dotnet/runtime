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
        public System.Globalization.DateTimeStyles DateTimeStyles { get { throw null; } set { } }
        public System.IFormatProvider FormatProvider { get { throw null; } }
        public string FormatString { get { throw null; } }
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
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { get { throw null; } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { throw null; } }
        public System.Collections.ObjectModel.ReadOnlyCollection<System.Type> KnownTypes { get { throw null; } }
        public bool SerializeReadOnlyTypes { get { throw null; } }
        public bool UseSimpleDictionaryFormat { get { throw null; } }
        public object ReadObject(System.IO.Stream stream) { throw null; }
        public void WriteObject(System.IO.Stream stream, object graph) { }
    }
    public partial class DataContractJsonSerializerSettings
    {
        public DataContractJsonSerializerSettings() { }
        public System.Runtime.Serialization.DateTimeFormat DateTimeFormat { get { throw null; } set { } }
        public System.Runtime.Serialization.EmitTypeInformation EmitTypeInformation { get { throw null; } set { } }
        public System.Collections.Generic.IEnumerable<System.Type> KnownTypes { get { throw null; } set { } }
        public int MaxItemsInObjectGraph { get { throw null; } set { } }
        public string RootName { get { throw null; } set { } }
        public bool SerializeReadOnlyTypes { get { throw null; } set { } }
        public bool UseSimpleDictionaryFormat { get { throw null; } set { } }
    }
}
