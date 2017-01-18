// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Configuration
{
    /// <summary>
    /// Event args for the SettingLoaded event.
    /// </summary>
    public class SettingsLoadedEventArgs : EventArgs
    {
        private SettingsProvider _provider;

        public SettingsLoadedEventArgs(SettingsProvider provider)
        {
            _provider = provider;
        }

        public SettingsProvider Provider
        {
            get
            {
                return _provider;
            }
        }
    }
}
