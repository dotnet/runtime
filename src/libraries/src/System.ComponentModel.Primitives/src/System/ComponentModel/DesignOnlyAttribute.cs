// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies whether a property can only be set at
    ///       design time.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class DesignOnlyAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignOnlyAttribute'/> class.
        ///    </para>
        /// </summary>
        public DesignOnlyAttribute(bool isDesignOnly) => IsDesignOnly = isDesignOnly;

        /// <summary>
        ///    <para>
        ///       Gets a value indicating whether a property can be set only at design time.
        ///    </para>
        /// </summary>
        public bool IsDesignOnly { get; }

        /// <summary>
        ///    <para>
        ///       Specifies that a property can be set only at design time. This <see langword='static '/>field is read-only. 
        ///    </para>
        /// </summary>
        public static readonly DesignOnlyAttribute Yes = new DesignOnlyAttribute(true);

        /// <summary>
        ///    <para>
        ///       Specifies that a property can be set at design time or at run time. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignOnlyAttribute No = new DesignOnlyAttribute(false);

        /// <summary>
        ///    <para>
        ///       Specifies the default value for the <see cref='System.ComponentModel.DesignOnlyAttribute'/>, which is <see cref='System.ComponentModel.DesignOnlyAttribute.No'/>. This <see langword='static'/> field is
        ///       read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignOnlyAttribute Default = No;

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignOnlyAttribute other = obj as DesignOnlyAttribute;
            return other?.IsDesignOnly == IsDesignOnly;
        }

        public override int GetHashCode() => IsDesignOnly.GetHashCode();

        public override bool IsDefaultAttribute() => IsDesignOnly == Default.IsDesignOnly;
    }
}
