// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security;
using System.Security.Authentication;
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

        private static readonly Lazy<bool> s_IsInHelix = new Lazy<bool>(() => Environment.GetEnvironmentVariables().Keys.Cast<string>().Any(key => key.StartsWith("HELIX")));
        public static bool IsInHelix => s_IsInHelix.Value;

        public static bool IsNetCore => Environment.Version.Major >= 5 || RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
        public static bool IsMonoRuntime => Type.GetType("Mono.RuntimeStructs") != null;
        public static bool IsNotMonoRuntime => !IsMonoRuntime;
        public static bool IsMonoInterpreter => GetIsRunningOnMonoInterpreter();
        public static bool IsNotMonoInterpreter => !IsMonoInterpreter;
        public static bool IsMonoAOT => Environment.GetEnvironmentVariable("MONO_AOT_MODE") == "aot";
        public static bool IsNotMonoAOT => Environment.GetEnvironmentVariable("MONO_AOT_MODE") != "aot";
        public static bool IsNativeAot => IsNotMonoRuntime && !IsReflectionEmitSupported;
        public static bool IsNotNativeAot => !IsNativeAot;
        public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"));
        public static bool IsNetBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));
        public static bool IsAndroid => RuntimeInformation.IsOSPlatform(OSPlatform.Create("ANDROID"));
        public static bool IsNotAndroid => !IsAndroid;
        public static bool IsAndroidX86 => IsAndroid && IsX86Process;
        public static bool IsNotAndroidX86 => !IsAndroidX86;
        public static bool IsiOS => RuntimeInformation.IsOSPlatform(OSPlatform.Create("IOS"));
        public static bool IstvOS => RuntimeInformation.IsOSPlatform(OSPlatform.Create("TVOS"));
        public static bool IsMacCatalyst => RuntimeInformation.IsOSPlatform(OSPlatform.Create("MACCATALYST"));
        public static bool IsNotMacCatalyst => !IsMacCatalyst;
        public static bool Isillumos => RuntimeInformation.IsOSPlatform(OSPlatform.Create("ILLUMOS"));
        public static bool IsSolaris => RuntimeInformation.IsOSPlatform(OSPlatform.Create("SOLARIS"));
        public static bool IsBrowser => RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
        public static bool IsWasi => RuntimeInformation.IsOSPlatform(OSPlatform.Create("WASI"));
        public static bool IsNotBrowser => !IsBrowser;
        public static bool IsNotWasi => !IsWasi;
        public static bool IsMobile => IsBrowser || IsWasi || IsAppleMobile || IsAndroid;
        public static bool IsNotMobile => !IsMobile;
        public static bool IsAppleMobile => IsMacCatalyst || IsiOS || IstvOS;
        public static bool IsNotAppleMobile => !IsAppleMobile;
        public static bool IsNotNetFramework => !IsNetFramework;
        public static bool IsBsdLike => IsApplePlatform || IsFreeBSD || IsNetBSD;

        public static bool IsArmProcess => RuntimeInformation.ProcessArchitecture == Architecture.Arm;
        public static bool IsNotArmProcess => !IsArmProcess;
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public static bool IsNotArm64Process => !IsArm64Process;
        public static bool IsArmOrArm64Process => IsArmProcess || IsArm64Process;
        public static bool IsNotArmNorArm64Process => !IsArmOrArm64Process;
        public static bool IsS390xProcess => (int)RuntimeInformation.ProcessArchitecture == 5; // Architecture.S390x
        public static bool IsArmv6Process => (int)RuntimeInformation.ProcessArchitecture == 7; // Architecture.Armv6
        public static bool IsPpc64leProcess => (int)RuntimeInformation.ProcessArchitecture == 8; // Architecture.Ppc64le
        public static bool IsRiscV64Process => (int)RuntimeInformation.ProcessArchitecture == 9; // Architecture.RiscV64;
        public static bool IsX64Process => RuntimeInformation.ProcessArchitecture == Architecture.X64;
        public static bool IsX86Process => RuntimeInformation.ProcessArchitecture == Architecture.X86;
        public static bool IsNotX86Process => !IsX86Process;
        public static bool IsArgIteratorSupported => IsMonoRuntime || (IsWindows && IsNotArmProcess && !IsNativeAot);
        public static bool IsArgIteratorNotSupported => !IsArgIteratorSupported;
        public static bool Is32BitProcess => IntPtr.Size == 4;
        public static bool Is64BitProcess => IntPtr.Size == 8;
        public static bool IsNotWindows => !IsWindows;

        private static volatile int s_isPrivilegedProcess = -1;
        public static bool IsPrivilegedProcess
        {
            get
            {
                int p = s_isPrivilegedProcess;
                if (p == -1)
                {
                    s_isPrivilegedProcess = p = AdminHelpers.IsProcessElevated() ? 1 : 0;
                }

                return p == 1;
            }
        }

        public static bool IsNotPrivilegedProcess => !IsPrivilegedProcess;

        public static bool IsMarshalGetExceptionPointersSupported => !IsMonoRuntime && !IsNativeAot;

        private static readonly Lazy<bool> s_isCheckedRuntime = new Lazy<bool>(() => AssemblyConfigurationEquals("Checked"));
        private static readonly Lazy<bool> s_isReleaseRuntime = new Lazy<bool>(() => AssemblyConfigurationEquals("Release"));
        private static readonly Lazy<bool> s_isDebugRuntime = new Lazy<bool>(() => AssemblyConfigurationEquals("Debug"));

        public static bool IsCheckedRuntime => s_isCheckedRuntime.Value;
        public static bool IsReleaseRuntime => s_isReleaseRuntime.Value;
        public static bool IsDebugRuntime => s_isDebugRuntime.Value;

        public static bool IsReleaseLibrary(Assembly assembly) => !IsDebuggable(assembly);
        public static bool IsDebugLibrary(Assembly assembly) => IsDebuggable(assembly);

        // For use as needed on tests that time out when run on a Debug or Checked runtime.
        // Not relevant for timeouts on external activities, such as network timeouts.
        public static int SlowRuntimeTimeoutModifier
        {
            get
            {
                if (IsReleaseRuntime)
                    return 1;
                if (IsRiscV64Process)
                    return IsDebugRuntime? 10 : 2;
                else
                    return IsDebugRuntime? 5 : 1;
            }
        }

        public static bool IsCaseInsensitiveOS => IsWindows || IsOSX || IsMacCatalyst;
        public static bool IsCaseSensitiveOS => !IsCaseInsensitiveOS;

