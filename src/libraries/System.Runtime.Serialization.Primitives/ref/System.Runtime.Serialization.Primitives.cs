// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
// ------------------------------------------------------------------------------
// Changes to this file must follow the http://aka.ms/api-review process.
// ------------------------------------------------------------------------------

[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.OnDeserializedAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.OnDeserializingAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.OnSerializedAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.OnSerializingAttribute))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.SerializationException))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.StreamingContext))]
[assembly: System.Runtime.CompilerServices.TypeForwardedTo(typeof(System.Runtime.Serialization.StreamingContextStates))]

namespace System.Runtime.Serialization
{
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited = false, AllowMultiple = false)]
    public sealed partial class CollectionDataContractAttribute : System.Attribute
    {
        public CollectionDataContractAttribute() { }
        public bool IsItemNameSetExplicitly { get { return default(bool); } }
        public bool IsKeyNameSetExplicitly { get { return default(bool); } }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsNamespaceSetExplicitly { get { return default(bool); } }
        public bool IsReference { get { return default(bool); } set { } }
        public bool IsReferenceSetExplicitly { get { return default(bool); } }
        public bool IsValueNameSetExplicitly { get { return default(bool); } }
        public string ItemName { get { return default(string); } set { } }
        public string KeyName { get { return default(string); } set { } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
        public string ValueName { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), Inherited = false, AllowMultiple = true)]
    public sealed partial class ContractNamespaceAttribute : System.Attribute
    {
        public ContractNamespaceAttribute(string contractNamespace) { }
        public string ClrNamespace { get { return default(string); } set { } }
        public string ContractNamespace { get { return default(string); } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(28), Inherited = false, AllowMultiple = false)]
    public sealed partial class DataContractAttribute : System.Attribute
    {
        public DataContractAttribute() { }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsNamespaceSetExplicitly { get { return default(bool); } }
        public bool IsReference { get { return default(bool); } set { } }
        public bool IsReferenceSetExplicitly { get { return default(bool); } }
        public string Name { get { return default(string); } set { } }
        public string Namespace { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited = false, AllowMultiple = false)]
    public sealed partial class DataMemberAttribute : System.Attribute
    {
        public DataMemberAttribute() { }
        public bool EmitDefaultValue { get { return default(bool); } set { } }
        public bool IsNameSetExplicitly { get { return default(bool); } }
        public bool IsRequired { get { return default(bool); } set { } }
        public string Name { get { return default(string); } set { } }
        public int Order { get { return default(int); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited = false, AllowMultiple = false)]
    public sealed partial class EnumMemberAttribute : System.Attribute
    {
        public EnumMemberAttribute() { }
        public bool IsValueSetExplicitly { get { return default(bool); } }
        public string Value { get { return default(string); } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited = false, AllowMultiple = false)]
    public sealed partial class IgnoreDataMemberAttribute : System.Attribute
    {
        public IgnoreDataMemberAttribute() { }
    }
    [Serializable]
    public partial class InvalidDataContractException : System.Exception
    {
        public InvalidDataContractException() { }
        public InvalidDataContractException(string message) { }
        public InvalidDataContractException(string message, System.Exception innerException) { }
        protected InvalidDataContractException(SerializationInfo info, StreamingContext context) : base(info, context) { }
    }
    public partial interface ISerializationSurrogateProvider
    {
        object GetDeserializedObject(object obj, System.Type targetType);
        object GetObjectToSerialize(object obj, System.Type targetType);
        System.Type GetSurrogateType(System.Type type);
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(12), Inherited = true, AllowMultiple = true)]
    public sealed partial class KnownTypeAttribute : System.Attribute
    {
        public KnownTypeAttribute(string methodName) { }
        public KnownTypeAttribute(System.Type type) { }
        public string MethodName { get { return default(string); } }
        public System.Type Type { get { return default(System.Type); } }
    }
}
