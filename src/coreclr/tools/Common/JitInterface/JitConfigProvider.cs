// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using Internal.TypeSystem;
using NumberStyles = System.Globalization.NumberStyles;

namespace Internal.JitInterface
{
    public sealed class JitConfigProvider
    {
        // Jit configuration is static because RyuJIT doesn't support multiple hosts within the same process.
        private static JitConfigProvider s_instance;
        public static JitConfigProvider Instance
        {
            get
            {
                Debug.Assert(s_instance != null);
                return s_instance;
            }
        }

        private CorJitFlag[] _jitFlags;
        private Dictionary<string, string> _config = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public static void Initialize(
            TargetDetails target,
            IEnumerable<CorJitFlag> jitFlags,
            IEnumerable<KeyValuePair<string, string>> parameters,
            string jitPath = null)
        {
            var config = new JitConfigProvider(jitFlags, parameters);

            // Make sure we didn't try to initialize two instances of JIT configuration.
            // RyuJIT doesn't support multiple hosts in a single process.
            if (Interlocked.CompareExchange(ref s_instance, config, null) != null)
                throw new InvalidOperationException();

            NativeLibrary.SetDllImportResolver(typeof(CorInfoImpl).Assembly, (libName, assembly, searchPath) =>
            {
                IntPtr libHandle = IntPtr.Zero;
                if (libName == CorInfoImpl.JitLibrary)
                {
                    if (!string.IsNullOrEmpty(jitPath))
                    {
                        libHandle = NativeLibrary.Load(jitPath);
                    }
                    else
                    {
                        libHandle = NativeLibrary.Load("clrjit_" + GetTargetSpec(target), assembly, searchPath);
                    }
                }
                if (libName == CorInfoImpl.JitSupportLibrary)
                {
                    libHandle = NativeLibrary.Load("jitinterface_" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant(), assembly, searchPath);
                }
                return libHandle;
            });

            CorInfoImpl.Startup(CorInfoImpl.TargetToOs(target));
        }

        public IntPtr UnmanagedInstance
        {
            get;
        }

        public IEnumerable<CorJitFlag> Flags => _jitFlags;

        /// <summary>
        /// Creates a new instance of <see cref="JitConfigProvider"/>.
        /// </summary>
        /// <param name="jitFlags">A collection of JIT compiler flags.</param>
        /// <param name="parameters">A collection of parameter name/value pairs.</param>
        public JitConfigProvider(IEnumerable<CorJitFlag> jitFlags, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            ArrayBuilder<CorJitFlag> jitFlagBuilder = default(ArrayBuilder<CorJitFlag>);
            foreach (CorJitFlag jitFlag in jitFlags)
            {
                jitFlagBuilder.Add(jitFlag);
            }

            _jitFlags = jitFlagBuilder.ToArray();

            foreach (var param in parameters)
            {
                _config[param.Key] = param.Value;
            }

            UnmanagedInstance = CreateUnmanagedInstance();
        }

        public bool HasFlag(CorJitFlag flag)
        {
            foreach (CorJitFlag definedFlag in _jitFlags)
                if (definedFlag == flag)
                    return true;

            return false;
        }

        public int GetIntConfigValue(string name, int defaultValue)
        {
            // Note: Parse the string as hex
            string stringValue;
            int intValue;
            if (_config.TryGetValue(name, out stringValue) &&
                int.TryParse(stringValue, NumberStyles.AllowHexSpecifier, null, out intValue))
            {
                return intValue;
            }

            return defaultValue;
        }

        public string GetStringConfigValue(string name)
        {
            string stringValue;
            if (_config.TryGetValue(name, out stringValue))
            {
                return stringValue;
            }

            return string.Empty;
        }

        private static string GetTargetSpec(TargetDetails target)
        {
            string targetArchComponent = target.Architecture switch
            {
                TargetArchitecture.X86 => "x86",
                TargetArchitecture.X64 => "x64",
                TargetArchitecture.ARM => "arm",
                TargetArchitecture.ARM64 => "arm64",
                TargetArchitecture.LoongArch64 => "loongarch64",
                TargetArchitecture.RiscV64 => "riscv64",
                _ => throw new NotImplementedException(target.Architecture.ToString())
            };

            string targetOSComponent;
            if (target.Architecture is TargetArchitecture.ARM64 or TargetArchitecture.ARM)
            {
                targetOSComponent = "universal";
            }
            else
            {
                targetOSComponent = target.OperatingSystem == TargetOS.Windows ? "win" : "unix";
            }

            return targetOSComponent + '_' + targetArchComponent + "_" + RuntimeInformation.ProcessArchitecture.ToString().ToLowerInvariant();
        }

        #region Unmanaged instance

        private static unsafe IntPtr CreateUnmanagedInstance()
        {
            // This potentially leaks memory, but since we only expect to have one per compilation,
            // it shouldn't matter...

            const int numCallbacks = 2;

            void** callbacks = (void**)NativeMemory.Alloc((nuint)(sizeof(void*) * numCallbacks));

            callbacks[0] = (delegate* unmanaged<IntPtr, byte*, int, int>)&getIntConfigValue;
            callbacks[1] = (delegate* unmanaged<IntPtr, byte*, byte*, int, int>)&getStringConfigValue;

            IntPtr instance = (IntPtr)NativeMemory.Alloc((nuint)sizeof(IntPtr));
            *(IntPtr*)instance = (IntPtr)callbacks;

            return instance;
        }

        [UnmanagedCallersOnly]
        private static unsafe int getIntConfigValue(IntPtr thisHandle, byte* name, int defaultValue)
        {
            return s_instance.GetIntConfigValue(Marshal.PtrToStringUTF8((IntPtr)name), defaultValue);
        }

        [UnmanagedCallersOnly]
        private static unsafe int getStringConfigValue(IntPtr thisHandle, byte* name, byte* retBuffer, int retBufferLength)
        {
            string result = s_instance.GetStringConfigValue(Marshal.PtrToStringUTF8((IntPtr)name));

            if (result == "")
            {
                return 0;
            }

            nuint requiredBufferSize;
            CorInfoImpl.PrintFromUtf16(result, retBuffer, (nuint)retBufferLength, &requiredBufferSize);
            return (int)requiredBufferSize;
        }

        #endregion
    }
}
