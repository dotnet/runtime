// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.Globalization;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the editor to use to change a property. This class cannot be inherited.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All, AllowMultiple = true, Inherited = true)]
    public sealed class EditorAttribute : Attribute
    {
        private string _baseTypeName;
        private string _typeName;
        private string _typeId;

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class with the default editor, which is
        ///    no editor.</para>
        /// </summary>
        public EditorAttribute()
        {
            _typeName = string.Empty;
            _baseTypeName = string.Empty;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class with the type name and base type
        ///    name of the editor.</para>
        /// </summary>
        public EditorAttribute(string typeName, string baseTypeName)
        {
            string temp = typeName.ToUpper(CultureInfo.InvariantCulture);
            Debug.Assert(temp.IndexOf(".DLL") == -1, "Came across: " + typeName + " . Please remove the .dll extension");
            _typeName = typeName;
            _baseTypeName = baseTypeName;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/> class.</para>
        /// </summary>
        public EditorAttribute(string typeName, Type baseType)
        {
            string temp = typeName.ToUpper(CultureInfo.InvariantCulture);
            Debug.Assert(temp.IndexOf(".DLL") == -1, "Came across: " + typeName + " . Please remove the .dll extension");
            _typeName = typeName;
            _baseTypeName = baseType.AssemblyQualifiedName;
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.EditorAttribute'/>
        /// class.</para>
        /// </summary>
        public EditorAttribute(Type type, Type baseType)
        {
            _typeName = type.AssemblyQualifiedName;
            _baseTypeName = baseType.AssemblyQualifiedName;
        }

        /// <summary>
        ///    <para>Gets the name of the base class or interface serving as a lookup key for this editor.</para>
        /// </summary>
        public string EditorBaseTypeName
        {
            get
            {
                return _baseTypeName;
            }
        }

        /// <summary>
        ///    <para>Gets the name of the editor class.</para>
        /// </summary>
        public string EditorTypeName
        {
            get
            {
                return _typeName;
            }
        }

        /// <internalonly/>
        /// <summary>
        ///    <para>
        ///       This defines a unique ID for this attribute type. It is used
        ///       by filtering algorithms to identify two attributes that are
        ///       the same type. For most attributes, this just returns the
        ///       Type instance for the attribute. EditorAttribute overrides
        ///       this to include the type of the editor base type.
        ///    </para>
        /// </summary>
        public override object TypeId
        {
            get
            {
                if (_typeId == null)
                {
                    string baseType = _baseTypeName;
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

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            EditorAttribute other = obj as EditorAttribute;

            return (other != null) && other._typeName == _typeName && other._baseTypeName == _baseTypeName;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}

