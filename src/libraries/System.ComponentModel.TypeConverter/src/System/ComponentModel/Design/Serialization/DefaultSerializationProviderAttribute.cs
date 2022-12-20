// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// The default serialization provider attribute is placed on a serializer
    /// to indicate the class to use as a default provider of that type of
    /// serializer. To be a default serialization provider, a class must
    /// implement IDesignerSerilaizationProvider and have an empty
    /// constructor. The class itself can be internal to the assembly.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    public sealed class DefaultSerializationProviderAttribute : Attribute
    {
        /// <summary>
        /// Creates a new DefaultSerializationProviderAttribute
        /// </summary>
        public DefaultSerializationProviderAttribute(Type providerType)
        {
            ArgumentNullException.ThrowIfNull(providerType);

            ProviderTypeName = providerType.AssemblyQualifiedName!;
        }

        /// <summary>
        /// Creates a new DefaultSerializationProviderAttribute
        /// </summary>
        public DefaultSerializationProviderAttribute(string providerTypeName)
        {
            ArgumentNullException.ThrowIfNull(providerTypeName);

            ProviderTypeName = providerTypeName;
        }

        /// <summary>
        /// Returns the type name for the default serialization provider.
        /// </summary>
        public string ProviderTypeName { get; }
    }
}
