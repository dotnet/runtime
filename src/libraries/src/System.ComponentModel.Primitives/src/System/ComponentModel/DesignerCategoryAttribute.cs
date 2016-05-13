// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies that the designer for a class belongs to a certain category.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
    public sealed class DesignerCategoryAttribute : Attribute, IIsDefaultAttribute, ITypeId
    {
        private readonly string _category;
        private string _typeId;

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
            _category = string.Empty;
        }

        /// <devdoc>
        ///    <para>
        ///       Initializes a new instance of the <see cref='System.ComponentModel.DesignerCategoryAttribute'/> class with
        ///       the given category name.
        ///    </para>
        /// </devdoc>
        public DesignerCategoryAttribute(string category)
        {
            _category = category;
        }

        /// <devdoc>
        ///    <para>
        ///       Gets the name of the category.
        ///    </para>
        /// </devdoc>
        public string Category => _category;

        /// <internalonly/>
        /// <devdoc>
        ///    <para>
        ///       This defines a unique ID for this attribute type. It is used
        ///       by filtering algorithms to identify two attributes that are
        ///       the same type. For most attributes, this just returns the
        ///       Type instance for the attribute. DesignerAttribute overrides
        ///       this to include the name of the category
        ///    </para>
        /// </devdoc>
        object ITypeId.TypeId
        {
            get
            {
                if (_typeId == null)
                {
                    _typeId = GetType().FullName + Category;
                }
                return _typeId;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            DesignerCategoryAttribute other = obj as DesignerCategoryAttribute;
            return (other != null) && other._category == _category;
        }

        public override int GetHashCode()
        {
            return _category.GetHashCode();
        }

        bool IIsDefaultAttribute.IsDefaultAttribute()
        {
            return _category.Equals(Default.Category);
        }
    }
}
