// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Indicates the SettingsManageability for a group of/individual setting.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Property)]
    public sealed class SettingsManageabilityAttribute : Attribute
    {
        private readonly SettingsManageability _manageability;

        /// <summary>
        /// Constructor takes a SettingsManageability enum value.
        /// </summary>
        public SettingsManageabilityAttribute(SettingsManageability manageability)
        {
            _manageability = manageability;
        }

        /// <summary>
        /// SettingsManageability value to use
        /// </summary>
        public SettingsManageability Manageability
        {
            get
            {
                return _manageability;
            }
        }
    }
}
