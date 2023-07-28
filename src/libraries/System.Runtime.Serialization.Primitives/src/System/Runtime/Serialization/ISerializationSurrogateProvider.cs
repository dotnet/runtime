// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Provides the methods needed to substitute one type for another by DataContractSerializer during serialization
    /// and deserialization. This interface together with <see cref="ISerializationSurrogateProvider2"/> (and
    /// `System.Runtime.Serialization.Schema.ISerializationCodeDomSurrogateProvider`) replace
    /// the `IDataContractSurrogate` from .Net 4.8.
    /// </summary>
    public interface ISerializationSurrogateProvider
    {
        /// <summary>
        /// During serialization, deserialization, and schema import and export, returns a data contract type that substitutes the specified type.
        /// (Formerly known as `GetDataContractType` on the .Net 4.8 `IDataContractSurrogate` interface.)
        /// </summary>
        /// <param name="type">The runtime type to substitute.</param>
        /// <returns>The <see cref="Type"/> to substitute for the type value. This type must be serializable by the DataContractSerializer. For example,
        /// it must be marked with the DataContractAttribute attribute or other mechanisms that the serializer recognizes.</returns>
        Type GetSurrogateType(Type type);

        /// <summary>
        /// During serialization, returns an object that substitutes the specified object.
        /// </summary>
        /// <param name="obj">The object to substitute.</param>
        /// <param name="targetType">The <see cref="Type"/> that the substituted object should be assigned to.</param>
        /// <returns>The substituted object that will be serialized. The object must be serializable by the DataContractSerializer. For example,
        /// it must be marked with the <see cref="DataContractAttribute"/> attribute or other mechanisms that the serializer recognizes.</returns>
        object GetObjectToSerialize(object obj, Type targetType);

        /// <summary>
        /// During deserialization, returns an object that is a substitute for the specified object.
        /// </summary>
        /// <param name="obj">The deserialized object to be substituted.</param>
        /// <param name="targetType">The <see cref="Type"/> that the substituted object should be assigned to.</param>
        /// <returns>The substituted deserialized object. This object must be of a type that is serializable by the DataContractSerializer. For example,
        /// it must be marked with the <see cref="DataContractAttribute"/> attribute or other mechanisms that the serializer recognizes.</returns>
        object GetDeserializedObject(object obj, Type targetType);
    }
}