#if NETCOREAPP
        public static bool FileCreateCaseSensitive => IsCaseSensitiveOS && !RuntimeInformation.RuntimeIdentifier.StartsWith("iossimulator")
                                                                        && !RuntimeInformation.RuntimeIdentifier.StartsWith("tvossimulator");
#else
        public static bool FileCreateCaseSensitive => IsCaseSensitiveOS;
#endif

        public static bool IsThreadingSupported => (!IsWasi && !IsBrowser) || IsWasmThreadingSupported;
        public static bool IsWasmThreadingSupported => IsBrowser && IsEnvironmentVariableTrue("IsBrowserThreadingSupported");
        public static bool IsNotWasmThreadingSupported => !IsWasmThreadingSupported;
        public static bool IsWasmBackgroundExec => IsBrowser && IsEnvironmentVariableTrue("IsWasmBackgroundExec");
        public static bool IsWasmBackgroundExecOrSingleThread => IsWasmBackgroundExec || IsNotWasmThreadingSupported;
        public static bool IsThreadingSupportedOrBrowserBackgroundExec => IsWasmBackgroundExec || !IsBrowser;
        public static bool IsBinaryFormatterSupported => IsNotMobile && !IsNativeAot;

        public static bool IsStartingProcessesSupported => !IsiOS && !IstvOS;

        public static bool IsSpeedOptimized => !IsSizeOptimized;
        public static bool IsSizeOptimized => IsBrowser || IsWasi || IsAndroid || IsAppleMobile;

        public static bool IsBrowserDomSupported => IsEnvironmentVariableTrue("IsBrowserDomSupported");
        public static bool IsBrowserDomSupportedOrNotBrowser => IsNotBrowser || IsBrowserDomSupported;
        public static bool IsBrowserDomSupportedOrNodeJS => IsBrowserDomSupported || IsNodeJS;
        public static bool IsNotBrowserDomSupported => !IsBrowserDomSupported;
        public static bool IsWebSocketSupported => IsEnvironmentVariableTrue("IsWebSocketSupported");
        public static bool IsNodeJS => IsEnvironmentVariableTrue("IsNodeJS");
        public static bool IsNotNodeJS => !IsNodeJS;
        public static bool IsNodeJSOnWindows => GetNodeJSPlatform() == "win32";
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

        public static bool IsAsyncFileIOSupported => !IsBrowser && !IsWasi;

        public static bool IsLineNumbersSupported => !IsNativeAot;

        public static bool IsInContainer => GetIsInContainer();
        public static bool IsNotInContainer => !IsInContainer;
        public static bool SupportsComInterop => IsWindows && IsNotMonoRuntime && !IsNativeAot; // matches definitions in clr.featuredefines.props

