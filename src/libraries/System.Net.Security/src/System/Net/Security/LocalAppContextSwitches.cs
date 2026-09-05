// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace System
{
    internal static partial class LocalAppContextSwitches
    {
        // OpenBSD's GSS-API (Heimdal) cannot drive password-based NTLM and reports a
        // missing Kerberos TGT as a missing credential, so managed NTLM is used there.
        internal static readonly bool IsOpenBsd = RuntimeInformation.IsOSPlatform(OSPlatform.Create("OPENBSD"));

        private static int s_disableTlsResume;
        internal static bool DisableTlsResume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.DisableTlsResume", "DOTNET_SYSTEM_NET_SECURITY_DISABLETLSRESUME", ref s_disableTlsResume);
        }

        // By default the peer certificate is not re-validated on a resumed (abbreviated) TLS
        // handshake, matching the behavior of common TLS stacks (e.g. OpenSSL, SChannel) that
        // do not re-run certificate verification when a session is resumed. This optimization
        // only applies on platforms where session resumption can be detected (currently Windows
        // and Linux); elsewhere the peer certificate is always re-validated. Enabling this
        // switch restores the previous behavior of re-validating the peer certificate (running
        // the chain build and the user validation callback) on every successful resumption.
        private static int s_revalidateCertificateOnTlsResume;
        internal static bool RevalidateCertificateOnTlsResume
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.RevalidateCertificateOnTlsResume", "DOTNET_SYSTEM_NET_SECURITY_REVALIDATECERTIFICATEONTLSRESUME", ref s_revalidateCertificateOnTlsResume);
        }

        private static int s_captureClientHello;
        internal static bool CaptureClientHello
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.CaptureClientHello", "DOTNET_SYSTEM_NET_SECURITY_CAPTURECLIENTHELLO", ref s_captureClientHello, defaultValue: true);
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

        private static int s_useLegacySslStreamHandshake;
        internal static bool UseLegacySslStreamHandshake
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.UseLegacySslStreamHandshake", "DOTNET_SYSTEM_NET_SECURITY_USELEGACYSSLSTREAMHANDSHAKE", ref s_useLegacySslStreamHandshake);
        }

#if !TARGET_WINDOWS
        private static int s_useManagedNtlm;
        [FeatureSwitchDefinition("System.Net.Security.UseManagedNtlm")]
        internal static bool UseManagedNtlm
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => GetCachedSwitchValue("System.Net.Security.UseManagedNtlm", ref s_useManagedNtlm,
                defaultValue: OperatingSystem.IsMacOS() || OperatingSystem.IsIOS() || OperatingSystem.IsMacCatalyst() ||
                IsOpenBsd ||
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
