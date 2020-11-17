// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Transactions.Configuration
{
    internal static class AppSettings
    {
        private static volatile bool s_settingsInitalized;
        private static readonly object s_appSettingsLock = new object();
        private static bool s_includeDistributedTxIdInExceptionMessage;

        private static void EnsureSettingsLoaded()
        {
            if (!s_settingsInitalized)
            {
                lock (s_appSettingsLock)
                {
                    if (!s_settingsInitalized)
                    {
                        // TODO: Determine how to handle configuration.
                        // This uses System.Configuration on .NET Framework to load:
                        // Transactions:IncludeDistributedTransactionIdInExceptionMessage
                        s_includeDistributedTxIdInExceptionMessage = false;
                        s_settingsInitalized = true;
                    }
                }
            }
        }

        internal static bool IncludeDistributedTxIdInExceptionMessage
        {
            get
            {
                EnsureSettingsLoaded();
                return s_includeDistributedTxIdInExceptionMessage;
            }
        }
    }
}
