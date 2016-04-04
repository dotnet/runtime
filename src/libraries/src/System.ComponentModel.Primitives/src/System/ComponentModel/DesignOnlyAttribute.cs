// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies whether a property can only be set at
    ///       design time.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class DesignOnlyAttribute : Attribute
    {
        private bool _isDesignOnly = false;

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignOnlyAttribute'/> class.
        ///    </para>
        /// </devdoc>
        public DesignOnlyAttribute(bool isDesignOnly)
        {
            _isDesignOnly = isDesignOnly;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets a value indicating whether a property
        ///       can be set only at design time.
        ///    </para>
        /// </devdoc>
        public bool IsDesignOnly
        {
            get
            {
                return _isDesignOnly;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Specifies that a property can be set only at design time. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </devdoc>
        public static readonly DesignOnlyAttribute Yes = new DesignOnlyAttribute(true);

        /// <devdoc>
        ///    <para>
        ///       Specifies
        ///       that a
        ///       property can be set at design time or at run
        ///       time. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignOnlyAttribute No = new DesignOnlyAttribute(false);

        /// <devdoc>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.DesignOnlyAttribute'/>, which is <see cref='System.ComponentModel.DesignOnlyAttribute.No'/>. This <see langword='static'/> field is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignOnlyAttribute Default = No;

        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        public override bool IsDefaultAttribute()
        {
            return IsDesignOnly == Default.IsDesignOnly;
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignOnlyAttribute other = obj as DesignOnlyAttribute;

            return (other != null) && other._isDesignOnly == _isDesignOnly;
        }

        public override int GetHashCode()
        {
            return _isDesignOnly.GetHashCode();
        }
    }
}
