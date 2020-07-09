// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Configuration
{
    /// <summary>
    /// Event args for the SettingLoaded event.
    /// </summary>
    public class SettingsLoadedEventArgs : EventArgs
    {
        private readonly SettingsProvider _provider;

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
