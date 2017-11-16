// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Globalization;

namespace System.ComponentModel.DataAnnotations
{
    /// <summary>
    ///     DisplayAttribute is a general-purpose attribute to specify user-visible globalizable strings for types and members.
    ///     The string properties of this class can be used either as literals or as resource identifiers into a specified
    ///     <see cref="ResourceType" />
    /// </summary>
    [AttributeUsage(
        AttributeTargets.Property | AttributeTargets.Field | AttributeTargets.Parameter | AttributeTargets.Method | AttributeTargets.Class,
        AllowMultiple = false)]
    public sealed class DisplayAttribute : Attribute
    {
        #region Member Fields

        private readonly LocalizableString _description = new LocalizableString("Description");
        private readonly LocalizableString _groupName = new LocalizableString("GroupName");
        private readonly LocalizableString _name = new LocalizableString("Name");
        private readonly LocalizableString _prompt = new LocalizableString("Prompt");
        private readonly LocalizableString _shortName = new LocalizableString("ShortName");
        private bool? _autoGenerateField;
        private bool? _autoGenerateFilter;
        private int? _order;
        private Type _resourceType;

        #endregion

        #region All Constructors

        #endregion

        #region Properties

        /// <summary>
        ///     Gets or sets the ShortName attribute property, which may be a resource key string.
        ///     <para>
        ///         Consumers must use the <see cref="GetShortName" /> method to retrieve the UI display string.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The property contains either the literal, non-localized string or the resource key
        ///     to be used in conjunction with <see cref="ResourceType" /> to configure a localized
        ///     short name for display.
        ///     <para>
        ///         The <see cref="GetShortName" /> method will return either the literal, non-localized
        ///         string or the localized string when <see cref="ResourceType" /> has been specified.
        ///     </para>
        /// </remarks>
        /// <value>
        ///     The short name is generally used as the grid column label for a UI element bound to the member
        ///     bearing this attribute.  A <c>null</c> or empty string is legal, and consumers must allow for that.
        /// </value>
        public string ShortName
        {
            get => _shortName.Value;
            set => _shortName.Value = value;
        }

        /// <summary>
        ///     Gets or sets the Name attribute property, which may be a resource key string.
        ///     <para>
        ///         Consumers must use the <see cref="GetName" /> method to retrieve the UI display string.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The property contains either the literal, non-localized string or the resource key
        ///     to be used in conjunction with <see cref="ResourceType" /> to configure a localized
        ///     name for display.
        ///     <para>
        ///         The <see cref="GetName" /> method will return either the literal, non-localized
        ///         string or the localized string when <see cref="ResourceType" /> has been specified.
        ///     </para>
        /// </remarks>
        /// <value>
        ///     The name is generally used as the field label for a UI element bound to the member
        ///     bearing this attribute.  A <c>null</c> or empty string is legal, and consumers must allow for that.
        /// </value>
        public string Name
        {
            get => _name.Value;
            set => _name.Value = value;
        }

        /// <summary>
        ///     Gets or sets the Description attribute property, which may be a resource key string.
        ///     <para>
        ///         Consumers must use the <see cref="GetDescription" /> method to retrieve the UI display string.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The property contains either the literal, non-localized string or the resource key
        ///     to be used in conjunction with <see cref="ResourceType" /> to configure a localized
        ///     description for display.
        ///     <para>
        ///         The <see cref="GetDescription" /> method will return either the literal, non-localized
        ///         string or the localized string when <see cref="ResourceType" /> has been specified.
        ///     </para>
        /// </remarks>
        /// <value>
        ///     Description is generally used as a tool tip or description a UI element bound to the member
        ///     bearing this attribute.  A <c>null</c> or empty string is legal, and consumers must allow for that.
        /// </value>
        public string Description
        {
            get => _description.Value;
            set => _description.Value = value;
        }

