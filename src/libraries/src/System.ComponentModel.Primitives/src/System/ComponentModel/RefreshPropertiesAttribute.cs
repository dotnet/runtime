// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para> Specifies how a designer refreshes when the property value is changed.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class RefreshPropertiesAttribute : Attribute
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

        /// <summary>
        /// </summary>
        /// <internalonly/>
        public RefreshPropertiesAttribute(RefreshProperties refresh)
        {
            RefreshProperties = refresh;
        }

        /// <summary>
        ///    <para>
        ///       Gets or sets
        ///       the refresh properties for the member.
        ///    </para>
        /// </summary>
        public RefreshProperties RefreshProperties { get; }

        /// <summary>
        ///    <para>
        ///       Overrides object's Equals method.
        ///    </para>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            RefreshPropertiesAttribute other = obj as RefreshPropertiesAttribute;
            return (other != null) && other.RefreshProperties == RefreshProperties;
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

        public override bool IsDefaultAttribute()
        {
            return Equals(RefreshPropertiesAttribute.Default);
        }
    }
}
