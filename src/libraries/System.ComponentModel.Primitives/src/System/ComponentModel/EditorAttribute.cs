// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the editor to use to change a property. This class cannot be inherited.
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class EditorAttribute : Attribute
    {
        private string? _typeId;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class
        /// with the default editor, which is no editor.
        /// </summary>
        public EditorAttribute()
        {
            EditorTypeName = string.Empty;
            EditorBaseTypeName = string.Empty;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class with the type name and base type
        /// name of the editor.
        /// </summary>
        public EditorAttribute(string typeName, string? baseTypeName)
        {
            ArgumentNullException.ThrowIfNull(typeName);

            EditorTypeName = typeName;
            EditorBaseTypeName = baseTypeName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class.
        /// </summary>
        public EditorAttribute(string typeName, Type baseType)
        {
            ArgumentNullException.ThrowIfNull(typeName);
            ArgumentNullException.ThrowIfNull(baseType);

            EditorTypeName = typeName;
            EditorBaseTypeName = baseType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class.
        /// </summary>
        public EditorAttribute(Type type, Type baseType)
        {
            ArgumentNullException.ThrowIfNull(type);
            ArgumentNullException.ThrowIfNull(baseType);

            EditorTypeName = type.AssemblyQualifiedName!;
            EditorBaseTypeName = baseType.AssemblyQualifiedName;
        }

        /// <summary>
        /// Gets the name of the base class or interface serving as a lookup key for this editor.
        /// </summary>
        public string? EditorBaseTypeName { get; }

        /// <summary>
        /// Gets the name of the editor class.
        /// </summary>
        public string EditorTypeName { get; }

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
                    string baseType = EditorBaseTypeName ?? string.Empty;
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

            return obj is EditorAttribute other
                && other.EditorTypeName == EditorTypeName
                && other.EditorBaseTypeName == EditorBaseTypeName;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
