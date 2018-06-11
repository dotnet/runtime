// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the data source and data member properties for a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ComplexBindingPropertiesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        /// </summary>
        public ComplexBindingPropertiesAttribute()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        /// </summary>
        public ComplexBindingPropertiesAttribute(string dataSource)
        {
            DataSource = dataSource;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        /// </summary>
        public ComplexBindingPropertiesAttribute(string dataSource, string dataMember)
        {
            DataSource = dataSource;
            DataMember = dataMember;
        }

        /// <summary>
        /// Gets the name of the data source property for the component this attribute is
        /// bound to.
        /// </summary>
        public string DataSource { get; }

        /// <summary>
        /// Gets the name of the data member property for the component this attribute is
        /// bound to.
        /// </summary>
        public string DataMember { get; }

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/>, which is <see langword='null'/>. This
        /// <see langword='static '/>field is read-only. 
        /// </summary>
        public static readonly ComplexBindingPropertiesAttribute Default = new ComplexBindingPropertiesAttribute();

        public override bool Equals(object obj)
        {
            return obj is ComplexBindingPropertiesAttribute other &&
                   other.DataSource == DataSource &&
                   other.DataMember == DataMember;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
