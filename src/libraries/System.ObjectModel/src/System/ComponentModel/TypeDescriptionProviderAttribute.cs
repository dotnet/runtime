// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class TypeDescriptionProviderAttribute : Attribute
    {
        /// <summary>
        /// Creates a new TypeDescriptionProviderAttribute object.
        /// </summary>
        public TypeDescriptionProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string typeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            TypeName = typeName;
        }

        /// <summary>
        /// Creates a new TypeDescriptionProviderAttribute object.
        /// </summary>
        public TypeDescriptionProviderAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            TypeName = type.AssemblyQualifiedName!;
        }

        /// <summary>
        /// The TypeName property returns the assembly qualified type name
        /// for the type description provider.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string TypeName { get; }
    }
}
