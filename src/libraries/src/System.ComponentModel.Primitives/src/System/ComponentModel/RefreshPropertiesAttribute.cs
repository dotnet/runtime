// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para> Specifies how a designer refreshes when the property value is changed.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class RefreshPropertiesAttribute : Attribute, IIsDefaultAttribute
    {
        /// <summary>
        ///    <para>
        ///       Indicates all properties should
        ///       be refreshed if the property value is changed. This field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly RefreshPropertiesAttribute All = new RefreshPropertiesAttribute(RefreshProperties.All);

        /// <summary>
        ///    <para>
        ///       Indicates all properties should
        ///       be invalidated and repainted if the
        ///       property value is changed. This field is read-only.
        ///    </para>
        /// </summary>
        public static readonly RefreshPropertiesAttribute Repaint = new RefreshPropertiesAttribute(RefreshProperties.Repaint);

        /// <summary>
        ///    <para>
        ///       Indicates that by default
        ///       no
        ///       properties should be refreshed if the property value
        ///       is changed. This field is read-only.
        ///    </para>
        /// </summary>
        public static readonly RefreshPropertiesAttribute Default = new RefreshPropertiesAttribute(RefreshProperties.None);

        private RefreshProperties _refresh;

        /// <summary>
        /// </summary>
        /// <internalonly/>
        public RefreshPropertiesAttribute(RefreshProperties refresh)
        {
            _refresh = refresh;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets
        ///       the refresh properties for the member.
        ///    </para>
        /// </summary>
        public RefreshProperties RefreshProperties
        {
            get
            {
                return _refresh;
            }
        }

        /// <summary>
        ///    <para>
        ///       Overrides object's Equals method.
        ///    </para>
        /// </summary>
        public override bool Equals(object value)
        {
            if (value is RefreshPropertiesAttribute)
            {
                return ((RefreshPropertiesAttribute)value).RefreshProperties == _refresh;
            }
            return false;
        }

        /// <summary>
        ///    <para>
        ///       Returns the hashcode for this object.
        ///    </para>
        /// </summary>
        public override int GetHashCode()
        {
            return base.GetHashCode();
        }

        /// <summary>
        ///    <para>Gets a value indicating whether the current attribute is the default.</para>
        /// </summary>
        bool IIsDefaultAttribute.IsDefaultAttribute()
        {
            return this.Equals(Default);
        }
    }
}
