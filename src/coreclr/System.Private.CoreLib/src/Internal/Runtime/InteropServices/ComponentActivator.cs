// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace Internal.Runtime.InteropServices
{
    public static class ComponentActivator
    {
        private static readonly Dictionary<string, IsolatedComponentLoadContext> s_assemblyLoadContexts = new Dictionary<string, IsolatedComponentLoadContext>(StringComparer.InvariantCulture);
        private static readonly Dictionary<IntPtr, Delegate> s_delegates = new Dictionary<IntPtr, Delegate>();

        public delegate int ComponentEntryPoint(IntPtr args, int sizeBytes);

        private static string MarshalToString(IntPtr arg, string argName)
        {
            string? result = Marshal.PtrToStringAuto(arg);
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
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        /// <param name="functionHandle">Pointer where to store the function pointer result</param>
        [UnmanagedCallersOnly]
        public static unsafe int LoadAssemblyAndGetFunctionPointer(IntPtr assemblyPathNative,
                                                                   IntPtr typeNameNative,
                                                                   IntPtr methodNameNative,
                                                                   IntPtr delegateTypeNative,
                                                                   IntPtr reserved,
                                                                   IntPtr functionHandle)
        {
            try
            {
                // Validate all parameters first.
                string assemblyPath = MarshalToString(assemblyPathNative, nameof(assemblyPathNative));
                string typeName = MarshalToString(typeNameNative, nameof(typeNameNative));
                string methodName = MarshalToString(methodNameNative, nameof(methodNameNative));

                if (reserved != IntPtr.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(reserved));
                }

                if (functionHandle == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(functionHandle));
                }

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
        /// Native hosting entry point for creating a native delegate
        /// </summary>
        /// <param name="typeNameNative">Assembly qualified type name</param>
        /// <param name="methodNameNative">Public static method name compatible with delegateType</param>
        /// <param name="delegateTypeNative">Assembly qualified delegate type name</param>
        /// <param name="loadContext">Extensibility parameter (currently unused)</param>
        /// <param name="reserved">Extensibility parameter (currently unused)</param>
        /// <param name="functionHandle">Pointer where to store the function pointer result</param>
        [UnmanagedCallersOnly]
        public static unsafe int GetFunctionPointer(IntPtr typeNameNative,
                                                    IntPtr methodNameNative,
                                                    IntPtr delegateTypeNative,
                                                    IntPtr loadContext,
                                                    IntPtr reserved,
                                                    IntPtr functionHandle)
        {
            try
            {
                // Validate all parameters first.
                string typeName = MarshalToString(typeNameNative, nameof(typeNameNative));
                string methodName = MarshalToString(methodNameNative, nameof(methodNameNative));

                if (loadContext != IntPtr.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(loadContext));
                }

                if (reserved != IntPtr.Zero)
                {
                    throw new ArgumentOutOfRangeException(nameof(reserved));
                }

                if (functionHandle == IntPtr.Zero)
                {
                    throw new ArgumentNullException(nameof(functionHandle));
                }

                // Create the function pointer.
                *(IntPtr*)functionHandle = InternalGetFunctionPointer(AssemblyLoadContext.Default, typeName, methodName, delegateTypeNative);
            }
            catch (Exception e)
            {
                return e.HResult;
            }

            return 0;
        }

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

        private static IntPtr InternalGetFunctionPointer(AssemblyLoadContext alc,
                                                         string typeName,
                                                         string methodName,
                                                         IntPtr delegateTypeNative)
        {
            // Create a resolver callback for types.
            Func<AssemblyName, Assembly> resolver = name => alc.LoadFromAssemblyName(name);

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
