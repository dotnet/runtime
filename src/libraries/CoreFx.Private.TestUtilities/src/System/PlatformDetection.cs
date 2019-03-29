// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Runtime.CompilerServices;
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

        public static bool HasWindowsShell => IsWindows && IsNotWindowsServerCore && IsNotWindowsNanoServer && IsNotWindowsIoTCore;
        public static bool IsUap => IsInAppContainer || IsNetNative;
        public static bool IsFullFramework => RuntimeInformation.FrameworkDescription.StartsWith(".NET Framework", StringComparison.OrdinalIgnoreCase);
        public static bool IsNetNative => RuntimeInformation.FrameworkDescription.StartsWith(".NET Native", StringComparison.OrdinalIgnoreCase);
        public static bool IsNetCore => RuntimeInformation.FrameworkDescription.StartsWith(".NET Core", StringComparison.OrdinalIgnoreCase);
        public static bool IsOSX => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        public static bool IsFreeBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("FREEBSD"));
        public static bool IsNetBSD => RuntimeInformation.IsOSPlatform(OSPlatform.Create("NETBSD"));
        public static bool IsNotWindows8x => !IsWindows8x;
        public static bool IsNotWindowsNanoServer => !IsWindowsNanoServer;
        public static bool IsNotWindowsServerCore => !IsWindowsServerCore;
        public static bool IsNotWindowsIoTCore => !IsWindowsIoTCore;
        public static bool IsNotWindowsHomeEdition => !IsWindowsHomeEdition;
        public static bool IsArmProcess => RuntimeInformation.ProcessArchitecture == Architecture.Arm;
        public static bool IsNotArmProcess => !IsArmProcess;
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;
        public static bool IsNotArm64Process => !IsArm64Process;
        public static bool IsArmOrArm64Process => IsArmProcess || IsArm64Process;
        public static bool IsNotArmNorArm64Process => !IsArmOrArm64Process;
        public static bool IsArgIteratorSupported => IsWindows && IsNotArmProcess;
        public static bool IsArgIteratorNotSupported => !IsArgIteratorSupported;
        public static bool Is32BitProcess => IntPtr.Size == 4;

        public static bool IsNotInAppContainer => !IsInAppContainer;
        public static bool IsWinRTSupported => IsWindows && !IsWindows7;
        public static bool IsNotWinRTSupported => !IsWinRTSupported;
        public static bool IsNotMacOsHighSierraOrHigher => !IsMacOsHighSierraOrHigher;

        public static bool IsDomainJoinedMachine => !Environment.MachineName.Equals(Environment.UserDomainName, StringComparison.OrdinalIgnoreCase);

        public static bool IsNotNetNative => !IsNetNative;

        // Windows - Schannel supports alpn from win8.1/2012 R2 and higher.
        // Linux - OpenSsl supports alpn from openssl 1.0.2 and higher.
        // OSX - SecureTransport doesn't expose alpn APIs. #30492
        public static bool SupportsAlpn => (IsWindows && !IsWindows7) ||
            ((!IsOSX && !IsWindows) &&
            (OpenSslVersion.Major >= 1 && (OpenSslVersion.Minor >= 1 || OpenSslVersion.Build >= 2)));
        public static bool SupportsClientAlpn => SupportsAlpn ||
            (RuntimeInformation.IsOSPlatform(OSPlatform.OSX) && PlatformDetection.OSXVersion > new Version(10, 12));

        // Officially, .NET Native only supports processes running in an AppContainer. However, the majority of tests still work fine
        // in a normal Win32 process and we often do so as running in an AppContainer imposes a substantial tax in debuggability
        // and investigatability. This predicate is used in ConditionalFacts to disable the specific tests that really need to be
        // running in AppContainer when running on .NetNative.
        public static bool IsNotNetNativeRunningAsConsoleApp => !(IsNetNative && !IsInAppContainer);

        private static Lazy<bool> m_isWindowsSubsystemForLinux = new Lazy<bool>(GetIsWindowsSubsystemForLinux);

        public static bool IsWindowsSubsystemForLinux => m_isWindowsSubsystemForLinux.Value;
        public static bool IsNotWindowsSubsystemForLinux => !IsWindowsSubsystemForLinux;

        private static bool GetIsWindowsSubsystemForLinux()
        {
            // https://github.com/Microsoft/BashOnWindows/issues/423#issuecomment-221627364

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                const string versionFile = "/proc/version";
                if (File.Exists(versionFile))
                {
                    string s = File.ReadAllText(versionFile);

                    if (s.Contains("Microsoft") || s.Contains("WSL"))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

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

        public static bool IsNotIntMaxValueArrayIndexSupported => s_largeArrayIsNotSupported.Value;

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

        private static volatile Tuple<bool> s_lazyNonZeroLowerBoundArraySupported;

        public static bool IsReflectionEmitSupported = !PlatformDetection.IsNetNative;

        // Tracked in: https://github.com/dotnet/corert/issues/3643 in case we change our mind about this.
        public static bool IsInvokingStaticConstructorsSupported => !PlatformDetection.IsNetNative;

        // System.Security.Cryptography.Xml.XmlDsigXsltTransform.GetOutput() relies on XslCompiledTransform which relies
        // heavily on Reflection.Emit
        public static bool IsXmlDsigXsltTransformSupported => !PlatformDetection.IsUap;
    }
}
