// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections.ObjectModel;
using System.Reflection;

namespace System.Runtime.Serialization
{
    /// <summary>
    /// Provides the methods needed to substitute one type for another by DataContractSerializer during export
    /// and import of XML schema documents (XSD). This interface builds upon <see cref="ISerializationSurrogateProvider"/>.
    /// Together (along with `System.Runtime.Serialization.Schema.ISerializationCodeDomSurrogateProvider`), these
    /// interfaces replace the `IDataContractSurrogate` from .Net 4.8.
    /// </summary>
    public interface ISerializationSurrogateProvider2 : ISerializationSurrogateProvider
    {
        /// <summary>
        /// During schema export operations, inserts annotations into the schema for non-null return values.
        /// </summary>
        /// <param name="memberInfo">A <see cref="MemberInfo"/> that describes the member.</param>
        /// <param name="dataContractType">The data contract type to be annotated.</param>
        /// <returns>An object that represents the annotation to be inserted into the XML schema definition.</returns>
        object? GetCustomDataToExport(MemberInfo memberInfo, Type dataContractType);

        /// <summary>
        /// During schema export operations, inserts annotations into the schema for non-null return values.
        /// </summary>
        /// <param name="runtimeType">The runtime type to be replaced.</param>
        /// <param name="dataContractType">The data contract type to be annotated.</param>
        /// <returns>An object that represents the annotation to be inserted into the XML schema definition.</returns>
        object? GetCustomDataToExport(Type runtimeType, Type dataContractType);

        /// <summary>
        /// Sets the collection of known types to use for serialization and deserialization of the custom data objects.
        /// </summary>
        /// <param name="customDataTypes">A <see cref="Collection{T}"/> of <see cref="Type"/> to add known types to.</param>
        void GetKnownCustomDataTypes(Collection<Type> customDataTypes);

        /// <summary>
        /// During schema import, returns the type referenced by the schema.
        /// </summary>
        /// <param name="typeName">The name of the type in schema.</param>
        /// <param name="typeNamespace">The namespace of the type in schema.</param>
        /// <param name="customData">The object that represents the annotation inserted into the XML schema definition, which is data that can be used for finding the referenced type.</param>
        /// <returns>The <see cref="Type"/> to use for the referenced type.</returns>
        Type? GetReferencedTypeOnImport(string typeName, string typeNamespace, object? customData);
    }
}
