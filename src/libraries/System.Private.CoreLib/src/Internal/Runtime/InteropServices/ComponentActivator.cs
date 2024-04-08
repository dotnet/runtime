// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;
using System.Runtime.Versioning;

namespace Internal.Runtime.InteropServices
{
    internal static partial class ComponentActivator
    {
        private const string TrimIncompatibleWarningMessage = "Native hosting is not trim compatible and this warning will be seen if trimming is enabled.";
        private const string NativeAOTIncompatibleWarningMessage = "The native code for the method requested might not be available at runtime.";

        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        private static readonly Dictionary<string, IsolatedComponentLoadContext> s_assemblyLoadContexts = new Dictionary<string, IsolatedComponentLoadContext>(StringComparer.InvariantCulture);
        private static readonly Dictionary<IntPtr, Delegate> s_delegates = new Dictionary<IntPtr, Delegate>();
        private static readonly HashSet<string> s_loadedInDefaultContext = new HashSet<string>(StringComparer.InvariantCulture);

        // Use a value defined in https://github.com/dotnet/runtime/blob/main/docs/design/features/host-error-codes.md
        // To indicate the specific error when IsSupported is false
        private const int HostFeatureDisabled = unchecked((int)0x800080a7);

        private static bool IsSupported { get; } = InitializeIsSupported();

        private static bool InitializeIsSupported() => AppContext.TryGetSwitch("System.Runtime.InteropServices.EnableConsumingManagedCodeFromNativeHosting", out bool isSupported) ? isSupported : true;

        public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);

        private static string MarshalToString(IntPtr arg, string argName)
        {
            string? result = Marshal.PtrToStringAuto(arg);
            ArgumentNullException.ThrowIfNull(result, argName);
            return result;
        }

