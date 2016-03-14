// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies whether a property should be localized.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class LocalizableAttribute : Attribute
    {
        private bool _isLocalizable = false;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.LocalizableAttribute'/> class.
        ///    </para>
        /// </devdoc>
        public LocalizableAttribute(bool isLocalizable)
        {
            _isLocalizable = isLocalizable;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether
        ///       a property should be localized.
        ///    </para>
        /// </devdoc>
        public bool IsLocalizable
        {
            get
            {
                return _isLocalizable;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Specifies that a property should be localized. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </devdoc>
        public static readonly LocalizableAttribute Yes = new LocalizableAttribute(true);

        /// <devdoc>
        ///    <para>
        ///       Specifies that a property should not be localized. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </devdoc>
        public static readonly LocalizableAttribute No = new LocalizableAttribute(false);

        /// <devdoc>
        ///    <para>
        ///       Specifies the default value, which is <see cref='System.ComponentModel.LocalizableAttribute.No'/> , that is
        ///       a property should not be localized. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public static readonly LocalizableAttribute Default = No;

        /// <internalonly/>
        /// <devdoc>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return (IsLocalizable == Default.IsLocalizable);
        }

        public override bool Equals(object obj)
        {
            LocalizableAttribute other = obj as LocalizableAttribute;
            return (other != null) && other.IsLocalizable == _isLocalizable;
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
