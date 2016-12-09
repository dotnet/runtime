// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the data source and data member properties for a component.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class ComplexBindingPropertiesAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        ///    </para>
        /// </summary>
        public ComplexBindingPropertiesAttribute()
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        ///    </para>
        /// </summary>
        public ComplexBindingPropertiesAttribute(string dataSource)
        {
            DataSource = dataSource;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/> class.
        ///    </para>
        /// </summary>
        public ComplexBindingPropertiesAttribute(string dataSource, string dataMember)
        {
            DataSource = dataSource;
            DataMember = dataMember;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the data source property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string DataSource { get; }

        /// <summary>
        ///    <para>
        ///       Gets the name of the data member property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string DataMember { get; }

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.ComplexBindingPropertiesAttribute'/>, which is <see langword='null'/>. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly ComplexBindingPropertiesAttribute Default = new ComplexBindingPropertiesAttribute();

        public override bool Equals(object obj)
        {
            ComplexBindingPropertiesAttribute other = obj as ComplexBindingPropertiesAttribute;
            return other != null &&
                   other.DataSource == DataSource &&
                   other.DataMember == DataMember;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
