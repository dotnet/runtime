// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies that the designer for a class belongs to a certain category.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DesignerCategoryAttribute : Attribute
    {
        /// <summary>
        ///    <para>
        ///       Specifies that a component marked with this category uses a
        ///       component designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignerCategoryAttribute Component = new DesignerCategoryAttribute("Component");

        /// <summary>
        ///    <para>
        ///       Specifies that a component marked with this category cannot use a visual
        ///       designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignerCategoryAttribute Default = new DesignerCategoryAttribute();

        /// <summary>
        ///    <para>
        ///       Specifies that a component marked with this category uses a form designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignerCategoryAttribute Form = new DesignerCategoryAttribute("Form");

        /// <summary>
        ///    <para>
        ///       Specifies that a component marked with this category uses a generic designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </summary>
        public static readonly DesignerCategoryAttribute Generic = new DesignerCategoryAttribute("Designer");

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with the
        ///       default category.
        ///    </para>
        /// </summary>
        public DesignerCategoryAttribute() => Category = string.Empty;

        /// <summary>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with
        ///       the given category name.
        ///    </para>
        /// </summary>
        public DesignerCategoryAttribute(string category) => Category = category;

        /// <summary>
        ///    <para>
        ///       Gets the name of the category.
        ///    </para>
        /// </summary>
        public string Category { get; }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignerCategoryAttribute other = obj as DesignerCategoryAttribute;
            return other != null && other.Category == Category;
        }

        public override int GetHashCode() => Category.GetHashCode();

        public override bool IsDefaultAttribute() => Category.Equals(Default.Category);

        public override object TypeId => GetType().FullName + Category;
    }
}
