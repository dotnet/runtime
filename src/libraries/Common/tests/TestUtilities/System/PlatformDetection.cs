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
        public static bool IsMobile => IsBrowser || IsMacCatalyst || IsiOS || IstvOS || IsAndroid;
        public static bool IsNotMobile => !IsMobile;
        public static bool IsNotNetFramework => !IsNetFramework;

        public static bool IsArmProcess => RuntimeInformation.ProcessArchitecture == Architecture.Arm;
        public static bool IsNotArmProcess => !IsArmProcess;
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public static bool IsNotArm64Process => !IsArm64Process;
        public static bool IsArmOrArm64Process => IsArmProcess || IsArm64Process;
        public static bool IsNotArmNorArm64Process => !IsArmOrArm64Process;
        public static bool IsArgIteratorSupported => IsMonoRuntime || (IsWindows && IsNotArmProcess);
        public static bool IsArgIteratorNotSupported => !IsArgIteratorSupported;
        public static bool Is32BitProcess => IntPtr.Size == 4;
        public static bool Is64BitProcess => IntPtr.Size == 8;
        public static bool IsNotWindows => !IsWindows;

        public static bool IsCaseInsensitiveOS => IsWindows || IsOSX || IsMacCatalyst;
        public static bool IsCaseSensitiveOS => !IsCaseInsensitiveOS;

        public static bool IsThreadingSupported => !IsBrowser;
        public static bool IsBinaryFormatterSupported => IsNotMobile;

        public static bool IsSpeedOptimized => !IsSizeOptimized;
        public static bool IsSizeOptimized => IsBrowser || IsAndroid || IsiOS || IstvOS || IsMacCatalyst;

        public static bool IsBrowserDomSupported => GetIsBrowserDomSupported();
        public static bool IsBrowserDomSupportedOrNotBrowser => IsNotBrowser || GetIsBrowserDomSupported();
        public static bool IsNotBrowserDomSupported => !IsBrowserDomSupported;
        public static bool LocalEchoServerIsNotAvailable => !LocalEchoServerIsAvailable;
        public static bool LocalEchoServerIsAvailable => IsBrowser;

        public static bool IsUsingLimitedCultures => !IsNotMobile;
        public static bool IsNotUsingLimitedCultures => IsNotMobile;

        public static bool IsLinqExpressionsBuiltWithIsInterpretingOnly => s_LinqExpressionsBuiltWithIsInterpretingOnly.Value;
        public static bool IsNotLinqExpressionsBuiltWithIsInterpretingOnly => !IsLinqExpressionsBuiltWithIsInterpretingOnly;
        private static readonly Lazy<bool> s_LinqExpressionsBuiltWithIsInterpretingOnly = new Lazy<bool>(GetLinqExpressionsBuiltWithIsInterpretingOnly);
        private static bool GetLinqExpressionsBuiltWithIsInterpretingOnly()
        {
            Type type = typeof(LambdaExpression);
            if (type != null)
            {
                // The "Accept" method is under FEATURE_COMPILE conditional so it should not exist
                MethodInfo methodInfo = type.GetMethod("Accept", BindingFlags.NonPublic | BindingFlags.Static);
                return methodInfo == null;
            }

            return false;
        }

        // Please make sure that you have the libgdiplus dependency installed.
        // For details, see https://docs.microsoft.com/dotnet/core/install/dependencies?pivots=os-macos&tabs=netcore31#libgdiplus
        public static bool IsDrawingSupported
        {
            get
            {
#if NETCOREAPP
                if (!IsWindows)
                {
                    if (IsMobile)
                    {
                        return false;
                    }
                    else if (IsOSX)
                    {
                        return NativeLibrary.TryLoad("libgdiplus.dylib", out _);
                    }
                    else
                    {
                       return NativeLibrary.TryLoad("libgdiplus.so", out _) || NativeLibrary.TryLoad("libgdiplus.so.0", out _);
                    }
                }
#endif

                return IsNotWindowsNanoServer && IsNotWindowsServerCore;

            }
        }

        public static bool IsAsyncFileIOSupported => !IsBrowser && !(IsWindows && IsMonoRuntime); // https://github.com/dotnet/runtime/issues/34582

        public static bool IsLineNumbersSupported => true;

        public static bool IsInContainer => GetIsInContainer();
        public static bool SupportsComInterop => IsWindows && IsNotMonoRuntime; // matches definitions in clr.featuredefines.props
        public static bool SupportsSsl3 => GetSsl3Support();
        public static bool SupportsSsl2 => IsWindows && !PlatformDetection.IsWindows10Version1607OrGreater;

#if NETCOREAPP
        public static bool IsReflectionEmitSupported => RuntimeFeature.IsDynamicCodeSupported;
#else
        public static bool IsReflectionEmitSupported => true;
