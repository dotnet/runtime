// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ILCompiler;
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
        private object _keepAlive; // Keeps callback delegates alive
        private InstructionSetSupport _instructionSetSupport;

        public static void Initialize(IEnumerable<CorJitFlag> jitFlags, IEnumerable<KeyValuePair<string, string>> parameters, InstructionSetSupport instructionSetSupport, string jitPath = null)
        {
            var config = new JitConfigProvider(jitFlags, parameters, instructionSetSupport);

            // Make sure we didn't try to initialize two instances of JIT configuration.
            // RyuJIT doesn't support multiple hosts in a single process.
            if (Interlocked.CompareExchange(ref s_instance, config, null) != null)
                throw new InvalidOperationException();

#if READYTORUN
            if (jitPath != null)
            {
                NativeLibrary.SetDllImportResolver(typeof(CorInfoImpl).Assembly, (libName, assembly, searchPath) =>
                {
                    IntPtr libHandle = IntPtr.Zero;
                    if (libName == CorInfoImpl.JitLibrary)
                    {
                        libHandle = NativeLibrary.Load(jitPath, assembly, searchPath);
                    }
                    return libHandle;
                });
            }
#else
            Debug.Assert(jitPath == null);
#endif

            CorInfoImpl.Startup();
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
        public JitConfigProvider(IEnumerable<CorJitFlag> jitFlags, IEnumerable<KeyValuePair<string, string>> parameters, InstructionSetSupport instructionSetSupport)
        {
            ArrayBuilder<CorJitFlag> jitFlagBuilder = new ArrayBuilder<CorJitFlag>();
            foreach (CorJitFlag jitFlag in jitFlags)
            {
                jitFlagBuilder.Add(jitFlag);
            }

            if (instructionSetSupport.IsInstructionSetSupported("Sse2") || instructionSetSupport.IsInstructionSetSupported("AdvSimd"))
            {
                jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_FEATURE_SIMD);
            }

            if (instructionSetSupport.Architecture == TypeSystem.TargetArchitecture.X64 || instructionSetSupport.Architecture == TypeSystem.TargetArchitecture.X86)
            {
                if (instructionSetSupport.IsInstructionSetSupported("Sse3"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_SSE3);
                if (instructionSetSupport.IsInstructionSetSupported("Ssse3"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_SSSE3);
                if (instructionSetSupport.IsInstructionSetSupported("Sse41"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_SSE41);
                if (instructionSetSupport.IsInstructionSetSupported("Sse42"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_SSE42);
                if (instructionSetSupport.IsInstructionSetSupported("Aes"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_AES);
                if (instructionSetSupport.IsInstructionSetSupported("Bmi1"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_BMI1);
                if (instructionSetSupport.IsInstructionSetSupported("Bmi2"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_BMI2);
                if (instructionSetSupport.IsInstructionSetSupported("Fma"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_FMA);
                if (instructionSetSupport.IsInstructionSetSupported("Lzcnt"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_LZCNT);
                if (instructionSetSupport.IsInstructionSetSupported("Pclmulqdq"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_PCLMULQDQ);
                if (instructionSetSupport.IsInstructionSetSupported("Popcnt"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_USE_POPCNT);
            }

            if (instructionSetSupport.Architecture == TypeSystem.TargetArchitecture.ARM64)
            {
                if (instructionSetSupport.IsInstructionSetSupported("ArmBase"))
                {
                    // No flag to enable this
                }

                if (instructionSetSupport.IsInstructionSetSupported("AdvSimd"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_HAS_ARM64_SIMD);
                if (instructionSetSupport.IsInstructionSetSupported("Aes"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_HAS_ARM64_AES);
                if (instructionSetSupport.IsInstructionSetSupported("Crc32"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_HAS_ARM64_CRC32);
                if (instructionSetSupport.IsInstructionSetSupported("Sha1"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_HAS_ARM64_SHA1);
                if (instructionSetSupport.IsInstructionSetSupported("Sha256"))
                    jitFlagBuilder.Add(CorJitFlag.CORJIT_FLAG_HAS_ARM64_SHA256);
            }

            _jitFlags = jitFlagBuilder.ToArray();
            _instructionSetSupport = instructionSetSupport;

            foreach (var param in parameters)
            {
                _config[param.Key] = param.Value;
            }

            UnmanagedInstance = CreateUnmanagedInstance();
        }

        public InstructionSetSupport InstructionSetSupport => _instructionSetSupport;

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
                Int32.TryParse(stringValue, NumberStyles.AllowHexSpecifier, null, out intValue))
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

            return String.Empty;
        }

        #region Unmanaged instance

        private unsafe IntPtr CreateUnmanagedInstance()
        {
            // TODO: this potentially leaks memory, but since we only expect to have one per compilation,
            // it shouldn't matter...

            const int numCallbacks = 2;

            IntPtr* callbacks = (IntPtr*)Marshal.AllocCoTaskMem(sizeof(IntPtr) * numCallbacks);
            object[] delegates = new object[numCallbacks];

            var d0 = new __getIntConfigValue(getIntConfigValue);
            callbacks[0] = Marshal.GetFunctionPointerForDelegate(d0);
            delegates[0] = d0;

            var d1 = new __getStringConfigValue(getStringConfigValue);
            callbacks[1] = Marshal.GetFunctionPointerForDelegate(d1);
            delegates[1] = d1;

            _keepAlive = delegates;
            IntPtr instance = Marshal.AllocCoTaskMem(sizeof(IntPtr));
            *(IntPtr**)instance = callbacks;

            return instance;
        }

        [UnmanagedFunctionPointer(default(CallingConvention))]
        private unsafe delegate int __getIntConfigValue(IntPtr thisHandle, [MarshalAs(UnmanagedType.LPWStr)] string name, int defaultValue);
        private unsafe int getIntConfigValue(IntPtr thisHandle, string name, int defaultValue)
        {
            return GetIntConfigValue(name, defaultValue);
        }

        [UnmanagedFunctionPointer(default(CallingConvention))]
        private unsafe delegate int __getStringConfigValue(IntPtr thisHandle, [MarshalAs(UnmanagedType.LPWStr)] string name, char* retBuffer, int retBufferLength);
        private unsafe int getStringConfigValue(IntPtr thisHandle, string name, char* retBuffer, int retBufferLength)
        {
            string result = GetStringConfigValue(name);

            for (int i = 0; i < Math.Min(retBufferLength, result.Length); i++)
                retBuffer[i] = result[i];

            return result.Length;
        }

        #endregion
    }
}
