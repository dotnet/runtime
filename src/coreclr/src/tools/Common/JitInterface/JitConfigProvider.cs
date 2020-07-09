// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using ILCompiler;
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
        private object _keepAlive; // Keeps callback delegates alive

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

#if READYTORUN
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
                        libHandle = NativeLibrary.Load("clrjit-" + GetTargetSpec(target), assembly, searchPath);
                    }
                }
                return libHandle;
            });
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
        public JitConfigProvider(IEnumerable<CorJitFlag> jitFlags, IEnumerable<KeyValuePair<string, string>> parameters)
        {
            ArrayBuilder<CorJitFlag> jitFlagBuilder = new ArrayBuilder<CorJitFlag>();
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

        private static string GetTargetSpec(TargetDetails target)
        {
            string targetOSComponent = (target.OperatingSystem == TargetOS.Windows ? "win" : "unix");
            string targetArchComponent = target.Architecture switch
            {
                TargetArchitecture.X86 => "x86",
                TargetArchitecture.X64 => "x64",
                TargetArchitecture.ARM => "arm",
                TargetArchitecture.ARM64 => "arm64",
                _ => throw new NotImplementedException(target.Architecture.ToString())
            };
            return targetOSComponent + '-' + targetArchComponent;
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
