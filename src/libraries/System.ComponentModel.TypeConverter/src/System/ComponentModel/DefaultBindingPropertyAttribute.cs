// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the default binding property for a component.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultBindingPropertyAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/> class.
        ///    </para>
        /// </summary>
        public DefaultBindingPropertyAttribute()
        {
        }

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/> class.
        ///    </para>
        /// </summary>
        public DefaultBindingPropertyAttribute(string name)
        {
            Name = name;
        }

        /// <summary>
        ///    <para>
        ///       Gets the name of the default binding property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </summary>
        public string Name { get; }

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.DefaultBindingPropertyAttribute'/>, which is <see langword='null'/>. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly DefaultBindingPropertyAttribute Default = new DefaultBindingPropertyAttribute();

        public override bool Equals(object obj)
        {
            DefaultBindingPropertyAttribute other = obj as DefaultBindingPropertyAttribute;
            return other != null && other.Name == Name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
