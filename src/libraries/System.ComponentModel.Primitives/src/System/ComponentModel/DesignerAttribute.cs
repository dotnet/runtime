// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the class to use to implement design-time services.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = true)]
    public sealed class DesignerAttribute : Attribute
    {
        private string? _typeId;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerAttribute'/> class using the name of the type that
        /// provides design-time services.
        /// </summary>
        public DesignerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string designerTypeName)
        {
            ArgumentNullException.ThrowIfNull(designerTypeName);

            DesignerTypeName = designerTypeName;
            DesignerBaseTypeName = "System.ComponentModel.Design.IDesigner, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerAttribute'/> class using the type that provides
        /// design-time services.
        /// </summary>
        public DesignerAttribute([DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type designerType)
        {
            ArgumentNullException.ThrowIfNull(designerType);

            DesignerTypeName = designerType.AssemblyQualifiedName!;
            DesignerBaseTypeName = "System.ComponentModel.Design.IDesigner, System, Version=4.0.0.0, Culture=neutral, PublicKeyToken=b77a5c561934e089";
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerAttribute'/> class using the designer type and the
        /// base class for the designer.
        /// </summary>
        public DesignerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string designerTypeName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string designerBaseTypeName)
        {
            ArgumentNullException.ThrowIfNull(designerTypeName);

            DesignerTypeName = designerTypeName;
            DesignerBaseTypeName = designerBaseTypeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerAttribute'/> class, using the name of the designer
        /// class and the base class for the designer.
        /// </summary>
        public DesignerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] string designerTypeName,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type designerBaseType)
        {
            ArgumentNullException.ThrowIfNull(designerTypeName);
            ArgumentNullException.ThrowIfNull(designerBaseType);

            DesignerTypeName = designerTypeName;
            DesignerBaseTypeName = designerBaseType.AssemblyQualifiedName!;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerAttribute'/> class using the types of the designer and
        /// designer base class.
        /// </summary>
        public DesignerAttribute(
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type designerType,
            [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)] Type designerBaseType)
        {
            ArgumentNullException.ThrowIfNull(designerType);
            ArgumentNullException.ThrowIfNull(designerBaseType);

            DesignerTypeName = designerType.AssemblyQualifiedName!;
            DesignerBaseTypeName = designerBaseType.AssemblyQualifiedName!;
        }

        /// <summary>
        /// Gets the name of the base type of this designer.
        /// </summary>
        // Using PublicParameterlessConstructor to preserve the type. See https://github.com/mono/linker/issues/1878
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string DesignerBaseTypeName { get; }

        /// <summary>
        /// Gets the name of the designer type associated with this designer attribute.
        /// </summary>
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]
        public string DesignerTypeName { get; }

        /// <summary>
        /// This defines a unique ID for this attribute type. It is used
        /// by filtering algorithms to identify two attributes that are
        /// the same type. For most attributes, this just returns the
        /// Type instance for the attribute. DesignerAttribute overrides
        /// this to include the type of the designer base type.
        /// </summary>
        public override object TypeId
        {
            get
            {
                if (_typeId == null)
                {
                    string baseType = DesignerBaseTypeName ?? string.Empty;
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

        public override bool Equals(object? obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is DesignerAttribute other
                && other.DesignerBaseTypeName == DesignerBaseTypeName
                && other.DesignerTypeName == DesignerTypeName;
        }

        public override int GetHashCode() => HashCode.Combine(DesignerBaseTypeName, DesignerTypeName);
    }
}
