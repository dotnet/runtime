using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace StandaloneApp
{
    public static class Program
    {
#if WINDOWS
       const CharSet OSCharSet = CharSet.Unicode;
#else
       const CharSet OSCharSet = CharSet.Ansi; // actually UTF8 on Unix
#endif

        [DllImport("hostfxr", CharSet = OSCharSet)]
        static extern uint hostfxr_get_native_search_directories(
            int argc, 
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            string[] argv, 
            StringBuilder buffer, 
            int bufferSize, 
            ref int required_buffer_size);

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

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = OSCharSet)]
        internal delegate void hostfxr_resolve_sdk2_result_fn(
            hostfxr_resolve_sdk2_result_key_t key,
            string value);

        [DllImport("hostfxr", CharSet = OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int hostfxr_resolve_sdk2(
            string exe_dir,
            string working_dir,
            hostfxr_resolve_sdk2_flags_t flags,
            hostfxr_resolve_sdk2_result_fn result);

        [UnmanagedFunctionPointer(CallingConvention.Cdecl, CharSet = OSCharSet)]
        internal delegate void hostfxr_get_available_sdks_result_fn(
            int sdk_count,
            [MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 0)]
            string[] sdk_dirs);

        [DllImport("hostfxr", CharSet = OSCharSet, ExactSpelling = true, CallingConvention = CallingConvention.Cdecl)]
        internal static extern int hostfxr_get_available_sdks(
            string exe_dir,
            hostfxr_get_available_sdks_result_fn result);

        const uint HostApiBufferTooSmall = 0x80008098;

        public static int Main(string[] args)
        {
            // write exception details to stdout so tha they can be seen in test assertion failures.
            try
            {
                MainCore(args);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return -1;
            }

            return 0;
        }

        public static void MainCore(string[] args)
        {
            Console.WriteLine("Hello World!");
            Console.WriteLine(string.Join(Environment.NewLine, args));

            // A small operation involving NewtonSoft.Json to ensure the assembly is loaded properly
            var t = typeof(Newtonsoft.Json.JsonReader);

            // Enable tracing so that test assertion failures are easier to diagnose.
            Environment.SetEnvironmentVariable("COREHOST_TRACE", "1");

            // If requested, test multilevel lookup using fake ProgramFiles location.
            // Note that this has to be set here and not in the calling test process because 
            // %ProgramFiles% gets reset on process creation.
            string testMultilevelLookupProgramFiles = Environment.GetEnvironmentVariable(
                "TEST_MULTILEVEL_LOOKUP_PROGRAM_FILES");

            if (testMultilevelLookupProgramFiles != null)
            {
                Environment.SetEnvironmentVariable("ProgramFiles", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("ProgramFiles(x86)", testMultilevelLookupProgramFiles);
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "1");
            }
            else
            {
                // never rely on machine state in test if we're not faking the multi-level lookup
                Environment.SetEnvironmentVariable("DOTNET_MULTILEVEL_LOOKUP", "0");
            }

            if (args.Length == 0)
            {
                throw new Exception("Invalid number of arguments passed");
            }

            string apiToTest = args[0];
            switch (apiToTest)
            {
                case nameof(hostfxr_get_native_search_directories):
                    Test_hostfxr_get_native_search_directories(args);
                    break;
                case nameof(hostfxr_resolve_sdk2):
                    Test_hostfxr_resolve_sdk2(args);
                    break;
                case nameof(hostfxr_get_available_sdks):
                    Test_hostfxr_get_available_sdks(args);
                    break;
                default:
                    throw new ArgumentException($"Invalid API to test passed as args[0]): {apiToTest}");
            }
        }

        /// <summary>
        /// Test invoking the native hostfxr api hostfxr_get_native_search_directories
        /// </summary>
        /// <param name="args[0]">hostfxr_get_native_search_directories</param>
        /// <param name="args[1]">Path to dotnet.exe</param>
        /// <param name="args[2]">Path to application</param>
        static void Test_hostfxr_get_native_search_directories(string[] args)
        {
            if (args.Length != 3)
            {
                throw new ArgumentException("Invalid number of arguments passed");
            }

            string pathToDotnet = args[1];
            string pathToApp = args[2];
            string[] argv = new[] { pathToDotnet, pathToApp };

            // Start with 0 bytes allocated to test re-entry and required_buffer_size
            StringBuilder buffer = new StringBuilder(0);
            int required_buffer_size = 0;

            uint rc = 0;
            for (int i = 0; i < 2; i++)
            {
                rc = hostfxr_get_native_search_directories(argv.Length, argv, buffer, buffer.Capacity + 1, ref required_buffer_size);
                if (rc != HostApiBufferTooSmall)
                {
                    break;
                }

                buffer = new StringBuilder(required_buffer_size);
            }

            if (rc == 0)
            {
                Console.WriteLine("hostfxr_get_native_search_directories:Success");
                Console.WriteLine($"hostfxr_get_native_search_directories buffer:[{buffer}]");
            }
            else
            {
                Console.WriteLine($"hostfxr_get_native_search_directories:Fail[{rc}]");
            }
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

            var data = new List<(hostfxr_resolve_sdk2_result_key_t, string)>();
            int rc = hostfxr_resolve_sdk2(
                exe_dir: args[1],
                working_dir: args[2],
                flags: Enum.Parse<hostfxr_resolve_sdk2_flags_t>(args[3]),
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
            int rc = hostfxr_get_available_sdks(
                exe_dir: args[1], 
                (sdk_count, sdk_dirs) => sdks = sdk_dirs);

            if (rc == 0)
            {
                Console.WriteLine("hostfxr_get_available_sdks:Success");
                Console.WriteLine($"hostfxr_get_available_sdks sdks:[{string.Join(';', sdks)}]");
            }
            else
            {
                Console.WriteLine($"hostfxr_get_native_search_directories:Fail[{rc}]");
            }
        }
    }
}
