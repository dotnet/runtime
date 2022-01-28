// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.Configuration
{
    internal sealed class MachineSettingsSection // ConfigurationSection
    {
        private static readonly MachineSettingsSection s_section = new MachineSettingsSection();
        private static TimeSpan s_maxTimeout = TimeSpan.Parse(ConfigurationStrings.DefaultMaxTimeout);

        internal static MachineSettingsSection GetSection() => s_section;

        public static TimeSpan MaxTimeout
        {
            get { return s_maxTimeout; }
        }
    }
}
