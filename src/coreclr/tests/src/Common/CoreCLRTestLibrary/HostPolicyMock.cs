﻿// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Loader;

namespace TestLibrary
{
    public class HostPolicyMock
    {
    #if PLATFORM_WINDOWS
        private const CharSet HostpolicyCharSet = CharSet.Unicode;
    #else
        private const CharSet HostpolicyCharSet = CharSet.Ansi;
    #endif

        [DllImport("hostpolicy", CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern int Set_corehost_resolve_component_dependencies_Values(
            int returnValue,
            string assemblyPaths,
            string nativeSearchPaths,
            string resourceSearchPaths);

        [DllImport("hostpolicy", CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern void Set_corehost_set_error_writer_returnValue(IntPtr error_writer);

        [DllImport("hostpolicy", CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern IntPtr Get_corehost_set_error_writer_lastSet_error_writer();

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        internal delegate void Callback_corehost_resolve_component_dependencies(
            string component_main_assembly_path);

        [DllImport("hostpolicy", CallingConvention = CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        private static extern void Set_corehost_resolve_component_dependencies_Callback(
            IntPtr callback);

        private static Type _assemblyDependencyResolverType;
        private static Type _corehost_error_writer_fnType;

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = HostpolicyCharSet)]
        public delegate void ErrorWriterDelegate(string message);

        public static string DeleteExistingHostpolicy(string coreRoot)
        {
#if REFERENCING_SYSTEMPRIVATECORELIB
            throw new Exception("This API is not supported when compiled referencing SPCL");

#else
            string hostPolicyFileName = XPlatformUtils.GetStandardNativeLibraryFileName("hostpolicy");
            string destinationPath = Path.Combine(coreRoot, hostPolicyFileName);

            if (File.Exists(destinationPath))
            {
                File.Delete(destinationPath);
            }

            return destinationPath;
#endif
        }

        public static void Initialize(string testBasePath, string coreRoot)
        {
#if !REFERENCING_SYSTEMPRIVATECORELIB
            string hostPolicyFileName = XPlatformUtils.GetStandardNativeLibraryFileName("hostpolicy");
            string destinationPath = DeleteExistingHostpolicy(coreRoot);

            File.Copy(
                Path.Combine(testBasePath, hostPolicyFileName),
                destinationPath);
#endif

            _assemblyDependencyResolverType = typeof(AssemblyDependencyResolver);

            // This is needed for marshalling of function pointers to work - requires private access to the ADR unfortunately
            // Delegate marshalling doesn't support casting delegates to anything but the original type
            // so we need to use the original type.
            _corehost_error_writer_fnType = _assemblyDependencyResolverType.GetNestedType("corehost_error_writer_fn", System.Reflection.BindingFlags.NonPublic);
        }

        public static MockValues_corehost_resolve_component_dependencies Mock_corehost_resolve_component_dependencies(
            int returnValue,
            string assemblyPaths,
            string nativeSearchPaths,
            string resourceSearchPaths)
        {
            Set_corehost_resolve_component_dependencies_Values(
                returnValue,
                assemblyPaths,
                nativeSearchPaths,
                resourceSearchPaths);

            return new MockValues_corehost_resolve_component_dependencies();
        }

        public class MockValues_corehost_resolve_component_dependencies : IDisposable
        {
            private Callback_corehost_resolve_component_dependencies callback;

            public Action<string> Callback
            {
                set
                {
                    callback = new Callback_corehost_resolve_component_dependencies(value);
                    if (callback != null)
                    {
                        Set_corehost_resolve_component_dependencies_Callback(
                            Marshal.GetFunctionPointerForDelegate(callback));
                    }
                    else
                    {
                        Set_corehost_resolve_component_dependencies_Callback(IntPtr.Zero);
                    }
                }
            }

            public void Dispose()
            {
                Set_corehost_resolve_component_dependencies_Values(
                    -1,
                    string.Empty,
                    string.Empty,
                    string.Empty);
                Set_corehost_resolve_component_dependencies_Callback(IntPtr.Zero);
                GC.KeepAlive(callback);
                callback = null;
            }
        }

        public static MockValues_corehost_set_error_writer Mock_corehost_set_error_writer()
        {
            return Mock_corehost_set_error_writer(IntPtr.Zero);
        }

        public static MockValues_corehost_set_error_writer Mock_corehost_set_error_writer(IntPtr existingErrorWriter)
        {
            Set_corehost_set_error_writer_returnValue(existingErrorWriter);

            return new MockValues_corehost_set_error_writer();
        }

        public class MockValues_corehost_set_error_writer : IDisposable
        {
            public IntPtr LastSetErrorWriterPtr
            {
                get
                {
                    return Get_corehost_set_error_writer_lastSet_error_writer();
                }
            }

            public Action<string> LastSetErrorWriter
            {
                get
                {
                    IntPtr errorWriterPtr = LastSetErrorWriterPtr;
                    if (errorWriterPtr == IntPtr.Zero)
                    {
                        return null;
                    }
                    else
                    {
                        Delegate d = Marshal.GetDelegateForFunctionPointer(errorWriterPtr, _corehost_error_writer_fnType);
                        return (string message) => { d.DynamicInvoke(message); };
                    }
                }
            }

            public void Dispose()
            {
                Set_corehost_set_error_writer_returnValue(IntPtr.Zero);
            }
        }
    }
}
