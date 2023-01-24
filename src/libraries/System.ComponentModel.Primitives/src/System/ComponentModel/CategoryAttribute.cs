// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;

namespace System.ComponentModel
{
    /// <summary>
    /// Specifies the category in which the property or event will be displayed in a
    /// visual designer.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class CategoryAttribute : Attribute
    {
        private static volatile CategoryAttribute? s_action;
        private static volatile CategoryAttribute? s_appearance;
        private static volatile CategoryAttribute? s_asynchronous;
        private static volatile CategoryAttribute? s_behavior;
        private static volatile CategoryAttribute? s_data;
        private static volatile CategoryAttribute? s_design;
        private static volatile CategoryAttribute? s_dragDrop;
        private static volatile CategoryAttribute? s_defAttr;
        private static volatile CategoryAttribute? s_focus;
        private static volatile CategoryAttribute? s_format;
        private static volatile CategoryAttribute? s_key;
        private static volatile CategoryAttribute? s_layout;
        private static volatile CategoryAttribute? s_mouse;
        private static volatile CategoryAttribute? s_windowStyle;

        private bool _localized;

        private readonly object _locker = new object();

        /// <summary>
        /// Provides the actual category name.
        /// </summary>
        private string _categoryValue;

        /// <summary>
        /// Gets the action category attribute.
        /// </summary>
        public static CategoryAttribute Action
        {
            get => s_action ??= new CategoryAttribute(nameof(Action));
        }

        /// <summary>
        /// Gets the appearance category attribute.
        /// </summary>
        public static CategoryAttribute Appearance
        {
            get => s_appearance ??= new CategoryAttribute(nameof(Appearance));
        }

        /// <summary>
        /// Gets the asynchronous category attribute.
        /// </summary>
        public static CategoryAttribute Asynchronous
        {
            get => s_asynchronous ??= new CategoryAttribute(nameof(Asynchronous));
        }

        /// <summary>
        /// Gets the behavior category attribute.
        /// </summary>
        public static CategoryAttribute Behavior
        {
            get => s_behavior ??= new CategoryAttribute(nameof(Behavior));
        }

        /// <summary>
        /// Gets the data category attribute.
        /// </summary>
        public static CategoryAttribute Data
        {
            get => s_data ??= new CategoryAttribute(nameof(Data));
        }

        /// <summary>
        /// Gets the default category attribute.
        /// </summary>
        public static CategoryAttribute Default
        {
            get => s_defAttr ??= new CategoryAttribute();
        }

        /// <summary>
        /// Gets the design category attribute.
        /// </summary>
        public static CategoryAttribute Design
        {
            get => s_design ??= new CategoryAttribute(nameof(Design));
        }

        /// <summary>
        /// Gets the drag and drop category attribute.
        /// </summary>
        public static CategoryAttribute DragDrop
        {
            get => s_dragDrop ??= new CategoryAttribute(nameof(DragDrop));
        }

        /// <summary>
        /// Gets the focus category attribute.
        /// </summary>
        public static CategoryAttribute Focus
        {
            get => s_focus ??= new CategoryAttribute(nameof(Focus));
        }

        /// <summary>
        /// Gets the format category attribute.
        /// </summary>
        public static CategoryAttribute Format
        {
            get => s_format ??= new CategoryAttribute(nameof(Format));
        }

        /// <summary>
        /// Gets the keyboard category attribute.
        /// </summary>
        public static CategoryAttribute Key
        {
            get => s_key ??= new CategoryAttribute(nameof(Key));
        }

        /// <summary>
        /// Gets the layout category attribute.
        /// </summary>
        public static CategoryAttribute Layout
        {
            get => s_layout ??= new CategoryAttribute(nameof(Layout));
        }

        /// <summary>
        /// Gets the mouse category attribute.
        /// </summary>
        public static CategoryAttribute Mouse
        {
            get => s_mouse ??= new CategoryAttribute(nameof(Mouse));
        }

        /// <summary>
        /// Gets the window style category attribute.
        /// </summary>
        public static CategoryAttribute WindowStyle
        {
            get => s_windowStyle ??= new CategoryAttribute(nameof(WindowStyle));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/>
        /// class with the default category.
        /// </summary>
        public CategoryAttribute() : this(nameof(Default))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref='System.ComponentModel.CategoryAttribute'/>
        /// class with the specified category name.
        /// </summary>
        public CategoryAttribute(string category)
        {
            _categoryValue = category;
        }

        /// <summary>
        /// Gets the name of the category for the property or event that this attribute is
        /// bound to.
        /// </summary>
        public string Category
        {
            get
            {
                if (!_localized)
                {
                    lock (_locker)
                    {
                        string? localizedValue = GetLocalizedString(_categoryValue);
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

        public override bool Equals([NotNullWhen(true)] object? obj) =>
            obj is CategoryAttribute other && other.Category == Category;

        public override int GetHashCode() => Category?.GetHashCode() ?? 0;

        /// <summary>
        /// Looks up the localized name of a given category.
        /// </summary>
        protected virtual string? GetLocalizedString(string value) => value switch
        {
            "Action" => SR.PropertyCategoryAction,
            "Appearance" => SR.PropertyCategoryAppearance,
            "Asynchronous" => SR.PropertyCategoryAsynchronous,
            "Behavior" => SR.PropertyCategoryBehavior,
            "Config" => SR.PropertyCategoryConfig,
            "Data" => SR.PropertyCategoryData,
            "DDE" => SR.PropertyCategoryDDE,
            "Default" => SR.PropertyCategoryDefault,
            "Design" => SR.PropertyCategoryDesign,
            "DragDrop" => SR.PropertyCategoryDragDrop,
            "Focus" => SR.PropertyCategoryFocus,
            "Font" => SR.PropertyCategoryFont,
            "Format" => SR.PropertyCategoryFormat,
            "Key" => SR.PropertyCategoryKey,
            "Layout" => SR.PropertyCategoryLayout,
            "List" => SR.PropertyCategoryList,
            "Mouse" => SR.PropertyCategoryMouse,
            "Position" => SR.PropertyCategoryPosition,
            "Scale" => SR.PropertyCategoryScale,
            "Text" => SR.PropertyCategoryText,
            "WindowStyle" => SR.PropertyCategoryWindowStyle,
            _ => null
        };

        public override bool IsDefaultAttribute() => Category == Default.Category;
    }
}
