// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///  Specifies that a object has no sub properties that are editable.
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ImmutableObjectAttribute : Attribute
    {
        /// <devdoc>
        ///  Specifies that a object has no sub properties that are editable.
        ///
        ///  This is usually used in the properties window to determine if an expandable object
        ///  should be rendered as read-only.
        /// </devdoc>
        public static readonly ImmutableObjectAttribute Yes = new ImmutableObjectAttribute(true);

        /// <devdoc>
        ///  Specifies that a object has at least one editable sub-property.
        ///
        ///  This is usually used in the properties window to determine if an expandable object
        ///  should be rendered as read-only.
        /// </devdoc>
        public static readonly ImmutableObjectAttribute No = new ImmutableObjectAttribute(false);


        /// <devdoc>
        ///  Defaults to ImmutableObjectAttribute.No
        /// </devdoc>
        public static readonly ImmutableObjectAttribute Default = No;

        private bool _immutable = true;

        /// <devdoc>
        ///  Constructs an ImmutableObjectAttribute object.
        ///
        /// </devdoc>
        public ImmutableObjectAttribute(bool immutable)
        {
            _immutable = immutable;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public bool Immutable
        {
            get
            {
                return _immutable;
            }
        }

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            ImmutableObjectAttribute other = obj as ImmutableObjectAttribute;
            return other != null && other.Immutable == _immutable;
        }

        /// <devdoc>
        ///    <para>
        ///       Returns the hashcode for this object.
        ///    </para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return base.GetHashCode();
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

