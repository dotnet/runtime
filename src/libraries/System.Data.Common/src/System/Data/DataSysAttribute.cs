// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.Data
{
    /// <summary>
    /// DescriptionAttribute marks a property, event, or extender with a
    /// description. Visual designers can display this description when referencing
    /// the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    [Obsolete("DataSysDescriptionAttribute has been deprecated and is not supported.")]
    public class DataSysDescriptionAttribute : DescriptionAttribute
    {
        private bool _replaced;

        /// <summary>
        /// Constructs a new sys description.
        /// </summary>
        [Obsolete("DataSysDescriptionAttribute has been deprecated and is not supported.")]
        public DataSysDescriptionAttribute(string description) : base(description)
        {
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
                    DescriptionValue = base.Description;
                }
                return base.Description;
            }
        }
    }
}
