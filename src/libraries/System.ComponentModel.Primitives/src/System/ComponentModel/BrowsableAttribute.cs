// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies whether a property or event should be displayed in a property
    /// browsing window.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public sealed class BrowsableAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a property or event can be modified at design time.
        /// This <see langword='static '/> field is read-only.
        /// </summary>
        public static readonly BrowsableAttribute Yes = new BrowsableAttribute(true);

        /// <summary>
        /// Specifies that a property or event cannot be modified at design time.'
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly BrowsableAttribute No = new BrowsableAttribute(false);

        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.BrowsableAttribute'/>,
        /// which is <see cref='System.ComponentModel.BrowsableAttribute.Yes'/>.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly BrowsableAttribute Default = Yes;

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.BrowsableAttribute'/> class.
        /// </summary>
        public BrowsableAttribute(bool browsable)
        {
            Browsable = browsable;
        }

        /// <summary>
        /// Gets a value indicating whether an object is browsable.
        /// </summary>
        public bool Browsable { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            BrowsableAttribute other = obj as BrowsableAttribute;
            return other?.Browsable == Browsable;
        }

        public override int GetHashCode() => Browsable.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
