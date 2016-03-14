// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies whether the property this attribute is bound to
    ///       is read-only or read/write.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class ReadOnlyAttribute : Attribute
    {
        private bool _isReadOnly = false;

        /// <devdoc>
        ///    <para>
        ///       Specifies that the property this attribute is bound to is read-only and
        ///       cannot be modified in the server explorer. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public static readonly ReadOnlyAttribute Yes = new ReadOnlyAttribute(true);

        /// <devdoc>
        ///    <para>
        ///       Specifies that the property this attribute is bound to is read/write and can
        ///       be modified at design time. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly ReadOnlyAttribute No = new ReadOnlyAttribute(false);

        /// <devdoc>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.ReadOnlyAttribute'/> , which is <see cref='System.ComponentModel.ReadOnlyAttribute.No'/>, that is,
        ///       the property this attribute is bound to is read/write. This <see langword='static'/> field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly ReadOnlyAttribute Default = No;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.ReadOnlyAttribute'/> class.
        ///    </para>
        /// </devdoc>
        public ReadOnlyAttribute(bool isReadOnly)
        {
            _isReadOnly = isReadOnly;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether the property this attribute is bound to is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public bool IsReadOnly
        {
            get
            {
                return _isReadOnly;
            }
        }

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool Equals(object value)
        {
            if (this == value)
            {
                return true;
            }

            ReadOnlyAttribute other = value as ReadOnlyAttribute;

            return other != null && other.IsReadOnly == IsReadOnly;
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
        ///    <para>
        ///       Determines if this attribute is the default.
        ///    </para>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return (this.IsReadOnly == Default.IsReadOnly);
        }
    }
}

