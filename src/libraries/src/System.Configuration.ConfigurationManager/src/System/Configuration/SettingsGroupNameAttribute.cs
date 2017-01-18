// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Name of a particular settings group.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class)]
    public sealed class SettingsGroupNameAttribute : Attribute
    {
        private readonly string _groupName;

        /// <summary>
        /// Constructor takes the group name.
        /// </summary>
        public SettingsGroupNameAttribute(string groupName)
        {
            _groupName = groupName;
        }

        /// <summary>
        /// Name of the settings group.
        /// </summary>
        public string GroupName
        {
            get
            {
                return _groupName;
            }
        }
    }
}
