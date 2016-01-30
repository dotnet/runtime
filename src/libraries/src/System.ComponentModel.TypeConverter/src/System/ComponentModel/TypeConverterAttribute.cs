// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies what type to use as
    ///       a converter for the object
    ///       this
    ///       attribute is bound to. This class cannot
    ///       be inherited.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class TypeConverterAttribute : Attribute
    {
        private string _typeName;

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.TypeConverterAttribute'/> class, using 
        ///    the specified type as the data converter for the object this attribute
        ///    is bound
        ///    to.</para>
        /// </devdoc>
        public TypeConverterAttribute(Type type)
        {
            _typeName = type.AssemblyQualifiedName;
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.TypeConverterAttribute'/> class, using 
        ///    the specified type name as the data converter for the object this attribute is bound to.</para>
        /// </devdoc>
        public TypeConverterAttribute(string typeName)
        {
            _typeName = typeName;
        }

        /// <devdoc>
        /// <para>Gets the fully qualified type name of the <see cref='System.Type'/>
        /// to use as a converter for the object this attribute
        /// is bound to.</para>
        /// </devdoc>
        public string ConverterTypeName
        {
            get
            {
                return _typeName;
            }
        }

        public override bool Equals(object obj)
        {
            TypeConverterAttribute other = obj as TypeConverterAttribute;
            return (other != null) && other.ConverterTypeName == _typeName;
        }

        public override int GetHashCode()
        {
            return _typeName.GetHashCode();
        }
    }
}