#endif

        public static bool IsInvokingStaticConstructorsSupported => true;

        // System.Security.Cryptography.Xml.XmlDsigXsltTransform.GetOutput() relies on XslCompiledTransform which relies
        // heavily on Reflection.Emit
        public static bool IsXmlDsigXsltTransformSupported => !PlatformDetection.IsInAppContainer;

        public static bool IsPreciseGcSupported => !IsMonoRuntime;

        public static bool IsNotIntMaxValueArrayIndexSupported => s_largeArrayIsNotSupported.Value;

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

        public static bool IsDomainJoinedMachine => !Environment.MachineName.Equals(Environment.UserDomainName, StringComparison.OrdinalIgnoreCase);
        public static bool IsNotDomainJoinedMachine => !IsDomainJoinedMachine;

        public static bool IsOpenSslSupported => IsLinux || IsFreeBSD || Isillumos || IsSolaris;

        public static bool UsesAppleCrypto => IsOSX || IsMacCatalyst || IsiOS || IstvOS;
        public static bool UsesMobileAppleCrypto => IsMacCatalyst || IsiOS || IstvOS;

        // Changed to `true` when linking
        public static bool IsBuiltWithAggressiveTrimming => false;

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
                return OpenSslVersion.Major >= 1 && (OpenSslVersion.Minor >= 1 || OpenSslVersion.Build >= 2);
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

        public static bool SupportsTls10 => s_supportsTls10.Value;
        public static bool SupportsTls11 => s_supportsTls11.Value;
        public static bool SupportsTls12 => s_supportsTls12.Value;
        public static bool SupportsTls13 => s_supportsTls13.Value;

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
        public static bool IsIcuGlobalization => ICUVersion > new Version(0,0,0,0);
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

        private static readonly Lazy<bool> _net5CompatFileStream = new Lazy<bool>(() => GetStaticNonPublicBooleanPropertyValue("System.IO.Strategies.FileStreamHelpers", "UseNet5CompatStrategy"));

        public static bool IsNet5CompatFileStreamEnabled => _net5CompatFileStream.Value;

        public static bool IsNet5CompatFileStreamDisabled => !IsNet5CompatFileStreamEnabled;

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

        private static bool GetSsl3Support()
        {
            if (IsWindows)
            {
                string clientKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Client";
                string serverKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\SSL 3.0\Server";

                object client, server;
                try
                {
                    client = Registry.GetValue(clientKey, "Enabled", null);
                    server = Registry.GetValue(serverKey, "Enabled", null);
                }
                catch (SecurityException)
                {
                    // Insufficient permission, assume that we don't have SSL3 (since we aren't exactly sure)
                    return false;
                }

                if (client is int c && server is int s)
                {
                    return c == 1 && s == 1;
                }

                // Missing key. If we're pre-20H1 then assume SSL3 is enabled.
                // Otherwise, disabled. (See comments on https://github.com/dotnet/runtime/issues/1166)
                // Alternatively the returned values must have been some other types.
                return !IsWindows10Version2004OrGreater;
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
            // on Windows, macOS, and Android TLS1.0/1.1 are supported.
            if (IsWindows || IsOSXLike || IsAndroid)
            {
                return true;
            }

            return OpenSslGetTlsSupport(SslProtocols.Tls);
        }

        private static bool GetTls11Support()
        {
            // on Windows, macOS, and Android TLS1.0/1.1 are supported.
            // TLS 1.1 and 1.2 can work on Windows7 but it is not enabled by default.
            if (IsWindows)
            {
                return !IsWindows7;
            }
            else if (IsOSXLike || IsAndroid)
            {
                return true;
            }

            return OpenSslGetTlsSupport(SslProtocols.Tls11);
        }

        private static bool GetTls12Support()
        {
            // TLS 1.1 and 1.2 can work on Windows7 but it is not enabled by default.
            return !IsWindows7;
        }

        private static bool GetTls13Support()
        {
            if (IsWindows)
            {
                if (!IsWindows10Version2004OrGreater)
                {
                    return false;
                }

                string clientKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Client";
                string serverKey = @"HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\SecurityProviders\SCHANNEL\Protocols\TLS 1.3\Server";

                object client, server;
                try
                {
                    client = Registry.GetValue(clientKey, "Enabled", null);
                    server = Registry.GetValue(serverKey, "Enabled", null);
                    if (client is int c && server is int s)
                    {
                        return c == 1 && s == 1;
                    }
                }
                catch { }
                // assume no if positive entry is missing on older Windows
                // Latest insider builds have TLS 1.3 enabled by default.
                // The build number is approximation.
                return IsWindows10Version2004Build19573OrGreater;
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
                return OpenSslVersion >= new Version(1,1,1);
            }

            return false;
        }

        private static bool GetIsRunningOnMonoInterpreter()
        {
#if NETCOREAPP
            if (IsBrowser)
                return RuntimeFeature.IsDynamicCodeSupported;
#endif
            // This is a temporary solution because mono does not support interpreter detection
            // within the runtime.
            var val = Environment.GetEnvironmentVariable("MONO_ENV_OPTIONS");
            return (val != null && val.Contains("--interpreter"));
        }

        private static bool GetIsBrowserDomSupported()
        {
            if (!IsBrowser)
                return false;

            var val = Environment.GetEnvironmentVariable("IsBrowserDomSupported");
            return (val != null && val == "true");
        }
    }
}
