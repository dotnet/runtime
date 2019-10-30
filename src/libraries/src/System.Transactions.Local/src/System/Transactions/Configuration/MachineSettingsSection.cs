// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions.Configuration
{
    internal sealed class MachineSettingsSection // ConfigurationSection
    {
        private static readonly MachineSettingsSection s_section = new MachineSettingsSection();
        private static TimeSpan s_maxTimeout = TimeSpan.Parse(ConfigurationStrings.DefaultMaxTimeout);

        internal static MachineSettingsSection GetSection() => s_section;

        public TimeSpan MaxTimeout
        {
            get { return s_maxTimeout; }
            set
            {
                if (value < TimeSpan.Zero || value > TimeSpan.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(MaxTimeout), SR.ConfigInvalidTimeSpanValue);
                }
                s_maxTimeout = value;
            }
        }
    }
}
