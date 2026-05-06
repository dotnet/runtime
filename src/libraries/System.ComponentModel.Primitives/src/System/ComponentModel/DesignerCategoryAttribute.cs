// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies that the designer for a class belongs to a certain category.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DesignerCategoryAttribute : Attribute
    {
        /// <summary>
        /// Specifies that a component marked with this category uses a component
        /// designer. This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerCategoryAttribute Component = new DesignerCategoryAttribute("Component");

        /// <summary>
        /// Specifies that a component marked with this category cannot use a visual
        /// designer. This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerCategoryAttribute Default = new DesignerCategoryAttribute();

        /// <summary>
        /// Specifies that a component marked with this category uses a form designer.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerCategoryAttribute Form = new DesignerCategoryAttribute("Form");

        /// <summary>
        /// Specifies that a component marked with this category uses a generic designer.
        /// This <see langword='static'/> field is read-only.
        /// </summary>
        public static readonly DesignerCategoryAttribute Generic = new DesignerCategoryAttribute("Designer");

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with the
        /// default category.
        /// </summary>
        public DesignerCategoryAttribute() : this(string.Empty)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with
        /// the given category name.
        /// </summary>
        public DesignerCategoryAttribute(string category)
        {
            Category = category;
        }

        /// <summary>
        /// Gets the name of the category.
        /// </summary>
        public string Category { get; }

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is DesignerCategoryAttribute other && other.Category == Category;

        public override int GetHashCode() => Category?.GetHashCode() ?? 0;

        public override bool IsDefaultAttribute() => Category == Default.Category;

        public override object TypeId => GetType().FullName + Category;
    }
}
