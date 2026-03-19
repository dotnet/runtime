// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        private static int s_disableTlsResume;
        internal static bool DisableTlsResume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.DisableTlsResume", "DOTNET_SYSTEM_NET_SECURITY_DISABLETLSRESUME", ref s_disableTlsResume);
        }

        private static int s_enableServerAiaDownloads;
        internal static bool EnableServerAiaDownloads
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.EnableServerAiaDownloads", "DOTNET_SYSTEM_NET_SECURITY_ENABLESERVERAIADOWNLOADS", ref s_enableServerAiaDownloads);
        }

        private static int s_enableOcspStapling;
        internal static bool EnableOcspStapling
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.EnableServerOcspStaplingFromOnlyCertificateOnLinux", ref s_enableOcspStapling);
        }

#if !TARGET_WINDOWS
        private static int s_useManagedNtlm;
        [FeatureSwitchDefinition("System.Net.Security.UseManagedNtlm")]
        internal static bool UseManagedNtlm
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.UseManagedNtlm", ref s_useManagedNtlm,
                defaultValue: OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ||
                (OperatingSystem.IsLinux() && RuntimeInformation.RuntimeIdentifier.StartsWith("linux-bionic-", StringComparison.OrdinalIgnoreCase)));
        }
#endif

#if TARGET_APPLE
        private static int s_useNetworkFramework;
        internal static bool UseNetworkFramework
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.UseNetworkFramework", "DOTNET_SYSTEM_NET_SECURITY_USENETWORKFRAMEWORK", ref s_useNetworkFramework);
        }
#endif
    }
}
