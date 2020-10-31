// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace HostApiInvokerApp
{
    public static class HostFXR
    {
        internal static class hostfxr
        {
            [Flags]
            internal enum hostfxr_resolve_sdk2_flags_t : int
            {
                disallow_prerelease = 0x1,
            }

            internal enum hostfxr_resolve_sdk2_result_key_t : int
            {
                resolved_sdk_dir = 0,
                global_json_path = 1,
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = Utils.OSCharSet)]
            internal delegate void hostfxr_resolve_sdk2_result_fn(
                hostfxr_resolve_sdk2_result_key_t key,
                string value);

            [DllImport(nameof(hostfxr), CharSet = Utils.OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_resolve_sdk2(
                string exe_dir,
                string working_dir,
                hostfxr_resolve_sdk2_flags_t flags,
                hostfxr_resolve_sdk2_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = Utils.OSCharSet)]
            internal delegate void hostfxr_get_available_sdks_result_fn(
                int sdk_count,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                string[] sdk_dirs);

            [DllImport(nameof(hostfxr), CharSet = Utils.OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_get_available_sdks(
                string exe_dir,
                hostfxr_get_available_sdks_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = Utils.OSCharSet)]
            internal delegate void hostfxr_error_writer_fn(
                string message);

            [DllImport(nameof(hostfxr), CharSet = Utils.OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr hostfxr_set_error_writer(
                hostfxr_error_writer_fn error_writer);
        }

        /// <summary>
        /// Test invoking the native hostfxr api hostfxr_resolve_sdk2
        /// </summary>
        /// <param name="args[0]">hostfxr_get_available_sdks</param>
        /// <param name="args[1]">Directory of dotnet executable</param>
        /// <param name="args[2]">Working directory where search for global.json begins</param>
        /// <param name="args[3]">Flags</param>
        static void Test_hostfxr_resolve_sdk2(string[] args)
        {
            if (args.Length != 4)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            var data = new List<(hostfxr.hostfxr_resolve_sdk2_result_key_t, string)>();
            int rc = hostfxr.hostfxr_resolve_sdk2(
                exe_dir: args[1],
                working_dir: args[2],
                flags: Enum.Parse<hostfxr.hostfxr_resolve_sdk2_flags_t>(args[3]),
                result: (key, value) => data.Add((key, value)));

            if (rc == 0)
            {
                Console.WriteLine("hostfxr_resolve_sdk2:Success");
            }
            else
            {
                Console.WriteLine($"hostfxr_resolve_sdk2:Fail[{rc}]");
            }

            Console.WriteLine($"hostfxr_resolve_sdk2 data:[{string.Join(';', data)}]");
        }

        /// <summary>
        /// Test invoking the native hostfxr api hostfxr_get_available_sdks
        /// </summary>
        /// <param name="args[0]">hostfxr_get_available_sdks</param>
        /// <param name="args[1]">Directory of dotnet executable</param>
        static void Test_hostfxr_get_available_sdks(string[] args)
        {
            if (args.Length != 2)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            string[] sdks = null;
            int rc = hostfxr.hostfxr_get_available_sdks(
                exe_dir: args[1],
                (sdk_count, sdk_dirs) => sdks = sdk_dirs);

            if (rc == 0)
            {
                Console.WriteLine("hostfxr_get_available_sdks:Success");
                Console.WriteLine($"hostfxr_get_available_sdks sdks:[{string.Join(';', sdks)}]");
            }
            else
            {
                Console.WriteLine($"hostfxr_get_available_sdks:Fail[{rc}]");
            }
        }

        static void Test_hostfxr_set_error_writer(string[] args)
        {
            hostfxr.hostfxr_error_writer_fn writer1 = (message) => { Console.WriteLine(nameof(writer1)); };
            IntPtr writer1Ptr = Marshal.GetFunctionPointerForDelegate(writer1);

            if (hostfxr.hostfxr_set_error_writer(writer1) != IntPtr.Zero)
            {
                throw new ApplicationException("Error writer should be null by default.");
            }

            hostfxr.hostfxr_error_writer_fn writer2 = (message) => { Console.WriteLine(nameof(writer2)); };
            IntPtr writer2Ptr = Marshal.GetFunctionPointerForDelegate(writer2);
            IntPtr previousWriterPtr = hostfxr.hostfxr_set_error_writer(writer2);

            if (previousWriterPtr != writer1Ptr)
            {
                throw new ApplicationException("First: The previous writer returned is not the one expected.");
            }

            previousWriterPtr = hostfxr.hostfxr_set_error_writer(null);
            if (previousWriterPtr != writer2Ptr)
            {
                throw new ApplicationException("Second: The previous writer returned is not the one expected.");
            }
        }

        public static bool RunTest(string apiToTest, string[] args)
        {
            switch (apiToTest)
            {
                case nameof(hostfxr.hostfxr_resolve_sdk2):
                    Test_hostfxr_resolve_sdk2(args);
                    break;
                case nameof(hostfxr.hostfxr_get_available_sdks):
                    Test_hostfxr_get_available_sdks(args);
                    break;
                case nameof(Test_hostfxr_set_error_writer):
                    Test_hostfxr_set_error_writer(args);
                    break;
                default:
                    return false;
            }

            Utils.LogModulePath("hostfxr");

            return true;
        }
    }
}
