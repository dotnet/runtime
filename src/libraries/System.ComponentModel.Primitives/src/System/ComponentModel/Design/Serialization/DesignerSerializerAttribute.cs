// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.


namespace System.ComponentModel.Design.Serialization
{
    /// <summary>
    /// This attribute can be placed on a class to indicate what serialization
    /// object should be used to serialize the class at design time.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class DesignerSerializerAttribute : Attribute
    {
        private string? _typeId;

        /// <summary>
        /// Creates a new designer serialization attribute.
        /// </summary>
        public DesignerSerializerAttribute(Type serializerType, Type baseSerializerType)
        {
            ArgumentNullException.ThrowIfNull(serializerType);
            ArgumentNullException.ThrowIfNull(baseSerializerType);

            SerializerTypeName = serializerType.AssemblyQualifiedName;
            SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Creates a new designer serialization attribute.
        /// </summary>
        public DesignerSerializerAttribute(string? serializerTypeName, Type baseSerializerType)
        {
            ArgumentNullException.ThrowIfNull(baseSerializerType);

            SerializerTypeName = serializerTypeName;
            SerializerBaseTypeName = baseSerializerType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Creates a new designer serialization attribute.
        /// </summary>
        public DesignerSerializerAttribute(string? serializerTypeName, string? baseSerializerTypeName)
        {
            SerializerTypeName = serializerTypeName;
            SerializerBaseTypeName = baseSerializerTypeName;
        }

        /// <summary>
        /// Retrieves the fully qualified type name of the serializer.
        /// </summary>
        public string? SerializerTypeName { get; }

        /// <summary>
        /// Retrieves the fully qualified type name of the serializer base type.
        /// </summary>
        public string? SerializerBaseTypeName { get; }

        /// <summary>
        /// This defines a unique ID for this attribute type. It is used
        /// by filtering algorithms to identify two attributes that are
        /// the same type. For most attributes, this just returns the
        /// Type instance for the attribute. EditorAttribute overrides
        /// this to include the type of the editor base type.
        /// </summary>
        public override object TypeId
        {
            get
            {
                if (_typeId == null)
                {
                    string baseType = SerializerBaseTypeName ?? string.Empty;
                    int comma = baseType.IndexOf(',');
                    if (comma != -1)
                    {
                        baseType = baseType.Substring(0, comma);
                    }
                    _typeId = GetType().FullName + baseType;
                }
                return _typeId;
            }
        }
    }
}
