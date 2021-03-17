// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.Collections.Generic;
using System.Linq;
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

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            internal struct hostfxr_dotnet_environment_sdk_info
            {
                internal nuint size;

                internal string version;
                internal string path;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            internal struct hostfxr_dotnet_environment_framework_info
            {
                internal nuint size;

                internal string name;
                internal string version;
                internal string path;
            }

            [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
            internal struct hostfxr_dotnet_environment_info
            {
                internal nuint size;

                internal string hostfxr_version;
                internal string hostfxr_commit_hash;

                internal nuint sdk_count;
                internal IntPtr sdks;

                internal nuint framework_count;
                internal IntPtr frameworks;
            }

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void hostfxr_resolve_sdk2_result_fn(
                hostfxr_resolve_sdk2_result_key_t key,
                string value);

            [DllImport(nameof(hostfxr), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_resolve_sdk2(
                string exe_dir,
                string working_dir,
                hostfxr_resolve_sdk2_flags_t flags,
                hostfxr_resolve_sdk2_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void hostfxr_get_available_sdks_result_fn(
                int sdk_count,
                [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
                string[] sdk_dirs);

            [DllImport(nameof(hostfxr), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_get_available_sdks(
                string exe_dir,
                hostfxr_get_available_sdks_result_fn result);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void hostfxr_error_writer_fn(
                string message);

            [DllImport(nameof(hostfxr), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern IntPtr hostfxr_set_error_writer(
                hostfxr_error_writer_fn error_writer);

            [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = CharSet.Auto)]
            internal delegate void hostfxr_get_dotnet_environment_info_result_fn(
                 IntPtr info,
                 IntPtr result_context);

            [DllImport(nameof(hostfxr), CharSet = CharSet.Auto, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
            internal static extern int hostfxr_get_dotnet_environment_info(
                string dotnet_root,
                IntPtr reserved,
                hostfxr_get_dotnet_environment_info_result_fn result,
                IntPtr result_context);
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

        /// <summary>
        /// Test that invokes native api hostfxr_get_dotnet_environment_info.
        /// </summary>
        /// <param name="args[0]">hostfxr_get_dotnet_environment_info</param>
        /// <param name="args[1]">(Optional) Path to the directory with dotnet.exe</param>
        static void Test_hostfxr_get_dotnet_environment_info(string[] args)
        {
            string dotnetExeDir = null;
            if (args.Length >= 2)
                dotnetExeDir = args[1];

            string hostfxr_version;
            string hostfxr_commit_hash;
            List<hostfxr.hostfxr_dotnet_environment_sdk_info> sdks = new List<hostfxr.hostfxr_dotnet_environment_sdk_info>();
            List<hostfxr.hostfxr_dotnet_environment_framework_info> frameworks = new List<hostfxr.hostfxr_dotnet_environment_framework_info>();

            hostfxr.hostfxr_get_dotnet_environment_info_result_fn result_fn = (IntPtr info, IntPtr result_context) =>
            {
                hostfxr.hostfxr_dotnet_environment_info environment_info = Marshal.PtrToStructure<hostfxr.hostfxr_dotnet_environment_info>(info);

                hostfxr_version = environment_info.hostfxr_version;
                hostfxr_commit_hash = environment_info.hostfxr_commit_hash;

                int env_info_size = Marshal.SizeOf(environment_info);

                if ((nuint)env_info_size != environment_info.size)
                    throw new Exception($"Size field value of hostfxr_dotnet_environment_info struct is {environment_info.size} but {env_info_size} was expected.");

                for (int i = 0; i < (int)environment_info.sdk_count; i++)
                {
                    IntPtr pSdkInfo = new IntPtr(environment_info.sdks.ToInt64() + (i * Marshal.SizeOf<hostfxr.hostfxr_dotnet_environment_sdk_info>()));
                    sdks.Add(Marshal.PtrToStructure<hostfxr.hostfxr_dotnet_environment_sdk_info>(pSdkInfo));

                    if ((nuint)Marshal.SizeOf(sdks[i]) != sdks[i].size)
                        throw new Exception($"Size field value of hostfxr_dotnet_environment_sdk_info struct is {sdks[i].size} but {Marshal.SizeOf(sdks[i])} was expected.");
                }

                for (int i = 0; i < (int)environment_info.framework_count; i++)
                {
                    IntPtr pFrameworkInfo = new IntPtr(environment_info.frameworks.ToInt64() + (i * Marshal.SizeOf<hostfxr.hostfxr_dotnet_environment_framework_info>()));
                    frameworks.Add(Marshal.PtrToStructure<hostfxr.hostfxr_dotnet_environment_framework_info>(pFrameworkInfo));

                    if ((nuint)Marshal.SizeOf(frameworks[i]) != frameworks[i].size)
                        throw new Exception($"Size field value of hostfxr_dotnet_environment_framework_info struct is {frameworks[i].size} but {Marshal.SizeOf(frameworks[i])} was expected.");
                }

                long result_context_as_int = result_context.ToInt64();
                if (result_context_as_int != 42)
                    throw new Exception($"Invalid result_context value: expected 42 but was {result_context_as_int}.");
            };

            if (dotnetExeDir == "test_invalid_result_ptr")
                result_fn = null;

            IntPtr reserved_ptr = IntPtr.Zero;
            if (dotnetExeDir == "test_invalid_reserved_ptr")
                reserved_ptr = new IntPtr(11);

            int rc = hostfxr.hostfxr_get_dotnet_environment_info(
                dotnet_root: dotnetExeDir,
                reserved: reserved_ptr,
                result: result_fn,
                result_context: new IntPtr(42));

            if (rc != 0)
            {
                Console.WriteLine($"hostfxr_get_dotnet_environment_info:Fail[{rc}]");
            }

            Console.WriteLine($"hostfxr_get_dotnet_environment_info sdk versions:[{string.Join(";", sdks.Select(s => s.version).ToList())}]");
            Console.WriteLine($"hostfxr_get_dotnet_environment_info sdk paths:[{string.Join(";", sdks.Select(s => s.path).ToList())}]");

            Console.WriteLine($"hostfxr_get_dotnet_environment_info framework names:[{string.Join(";", frameworks.Select(f => f.name).ToList())}]");
            Console.WriteLine($"hostfxr_get_dotnet_environment_info framework versions:[{string.Join(";", frameworks.Select(f => f.version).ToList())}]");
            Console.WriteLine($"hostfxr_get_dotnet_environment_info framework paths:[{string.Join(";", frameworks.Select(f => f.path).ToList())}]");

            Console.WriteLine("hostfxr_get_dotnet_environment_info:Success");
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
                case nameof(hostfxr.hostfxr_get_dotnet_environment_info):
                    Test_hostfxr_get_dotnet_environment_info(args);
                    break;
                default:
                    return false;
            }

            Utils.LogModulePath("hostfxr");

            return true;
        }
    }
}
