// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq.Expressions;
using System.Security;
using System.Security.Authentication;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.CompilerServices;
using Microsoft.Win32;
using Xunit;

namespace System
{
    public static partial class PlatformDetection
    {
        //
        // Do not use the " { get; } = <expression> " pattern here. Having all the initialization happen in the type initializer
        // means that one exception anywhere means all tests using PlatformDetection fail. If you feel a value is worth latching,
        // do it in a way that failures don't cascade.
        //

        public static bool IsNetCore => Environment.Version.Major >= 5 || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
        public static bool IsMonoRuntime => Type.GetType("Mono.RuntimeStructs") != null;
        public static bool IsNotMonoRuntime => !IsMonoRuntime;
        public static bool IsMonoInterpreter => GetIsRunningOnMonoInterpreter();
        public static bool IsMonoAOT => Environment.GetEnvironmentVariable("MONO_AOT_MODE") == "aot";
        public static bool IsNotMonoAOT => Environment.GetEnvironmentVariable("MONO_AOT_MODE") != "aot";
        public static bool IsNativeAot => IsNotMonoRuntime && !IsReflectionEmitSupported;
        public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"));
        public static bool IsNetBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));
        public static bool IsAndroid => RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
        public static bool IsiOS => RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"));
        public static bool IstvOS => RuntimeInformation.IsOSPlatform(OSPlatform.Create("TVOS"));
        public static bool IsMacCatalyst => RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACCATALYST"));
        public static bool Isillumos => RuntimeInformation.IsOSPlatform(OSPlatform.Create("ILLUMOS"));
        public static bool IsSolaris => RuntimeInformation.IsOSPlatform(OSPlatform.Create("SOLARIS"));
        public static bool IsBrowser => RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
        public static bool IsNotBrowser => !IsBrowser;
        public static bool IsMobile => IsBrowser || IsAppleMobile || IsAndroid;
        public static bool IsNotMobile => !IsMobile;
        public static bool IsAppleMobile => IsMacCatalyst || IsiOS || IstvOS;
        public static bool IsNotAppleMobile => !IsAppleMobile;
        public static bool IsNotNetFramework => !IsNetFramework;
        public static bool IsBsdLike => IsOSXLike || IsFreeBSD || IsNetBSD;

        public static bool IsArmProcess => RuntimeInformation.ProcessArchitecture == Architecture.Arm;
        public static bool IsNotArmProcess => !IsArmProcess;
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public static bool IsNotArm64Process => !IsArm64Process;
        public static bool IsArmOrArm64Process => IsArmProcess || IsArm64Process;
        public static bool IsNotArmNorArm64Process => !IsArmOrArm64Process;
        public static bool IsArmv6Process => (int)RuntimeInformation.ProcessArchitecture == 7; // Architecture.Armv6
        public static bool IsX64Process => RuntimeInformation.ProcessArchitecture == Architecture.X64;
        public static bool IsX86Process => RuntimeInformation.ProcessArchitecture == Architecture.X86;
        public static bool IsNotX86Process => !IsX86Process;
        public static bool IsArgIteratorSupported => IsMonoRuntime || (IsWindows && IsNotArmProcess && !IsNativeAot);
        public static bool IsArgIteratorNotSupported => !IsArgIteratorSupported;
        public static bool Is32BitProcess => IntPtr.Size == 4;
        public static bool Is64BitProcess => IntPtr.Size == 8;
        public static bool IsNotWindows => !IsWindows;

        private static Lazy<bool> s_isCheckedRuntime => new Lazy<bool>(() => AssemblyConfigurationEquals("Checked"));
        private static Lazy<bool> s_isReleaseRuntime => new Lazy<bool>(() => AssemblyConfigurationEquals("Release"));
        private static Lazy<bool> s_isDebugRuntime => new Lazy<bool>(() => AssemblyConfigurationEquals("Debug"));

        public static bool IsCheckedRuntime => s_isCheckedRuntime.Value;
        public static bool IsReleaseRuntime => s_isReleaseRuntime.Value;
        public static bool IsDebugRuntime => s_isDebugRuntime.Value;

        // For use as needed on tests that time out when run on a Debug runtime.
        // Not relevant for timeouts on external activities, such as network timeouts.
        public static int SlowRuntimeTimeoutModifier = (PlatformDetection.IsDebugRuntime ? 5 : 1);

        public static bool IsCaseInsensitiveOS => IsWindows || IsOSX || IsMacCatalyst;

