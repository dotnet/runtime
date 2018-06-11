// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the data source and data member properties for a component.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class LookupBindingPropertiesAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of
        /// the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/> class.
        /// </summary>
        public LookupBindingPropertiesAttribute()
        {
            DataSource = null;
            DisplayMember = null;
            ValueMember = null;
            LookupMember = null;
        }

        /// <summary>
        /// Initializes a new instance of
        /// the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/> class.
        /// </summary>
        public LookupBindingPropertiesAttribute(string dataSource, string displayMember, string valueMember, string lookupMember)
        {
            DataSource = dataSource;
            DisplayMember = displayMember;
            ValueMember = valueMember;
            LookupMember = lookupMember;
        }

        /// <summary>
        /// Gets the name of the data source property for the component this attribute is
        /// bound to.
        /// </summary>
        public string DataSource { get; }

        /// <summary>
        /// Gets the name of the display member property for the component this attribute is
        /// bound to.
        /// </summary>
        public string DisplayMember { get; }

        /// <summary>
        /// Gets the name of the value member property for the component this attribute is
        /// bound to.
        /// </summary>
        public string ValueMember { get; }

        /// <summary>
        /// Gets the name of the  member property for the component this attribute is
        /// bound to.
        /// </summary>
        public string LookupMember { get; }

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/>, which is <see langword='null'/>. This
        /// <see langword='static '/>field is read-only. 
        /// </summary>
        public static readonly LookupBindingPropertiesAttribute Default = new LookupBindingPropertiesAttribute();

        public override bool Equals(object obj)
        {
            return obj is LookupBindingPropertiesAttribute other &&
                   other.DataSource == DataSource &&
                   other.DisplayMember == DisplayMember &&
                   other.ValueMember == ValueMember &&
                   other.LookupMember == LookupMember;
        }

        public override int GetHashCode() => base.GetHashCode();
    }
}
