// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.ComponentModel;

namespace System.ServiceProcess
{
    /// <summary>
    /// DescriptionAttribute marks a property, event, or extender with a
    /// description. Visual designers can display this description when referencing
    /// the member.
    /// </summary>
    [AttributeUsage(AttributeTargets.All)]
    public class ServiceProcessDescriptionAttribute : DescriptionAttribute
    {
        private bool replaced;

        /// <summary>
        /// Constructs a new sys description
        /// </summary>
        public ServiceProcessDescriptionAttribute(string description) : base(description)
        {
        }

        /// <summary>
        /// Retrieves the description text.
        /// </summary>
        public override string Description
        {
            get
            {
                if (!replaced)
                {
                    replaced = true;
                    DescriptionValue = base.Description;
                }
                return base.Description;
            }
        }
    }
}
