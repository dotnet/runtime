// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Security.Permissions;

namespace System.ComponentModel
{
    /// <devdoc>
    ///    <para>Specifies the category in which the property or event will be displayed in a
    ///       visual designer.</para>
    /// </devdoc>
    [AttributeUsage(AttributeTargets.All)]
    public class CategoryAttribute : Attribute
    {
        private static volatile CategoryAttribute s_appearance;
        private static volatile CategoryAttribute s_asynchronous;
        private static volatile CategoryAttribute s_behavior;
        private static volatile CategoryAttribute s_data;
        private static volatile CategoryAttribute s_design;
        private static volatile CategoryAttribute s_action;
        private static volatile CategoryAttribute s_format;
        private static volatile CategoryAttribute s_layout;
        private static volatile CategoryAttribute s_mouse;
        private static volatile CategoryAttribute s_key;
        private static volatile CategoryAttribute s_focus;
        private static volatile CategoryAttribute s_windowStyle;
        private static volatile CategoryAttribute s_dragDrop;
        private static volatile CategoryAttribute s_defAttr;

        private bool _localized;

        /// <devdoc>
        ///    <para>
        ///       Provides the actual category name.
        ///    </para>
        /// </devdoc>
        private string _categoryValue;

        /// <devdoc>
        ///    <para>Gets the action category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Action
        {
            get
            {
                if (s_action == null)
                {
                    s_action = new CategoryAttribute("Action");
                }
                return s_action;
            }
        }

        /// <devdoc>
        ///    <para>Gets the appearance category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Appearance
        {
            get
            {
                if (s_appearance == null)
                {
                    s_appearance = new CategoryAttribute("Appearance");
                }
                return s_appearance;
            }
        }

        /// <devdoc>
        ///    <para>Gets the asynchronous category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Asynchronous
        {
            get
            {
                if (s_asynchronous == null)
                {
                    s_asynchronous = new CategoryAttribute("Asynchronous");
                }
                return s_asynchronous;
            }
        }

        /// <devdoc>
        ///    <para>Gets the behavior category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Behavior
        {
            get
            {
                if (s_behavior == null)
                {
                    s_behavior = new CategoryAttribute("Behavior");
                }
                return s_behavior;
            }
        }

        /// <devdoc>
        ///    <para>Gets the data category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Data
        {
            get
            {
                if (s_data == null)
                {
                    s_data = new CategoryAttribute("Data");
                }
                return s_data;
            }
        }

        /// <devdoc>
        ///    <para>Gets the default category attribute.</para>
        /// </devdoc>
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

        /// <devdoc>
        ///    <para>Gets the design category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Design
        {
            get
            {
                if (s_design == null)
                {
                    s_design = new CategoryAttribute("Design");
                }
                return s_design;
            }
        }

        /// <devdoc>
        ///    <para>Gets the drag and drop category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute DragDrop
        {
            get
            {
                if (s_dragDrop == null)
                {
                    s_dragDrop = new CategoryAttribute("DragDrop");
                }
                return s_dragDrop;
            }
        }

        /// <devdoc>
        ///    <para>Gets the focus category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Focus
        {
            get
            {
                if (s_focus == null)
                {
                    s_focus = new CategoryAttribute("Focus");
                }
                return s_focus;
            }
        }

        /// <devdoc>
        ///    <para>Gets the format category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Format
        {
            get
            {
                if (s_format == null)
                {
                    s_format = new CategoryAttribute("Format");
                }
                return s_format;
            }
        }

        /// <devdoc>
        ///    <para>Gets the keyboard category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Key
        {
            get
            {
                if (s_key == null)
                {
                    s_key = new CategoryAttribute("Key");
                }
                return s_key;
            }
        }

        /// <devdoc>
        ///    <para>Gets the layout category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Layout
        {
            get
            {
                if (s_layout == null)
                {
                    s_layout = new CategoryAttribute("Layout");
                }
                return s_layout;
            }
        }

        /// <devdoc>
        ///    <para>Gets the mouse category attribute.</para>
        /// </devdoc>
        public static CategoryAttribute Mouse
        {
            get
            {
                if (s_mouse == null)
                {
                    s_mouse = new CategoryAttribute("Mouse");
                }
                return s_mouse;
            }
        }

        /// <devdoc>
        ///    <para> Gets the window style category 
        ///       attribute.</para>
        /// </devdoc>
        public static CategoryAttribute WindowStyle
        {
            get
            {
                if (s_windowStyle == null)
                {
                    s_windowStyle = new CategoryAttribute("WindowStyle");
                }
                return s_windowStyle;
            }
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/> 
        /// class with the default category.</para>
        /// </devdoc>
        public CategoryAttribute() : this("Default")
        {
        }

        /// <devdoc>
        /// <para>Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/> class with
        ///    the specified category name.</para>
        /// </devdoc>
        public CategoryAttribute(string category)
        {
            _categoryValue = category;
            _localized = false;
        }

        /// <devdoc>
        ///    <para>Gets the name of the category for the property or event 
        ///       that this attribute is bound to.</para>
        /// </devdoc>
        public string Category
        {
            get
            {
                if (!_localized)
                {
                    _localized = true;
                    string localizedValue = GetLocalizedString(_categoryValue);
                    if (localizedValue != null)
                    {
                        _categoryValue = localizedValue;
                    }
                }
                return _categoryValue;
            }
        }

        /// <devdoc>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        /// <internalonly/>
        public override bool Equals(object obj)
        {
            if (obj == this)
            {
                return true;
            }

            if (obj is CategoryAttribute)
            {
                return Category.Equals(((CategoryAttribute)obj).Category);
            }
            return false;
        }

        /// <devdoc>
        ///    <para>[To be supplied.]</para>
        /// </devdoc>
        public override int GetHashCode()
        {
            return Category.GetHashCode();
        }

        /// <devdoc>
        ///    <para>Looks up the localized name of a given category.</para>
        /// </devdoc>
        protected virtual string GetLocalizedString(string value)
        {
#if !SILVERLIGHT
            return (string)SR.GetObject("PropertyCategory" + value);
#else
            bool usedFallback;
            string localizedString = SR.GetString("PropertyCategory" + value, out usedFallback);
            if (usedFallback) {
                return null;
            }
            return localizedString;
#endif
        }

#if !SILVERLIGHT
        /// <devdoc>
        /// </devdoc>
        /// <devdoc>
        /// </devdoc>
        /// <internalonly/>
        /// <internalonly/>
        public override bool IsDefaultAttribute()
        {
            return Category.Equals(Default.Category);
        }
#endif
    }
}

