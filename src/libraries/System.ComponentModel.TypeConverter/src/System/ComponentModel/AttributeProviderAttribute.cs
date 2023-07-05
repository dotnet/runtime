// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Property)]
    public class AttributeProviderAttribute : Attribute
    {
        private const DynamicallyAccessedMemberTypes RequiredMemberTypes = DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicMethods | DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicNestedTypes | DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.PublicEvents;

        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute([DynamicallyAccessedMembers(RequiredMemberTypes)] string typeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            TypeName = typeName;
        }

        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute([DynamicallyAccessedMembers(RequiredMemberTypes)] string typeName, string propertyName)
        {
            ArgumentNullException.ThrowIfNull(typeName);
            ArgumentNullException.ThrowIfNull(propertyName);

            TypeName = typeName;
            PropertyName = propertyName;
        }

        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute([DynamicallyAccessedMembers(RequiredMemberTypes)] Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            TypeName = type.AssemblyQualifiedName;
        }

        /// <summary>
        /// The TypeName property returns the assembly qualified type name
        /// passed into the constructor.
        /// </summary>
        [DynamicallyAccessedMembers(RequiredMemberTypes)]
        public string? TypeName { get; }

        /// <summary>
        /// The TypeName property returns the property name that will be used to query attributes from.
        /// </summary>
        public string? PropertyName { get; }
    }
}
