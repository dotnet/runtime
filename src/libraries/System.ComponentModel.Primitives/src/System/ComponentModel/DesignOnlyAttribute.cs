// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether a property can only be set at design time.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class DesignOnlyAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a property can be set only at design time.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignOnlyAttribute Yes = new DesignOnlyAttribute(true);

        /// <summary>
        /// Specifies that a property can be set at design time or at run time.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignOnlyAttribute No = new DesignOnlyAttribute(false);

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.DesignOnlyAttribute'/>,
        /// which is <see cref='System.ComponentModel.DesignOnlyAttribute.No'/>.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignOnlyAttribute Default = No;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignOnlyAttribute'/> class.
        /// </summary>
        public DesignOnlyAttribute(bool isDesignOnly)
        {
            IsDesignOnly = isDesignOnly;
        }

        /// <summary>
        /// Gets a value indicating whether a property can be set only at design time.
        /// </summary>
        public bool IsDesignOnly { get; }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is DesignOnlyAttribute other && other.IsDesignOnly == IsDesignOnly;

        public override int GetHashCode() => IsDesignOnly.GetHashCode();

        public override bool IsDefaultAttribute() => IsDesignOnly == Default.IsDesignOnly;
    }
}
