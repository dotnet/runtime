// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies that the property can be used as an application setting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [Obsolete("Use System.ComponentModel.SettingsBindableAttribute instead to work with the new settings model.")]
    public class RecommendedAsConfigurableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of
        /// the <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute'/> class.
        /// </summary>
        public RecommendedAsConfigurableAttribute(bool recommendedAsConfigurable)
        {
            RecommendedAsConfigurable = recommendedAsConfigurable;
        }

        /// <summary>
        /// Gets a value indicating whether the property this
        /// attribute is bound to can be used as an application setting.
        /// </summary>
        public bool RecommendedAsConfigurable { get; }

        /// <summary>
        /// Specifies that a property cannot be used as an application setting. This
        /// <see langword='static '/>field is read-only. 
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute No = new RecommendedAsConfigurableAttribute(false);

        /// <summary>
        /// Specifies
        /// that a property can be used as an application setting. This <see langword='static '/>field is read-only.
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute Yes = new RecommendedAsConfigurableAttribute(true);

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute'/>, which is <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute.No'/>. This <see langword='static '/>field is
        /// read-only.
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute Default = No;

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            return obj is RecommendedAsConfigurableAttribute other && other.RecommendedAsConfigurable == RecommendedAsConfigurable;
        }

        /// <summary>
        /// Returns the hashcode for this object.
        /// </summary>
        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => !RecommendedAsConfigurable;
    }
}
