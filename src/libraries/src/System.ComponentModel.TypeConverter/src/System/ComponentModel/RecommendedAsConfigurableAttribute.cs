// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies that the property can be
    ///       used as an application setting.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Property)]
    [Obsolete("Use System.ComponentModel.SettingsBindableAttribute instead to work with the new settings model.")]
    public class RecommendedAsConfigurableAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of
        ///       the <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute'/> class.
        ///    </para>
        /// </summary>
        public RecommendedAsConfigurableAttribute(bool recommendedAsConfigurable)
        {
            RecommendedAsConfigurable = recommendedAsConfigurable;
        }

        /// <summary>
        ///    <para>Gets a value indicating whether the property this
        ///       attribute is bound to can be used as an application setting.</para>
        /// </summary>
        public bool RecommendedAsConfigurable { get; }

        /// <summary>
        ///    <para>
        ///       Specifies that a property cannot be used as an application setting. This
        ///    <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute No = new RecommendedAsConfigurableAttribute(false);

        /// <summary>
        ///    <para>
        ///       Specifies
        ///       that a property can be used as an application setting. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute Yes = new RecommendedAsConfigurableAttribute(true);

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute'/>, which is <see cref='System.ComponentModel.RecommendedAsConfigurableAttribute.No'/>. This <see langword='static '/>field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly RecommendedAsConfigurableAttribute Default = No;

        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            RecommendedAsConfigurableAttribute other = obj as RecommendedAsConfigurableAttribute;

            return other != null && other.RecommendedAsConfigurable == RecommendedAsConfigurable;
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

        /// <internalonly/>
        /// <summary>
        /// </summary>
        public override bool IsDefaultAttribute()
        {
            return !RecommendedAsConfigurable;
        }
    }
}
