// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies that the designer for a class belongs to a certain category.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DesignerCategoryAttribute : Attribute
    {
        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a
        ///       component designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Component = new DesignerCategoryAttribute("Component");

        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category cannot use a visual
        ///       designer. This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Default = new DesignerCategoryAttribute();

        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a form designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Form = new DesignerCategoryAttribute("Form");

        /// <devdoc>
        ///    <para>
        ///       Specifies that a component marked with this category uses a generic designer.
        ///       This <see langword='static '/>field is read-only.
        ///    </para>
        /// </devdoc>
        public static readonly DesignerCategoryAttribute Generic = new DesignerCategoryAttribute("Designer");

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with the
        ///       default category.
        ///    </para>
        /// </devdoc>
        public DesignerCategoryAttribute()
        {
            Category = string.Empty;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with
        ///       the given category name.
        ///    </para>
        /// </devdoc>
        public DesignerCategoryAttribute(string category)
        {
            Category = category;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the category.
        ///    </para>
        /// </devdoc>
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

        public override int GetHashCode()
        {
            return Category.GetHashCode();
        }
    }
}