#if NETCOREAPP
        public static bool IsCaseSensitiveOS => !IsCaseInsensitiveOS && !RuntimeInformation.RuntimeIdentifier.StartsWith("iossimulator")
                                                                     && !RuntimeInformation.RuntimeIdentifier.StartsWith("tvossimulator");
#else
        public static bool IsCaseSensitiveOS => !IsCaseInsensitiveOS;
#endif

        public static bool IsThreadingSupported => !IsBrowser;
        public static bool IsBinaryFormatterSupported => IsNotMobile && !IsNativeAot;
        public static bool IsSymLinkSupported => !IsiOS && !IstvOS;

        public static bool IsSpeedOptimized => !IsSizeOptimized;
        public static bool IsSizeOptimized => IsBrowser || IsAndroid || IsAppleMobile;

        public static bool IsBrowserDomSupported => IsEnvironmentVariableTrue("IsBrowserDomSupported");
        public static bool IsBrowserDomSupportedOrNotBrowser => IsNotBrowser || IsBrowserDomSupported;
        public static bool IsNotBrowserDomSupported => !IsBrowserDomSupported;
        public static bool IsWebSocketSupported => IsEnvironmentVariableTrue("IsWebSocketSupported");
        public static bool IsNodeJS => IsEnvironmentVariableTrue("IsNodeJS");
        public static bool IsNotNodeJS => !IsNodeJS;
        public static bool LocalEchoServerIsNotAvailable => !LocalEchoServerIsAvailable;
        public static bool LocalEchoServerIsAvailable => IsBrowser;

        public static bool IsUsingLimitedCultures => !IsNotMobile;
        public static bool IsNotUsingLimitedCultures => IsNotMobile;

        public static bool IsLinqExpressionsBuiltWithIsInterpretingOnly => s_linqExpressionsBuiltWithIsInterpretingOnly.Value;
        public static bool IsNotLinqExpressionsBuiltWithIsInterpretingOnly => !IsLinqExpressionsBuiltWithIsInterpretingOnly;
        private static readonly Lazy<bool> s_linqExpressionsBuiltWithIsInterpretingOnly = new Lazy<bool>(GetLinqExpressionsBuiltWithIsInterpretingOnly);
        private static bool GetLinqExpressionsBuiltWithIsInterpretingOnly()
        {
            return !(bool)typeof(LambdaExpression).GetMethod("get_CanCompileToIL").Invoke(null, Array.Empty<object>());
        }

        // Drawing is not supported on non windows platforms in .NET 7.0+.
        public static bool IsDrawingSupported => IsWindows && IsNotWindowsNanoServer && IsNotWindowsServerCore;

        public static bool IsAsyncFileIOSupported => !IsBrowser;

        public static bool IsLineNumbersSupported => !IsNativeAot;

        public static bool IsInContainer => GetIsInContainer();
        public static bool SupportsComInterop => IsWindows && IsNotMonoRuntime && !IsNativeAot; // matches definitions in clr.featuredefines.props
        public static bool SupportsSsl3 => GetSsl3Support();
        public static bool SupportsSsl2 => IsWindows && !PlatformDetection.IsWindows10Version1607OrGreater;

#if NETCOREAPP
        public static bool IsReflectionEmitSupported => RuntimeFeature.IsDynamicCodeSupported;
        public static bool IsNotReflectionEmitSupported => !IsReflectionEmitSupported;
#else
        public static bool IsReflectionEmitSupported => true;
