// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Description for a particular settings group.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsGroupDescriptionAttribute : Attribute
    {
        private readonly string _description;

        /// <summary>
        /// Constructor takes the description string.
        /// </summary>
        public SettingsGroupDescriptionAttribute(string description)
        {
            _description = description;
        }

        /// <summary>
        /// Description string.
        /// </summary>
        public string Description
        {
            get
            {
                return _description;
            }
        }
    }

}
