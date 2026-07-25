// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.Configuration
{
    internal sealed class DefaultSettingsSection // ConfigurationSection
    {
        private static readonly DefaultSettingsSection s_section = new DefaultSettingsSection();

        internal static DefaultSettingsSection GetSection() => s_section;

        public static string DistributedTransactionManagerName { get; set; } = ConfigurationStrings.DefaultDistributedTransactionManagerName;

        public static TimeSpan Timeout => TimeSpan.FromMinutes(1);
    }
}
