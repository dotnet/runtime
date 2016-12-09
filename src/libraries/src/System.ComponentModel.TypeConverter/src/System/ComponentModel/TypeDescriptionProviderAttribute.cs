// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public sealed class TypeDescriptionProviderAttribute : Attribute
    {
        /// <summary>
        ///     Creates a new TypeDescriptionProviderAttribute object.
        /// </summary>
        public TypeDescriptionProviderAttribute(string typeName)
        {
            if (typeName == null)
            {
                throw new ArgumentNullException(nameof(typeName));
            }

            TypeName = typeName;
        }

        /// <summary>
        ///     Creates a new TypeDescriptionProviderAttribute object.
        /// </summary>
        public TypeDescriptionProviderAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            TypeName = type.AssemblyQualifiedName;
        }

        /// <summary>
        ///     The TypeName property returns the assembly qualified type name 
        ///     for the type description provider.
        /// </summary>
        public string TypeName { get; }
    }
}

