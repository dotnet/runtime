// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;
using System.Diagnostics;

namespace System.Timers
{
    /// <summary>
    /// DescriptionAttribute marks a property, event, or extender with a
    /// description. Visual designers can display this description when referencing
    /// the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class TimersDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced;

        /// <summary>
        /// Constructs a new sys description.
        /// </summary>
        public TimersDescriptionAttribute(string description) : base(description) { }

        /// <summary>
        /// Constructs a new localized sys description.
        /// </summary>
        internal TimersDescriptionAttribute(string description, string? unused) : base(SR.GetResourceString(description))
        {
            // Needed for overload resolution
            Debug.Assert(unused == null);
        }

        /// <summary>
        /// Retrieves the description text.
        /// </summary>
        public override string Description
        {
            get
            {
                if (!_replaced)
                {
                    _replaced = true;
                    DescriptionValue = SR.Format(base.Description);
                }
                return base.Description;
            }
        }
    }
}
