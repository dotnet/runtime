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
        public static bool IsArm64Process => RuntimeInformation.ProcessArchitecture == Architecture.Arm64;

        public static bool IsWindows => OperatingSystem.IsWindows();

        public static bool IsBuiltInComEnabled => IsWindows && !Utilities.IsCoreClrInterpreter
                                            && (AppContext.TryGetSwitch("System.Runtime.InteropServices.BuiltInComInterop.IsSupported", out bool isEnabled)
                                                ? isEnabled
                                                : true);

        public static bool IsICorProfilerEnabled => !Utilities.IsNativeAot && !IsMonoRuntime;
        public static bool IsICorProfilerEnterLeaveHooksEnabled => IsICorProfilerEnabled && !Utilities.IsCoreClrInterpreter;

        public static bool IsRareEnumsSupported => !Utilities.IsNativeAot;

        public static bool IsCollectibleAssembliesSupported => !Utilities.IsNativeAot;

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

        public static bool IsNonZeroLowerBoundArrayNotSupported => !IsNonZeroLowerBoundArraySupported;

        public static bool IsTypeEquivalenceSupported => IsWindows && !Utilities.IsNativeAot && !Utilities.IsMonoRuntime;
        public static bool IsVarArgSupported => IsWindows && !Utilities.IsNativeAot && !Utilities.IsMonoRuntime && !Utilities.IsCoreClrInterpreter;

        public static bool IsExceptionInteropSupported => IsWindows && !Utilities.IsNativeAot && !Utilities.IsMonoRuntime && !Utilities.IsCoreClrInterpreter;

        public static bool IsMonoRuntime => Type.GetType("Mono.RuntimeStructs") != null;

        static string _variant = Environment.GetEnvironmentVariable("DOTNET_RUNTIME_VARIANT");

        public static bool IsMonoLLVMAOT => _variant == "llvmaot";
        public static bool IsMonoLLVMFULLAOT => _variant == "llvmfullaot";
        public static bool IsMonoMINIFULLAOT => _variant == "minifullaot";
        public static bool IsMonoFULLAOT => IsMonoLLVMFULLAOT || IsMonoMINIFULLAOT;
        public static bool IsMonoInterpreter => _variant == "monointerpreter";

        // These platforms have not had their infrastructure updated to support native test assets.
        public static bool PlatformDoesNotSupportNativeTestAssets =>
            OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsAndroid() || OperatingSystem.IsBrowser() || OperatingSystem.IsWasi();
        public static bool IsAppleMobile => OperatingSystem.IsIOS() || OperatingSystem.IsTvOS() || OperatingSystem.IsMacCatalyst();

        // wasm properties
        public static bool IsBrowser => RuntimeInformation.IsOSPlatform(OSPlatform.Create("BROWSER"));
        public static bool IsWasi => RuntimeInformation.IsOSPlatform(OSPlatform.Create("WASI"));
        public static bool IsWasm => IsBrowser || IsWasi;
        public static bool IsNotBrowser => !IsBrowser;
        public static bool IsNotWasi => !IsWasi;
        public static bool IsThreadingSupported => (!IsWasi && !IsBrowser) || IsWasmThreadingSupported;
        public static bool IsWasmThreadingSupported => IsBrowser && IsEnvironmentVariableTrue("IsBrowserThreadingSupported");
        public static bool IsNotWasmThreadingSupported => !IsWasmThreadingSupported;

        private static bool IsEnvironmentVariableTrue(string variableName)
        {
            if (!IsBrowser)
                return false;

            return Environment.GetEnvironmentVariable(variableName) is "true";
        }
    }
}
