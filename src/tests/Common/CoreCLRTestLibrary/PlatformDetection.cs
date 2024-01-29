// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace TestLibrary
{
    public static class PlatformDetection
    {
        public static bool Is32BitProcess => IntPtr.Size == 4;
        public static bool Is64BitProcess => IntPtr.Size == 8;

        public static bool IsX86Process => RuntimeInformation.ProcessArchitecture == Architecture.X86;
        public static bool IsNotX86Process => !IsX86Process;

        public static bool IsWindows => OperatingSystem.IsWindows();

        public static bool IsBuiltInComEnabled => IsWindows
                                            && (AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out bool isEnabled)
                                                ? isEnabled
                                                : true);

        public static bool IsRareEnumsSupported => !Utilities.IsNativeAot;

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

        static string _variant = Environment.GetEnvironmentVariable("DOTNET_RUNTIME_VARIANT");

        public static bool IsMonoLLVMAOT => _variant == "llvmaot";
        public static bool IsMonoLLVMFULLAOT => _variant == "llvmfullaot";
        public static bool IsMonoInterpreter => _variant == "monointerpreter";

        // These platforms have not had their infrastructure updated to support native test assets.
        public static bool PlatformDoesNotSupportNativeTestAssets =>
            OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsWatchOS() || OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();
    }
}
