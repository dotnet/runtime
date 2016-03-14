// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies that
    ///       this property can be combined with properties belonging to
    ///       other objects in a properties window.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class MergablePropertyAttribute : Attribute
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies that a property can be combined with properties belonging to other
        ///       objects in a properties window. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly MergablePropertyAttribute Yes = new MergablePropertyAttribute(true);

        /// <devdoc>
        ///    <para>
        ///       Specifies that a property cannot be combined with properties belonging to
        ///       other objects in a properties window. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public static readonly MergablePropertyAttribute No = new MergablePropertyAttribute(false);

        /// <devdoc>
        ///    <para>
        ///       Specifies the default value, which is <see cref='System.ComponentModel.MergablePropertyAttribute.Yes'/>, that is a property can be combined with
        ///       properties belonging to other objects in a properties window. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly MergablePropertyAttribute Default = Yes;

        private bool _allowMerge;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.MergablePropertyAttribute'/>
        ///       class.
        ///    </para>
        /// </devdoc>
        public MergablePropertyAttribute(bool allowMerge)
        {
            _allowMerge = allowMerge;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether this
        ///       property can be combined with properties belonging to other objects in a
        ///       properties window.
        ///    </para>
        /// </devdoc>
        public bool AllowMerge
        {
            get
            {
                return _allowMerge;
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

            MergablePropertyAttribute other = obj as MergablePropertyAttribute;
            return other != null && other.AllowMerge == _allowMerge;
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

        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        public override bool IsDefaultAttribute()
        {
            return (this.Equals(Default));
        }
    }
}