#endif

        public static bool IsInvokingStaticConstructorsSupported => !IsNativeAot;

        public static bool IsMetadataUpdateSupported => !IsNativeAot;

        // System.Security.Cryptography.Xml.XmlDsigXsltTransform.GetOutput() relies on XslCompiledTransform which relies
        // heavily on Reflection.Emit
        public static bool IsXmlDsigXsltTransformSupported => !PlatformDetection.IsInAppContainer && IsReflectionEmitSupported;

        public static bool IsPreciseGcSupported => !IsMonoRuntime;

        public static bool IsNotIntMaxValueArrayIndexSupported => s_largeArrayIsNotSupported.Value;

        public static bool IsAssemblyLoadingSupported => !IsNativeAot;
        public static bool IsMethodBodySupported => !IsNativeAot;
        public static bool IsDebuggerTypeProxyAttributeSupported => !IsNativeAot;

        private static volatile Tuple<bool> s_lazyNonZeroLowerBoundArraySupported;
        public static bool IsNonZeroLowerBoundArraySupported
        {
            get
            {
                if (s_lazyNonZeroLowerBoundArraySupported == null)
                {
                    bool nonZeroLowerBoundArraysSupported = false;
                    try
                    {
                        Array.CreateInstance(typeof(int), new int[] { 5 }, new int[] { 5 });
                        nonZeroLowerBoundArraysSupported = true;
                    }
                    catch (PlatformNotSupportedException)
                    {
                    }
                    s_lazyNonZeroLowerBoundArraySupported = Tuple.Create<bool>(nonZeroLowerBoundArraysSupported);
                }
                return s_lazyNonZeroLowerBoundArraySupported.Item1;
            }
        }

        private static volatile Tuple<bool> s_lazyMetadataTokensSupported;
        public static bool IsMetadataTokenSupported
        {
            get
            {
                if (s_lazyMetadataTokensSupported == null)
                {
                    bool metadataTokensSupported = false;
                    try
                    {
                        _ = typeof(PlatformDetection).MetadataToken;
                        metadataTokensSupported = true;
                    }
                    catch (InvalidOperationException)
                    {
                    }
                    s_lazyMetadataTokensSupported = Tuple.Create<bool>(metadataTokensSupported);
                }
                return s_lazyMetadataTokensSupported.Item1;
            }
        }

        public static bool IsDomainJoinedMachine => !Environment.MachineName.Equals(Environment.UserDomainName, StringComparison.OrdinalIgnoreCase);
        public static bool IsNotDomainJoinedMachine => !IsDomainJoinedMachine;

        public static bool IsOpenSslSupported => IsLinux || IsFreeBSD || Isillumos || IsSolaris;

        public static bool UsesAppleCrypto => IsOSX || IsMacCatalyst || IsiOS || IstvOS;
        public static bool UsesMobileAppleCrypto => IsMacCatalyst || IsiOS || IstvOS;

        // Changed to `true` when linking
        public static bool IsBuiltWithAggressiveTrimming => false;
        public static bool IsNotBuiltWithAggressiveTrimming => !IsBuiltWithAggressiveTrimming;

        // Windows - Schannel supports alpn from win8.1/2012 R2 and higher.
        // Linux - OpenSsl supports alpn from openssl 1.0.2 and higher.
        // OSX - SecureTransport doesn't expose alpn APIs. TODO https://github.com/dotnet/runtime/issues/27727
        // Android - Platform supports alpn from API level 29 and higher
        private static Lazy<bool> s_supportsAlpn = new Lazy<bool>(GetAlpnSupport);
        private static bool GetAlpnSupport()
        {
            if (IsWindows && !IsWindows7 && !IsNetFramework)
            {
                return true;
            }

            if (IsOpenSslSupported)
            {
                if (OpenSslVersion.Major >= 3)
                {
                    return true;
                }

                return OpenSslVersion.Major == 1 && (OpenSslVersion.Minor >= 1 || OpenSslVersion.Build >= 2);
            }

            if (IsAndroid)
            {
                return Interop.AndroidCrypto.SSLSupportsApplicationProtocolsConfiguration();
            }

            return false;
        }

        public static bool SupportsAlpn => s_supportsAlpn.Value;
        public static bool SupportsClientAlpn => SupportsAlpn || IsOSX || IsMacCatalyst || IsiOS || IstvOS;

        private static Lazy<bool> s_supportsTls10 = new Lazy<bool>(GetTls10Support);
        private static Lazy<bool> s_supportsTls11 = new Lazy<bool>(GetTls11Support);
        private static Lazy<bool> s_supportsTls12 = new Lazy<bool>(GetTls12Support);
        private static Lazy<bool> s_supportsTls13 = new Lazy<bool>(GetTls13Support);
        private static Lazy<bool> s_sendsCAListByDefault = new Lazy<bool>(GetSendsCAListByDefault);

        public static bool SupportsTls10 => s_supportsTls10.Value;
        public static bool SupportsTls11 => s_supportsTls11.Value;
        public static bool SupportsTls12 => s_supportsTls12.Value;
        public static bool SupportsTls13 => s_supportsTls13.Value;
        public static bool SendsCAListByDefault => s_sendsCAListByDefault.Value;
        public static bool SupportsSendingCustomCANamesInTls => UsesAppleCrypto || IsOpenSslSupported || (PlatformDetection.IsWindows8xOrLater && SendsCAListByDefault);

        private static Lazy<bool> s_largeArrayIsNotSupported = new Lazy<bool>(IsLargeArrayNotSupported);

        [MethodImpl(MethodImplOptions.NoOptimization)]
        private static bool IsLargeArrayNotSupported()
        {
            try
            {
                var tmp = new byte[int.MaxValue];
                return tmp == null;
            }
            catch (OutOfMemoryException)
            {
                return true;
            }
        }

        public static string GetDistroVersionString()
        {
            if (IsWindows)
            {
                return "WindowsProductType=" + GetWindowsProductType() + " WindowsInstallationType=" + GetWindowsInstallationType();
            }
            else if (IsOSX)
            {
                return "OSX Version=" + Environment.OSVersion.Version.ToString();
            }
            else
            {
                DistroInfo v = GetDistroInfo();

                return $"Distro={v.Id} VersionId={v.VersionId}";
            }
        }

        private static readonly Lazy<bool> m_isInvariant = new Lazy<bool>(() => GetStaticNonPublicBooleanPropertyValue("System.Globalization.GlobalizationMode", "Invariant"));

        private static bool GetStaticNonPublicBooleanPropertyValue(string typeName, string propertyName)
        {
            if (Type.GetType(typeName)?.GetProperty(propertyName, BindingFlags.NonPublic | BindingFlags.Static)?.GetMethod is MethodInfo mi)
            {
                return (bool)mi.Invoke(null, null);
            }

            return false;
        }

        private static readonly Lazy<Version> m_icuVersion = new Lazy<Version>(GetICUVersion);
        public static Version ICUVersion => m_icuVersion.Value;

        public static bool IsInvariantGlobalization => m_isInvariant.Value;
        public static bool IsNotInvariantGlobalization => !IsInvariantGlobalization;
        public static bool IsIcuGlobalization => ICUVersion > new Version(0, 0, 0, 0);
        public static bool IsNlsGlobalization => IsNotInvariantGlobalization && !IsIcuGlobalization;

        public static bool IsSubstAvailable
        {
            get
            {
                try
                {
                    if (IsWindows)
                    {
                        string systemRoot = Environment.GetEnvironmentVariable("SystemRoot");
                        if (string.IsNullOrWhiteSpace(systemRoot))
                        {
                            return false;
                        }
                        string system32 = Path.Combine(systemRoot, "System32");
                        return File.Exists(Path.Combine(system32, "subst.exe"));
                    }
                }
                catch { }
                return false;
            }
        }

        private static Version GetICUVersion()
        {
            int version = 0;
            try
            {
                Type interopGlobalization = Type.GetType("Interop+Globalization, System.Private.CoreLib");
                if (interopGlobalization != null)
                {
                    MethodInfo methodInfo = interopGlobalization.GetMethod("GetICUVersion", BindingFlags.NonPublic | BindingFlags.Static);
                    if (methodInfo != null)
                    {
                        version = (int)methodInfo.Invoke(null, null);
                    }
                }
            }
            catch { }

            return new Version(version >> 24,
                              (version >> 16) & 0xFF,
                              (version >> 8) & 0xFF,
                              version & 0xFF);
        }

        private static readonly Lazy<bool> s_fileLockingDisabled = new Lazy<bool>(() => GetStaticNonPublicBooleanPropertyValue("Microsoft.Win32.SafeHandles.SafeFileHandle", "DisableFileLocking"));

        public static bool IsFileLockingEnabled => IsWindows || !s_fileLockingDisabled.Value;

        private static bool GetIsInContainer()
        {
            if (IsWindows)
            {
                string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control";
                return Registry.GetValue(key, "ContainerType", defaultValue: null) != null;
            }

            return (IsLinux && File.Exists("/.dockerenv"));
        }

        private static bool GetProtocolSupportFromWindowsRegistry(SslProtocols protocol, bool defaultProtocolSupport, bool disabledByDefault = false)
        {
            string registryProtocolName = protocol switch
            {
#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
                SslProtocols.Ssl3 => "SSL 3.0",
#pragma warning restore CS0618
                SslProtocols.Tls => "TLS 1.0",
                SslProtocols.Tls11 => "TLS 1.1",
                SslProtocols.Tls12 => "TLS 1.2",
#if !NETFRAMEWORK
                SslProtocols.Tls13 => "TLS 1.3",
#endif
                _ => throw new Exception($"Registry key not defined for {protocol}.")
            };

            string clientKey = @$"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\{registryProtocolName}\Client";
            string serverKey = @$"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\{registryProtocolName}\Server";

            object client, server;
            object clientDefault, serverDefault;
            try
            {
                client = Registry.GetValue(clientKey, "Enabled", defaultProtocolSupport ? 1 : 0);
                server = Registry.GetValue(serverKey, "Enabled", defaultProtocolSupport ? 1 : 0);

                clientDefault = Registry.GetValue(clientKey, "DisabledByDefault", 1);
                serverDefault = Registry.GetValue(serverKey, "DisabledByDefault", 1);

                if (client is int c && server is int s && clientDefault is int cd && serverDefault is int sd)
                {
                    return (c == 1 && s == 1) && (!disabledByDefault || (cd == 0 && sd == 0));
                }
            }
            catch (SecurityException)
            {
                // Insufficient permission, assume that we don't have protocol support (since we aren't exactly sure)
                return false;
            }
            catch { }

            return defaultProtocolSupport;
        }

        private static bool GetSsl3Support()
        {
            if (IsWindows)
            {

                // Missing key. If we're pre-20H1 then assume SSL3 is enabled.
                // Otherwise, disabled. (See comments on https://github.com/dotnet/runtime/issues/1166)
                // Alternatively the returned values must have been some other types.
                bool ssl3DefaultSupport = !IsWindows10Version2004OrGreater;

#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
                return GetProtocolSupportFromWindowsRegistry(SslProtocols.Ssl3, ssl3DefaultSupport);
#pragma warning restore CS0618

            }

            return (IsOSX || (IsLinux && OpenSslVersion < new Version(1, 0, 2) && !IsDebian));
        }

        private static bool OpenSslGetTlsSupport(SslProtocols protocol)
        {
            Debug.Assert(IsOpenSslSupported);

            int ret = Interop.OpenSsl.OpenSslGetProtocolSupport((int)protocol);
            return ret == 1;
        }

        private static readonly Lazy<SslProtocols> s_androidSupportedSslProtocols = new Lazy<SslProtocols>(Interop.AndroidCrypto.SSLGetSupportedProtocols);
        private static bool AndroidGetSslProtocolSupport(SslProtocols protocol)
        {
            Debug.Assert(IsAndroid);
            return (protocol & s_androidSupportedSslProtocols.Value) == protocol;
        }

        private static bool GetTls10Support()
        {
            // on macOS and Android TLS 1.0 is supported.
            if (IsOSXLike || IsAndroid)
            {
                return true;
            }

            // Windows depend on registry, enabled by default on all supported versions.
            if (IsWindows)
            {
                return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls, defaultProtocolSupport: true) && !IsWindows10Version20348OrGreater;
            }

            return OpenSslGetTlsSupport(SslProtocols.Tls);
        }

        private static bool GetTls11Support()
        {
            if (IsWindows)
            {
                // TLS 1.1 can work on Windows 7 but it is disabled by default.
                if (IsWindows7)
                {
                    return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls11, defaultProtocolSupport: false, disabledByDefault: true);
                }

                // It is enabled on other versions unless explicitly disabled.
                return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls11, defaultProtocolSupport: true) && !IsWindows10Version20348OrGreater;
            }
            // on macOS and Android TLS 1.1 is supported.
            else if (IsOSXLike || IsAndroid)
            {
                return true;
            }

            return OpenSslGetTlsSupport(SslProtocols.Tls11);
        }

        private static bool GetTls12Support()
        {
            if (IsWindows)
            {
                // TLS 1.2 can work on Windows 7 but it is disabled by default.
                if (IsWindows7)
                {
                    return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls12, defaultProtocolSupport: false, disabledByDefault: true);
                }

                // It is enabled on other versions unless explicitly disabled.
                return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls12, defaultProtocolSupport: true);
            }

            return true;
        }

        private static bool GetTls13Support()
        {
            if (IsWindows)
            {
                if (!IsWindows10Version2004OrGreater)
                {
                    return false;
                }
                // assume no if positive entry is missing on older Windows
                // Latest insider builds have TLS 1.3 enabled by default.
                // The build number is approximation.
                bool defaultProtocolSupport = IsWindows10Version20348OrGreater;

#if NETFRAMEWORK
                return false;
#else
                return GetProtocolSupportFromWindowsRegistry(SslProtocols.Tls13, defaultProtocolSupport);
#endif

            }
            else if (IsOSX || IsMacCatalyst || IsiOS || IstvOS)
            {
                // [ActiveIssue("https://github.com/dotnet/runtime/issues/1979")]
                return false;
            }
            else if (IsAndroid)
            {
#if NETFRAMEWORK
                return false;
#else
                return AndroidGetSslProtocolSupport(SslProtocols.Tls13);
#endif
            }
            else if (IsOpenSslSupported)
            {
                // Covers Linux, FreeBSD, illumos and Solaris
                return OpenSslVersion >= new Version(1, 1, 1);
            }

            return false;
        }

        private static bool GetSendsCAListByDefault()
        {
            if (IsWindows)
            {
                // Sending TrustedIssuers is conditioned on the registry. Win7 sends trusted issuer list by default,
                // newer Windows versions don't.
                object val = Registry.GetValue(@"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL", "SendTrustedIssuerList", IsWindows7 ? 1 : 0);
                if (val is int i)
                {
                    return i == 1;
                }
            }

            return false;
        }

        private static bool GetIsRunningOnMonoInterpreter()
        {
#if NETCOREAPP
            return IsMonoRuntime && RuntimeFeature.IsDynamicCodeSupported && !RuntimeFeature.IsDynamicCodeCompiled;
#else
            return false;
#endif
        }

        private static bool IsEnvironmentVariableTrue(string variableName)
        {
            if (!IsBrowser)
                return false;

            var val = Environment.GetEnvironmentVariable(variableName);
            return (val != null && val == "true");
        }

        private static bool AssemblyConfigurationEquals(string configuration)
        {
            AssemblyConfigurationAttribute assemblyConfigurationAttribute = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

            return assemblyConfigurationAttribute != null &&
                string.Equals(assemblyConfigurationAttribute.Configuration, configuration, StringComparison.InvariantCulture);
        }
    }
}