        /// <summary>
        ///     Gets or sets the Prompt attribute property, which may be a resource key string.
        ///     <para>
        ///         Consumers must use the <see cref="GetPrompt" /> method to retrieve the UI display string.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The property contains either the literal, non-localized string or the resource key
        ///     to be used in conjunction with <see cref="ResourceType" /> to configure a localized
        ///     prompt for display.
        ///     <para>
        ///         The <see cref="GetPrompt" /> method will return either the literal, non-localized
        ///         string or the localized string when <see cref="ResourceType" /> has been specified.
        ///     </para>
        /// </remarks>
        /// <value>
        ///     A prompt is generally used as a prompt or watermark for a UI element bound to the member
        ///     bearing this attribute.  A <c>null</c> or empty string is legal, and consumers must allow for that.
        /// </value>
        public string Prompt
        {
            get => _prompt.Value;
            set => _prompt.Value = value;
        }

        /// <summary>
        ///     Gets or sets the GroupName attribute property, which may be a resource key string.
        ///     <para>
        ///         Consumers must use the <see cref="GetGroupName" /> method to retrieve the UI display string.
        ///     </para>
        /// </summary>
        /// <remarks>
        ///     The property contains either the literal, non-localized string or the resource key
        ///     to be used in conjunction with <see cref="ResourceType" /> to configure a localized
        ///     group name for display.
        ///     <para>
        ///         The <see cref="GetGroupName" /> method will return either the literal, non-localized
        ///         string or the localized string when <see cref="ResourceType" /> has been specified.
        ///     </para>
        /// </remarks>
        /// <value>
        ///     A group name is used for grouping fields into the UI.  A <c>null</c> or empty string is legal,
        ///     and consumers must allow for that.
        /// </value>
        public string GroupName
        {
            get => _groupName.Value;
            set => _groupName.Value = value;
        }

        /// <summary>
        ///     Gets or sets the <see cref="System.Type" /> that contains the resources for <see cref="ShortName" />,
        ///     <see cref="Name" />, <see cref="Description" />, <see cref="Prompt" />, and <see cref="GroupName" />.
        ///     Using <see cref="ResourceType" /> along with these Key properties, allows the <see cref="GetShortName" />,
        ///     <see cref="GetName" />, <see cref="GetDescription" />, <see cref="GetPrompt" />, and <see cref="GetGroupName" />
        ///     methods to return localized values.
        /// </summary>
        public Type ResourceType
        {
            get => _resourceType;
            set
            {
                if (_resourceType != value)
                {
                    _resourceType = value;

                    _shortName.ResourceType = value;
                    _name.ResourceType = value;
                    _description.ResourceType = value;
                    _prompt.ResourceType = value;
                    _groupName.ResourceType = value;
                }
            }
        }

        /// <summary>
        ///     Gets or sets whether UI should be generated automatically to display this field. If this property is not
        ///     set then the presentation layer will automatically determine whether UI should be generated. Setting this
        ///     property allows an override of the default behavior of the presentation layer.
        ///     <para>
        ///         Consumers must use the <see cref="GetAutoGenerateField" /> method to retrieve the value, as this property
        ///         getter will throw
        ///         an exception if the value has not been set.
        ///     </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     If the getter of this property is invoked when the value has not been explicitly set using the setter.
        /// </exception>
        public bool AutoGenerateField
        {
            get
            {
                if (!_autoGenerateField.HasValue)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        SR.DisplayAttribute_PropertyNotSet, "AutoGenerateField",
                        "GetAutoGenerateField"));
                }

