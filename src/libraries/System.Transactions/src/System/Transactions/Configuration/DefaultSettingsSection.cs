// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace System.Transactions.Configuration
{
    internal sealed class DefaultSettingsSection // ConfigurationSection
    {
        private static readonly DefaultSettingsSection s_section = new DefaultSettingsSection();
        private static TimeSpan s_timeout = TimeSpan.Parse(ConfigurationStrings.DefaultTimeout);

        internal static DefaultSettingsSection GetSection() => s_section;

        public string DistributedTransactionManagerName { get; set; } = ConfigurationStrings.DefaultDistributedTransactionManagerName;

        public TimeSpan Timeout
        {
            get { return s_timeout; }
            set
            {
                if (value < TimeSpan.Zero || value > TimeSpan.MaxValue)
                {
                    throw new ArgumentOutOfRangeException(nameof(Timeout), SR.ConfigInvalidTimeSpanValue);
                }
                s_timeout = value;
            }
        }
    }
}
