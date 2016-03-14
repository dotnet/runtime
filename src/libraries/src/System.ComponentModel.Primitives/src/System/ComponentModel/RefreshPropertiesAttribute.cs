// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para> Specifies how a designer refreshes when the property value is changed.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class RefreshPropertiesAttribute : Attribute
    {
        /// <devdoc>
        ///    <para>
        ///       Indicates all properties should
        ///       be refreshed if the property value is changed. This field is
        ///       read-only.
        ///    </para>
        /// </devdoc>
        public static readonly RefreshPropertiesAttribute All = new RefreshPropertiesAttribute(RefreshProperties.All);

        /// <devdoc>
        ///    <para>
        ///       Indicates all properties should
        ///       be invalidated and repainted if the
        ///       property value is changed. This field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly RefreshPropertiesAttribute Repaint = new RefreshPropertiesAttribute(RefreshProperties.Repaint);

        /// <devdoc>
        ///    <para>
        ///       Indicates that by default
        ///       no
        ///       properties should be refreshed if the property value
        ///       is changed. This field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly RefreshPropertiesAttribute Default = new RefreshPropertiesAttribute(RefreshProperties.None);

        private RefreshProperties _refresh;

        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        public RefreshPropertiesAttribute(RefreshProperties refresh)
        {
            _refresh = refresh;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets or sets
        ///       the refresh properties for the member.
        ///    </para>
        /// </devdoc>
        public RefreshProperties RefreshProperties
        {
            get
            {
                return _refresh;
            }
        }

        /// <devdoc>
        ///    <para>
        ///       Overrides object's Equals method.
        ///    </para>
        /// </devdoc>
        public override bool Equals(object value)
        {
            if (value is RefreshPropertiesAttribute)
            {
                return (((RefreshPropertiesAttribute)value).RefreshProperties == _refresh);
            }
            return false;
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
        ///    <para>Gets a value indicating whether the current attribute is the default.</para>
        /// </devdoc>
        public override bool IsDefaultAttribute()
        {
            return this.Equals(Default);
        }
    }
}