        /// <summary>
        /// Native hosting entry point for creating a native delegate
        /// </summary>
        /// <param name="assemblyPathNative">Fully qualified path to assembly</param>
        /// <param name="typeNameNative">Assembly qualified type name</param>
        /// <param name="methodNameNative">Public static method name compatible with delegateType</param>
        /// <param name="delegateTypeNative">Assembly qualified delegate type name</param>
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        /// <param name="functionHandle">Pointer where to store the function pointer result</param>
        [RequiresDynamicCode(NativeAOTIncompatibleWarningMessage)]
        [RequiresUnreferencedCode(TrimIncompatibleWarningMessage, Url = "https://aka.ms/dotnet-illink/nativehost")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        [UnmanagedCallersOnly]
        public static unsafe int LoadAssemblyAndGetFunctionPointer(IntPtr assemblyPathNative,
                                                                   IntPtr typeNameNative,
                                                                   IntPtr methodNameNative,
                                                                   IntPtr delegateTypeNative,
                                                                   IntPtr reserved,
                                                                   IntPtr functionHandle)
        {
            if (!IsSupported)
                return HostFeatureDisabled;

            try
            {
                // Validate all parameters first.
                string assemblyPath = MarshalToString(assemblyPathNative, nameof(assemblyPathNative));
                string typeName = MarshalToString(typeNameNative, nameof(typeNameNative));
                string methodName = MarshalToString(methodNameNative, nameof(methodNameNative));

                ArgumentOutOfRangeException.ThrowIfNotEqual(reserved, IntPtr.Zero);
                ArgumentNullException.ThrowIfNull(functionHandle);

                // Set up the AssemblyLoadContext for this delegate.
                AssemblyLoadContext alc = GetIsolatedComponentLoadContext(assemblyPath);

                // Create the function pointer.
                *(IntPtr*)functionHandle = InternalGetFunctionPointer(alc, typeName, methodName, delegateTypeNative);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;
        }

        /// <summary>
        /// Native hosting entry point for loading an assembly from a path
        /// </summary>
        /// <param name="assemblyPathNative">Fully qualified path to assembly</param>
        /// <param name="loadContext">Extensibility parameter (currently unused)</param>
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        [RequiresDynamicCode(NativeAOTIncompatibleWarningMessage)]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        [UnmanagedCallersOnly]
        public static unsafe int LoadAssembly(IntPtr assemblyPathNative, IntPtr loadContext, IntPtr reserved)
        {
            if (!IsSupported)
                return HostFeatureDisabled;

            try
            {
                string assemblyPath = MarshalToString(assemblyPathNative, nameof(assemblyPathNative));

                ArgumentOutOfRangeException.ThrowIfNotEqual(loadContext, IntPtr.Zero);
                ArgumentOutOfRangeException.ThrowIfNotEqual(reserved, IntPtr.Zero);

                LoadAssemblyLocal(assemblyPath);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The same feature switch applies to GetFunctionPointer and this function. We rely on the warning from GetFunctionPointer.")]
            static void LoadAssemblyLocal(string assemblyPath) => LoadAssemblyImpl(assemblyPath);
        }

        [RequiresUnreferencedCode(TrimIncompatibleWarningMessage, Url = "https://aka.ms/dotnet-illink/nativehost")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        private static void LoadAssemblyImpl(string assemblyPath)
        {
            lock (s_loadedInDefaultContext)
            {
                if (s_loadedInDefaultContext.Contains(assemblyPath))
                    return;

                var resolver = new AssemblyDependencyResolver(assemblyPath);
                AssemblyLoadContext.Default.Resolving +=
                    (context, assemblyName) =>
                    {
                        string? assemblyPath = resolver.ResolveAssemblyToPath(assemblyName);
                        return assemblyPath != null
                            ? context.LoadFromAssemblyPath(assemblyPath)
                            : null;
                    };

                AssemblyLoadContext.Default.LoadFromAssemblyPath(assemblyPath);
                s_loadedInDefaultContext.Add(assemblyPath);
            }
        }

        /// <summary>
        /// Native hosting entry point for loading an assembly from a byte array
        /// </summary>
        /// <param name="assembly">Bytes of the assembly to load</param>
        /// <param name="assemblyByteLength">Byte length of the assembly to load</param>
        /// <param name="symbols">Optional. Bytes of the symbols for the assembly</param>
        /// <param name="symbolsByteLength">Optional. Byte length of the symbols for the assembly</param>
        /// <param name="loadContext">Extensibility parameter (currently unused)</param>
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        [RequiresDynamicCode(NativeAOTIncompatibleWarningMessage)]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("maccatalyst")]
        [UnsupportedOSPlatform("tvos")]
        [UnmanagedCallersOnly]
        public static unsafe int LoadAssemblyBytes(byte* assembly, nint assemblyByteLength, byte* symbols, nint symbolsByteLength, IntPtr loadContext, IntPtr reserved)
        {
            if (!IsSupported)
                return HostFeatureDisabled;

            try
            {
                ArgumentNullException.ThrowIfNull(assembly);
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(assemblyByteLength);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(assemblyByteLength, int.MaxValue);
                ArgumentOutOfRangeException.ThrowIfNotEqual(loadContext, IntPtr.Zero);
                ArgumentOutOfRangeException.ThrowIfNotEqual(reserved, IntPtr.Zero);

                ReadOnlySpan<byte> assemblySpan = new ReadOnlySpan<byte>(assembly, (int)assemblyByteLength);
                ReadOnlySpan<byte> symbolsSpan = default;
                if (symbols != null && symbolsByteLength > 0)
                {
                    symbolsSpan = new ReadOnlySpan<byte>(symbols, (int)symbolsByteLength);
                }

                LoadAssemblyBytesLocal(assemblySpan, symbolsSpan);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;

            [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026:RequiresUnreferencedCode",
                Justification = "The same feature switch applies to GetFunctionPointer and this function. We rely on the warning from GetFunctionPointer.")]
            static void LoadAssemblyBytesLocal(ReadOnlySpan<byte> assemblyBytes, ReadOnlySpan<byte> symbolsBytes) => AssemblyLoadContext.Default.InternalLoad(assemblyBytes, symbolsBytes);
        }

        /// <summary>
        /// Native hosting entry point for creating a native delegate
        /// </summary>
        /// <param name="typeNameNative">Assembly qualified type name</param>
        /// <param name="methodNameNative">Public static method name compatible with delegateType</param>
        /// <param name="delegateTypeNative">Assembly qualified delegate type name</param>
        /// <param name="loadContext">Extensibility parameter (currently unused)</param>
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        /// <param name="functionHandle">Pointer where to store the function pointer result</param>
        [RequiresDynamicCode(NativeAOTIncompatibleWarningMessage)]
        [UnmanagedCallersOnly]
        public static unsafe int GetFunctionPointer(IntPtr typeNameNative,
                                                    IntPtr methodNameNative,
                                                    IntPtr delegateTypeNative,
                                                    IntPtr loadContext,
                                                    IntPtr reserved,
                                                    IntPtr functionHandle)
        {
            if (!IsSupported)
            {
#if CORECLR
                try
                {
                    OnDisabledGetFunctionPointerCall(typeNameNative, methodNameNative);
                }
                catch (Exception e)
                {
                    // The callback can intentionally throw NotSupportedException to provide errors to consumers,
                    // so we let that one through. Any other exceptions must not be leaked out.
                    if (e is NotSupportedException)
                        throw;

                    return e.HResult;
                }
#endif
                return HostFeatureDisabled;
            }

            try
            {
                // Validate all parameters first.
                string typeName = MarshalToString(typeNameNative, nameof(typeNameNative));
                string methodName = MarshalToString(methodNameNative, nameof(methodNameNative));

                ArgumentOutOfRangeException.ThrowIfNotEqual(loadContext, IntPtr.Zero);
                ArgumentOutOfRangeException.ThrowIfNotEqual(reserved, IntPtr.Zero);
                ArgumentNullException.ThrowIfNull(functionHandle);

#pragma warning disable IL2026 // suppressed in ILLink.Suppressions.LibraryBuild.xml
                // Create the function pointer.
                *(IntPtr*)functionHandle = InternalGetFunctionPointer(AssemblyLoadContext.Default, typeName, methodName, delegateTypeNative);
#pragma warning restore IL2026
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;
        }

        [RequiresUnreferencedCode(TrimIncompatibleWarningMessage, Url = "https://aka.ms/dotnet-illink/nativehost")]
        [UnsupportedOSPlatform("android")]
        [UnsupportedOSPlatform("browser")]
        [UnsupportedOSPlatform("ios")]
        [UnsupportedOSPlatform("tvos")]
        private static IsolatedComponentLoadContext GetIsolatedComponentLoadContext(string assemblyPath)
        {
            IsolatedComponentLoadContext? alc;

            lock (s_assemblyLoadContexts)
            {
                if (!s_assemblyLoadContexts.TryGetValue(assemblyPath, out alc))
                {
                    alc = new IsolatedComponentLoadContext(assemblyPath);
                    s_assemblyLoadContexts.Add(assemblyPath, alc);
                }
            }

            return alc;
        }

        [RequiresDynamicCode(NativeAOTIncompatibleWarningMessage)]
        [RequiresUnreferencedCode(TrimIncompatibleWarningMessage, Url = "https://aka.ms/dotnet-illink/nativehost")]
        private static IntPtr InternalGetFunctionPointer(AssemblyLoadContext alc,
                                                         string typeName,
                                                         string methodName,
                                                         IntPtr delegateTypeNative)
        {
            // Create a resolver callback for types.
            Func<AssemblyName, Assembly> resolver = alc.LoadFromAssemblyName;

            // Determine the signature of the type. There are 3 possibilities:
            //  * No delegate type was supplied - use the default (i.e. ComponentEntryPoint).
            //  * A sentinel value was supplied - the function is marked UnmanagedCallersOnly. This means
            //      a function pointer can be returned without creating a delegate.
            //  * A delegate type was supplied - Load the type and create a delegate for that method.
            Type? delegateType;
            if (delegateTypeNative == IntPtr.Zero)
            {
                delegateType = typeof(ComponentEntryPoint);
            }
            else if (delegateTypeNative == (IntPtr)(-1))
            {
                delegateType = null;
            }
            else
            {
                string delegateTypeName = MarshalToString(delegateTypeNative, nameof(delegateTypeNative));
                delegateType = Type.GetType(delegateTypeName, resolver, null, throwOnError: true)!;
            }

            // Get the requested type.
            Type type = Type.GetType(typeName, resolver, null, throwOnError: true)!;

            IntPtr functionPtr;
            if (delegateType == null)
            {
                // Match search semantics of the CreateDelegate() function below.
                BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic;
                MethodInfo? methodInfo = type.GetMethod(methodName, bindingFlags);
                if (methodInfo == null)
                    throw new MissingMethodException(typeName, methodName);

                // Verify the function is properly marked.
                if (null == methodInfo.GetCustomAttribute<UnmanagedCallersOnlyAttribute>())
                    throw new InvalidOperationException(SR.InvalidOperation_FunctionMissingUnmanagedCallersOnly);

                functionPtr = methodInfo.MethodHandle.GetFunctionPointer();
            }
            else
            {
                Delegate d = Delegate.CreateDelegate(delegateType, type, methodName)!;

                functionPtr = Marshal.GetFunctionPointerForDelegate(d);

                lock (s_delegates)
                {
                    // Keep a reference to the delegate to prevent it from being garbage collected
                    s_delegates[functionPtr] = d;
                }
            }

            return functionPtr;
        }
    }
}
