// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies how a designer refreshes when the property value is changed.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class RefreshPropertiesAttribute : Attribute
    {
        /// <summary>
        /// Indicates all properties should be refreshed if the property value is changed.
        /// This field is read-only.
        /// </summary>
        public static readonly RefreshPropertiesAttribute All = new RefreshPropertiesAttribute(RefreshProperties.All);

        /// <summary>
        /// Indicates all properties should be invalidated and repainted if the property
        /// value is changed. This field is read-only.
        /// </summary>
        public static readonly RefreshPropertiesAttribute Repaint = new RefreshPropertiesAttribute(RefreshProperties.Repaint);

        /// <summary>
        /// Indicates that by default no properties should be refreshed if the property
        /// value is changed. This field is read-only.
        /// </summary>
        public static readonly RefreshPropertiesAttribute Default = new RefreshPropertiesAttribute(RefreshProperties.None);

        public RefreshPropertiesAttribute(RefreshProperties refresh)
        {
            RefreshProperties = refresh;
        }

        /// <summary>
        /// Gets the refresh properties for the member.
        /// </summary>
        public RefreshProperties RefreshProperties { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            RefreshPropertiesAttribute other = obj as RefreshPropertiesAttribute;
            return other?.RefreshProperties == RefreshProperties;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
