// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.ComponentModel;

namespace System.Diagnostics
{
    /// <summary>Specifies a description for a property or event.</summary>
    /// <remarks>A visual designer can display the description when referencing the component member, such as in a Properties window. Access the <see cref="System.Diagnostics.MonitoringDescriptionAttribute.Description" /> property to get or set the text associated with this attribute.</remarks>
    /// <altmember cref="System.ComponentModel.DescriptionAttribute"/>
    /// <altmember cref="System.ComponentModel.PropertyDescriptor"/>
    /// <altmember cref="System.ComponentModel.EventDescriptor"/>
    [AttributeUsage(AttributeTargets.All)]
    public class MonitoringDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced;

        /// <summary>Initializes a new instance of the <see cref="System.Diagnostics.MonitoringDescriptionAttribute" /> class, using the specified description.</summary>
        /// <param name="description">The application-defined description text.</param>
        /// <remarks>The description you specify in the <xref:System.Diagnostics.MonitoringDescriptionAttribute.%23ctor%2A> constructor is displayed by a visual designer when you access the property, event, or extender to which the attribute applies</remarks>
        /// <altmember cref="System.Diagnostics.MonitoringDescriptionAttribute.Description"/>
        public MonitoringDescriptionAttribute(string description) : base(description)
        {
        }

        /// <summary>Gets description text associated with the item monitored.</summary>
        /// <value>An application-defined description.</value>
        /// <altmember cref="System.Diagnostics.MonitoringDescriptionAttribute.#ctor(string)"/>
        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;
                    DescriptionValue = base.Description;
                }
                return base.Description;
            }
        }
    }
}
