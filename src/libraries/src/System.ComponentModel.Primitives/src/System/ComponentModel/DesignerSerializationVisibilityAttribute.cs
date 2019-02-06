// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///  Specifies the visibility of this property or method as seen by the designer
    /// serializer.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Method | AttributeTargets.Field | AttributeTargets.Event)]
    public sealed class DesignerSerializationVisibilityAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a visual designer should serialize the contents of this property,
        /// rather than the property itself.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerSerializationVisibilityAttribute Content = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Content);

        /// <summary>
        /// Specifies that a visual designer will not serialize the value of this property.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerSerializationVisibilityAttribute Hidden = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Hidden);

        /// <summary>
        /// Specifies that a visual designer may use default rules when serializing the
        /// value of a property.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerSerializationVisibilityAttribute Visible = new DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility.Visible);

        /// <summary>
        /// Specifies the default value, which is <see cref='System.ComponentModel.DesignerSerializationVisibilityAttribute.Visible'/>,
        /// that is, a visual designer  uses default rules to generate the value of a property.
        /// This <see langword='static'/> field is read-only. 
        /// </summary>
        public static readonly DesignerSerializationVisibilityAttribute Default = Visible;

        /// <summary>
        /// Initializes a new instance of the System.ComponentModel.PersistContentsAttribute class.
        /// </summary>
        public DesignerSerializationVisibilityAttribute(DesignerSerializationVisibility visibility)
        {
            Visibility = visibility;
        }

        /// <summary>
        /// Gets a value indicating whether a visual designer must generate special code
        /// to persist the value of a property.
        /// </summary>
        public DesignerSerializationVisibility Visibility { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignerSerializationVisibilityAttribute other = obj as DesignerSerializationVisibilityAttribute;
            return other?.Visibility == Visibility;
        }

        public override int GetHashCode() => base.GetHashCode();

        public override bool IsDefaultAttribute() => Equals(Default);
    }
}