                return _autoGenerateField.GetValueOrDefault();
            }
            set => _autoGenerateField = value;
        }

        /// <summary>
        ///     Gets or sets whether UI should be generated automatically to display filtering for this field. If this property is
        ///     not
        ///     set then the presentation layer will automatically determine whether filtering UI should be generated. Setting this
        ///     property allows an override of the default behavior of the presentation layer.
        ///     <para>
        ///         Consumers must use the <see cref="GetAutoGenerateFilter" /> method to retrieve the value, as this property
        ///         getter will throw
        ///         an exception if the value has not been set.
        ///     </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     If the getter of this property is invoked when the value has not been explicitly set using the setter.
        /// </exception>
        public bool AutoGenerateFilter
        {
            get
            {
                if (!_autoGenerateFilter.HasValue)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        SR.DisplayAttribute_PropertyNotSet, "AutoGenerateFilter",
                        "GetAutoGenerateFilter"));
                }

                return _autoGenerateFilter.GetValueOrDefault();
            }
            set => _autoGenerateFilter = value;
        }

        /// <summary>
        ///     Gets or sets the order in which this field should be displayed.  If this property is not set then
        ///     the presentation layer will automatically determine the order.  Setting this property explicitly
        ///     allows an override of the default behavior of the presentation layer.
        ///     <para>
        ///         Consumers must use the <see cref="GetOrder" /> method to retrieve the value, as this property getter will throw
        ///         an exception if the value has not been set.
        ///     </para>
        /// </summary>
        /// <exception cref="System.InvalidOperationException">
        ///     If the getter of this property is invoked when the value has not been explicitly set using the setter.
        /// </exception>
        public int Order
        {
            get
            {
                if (!_order.HasValue)
                {
                    throw new InvalidOperationException(string.Format(CultureInfo.CurrentCulture,
                        SR.DisplayAttribute_PropertyNotSet, "Order", "GetOrder"));
                }

                return _order.GetValueOrDefault();
            }
            set => _order = value;
        }

        #endregion

        #region Methods

        /// <summary>
        ///     Gets the UI display string for ShortName.
        ///     <para>
        ///         This can be either a literal, non-localized string provided to <see cref="ShortName" /> or the
        ///         localized string found when <see cref="ResourceType" /> has been specified and <see cref="ShortName" />
        ///         represents a resource key within that resource type.
        ///     </para>
        /// </summary>
        /// <returns>
        ///     When <see cref="ResourceType" /> has not been specified, the value of
        ///     <see cref="ShortName" /> will be returned.
        ///     <para>
        ///         When <see cref="ResourceType" /> has been specified and <see cref="ShortName" />
        ///         represents a resource key within that resource type, then the localized value will be returned.
        ///     </para>
        ///     <para>
        ///         If <see cref="ShortName" /> is <c>null</c>, the value from <see cref="GetName" /> will be returned.
        ///     </para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     After setting both the <see cref="ResourceType" /> property and the <see cref="ShortName" /> property,
        ///     but a public static property with a name matching the <see cref="ShortName" /> value couldn't be found
        ///     on the <see cref="ResourceType" />.
        /// </exception>
        public string GetShortName() => _shortName.GetLocalizableValue() ?? GetName();

        /// <summary>
        ///     Gets the UI display string for Name.
        ///     <para>
        ///         This can be either a literal, non-localized string provided to <see cref="Name" /> or the
        ///         localized string found when <see cref="ResourceType" /> has been specified and <see cref="Name" />
        ///         represents a resource key within that resource type.
        ///     </para>
        /// </summary>
        /// <returns>
        ///     When <see cref="ResourceType" /> has not been specified, the value of
        ///     <see cref="Name" /> will be returned.
        ///     <para>
        ///         When <see cref="ResourceType" /> has been specified and <see cref="Name" />
        ///         represents a resource key within that resource type, then the localized value will be returned.
        ///     </para>
        ///     <para>
        ///         Can return <c>null</c> and will not fall back onto other values, as it's more likely for the
        ///         consumer to want to fall back onto the property name.
        ///     </para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     After setting both the <see cref="ResourceType" /> property and the <see cref="Name" /> property,
        ///     but a public static property with a name matching the <see cref="Name" /> value couldn't be found
        ///     on the <see cref="ResourceType" />.
        /// </exception>
        public string GetName() => _name.GetLocalizableValue();

        /// <summary>
        ///     Gets the UI display string for Description.
        ///     <para>
        ///         This can be either a literal, non-localized string provided to <see cref="Description" /> or the
        ///         localized string found when <see cref="ResourceType" /> has been specified and <see cref="Description" />
        ///         represents a resource key within that resource type.
        ///     </para>
        /// </summary>
        /// <returns>
        ///     When <see cref="ResourceType" /> has not been specified, the value of
        ///     <see cref="Description" /> will be returned.
        ///     <para>
        ///         When <see cref="ResourceType" /> has been specified and <see cref="Description" />
        ///         represents a resource key within that resource type, then the localized value will be returned.
        ///     </para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     After setting both the <see cref="ResourceType" /> property and the <see cref="Description" /> property,
        ///     but a public static property with a name matching the <see cref="Description" /> value couldn't be found
        ///     on the <see cref="ResourceType" />.
        /// </exception>
        public string GetDescription() => _description.GetLocalizableValue();

        /// <summary>
        ///     Gets the UI display string for Prompt.
        ///     <para>
        ///         This can be either a literal, non-localized string provided to <see cref="Prompt" /> or the
        ///         localized string found when <see cref="ResourceType" /> has been specified and <see cref="Prompt" />
        ///         represents a resource key within that resource type.
        ///     </para>
        /// </summary>
        /// <returns>
        ///     When <see cref="ResourceType" /> has not been specified, the value of
        ///     <see cref="Prompt" /> will be returned.
        ///     <para>
        ///         When <see cref="ResourceType" /> has been specified and <see cref="Prompt" />
        ///         represents a resource key within that resource type, then the localized value will be returned.
        ///     </para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     After setting both the <see cref="ResourceType" /> property and the <see cref="Prompt" /> property,
        ///     but a public static property with a name matching the <see cref="Prompt" /> value couldn't be found
        ///     on the <see cref="ResourceType" />.
        /// </exception>
        public string GetPrompt() => _prompt.GetLocalizableValue();

        /// <summary>
        ///     Gets the UI display string for GroupName.
        ///     <para>
        ///         This can be either a literal, non-localized string provided to <see cref="GroupName" /> or the
        ///         localized string found when <see cref="ResourceType" /> has been specified and <see cref="GroupName" />
        ///         represents a resource key within that resource type.
        ///     </para>
        /// </summary>
        /// <returns>
        ///     When <see cref="ResourceType" /> has not been specified, the value of
        ///     <see cref="GroupName" /> will be returned.
        ///     <para>
        ///         When <see cref="ResourceType" /> has been specified and <see cref="GroupName" />
        ///         represents a resource key within that resource type, then the localized value will be returned.
        ///     </para>
        /// </returns>
        /// <exception cref="System.InvalidOperationException">
        ///     After setting both the <see cref="ResourceType" /> property and the <see cref="GroupName" /> property,
        ///     but a public static property with a name matching the <see cref="GroupName" /> value couldn't be found
        ///     on the <see cref="ResourceType" />.
        /// </exception>
        public string GetGroupName() => _groupName.GetLocalizableValue();

        /// <summary>
        ///     Gets the value of <see cref="AutoGenerateField" /> if it has been set, or <c>null</c>.
        /// </summary>
        /// <returns>
        ///     When <see cref="AutoGenerateField" /> has been set returns the value of that property.
        ///     <para>
        ///         When <see cref="AutoGenerateField" /> has not been set returns <c>null</c>.
        ///     </para>
        /// </returns>
        public bool? GetAutoGenerateField() => _autoGenerateField;

        /// <summary>
        ///     Gets the value of <see cref="AutoGenerateFilter" /> if it has been set, or <c>null</c>.
        /// </summary>
        /// <returns>
        ///     When <see cref="AutoGenerateFilter" /> has been set returns the value of that property.
        ///     <para>
        ///         When <see cref="AutoGenerateFilter" /> has not been set returns <c>null</c>.
        ///     </para>
        /// </returns>
        public bool? GetAutoGenerateFilter() => _autoGenerateFilter;

        /// <summary>
        ///     Gets the value of <see cref="Order" /> if it has been set, or <c>null</c>.
        /// </summary>
        /// <returns>
        ///     When <see cref="Order" /> has been set returns the value of that property.
        ///     <para>
        ///         When <see cref="Order" /> has not been set returns <c>null</c>.
        ///     </para>
        /// </returns>
        /// <remarks>
        ///     When an order is not specified, presentation layers should consider using the value
        ///     of 10000.  This value allows for explicitly-ordered fields to be displayed before
        ///     and after the fields that don't specify an order.
        /// </remarks>
        public int? GetOrder() => _order;

        #endregion
    }
}
