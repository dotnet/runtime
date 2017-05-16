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
        public bool IsItemNameSetExplicitly { get { throw null; } }
        public bool IsKeyNameSetExplicitly { get { throw null; } }
        public bool IsNameSetExplicitly { get { throw null; } }
        public bool IsNamespaceSetExplicitly { get { throw null; } }
        public bool IsReference { get { throw null; } set { } }
        public bool IsReferenceSetExplicitly { get { throw null; } }
        public bool IsValueNameSetExplicitly { get { throw null; } }
        public string ItemName { get { throw null; } set { } }
        public string KeyName { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
        public string ValueName { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(3), Inherited = false, AllowMultiple = true)]
    public sealed partial class ContractNamespaceAttribute : System.Attribute
    {
        public ContractNamespaceAttribute(string contractNamespace) { }
        public string ClrNamespace { get { throw null; } set { } }
        public string ContractNamespace { get { throw null; } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(28), Inherited = false, AllowMultiple = false)]
    public sealed partial class DataContractAttribute : System.Attribute
    {
        public DataContractAttribute() { }
        public bool IsNameSetExplicitly { get { throw null; } }
        public bool IsNamespaceSetExplicitly { get { throw null; } }
        public bool IsReference { get { throw null; } set { } }
        public bool IsReferenceSetExplicitly { get { throw null; } }
        public string Name { get { throw null; } set { } }
        public string Namespace { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited = false, AllowMultiple = false)]
    public sealed partial class DataMemberAttribute : System.Attribute
    {
        public DataMemberAttribute() { }
        public bool EmitDefaultValue { get { throw null; } set { } }
        public bool IsNameSetExplicitly { get { throw null; } }
        public bool IsRequired { get { throw null; } set { } }
        public string Name { get { throw null; } set { } }
        public int Order { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(256), Inherited = false, AllowMultiple = false)]
    public sealed partial class EnumMemberAttribute : System.Attribute
    {
        public EnumMemberAttribute() { }
        public bool IsValueSetExplicitly { get { throw null; } }
        public string Value { get { throw null; } set { } }
    }
    [System.AttributeUsageAttribute((System.AttributeTargets)(384), Inherited = false, AllowMultiple = false)]
    public sealed partial class IgnoreDataMemberAttribute : System.Attribute
    {
        public IgnoreDataMemberAttribute() { }
    }
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
        public string MethodName { get { throw null; } }
        public System.Type Type { get { throw null; } }
    }
}
