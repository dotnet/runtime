// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.ComponentModel
{
    /// <summary>
    ///    <para>Specifies the category in which the property or event will be displayed in a
    ///       visual designer.</para>
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class CategoryAttribute : Attribute
    {
        private static volatile CategoryAttribute s_action;
        private static volatile CategoryAttribute s_appearance;
        private static volatile CategoryAttribute s_asynchronous;
        private static volatile CategoryAttribute s_behavior;
        private static volatile CategoryAttribute s_data;
        private static volatile CategoryAttribute s_design;
        private static volatile CategoryAttribute s_dragDrop;
        private static volatile CategoryAttribute s_defAttr;
        private static volatile CategoryAttribute s_focus;
        private static volatile CategoryAttribute s_format;
        private static volatile CategoryAttribute s_key;
        private static volatile CategoryAttribute s_layout;
        private static volatile CategoryAttribute s_mouse;
        private static volatile CategoryAttribute s_windowStyle;

        private bool _localized;

        private object _locker = new Object();

        /// <summary>
        ///    <para>
        ///       Provides the actual category name.
        ///    </para>
        /// </summary>
        private string _categoryValue;

        /// <summary>
        ///    <para>Gets the action category attribute.</para>
        /// </summary>
        public static CategoryAttribute Action
        {
            get
            {
                if (s_action == null)
                {
                    s_action = new CategoryAttribute(nameof(Action));
                }
                return s_action;
            }
        }

        /// <summary>
        ///    <para>Gets the appearance category attribute.</para>
        /// </summary>
        public static CategoryAttribute Appearance
        {
            get
            {
                if (s_appearance == null)
                {
                    s_appearance = new CategoryAttribute(nameof(Appearance));
                }
                return s_appearance;
            }
        }

        /// <summary>
        ///    <para>Gets the asynchronous category attribute.</para>
        /// </summary>
        public static CategoryAttribute Asynchronous
        {
            get
            {
                if (s_asynchronous == null)
                {
                    s_asynchronous = new CategoryAttribute(nameof(Asynchronous));
                }
                return s_asynchronous;
            }
        }

        /// <summary>
        ///    <para>Gets the behavior category attribute.</para>
        /// </summary>
        public static CategoryAttribute Behavior
        {
            get
            {
                if (s_behavior == null)
                {
                    s_behavior = new CategoryAttribute(nameof(Behavior));
                }
                return s_behavior;
            }
        }

        /// <summary>
        ///    <para>Gets the data category attribute.</para>
        /// </summary>
        public static CategoryAttribute Data
        {
            get
            {
                if (s_data == null)
                {
                    s_data = new CategoryAttribute(nameof(Data));
                }
                return s_data;
            }
        }

        /// <summary>
        ///    <para>Gets the default category attribute.</para>
        /// </summary>
        public static CategoryAttribute Default
        {
            get
            {
                if (s_defAttr == null)
                {
                    s_defAttr = new CategoryAttribute();
                }
                return s_defAttr;
            }
        }

        /// <summary>
        ///    <para>Gets the design category attribute.</para>
        /// </summary>
        public static CategoryAttribute Design
        {
            get
            {
                if (s_design == null)
                {
                    s_design = new CategoryAttribute(nameof(Design));
                }
                return s_design;
            }
        }

        /// <summary>
        ///    <para>Gets the drag and drop category attribute.</para>
        /// </summary>
        public static CategoryAttribute DragDrop
        {
            get
            {
                if (s_dragDrop == null)
                {
                    s_dragDrop = new CategoryAttribute(nameof(DragDrop));
                }
                return s_dragDrop;
            }
        }

        /// <summary>
        ///    <para>Gets the focus category attribute.</para>
        /// </summary>
        public static CategoryAttribute Focus
        {
            get
            {
                if (s_focus == null)
                {
                    s_focus = new CategoryAttribute(nameof(Focus));
                }
                return s_focus;
            }
        }

        /// <summary>
        ///    <para>Gets the format category attribute.</para>
        /// </summary>
        public static CategoryAttribute Format
        {
            get
            {
                if (s_format == null)
                {
                    s_format = new CategoryAttribute(nameof(Format));
                }
                return s_format;
            }
        }

        /// <summary>
        ///    <para>Gets the keyboard category attribute.</para>
        /// </summary>
        public static CategoryAttribute Key
        {
            get
            {
                if (s_key == null)
                {
                    s_key = new CategoryAttribute(nameof(Key));
                }
                return s_key;
            }
        }

        /// <summary>
        ///    <para>Gets the layout category attribute.</para>
        /// </summary>
        public static CategoryAttribute Layout
        {
            get
            {
                if (s_layout == null)
                {
                    s_layout = new CategoryAttribute(nameof(Layout));
                }
                return s_layout;
            }
        }

        /// <summary>
        ///    <para>Gets the mouse category attribute.</para>
        /// </summary>
        public static CategoryAttribute Mouse
        {
            get
            {
                if (s_mouse == null)
                {
                    s_mouse = new CategoryAttribute(nameof(Mouse));
                }
                return s_mouse;
            }
        }

        /// <summary>
        ///    <para> Gets the window style category 
        ///       attribute.</para>
        /// </summary>
        public static CategoryAttribute WindowStyle
        {
            get
            {
                if (s_windowStyle == null)
                {
                    s_windowStyle = new CategoryAttribute(nameof(WindowStyle));
                }
                return s_windowStyle;
            }
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/> 
        /// class with the default category.</para>
        /// </summary>
        public CategoryAttribute() : this(nameof(Default))
        {
        }

        /// <summary>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/> class with
        ///    the specified category name.</para>
        /// </summary>
        public CategoryAttribute(string category)
        {
            _categoryValue = category;
        }

        /// <summary>
        ///    <para>Gets the name of the category for the property or event 
        ///       that this attribute is bound to.</para>
        /// </summary>
        public string Category
        {
            get
            {
                if (!_localized)
                {
                    lock (_locker)
                    {
                        string localizedValue = GetLocalizedString(_categoryValue);
                        if (localizedValue != null)
                        {
                            _categoryValue = localizedValue;
                        }

                        _localized = true;
                    }
                }

                return _categoryValue;
            }
        }

        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            CategoryAttribute other = obj as CategoryAttribute;
            return other != null && Category.Equals(other.Category);
        }

        public override int GetHashCode() => Category.GetHashCode();

        /// <summary>
        ///    <para>Looks up the localized name of a given category.</para>
        /// </summary>
        protected virtual string GetLocalizedString(string value) => SR.GetResourceString("PropertyCategory" + value, null);

        public override bool IsDefaultAttribute() => Category.Equals(Default.Category);
    }
}