#if NETCOREAPP
        public static bool IsBuiltInComEnabled => SupportsComInterop
                                            && (AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out bool isEnabled)
                                                ? isEnabled
                                                : true);
#else
        public static bool IsBuiltInComEnabled => SupportsComInterop;
#endif

        // Automation refers to OLE Automation support. Automation support here means the OS
        // and runtime provide support for the following: IDispatch, STA apartments, etc. This
        // is typically available whenever COM support is enabled, but Windows Nano Server is an exception.
        public static bool IsBuiltInComEnabledWithOSAutomationSupport => IsBuiltInComEnabled && IsNotWindowsNanoServer;

        public static bool SupportsSsl3 => GetSsl3Support();
        public static bool SupportsSsl2 => IsWindows && !PlatformDetection.IsWindows10Version1607OrGreater;

#if NETCOREAPP
        public static bool IsReflectionEmitSupported => RuntimeFeature.IsDynamicCodeSupported;
        public static bool IsNotReflectionEmitSupported => !IsReflectionEmitSupported;
#else
        public static bool IsReflectionEmitSupported => true;
#endif

        public static bool IsInvokingStaticConstructorsSupported => !IsNativeAot;
        public static bool IsInvokingFinalizersSupported => !IsNativeAot;
        public static bool IsTypeEquivalenceSupported => !IsNativeAot && !IsMonoRuntime && IsWindows;

        public static bool IsMetadataUpdateSupported => !IsNativeAot;

        // System.Security.Cryptography.Xml.XmlDsigXsltTransform.GetOutput() relies on XslCompiledTransform which relies
        // heavily on Reflection.Emit
        public static bool IsXmlDsigXsltTransformSupported => !PlatformDetection.IsInAppContainer && IsReflectionEmitSupported;

        public static bool IsPreciseGcSupported => !IsMonoRuntime;

        public static bool IsRareEnumsSupported => !IsNativeAot;

        public static bool IsNotIntMaxValueArrayIndexSupported => s_largeArrayIsNotSupported.Value;

        public static bool IsAssemblyLoadingSupported => !IsNativeAot;
        public static bool IsNonBundledAssemblyLoadingSupported => IsAssemblyLoadingSupported && !IsMonoAOT;
        public static bool IsMethodBodySupported => !IsNativeAot;
        public static bool IsDebuggerTypeProxyAttributeSupported => !IsNativeAot;
        public static bool HasAssemblyFiles => !string.IsNullOrEmpty(typeof(PlatformDetection).Assembly.Location);
        public static bool HasHostExecutable => HasAssemblyFiles; // single-file don't have a host
        public static bool IsSingleFile => !HasAssemblyFiles;

        public static bool IsReadyToRunCompiled => Environment.GetEnvironmentVariable("TEST_READY_TO_RUN_MODE") == "1";

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
        public static bool OpenSslNotPresentOnSystem => !OpenSslPresentOnSystem;

        public static bool UsesAppleCrypto => IsOSX || IsMacCatalyst || IsiOS || IstvOS;
        public static bool UsesMobileAppleCrypto => IsMacCatalyst || IsiOS || IstvOS;

        // Changed to `true` when trimming
        public static bool IsBuiltWithAggressiveTrimming => IsNativeAot;
        public static bool IsNotBuiltWithAggressiveTrimming => !IsBuiltWithAggressiveTrimming;
        public static bool IsTrimmedWithILLink => IsBuiltWithAggressiveTrimming && !IsNativeAot;

        // Windows - Schannel supports alpn from win8.1/2012 R2 and higher.
        // Linux - OpenSsl supports alpn from openssl 1.0.2 and higher.
        // Android - Platform supports alpn from API level 29 and higher
        private static readonly Lazy<bool> s_supportsAlpn = new Lazy<bool>(GetAlpnSupport);
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

            if (IsOSX)
            {
                return true;
            }

            return false;
        }

        public static bool SupportsAlpn => s_supportsAlpn.Value;
        public static bool SupportsClientAlpn => SupportsAlpn || IsOSX || IsMacCatalyst || IsiOS || IstvOS;
        public static bool SupportsHardLinkCreation => !IsAndroid && !IsLinuxBionic;

        private static readonly Lazy<bool> s_supportsTls10 = new Lazy<bool>(GetTls10Support);
        private static readonly Lazy<bool> s_supportsTls11 = new Lazy<bool>(GetTls11Support);
        private static readonly Lazy<bool> s_supportsTls12 = new Lazy<bool>(GetTls12Support);
        private static readonly Lazy<bool> s_supportsTls13 = new Lazy<bool>(GetTls13Support);
        private static readonly Lazy<bool> s_sendsCAListByDefault = new Lazy<bool>(GetSendsCAListByDefault);
        private static readonly Lazy<bool> s_supportsSha3 = new Lazy<bool>(GetSupportsSha3);

        public static bool SupportsTls10 => s_supportsTls10.Value;
        public static bool SupportsTls11 => s_supportsTls11.Value;
        public static bool SupportsTls12 => s_supportsTls12.Value;
        public static bool SupportsTls13 => s_supportsTls13.Value;
        public static bool SendsCAListByDefault => s_sendsCAListByDefault.Value;
        public static bool SupportsSendingCustomCANamesInTls => UsesAppleCrypto || IsOpenSslSupported || (PlatformDetection.IsWindows8xOrLater && SendsCAListByDefault);
        public static bool SupportsSha3 => s_supportsSha3.Value;
        public static bool DoesNotSupportSha3 => !s_supportsSha3.Value;

        private static readonly Lazy<bool> s_largeArrayIsNotSupported = new Lazy<bool>(IsLargeArrayNotSupported);

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

        private static readonly Lazy<bool> m_isInvariant = new Lazy<bool>(()
            => (bool?)Type.GetType("System.Globalization.GlobalizationMode")?.GetProperty("Invariant", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) == true);

        private static readonly Lazy<bool> m_isHybrid = new Lazy<bool>(()
            => (bool?)Type.GetType("System.Globalization.GlobalizationMode")?.GetProperty("Hybrid", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) == true);

        private static readonly Lazy<Version> m_icuVersion = new Lazy<Version>(GetICUVersion);
        public static Version ICUVersion => m_icuVersion.Value;

        public static bool IsInvariantGlobalization => m_isInvariant.Value;
        public static bool IsHybridGlobalization => m_isHybrid.Value;
        public static bool IsHybridGlobalizationOnBrowser => m_isHybrid.Value && IsBrowser;
        public static bool IsHybridGlobalizationOnApplePlatform => m_isHybrid.Value && (IsMacCatalyst || IsiOS || IstvOS);
        public static bool IsNotHybridGlobalizationOnBrowser => !IsHybridGlobalizationOnBrowser;
        public static bool IsNotInvariantGlobalization => !IsInvariantGlobalization;
        public static bool IsNotHybridGlobalization => !IsHybridGlobalization;
        public static bool IsNotHybridGlobalizationOnApplePlatform => !IsHybridGlobalizationOnApplePlatform;

        // HG on apple platforms implies ICU
        public static bool IsIcuGlobalization => !IsInvariantGlobalization && (IsHybridGlobalizationOnApplePlatform || ICUVersion > new Version(0, 0, 0, 0));

        public static bool IsIcuGlobalizationAndNotHybridOnBrowser => IsIcuGlobalization && IsNotHybridGlobalizationOnBrowser;
        public static bool IsNlsGlobalization => IsNotInvariantGlobalization && !IsIcuGlobalization && !IsHybridGlobalization;

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
            // When HG on Apple platforms, our ICU lib is not loaded
            if (IsNotHybridGlobalizationOnApplePlatform)
            {
                try
                {
                    Type interopGlobalization = Type.GetType("Interop+Globalization, System.Private.CoreLib");
                    if (interopGlobalization != null)
                    {
                        MethodInfo methodInfo = interopGlobalization.GetMethod("GetICUVersion", BindingFlags.NonPublic | BindingFlags.Static);
                        if (methodInfo != null)
                        {
                            // Ensure that ICU has been loaded
                            GC.KeepAlive(System.Globalization.CultureInfo.InstalledUICulture);

                            version = (int)methodInfo.Invoke(null, null);
                        }
                    }
                }
                catch { }
            }

            return new Version(version >> 24,
                              (version >> 16) & 0xFF,
                              (version >> 8) & 0xFF,
                              version & 0xFF);
        }

        private static readonly Lazy<bool> s_fileLockingDisabled = new Lazy<bool>(()
            => (bool?)Type.GetType("Microsoft.Win32.SafeHandles.SafeFileHandle")?.GetProperty("DisableFileLocking", BindingFlags.NonPublic | BindingFlags.Static)?.GetValue(null) == true);

        public static bool IsFileLockingEnabled => IsWindows || !s_fileLockingDisabled.Value;

        private static bool GetIsInContainer()
        {
            if (IsWindows)
            {
                string key = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control";
                return Registry.GetValue(key, "ContainerType", defaultValue: null) != null;
            }

            // '/.dockerenv' - is to check if this is running in a codespace
            return (IsLinux && File.Exists("/.dockerenv"));
        }

        private static bool GetProtocolSupportFromWindowsRegistry(SslProtocols protocol, bool defaultProtocolSupport, bool disabledByDefault = false)
        {
            string registryProtocolName = protocol switch
            {
#pragma warning disable CS0618 // Ssl2 and Ssl3 are obsolete
                SslProtocols.Ssl3 => "SSL 3.0",
#pragma warning restore CS0618
#pragma warning disable SYSLIB0039 // TLS versions 1.0 and 1.1 have known vulnerabilities
                SslProtocols.Tls => "TLS 1.0",
                SslProtocols.Tls11 => "TLS 1.1",
#pragma warning restore SYSLIB0039
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

#pragma warning disable SYSLIB0039 // TLS versions 1.0 and 1.1 have known vulnerabilities
        private static bool GetTls10Support()
        {
            // on macOS and Android TLS 1.0 is supported.
            if (IsApplePlatform || IsAndroid)
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
            else if (IsApplePlatform || IsAndroid)
            {
                return true;
            }

            return OpenSslGetTlsSupport(SslProtocols.Tls11);
        }
#pragma warning restore SYSLIB0039

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

            return Environment.GetEnvironmentVariable(variableName) is "true";
        }

        private static string GetNodeJSPlatform()
        {
            if (!IsNodeJS)
                return null;

            return Environment.GetEnvironmentVariable("NodeJSPlatform");
        }

        private static bool AssemblyConfigurationEquals(string configuration)
        {
            AssemblyConfigurationAttribute assemblyConfigurationAttribute = typeof(string).Assembly.GetCustomAttribute<AssemblyConfigurationAttribute>();

            return assemblyConfigurationAttribute != null &&
                string.Equals(assemblyConfigurationAttribute.Configuration, configuration, StringComparison.InvariantCulture);
        }

        private static bool IsDebuggable(Assembly assembly)
        {
            DebuggableAttribute debuggableAttribute = assembly.GetCustomAttribute<DebuggableAttribute>();

            return debuggableAttribute != null && debuggableAttribute.IsJITTrackingEnabled;
        }

        private static bool GetSupportsSha3()
        {
            if (IsOpenSslSupported)
            {
                if (OpenSslVersion.Major >= 3)
                {
                    return true;
                }

                return OpenSslVersion.Major == 1 && OpenSslVersion.Minor >= 1 && OpenSslVersion.Build >= 1;
            }

            if (IsWindowsVersionOrLater(10, 0, 25324))
            {
                return true;
            }

            return false;
        }
    }
}
