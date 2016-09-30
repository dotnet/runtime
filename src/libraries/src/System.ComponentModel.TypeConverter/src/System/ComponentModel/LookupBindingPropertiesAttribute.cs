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
    public sealed class LookupBindingPropertiesAttribute : Attribute
    {
        private readonly string _dataSource;
        private readonly string _displayMember;
        private readonly string _valueMember;
        private readonly string _lookupMember;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/> class.
        ///    </para>
        /// </summary>
        public LookupBindingPropertiesAttribute()
        {
            _dataSource = null;
            _displayMember = null;
            _valueMember = null;
            _lookupMember = null;
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/> class.
        ///    </para>
        /// </summary>
        public LookupBindingPropertiesAttribute(string dataSource, string displayMember, string valueMember, string lookupMember)
        {
            _dataSource = dataSource;
            _displayMember = displayMember;
            _valueMember = valueMember;
            _lookupMember = lookupMember;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the data source property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string DataSource
        {
            get
            {
                return _dataSource;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the display member property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string DisplayMember
        {
            get
            {
                return _displayMember;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the value member property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string ValueMember
        {
            get
            {
                return _valueMember;
            }
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the  member property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string LookupMember
        {
            get
            {
                return _lookupMember;
            }
        }

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.LookupBindingPropertiesAttribute'/>, which is <see langword='null'/>. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly LookupBindingPropertiesAttribute Default = new LookupBindingPropertiesAttribute();

        public override bool Equals(object obj)
        {
            LookupBindingPropertiesAttribute other = obj as LookupBindingPropertiesAttribute;
            return other != null &&
                   other.DataSource == _dataSource &&
                   other._displayMember == _displayMember &&
                   other._valueMember == _valueMember &&
                   other._lookupMember == _lookupMember;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
