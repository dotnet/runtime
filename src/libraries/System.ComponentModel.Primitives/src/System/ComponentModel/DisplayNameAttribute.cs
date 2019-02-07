// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the display name for a property or event.
    /// The default is the name of the property or event.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Event | AttributeTargets.Class | AttributeTargets.Method)]
    public class DisplayNameAttribute : Attribute
    {
        /// <summary>
        /// Specifies the default value for the <see cref='System.ComponentModel.DisplayNameAttribute'/>,
        /// which is an empty string (""). This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DisplayNameAttribute Default = new DisplayNameAttribute();

        public DisplayNameAttribute() : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DisplayNameAttribute'/> class.
        /// </summary>
        public DisplayNameAttribute(string displayName)
        {
            DisplayNameValue = displayName;
        }

        /// <summary>
        /// Gets the description stored in this attribute.
        /// </summary>
        public virtual string DisplayName => DisplayNameValue;

        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        protected string DisplayNameValue { get; set; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DisplayNameAttribute other = obj as DisplayNameAttribute;
            return other != null && other.DisplayName == DisplayName;
        }

        public override int GetHashCode() => DisplayName.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
