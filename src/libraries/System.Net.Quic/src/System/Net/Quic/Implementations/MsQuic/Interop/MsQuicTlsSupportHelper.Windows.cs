// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Microsoft.Win32;

namespace System.Net.Quic.Implementations.MsQuic.Internal
{
    internal static partial class MsQuicTlsSupportHelper
    {
        private const string SChannelTLS1_3RegKey = @"SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3";

        public static bool IsTls1_3Disabled()
        {
            using var tls13Key = Registry.LocalMachine.OpenSubKey(SChannelTLS1_3RegKey);

            if (tls13Key is null) return false;

            if (tls13Key.GetValue("Enabled") is int enabled)
            {
                return enabled == 0;
            }

            return false;
        }
    }
}
