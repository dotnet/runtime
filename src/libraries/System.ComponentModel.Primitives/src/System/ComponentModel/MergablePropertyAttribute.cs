// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies that this property can be combined with properties belonging to
    /// other objects in a properties window.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class MergablePropertyAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a property can be combined with properties belonging to other
        /// objects in a properties window.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly MergablePropertyAttribute Yes = new MergablePropertyAttribute(true);

        /// <summary>
        /// Specifies that a property cannot be combined with properties belonging to
        /// other objects in a properties window.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly MergablePropertyAttribute No = new MergablePropertyAttribute(false);

        /// <summary>
        /// Specifies the default value, which is <see cref='System.ComponentModel.MergablePropertyAttribute.Yes'/>,
        /// that is a property can be combined with properties belonging to other objects
        /// in a properties window.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly MergablePropertyAttribute Default = Yes;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.MergablePropertyAttribute'/>
        /// class.
        /// </summary>
        public MergablePropertyAttribute(bool allowMerge)
        {
            AllowMerge = allowMerge;
        }

        /// <summary>
        /// Gets a value indicating whether this property can be combined with properties
        /// belonging to other objects in a properties window.
        /// </summary>
        public bool AllowMerge { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            MergablePropertyAttribute other = obj as MergablePropertyAttribute;
            return other?.AllowMerge == AllowMerge;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
