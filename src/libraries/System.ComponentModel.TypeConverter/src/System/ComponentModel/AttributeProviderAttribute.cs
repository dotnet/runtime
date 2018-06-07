// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    [SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Property)]
    public class AttributeProviderAttribute : Attribute
    {
        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute(string typeName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
        }

        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute(string typeName, string propertyName)
        {
            TypeName = typeName ?? throw new ArgumentNullException(nameof(typeName));
            PropertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
        }

        /// <summary>
        /// Creates a new AttributeProviderAttribute object.
        /// </summary>
        public AttributeProviderAttribute(Type type)
        {
            if (type == null)
            {
                throw new ArgumentNullException(nameof(type));
            }

            TypeName = type.AssemblyQualifiedName;
        }

        /// <summary>
        /// The TypeName property returns the assembly qualified type name 
        /// passed into the constructor.
        /// </summary>
        public string TypeName { get; }

        /// <summary>
        /// The TypeName property returns the property name that will be used to query attributes from.
        /// </summary>
        public string PropertyName { get; }
    }
}
