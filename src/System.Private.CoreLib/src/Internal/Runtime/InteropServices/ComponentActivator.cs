// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices
{
    public static class ComponentActivator
    {
        private static readonly Dictionary<string, IsolatedComponentLoadContext> s_AssemblyLoadContexts;
        private static readonly Dictionary<IntPtr, Delegate> s_Delegates = new Dictionary<IntPtr, Delegate>();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
        public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);

        static ComponentActivator()
        {
            s_AssemblyLoadContexts = new Dictionary<string, IsolatedComponentLoadContext>(StringComparer.InvariantCulture);
        }

        private static string MarshalToString(IntPtr arg, string argName)
        {
            if (arg == IntPtr.Zero)
            {
                throw new ArgumentNullException(argName);
            }

#if PLATFORM_WINDOWS
            string? result = Marshal.PtrToStringUni(arg);
#else
            string? result = Marshal.PtrToStringUTF8(arg);
#endif

            if (result == null)
            {
                throw new ArgumentNullException(argName);
            }
            return result;
        }

        /// <summary>
        /// Native hosting entry point for creating a native delegate
        /// </summary>
        /// <param name="assemblyPathNative">Fully qualified path to assembly</param>
        /// <param name="typeNameNative">Assembly qualified type name</param>
        /// <param name="methodNameNative">Public static method name compatible with delegateType</param>
        /// <param name="delegateTypeNative">Assembly qualified delegate type name</param>
        /// <param name="flags">Extensibility flags (currently unused)</param>
        /// <param name="functionHandle">Pointer where to store the function pointer result</param>
        public static int CreateNativeDelegate(IntPtr assemblyPathNative,
                                               IntPtr typeNameNative,
                                               IntPtr methodNameNative,
                                               IntPtr delegateTypeNative,
                                               int flags,
                                               IntPtr functionHandle)
        {
            try
            {
                string assemblyPath = MarshalToString(assemblyPathNative, nameof(assemblyPathNative));
                string typeName     = MarshalToString(typeNameNative, nameof(typeNameNative));
                string methodName   = MarshalToString(methodNameNative, nameof(methodNameNative));

                string delegateType;
                if (delegateTypeNative == IntPtr.Zero)
                {
                    delegateType = typeof(ComponentEntryPoint).AssemblyQualifiedName!;
                }
                else
                {
                    delegateType = MarshalToString(delegateTypeNative, nameof(delegateTypeNative));
                }

                if (flags != 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(flags));
                }

                if (functionHandle == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(functionHandle));
                }

                Delegate d = CreateDelegate(assemblyPath, typeName, methodName, delegateType);

                IntPtr functionPtr = Marshal.GetFunctionPointerForDelegate(d);

                lock(s_Delegates)
                {
                    // Keep a reference to the delegate to prevent it from being garbage collected
                    s_Delegates[functionPtr] = d;
                }

                Marshal.WriteIntPtr(functionHandle, functionPtr);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;
        }

        private static Delegate CreateDelegate(string assemblyPath, string typeName, string methodName, string delegateTypeName)
        {
            // Throws
            IsolatedComponentLoadContext alc = GetIsolatedComponentLoadContext(assemblyPath);

            Func<AssemblyName,Assembly> resolver = name => alc.LoadFromAssemblyName(name);

            // Throws
            Type type = Type.GetType(typeName, resolver, null, throwOnError: true)!;

            // Throws
            Type delegateType = Type.GetType(delegateTypeName, resolver, null, throwOnError: true)!;

            // Throws
            return Delegate.CreateDelegate(delegateType, type, methodName)!;
        }

        private static IsolatedComponentLoadContext GetIsolatedComponentLoadContext(string assemblyPath)
        {
            IsolatedComponentLoadContext alc;

            lock (s_AssemblyLoadContexts)
            {
                if (!s_AssemblyLoadContexts.TryGetValue(assemblyPath, out alc))
                {
                    alc = new IsolatedComponentLoadContext(assemblyPath);
                    s_AssemblyLoadContexts.Add(assemblyPath, alc);
                }
            }

            return alc;
        }
    }
}
