// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies the display name for a property or event.  The default is the name of the property or event.</para>
    /// </devdoc>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Performance", "CA1813:AvoidUnsealedAttributes")]
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class | AttributeTargets.Method)]
    public class DisplayNameAttribute : Attribute
    {
        /// <devdoc>
        /// <para>Specifies the default value for the <see cref='System.ComponentModel.DisplayNameAttribute'/> , which is an
        ///    empty string (""). This <see langword='static'/> field is read-only.</para>
        /// </devdoc>
        public static readonly DisplayNameAttribute Default = new DisplayNameAttribute();
        private string _displayName;

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public DisplayNameAttribute() : this(string.Empty)
        {
        }

        /// <devdoc>
        ///    <para>Initializes a new instance of the <see cref='System.ComponentModel.DisplayNameAttribute'/> class.</para>
        /// </devdoc>
        public DisplayNameAttribute(string displayName)
        {
            _displayName = displayName;
        }

        /// <devdoc>
        ///    <para>Gets the description stored in this attribute.</para>
        /// </devdoc>
        public virtual string DisplayName
        {
            get
            {
                return DisplayNameValue;
            }
        }

        /// <devdoc>
        ///     Read/Write property that directly modifies the string stored
        ///     in the description attribute. The default implementation
        ///     of the Description property simply returns this value.
        /// </devdoc>
        protected string DisplayNameValue
        {
            get
            {
                return _displayName;
            }
            set
            {
                _displayName = value;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DisplayNameAttribute other = obj as DisplayNameAttribute;

            return (other != null) && other.DisplayName == DisplayName;
        }

        public override int GetHashCode()
        {
            return DisplayName.GetHashCode();
        }

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return (this.Equals(Default));
        }
    }
}
