// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>
    ///        Specifies what type to use as a converter for the object this
    ///        attribute is bound to. This class cannot be inherited.
    ///    </para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class TypeConverterAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///        Specifies the type to use as a converter for the object this attribute is bound to. This
        ///        <see langword='static '/>field is read-only. </para>
        /// </summary>
        public static readonly TypeConverterAttribute Default = new TypeConverterAttribute();

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.TypeConverterAttribute'/> class with the
        ///       default type converter, which is an empty string ("").
        ///    </para>
        /// </summary>
        public TypeConverterAttribute()
        {
            ConverterTypeName = string.Empty;
        }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref='System.ComponentModel.TypeConverterAttribute'/> class,
        ///         using the specified type as the data converter for the object this attribute is bound to.
        ///     </para>
        /// </summary>
        public TypeConverterAttribute(Type type)
        {
            ConverterTypeName = type.AssemblyQualifiedName;
        }

        /// <summary>
        ///     <para>
        ///         Initializes a new instance of the <see cref='System.ComponentModel.TypeConverterAttribute'/> class,
        ///         using the specified type name as the data converter for the object this attribute is bound to.
        ///     </para>
        /// </summary>
        public TypeConverterAttribute(string typeName)
        {
            ConverterTypeName = typeName;
        }

        /// <summary>
        ///     <para>
        ///         Gets the fully qualified type name of the <see cref='System.Type'/> to use as a converter for
        ///         the object this attribute is bound to.
        ///     </para>
        /// </summary>
        public string ConverterTypeName { get; }

        public override bool Equals(object obj)
        {
            TypeConverterAttribute other = obj as TypeConverterAttribute;
            return (other != null) && other.ConverterTypeName == ConverterTypeName;
        }

        public override int GetHashCode()
        {
            return ConverterTypeName.GetHashCode();
        }
    }
}
