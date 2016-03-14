// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies the default property for a component.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class DefaultPropertyAttribute : Attribute
    {
        /// <devdoc>
        ///     This is the default event name.
        /// </devdoc>
        private readonly string _name;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.DefaultPropertyAttribute'/> class.
        ///    </para>
        /// </devdoc>
        public DefaultPropertyAttribute(string name)
        {
            _name = name;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the default property for the component this attribute is
        ///       bound to.
        ///    </para>
        /// </devdoc>
        public string Name
        {
            get
            {
                return _name;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.DefaultPropertyAttribute'/>, which is <see langword='null'/>. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </devdoc>
        public static readonly DefaultPropertyAttribute Default = new DefaultPropertyAttribute(null);

        public override bool Equals(object obj)
        {
            DefaultPropertyAttribute other = obj as DefaultPropertyAttribute;
            return (other != null) && other.Name == _name;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
