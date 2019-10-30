// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// This interface is an extension to SettingsProvider that a provider can implement
    /// to support additional functionality for settings classes that derive from ApplicationSettingsBase.
    /// </summary>
    public interface IApplicationSettingsProvider
    {
        /// <summary>
        /// Retrieves the previous value of a given SettingsProperty. This is used in conjunction with Upgrade.
        /// </summary>
        SettingsPropertyValue GetPreviousVersion(SettingsContext context, SettingsProperty property);

        /// <summary>
        /// Resets all settings to their "default" values.
        /// </summary>
        void Reset(SettingsContext context);

        /// <summary>
        /// Indicates to the provider that the app has been upgraded. This is a chance for the provider to upgrade
        /// its stored settings as appropriate.
        /// </summary>
        void Upgrade(SettingsContext context, SettingsPropertyCollection properties);
    }
}
