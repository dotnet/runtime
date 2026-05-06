// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether a property should be localized.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class LocalizableAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.LocalizableAttribute'/> class.
        /// </summary>
        public LocalizableAttribute(bool isLocalizable)
        {
            IsLocalizable = isLocalizable;
        }

        /// <summary>
        /// Gets a value indicating whether a property should be localized.
        /// </summary>
        public bool IsLocalizable { get; }

        /// <summary>
        /// Specifies that a property should be localized.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly LocalizableAttribute Yes = new LocalizableAttribute(true);

        /// <summary>
        /// Specifies that a property should not be localized.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly LocalizableAttribute No = new LocalizableAttribute(false);

        /// <summary>
        /// Specifies the default value, which is <see cref='System.ComponentModel.LocalizableAttribute.No'/>,
        /// that is a property should not be localized.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly LocalizableAttribute Default = No;

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is LocalizableAttribute other && other.IsLocalizable == IsLocalizable;

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => IsLocalizable == Default.IsLocalizable;
    }
}
