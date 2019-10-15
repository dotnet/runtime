// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Runtime.CompilerServices;

namespace System.Windows.Markup
{
    /// <summary>
    /// Attribute to associate a ValueSerializer class with a value type or to override
    /// which value serializer to use for a property. A value serializer can be associated
    /// with an attached property by placing the attribute on the static accessor for the
    /// attached property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Struct | AttributeTargets.Enum | AttributeTargets.Property | AttributeTargets.Method, AllowMultiple = false, Inherited = true)]
    [TypeForwardedFrom("WindowsBase, Version=4.0.0.0, Culture=neutral, PublicKeyToken=31bf3856ad364e35")]
    public sealed class ValueSerializerAttribute : Attribute
    {
        private Type _valueSerializerType;
        private readonly string _valueSerializerTypeName;

        /// <summary>
        /// Constructor for the ValueSerializerAttribute
        /// </summary>
        /// <param name="valueSerializerType">Type of the value serializer being associated with a type or property</param>
        public ValueSerializerAttribute(Type valueSerializerType)
        {
            _valueSerializerType = valueSerializerType;
        }

        /// <summary>
        /// Constructor for the ValueSerializerAttribute
        /// </summary>
        /// <param name="valueSerializerTypeName">Fully qualified type name of the value serializer being associated with a type or property</param>
        public ValueSerializerAttribute(string valueSerializerTypeName)
        {
            _valueSerializerTypeName = valueSerializerTypeName;
        }

        /// <summary>
        /// The type of the value serializer to create for this type or property.
        /// </summary>
        public Type ValueSerializerType
        {
            get
            {
                if (_valueSerializerType == null && _valueSerializerTypeName != null)
                {
                    _valueSerializerType = Type.GetType(_valueSerializerTypeName);
                }

                return _valueSerializerType;
            }
        }

        /// <summary>
        /// The assembly qualified name of the value serializer type for this type or property.
        /// </summary>
        public string ValueSerializerTypeName
        {
            get
            {
                if (_valueSerializerType != null)
                {
                    return _valueSerializerType.AssemblyQualifiedName;
                }
                else
                {
                    return _valueSerializerTypeName;
                }
            }
        }
    }
}
