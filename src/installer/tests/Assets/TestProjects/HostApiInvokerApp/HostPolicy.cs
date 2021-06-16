// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace HostApiInvokerApp
{
    public static class HostPolicy
    {
        internal static class hostpolicy
        {
            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void corehost_resolve_component_dependencies_result_fn(
                string assembly_paths,
                string native_search_paths,
                string resource_search_paths);

            [DllImport(nameof(hostpolicy), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int corehost_resolve_component_dependencies(
                string component_main_assembly_path, 
                corehost_resolve_component_dependencies_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void corehost_error_writer_fn(
                string message);

            [DllImport(nameof(hostpolicy), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr corehost_set_error_writer(
                corehost_error_writer_fn error_writer);
        }

        static void Test_corehost_set_error_writer(string[] args)
        {
            hostpolicy.corehost_error_writer_fn writer1 = (message) => { Console.WriteLine(nameof(writer1)); };
            IntPtr writer1Ptr = Marshal.GetFunctionPointerForDelegate(writer1);

            if (hostpolicy.corehost_set_error_writer(writer1) != IntPtr.Zero)
            {
                throw new ApplicationException("Error writer should be null by default.");
            }

            hostpolicy.corehost_error_writer_fn writer2 = (message) => { Console.WriteLine(nameof(writer2)); };
            IntPtr writer2Ptr = Marshal.GetFunctionPointerForDelegate(writer2);
            IntPtr previousWriterPtr = hostpolicy.corehost_set_error_writer(writer2);

            if (previousWriterPtr != writer1Ptr)
            {
                throw new ApplicationException("First: The previous writer returned is not the one expected.");
            }

            previousWriterPtr = hostpolicy.corehost_set_error_writer(null);
            if (previousWriterPtr != writer2Ptr)
            {
                throw new ApplicationException("Second: The previous writer returned is not the one expected.");
            }
        }

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case nameof(Test_corehost_set_error_writer):
                    Test_corehost_set_error_writer(args);
                    break;
                default:
                    return false;
            }

            Utils.LogModulePath("hostpolicy");

            return true;
        }
    }
}
